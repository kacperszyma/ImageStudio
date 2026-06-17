import { ImageIcon } from "lucide-react"
import { AspectRatio } from "@/components/ui/aspect-ratio"
import { Skeleton } from "@/components/ui/skeleton"

type ImageState = "idle" | "loading" | "result"

export function ImageArea({ state }: { state: ImageState }) {
  return (
    <div className="w-full rounded-xl overflow-hidden border border-border">
      <AspectRatio ratio={1}>
        {state === "idle" && (
          <div className="flex h-full flex-col items-center justify-center gap-3 text-muted-foreground">
            <ImageIcon className="size-10 opacity-30" />
            <span className="text-sm">Your image will appear here</span>
          </div>
        )}
        {state === "loading" && <Skeleton className="h-full w-full rounded-none" />}
        {state === "result" && (
          <img
            src="https://placehold.co/512x512"
            alt="Generated"
            className="h-full w-full object-cover"
          />
        )}
      </AspectRatio>
    </div>
  )
}
