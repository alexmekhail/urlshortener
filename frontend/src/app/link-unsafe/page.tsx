import Link from 'next/link'

export default function LinkUnsafePage() {
  return (
    <div className="flex min-h-[calc(100vh-65px)] items-center justify-center px-4 py-12">
      <div className="w-full max-w-md text-center">
        <p className="mb-2 text-sm font-semibold uppercase tracking-widest text-red-400">
          Unsafe link
        </p>
        <h1 className="mb-3 text-3xl font-extrabold tracking-tight text-zinc-50 sm:text-4xl">
          This link was flagged as unsafe
        </h1>
        <p className="mb-8 text-zinc-400">
          The destination was flagged by Google Safe Browsing and this short link has
          been deactivated to protect you.
        </p>
        <Link
          href="/"
          className="inline-flex items-center justify-center rounded-xl bg-white px-5 py-3
            text-sm font-semibold text-zinc-900 shadow-sm transition hover:bg-zinc-200 active:scale-[.98]"
        >
          Shorten a new link
        </Link>
      </div>
    </div>
  )
}
