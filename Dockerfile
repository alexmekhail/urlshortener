# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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
# PublishReadyToRun pre-compiles to native code, cutting cold-start JIT time.
RUN dotnet publish -c Release -o /publish -p:PublishReadyToRun=true

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /publish .
ENV ASPNETCORE_ENVIRONMENT=Production
# Vercel injects PORT at runtime; Program.cs reads it via Environment.GetEnvironmentVariable.
EXPOSE 8080
ENTRYPOINT ["dotnet", "urlshortener.dll"]
