import requests
from dotenv import dotenv_values

config = dotenv_values(".env")
key = config["FAL_API_KEY"]

endpoint_ids = [
    "fal-ai/flux/schnell",
    "fal-ai/flux-pro/v1.1",
    "fal-ai/flux-2-pro",
    "openai/gpt-image-2",
    "fal-ai/nano-banana-pro",
    "xai/grok-imagine-image",
]

response = requests.get(
    "https://fal.ai/api/models",
    headers={"Authorization": f"Key {key}"},
    params=[("endpoint_id", eid) for eid in endpoint_ids],
)

data = response.json()
for m in data["items"]:
    print(f"{m['id']} - {m['title']}")
    print(f"  {m.get('pricingInfoOverride') or m.get('shortDescription', 'no pricing info')}")
    print()
