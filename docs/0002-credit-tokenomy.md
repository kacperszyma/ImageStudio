# 0002 — Credit Tokenomy

## Model Pricing

Credits are set 1:1 with fal cost in millicents (no markup baked in). The dollar-to-credit exchange rate is TBD and applies uniformly as a multiplier.

| Model | Slug | Fal ID | Credits | Fal cost |
|---|---|---|---|---|
| FLUX.1 Schnell | `flux-schnell` | `fal-ai/flux/schnell` | 3 | $0.003 |
| Grok Image | `grok-image` | `xai/grok-imagine-image` | 22 | $0.022 |
| FLUX 2 Pro | `flux-2-pro` | `fal-ai/flux-2-pro` | 30 | $0.030 |
| Nano Banana 2 | `nano-banana-2` | `fal-ai/nano-banana-2` | 80 | $0.080 |
| Nano Banana Pro | `nano-banana-pro` | `fal-ai/nano-banana-pro` | 150 | $0.150 |
| GPT Image 2 | `gpt-image-2` | `openai/gpt-image-2` | 167 | $0.167 |

## Exchange Rate

TBD. When set, the effective price per generation = `(credits / exchange_rate) * $1`.

Example: 1000 credits = $5 → exchange rate = 200 → flux-schnell costs $0.015, gpt-image-2 costs $0.835.
