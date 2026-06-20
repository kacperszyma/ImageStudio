import { useAuth0 } from "@auth0/auth0-react"
import { Layout } from "@/components/Layout"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"

function getInitials(name?: string) {
  if (!name) return "?"
  return name.split(" ").map((n) => n[0]).slice(0, 2).join("").toUpperCase()
}

export default function ProfilePage() {
  const { user } = useAuth0()

  return (
    <Layout>
      <div className="w-full max-w-md mx-auto pt-8">
        <div className="flex flex-col items-center gap-4 mb-8">
          <Avatar className="size-20">
            <AvatarImage src={user?.picture} alt={user?.name} />
            <AvatarFallback className="text-lg">{getInitials(user?.name)}</AvatarFallback>
          </Avatar>
          <div className="text-center">
            <h1 className="text-xl font-semibold">{user?.name}</h1>
            <p className="text-sm text-muted-foreground mt-1">{user?.email}</p>
          </div>
        </div>

        <div className="border border-border rounded-xl divide-y divide-border">
          <div className="px-4 py-3 flex justify-between text-sm">
            <span className="text-muted-foreground">Email</span>
            <span>{user?.email}</span>
          </div>
          <div className="px-4 py-3 flex justify-between text-sm">
            <span className="text-muted-foreground">Email verified</span>
            <span>{user?.email_verified ? "Yes" : "No"}</span>
          </div>
          <div className="px-4 py-3 flex justify-between text-sm">
            <span className="text-muted-foreground">Name</span>
            <span>{user?.name ?? "—"}</span>
          </div>
        </div>
      </div>
    </Layout>
  )
}
