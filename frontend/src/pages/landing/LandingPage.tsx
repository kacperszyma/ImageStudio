import { useAuth0 } from "@auth0/auth0-react"
import { ArrowRight } from "lucide-react"
import { Button } from "@/components/ui/button"
import { ModelLogos } from "./subcomponents/ModelLogos"
import { Footer } from "@/components/Footer"

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
                Every top image model. One studio.
              </h1>

              <p className="mt-5 text-base text-pretty text-muted-foreground sm:text-lg">
                Flux, Grok Image, Nano Banana and GPT Image — switch mid-project
                and pay only for the renders you keep. No subscriptions. Just
                Pebbles.
              </p>

              <div className="mt-8 flex justify-start">
                <Button
                  size="lg"
                  onClick={signup}
                  className="h-14 px-8 text-base [&_svg:not([class*='size-'])]:size-5"
                >
                  Start creating free
                  <ArrowRight data-icon="inline-end" />
                </Button>
              </div>
            </div>

            <img
              src="/mission-control.png"
              alt="ImageStudio mission control"
              className="w-full rounded-3xl border border-border object-cover"
            />
          </div>
        </section>

        <section id="models" className="border-t border-border py-16 sm:py-20">
          <h2 className="mx-auto max-w-xl px-6 text-2xl font-semibold tracking-tight font-heading sm:text-3xl">
            Every leading model. One balance.
          </h2>

          <div className="mt-10">
            <ModelLogos />
          </div>
        </section>
      </main>

      <Footer onLoginClick={login} showModelsLink />
    </div>
  )
}
