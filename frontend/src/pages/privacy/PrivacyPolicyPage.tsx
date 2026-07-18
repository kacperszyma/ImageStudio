import { Link } from "react-router"
import { ArrowLeft } from "lucide-react"

const EFFECTIVE_DATE = "2026-07-18"
const CONTACT_EMAIL = "k.szymanski617@gmail.com"

export default function PrivacyPolicyPage() {
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
          Privacy Policy
        </h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Effective as of {EFFECTIVE_DATE}
        </p>

        <div className="mt-10 flex flex-col gap-10 text-sm leading-relaxed text-muted-foreground [&_h2]:font-heading [&_h2]:text-lg [&_h2]:font-semibold [&_h2]:text-foreground [&_p]:mt-3 [&_ul]:mt-3 [&_ul]:list-disc [&_ul]:pl-5 [&_li]:mt-1.5 [&_a]:text-foreground [&_a]:underline [&_a]:underline-offset-2">
          <section>
            <p>
              This privacy policy applies to ImageStudio, the web application
              and related services (collectively, the &ldquo;Application&rdquo;)
              operated by Kacper Szymański (the &ldquo;Service Provider&rdquo;).
              ImageStudio is a web application only — there is no companion
              mobile app.
            </p>
          </section>

          <section>
            <h2>Data controller information</h2>
            <p>Kacper Szymański acts as the Data Controller responsible for the processing of your personal data.</p>
            <ul>
              <li>Name: Kacper Szymański</li>
              <li>Address: Warsaw, Poland</li>
              <li>Email: <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a></li>
            </ul>
            <p>
              For data protection inquiries and to exercise your GDPR rights, please
              contact the Data Controller using the details above.
            </p>
          </section>

          <section>
            <h2>What information does the Application obtain and how is it used?</h2>
            <p>
              The Application collects the information you supply when you register
              for and use the service — including your email address (via our
              authentication provider), the prompts and reference images you submit
              to generate content, your generation history, and your token/billing
              activity. Registration is required to use the Application.
            </p>
            <p>
              The Service Provider may also use this information to send important
              account notices and, where permitted by law, marketing communications.
            </p>
          </section>

          <section>
            <h2>Legal basis for processing your personal data</h2>
            <p>Where the GDPR applies, the Service Provider relies on one or more of the following lawful bases:</p>
            <ul>
              <li><strong className="text-foreground">Contract performance:</strong> processing necessary to provide the Application, including generating images you request and managing your token balance.</li>
              <li><strong className="text-foreground">Consent:</strong> where you have given explicit consent, including for marketing communications. You may withdraw consent at any time without affecting processing that occurred before withdrawal.</li>
              <li><strong className="text-foreground">Legitimate interests:</strong> for maintaining security, preventing fraud and abuse, and improving the Application's core functionality.</li>
              <li><strong className="text-foreground">Legal obligation:</strong> to comply with applicable laws or government requests.</li>
            </ul>
          </section>

          <section>
            <h2>Cookies and similar technologies</h2>
            <p>
              The Application uses a single functional cookie to remember your
              sidebar display preference. Our authentication provider stores your
              session using browser local storage rather than cookies. The
              Application does not use analytics, advertising, or tracking cookies,
              and does not require a cookie-consent banner as a result. If that ever
              changes, this policy and the Application will be updated to obtain
              your consent before any non-essential tracking technology is used.
            </p>
          </section>

          <section>
            <h2>Does the Application use Artificial Intelligence (AI) technologies?</h2>
            <p>
              Yes — generating images is the core function of the Application. When
              you submit a prompt (and optionally a reference image), that content is
              sent to our AI infrastructure provider, fal.ai, which routes the
              request to the underlying model provider you selected: Black Forest
              Labs (Flux), xAI (Grok Image), Google (Nano Banana), or OpenAI (GPT
              Image). These providers process your prompt and any submitted images
              solely to generate the output you requested, subject to their own data
              handling practices. The Service Provider does not use your prompts or
              generated images to train its own models.
            </p>
          </section>

          <section>
            <h2>Third-party service providers</h2>
            <p>The Application relies on the following third-party service providers to operate:</p>
            <ul>
              <li><strong className="text-foreground">Auth0</strong> — authentication and account sessions.</li>
              <li><strong className="text-foreground">Stripe</strong> — payment processing for token purchases. The Service Provider does not store your full payment card details; these are handled directly by Stripe.</li>
              <li><strong className="text-foreground">Google Cloud Storage</strong> — storage of the images you generate.</li>
              <li><strong className="text-foreground">fal.ai and the underlying AI model providers</strong> (Black Forest Labs, xAI, Google, OpenAI) — processing prompts and reference images to generate your requested output, as described above.</li>
            </ul>
            <p>
              Where the GDPR applies, the Service Provider enters into Data
              Processing Agreements (DPAs) with these providers as required by
              Article 28 of the GDPR, imposing the same data protection obligations
              described in this Privacy Policy.
            </p>
          </section>

          <section>
            <h2>What information does the Application collect automatically?</h2>
            <p>
              The Application may automatically collect your IP address, browser
              type, operating system, and information about how you use the
              Application (such as generation activity and error logs).
            </p>
          </section>

          <section>
            <h2>Does the Application collect precise real-time location information?</h2>
            <p>No. The Application does not gather precise location information from your device.</p>
          </section>

          <section>
            <h2>International data transfers</h2>
            <p>
              The Service Provider or its third-party service providers may transfer
              personal data outside the European Economic Area (EEA). Where this
              occurs, an appropriate transfer mechanism is used, such as:
            </p>
            <ul>
              <li>Adequacy decisions by the European Commission</li>
              <li>Standard Contractual Clauses (SCCs) approved by the European Commission</li>
              <li>Other safeguards or derogations recognized under GDPR Chapter V, including consent where legally permitted</li>
            </ul>
          </section>

          <section>
            <h2>What are my opt-out rights?</h2>
            <p>
              You can stop further collection of your information by ceasing to use
              the Application. This does not automatically delete information
              already transmitted to the Service Provider or its third-party
              providers. To request deletion of your data, withdraw consent, or
              exercise any other right, contact the Service Provider at{" "}
              <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.
            </p>
          </section>

          <section>
            <h2>Data retention</h2>
            <ul>
              <li>Account and generation data: retained for the duration of your use of the Application plus 12 months thereafter, unless a longer period is required by law.</li>
              <li>Automatically collected data (logs, etc.): retained for up to 24 months.</li>
              <li>Billing records: retained as long as required by applicable tax and accounting law.</li>
            </ul>
            <p>
              You may request deletion of your data at any time, except where
              retention is required by law, by contacting{" "}
              <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.
            </p>
          </section>

          <section>
            <h2>Children's privacy</h2>
            <p>
              The Application is not intended for individuals under 16 years of age,
              or the higher age of digital consent established under applicable
              law. The Service Provider does not knowingly collect personal data
              from children. If you believe a child has provided personal data to
              the Service Provider, please contact us so it can be removed.
            </p>
          </section>

          <section>
            <h2>How is your information kept secure?</h2>
            <p>
              The Service Provider implements reasonable technical and
              organizational safeguards to protect your information. However, no
              method of transmission or storage is completely secure, and no
              guarantee of absolute security can be made.
            </p>
          </section>

          <section>
            <h2>Data breach notification</h2>
            <p>
              In the event of a personal data breach posing a risk to your rights
              and freedoms, the Service Provider will notify the relevant
              supervisory authority within 72 hours of becoming aware of it, and
              will notify affected individuals without undue delay where the breach
              is likely to result in a high risk to their rights and freedoms.
            </p>
          </section>

          <section>
            <h2>Changes to this policy</h2>
            <p>
              This Privacy Policy may be updated from time to time. Material
              changes will be posted with an updated effective date, and where
              required by law, consent will be sought before they take effect.
            </p>
          </section>

          <section>
            <h2>Your GDPR data protection rights</h2>
            <ul>
              <li><strong className="text-foreground">Right of access</strong> — request access to your personal data.</li>
              <li><strong className="text-foreground">Right to rectification</strong> — request correction of inaccurate data.</li>
              <li><strong className="text-foreground">Right to erasure</strong> — request deletion of your personal data.</li>
              <li><strong className="text-foreground">Right to restrict processing</strong> — request that the Data Controller limits how your data is used.</li>
              <li><strong className="text-foreground">Right to data portability</strong> — request a copy of your data in a structured, machine-readable format.</li>
              <li><strong className="text-foreground">Right to object</strong> — object to processing based on legitimate interests, including direct marketing at any time.</li>
              <li><strong className="text-foreground">Right to withdraw consent</strong> — withdraw consent at any time where processing is based on it.</li>
            </ul>
            <p>
              If you believe your data protection rights have been violated, you may
              lodge a complaint with your local Data Protection Authority. Contact
              details for EU authorities are available at{" "}
              <a href="https://edpb.ec.europa.eu/about-edpb/members_en" target="_blank" rel="noopener noreferrer">
                edpb.ec.europa.eu
              </a>. UK residents may contact the Information Commissioner's Office
              at <a href="https://ico.org.uk" target="_blank" rel="noopener noreferrer">ico.org.uk</a>.
            </p>
          </section>

          <section>
            <h2>Your California privacy rights (CCPA/CPRA)</h2>
            <p>California residents have the right to know, delete, and correct personal information collected about them, to opt out of the sale or sharing of personal information, and to non-discrimination for exercising these rights. To exercise any of these rights, contact <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>.</p>
          </section>

          <section>
            <h2>How can you contact the Data Controller?</h2>
            <p>
              If you have any questions about this Privacy Policy or how your data
              is handled, contact the Service Provider at{" "}
              <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>. The Service
              Provider will respond within one month of receiving your request,
              extendable by up to two months where necessary due to complexity or
              volume, as permitted by applicable law.
            </p>
          </section>
        </div>
      </main>
    </div>
  )
}
