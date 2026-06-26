import { useCallback } from "react"
import { useParams, useNavigate, Navigate } from "react-router"
import { useAuth0 } from "@auth0/auth0-react"
import { useQuery } from "@tanstack/react-query"
import {
  EmbeddedCheckoutProvider,
  EmbeddedCheckout,
} from "@stripe/react-stripe-js"
import { Layout } from "@/components/Layout"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { ChevronLeft } from "lucide-react"
import { stripePromise } from "@/lib/stripe"
import { CreateCheckoutSession, GetPackages, type PebblePackage } from "@/api/queries"

export default function CheckoutPage() {
  const { packageId } = useParams<{ packageId: string }>()
  const navigate = useNavigate()
  const { getAccessTokenSilently } = useAuth0()

  const { data: packages, isLoading } = useQuery<PebblePackage[]>({
    queryKey: ["packages"],
    queryFn: () => GetPackages(getAccessTokenSilently),
  })
  const pkg = packages?.find((p) => p.nameId === packageId)

  const fetchClientSecret = useCallback(
    () => CreateCheckoutSession(packageId!, getAccessTokenSilently),
    [packageId, getAccessTokenSilently],
  )

  // Wait for the catalog before deciding whether the package is valid.
  if (isLoading) {
    return (
      <Layout>
        <div className="w-full max-w-lg mx-auto pt-8">
          <Skeleton className="h-64 rounded-xl" />
        </div>
      </Layout>
    )
  }

  if (!pkg) return <Navigate to="/tokens/buy" replace />

  return (
    <Layout>
      <div className="w-full max-w-lg mx-auto pt-8">
        <Button
          variant="ghost"
          size="sm"
          className="mb-4 -ml-2 text-muted-foreground"
          onClick={() => navigate("/tokens/buy")}
        >
          <ChevronLeft size={16} />
          Back
        </Button>

        <div className="mb-6">
          <h1 className="text-lg font-semibold">Checkout</h1>
          <p className="text-sm text-muted-foreground mt-1">
            {pkg.pebbleAmount.toLocaleString()} Pebbles — ${pkg.dollarPrice.toFixed(2)}
          </p>
        </div>

        <div className="border border-border rounded-xl overflow-hidden">
          <EmbeddedCheckoutProvider
            stripe={stripePromise}
            options={{ fetchClientSecret }}
          >
            <EmbeddedCheckout />
          </EmbeddedCheckoutProvider>
        </div>
      </div>
    </Layout>
  )
}
