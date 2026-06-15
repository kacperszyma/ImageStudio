import { Routes, Route } from "react-router"
import { TooltipProvider } from "@/components/ui/tooltip"
import HomePage from "@/pages/home/HomePage"
import { Auth0Provider } from "@auth0/auth0-react";

export default function App() {
  return (
    <TooltipProvider>
      <Auth0Provider
      domain="dev-yw7pijmj3lf7zgrf.us.auth0.com"
      clientId="4O1P4ReZboEKgAjIve6z8WsMaO5NnMeq"
      authorizationParams={{ redirect_uri: window.location.origin }}
      >
      <Routes>
        <Route path="/" element={<HomePage />} />
      </Routes>
      </Auth0Provider>
    </TooltipProvider>
  )
}
