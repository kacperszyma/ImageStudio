import { Link } from "react-router"
import { ArrowLeft } from "lucide-react"
import { Footer } from "@/components/Footer"

const EFFECTIVE_DATE = "2026-07-18"
const CONTACT_EMAIL = "k.szymanski617@gmail.com"

export default function TermsPage() {
  return (
    <div className="flex min-h-screen flex-col bg-background text-foreground">
      <header className="mx-auto flex w-full max-w-3xl items-center gap-2 px-6 py-6">
        <img src="/favicon.svg" alt="" className="h-7 w-7" />
        <span className="font-heading text-base font-semibold tracking-tight">
          ImageStudio
        </span>
      </header>

      <main className="mx-auto w-full max-w-3xl flex-1 px-6 pb-24">
        <Link
          to="/"
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to ImageStudio
        </Link>

        <h1 className="mt-6 text-3xl font-semibold tracking-tight font-heading sm:text-4xl">
          Terms & Conditions
        </h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Effective as of {EFFECTIVE_DATE}
        </p>

        <div className="mt-10 flex flex-col gap-10 text-sm leading-relaxed text-muted-foreground [&_h2]:font-heading [&_h2]:text-lg [&_h2]:font-semibold [&_h2]:text-foreground [&_p]:mt-3 [&_ul]:mt-3 [&_ul]:list-disc [&_ul]:pl-5 [&_li]:mt-1.5 [&_a]:text-foreground [&_a]:underline [&_a]:underline-offset-2">
          <section>
            <p>
              These Terms and Conditions apply to ImageStudio, the web application
              and related services (collectively, the &ldquo;Application&rdquo;)
              operated by Kacper Szymański (the &ldquo;Service Provider&rdquo;).
              ImageStudio is a web application only — there is no companion mobile
              app.
            </p>
            <p>
              By accessing or using the Application, you agree to these Terms. You
              should read them carefully before using the Application.
            </p>
          </section>

          <section>
            <h2>License to use the Application</h2>
            <p>
              Subject to your compliance with these Terms, the Service Provider
              grants you a limited, non-exclusive, non-transferable, revocable
              license to access and use the Application for personal or internal
              business purposes. You may not reproduce, distribute, modify, create
              derivative works from, reverse engineer, decompile, or disassemble
              the Application, except to the extent expressly permitted by
              applicable law.
            </p>
          </section>

          <section>
            <h2>Intellectual property</h2>
            <p>
              The Service Provider retains all intellectual property rights in the
              Application, including its code, design, trademarks, trade names,
              logos, and branding. Nothing in these Terms grants you any license or
              right to use the Service Provider's trademarks, logos, or branding
              for any purpose. You agree not to remove, alter, or obscure any
              copyright, trademark, or other proprietary notices displayed in the
              Application.
            </p>
          </section>

          <section>
            <h2>Eligibility</h2>
            <p>
              By using the Application, you represent that you are legally
              permitted to do so in your jurisdiction. You must be at least 16
              years of age (or the age of digital consent in your jurisdiction) to
              use the Application. If you are below that age, a parent or legal
              guardian must review and accept these Terms on your behalf.
            </p>
          </section>

          <section>
            <h2>Purchases and Pebbles (virtual currency)</h2>
            <p>
              The Application uses a virtual currency, Pebbles, which you may
              purchase to generate images. Payments are processed by Stripe; the
              Service Provider does not store your full payment card details.
            </p>
            <ul>
              <li>Pebbles have no monetary value outside the Application and cannot be exchanged for cash.</li>
              <li>Pebbles are non-transferable between accounts.</li>
              <li>Purchases are generally non-refundable, except where required by applicable consumer protection law or where a paid generation fails to complete due to an error on the Service Provider's side.</li>
              <li>Prices for Pebbles and generations may change at any time; changes will not affect Pebbles already purchased.</li>
            </ul>
            <p>
              If you believe you were charged in error or a generation failed
              without a refund of the Pebbles spent, contact{" "}
              <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.
            </p>
          </section>

          <section>
            <h2>Use of Artificial Intelligence</h2>
            <p>
              The Application's core function is generating images using
              third-party AI models. When you submit a prompt (and optionally a
              reference image), it is sent to our AI infrastructure provider,
              fal.ai, which routes the request to the model you selected: Black
              Forest Labs (Flux), xAI (Grok Image), Google (Nano Banana), or
              OpenAI (GPT Image). You are responsible for the prompts you submit
              and the outputs you request. Generated images may occasionally be
              inaccurate, unexpected, or unsuitable for your intended use — the
              Application is provided without warranty as to the outputs of any
              AI model, as described under Limitation of Liability below.
            </p>
          </section>

          <section>
            <h2>Acceptable use</h2>
            <p>You agree not to use the Application to generate or submit prompts or reference images that:</p>
            <ul>
              <li>Are illegal or infringe a third party's intellectual property rights</li>
              <li>Depict real, identifiable individuals without their consent</li>
              <li>Constitute child sexual abuse material, or otherwise sexualize minors</li>
              <li>Are intended to harass, defame, or threaten another person</li>
              <li>Attempt to generate malware, phishing content, or facilitate fraud</li>
            </ul>
            <p>
              The Service Provider reserves the right to remove content, suspend,
              or terminate accounts that violate this section, and to cooperate
              with law enforcement where illegal content is identified. Your
              generations are private to your account — the Application does not
              publish, share, or otherwise make your generations visible to other
              users or the public.
            </p>
          </section>

          <section>
            <h2>Your content</h2>
            <p>
              You retain ownership of the prompts and reference images you submit,
              and of the images generated for you, subject to the license terms of
              the underlying AI model provider that produced them. You grant the
              Service Provider a limited license to store and process this content
              solely to provide the Application to you (for example, to display
              your generation history). The Service Provider does not use your
              prompts or generated images to train its own models. Processing of
              personal data contained in your content is governed by the{" "}
              <Link to="/privacy">Privacy Policy</Link>.
            </p>
          </section>

          <section>
            <h2>Termination</h2>
            <p>
              The Service Provider may suspend your access to the Application if
              you materially breach these Terms, and will provide notice of the
              breach with 14 days to remedy it where the breach is capable of
              cure. The Service Provider may suspend or terminate your access
              immediately, without notice, if you violate applicable law, infringe
              intellectual property rights, or engage in activity that could harm
              other users or the Service Provider. Upon termination, your right to
              use the Application ends.
            </p>
          </section>

          <section>
            <h2>Availability</h2>
            <p>
              The Service Provider may update, modify, or discontinue the
              Application, or any part of it, at any time. Some functions require
              an active internet connection; the Service Provider is not
              responsible for the Application's unavailability due to your own
              connectivity issues. Nothing in these Terms limits any rights you
              have under applicable consumer protection laws that cannot be
              lawfully excluded.
            </p>
          </section>

          <section>
            <h2>Limitation of liability</h2>
            <p>
              To the fullest extent permitted by law, the Service Provider shall
              not be liable for any indirect, incidental, special, consequential,
              or punitive damages, including lost profits, data loss, or business
              interruption, even if advised of the possibility of such damages.
            </p>
            <p>However, the Service Provider retains full liability for:</p>
            <ul>
              <li>Death or personal injury caused by negligence</li>
              <li>Fraud or fraudulent misrepresentation</li>
              <li>Any other liability that cannot be excluded or limited under applicable law</li>
            </ul>
            <p>
              To the fullest extent permitted by law, the Service Provider's total
              liability for any claim shall not exceed the amount you paid for
              Pebbles in the 12 months preceding the claim, or the minimum amount
              required under applicable law, whichever is greater. The Service
              Provider accepts no liability for outputs generated by third-party
              AI models beyond what is required by applicable law.
            </p>
          </section>

          <section>
            <h2>Indemnification</h2>
            <p>
              To the fullest extent permitted by law, you agree to indemnify and
              hold harmless the Service Provider from claims, liabilities,
              damages, losses, and reasonable legal fees arising out of your
              breach of these Terms or your intentional misuse of the
              Application. This indemnification does not apply to claims arising
              from the Service Provider's own negligence, breach of these Terms,
              or violation of applicable law. In jurisdictions where consumer
              indemnification is restricted by law, this clause is limited to the
              maximum extent permitted.
            </p>
          </section>

          <section>
            <h2>Governing law and jurisdiction</h2>
            <p>
              These Terms are governed by the laws of Poland, excluding conflict
              of law rules, except to the extent mandatory consumer protection
              laws of your country of residence provide otherwise. Disputes
              arising out of or relating to these Terms will be brought before the
              courts with jurisdiction under applicable law, without limiting any
              right you may have to bring a claim in a court competent under
              mandatory law.
            </p>
          </section>

          <section>
            <h2>Severability</h2>
            <p>
              If any provision of these Terms is held invalid, illegal, or
              unenforceable, that provision will be modified to the minimum extent
              necessary to make it valid and enforceable, and the remaining
              provisions will remain in full force and effect.
            </p>
          </section>

          <section>
            <h2>Entire agreement</h2>
            <p>
              These Terms, together with the{" "}
              <Link to="/privacy">Privacy Policy</Link>, constitute the entire
              agreement between you and the Service Provider concerning your use
              of the Application, superseding any prior agreements or
              understandings.
            </p>
          </section>

          <section>
            <h2>Changes to these Terms</h2>
            <p>
              The Service Provider may update these Terms from time to time.
              Material changes will be posted on this page with an updated
              effective date. Previous versions are available on request by
              contacting <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.
            </p>
          </section>

          <section>
            <h2>Contact us</h2>
            <p>
              If you have any questions or suggestions about these Terms, please
              contact the Service Provider at{" "}
              <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.
            </p>
          </section>
        </div>
      </main>

      <Footer />
    </div>
  )
}
