import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { GetHistory } from "@/api/queries"
import { Layout } from "@/components/Layout"
import { Skeleton } from "@/components/ui/skeleton"
import { ImageIcon } from "lucide-react"

type GenerationDetails = {
  modelSlug: string
  prompt: string
  imageUrl: string
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
              <Skeleton key={i} className="aspect-square rounded-xl" />
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
              <div
                key={i}
                className="group relative rounded-xl overflow-hidden border border-border aspect-square bg-muted"
              >
                <img
                  src={item.imageUrl}
                  alt={item.prompt}
                  className="w-full h-full object-cover"
                />
                <div className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity flex flex-col justify-end p-3 gap-2">
                  <p className="text-xs text-white line-clamp-4">{item.prompt}</p>
                  <span className="text-[10px] font-medium text-white/60">{item.modelSlug}</span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </Layout>
  )
}
