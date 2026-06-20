import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { Layout } from "@/components/Layout"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Stone } from "lucide-react"
import { GetBalance } from "@/api/queries"

const tiers = [
  { pebbles: 200, price: 1.0, label: null, originalPrice: null },
  { pebbles: 1000, price: 4.5, label: "10% off", originalPrice: 5.0 },
  { pebbles: 2000, price: 8.0, label: "20% off", originalPrice: 10.0 },
]

export default function BuyPage() {
  const { getAccessTokenSilently } = useAuth0()
  const { data: balance, isLoading } = useQuery<number>({
    queryKey: ["balance"],
    queryFn: () => GetBalance(getAccessTokenSilently),
  })

  return (
    <Layout>
      <div className="w-full max-w-lg mx-auto pt-8">
        <div className="mb-6">
          <h1 className="text-lg font-semibold">Buy Pebbles</h1>
          <p className="text-sm text-muted-foreground mt-1">
            200 Pebbles per $1.00 — use them to generate images.
          </p>
        </div>

        <div className="border border-border rounded-xl px-5 py-4 flex items-center justify-between mb-6">
          <span className="text-sm text-muted-foreground">Current balance</span>
          {isLoading ? (
            <Skeleton className="h-5 w-16" />
          ) : (
            <div className="flex items-center gap-1.5 font-semibold">
              <Stone size={14} className="text-muted-foreground" />
              {balance ?? 0} Pebbles
            </div>
          )}
        </div>

        <div className="flex flex-col gap-3">
          {tiers.map((tier) => (
            <div
              key={tier.pebbles}
              className="border border-border rounded-xl px-5 py-4 flex items-center justify-between"
            >
              <div className="flex flex-col gap-1">
                <div className="flex items-center gap-2">
                  <Stone size={15} className="text-muted-foreground" />
                  <span className="font-semibold">{tier.pebbles.toLocaleString()} Pebbles</span>
                  {tier.label && (
                    <span className="text-xs font-medium text-green-500 bg-green-500/10 px-2 py-0.5 rounded-full">
                      {tier.label}
                    </span>
                  )}
                </div>
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <span>${tier.price.toFixed(2)}</span>
                  {tier.originalPrice && (
                    <span className="line-through text-xs">${tier.originalPrice.toFixed(2)}</span>
                  )}
                </div>
              </div>
              <Button variant="outline" disabled>
                Coming soon
              </Button>
            </div>
          ))}
        </div>
      </div>
    </Layout>
  )
}
