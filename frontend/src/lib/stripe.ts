import { loadStripe, type Stripe } from "@stripe/stripe-js"

const publishableKey = import.meta.env.VITE_STRIPE_PUBLISHABLE_KEY

// Only call loadStripe with a real key — an empty string throws an
// IntegrationError. Resolves to null until the key is configured.
export const stripePromise: Promise<Stripe | null> = publishableKey
    ? loadStripe(publishableKey)
    : Promise.resolve(null)

if (!publishableKey) {
    console.warn("VITE_STRIPE_PUBLISHABLE_KEY is not set — Stripe checkout will not load.")
}
