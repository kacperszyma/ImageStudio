"use client"

import { useState, useEffect, useRef } from "react"
import { HubConnection } from "@microsoft/signalr"
import { GetModels } from "@/api/queries"
import { buildGenerationConnection, registerGenerationHandlers, startGeneration } from "@/api/generationHub"
import { estimatedGenerationMs } from "./modelEstimates"
import { useAuth0 } from '@auth0/auth0-react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Layout } from "@/components/Layout"
import { ImageArea } from "./subcomponents/ImageArea"
import { LoginPrompt } from "./subcomponents/LoginPrompt"
import { GenerateControls } from "./subcomponents/GenerateControls"

type ImageModel = { slug: string; creditCost: number }
type ImageState = "idle" | "loading" | "result"

export default function HomePage() {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0()
  const queryClient = useQueryClient()
  const [imageState, setImageState] = useState<ImageState>("idle")
  const [imageUrl, setImageUrl] = useState<string>()
  const [model, setModel] = useState<string>("")
  const [prompt, setPrompt] = useState<string>("")
  const [hubConnected, setHubConnected] = useState(false)
  const [progress, setProgress] = useState(0)
  const [statusText, setStatusText] = useState("")
  const [error, setError] = useState<string>()
  // `acceptedAtRef` anchors the asymptotic climb below; null while we're still
  // waiting on the hub to ack the enqueue.
  const acceptedAtRef = useRef<number | null>(null)
  const expectedMsRef = useRef(15000)
  const doneRef = useRef(false)
  const hubRef = useRef<HubConnection | null>(null)

  const { isLoading, data: models } = useQuery<ImageModel[]>({
    queryKey: ["models"],
    queryFn: () => GetModels(getAccessTokenSilently),
    enabled: isAuthenticated
  })

  // Default to the first model once the list loads; a user pick (via
  // onModelChange) leaves `model` non-empty so this never overrides it.
  if (models && models.length > 0 && !model) {
    setModel(models[0].slug)
  }

  useEffect(() => {
    if (!isAuthenticated) return

    const connection = buildGenerationConnection(getAccessTokenSilently)

    registerGenerationHandlers(connection, {
      onAccepted: () => {
        // Enqueued; start the asymptotic climb while we wait for the webhook.
        acceptedAtRef.current = Date.now()
      },
      onComplete: (_jobId, url) => {
        // Preload so the bar holds below 100 until the bytes are cached; revealing
        // a not-yet-loaded <img> would briefly paint its alt text.
        const img = new Image()
        const reveal = () => {
          setImageUrl(url)
          doneRef.current = true
        }
        img.onload = reveal
        img.onerror = reveal
        img.src = url
        queryClient.invalidateQueries({ queryKey: ["balance"] })
      },
      onFailed: (reason) => {
        setError(
          reason === "InsufficientFunds"
            ? "Not enough credits for this generation."
            : "Generation failed. Please try again."
        )
        setImageState("idle")
        setProgress(0)
        acceptedAtRef.current = null
        doneRef.current = false
      },
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

  // Animate progress toward a moving target; reveal the image once it reaches 100.
  // Before the hub ack the target sits at 30. After the ack it never stalls: it
  // creeps from 70 toward 95 on a decaying curve paced by the model's typical
  // latency, so the bar keeps moving instead of freezing for the whole wait.
  useEffect(() => {
    if (imageState !== "loading") return

    const id = setInterval(() => {
      const target = computeTarget()
      setProgress(p => {
        const next = p + 1.5 < target ? p + 1.5 : target
        if (next >= 100) setImageState("result")
        return next
      })
      setStatusText(prev => {
        const next = computeStatusText()
        return next === prev ? prev : next
      })
    }, 50)

    return () => clearInterval(id)

    function computeTarget(): number {
      if (doneRef.current) return 100
      if (acceptedAtRef.current === null) return 30
      const elapsed = Date.now() - acceptedAtRef.current
      const tau = expectedMsRef.current / 2
      const asymptote = 95
      return 70 + (asymptote - 70) * (1 - Math.exp(-elapsed / tau))
    }

    function computeStatusText(): string {
      if (acceptedAtRef.current === null) return "Sending prompt…"
      const elapsed = Date.now() - acceptedAtRef.current
      return elapsed < expectedMsRef.current ? "Generating…" : "Almost there…"
    }
  }, [imageState])

  async function handleGenerate() {
    if (!hubRef.current || !hubConnected || !prompt.trim()) return
    setError(undefined)
    setProgress(0)
    setStatusText("Sending prompt…")
    acceptedAtRef.current = null
    doneRef.current = false
    expectedMsRef.current = estimatedGenerationMs(model)
    setImageState("loading")
    await startGeneration(hubRef.current, model, prompt)
  }

  return (
    <Layout>
      <div className="flex flex-col gap-4 w-full max-w-2xl mx-auto">
        <ImageArea state={imageState} imageUrl={imageUrl} progress={progress} statusText={statusText} />
        {!isAuthenticated ? (
          <LoginPrompt />
        ) : (
          <>
            {error && (
              <p className="text-sm text-destructive text-center">{error}</p>
            )}
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
          </>
        )}
      </div>
    </Layout>
  )
}
