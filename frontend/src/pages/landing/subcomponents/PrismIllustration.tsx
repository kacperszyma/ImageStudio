type Take = {
  model: string
  provider: string
  color: string
  fill: string
}

const takes: Take[] = [
  {
    model: "Flux 2 Pro",
    provider: "Black Forest Labs",
    color: "#7e14ff",
    fill: "radial-gradient(circle at 32% 28%, #caa2ff 0%, #8f2bff 45%, #4a0ecf 100%)",
  },
  {
    model: "Grok Image",
    provider: "xAI",
    color: "#ffb238",
    fill: "linear-gradient(135deg, #ffe2ad 0%, #ffb238 55%, #d97a06 100%)",
  },
  {
    model: "Nano Banana Pro",
    provider: "Google",
    color: "#47bfff",
    fill: "linear-gradient(155deg, #cdf0ff 0%, #47bfff 55%, #1177b3 100%)",
  },
  {
    model: "GPT Image 2",
    provider: "OpenAI",
    color: "#ff4fa3",
    fill: "conic-gradient(from 200deg, #ffd3ea 0%, #ff4fa3 45%, #b8157a 100%)",
  },
]

// The signature visual: one prompt, refracted through the studio's prism mark
// into four distinct model outputs — the page's whole thesis in one panel.
export function PrismIllustration() {
  return (
    <div
      className="relative overflow-hidden rounded-3xl border border-border bg-card/60 px-6 py-8 sm:px-10 sm:py-10"
      aria-hidden="true"
    >
      <div
        className="pointer-events-none absolute -top-24 left-1/2 h-64 w-64 -translate-x-1/2 rounded-full opacity-20 blur-3xl"
        style={{ background: "#7e14ff" }}
      />

      <div className="relative flex flex-col items-center gap-6 md:flex-row md:items-center md:gap-6">
        <div className="flex shrink-0 items-center gap-2 rounded-full border border-dashed border-[#7e14ff]/40 bg-background/80 px-4 py-2 text-sm text-muted-foreground">
          <span>&ldquo;a fox reading, watercolor&rdquo;</span>
        </div>

        <div className="hidden h-px flex-1 bg-gradient-to-r from-[#7e14ff]/30 to-[#7e14ff] md:block" />
        <div className="h-6 w-px bg-gradient-to-b from-[#7e14ff]/30 to-[#7e14ff] md:hidden" />

        <img
          src="/favicon.svg"
          alt=""
          className="h-14 w-14 shrink-0 drop-shadow-[0_0_24px_rgba(126,20,255,0.45)]"
        />

        <div className="hidden h-px w-6 bg-[#7e14ff] md:block" />
        <div className="h-6 w-px bg-[#7e14ff] md:hidden" />

        <div className="flex w-full flex-col gap-3 md:w-auto md:border-l md:border-border md:pl-6">
          {takes.map((take) => (
            <div key={take.model} className="flex items-center gap-3">
              <span
                className="hidden h-px w-4 shrink-0 md:block"
                style={{ background: take.color }}
              />
              <span
                className="h-10 w-10 shrink-0 rounded-lg"
                style={{ background: take.fill }}
              />
              <div className="text-xs leading-tight">
                <div className="font-medium text-foreground">{take.model}</div>
                <div className="text-muted-foreground">{take.provider}</div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
