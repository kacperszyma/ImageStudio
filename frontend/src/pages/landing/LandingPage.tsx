import { Link } from "react-router"
import { useAuth0 } from "@auth0/auth0-react"
import { ArrowRight, ChevronDown } from "lucide-react"
import { Button } from "@/components/ui/button"
import { PrismIllustration } from "./subcomponents/PrismIllustration"
import { ModelLogos } from "./subcomponents/ModelLogos"

export default function LandingPage() {
  const { loginWithRedirect } = useAuth0()

  const signup = () =>
    loginWithRedirect({ authorizationParams: { screen_hint: "signup" } })
  const login = () => loginWithRedirect()

  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <header className="mx-auto flex w-full max-w-6xl items-center justify-between px-6 py-6">
        <div className="flex items-center gap-2">
          <img src="/favicon.svg" alt="" className="h-7 w-7" />
          <span className="font-heading text-base font-semibold tracking-tight">
            ImageStudio
          </span>
        </div>
        <Button variant="ghost" size="sm" onClick={login}>
          Log in
        </Button>
      </header>

      <main className="flex-1">
        <section className="mx-auto max-w-6xl px-6 pt-6 pb-20 sm:pt-10">
          <div className="grid items-center gap-12 lg:grid-cols-2 lg:gap-16">
            <div>
              <h1 className="text-4xl font-semibold tracking-tight text-balance font-heading sm:text-5xl md:text-6xl">
                One prompt. Every model worth using.
              </h1>

              <p className="mt-5 text-base text-pretty text-muted-foreground sm:text-lg">
                Flux, Grok Image, Nano Banana and GPT Image, side by side in one
                studio. Switch models mid-project and pay only for the renders
                you keep — no subscriptions, just Pebbles in one balance.
              </p>

              <div className="mt-8 flex flex-col items-start justify-center gap-3 sm:flex-row">
                <Button size="lg" onClick={signup}>
                  Start creating free
                  <ArrowRight data-icon="inline-end" />
                </Button>
                <Button size="lg" variant="outline" render={<a href="#models" />}>
                  See the models
                  <ChevronDown data-icon="inline-end" />
                </Button>
              </div>
            </div>

            <PrismIllustration />
          </div>
        </section>

        <section id="models" className="border-t border-border">
          <div className="mx-auto max-w-6xl px-6 py-16 sm:py-20">
            <h2 className="max-w-xl text-2xl font-semibold tracking-tight font-heading sm:text-3xl">
              Every leading model. One balance.
            </h2>

            <div className="mt-10">
              <ModelLogos />
            </div>
          </div>
        </section>
      </main>

      <footer className="border-t border-border">
        <div className="mx-auto flex w-full max-w-6xl flex-col items-center justify-between gap-4 px-6 py-8 text-sm text-muted-foreground sm:flex-row">
          <div className="flex items-center gap-2">
            <img src="/favicon.svg" alt="" className="h-5 w-5" />
            <span className="font-heading font-medium text-foreground">
              ImageStudio
            </span>
          </div>
          <nav className="flex items-center gap-6">
            <a href="#models" className="transition-colors hover:text-foreground">
              Models
            </a>
            <Link to="/privacy" className="transition-colors hover:text-foreground">
              Privacy Policy
            </Link>
            <Link to="/terms" className="transition-colors hover:text-foreground">
              Terms
            </Link>
            <button
              onClick={login}
              className="transition-colors hover:text-foreground"
            >
              Log in
            </button>
          </nav>
          <span>© 2026 ImageStudio</span>
        </div>
      </footer>
    </div>
  )
}
