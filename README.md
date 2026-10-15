# URLy — URL Shortener

A full-stack URL shortener: paste a long link, get a short one. Built as a portfolio demo.

**Live demo:** https://www.url-y.net *(~1 min cold start — Render free tier sleeps after 15 min of inactivity)*

---

## Stack

| Layer | Technology |
|---|---|
| API | C# / ASP.NET Core 8 |
| ORM | Entity Framework Core 8 + Npgsql |
| Database | Neon Postgres (serverless, free tier) |
| Frontend | Next.js 14 / React 18 / Tailwind CSS (static export, served by ASP.NET) |
| Hosting | Render free web service (Docker) |
| CI | GitHub Actions |

## Architecture

```mermaid
graph LR
    Browser -->|GET /| API[ASP.NET Core\nRender Web Service]
    Browser -->|POST /api/urls| API
    Browser -->|GET /abc123| API
    API -->|Migrate + Query| DB[(Neon Postgres)]
    API -->|Serves wwwroot| Static[Next.js Static Export]
```

> The frontend is compiled to static HTML/JS at Docker build time and served directly by
> ASP.NET Core as `wwwroot`. No separate frontend server is needed.

---

## Key Features

- **Shorten any http/https URL** — no account required
- **Admin dashboard** — list and deactivate links (requires API key)
- **Case-sensitive slugs** — 6-character base-62 random codes
- **Duplicate detection** — posting the same URL twice returns the same short link
- **Security**: rate limiting (10 POST/IP/min), http/https-only, max URL length (2000 chars), self-referral rejection
- **Auto-migrations** on startup — schema is always up to date
- **`/health`** endpoint for Render's health check

---

## API Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/urls` | None | Create a short URL |
| `GET` | `/api/urls` | API key | List all active URLs |
| `GET` | `/api/urls/{slug}` | None | Get URL record by slug |
| `DELETE` | `/api/urls/{slug}` | API key | Soft-delete (deactivate) |
| `GET` | `/{slug}` | None | Redirect to original URL |
| `GET` | `/health` | None | Health check |

**Example — shorten a URL:**
```bash
curl -X POST https://www.url-y.net/api/urls \
  -H "Content-Type: application/json" \
  -d '{"url": "https://github.com/alexmekhail"}'
# → "https://www.url-y.net/ab1Cd2"
```

**Example — delete a link (requires API key from Render dashboard):**
```bash
curl -X DELETE https://www.url-y.net/api/urls/ab1Cd2 \
  -H "X-Api-Key: your-api-key"
```

---

## Screenshots

> *Add a screenshot of the home page here*
> ![Home page](docs/screenshot-home.png)

> *Add a screenshot of the dashboard here*
> ![Dashboard](docs/screenshot-dashboard.png)

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

# Build and start (API on http://localhost:5000)
docker compose up --build

# The API is now at http://localhost:5000
# Swagger UI:  http://localhost:5000/swagger
```

**Without Docker (frontend + backend separately):**
```bash
# Terminal 1 — API
dotnet run --project urlshortener.csproj
# Requires a local Postgres; set DATABASE_URL in your environment or user-secrets

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
| `AUTH__APIKEY` | Yes | Secret key for the admin API (set `X-Api-Key` header) |
| `APP__SHORTURLDOMAIN` | Yes | Public base URL used to build short links, e.g. `https://urlshortener.onrender.com` |
| `PORT` | Set by Render | TCP port to listen on (defaults to 5000 locally) |
| `CORS__ALLOWEDORIGINS__0` | No | Extra origin to allow (not needed when frontend is same-origin) |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` enables Swagger UI |

---

## Deployment (Render + Neon)

1. **Neon:** create a free Postgres project → copy the connection string.
2. **Render:** create a new Web Service → connect this repo → choose *Docker*.
3. Set env vars in the Render dashboard:
   - `DATABASE_URL` → paste the Neon connection string
   - `APP__SHORTURLDOMAIN` → paste the `https://<service>.onrender.com` URL Render assigns
   - `AUTH__APIKEY` → Render can generate one for you (Blueprint does this automatically)
4. (Optional) Use the **Render Blueprint** — connect the repo and Render will read `render.yaml` to configure everything automatically.

Render auto-deploys from `main` on every push.

---

## Hosting History

Originally deployed on **Azure App Service** (Free tier) with **Azure SQL Database** (serverless).
Azure SQL alone was costing ~$10/day even on the serverless plan.
Migrated to **Render + Neon Postgres** in October 2026 — total hosting cost is now **$0/month**.
