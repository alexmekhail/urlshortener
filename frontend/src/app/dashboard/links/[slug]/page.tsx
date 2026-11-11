'use client'

import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'next/navigation'
import Link from 'next/link'
import { getUrlStats } from '@/lib/api'
import { ApiError } from '@/types'
import type { UrlStats } from '@/types'
import Spinner from '@/components/Spinner'
import ClicksChart from '@/components/ClicksChart'
import BreakdownList from '@/components/BreakdownList'

const LS_KEY = 'urlshortener_apikey'
const DAY_OPTIONS = [7, 30, 90] as const

export default function LinkStatsPage() {
  const params = useParams<{ slug: string }>()
  const slug = decodeURIComponent(params.slug)

  const [apiKey, setApiKey] = useState('')
  const [days, setDays] = useState<(typeof DAY_OPTIONS)[number]>(30)
  const [stats, setStats] = useState<UrlStats | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const stored = localStorage.getItem(LS_KEY)
    if (stored) setApiKey(stored)
  }, [])

  const fetchStats = useCallback(async () => {
    if (!apiKey) return
    setLoading(true)
    setError(null)
    try {
      const data = await getUrlStats(slug, apiKey, days)
      setStats(data)
    } catch (err) {
      setError(
        err instanceof ApiError ? err.message : 'Could not load stats. Is the API running?',
      )
    } finally {
      setLoading(false)
    }
  }, [apiKey, slug, days])

  useEffect(() => {
    void fetchStats()
  }, [fetchStats])

  return (
    <div className="mx-auto max-w-4xl px-4 py-8">
      <Link
        href="/dashboard"
        className="mb-4 inline-block text-sm text-zinc-400 transition hover:text-zinc-100"
      >
        ← Back to dashboard
      </Link>

      <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
        <h1 className="font-mono text-2xl font-bold text-indigo-300">{slug}</h1>
        <div className="flex gap-1 rounded-lg border border-white/10 bg-zinc-900/60 p-1">
          {DAY_OPTIONS.map((d) => (
            <button
              key={d}
              onClick={() => setDays(d)}
              className={`rounded-md px-3 py-1 text-xs font-medium transition ${
                days === d
                  ? 'bg-gradient-accent text-white'
                  : 'text-zinc-400 hover:text-zinc-100'
              }`}
            >
              {d}d
            </button>
          ))}
        </div>
      </div>

      {!apiKey ? (
        <div className="rounded-xl border border-white/10 bg-zinc-900/60 px-4 py-3 text-sm text-zinc-400">
          Enter your admin API key on the{' '}
          <Link href="/dashboard" className="text-indigo-400 hover:underline">
            dashboard
          </Link>{' '}
          to view stats.
        </div>
      ) : loading ? (
        <div className="space-y-2 py-4" aria-label="Loading stats…">
          {[...Array(3)].map((_, i) => (
            <div key={i} className="h-10 animate-pulse rounded-md bg-white/5" />
          ))}
        </div>
      ) : error ? (
        <div
          className="rounded-xl border border-red-400/20 bg-red-400/10 px-4 py-3 text-sm text-red-300"
          role="alert"
        >
          <strong>Error:</strong> {error}
        </div>
      ) : stats ? (
        <div className="space-y-6">
          <div className="rounded-xl border border-white/10 bg-zinc-900/60 p-5 shadow-glow-sm">
            <p className="mb-1 text-xs font-semibold uppercase tracking-widest text-violet-400">
              Total clicks (last {days} days)
            </p>
            <p className="mb-4 text-4xl font-extrabold text-zinc-50">{stats.totalClicks}</p>
            <ClicksChart data={stats.clicksPerDay} label={`Clicks per day for ${slug}`} />
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <BreakdownList title="Top Referrers" items={stats.topReferrers} emptyLabel="No referrer data yet" />
            <BreakdownList title="Countries" items={stats.countries} emptyLabel="No country data yet" />
            <BreakdownList title="Devices" items={stats.devices} emptyLabel="No device data yet" />
            <BreakdownList title="Browsers" items={stats.browsers} emptyLabel="No browser data yet" />
          </div>
        </div>
      ) : null}
    </div>
  )
}
