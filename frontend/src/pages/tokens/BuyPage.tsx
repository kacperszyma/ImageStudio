import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { useNavigate } from "react-router"
import { Layout } from "@/components/Layout"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Stone } from "lucide-react"
import { GetBalance, GetPackages, type PebblePackage } from "@/api/queries"
import { discountLabel, originalPrice } from "./packages"

export default function BuyPage() {
  const navigate = useNavigate()
  const { getAccessTokenSilently } = useAuth0()
  const { data: balance, isLoading } = useQuery<number>({
    queryKey: ["balance"],
    queryFn: () => GetBalance(getAccessTokenSilently),
  })
  const { data: packages, isLoading: packagesLoading } = useQuery<PebblePackage[]>({
    queryKey: ["packages"],
    queryFn: () => GetPackages(getAccessTokenSilently),
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
          {packagesLoading
            ? Array.from({ length: 3 }).map((_, i) => (
                <Skeleton key={i} className="h-[74px] rounded-xl" />
              ))
            : packages?.map((pkg) => {
                const label = discountLabel(pkg)
                const original = originalPrice(pkg)
                return (
                  <div
                    key={pkg.nameId}
                    className="border border-border rounded-xl px-5 py-4 flex items-center justify-between"
                  >
                    <div className="flex flex-col gap-1">
                      <div className="flex items-center gap-2">
                        <Stone size={15} className="text-muted-foreground" />
                        <span className="font-semibold">
                          {pkg.pebbleAmount.toLocaleString()} Pebbles
                        </span>
                        {label && (
                          <span className="text-xs font-medium text-green-500 bg-green-500/10 px-2 py-0.5 rounded-full">
                            {label}
                          </span>
                        )}
                      </div>
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <span>${pkg.dollarPrice.toFixed(2)}</span>
                        {original && (
                          <span className="line-through text-xs">${original.toFixed(2)}</span>
                        )}
                      </div>
                    </div>
                    <Button
                      variant="outline"
                      onClick={() => navigate(`/tokens/checkout/${pkg.nameId}`)}
                    >
                      Buy
                    </Button>
                  </div>
                )
              })}
        </div>
      </div>
    </Layout>
  )
}
