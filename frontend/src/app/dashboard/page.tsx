'use client'

import { useCallback, useEffect, useState } from 'react'
import { getUrls, deactivateUrl } from '@/lib/api'
import { ApiError } from '@/types'
import type { UrlRecord } from '@/types'
import Spinner from '@/components/Spinner'

const LS_KEY = 'urlshortener_apikey'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function truncateUrl(url: string, max = 60): string {
  return url.length > max ? `${url.slice(0, max)}…` : url
}

// ── Sub-components ────────────────────────────────────────────────────────────

function StatusBadge({ active }: { active: boolean }) {
  return (
    <span
      className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${
        active
          ? 'bg-emerald-400/10 text-emerald-400 ring-1 ring-inset ring-emerald-400/20'
          : 'bg-white/5 text-zinc-500 ring-1 ring-inset ring-white/10'
      }`}
    >
      {active ? 'Active' : 'Inactive'}
    </span>
  )
}

interface DeactivateButtonProps {
  urlId: string
  busy: boolean
  error: string | undefined
  onDeactivate: (urlId: string) => void
}

function DeactivateButton({ urlId, busy, error, onDeactivate }: DeactivateButtonProps) {
  return (
    <div className="flex flex-col items-start gap-1">
      <button
        onClick={() => onDeactivate(urlId)}
        disabled={busy}
        className="flex items-center gap-1.5 rounded-md border border-red-400/30 px-3 py-1
          text-xs font-medium text-red-400 transition
          hover:bg-red-400/10 active:scale-95 disabled:cursor-not-allowed disabled:opacity-50"
        aria-label={`Deactivate ${urlId}`}
      >
        {busy && <Spinner className="h-3 w-3" />}
        {busy ? 'Deactivating…' : 'Deactivate'}
      </button>
      {error && (
        <span className="text-xs text-red-400" role="alert">
          {error}
        </span>
      )}
    </div>
  )
}

// ── Skeleton loader ───────────────────────────────────────────────────────────

function TableSkeleton() {
  return (
    <div className="space-y-2 py-4" aria-label="Loading URLs…">
      {[...Array(4)].map((_, i) => (
        <div key={i} className="h-10 animate-pulse rounded-md bg-white/5" />
      ))}
    </div>
  )
}

// ── Main page ─────────────────────────────────────────────────────────────────

export default function DashboardPage() {
  const [apiKey, setApiKey] = useState('')
  const [urls, setUrls] = useState<UrlRecord[]>([])
  // Don't show loading spinner until we actually have a key to fetch with.
  const [loading, setLoading] = useState(false)
  const [fetchError, setFetchError] = useState<string | null>(null)

  /** Per-row busy flags while a DELETE is in flight */
  const [deactivating, setDeactivating] = useState<Record<string, boolean>>({})
  /** Per-row error messages from failed DELETE requests */
  const [rowErrors, setRowErrors] = useState<Record<string, string>>({})

  // Hydrate the API key from localStorage on first render (client-only).
  // Setting apiKey here causes fetchUrls (which depends on apiKey) to re-run
  // automatically via the effect below — no manual fetch call needed.
  useEffect(() => {
    const stored = localStorage.getItem(LS_KEY)
    if (stored) setApiKey(stored)
  }, [])

  const fetchUrls = useCallback(async () => {
    // Don't attempt a fetch without a key — it would always fail with 401.
    if (!apiKey) return
    setLoading(true)
    setFetchError(null)
    try {
      const data = await getUrls(apiKey)
      // API already returns newest-first, but sort client-side as a safety net.
      setUrls(data.slice().sort((a, b) => b.createdAt.localeCompare(a.createdAt)))
    } catch (err) {
      setFetchError(
        err instanceof ApiError ? err.message : 'Could not load URLs. Is the API running?',
      )
    } finally {
      setLoading(false)
    }
  }, [apiKey])

  useEffect(() => {
    void fetchUrls()
  }, [fetchUrls])

  function handleApiKeyChange(value: string) {
    setApiKey(value)
    localStorage.setItem(LS_KEY, value)
  }

  async function handleDeactivate(urlId: string) {
    // Clear previous error for this row
    setRowErrors((prev) => {
      const next = { ...prev }
      delete next[urlId]
      return next
    })
    setDeactivating((prev) => ({ ...prev, [urlId]: true }))

    try {
      await deactivateUrl(urlId, apiKey)
      // Update the row in place — no full page reload
      setUrls((prev) =>
        prev.map((u) => (u.urlId === urlId ? { ...u, isActive: false } : u)),
      )
    } catch (err) {
      const msg =
        err instanceof ApiError ? err.message : 'Deactivation failed. Please try again.'
      setRowErrors((prev) => ({ ...prev, [urlId]: msg }))
    } finally {
      setDeactivating((prev) => {
        const next = { ...prev }
        delete next[urlId]
        return next
      })
    }
  }

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold text-zinc-100">Admin Dashboard</h1>
        <span className="text-sm text-zinc-500">{urls.length} URL{urls.length !== 1 ? 's' : ''}</span>
      </div>

      {/* ── API Key panel ───────────────────────────────────────────────────── */}
      <div className="mb-6 rounded-xl border border-white/10 bg-zinc-900/60 p-4 shadow-sm">
        <label htmlFor="api-key-input" className="mb-1.5 block text-sm font-medium text-zinc-300">
          Admin API Key
        </label>
        <div className="flex flex-wrap gap-2">
          <input
            id="api-key-input"
            type="password"
            value={apiKey}
            onChange={(e) => handleApiKeyChange(e.target.value)}
            placeholder="Paste your X-Api-Key here"
            className="min-w-0 flex-1 rounded-lg border border-white/10 bg-zinc-950/60 px-3 py-2 text-sm text-zinc-100 shadow-sm
              placeholder:text-zinc-500
              focus:border-indigo-400/60 focus:outline-none focus:ring-2 focus:ring-indigo-500/30"
            aria-label="Admin API key"
          />
          <button
            onClick={() => void fetchUrls()}
            disabled={loading}
            className="flex items-center gap-1.5 rounded-lg bg-gradient-accent px-4 py-2 text-sm
              font-semibold text-white shadow-sm transition
              hover:opacity-90 disabled:opacity-50"
          >
            {loading && <Spinner className="h-3.5 w-3.5" />}
            {loading ? 'Loading…' : 'Refresh'}
          </button>
        </div>
        <p className="mt-1.5 text-xs text-zinc-500">
          Saved automatically in localStorage. Required for deactivating links.
        </p>
      </div>

      {/* ── Table area ──────────────────────────────────────────────────────── */}
      {loading ? (
        <TableSkeleton />
      ) : fetchError ? (
        <div
          className="rounded-xl border border-red-400/20 bg-red-400/10 px-4 py-3 text-sm text-red-300"
          role="alert"
        >
          <strong>Error:</strong> {fetchError}
        </div>
      ) : (
        <div className="overflow-x-auto rounded-xl border border-white/10 bg-zinc-900/60 shadow-sm">
          <table className="min-w-full divide-y divide-white/10 text-sm">
            <thead>
              <tr className="bg-white/[0.03]">
                {['Slug', 'Original URL', 'Created', 'Hits', 'Status', 'Action'].map(
                  (heading) => (
                    <th
                      key={heading}
                      scope="col"
                      className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-zinc-500"
                    >
                      {heading}
                    </th>
                  ),
                )}
              </tr>
            </thead>
            <tbody className="divide-y divide-white/10">
              {urls.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-12 text-center text-zinc-500">
                    No URLs yet.{' '}
                    <a href="/" className="text-indigo-400 hover:underline">
                      Shorten one on the home page!
                    </a>
                  </td>
                </tr>
              ) : (
                urls.map((u) => (
                  <tr
                    key={u.urlId}
                    className={`transition-colors ${
                      u.isActive
                        ? 'hover:bg-white/[0.03]'
                        : 'bg-white/[0.015] opacity-60'
                    }`}
                  >
                    {/* Slug */}
                    <td className="whitespace-nowrap px-4 py-3 font-mono text-indigo-300">
                      {u.urlId}
                    </td>

                    {/* Original URL — full URL shown on hover */}
                    <td className="max-w-xs px-4 py-3 text-zinc-300">
                      <span title={u.originalUrl} className="cursor-help">
                        {truncateUrl(u.originalUrl)}
                      </span>
                    </td>

                    {/* Created date */}
                    <td className="whitespace-nowrap px-4 py-3 text-zinc-500">
                      {formatDate(u.createdAt)}
                    </td>

                    {/* Hits — not yet tracked by the API */}
                    <td className="px-4 py-3 text-zinc-600">—</td>

                    {/* Status */}
                    <td className="px-4 py-3">
                      <StatusBadge active={u.isActive} />
                    </td>

                    {/* Action */}
                    <td className="px-4 py-3">
                      {u.isActive ? (
                        <DeactivateButton
                          urlId={u.urlId}
                          busy={!!deactivating[u.urlId]}
                          error={rowErrors[u.urlId]}
                          onDeactivate={handleDeactivate}
                        />
                      ) : (
                        <span className="text-xs text-zinc-600">—</span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
