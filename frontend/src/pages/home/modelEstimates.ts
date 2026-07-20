// Rough calibration for the fake-progress curve — not an SLA. Used only to
// pace how fast the progress bar creeps while we wait on the provider
// webhook, so slower models don't look "stuck" sooner than faster ones.
const ESTIMATED_SECONDS: Record<string, number> = {
  "flux-schnell": 4,
  "grok-image": 10,
  "flux-2-pro": 15,
  "nano-banana-2": 15,
  "nano-banana-pro": 30,
  "gpt-image-2": 25,
}

const DEFAULT_ESTIMATED_SECONDS = 15

export function estimatedGenerationMs(modelSlug: string): number {
  return (ESTIMATED_SECONDS[modelSlug] ?? DEFAULT_ESTIMATED_SECONDS) * 1000
}
