import { useEffect } from "react"
import { useNavigate, useSearchParams } from "react-router"
import { useQueryClient } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { RedeemSession } from "@/api/queries"
import { Layout } from "@/components/Layout"
import { Button } from "@/components/ui/button"
import { CheckCircle2 } from "lucide-react"

export default function CompletePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { getAccessTokenSilently } = useAuth0()
  const [params] = useSearchParams()
  const sessionId = params.get("session_id")

  // Credit pebbles immediately by fetching the session from Stripe directly —
  // this heals the case where the webhook hasn't arrived yet (or was missed).
  // The server uses the session id as an idempotency key, so a later webhook
  // delivery is a safe no-op.
  useEffect(() => {
    if (!sessionId) return
    RedeemSession(sessionId, getAccessTokenSilently).then(() => {
      queryClient.invalidateQueries({ queryKey: ["balance"] })
      queryClient.invalidateQueries({ queryKey: ["purchases"] })
    })
  }, [sessionId, getAccessTokenSilently, queryClient])

  return (
    <Layout>
      <div className="w-full max-w-lg mx-auto pt-16 flex flex-col items-center text-center">
        <CheckCircle2 size={40} className="text-green-500 mb-4" />
        <h1 className="text-lg font-semibold">Payment complete</h1>
        <p className="text-sm text-muted-foreground mt-1 mb-6">
          Your Pebbles are on the way — they’ll appear in your balance shortly.
        </p>
        <div className="flex gap-3">
          <Button variant="outline" onClick={() => navigate("/tokens/buy")}>
            Buy more
          </Button>
          <Button onClick={() => navigate("/")}>Start generating</Button>
        </div>
      </div>
    </Layout>
  )
}
