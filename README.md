# URLy — URL Shortener

A full-stack URL shortener with a custom domain, built with ASP.NET Core 8 and Next.js 14.

**Live:** [https://url-y.net](https://url-y.net) — shorten any link, no account required.
**Dashboard:** [https://frontend-am11.vercel.app/dashboard](https://frontend-am11.vercel.app/dashboard) — admin view (API key required).

- **Anyone** can shorten a link — no account or sign-up needed.
- **Admins** can list all active links and soft-delete them via an API key.
- Duplicate URLs return the existing short link instead of creating a new one.

---

## Stack

| Layer | Technology |
|-------|-----------|
| API | ASP.NET Core 8 Web API |
| Database | Azure SQL (EF Core 8) · SQLite fallback for local dev |
| Frontend | Next.js 14 (App Router) |
| Hosting — API | Azure App Service (Free tier) |
| Hosting — Frontend | Vercel |
| Custom domain + SSL | Cloudflare (proxy + Worker → Azure) |

---

## Project structure

```
urlshortener/               ← .NET solution root (git root)
├── Controllers/
│   ├── ShortURLsController.cs   (POST, GET, DELETE /api/urls)
│   └── NavigateController.cs    (GET /{slug} → redirect)
├── Migrations/
├── Models/
├── Security/
│   └── ApiKeyAuthenticationHandler.cs
├── Services/
│   └── SlugGenerator.cs
├── UrlShortener.Tests/     ← integration tests (WebApplicationFactory)
├── frontend/               ← Next.js app
│   ├── src/
│   │   ├── app/            (App Router pages)
│   │   ├── components/
│   │   ├── lib/api.ts      (all fetch calls)
│   │   └── types/
│   └── .env.local.example
├── appsettings.json
├── Program.cs
└── urlshortener.sln
```

---

## Running locally

### Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.x |
| Node.js | 18+ |

No external database needed — the API falls back to a local SQLite file automatically when `DefaultConnection` is not set.

---

### 1 — API

```bash
cd urlshortener   # solution root

# Optional: override the admin key (defaults to a dev placeholder)
dotnet user-secrets set "Auth:ApiKey" "my-secret-key"

dotnet run
# → http://localhost:5000
```

The SQLite database (`urlshortener-dev.db`) is created automatically on first run.

---

### 2 — Frontend

```bash
cd frontend
cp .env.local.example .env.local   # sets NEXT_PUBLIC_API_URL=http://localhost:5000

npm install
npm run dev
# → http://localhost:3000
```

| Page | URL | Description |
|------|-----|-------------|
| Home | `/` | Shorten any URL — no auth required |
| Dashboard | `/dashboard` | Enter your API key to list and delete links |

---

## Environment variables

### API

| Variable | Local (user-secrets) | Production (App Setting) |
|----------|---------------------|--------------------------|
| `ConnectionStrings:DefaultConnection` | `dotnet user-secrets set` | `CONNECTIONSTRINGS__DEFAULTCONNECTION` |
| `Auth:ApiKey` | `dotnet user-secrets set` | `AUTH__APIKEY` |
| `App:ShortUrlDomain` | `appsettings.json` | `APP__SHORTURLDOMAIN` |
| `Cors:AllowedOrigins:0` | `appsettings.json` | `CORS__ALLOWEDORIGINS__0` |

> `App:ShortUrlDomain` sets the canonical domain embedded in generated short links (e.g. `https://url-y.net`).
> If unset the API falls back to the incoming request host — correct for local dev.

### Frontend

| Variable | Purpose | Example |
|----------|---------|---------|
| `NEXT_PUBLIC_API_URL` | Base URL of the .NET API (no trailing slash) | `https://url-y.net` |

---

## API reference

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/urls` | None | Shorten a URL. Body: `{"url":"https://..."}`. Returns 201 (new) or 200 (duplicate). |
| `GET` | `/api/urls` | `X-Api-Key` | List all active URLs, newest first. |
| `GET` | `/api/urls/{slug}` | None | Get a single URL record. |
| `DELETE` | `/api/urls/{slug}` | `X-Api-Key` | Soft-delete (sets `IsActive = false`). |
| `GET` | `/{slug}` | None | Redirect to the original URL (302). Unknown slugs redirect to google.com. |

---

## Running the tests

```bash
dotnet test urlshortener.sln --verbosity normal
```

Uses an EF Core in-memory database and `WebApplicationFactory` — no real database or secrets needed.

---

## Deploying to production

### Frontend → Vercel

1. Import the repo in the [Vercel dashboard](https://vercel.com/new).
2. Set **Root Directory** to `urlshortener/frontend`.
3. Add environment variable: `NEXT_PUBLIC_API_URL = https://url-y.net`
4. Deploy.

### API → Azure App Service

1. Publish the project: `dotnet publish -c Release -o ./publish-output`
2. Zip and deploy: `az webapp deploy --resource-group <rg> --name <app> --src-path api-deploy.zip --type zip`
3. Add Application Settings:

   | Name | Value |
   |------|-------|
   | `CONNECTIONSTRINGS__DEFAULTCONNECTION` | Azure SQL connection string |
   | `AUTH__APIKEY` | Strong random secret |
   | `APP__SHORTURLDOMAIN` | `https://url-y.net` |
   | `CORS__ALLOWEDORIGINS__0` | `https://frontend-am11.vercel.app` |

### Custom domain (Cloudflare)

A Cloudflare Worker proxies `url-y.net` → `alexmekhurlshortener.azurewebsites.net`, with the `Host` header rewritten so Azure routes the request correctly. Cloudflare handles SSL for free.

---

## CI

GitHub Actions (`.github/workflows/build.yml`) runs on every push to `main`:

1. Restore dependencies
2. Build (Release)
3. Run all tests (`Auth__ApiKey` injected via workflow `env`)
