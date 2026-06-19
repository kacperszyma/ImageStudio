import { Sidebar, SidebarContent, SidebarFooter, SidebarGroup, SidebarGroupLabel, SidebarHeader } from "@/components/ui/sidebar"
import { NavUser } from "@/components/nav-user"
import { Stone } from "lucide-react"
import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { GetBalance } from "@/api/queries"

export function AppSidebar() {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0()
  const { data: balance } = useQuery<number>({
    queryKey: ["balance"],
    queryFn: () => GetBalance(getAccessTokenSilently),
    enabled: isAuthenticated,
  })

  return (
    <Sidebar>
      <SidebarHeader className="p-4 flex flex-row items-center justify-between">
        <span className="font-semibold text-sm">ImageStudio</span>
        {balance !== undefined && (
          <span className="text-xs text-muted-foreground flex items-center gap-1"><Stone size={12} />{balance}</span>
        )}
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Projects</SidebarGroupLabel>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <NavUser />
      </SidebarFooter>
    </Sidebar>
  )
}
