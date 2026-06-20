import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { Link } from "react-router"
import { Line, LineChart, XAxis, YAxis, CartesianGrid } from "recharts"
import { GetBalance, GetTransactions, GetHistory, type TransactionDto, type GenerationDetails } from "@/api/queries"
import { Layout } from "@/components/Layout"
import { Skeleton } from "@/components/ui/skeleton"
import { Button } from "@/components/ui/button"
import { Stone, ArrowUpRight, Zap } from "lucide-react"
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart"

function txEffect(tx: TransactionDto): number {
  return tx.type === "TopUp" || tx.type === "Unfreeze" ? tx.amount : -tx.amount
}

function txLabel(type: TransactionDto["type"]) {
  switch (type) {
    case "TopUp": return "Top-up"
    case "Freeze": return "Reserved"
    case "Charge": return "Charged"
    case "Unfreeze": return "Refunded"
  }
}

function txAmountClass(type: TransactionDto["type"]) {
  return type === "TopUp" || type === "Unfreeze" ? "text-green-500" : "text-destructive"
}

function txPrefix(type: TransactionDto["type"]) {
  return type === "TopUp" || type === "Unfreeze" ? "+" : ""
}

function buildChartData(transactions: TransactionDto[]) {
  const sorted = [...transactions].sort(
    (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
  )
  let running = 0
  return sorted.map((tx) => {
    running += txEffect(tx)
    return {
      date: new Date(tx.createdAt).toLocaleDateString(undefined, { month: "short", day: "numeric" }),
      balance: running,
    }
  })
}

const chartConfig = {
  balance: { label: "Balance", color: "var(--color-primary)" },
} satisfies ChartConfig

export default function TokenPage() {
  const { getAccessTokenSilently } = useAuth0()

  const { data: balance, isLoading: balanceLoading } = useQuery<number>({
    queryKey: ["balance"],
    queryFn: () => GetBalance(getAccessTokenSilently),
  })

  const { data: transactions, isLoading: txLoading } = useQuery<TransactionDto[]>({
    queryKey: ["transactions"],
    queryFn: () => GetTransactions(getAccessTokenSilently),
  })

  const { data: history, isLoading: historyLoading } = useQuery<GenerationDetails[]>({
    queryKey: ["history"],
    queryFn: () => GetHistory(getAccessTokenSilently),
  })

  const chartData = transactions ? buildChartData(transactions) : []
  const recentImages = history?.slice(-4).reverse() ?? []

  return (
    <Layout>
      <div className="w-full flex flex-col gap-4 h-full">

        {/* Top row */}
        <div className="grid grid-cols-3 gap-4">
          {/* Chart */}
          <div className="col-span-2 border border-border rounded-xl p-4 flex flex-col gap-3">
            <span className="text-sm font-medium">Balance over time</span>
            {txLoading ? (
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

          {/* Balance */}
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
        <div className="grid grid-cols-2 gap-4">
          {/* Transaction list */}
          <div className="border border-border rounded-xl overflow-hidden">
            <div className="px-4 py-3 border-b border-border">
              <span className="text-sm font-medium">Transactions</span>
            </div>

            {txLoading && (
              <div className="divide-y divide-border">
                {Array.from({ length: 4 }).map((_, i) => (
                  <div key={i} className="px-4 py-3 flex justify-between items-center">
                    <Skeleton className="h-4 w-24" />
                    <Skeleton className="h-4 w-12" />
                  </div>
                ))}
              </div>
            )}

            {!txLoading && (!transactions || transactions.length === 0) && (
              <div className="px-4 py-8 text-center text-sm text-muted-foreground">
                No transactions yet.
              </div>
            )}

            {transactions && transactions.length > 0 && (
              <div className="divide-y divide-border">
                {[...transactions]
                  .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
                  .map((tx) => (
                    <div key={tx.id} className="px-4 py-3 flex items-center justify-between text-sm">
                      <div className="flex flex-col gap-0.5">
                        <span>{txLabel(tx.type)}</span>
                        <span className="text-xs text-muted-foreground">
                          {new Date(tx.createdAt).toLocaleDateString(undefined, {
                            month: "short",
                            day: "numeric",
                            year: "numeric",
                          })}
                        </span>
                      </div>
                      <span className={`font-medium tabular-nums ${txAmountClass(tx.type)}`}>
                        {txPrefix(tx.type)}{tx.amount}
                      </span>
                    </div>
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
              <div className="grid grid-cols-4 gap-2 p-3">
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
              <div className="grid grid-cols-4 gap-2 p-3">
                {recentImages.map((img, i) => (
                  <div key={i} className="aspect-square rounded-lg overflow-hidden bg-muted">
                    <img
                      src={img.imageUrl}
                      alt={img.prompt}
                      className="w-full h-full object-cover"
                      title={img.prompt}
                    />
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

      </div>
    </Layout>
  )
}
