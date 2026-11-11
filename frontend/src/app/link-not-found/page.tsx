import Link from 'next/link'

export default function LinkNotFoundPage() {
  return (
    <div className="flex min-h-[calc(100vh-65px)] items-center justify-center px-4 py-12">
      <div className="w-full max-w-md text-center">
        <p className="mb-2 text-sm font-semibold uppercase tracking-widest text-violet-400">
          Link not found
        </p>
        <h1 className="mb-3 text-3xl font-extrabold tracking-tight text-gradient-accent sm:text-4xl">
          This short link doesn&apos;t exist
        </h1>
        <p className="mb-8 text-zinc-400">
          It may have been deactivated, or the link was mistyped.
        </p>
        <Link
          href="/"
          className="inline-flex items-center justify-center rounded-xl bg-gradient-accent px-5 py-3
            text-sm font-semibold text-white shadow-sm transition hover:opacity-90 active:scale-[.98]"
        >
          Shorten a new link
        </Link>
      </div>
    </div>
  )
}
