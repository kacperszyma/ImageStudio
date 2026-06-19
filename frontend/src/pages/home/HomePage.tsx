"use client"

import { useState, useEffect } from "react"
import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar"
import { Generate, GetModels, GetBalance } from "@/api/queries"
import { useAuth0 } from '@auth0/auth0-react'
import { useQuery } from '@tanstack/react-query'
import { AppSidebar } from "./subcomponents/AppSidebar"
import { ImageArea } from "./subcomponents/ImageArea"
import { LoginPrompt } from "./subcomponents/LoginPrompt"
import { GenerateControls } from "./subcomponents/GenerateControls"

type ImageModel = { name: string }
type ImageState = "idle" | "loading" | "result"

export default function HomePage() {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0()
  const [imageState, setImageState] = useState<ImageState>("idle")
  const [model, setModel] = useState<string>("")

  const { isLoading, data: models } = useQuery<ImageModel[]>({
    queryKey: ["models"],
    queryFn: () => GetModels(getAccessTokenSilently),
    enabled: isAuthenticated
  })

  useEffect(() => {
    if (models && models.length > 0) setModel(models[0].name)
  }, [models])

  async function handleGenerate() {
    console.log(await GetBalance(getAccessTokenSilently))
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
          {!isAuthenticated ? (
            <LoginPrompt />
          ) : (
            <GenerateControls
              models={models}
              isLoading={isLoading}
              model={model}
              onModelChange={setModel}
              onGenerate={handleGenerate}
            />
          )}
        </div>
      </main>
    </SidebarProvider>
  )
}
