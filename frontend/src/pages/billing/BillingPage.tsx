import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { Link } from "react-router"
import { GetPurchases, type PurchaseDto } from "@/api/queries"
import { Layout } from "@/components/Layout"
import { Skeleton } from "@/components/ui/skeleton"
import { Button } from "@/components/ui/button"
import { Stone, CreditCard, Receipt, Zap } from "lucide-react"

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    month: "short", day: "numeric", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  })
}

function Summary({ label, value, icon }: { label: string; value: React.ReactNode; icon: React.ReactNode }) {
  return (
    <div className="border border-border rounded-xl px-5 py-4 flex flex-col gap-1">
      <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
        {icon}
        {label}
      </div>
      <span className="text-xl font-semibold tabular-nums">{value}</span>
    </div>
  )
}

export default function BillingPage() {
  const { getAccessTokenSilently } = useAuth0()
  const { data: purchases, isLoading } = useQuery<PurchaseDto[]>({
    queryKey: ["purchases"],
    queryFn: () => GetPurchases(getAccessTokenSilently),
  })

  const sorted = purchases
    ? [...purchases].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    : []

  const totalSpent = sorted.reduce((sum, p) => sum + p.dollarAmount, 0)
  const totalPebbles = sorted.reduce((sum, p) => sum + p.pebbleAmount, 0)

  return (
    <Layout>
      <div className="w-full max-w-2xl mx-auto pt-2">
        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-lg font-semibold">Billing</h1>
            <p className="text-sm text-muted-foreground mt-1">
              Your Pebble purchase history.
            </p>
          </div>
          <Button size="sm" className="gap-1.5" render={<Link to="/tokens/buy" />}>
            <Zap size={14} />
            Buy Pebbles
          </Button>
        </div>

        <div className="grid grid-cols-3 gap-3 mb-6">
          {isLoading ? (
            Array.from({ length: 3 }).map((_, i) => <Skeleton key={i} className="h-[78px] rounded-xl" />)
          ) : (
            <>
              <Summary
                label="Total spent"
                value={`$${totalSpent.toFixed(2)}`}
                icon={<CreditCard size={12} />}
              />
              <Summary
                label="Pebbles bought"
                value={
                  <span className="flex items-center gap-1">
                    <Stone size={16} className="text-muted-foreground" />
                    {totalPebbles.toLocaleString()}
                  </span>
                }
                icon={<Stone size={12} />}
              />
              <Summary
                label="Purchases"
                value={sorted.length}
                icon={<Receipt size={12} />}
              />
            </>
          )}
        </div>

        <div className="border border-border rounded-xl overflow-hidden">
          <div className="px-4 py-3 border-b border-border flex items-center gap-2">
            <Receipt size={14} className="text-muted-foreground" />
            <span className="text-sm font-medium">Transactions</span>
          </div>

          {isLoading && (
            <div className="divide-y divide-border">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="px-4 py-4 flex justify-between items-center">
                  <div className="flex flex-col gap-1.5">
                    <Skeleton className="h-4 w-28" />
                    <Skeleton className="h-3 w-40" />
                  </div>
                  <Skeleton className="h-4 w-16" />
                </div>
              ))}
            </div>
          )}

          {!isLoading && sorted.length === 0 && (
            <div className="px-4 py-16 text-center text-sm text-muted-foreground">
              No purchases yet.
            </div>
          )}

          {sorted.length > 0 && (
            <div className="divide-y divide-border">
              {sorted.map((p) => (
                <div key={p.id} className="px-4 py-4 flex items-center justify-between gap-4">
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="size-9 shrink-0 rounded-full bg-muted flex items-center justify-center">
                      <Stone size={16} className="text-muted-foreground" />
                    </div>
                    <div className="flex flex-col gap-0.5 min-w-0">
                      <span className="text-sm font-medium flex items-center gap-1">
                        +{p.pebbleAmount.toLocaleString()} Pebbles
                      </span>
                      <span className="text-xs text-muted-foreground truncate">
                        {formatDateTime(p.createdAt)}
                      </span>
                      <span className="text-[10px] font-mono text-muted-foreground/60">
                        {p.packageNameId} · {p.id.slice(0, 8)}…
                      </span>
                    </div>
                  </div>
                  <div className="text-right shrink-0">
                    <div className="text-sm font-semibold tabular-nums">${p.dollarAmount.toFixed(2)}</div>
                    <div className="text-[11px] text-green-500">Paid</div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </Layout>
  )
}
