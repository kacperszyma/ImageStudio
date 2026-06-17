import { Textarea } from "@/components/ui/textarea"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

type ImageModel = { name: string }

type Props = {
  models: ImageModel[] | undefined
  isLoading: boolean
  model: string
  onModelChange: (value: string) => void
  onGenerate: () => void
}

export function GenerateControls({ models, isLoading, model, onModelChange, onGenerate }: Props) {
  return (
    <>
      <Textarea placeholder="Describe the image you want to generate…" className="resize-none" rows={3} />
      <div className="flex gap-2">
        {isLoading ? (
          <Skeleton className="h-9 w-48 rounded-md" />
        ) : (
          <Select value={model} onValueChange={onModelChange}>
            <SelectTrigger className="w-48">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {models?.map((m) => (
                <SelectItem key={m.name} value={m.name}>{m.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
        <Button className="flex-1" onClick={onGenerate}>Generate</Button>
      </div>
    </>
  )
}
