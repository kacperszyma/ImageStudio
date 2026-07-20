import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar"
import { AppSidebar } from "@/pages/home/subcomponents/AppSidebar"
import { Footer } from "@/components/Footer"

export function Layout({ children }: { children: React.ReactNode }) {
  return (
    <SidebarProvider>
      <AppSidebar />
      <div className="flex flex-1 flex-col min-h-0 overflow-auto">
        <main className="flex flex-1 flex-col p-4 gap-4 min-h-0">
          <SidebarTrigger />
          {children}
        </main>
        <Footer />
      </div>
    </SidebarProvider>
  )
}
