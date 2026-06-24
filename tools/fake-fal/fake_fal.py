"""
fake-fal: a local stand-in for Fal's queue API for end-to-end testing.

It speaks the same wire protocol as Fal, so the real FalGenerationProvider runs
unchanged — you only repoint a few URLs at this server (see README.md):

  1. Accepts the enqueue POST  /<model-path>?fal_webhook=<url>  with {"prompt": ...}
     and immediately returns {"request_id", "gateway_request_id"}.
  2. In the background, generates the image with Stable Diffusion 1.5 (ignoring
     whichever model was requested — that's the point: no paid API calls).
  3. POSTs the result back to <fal_webhook> with the same JSON body and the same
     X-Fal-Webhook-* headers Fal sends, signed with an Ed25519 key.
  4. Serves /.well-known/jwks.json with that key's public half, so the app's
     signature verifier validates these webhooks exactly as it would real ones.

Real SD1.5 only: if torch/diffusers or the weights are missing, it fails loudly.
"""
import base64
import hashlib
import json
import os
import tempfile
import threading
import time
import uuid

import requests
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey
from cryptography.hazmat.primitives.serialization import Encoding, PublicFormat
from flask import Flask, abort, jsonify, request, send_from_directory

# --- config ------------------------------------------------------------------
SD_MODEL_ID = os.environ.get("SD_MODEL_ID", "stable-diffusion-v1-5/stable-diffusion-v1-5")
SD_STEPS = int(os.environ.get("SD_STEPS", "25"))
SD_SIZE = int(os.environ.get("SD_SIZE", "512"))
PORT = int(os.environ.get("FAKE_FAL_PORT", "8080"))
# URL the app (and the browser) use to reach this server's hosted images.
PUBLIC_BASE = os.environ.get("FAKE_FAL_PUBLIC_BASE", f"http://localhost:{PORT}")
USER_ID = os.environ.get("FAKE_FAL_USER_ID", "local-user")

IMAGES_DIR = os.path.join(tempfile.gettempdir(), "fake-fal-images")
os.makedirs(IMAGES_DIR, exist_ok=True)

# --- signing key (mimics Fal's; the app fetches the public half via JWKS) -----
_private_key = Ed25519PrivateKey.generate()
_public_raw = _private_key.public_key().public_bytes(Encoding.Raw, PublicFormat.Raw)


def _b64url(raw: bytes) -> str:
    """Unpadded base64url, as used by JWK 'x' fields."""
    return base64.urlsafe_b64encode(raw).rstrip(b"=").decode()


JWK = {"kty": "OKP", "crv": "Ed25519", "use": "sig", "alg": "EdDSA", "x": _b64url(_public_raw)}

# --- Stable Diffusion 1.5 (loaded once at startup; real-only) -----------------
print(f"[fake-fal] loading {SD_MODEL_ID} ...")
import torch  # noqa: E402  (heavy import; keep it after the cheap config above)
from diffusers import StableDiffusionPipeline  # noqa: E402

_device = "cuda" if torch.cuda.is_available() else "cpu"
_dtype = torch.float16 if _device == "cuda" else torch.float32
_pipe = StableDiffusionPipeline.from_pretrained(
    SD_MODEL_ID, torch_dtype=_dtype, safety_checker=None, requires_safety_checker=False
).to(_device)
print(f"[fake-fal] SD1.5 ready on {_device}.")

app = Flask(__name__)


@app.get("/.well-known/jwks.json")
def jwks():
    return jsonify({"keys": [JWK]})


@app.get("/images/<name>")
def images(name):
    return send_from_directory(IMAGES_DIR, name)


@app.post("/<path:model_path>")
def enqueue(model_path):
    webhook_url = request.args.get("fal_webhook")
    if not webhook_url:
        abort(400, "missing fal_webhook query parameter")

    prompt = (request.get_json(silent=True) or {}).get("prompt", "")
    request_id = str(uuid.uuid4())
    gateway_request_id = str(uuid.uuid4())

    # Generate + deliver off the request thread, exactly like Fal's async queue.
    threading.Thread(
        target=_process,
        args=(request_id, gateway_request_id, prompt, webhook_url, model_path),
        daemon=True,
    ).start()

    print(f"[fake-fal] enqueued {request_id} (requested '{model_path}') -> SD1.5")
    return jsonify({"request_id": request_id, "gateway_request_id": gateway_request_id})


def _process(request_id, gateway_request_id, prompt, webhook_url, model_path):
    try:
        image = _pipe(prompt, num_inference_steps=SD_STEPS, height=SD_SIZE, width=SD_SIZE).images[0]
        filename = f"{request_id}.png"
        image.save(os.path.join(IMAGES_DIR, filename))
        width, height = image.size
        payload = {
            "request_id": request_id,
            "gateway_request_id": gateway_request_id,
            "status": "OK",
            "payload": {
                "images": [{
                    "url": f"{PUBLIC_BASE}/images/{filename}",
                    "width": width,
                    "height": height,
                    "content_type": "image/png",
                }],
                "prompt": prompt,
                "seed": 0,
                "has_nsfw_concepts": [False],
            },
            "error": None,
        }
    except Exception as exc:  # surface failures the same shape Fal does
        print(f"[fake-fal] generation failed for {request_id}: {exc}")
        payload = {
            "request_id": request_id,
            "gateway_request_id": gateway_request_id,
            "status": "ERROR",
            "payload": None,
            "error": str(exc),
        }

    _deliver(webhook_url, payload)


def _deliver(webhook_url, payload):
    # Sign and send the EXACT bytes the app will hash — never let requests
    # re-serialize the body, or the signature won't match.
    body = json.dumps(payload).encode("utf-8")
    timestamp = str(int(time.time()))
    body_hash = hashlib.sha256(body).hexdigest()
    message = f"{payload['request_id']}\n{USER_ID}\n{timestamp}\n{body_hash}".encode("utf-8")
    signature = _private_key.sign(message).hex()

    headers = {
        "Content-Type": "application/json",
        "X-Fal-Webhook-Request-Id": payload["request_id"],
        "X-Fal-Webhook-User-Id": USER_ID,
        "X-Fal-Webhook-Timestamp": timestamp,
        "X-Fal-Webhook-Signature": signature,
    }
    try:
        resp = requests.post(webhook_url, data=body, headers=headers, timeout=30)
        print(f"[fake-fal] delivered {payload['request_id']} -> {webhook_url} [{resp.status_code}]")
    except requests.RequestException as exc:
        print(f"[fake-fal] webhook delivery to {webhook_url} failed: {exc}")


if __name__ == "__main__":
    print(f"[fake-fal] JWKS at {PUBLIC_BASE}/.well-known/jwks.json")
    print(f"[fake-fal] images served from {PUBLIC_BASE}/images/  (disk: {IMAGES_DIR})")
    # threaded=True so image serving / new enqueues aren't blocked by generation.
    app.run(host="0.0.0.0", port=PORT, threaded=True)
