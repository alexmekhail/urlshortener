# URLy — URL Shortener

A full-stack URL shortener: paste a long link, get a short one. Built as a portfolio demo.

**Live demo:** https://www.url-y.net

---

## Stack

| Layer | Technology |
|---|---|
| API | C# / ASP.NET Core 8 |
| ORM | Entity Framework Core 8 + Npgsql |
| Database | Neon Postgres (serverless, free tier) |
| Frontend | Next.js 15 / React / Tailwind CSS |
| Hosting | Vercel (container runtime + Next.js, one project) |

## Architecture

```mermaid
graph LR
    Browser -->|GET /| Vercel[Vercel Edge\nRewrite]
    Vercel -->|"/api/*, /health, /{slug}"| API[ASP.NET Core\nContainer service]
    Vercel -->|/*| FE[Next.js\nFrontend service]
    API -->|Query| DB[(Neon Postgres)]
```

Both services share the same Vercel domain. Routing is handled by `vercel.json`:

- `GET /{6-char-slug}` → API (single indexed lookup, 302 redirect, 5-min CDN cache)
- `POST /api/urls` → API
- `GET /api/urls` → API
- `DELETE /api/urls/{slug}` → API
- `GET /health` → API
- `GET /*` → Next.js frontend

---

## Key Features

- **Shorten any http/https URL** — no account required
- **Admin dashboard** — list and deactivate links, with a per-link click count (requires API key)
- **Click analytics** — total clicks, a daily time series, and referrer/country/device/browser breakdowns per link
- **Case-sensitive slugs** — 6-character base-62 random codes
- **Link expiration** — optional `expiresIn` of 1 hour, 1 day, 7 days, or 30 days; expired links stop redirecting
- **Duplicate detection** — posting the same URL twice (with no expiry) returns the same short link
- **Security**: rate limiting (10 POST/IP/min), http/https-only, max URL length (2000 chars), self-referral rejection
- **`/health`** endpoint for uptime monitoring
- **Fast cold starts** — `PublishReadyToRun` pre-compiles .NET to native code; Vercel scales down after 5 min idle but restarts in ~2–5 s

### Click Analytics

Every redirect enqueues a click event (timestamp, referrer host, country, device type,
browser — **never the raw IP address**) onto a bounded in-memory queue. A background
service drains the queue and batch-inserts into Postgres, so recording a click never
adds latency to the redirect response; if the queue is ever full, new events are
dropped rather than blocking.

Country comes from the `x-vercel-ip-country` header (present on Vercel; absent/`null`
elsewhere). Device type and browser are parsed from the User-Agent with a small
built-in heuristic — no external UA-parsing dependency.

**Caching tradeoff:** redirects used to be cached by the CDN/browser for 5 minutes
(`public, max-age=300`). Click analytics require every click to actually reach the
API, so redirects are now served with `Cache-Control: private, no-store`. This trades
away CDN offload for accurate click counts — every redirect is a live request to the API.

### Link Expiration

`POST /api/urls` accepts an optional `expiresIn`: one of `"1h"`, `"1d"`, `"7d"`, `"30d"`,
or omitted/`null` for a link that never expires. Invalid values return `400`.

Expiry is checked at redirect time, not via a cleanup job — an expired link simply
stops redirecting (it stays in the database and still counts toward duplicate checks
as "not a match"). Unknown, deactivated, and expired slugs all redirect (302) to a
frontend page instead of the API returning a raw 404/410:

- Unknown or deactivated slug → `/link-not-found`
- Known slug past its `expiresAt` → `/link-expired`

Duplicate detection only reuses an existing link when the request has **no** `expiresIn`
and the existing link is active, not expired, and also has no expiry — a link created
with an expiry is never deduplicated against.

---

## API Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/urls` | None | Create a short URL |
| `GET` | `/api/urls` | API key | List all active URLs |
| `GET` | `/api/urls/{slug}` | None | Get URL record by slug |
| `DELETE` | `/api/urls/{slug}` | API key | Soft-delete (deactivate) |
| `GET` | `/{slug}` | None | Redirect to original URL (records a click) |
| `GET` | `/api/urls/{slug}/stats?days=30` | API key | Click analytics: totals, daily series, referrer/country/device/browser breakdowns |
| `GET` | `/health` | None | Health check |

