import { Link, useLocation } from "react-router"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar"
import { Tooltip, TooltipTrigger, TooltipContent } from "@/components/ui/tooltip"
import { NavUser } from "@/components/nav-user"
import { Stone, Wand2, Clock, Plus } from "lucide-react"
import { useQuery } from "@tanstack/react-query"
import { useAuth0 } from "@auth0/auth0-react"
import { GetBalance } from "@/api/queries"

const navItems = [
  { label: "Generate", href: "/", icon: <Wand2 size={16} /> },
  { label: "History", href: "/history", icon: <Clock size={16} /> },
  { label: "Tokens", href: "/tokens", icon: <Stone size={16} /> },
]

export function AppSidebar() {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0()
  const { pathname } = useLocation()
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
          <Tooltip>
            <TooltipTrigger
              render={<Link to="/tokens/buy" className="flex items-center gap-1.5 font-semibold text-sm hover:text-primary transition-colors" />}
            >
              <Stone size={14} />
              {balance}
            </TooltipTrigger>
            <TooltipContent side="bottom">
              <Plus size={10} className="inline mr-0.5" />
              Buy Pebbles
            </TooltipContent>
          </Tooltip>
        )}
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Navigation</SidebarGroupLabel>
          <SidebarMenu>
            {navItems.map((item) => (
              <SidebarMenuItem key={item.href}>
                <SidebarMenuButton
                  render={<Link to={item.href} />}
                  isActive={pathname === item.href}
                >
                  {item.icon}
                  <span>{item.label}</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
            ))}
          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <NavUser />
      </SidebarFooter>
    </Sidebar>
  )
}
