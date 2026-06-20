"use client"

import { useState, useEffect, useRef } from "react"
import { HubConnection } from "@microsoft/signalr"
import { GetModels, GetBalance } from "@/api/queries"
import { buildGenerationConnection } from "@/api/generationHub"
import { useAuth0 } from '@auth0/auth0-react'
import { useQuery } from '@tanstack/react-query'
import { Layout } from "@/components/Layout"
import { ImageArea } from "./subcomponents/ImageArea"
import { LoginPrompt } from "./subcomponents/LoginPrompt"
import { GenerateControls } from "./subcomponents/GenerateControls"

type ImageModel = { slug: string; creditCost: number }
type ImageState = "idle" | "loading" | "result"

export default function HomePage() {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0()
  const [imageState, setImageState] = useState<ImageState>("idle")
  const [imageUrl, setImageUrl] = useState<string>()
  const [model, setModel] = useState<string>("")
  const [prompt, setPrompt] = useState<string>("")
  const [hubConnected, setHubConnected] = useState(false)
  const [progress, setProgress] = useState(0)
  const gateRef = useRef(0)
  const hubRef = useRef<HubConnection | null>(null)

  const { isLoading, data: models } = useQuery<ImageModel[]>({
    queryKey: ["models"],
    queryFn: () => GetModels(getAccessTokenSilently),
    enabled: isAuthenticated
  })

  useEffect(() => {
    if (models && models.length > 0) setModel(models[0].slug)
  }, [models])

  useEffect(() => {
    if (!isAuthenticated) return

    const connection = buildGenerationConnection(getAccessTokenSilently)

    connection.on("GenerationProgress", (percent: number) => {
      gateRef.current = percent
    })

    connection.on("GenerationComplete", (_jobId: string, url: string) => {
      setImageUrl(url)
      gateRef.current = 100
    })

    connection.on("GenerationFailed", (message: string) => {
      console.error("Generation failed:", message)
      setImageState("idle")
    })

    connection.onclose(() => setHubConnected(false))
    connection.onreconnecting(() => setHubConnected(false))
    connection.onreconnected(() => setHubConnected(true))

    connection.start()
      .then(() => setHubConnected(true))
      .catch(console.error)

    hubRef.current = connection

    return () => { connection.stop() }
  }, [isAuthenticated])

  // Animate progress up to the current gate
  useEffect(() => {
    if (imageState !== "loading") return

    const id = setInterval(() => {
      setProgress(p => {
        const next = p + 1.5
        return next < gateRef.current ? next : gateRef.current
      })
    }, 50)

    return () => clearInterval(id)
  }, [imageState])

  // When bar reaches 100, reveal the image
  useEffect(() => {
    if (progress >= 100 && imageState === "loading") {
      setImageState("result")
    }
  }, [progress, imageState])

  async function handleGenerate() {
    if (!hubRef.current || !hubConnected || !prompt.trim()) return
    console.log("Prompt:", prompt)
    console.log(await GetBalance(getAccessTokenSilently))
    setProgress(0)
    gateRef.current = 30
    setImageState("loading")
    await hubRef.current.invoke("Generate", model, prompt)
  }

  return (
    <Layout>
      <div className="flex flex-col gap-4 w-full max-w-2xl mx-auto">
        <ImageArea state={imageState} imageUrl={imageUrl} progress={progress} />
        {!isAuthenticated ? (
          <LoginPrompt />
        ) : (
          <GenerateControls
            models={models}
            isLoading={isLoading}
            model={model}
            onModelChange={setModel}
            prompt={prompt}
            onPromptChange={setPrompt}
            onGenerate={handleGenerate}
            disabled={!hubConnected}
          />
        )}
      </div>
    </Layout>
  )
}
