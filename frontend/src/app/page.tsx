'use client'

import { useEffect, useRef, useState } from 'react'
import { shortenUrl } from '@/lib/api'
import { ApiError } from '@/types'
import Spinner from '@/components/Spinner'
import QrCode from '@/components/QrCode'

const EXPIRATION_LABELS: Record<string, string> = {
  '': 'Never expires',
  '1h': 'Expires in 1 hour',
  '1d': 'Expires in 1 day',
  '7d': 'Expires in 7 days',
  '30d': 'Expires in 30 days',
}

const ALIAS_PATTERN = /^[A-Za-z0-9_-]{3,30}$/

export default function HomePage() {
  const [inputUrl, setInputUrl] = useState('')
  const [expiresIn, setExpiresIn] = useState('')
  const [showAdvanced, setShowAdvanced] = useState(false)
  const [alias, setAlias] = useState('')
  const [previewHost, setPreviewHost] = useState('url-y.net')
  const [shortUrl, setShortUrl] = useState<string | null>(null)
  const [resultExpiresIn, setResultExpiresIn] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [copied, setCopied] = useState(false)
  const copyTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    setPreviewHost(window.location.host)
  }, [])

  const aliasFormatError =
    alias && !ALIAS_PATTERN.test(alias)
      ? 'Alias must be 3-30 characters: letters, numbers, hyphens, underscores.'
      : null

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setError(null)
    setShortUrl(null)

    if (aliasFormatError) {
      setError(aliasFormatError)
      return
    }

    setLoading(true)

    try {
      const result = await shortenUrl(inputUrl, expiresIn, alias || undefined)
      setShortUrl(result)
      setResultExpiresIn(expiresIn)
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
        <h1 className="mb-3 text-center text-4xl font-extrabold tracking-tight text-zinc-50 sm:text-5xl">
          Shorten any URL
        </h1>
        <p className="mb-10 text-center text-base text-zinc-400">
          Paste a long link and get a short one instantly — no account needed.
        </p>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-3" noValidate>
          <div className="glass flex flex-col gap-2 rounded-2xl border border-white/10 p-2 shadow-glow-sm sm:flex-row">
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
              className="w-full flex-1 rounded-xl border border-white/10 bg-zinc-900/60 px-4 py-3 text-sm text-zinc-100 shadow-sm transition
                placeholder:text-zinc-500
                focus:border-white/30 focus:outline-none focus:ring-2 focus:ring-white/10
                disabled:opacity-60"
              disabled={loading}
              aria-describedby={error ? 'url-error' : undefined}
            />

            <button
              type="submit"
              disabled={loading || !inputUrl}
              className="flex shrink-0 items-center justify-center gap-2 rounded-xl bg-white px-5 py-3
                text-sm font-semibold text-zinc-900 shadow-sm transition
                hover:bg-zinc-200 active:scale-[.98]
                disabled:cursor-not-allowed disabled:opacity-50"
            >
              {loading && <Spinner />}
              {loading ? 'Shortening…' : 'Shorten URL'}
            </button>
          </div>

          {/* Expiration selector + advanced options toggle */}
          <div className="flex flex-wrap items-center justify-center gap-x-4 gap-y-2 px-1 text-sm text-zinc-400">
            <div className="flex items-center gap-2">
              <label htmlFor="expires-select">Expires</label>
              <select
                id="expires-select"
                value={expiresIn}
                onChange={(e) => setExpiresIn(e.target.value)}
                disabled={loading}
                className="rounded-lg border border-white/10 bg-zinc-900/60 px-2 py-1 text-sm text-zinc-200 transition
                  focus:border-white/30 focus:outline-none focus:ring-2 focus:ring-white/10
                  disabled:opacity-60"
              >
                <option value="">Never</option>
                <option value="1h">1 hour</option>
                <option value="1d">1 day</option>
                <option value="7d">7 days</option>
                <option value="30d">30 days</option>
              </select>
            </div>

            <button
              type="button"
              onClick={() => setShowAdvanced((v) => !v)}
              className="text-xs font-medium text-zinc-400 underline decoration-dotted underline-offset-2 transition hover:text-zinc-200"
              aria-expanded={showAdvanced}
              aria-controls="advanced-options"
            >
              {showAdvanced ? 'Hide advanced options' : 'Advanced options'}
            </button>
          </div>

          {/* Advanced options — custom alias */}
          {showAdvanced && (
            <div id="advanced-options" className="glass rounded-xl border border-white/10 p-3">
              <label htmlFor="alias-input" className="mb-1.5 block text-xs font-medium text-zinc-400">
                Custom alias (optional)
              </label>
              <input
                id="alias-input"
                type="text"
                value={alias}
                onChange={(e) => setAlias(e.target.value)}
                placeholder="my-link"
                disabled={loading}
                className="w-full rounded-lg border border-white/10 bg-zinc-900/60 px-3 py-2 text-sm text-zinc-100 shadow-sm transition
                  placeholder:text-zinc-500
                  focus:border-white/30 focus:outline-none focus:ring-2 focus:ring-white/10
                  disabled:opacity-60"
                aria-describedby="alias-preview"
              />
              <p id="alias-preview" className="mt-1.5 font-mono text-xs text-zinc-500">
                {previewHost}/{alias || <span className="italic text-zinc-600">random</span>}
              </p>
              {aliasFormatError && (
                <p className="mt-1.5 text-xs text-red-400" role="alert">
                  {aliasFormatError}
                </p>
              )}
            </div>
          )}

          {/* Inline error — shown instead of alert() */}
          {error && (
            <p
              id="url-error"
              className="flex items-start gap-1.5 px-1 text-sm text-red-400"
              role="alert"
            >
              <span aria-hidden>⚠</span>
              {error}
            </p>
          )}
        </form>

        {/* Success result */}
        {shortUrl && (
          <div
            className="mt-8 animate-fade-in-up rounded-2xl border border-white/10 bg-zinc-900/60 p-5 shadow-glow backdrop-blur-sm"
            role="region"
            aria-label="Your short link"
          >
            <p className="mb-3 text-xs font-semibold uppercase tracking-widest text-zinc-400">
              Your short link
            </p>
            <div className="flex items-center gap-3">
              <a
                href={shortUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="min-w-0 flex-1 truncate rounded-lg border border-white/10 bg-zinc-950/60 px-3 py-2
                  font-mono text-sm text-zinc-100 underline decoration-dotted underline-offset-2 transition hover:text-white"
              >
                {shortUrl}
              </a>
              <button
                onClick={handleCopy}
                className="shrink-0 rounded-lg border border-white/10 bg-white/5 px-3 py-2
                  text-xs font-semibold text-zinc-200 shadow-sm transition
                  hover:bg-white/10 active:scale-95"
                aria-label={copied ? 'Link copied!' : 'Copy short link'}
              >
                {copied ? '✓ Copied!' : 'Copy'}
              </button>
            </div>
            <p className="mt-3 text-xs text-zinc-500">{EXPIRATION_LABELS[resultExpiresIn]}</p>

            <div className="mt-5 flex justify-center border-t border-white/10 pt-5">
              <QrCode value={shortUrl} />
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
