import { ApiError } from '@/types'
import type { UrlRecord } from '@/types'

/** Strip a trailing slash so callers don't have to think about it. */
const base = () =>
  (process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000').replace(/\/$/, '')

// ── Shared response handler ───────────────────────────────────────────────────

/**
 * Reads the response body, throws a typed ApiError for non-2xx responses, and
 * returns the parsed JSON (or plain text) for success responses.
 */
async function handleResponse<T>(res: Response): Promise<T> {
  const text = await res.text().catch(() => '')

  if (!res.ok) {
    // Try to unwrap the error message from various ASP.NET Core shapes:
    // 1. Plain JSON string:   "URL must be a valid http or https address."
    // 2. ProblemDetails obj:  { "title": "...", "detail": "..." }
    // 3. Raw text / empty
    let message: string
    if (!text) {
      message =
        res.status === 401
          ? 'Invalid or missing API key.'
          : `Request failed with status ${res.status}.`
    } else {
      try {
        const parsed: unknown = JSON.parse(text)
        if (typeof parsed === 'string') {
          message = parsed
        } else if (parsed !== null && typeof parsed === 'object') {
          const obj = parsed as Record<string, unknown>
          message =
            typeof obj.detail === 'string'
              ? obj.detail
              : typeof obj.title === 'string'
                ? obj.title
                : text
        } else {
          message = text
        }
      } catch {
        message = text
      }
    }
    throw new ApiError(res.status, message)
  }

  // 204 No Content or empty body
  if (!text) return undefined as T

  try {
    return JSON.parse(text) as T
  } catch {
    return text as unknown as T
  }
}

// ── Public API functions ──────────────────────────────────────────────────────

/**
 * POST /api/urls
 * No authentication required.
 * Returns the short URL string (e.g. "http://localhost:5000/navigate/abc123").
 * Throws ApiError on 400 (validation) or network failure.
 */
export async function shortenUrl(url: string): Promise<string> {
  const res = await fetch(`${base()}/api/urls`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ url }),
  })
  // The API returns a JSON-quoted string — handleResponse already JSON.parses it.
  return handleResponse<string>(res)
}

/**
 * GET /api/urls
 * Requires X-Api-Key header.
 * Returns all active URL records ordered newest-first.
 * Throws ApiError on 401 (bad/missing key).
 */
export async function getUrls(apiKey: string): Promise<UrlRecord[]> {
  const res = await fetch(`${base()}/api/urls`, {
    // Always fetch fresh data — this is a dashboard, not a cached page.
    cache: 'no-store',
    headers: { 'X-Api-Key': apiKey },
  })
  return handleResponse<UrlRecord[]>(res)
}

/**
 * DELETE /api/urls/{slug}
 * Requires X-Api-Key header.
 * Soft-deletes the URL (sets IsActive = false).
 * Throws ApiError on 401 (bad key) or 404 (slug not found).
 */
export async function deactivateUrl(slug: string, apiKey: string): Promise<void> {
  const res = await fetch(`${base()}/api/urls/${encodeURIComponent(slug)}`, {
    method: 'DELETE',
    headers: { 'X-Api-Key': apiKey },
  })
  await handleResponse<void>(res)
}
