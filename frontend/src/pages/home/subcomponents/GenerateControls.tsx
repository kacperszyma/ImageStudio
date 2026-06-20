import { Textarea } from "@/components/ui/textarea"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Stone } from "lucide-react"

type ImageModel = { slug: string; creditCost: number }

type Props = {
  models: ImageModel[] | undefined
  isLoading: boolean
  model: string
  onModelChange: (value: string) => void
  prompt: string
  onPromptChange: (value: string) => void
  onGenerate: () => void
  disabled?: boolean
}

export function GenerateControls({ models, isLoading, model, onModelChange, prompt, onPromptChange, onGenerate, disabled }: Props) {
  const currentCost = models?.find((m) => m.slug === model)?.creditCost

  return (
    <>
      <Textarea placeholder="Describe the image you want to generate…" className="resize-none" rows={3} value={prompt} onChange={e => onPromptChange(e.target.value)} />
      <div className="flex gap-2">
        {isLoading || (!!models?.length && !model) ? (
          <Skeleton className="h-9 w-48 rounded-md" />
        ) : (
          <Select value={model} onValueChange={(v) => v && onModelChange(v)}>
            <SelectTrigger className="w-48">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {models?.map((m, i) => (
                <SelectItem key={`${i}-${m.slug}`} value={m.slug}>
                  <span className="flex items-center justify-between gap-4 w-full">
                    <span>{m.slug}</span>
                    <span className="flex items-center gap-0.5 text-muted-foreground">
                      <Stone size={10} />
                      {m.creditCost}
                    </span>
                  </span>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
        <Button className="flex-1 gap-1.5" onClick={onGenerate} disabled={disabled}>
          Generate
          {currentCost !== undefined && (
            <span className="flex items-center gap-0.5 opacity-70 text-xs">
              · <Stone size={10} /> {currentCost}
            </span>
          )}
        </Button>
      </div>
    </>
  )
}
