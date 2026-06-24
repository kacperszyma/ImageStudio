import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { useParams, Link } from "react-router"
import { GetGenerationDetail, type GenerationDetailDto } from "@/api/queries"
import { Layout } from "@/components/Layout"
import { Skeleton } from "@/components/ui/skeleton"
import { Stone, ArrowLeft, ImageIcon } from "lucide-react"

function statusBadge(status: string) {
  const base = "text-xs font-medium px-2 py-0.5 rounded-full"
  switch (status) {
    case "Completed": return <span className={`${base} bg-green-50 text-green-700 border border-green-200`}>Completed</span>
    case "Failed":    return <span className={`${base} bg-red-50 text-red-700 border border-red-200`}>Failed</span>
    default:          return <span className={`${base} bg-amber-50 text-amber-700 border border-amber-200`}>{status}</span>
  }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, {
    month: "short", day: "numeric", year: "numeric",
    hour: "2-digit", minute: "2-digit",
  })
}

function formatDuration(duration: string | null) {
  if (!duration) return "—"
  // "00:00:12.3456789" → "12.3s"
  const match = duration.match(/(\d+):(\d+):(\d+(?:\.\d+)?)/)
  if (!match) return duration
  const h = parseInt(match[1]), m = parseInt(match[2]), s = parseFloat(match[3])
  if (h > 0) return `${h}h ${m}m`
  if (m > 0) return `${m}m ${Math.round(s)}s`
  return `${s.toFixed(1)}s`
}

function MetaRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex justify-between items-center text-sm py-2 border-b border-border last:border-0">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium text-right">{children}</span>
    </div>
  )
}

function DetailView({ data }: { data: GenerationDetailDto }) {
  return (
    <div className="grid grid-cols-[3fr_2fr] gap-6 items-stretch">
      {/* Image */}
      <div className="rounded-xl overflow-hidden bg-muted">
        {data.imageUrl ? (
          <img src={data.imageUrl} alt={data.prompt} className="w-full h-full object-cover" />
        ) : (
          <div className="w-full h-full flex items-center justify-center">
            <ImageIcon size={36} className="text-muted-foreground opacity-30" />
          </div>
        )}
      </div>

      {/* Metadata */}
      <div className="flex flex-col gap-5">
        <div className="flex items-center gap-2">
          {statusBadge(data.status)}
          <span className="text-xs text-muted-foreground font-mono">{data.modelSlug}</span>
          <div className="ml-auto flex items-center gap-1 text-xs font-semibold text-amber-600 bg-amber-50 border border-amber-100 rounded-full px-2 py-0.5">
            <Stone size={10} />
            {data.creditCost}
          </div>
        </div>

        <div className="border border-border rounded-xl overflow-hidden">
          <div className="px-4 py-2 border-b border-border">
            <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Prompt</span>
          </div>
          <p className="text-sm leading-relaxed text-foreground px-4 py-3">{data.prompt}</p>
        </div>

        <div className="border border-border rounded-xl overflow-hidden">
          <div className="px-4 py-2 border-b border-border">
            <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Timing</span>
          </div>
          <div className="px-4 py-1">
            <MetaRow label="Created">{formatDate(data.createdAt)}</MetaRow>
            <MetaRow label="Completed">{data.completedAt ? formatDate(data.completedAt) : "—"}</MetaRow>
            <MetaRow label="Duration">{formatDuration(data.duration)}</MetaRow>
          </div>
        </div>

        <div className="border border-border rounded-xl overflow-hidden">
          <div className="px-4 py-2 border-b border-border">
            <Link to="/tokens" className="text-xs font-medium text-muted-foreground uppercase tracking-wide hover:text-foreground transition-colors">
              Balance ↗
            </Link>
          </div>
          <div className="px-4 py-1">
            <MetaRow label="Before">
              <span className="flex items-center gap-1">{data.balanceBefore} <Stone size={11} /></span>
            </MetaRow>
            <MetaRow label="After">
              <span className="flex items-center gap-1">{data.balanceAfter} <Stone size={11} /></span>
            </MetaRow>
            <MetaRow label="Spent">
              <span className="flex items-center gap-1 text-destructive">−{data.creditCost} <Stone size={11} /></span>
            </MetaRow>
          </div>
        </div>
      </div>
    </div>
  )
}

export default function GenerationDetailPage() {
  const { id } = useParams<{ id: string }>()
  const { getAccessTokenSilently } = useAuth0()

  const { data, isLoading, isError } = useQuery<GenerationDetailDto>({
    queryKey: ["generation", id],
    queryFn: () => GetGenerationDetail(id!, getAccessTokenSilently),
    enabled: !!id,
  })

  return (
    <Layout>
      <div className="w-full max-w-3xl mx-auto flex flex-col gap-6">
        <Link
          to="/history"
          className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors w-fit"
        >
          <ArrowLeft size={14} />
          Back to history
        </Link>

        {isLoading && (
          <div className="grid grid-cols-[3fr_2fr] gap-6">
            <Skeleton className="aspect-square rounded-xl" />
            <div className="flex flex-col gap-4">
              <Skeleton className="h-5 w-32" />
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-28 w-full rounded-xl" />
              <Skeleton className="h-28 w-full rounded-xl" />
            </div>
          </div>
        )}

        {isError && (
          <div className="flex items-center justify-center py-24 text-sm text-muted-foreground">
            Generation not found.
          </div>
        )}

        {data && <DetailView data={data} />}
      </div>
    </Layout>
  )
}
