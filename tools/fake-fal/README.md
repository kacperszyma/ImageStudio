# fake-fal

A local stand-in for Fal's queue API so you can run the app **end-to-end without
spending real money**. It generates every image with Stable Diffusion 1.5
(whatever model the UI picks is ignored) and posts a **properly signed** webhook
back to the API — so the real `FalGenerationProvider` *and* the signature
verifier run exactly as they do against production Fal.

## How it works

```
App ──POST /<model>?fal_webhook=URL──▶ fake-fal      (returns request_id)
                                          │ SD1.5 generates
App ◀──signed POST URL (X-Fal-Webhook-*)──┘
App ──GET /.well-known/jwks.json──▶ fake-fal          (verifies the signature)
Browser ──GET /images/<id>.png──▶ fake-fal            (renders the result)
```

Nothing in the C# changes between local and prod — only the URLs below differ.

## Setup

```bash
cd tools/fake-fal
pip install -r requirements.txt
```

First run downloads the SD1.5 weights (~4 GB) from Hugging Face. CPU works but is
slow; a CUDA GPU is used automatically if present.

## Run

```bash
python fake_fal.py        # listens on :8080
```

Then point the API at it via the backend `.env`:

```dotenv
FAL_API_KEY=local
FAL_WEBHOOK_URL=http://localhost:5253/api/fal/webhook
FAL_QUEUE_URL=http://localhost:8080/
FAL_JWKS_URL=http://localhost:8080/.well-known/jwks.json
```

`FAL_QUEUE_URL` and `FAL_JWKS_URL` default to real Fal when unset, so omit them
to switch back to production. `FAL_WEBHOOK_URL`'s path must match the API's
webhook route (`/api/fal/webhook`); the host:port must be reachable from this
server (localhost is fine since both run on your machine).

## Tunables (env vars)

| var | default | meaning |
|-----|---------|---------|
| `SD_MODEL_ID` | `stable-diffusion-v1-5/stable-diffusion-v1-5` | HF model id |
| `SD_STEPS` | `25` | inference steps (lower = faster) |
| `SD_SIZE` | `512` | square output size |
| `FAKE_FAL_PORT` | `8080` | listen port |
| `FAKE_FAL_PUBLIC_BASE` | `http://localhost:8080` | base URL embedded in image links |
