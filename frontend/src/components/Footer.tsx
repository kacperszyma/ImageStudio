import { Link } from "react-router"

export function Footer({
  onLoginClick,
  showModelsLink = false,
}: {
  onLoginClick?: () => void
  showModelsLink?: boolean
}) {
  return (
    <footer className="border-t border-border">
      <div className="mx-auto flex w-full max-w-6xl flex-col items-center justify-between gap-4 px-6 py-8 text-sm text-muted-foreground sm:flex-row">
        <div className="flex items-center gap-2">
          <img src="/favicon.svg" alt="" className="h-5 w-5" />
          <span className="font-heading font-medium text-foreground">
            ImageStudio
          </span>
        </div>
        <nav className="flex items-center gap-6">
          {showModelsLink && (
            <a href="#models" className="transition-colors hover:text-foreground">
              Models
            </a>
          )}
          <Link to="/privacy" className="transition-colors hover:text-foreground">
            Privacy Policy
          </Link>
          <Link to="/terms" className="transition-colors hover:text-foreground">
            Terms
          </Link>
          {onLoginClick && (
            <button
              onClick={onLoginClick}
              className="transition-colors hover:text-foreground"
            >
              Log in
            </button>
          )}
        </nav>
        <span>© 2026 ImageStudio</span>
      </div>
    </footer>
  )
}
