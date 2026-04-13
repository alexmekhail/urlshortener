import Link from 'next/link'

export default function Header() {
  return (
    <header className="border-b border-gray-200 bg-white">
      <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-4">
        <Link
          href="/"
          className="text-xl font-bold text-indigo-600 hover:text-indigo-700 transition-colors"
        >
          URLy
        </Link>
        <nav>
          <Link
            href="/dashboard"
            className="text-sm font-medium text-gray-600 hover:text-gray-900 transition-colors"
          >
            Dashboard →
          </Link>
        </nav>
      </div>
    </header>
  )
}
