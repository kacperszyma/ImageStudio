import { useEffect } from "react"
import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { useParams, Link, useNavigate } from "react-router"
import { GetSpendDetail, type TransactionDetailDto, type TransactionResult } from "@/api/queries"
import { Layout } from "@/components/Layout"
import { Skeleton } from "@/components/ui/skeleton"
import { Stone, ArrowLeft } from "lucide-react"

const typeLabel: Record<TransactionDetailDto["type"], string> = {
  TopUp: "Top-up",
  Freeze: "Reserved",
  Charge: "Charged",
  Unfreeze: "Refunded",
}

function amountStyle(type: TransactionDetailDto["type"]) {
  return type === "TopUp" || type === "Unfreeze" ? "text-green-500" : "text-destructive"
}

function amountPrefix(type: TransactionDetailDto["type"]) {
  return type === "TopUp" || type === "Unfreeze" ? "+" : "−"
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, {
    month: "short", day: "numeric", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  })
}

function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex justify-between items-center text-sm py-2.5 border-b border-border last:border-0">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{children}</span>
    </div>
  )
}

export default function TransactionDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { getAccessTokenSilently } = useAuth0()
  const navigate = useNavigate()

  const { data, isLoading, isError } = useQuery<TransactionResult>({
    queryKey: ["transaction", id],
    queryFn: () => GetSpendDetail(id!, getAccessTokenSilently),
    enabled: !!id,
  })

  useEffect(() => {
    if (data?.kind === 'generation') {
      navigate(`/generations/${data.generationId}`, { replace: true })
    }
  }, [data, navigate])

  return (
    <Layout>
      <div className="w-full max-w-sm mx-auto flex flex-col gap-6">
        <Link
          to="/tokens"
          className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors w-fit"
        >
          <ArrowLeft size={14} />
          Back
        </Link>

        {(isLoading || data?.kind === 'generation') && (
          <div className="border border-border rounded-xl px-4 py-1">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="flex justify-between items-center py-2.5 border-b border-border last:border-0">
                <Skeleton className="h-4 w-20" />
                <Skeleton className="h-4 w-24" />
              </div>
            ))}
          </div>
        )}

        {isError && (
          <div className="text-center py-24 text-sm text-muted-foreground">
            Transaction not found.
          </div>
        )}

        {data?.kind === 'transaction' && (
          <div className="border border-border rounded-xl px-4 py-1">
            <Row label="Type">{typeLabel[data.data.type]}</Row>
            <Row label="Amount">
              <span className={`flex items-center gap-1 ${amountStyle(data.data.type)}`}>
                {amountPrefix(data.data.type)}{data.data.amount} <Stone size={12} />
              </span>
            </Row>
            <Row label="Date">{formatDate(data.data.createdAt)}</Row>
            <Row label="ID">
              <span className="font-mono text-xs text-muted-foreground">{data.data.id.slice(0, 8)}…</span>
            </Row>
          </div>
        )}
      </div>
    </Layout>
  )
}
