import { ImageIcon } from "lucide-react"
import { AspectRatio } from "@/components/ui/aspect-ratio"
import { Skeleton } from "@/components/ui/skeleton"

type ImageState = "idle" | "loading" | "result"

export function ImageArea({ state, imageUrl, progress }: { state: ImageState; imageUrl?: string; progress?: number }) {
  return (
    <div className="w-full rounded-xl overflow-hidden border border-border">
      <AspectRatio ratio={1}>
        {state === "idle" && (
          <div className="flex h-full flex-col items-center justify-center gap-3 text-muted-foreground">
            <ImageIcon className="size-10 opacity-30" />
            <span className="text-sm">Your image will appear here</span>
          </div>
        )}
        {state === "loading" && (
          <div className="relative h-full w-full">
            <Skeleton className="h-full w-full rounded-none" />
            <div className="absolute bottom-0 left-0 right-0 h-1 bg-muted">
              <div
                className="h-full bg-primary transition-[width] duration-300 ease-out"
                style={{ width: `${progress ?? 0}%` }}
              />
            </div>
          </div>
        )}
        {state === "result" && (
          <img
            src={imageUrl}
            alt="Generated"
            className="h-full w-full object-cover"
          />
        )}
      </AspectRatio>
    </div>
  )
}
