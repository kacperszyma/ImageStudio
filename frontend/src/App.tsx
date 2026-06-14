import { Routes, Route } from "react-router"
import { TooltipProvider } from "@/components/ui/tooltip"
import HomePage from "@/pages/home/HomePage"
import LoginPage from "@/pages/auth/LoginPage"
import SignupPage from "@/pages/auth/SignupPage"

export default function App() {
  return (
    <TooltipProvider>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<SignupPage />} />
      </Routes>
    </TooltipProvider>
  )
}
