import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { Link } from "react-router"
import { Line, LineChart, XAxis, YAxis, CartesianGrid } from "recharts"
import { GetBalance, GetSpendingHistory, GetPurchases, GetHistory, type TransactionDto, type PurchaseDto, type GenerationDetails } from "@/api/queries"
import { Layout } from "@/components/Layout"
import { Skeleton } from "@/components/ui/skeleton"
import { Button } from "@/components/ui/button"
import { Stone, ArrowUpRight, Zap, CreditCard } from "lucide-react"
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart"

// Signed pebble delta for a spend entry: a refund (Unfreeze) adds back, every
// other charge subtracts.
function spendDelta(tx: TransactionDto): number {
  return tx.type === "Unfreeze" ? tx.amount : -tx.amount
}

function spendAmountClass(type: TransactionDto["type"]) {
  return type === "Unfreeze" ? "text-green-500" : "text-destructive"
}

// A single dated change to the balance, whatever its source.
type BalanceEvent = { createdAt: string; delta: number }

function buildChartData(events: BalanceEvent[], currentBalance: number) {
  const sorted = [...events].sort(
    (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
  )
  const points: { date: string; balance: number }[] = []
  let running = currentBalance
  for (let i = sorted.length - 1; i >= 0; i--) {
    const e = sorted[i]
    points.unshift({
      date: new Date(e.createdAt).toLocaleDateString(undefined, { month: "short", day: "numeric" }),
      balance: running,
    })
    running -= e.delta
  }
  return points
}

const chartConfig = {
  balance: { label: "Balance", color: "var(--color-primary)" },
} satisfies ChartConfig

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { month: "short", day: "numeric", year: "numeric" })
}

