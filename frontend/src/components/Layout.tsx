import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar"
import { AppSidebar } from "@/pages/home/subcomponents/AppSidebar"

export function Layout({ children }: { children: React.ReactNode }) {
  return (
    <SidebarProvider>
      <AppSidebar />
      <main className="flex flex-1 flex-col p-4 gap-4 min-h-0 overflow-auto">
        <SidebarTrigger />
        {children}
      </main>
    </SidebarProvider>
  )
}
