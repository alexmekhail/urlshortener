/**
 * Matches the C# Url entity returned by GET /api/urls.
 * Property names are camelCase because ASP.NET Core serialises with
 * System.Text.Json's default camelCase policy.
 */
export interface UrlRecord {
  urlId: string
  userId: number
  originalUrl: string
  shortenedUrl: string
  /** ISO 8601 datetime string */
  createdAt: string
  /** ISO 8601 datetime string, or null when no expiry is set */
  expiresAt: string | null
  isActive: boolean
  /** Total recorded clicks for this link. */
  clickCount: number
}

/** One named count in a stats breakdown (referrer host, country, device, browser). */
export interface CountBreakdown {
  key: string
  count: number
}

/** Click count for a single calendar day (ISO 8601 date, e.g. "2026-11-10"). */
export interface DailyClickCount {
  date: string
  count: number
}

/** Response shape of GET /api/urls/{slug}/stats */
export interface UrlStats {
  slug: string
  totalClicks: number
  clicksPerDay: DailyClickCount[]
  topReferrers: CountBreakdown[]
  countries: CountBreakdown[]
  devices: CountBreakdown[]
  browsers: CountBreakdown[]
}

/** Body sent to POST /api/urls */
export interface CreateUrlRequest {
  url: string
}

/**
 * The POST /api/urls endpoint returns a plain JSON-quoted string — not an
 * object — so there is no wrapper type.  This alias documents the intent.
 */
export type ShortUrlResponse = string

/** Typed error thrown by every function in lib/api.ts */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}
