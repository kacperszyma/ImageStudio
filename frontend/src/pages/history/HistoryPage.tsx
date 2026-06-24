import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { GetHistory, type GenerationDetails } from "@/api/queries"
import { Layout } from "@/components/Layout"
import { Skeleton } from "@/components/ui/skeleton"
import { ImageIcon, Stone } from "lucide-react"

function GenerationCard({ item }: { item: GenerationDetails }) {
  return (
    <div className="rounded-xl border border-border bg-card overflow-hidden flex flex-col">
      <div className="aspect-[4/3] bg-muted overflow-hidden">
        {item.imageUrl ? (
          <img
            src={item.imageUrl}
            alt={item.prompt}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center">
            <ImageIcon size={28} className="text-muted-foreground opacity-30" />
          </div>
        )}
      </div>

      <div className="p-3 flex flex-col gap-2">
        <div className="flex items-center justify-between gap-2">
          <span className="text-[10px] font-medium text-muted-foreground bg-muted rounded-full px-2 py-0.5 uppercase tracking-wide">
            Text to image
          </span>
          <div className="flex items-center gap-1 text-[11px] font-semibold text-amber-600 bg-amber-50 border border-amber-100 rounded-full px-2 py-0.5">
            <Stone size={10} />
            {item.creditCost}
          </div>
        </div>

        <p className="text-xs text-foreground line-clamp-2 leading-relaxed">{item.prompt}</p>

        <span className="text-[10px] text-muted-foreground">{item.modelSlug}</span>
      </div>
    </div>
  )
}

function CardSkeleton() {
  return (
    <div className="rounded-xl border border-border bg-card overflow-hidden flex flex-col">
      <Skeleton className="aspect-[4/3] rounded-none" />
      <div className="p-3 flex flex-col gap-2">
        <div className="flex items-center justify-between">
          <Skeleton className="h-4 w-20 rounded-full" />
          <Skeleton className="h-4 w-12 rounded-full" />
        </div>
        <Skeleton className="h-3 w-full" />
        <Skeleton className="h-3 w-3/4" />
        <Skeleton className="h-3 w-16" />
      </div>
    </div>
  )
}

export default function HistoryPage() {
  const { getAccessTokenSilently } = useAuth0()
  const { data: history, isLoading } = useQuery<GenerationDetails[]>({
    queryKey: ["history"],
    queryFn: () => GetHistory(getAccessTokenSilently),
  })

  return (
    <Layout>
      <div className="w-full max-w-4xl mx-auto">
        <h1 className="text-lg font-semibold mb-6">History</h1>

        {isLoading && (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <CardSkeleton key={i} />
            ))}
          </div>
        )}

        {!isLoading && (!history || history.length === 0) && (
          <div className="flex flex-col items-center justify-center py-24 text-muted-foreground gap-3">
            <ImageIcon size={40} className="opacity-30" />
            <span className="text-sm">No generations yet. Go create something.</span>
          </div>
        )}

        {history && history.length > 0 && (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4">
            {history.map((item, i) => (
              <GenerationCard key={i} item={item} />
            ))}
          </div>
        )}
      </div>
    </Layout>
  )
}
