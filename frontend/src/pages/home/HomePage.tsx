import { Sidebar, SidebarContent, SidebarFooter, SidebarGroup, SidebarGroupLabel, SidebarHeader, SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar"
import { Textarea } from "@/components/ui/textarea"
import { NavUser } from "@/components/nav-user"

const mockUser = {
  name: "Bob",
  email: "bob@gmail.com",
  avatar: "",
}

function AppSidebar() {
  return (
    <Sidebar>
      <SidebarHeader className="p-4 font-semibold text-sm">ImageStudio</SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Projects</SidebarGroupLabel>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <NavUser user={mockUser} />
      </SidebarFooter>
    </Sidebar>
  )
}

export default function HomePage() {
  return (
    <SidebarProvider>
      <AppSidebar />
      <main className="flex flex-1 flex-col gap-4 p-4">
        <SidebarTrigger />
        <Textarea placeholder="Type something…" className="flex-1 resize-none" />
      </main>
    </SidebarProvider>
  )
}
