import { Routes, Route } from "react-router"
import { TooltipProvider } from "@/components/ui/tooltip"
import HomePage from "@/pages/home/HomePage"
import HistoryPage from "@/pages/history/HistoryPage"
import ProfilePage from "@/pages/profile/ProfilePage"
import TokenPage from "@/pages/tokens/TokenPage"
import BuyPage from "@/pages/tokens/BuyPage"
import GenerationDetailPage from "@/pages/generations/GenerationDetailPage"
import TransactionDetailPage from "@/pages/transactions/TransactionDetailPage"
import { Auth0Provider } from "@auth0/auth0-react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const queryClient = new QueryClient();

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
            <Route path="/" element={<HomePage />} />
            <Route path="/history" element={<HistoryPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/tokens" element={<TokenPage />} />
            <Route path="/tokens/buy" element={<BuyPage />} />
            <Route path="/generations/:id" element={<GenerationDetailPage />} />
            <Route path="/transactions/:id" element={<TransactionDetailPage />} />
          </Routes>
        </Auth0Provider>
      </TooltipProvider>
    </QueryClientProvider>
  )
}
