import { Routes, Route } from "react-router"
import { TooltipProvider } from "@/components/ui/tooltip"
import { useAuth0 } from "@auth0/auth0-react"
import HomePage from "@/pages/home/HomePage"
import LandingPage from "@/pages/landing/LandingPage"
import HistoryPage from "@/pages/history/HistoryPage"
import ProfilePage from "@/pages/profile/ProfilePage"
import TokenPage from "@/pages/tokens/TokenPage"
import BuyPage from "@/pages/tokens/BuyPage"
import CheckoutPage from "@/pages/tokens/CheckoutPage"
import CompletePage from "@/pages/tokens/CompletePage"
import BillingPage from "@/pages/billing/BillingPage"
import GenerationDetailPage from "@/pages/generations/GenerationDetailPage"
import TransactionDetailPage from "@/pages/transactions/TransactionDetailPage"
import { Auth0Provider } from "@auth0/auth0-react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const queryClient = new QueryClient();

// "/" is the landing page for signed-out visitors and the studio for
// signed-in users — avoid a flash of one for the other while Auth0 resolves.
function RootRoute() {
  const { isAuthenticated, isLoading } = useAuth0()
  if (isLoading) return null
  return isAuthenticated ? <HomePage /> : <LandingPage />
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <TooltipProvider>
        <Auth0Provider
          domain="dev-yw7pijmj3lf7zgrf.us.auth0.com"
          clientId="4O1P4ReZboEKgAjIve6z8WsMaO5NnMeq"
          authorizationParams={{ redirect_uri: window.location.origin, audience: "https://imagestudio-api" }}
          cacheLocation="localstorage"
          useRefreshTokens={true}
        >
          <Routes>
            <Route path="/" element={<RootRoute />} />
            <Route path="/history" element={<HistoryPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/tokens" element={<TokenPage />} />
            <Route path="/tokens/buy" element={<BuyPage />} />
            <Route path="/tokens/checkout/:packageId" element={<CheckoutPage />} />
            <Route path="/tokens/complete" element={<CompletePage />} />
            <Route path="/billing" element={<BillingPage />} />
            <Route path="/generations/:id" element={<GenerationDetailPage />} />
            <Route path="/transactions/:id" element={<TransactionDetailPage />} />
          </Routes>
        </Auth0Provider>
      </TooltipProvider>
    </QueryClientProvider>
  )
}