**Example — shorten a URL:**
```bash
curl -X POST https://www.url-y.net/api/urls \
  -H "Content-Type: application/json" \
  -d '{"url": "https://github.com/alexmekhail"}'
# → "https://www.url-y.net/ab1Cd2"
```

**Example — delete a link (requires API key):**
```bash
curl -X DELETE https://www.url-y.net/api/urls/ab1Cd2 \
  -H "X-Api-Key: your-api-key"
```

---

## Testing

**What's covered:**
- 4 slug generator unit tests (length, charset, variety, no special chars)
- 15 API integration tests (create, delete, list, redirect, CORS, auth)
- 3 case-sensitivity tests (Postgres is case-sensitive; slugs must match exactly)
- URL validation tests (max length, self-referral rejection, http/https-only)
- Rate limiting tests (POST returns 429 after limit is exceeded; GET is not limited)

Integration tests use **Testcontainers** — a real Postgres container spins up per test class. No mocks.

**Run locally:**
```bash
dotnet test urlshortener.sln --verbosity normal
```

> Docker must be running (Testcontainers needs it for the Postgres container).

---

## Local Dev Setup

**With Docker Compose (recommended):**
```bash
# Copy and edit env vars
cp .env.example .env.local      # then fill in AUTH__APIKEY

# Build and start (API on http://localhost:5001, Postgres on 5433)
docker compose up --build
```

**Without Docker (frontend + backend separately):**
```bash
# Terminal 1 — API
dotnet run --project urlshortener.csproj
# Requires a local Postgres; set DATABASE_URL in your environment or user-secrets.
# Schema is created automatically on first run (Development mode).

# Terminal 2 — Frontend dev server
cd frontend
cp .env.local.example .env.local    # set NEXT_PUBLIC_API_URL=http://localhost:5000
npm install && npm run dev
# Frontend: http://localhost:3000
```

---

## Environment Variables

| Variable | Required | Description |
|---|---|---|
| `DATABASE_URL` | Yes | Neon (or any Postgres) connection string — `postgres://user:pass@host/db` |
| `AUTH__APIKEY` | Yes | Secret key for the admin API (`X-Api-Key` header) |
| `APP__SHORTURLDOMAIN` | Yes | Public base URL used to build short links, e.g. `https://www.url-y.net` |
| `PORT` | Set by Vercel | TCP port to listen on (defaults to 5000 locally) |
| `RUN_MIGRATIONS` | No | Set to `1` to run EF migrations on boot (use once after schema changes, then remove) |
| `CORS__ALLOWEDORIGINS__0` | No | Extra origin to allow (not needed when frontend is same-origin) |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` enables Swagger UI |

---

## Deployment (Vercel + Neon)

### One-time setup

1. **Neon:** create a free Postgres project → copy the **pooled** connection string (the one with `-pooler` in the hostname).

2. **Vercel:** create a new project → connect this repo → Vercel will detect `vercel.json` and configure two services automatically:
   - `frontend` — Next.js (auto-detected)
   - `api` — ASP.NET Core container (Dockerfile)

3. Set environment variables in the Vercel project dashboard (**Settings → Environment Variables**):

   | Variable | Value |
   |---|---|
   | `DATABASE_URL` | Neon pooled `postgres://...` URI |
   | `AUTH__APIKEY` | A strong random secret (e.g. `openssl rand -hex 32`) |
   | `APP__SHORTURLDOMAIN` | Your Vercel production URL, e.g. `https://www.url-y.net` |

4. **Run migrations once:** add `RUN_MIGRATIONS=1` to the production env vars, deploy, then remove it. Subsequent deploys skip migrations for a fast cold start.

   Alternatively, run migrations from your local machine:
   ```bash
   DATABASE_URL="<your-neon-connection-string>" dotnet ef database update
   ```

### Redeploy

Push to `main` — Vercel rebuilds and redeploys both services automatically.

---

## Hosting History

- **Azure App Service + Azure SQL** — Azure SQL alone cost ~$10/day on the serverless plan.
- **Render + Neon Postgres** (October 2026) — $0/month, but ~60-second cold starts on Render's free tier.
- **Vercel + Neon Postgres** (November 2026) — $0/month, ~2–5-second cold starts on Vercel's container runtime.
