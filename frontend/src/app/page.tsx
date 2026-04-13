'use client'

import { useRef, useState } from 'react'
import { shortenUrl } from '@/lib/api'
import { ApiError } from '@/types'
import Spinner from '@/components/Spinner'

export default function HomePage() {
  const [inputUrl, setInputUrl] = useState('')
  const [shortUrl, setShortUrl] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [copied, setCopied] = useState(false)
  const copyTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setError(null)
    setShortUrl(null)
    setLoading(true)

    try {
      const result = await shortenUrl(inputUrl)
      setShortUrl(result)
    } catch (err) {
      setError(
        err instanceof ApiError
          ? err.message
          : 'Something went wrong. Please try again.',
      )
    } finally {
      setLoading(false)
    }
  }

  async function handleCopy() {
    if (!shortUrl) return
    try {
      await navigator.clipboard.writeText(shortUrl)
      setCopied(true)
      if (copyTimerRef.current) clearTimeout(copyTimerRef.current)
      copyTimerRef.current = setTimeout(() => setCopied(false), 2000)
    } catch {
      // Clipboard API unavailable (e.g. non-HTTPS) — fall back silently
    }
  }

  return (
    <div className="flex min-h-[calc(100vh-65px)] items-center justify-center px-4 py-12">
      <div className="w-full max-w-lg">
        {/* Hero */}
        <h1 className="mb-2 text-center text-4xl font-extrabold tracking-tight text-gray-900">
          Shorten any URL
        </h1>
        <p className="mb-10 text-center text-base text-gray-500">
          Paste a long link and get a short one instantly — no account needed.
        </p>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-3" noValidate>
          <div>
            <label htmlFor="url-input" className="sr-only">
              Long URL
            </label>
            <input
              id="url-input"
              type="url"
              value={inputUrl}
              onChange={(e) => setInputUrl(e.target.value)}
              placeholder="https://example.com/your/very/long/path?with=params"
              required
              className="w-full rounded-xl border border-gray-300 px-4 py-3 text-sm shadow-sm transition
                focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-200
                disabled:bg-gray-100"
              disabled={loading}
              aria-describedby={error ? 'url-error' : undefined}
            />

            {/* Inline error — shown instead of alert() */}
            {error && (
              <p
                id="url-error"
                className="mt-2 flex items-start gap-1.5 text-sm text-red-600"
                role="alert"
              >
                <span aria-hidden>⚠</span>
                {error}
              </p>
            )}
          </div>

          <button
            type="submit"
            disabled={loading || !inputUrl}
            className="flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 px-4 py-3
              text-sm font-semibold text-white shadow-sm transition
              hover:bg-indigo-700 active:scale-[.98]
              disabled:cursor-not-allowed disabled:opacity-60"
          >
            {loading && <Spinner />}
            {loading ? 'Shortening…' : 'Shorten URL'}
          </button>
        </form>

        {/* Success result */}
        {shortUrl && (
          <div
            className="mt-8 rounded-xl border border-green-200 bg-green-50 p-5 shadow-sm"
            role="region"
            aria-label="Your short link"
          >
            <p className="mb-3 text-xs font-semibold uppercase tracking-widest text-green-600">
              Your short link
            </p>
            <div className="flex items-center gap-3">
              <a
                href={shortUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="min-w-0 flex-1 truncate text-sm font-medium text-indigo-600
                  underline decoration-dotted underline-offset-2 hover:text-indigo-800"
              >
                {shortUrl}
              </a>
              <button
                onClick={handleCopy}
                className="shrink-0 rounded-lg border border-green-300 bg-white px-3 py-1.5
                  text-xs font-semibold text-green-700 shadow-sm transition
                  hover:bg-green-100 active:scale-95"
                aria-label={copied ? 'Link copied!' : 'Copy short link'}
              >
                {copied ? '✓ Copied!' : 'Copy'}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