export default function TokenPage() {
  const { getAccessTokenSilently } = useAuth0()

  const { data: balance, isLoading: balanceLoading } = useQuery<number>({
    queryKey: ["balance"],
    queryFn: () => GetBalance(getAccessTokenSilently),
  })

  const { data: spend, isLoading: spendLoading } = useQuery<TransactionDto[]>({
    queryKey: ["spend"],
    queryFn: () => GetSpendingHistory(getAccessTokenSilently),
  })

  const { data: purchaseData, isLoading: purchasesLoading } = useQuery<PurchaseDto[]>({
    queryKey: ["purchases"],
    queryFn: () => GetPurchases(getAccessTokenSilently),
  })

  const { data: history, isLoading: historyLoading } = useQuery<GenerationDetails[]>({
    queryKey: ["history"],
    queryFn: () => GetHistory(getAccessTokenSilently),
  })

  const recentImages = history?.slice(-4).reverse() ?? []

  // The chart reconstructs balance over time from both money top-ups and pebble spends.
  const chartLoading = spendLoading || purchasesLoading
  const events: BalanceEvent[] = [
    ...(purchaseData ?? []).map(p => ({ createdAt: p.createdAt, delta: p.pebbleAmount })),
    ...(spend ?? []).map(s => ({ createdAt: s.createdAt, delta: spendDelta(s) })),
  ]
  const chartData = chartLoading ? [] : buildChartData(events, balance ?? 0)

  const purchases = purchaseData
    ? [...purchaseData].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    : []

  const spends = spend
    ? [...spend].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    : []

  return (
    <Layout>
      <div className="w-full flex flex-col gap-4 h-full">

        {/* Top row */}
        <div className="grid grid-cols-3 gap-4">
          <div className="col-span-2 border border-border rounded-xl p-4 flex flex-col gap-3">
            <span className="text-sm font-medium">Balance over time</span>
            {chartLoading ? (
              <Skeleton className="h-52 w-full" />
            ) : chartData.length === 0 ? (
              <div className="h-52 flex items-center justify-center text-sm text-muted-foreground">
                No data yet.
              </div>
            ) : (
              <ChartContainer config={chartConfig} className="h-52 w-full">
                <LineChart data={chartData} margin={{ top: 4, right: 8, bottom: 0, left: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--color-border)" />
                  <XAxis
                    dataKey="date"
                    tick={{ fontSize: 11, fill: "var(--color-muted-foreground)" }}
                    axisLine={false}
                    tickLine={false}
                  />
                  <YAxis
                    tick={{ fontSize: 11, fill: "var(--color-muted-foreground)" }}
                    axisLine={false}
                    tickLine={false}
                    width={36}
                  />
                  <ChartTooltip content={<ChartTooltipContent />} />
                  <Line
                    type="monotone"
                    dataKey="balance"
                    stroke="var(--color-primary)"
                    strokeWidth={2}
                    dot={{ r: 3, fill: "var(--color-primary)" }}
                    activeDot={{ r: 5 }}
                  />
                </LineChart>
              </ChartContainer>
            )}
          </div>

          <div className="border border-border rounded-xl p-6 flex flex-col items-center justify-center gap-3">
            {balanceLoading ? (
              <Skeleton className="h-14 w-28" />
            ) : (
              <div className="flex items-center gap-2">
                <Stone size={26} className="text-muted-foreground" />
                <span className="text-5xl font-bold tabular-nums">{balance ?? 0}</span>
              </div>
            )}
            <span className="text-sm text-muted-foreground">Pebbles remaining</span>
            <Button size="lg" className="mt-3 w-full gap-2" render={<Link to="/tokens/buy" />}>
              <Zap size={16} />
              Buy Pebbles
            </Button>
          </div>
        </div>

        {/* Bottom row */}
        <div className="grid grid-cols-3 gap-4">

          {/* Purchase history */}
          <div className="border border-border rounded-xl overflow-hidden">
            <div className="px-4 py-3 border-b border-border flex items-center justify-between">
              <div className="flex items-center gap-2">
                <CreditCard size={14} className="text-muted-foreground" />
                <span className="text-sm font-medium">Purchases</span>
              </div>
              <Link
                to="/billing"
                className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1 transition-colors"
              >
                View all <ArrowUpRight size={12} />
              </Link>
            </div>

            {purchasesLoading && (
              <div className="divide-y divide-border">
                {Array.from({ length: 3 }).map((_, i) => (
                  <div key={i} className="px-4 py-3 flex justify-between items-center">
                    <Skeleton className="h-4 w-20" />
                    <Skeleton className="h-4 w-14" />
                  </div>
                ))}
              </div>
            )}

            {!purchasesLoading && purchases.length === 0 && (
              <div className="px-4 py-8 text-center text-sm text-muted-foreground">
                No purchases yet.
              </div>
            )}

            {purchases.length > 0 && (
              <div className="divide-y divide-border">
                {purchases.map((p) => (
                  <div key={p.id} className="px-4 py-3 flex items-center justify-between text-sm">
                    <div className="flex flex-col gap-0.5">
                      <span>${p.dollarAmount.toFixed(2)}</span>
                      <span className="text-xs text-muted-foreground">{formatDate(p.createdAt)}</span>
                    </div>
                    <span className="font-medium tabular-nums text-green-500 flex items-center gap-1">
                      +{p.pebbleAmount.toLocaleString()}
                      <Stone size={11} />
                    </span>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Spend history */}
          <div className="border border-border rounded-xl overflow-hidden">
            <div className="px-4 py-3 border-b border-border flex items-center gap-2">
              <Stone size={14} className="text-muted-foreground" />
              <span className="text-sm font-medium">Spend history</span>
            </div>

            {spendLoading && (
              <div className="divide-y divide-border">
                {Array.from({ length: 3 }).map((_, i) => (
                  <div key={i} className="px-4 py-3 flex justify-between items-center">
                    <Skeleton className="h-4 w-20" />
                    <Skeleton className="h-4 w-14" />
                  </div>
                ))}
              </div>
            )}

            {!spendLoading && spends.length === 0 && (
              <div className="px-4 py-8 text-center text-sm text-muted-foreground">
                Nothing spent yet.
              </div>
            )}

            {spends.length > 0 && (
              <div className="divide-y divide-border">
                {spends.map((tx) => (
                  <Link
                    key={tx.id}
                    to={`/transactions/${tx.id}`}
                    className="px-4 py-3 flex items-center justify-between text-sm transition-colors hover:bg-muted/60 hover:shadow-[inset_0_0_0_1px_hsl(var(--border))]"
                  >
                    <span className="text-muted-foreground">{formatDate(tx.createdAt)}</span>
                    <span className={`font-medium tabular-nums flex items-center gap-1 ${spendAmountClass(tx.type)}`}>
                      {tx.type === "Unfreeze" ? "+" : ""}{tx.amount}
                      <Stone size={11} />
                    </span>
                  </Link>
                ))}
              </div>
            )}
          </div>

          {/* Recent generations */}
          <div className="border border-border rounded-xl overflow-hidden">
            <div className="px-4 py-3 border-b border-border flex items-center justify-between">
              <span className="text-sm font-medium">Recent generations</span>
              <Link
                to="/history"
                className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1 transition-colors"
              >
                View all <ArrowUpRight size={12} />
              </Link>
            </div>

            {historyLoading && (
              <div className="grid grid-cols-2 gap-2 p-3">
                {Array.from({ length: 4 }).map((_, i) => (
                  <Skeleton key={i} className="aspect-square rounded-lg" />
                ))}
              </div>
            )}

            {!historyLoading && recentImages.length === 0 && (
              <div className="px-4 py-8 text-center text-sm text-muted-foreground">
                No generations yet.
              </div>
            )}

            {recentImages.length > 0 && (
              <div className="grid grid-cols-2 gap-2 p-3">
                {recentImages.map((img) => (
                  <Link
                    key={img.jobId}
                    to={`/generations/${img.jobId}`}
                    className="aspect-square rounded-lg overflow-hidden bg-muted block relative group"
                  >
                    {img.imageUrl && (
                      <img
                        src={img.imageUrl}
                        alt={img.prompt}
                        className="w-full h-full object-cover"
                        title={img.prompt}
                      />
                    )}
                    <div className="absolute inset-0 bg-white/20 opacity-0 group-hover:opacity-100 transition-opacity" />
                  </Link>
                ))}
              </div>
            )}
          </div>

        </div>

      </div>
    </Layout>
  )
}
