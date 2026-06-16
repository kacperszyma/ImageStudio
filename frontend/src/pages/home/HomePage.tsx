"use client"

import { useState } from "react"
import { ImageIcon } from "lucide-react"
import { Sidebar, SidebarContent, SidebarFooter, SidebarGroup, SidebarGroupLabel, SidebarHeader, SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar"
import { Textarea } from "@/components/ui/textarea"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { AspectRatio } from "@/components/ui/aspect-ratio"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { NavUser } from "@/components/nav-user"
import { Generate } from "@/api/queries"

const MODELS = [
  { value: "dall-e-3", label: "DALL·E 3" },
  { value: "dall-e-2", label: "DALL·E 2" },
  { value: "stable-diffusion-xl", label: "Stable Diffusion XL" },
  { value: "imagen-3", label: "Imagen 3" },
]

type ImageState = "idle" | "loading" | "result"

function ImageArea({ state }: { state: ImageState }) {
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

function AppSidebar() {
  return (
    <Sidebar>
      <SidebarHeader className="p-4 font-semibold text-sm">ImageStudio</SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Projects</SidebarGroupLabel>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <NavUser />
      </SidebarFooter>
    </Sidebar>
  )
}

export default function HomePage() {
  const [imageState, setImageState] = useState<ImageState>("idle")
  const [model, setModel] = useState(MODELS[0].value)

  async function handleGenerate() {
    console.log(await Generate("lol", "Dalai Lama"))
    setImageState("loading")
    setTimeout(() => setImageState("result"), 2000)
  }

  return (
    <SidebarProvider>
      <AppSidebar />
      <main className="flex flex-1 flex-col p-4 gap-4">
        <SidebarTrigger />
        <div className="flex flex-col gap-4 w-full max-w-2xl mx-auto">
          <ImageArea state={imageState} />
          <Textarea placeholder="Describe the image you want to generate…" className="resize-none" rows={3} />
          <div className="flex gap-2">
            <Select value={model} onValueChange={setModel}>
              <SelectTrigger className="w-48">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {MODELS.map((m) => (
                  <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button className="flex-1" onClick={handleGenerate}>Generate</Button>
          </div>
        </div>
      </main>
    </SidebarProvider>
  )
}
