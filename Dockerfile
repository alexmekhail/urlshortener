# ── Stage 1: Build Next.js frontend ──────────────────────────────────────────
FROM node:20-alpine AS frontend
WORKDIR /src
COPY frontend/package*.json ./
RUN npm ci --prefer-offline
COPY frontend/ .
# Empty NEXT_PUBLIC_API_URL → relative /api/... calls (same-origin, no CORS needed)
ENV NEXT_PUBLIC_API_URL=""
RUN npm run build

# ── Stage 2: Build ASP.NET Core API ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src
COPY urlshortener.csproj ./
RUN dotnet restore
COPY Controllers/ ./Controllers/
COPY Helper/ ./Helper/
COPY Migrations/ ./Migrations/
COPY Models/ ./Models/
COPY Properties/ ./Properties/
COPY Security/ ./Security/
COPY Services/ ./Services/
COPY Program.cs .
COPY UrlShortenerContextFactory.cs .
COPY appsettings*.json .
RUN dotnet publish -c Release -o /publish

# ── Stage 3: Runtime image ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=backend /publish .
# wwwroot is where ASP.NET Core's static files middleware looks by default
COPY --from=frontend /src/out ./wwwroot

ENV ASPNETCORE_ENVIRONMENT=Production
# PORT is injected by Render at runtime; Program.cs reads it via Environment.GetEnvironmentVariable
EXPOSE 10000
ENTRYPOINT ["dotnet", "urlshortener.dll"]
