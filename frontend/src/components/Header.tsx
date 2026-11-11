import Link from 'next/link'

export default function Header() {
  return (
    <header className="sticky top-0 z-50 border-b border-white/10 bg-zinc-950/70 backdrop-blur-md">
      <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-4">
        <Link
          href="/"
          className="text-xl font-bold tracking-tight text-zinc-50 transition-opacity hover:opacity-80"
        >
          URLy
        </Link>
        <nav>
          <Link
            href="/dashboard"
            className="text-sm font-medium text-zinc-400 transition-colors hover:text-zinc-100"
          >
            Dashboard →
          </Link>
        </nav>
      </div>
    </header>
  )
}
