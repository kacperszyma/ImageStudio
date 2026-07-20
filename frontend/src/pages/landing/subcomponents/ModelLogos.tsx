import type { ReactNode } from "react"

type Provider = {
  name: string
  models: string[]
  mark: ReactNode
}

function FluxMark() {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className="h-6 w-6">
      <path d="M0 20.683L12.01 2.5 24 20.683h-2.233L12.009 5.878 3.471 18.806h12.122l1.239 1.877H0z" />
      <path d="M8.069 16.724l2.073-3.115 2.074 3.115H8.069zM18.24 20.683l-5.668-8.707h2.177l5.686 8.707h-2.196zM19.74 11.676l2.13-3.19 2.13 3.19h-4.26z" />
    </svg>
  )
}

function GrokMark() {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className="h-6 w-6">
      <path d="M9.27 15.29l7.978-5.897c.391-.29.95-.177 1.137.272.98 2.369.542 5.215-1.41 7.169-1.951 1.954-4.667 2.382-7.149 1.406l-2.711 1.257c3.889 2.661 8.611 2.003 11.562-.953 2.341-2.344 3.066-5.539 2.388-8.42l.006.007c-.983-4.232.242-5.924 2.75-9.383.06-.082.12-.164.179-.248l-3.301 3.305v-.01L9.267 15.292M7.623 16.723c-2.792-2.67-2.31-6.801.071-9.184 1.761-1.763 4.647-2.483 7.166-1.425l2.705-1.25a7.808 7.808 0 00-1.829-1A8.975 8.975 0 005.984 5.83c-2.533 2.536-3.33 6.436-1.962 9.764 1.022 2.487-.653 4.246-2.34 6.022-.599.63-1.199 1.259-1.682 1.925l7.62-6.815" />
    </svg>
  )
}

function OpenAiMark() {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className="h-6 w-6">
      <path d="M9.205 8.658v-2.26c0-.19.072-.333.238-.428l4.543-2.616c.619-.357 1.356-.523 2.117-.523 2.854 0 4.662 2.212 4.662 4.566 0 .167 0 .357-.024.547l-4.71-2.759a.797.797 0 00-.856 0l-5.97 3.473zm10.609 8.8V12.06c0-.333-.143-.57-.429-.737l-5.97-3.473 1.95-1.118a.433.433 0 01.476 0l4.543 2.617c1.309.76 2.189 2.378 2.189 3.948 0 1.808-1.07 3.473-2.76 4.163zM7.802 12.703l-1.95-1.142c-.167-.095-.239-.238-.239-.428V5.899c0-2.545 1.95-4.472 4.591-4.472 1 0 1.927.333 2.712.928L8.23 5.067c-.285.166-.428.404-.428.737v6.898zM12 15.128l-2.795-1.57v-3.33L12 8.658l2.795 1.57v3.33L12 15.128zm1.796 7.23c-1 0-1.927-.332-2.712-.927l4.686-2.712c.285-.166.428-.404.428-.737v-6.898l1.974 1.142c.167.095.238.238.238.428v5.233c0 2.545-1.974 4.472-4.614 4.472zm-5.637-5.303l-4.544-2.617c-1.308-.761-2.188-2.378-2.188-3.948A4.482 4.482 0 014.21 6.327v5.423c0 .333.143.571.428.738l5.947 3.449-1.95 1.118a.432.432 0 01-.476 0zm-.262 3.9c-2.688 0-4.662-2.021-4.662-4.519 0-.19.024-.38.047-.57l4.686 2.71c.286.167.571.167.856 0l5.97-3.448v2.26c0 .19-.07.333-.237.428l-4.543 2.616c-.619.357-1.356.523-2.117.523zm5.899 2.83a5.947 5.947 0 005.827-4.756C22.287 18.339 24 15.84 24 13.296c0-1.665-.713-3.282-1.998-4.448.119-.5.19-.999.19-1.498 0-3.401-2.759-5.947-5.946-5.947-.642 0-1.26.095-1.88.31A5.962 5.962 0 0010.205 0a5.947 5.947 0 00-5.827 4.757C1.713 5.447 0 7.945 0 10.49c0 1.666.713 3.283 1.998 4.448-.119.5-.19 1-.19 1.499 0 3.401 2.759 5.946 5.946 5.946.642 0 1.26-.095 1.88-.309a5.96 5.96 0 004.162 1.713z" />
    </svg>
  )
}

function NanoBananaMark() {
  return (
    <img
      src="/nanobanana-color.svg"
      alt=""
      className="h-6 w-6 grayscale brightness-0 opacity-60 dark:invert"
    />
  )
}

const providers: Provider[] = [
  {
    name: "Black Forest Labs",
    models: ["Flux 2 Pro", "Flux Schnell"],
    mark: <FluxMark />,
  },
  {
    name: "xAI",
    models: ["Grok Image"],
    mark: <GrokMark />,
  },
  {
    name: "Google",
    models: ["Nano Banana Pro", "Nano Banana 2"],
    mark: <NanoBananaMark />,
  },
  {
    name: "OpenAI",
    models: ["GPT Image 2"],
    mark: <OpenAiMark />,
  },
]

// One "half" of the strip must stay wider than any real viewport, or the
// loop point becomes visible as a jump once the last icon scrolls off.
const COPIES_PER_HALF = 6
const loop = Array.from({ length: COPIES_PER_HALF * 2 }, () => providers).flat()

export function ModelLogos() {
  return (
    <div className="relative w-full overflow-hidden border-y border-border bg-card/40 backdrop-blur-xl">
      <div
        className="pointer-events-none absolute -top-24 left-1/2 h-64 w-64 -translate-x-1/2 rounded-full opacity-20 blur-3xl"
        style={{ background: "#f7ec6a" }}
        aria-hidden="true"
      />

      <div className="relative py-8 [mask-image:linear-gradient(to_right,transparent,black_8%,black_92%,transparent)]">
        <ul
          className="flex w-max animate-marquee gap-4 px-6"
          style={{ animationDuration: `${28 * COPIES_PER_HALF}s` }}
        >
          {loop.map((provider, i) => (
            <li
              key={`${provider.name}-${i}`}
              aria-hidden={i >= providers.length}
              className="flex w-64 shrink-0 items-center gap-3 px-6 py-4"
            >
              <span className="flex h-10 w-10 shrink-0 items-center justify-center text-foreground/60">
                {provider.mark}
              </span>
              <div className="min-w-0">
                <div className="font-heading text-sm font-medium text-foreground/60 truncate">
                  {provider.name}
                </div>
                <div className="mt-0.5 text-xs text-foreground/40 truncate">
                  {provider.models.join(" · ")}
                </div>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}
