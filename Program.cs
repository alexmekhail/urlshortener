using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Threading.RateLimiting;
using UrlShortener.Helper;
using UrlShortener.Models;
using UrlShortener.Security;
using UrlShortener.Services;

namespace UrlShortener;

public class Program
{
    public static void Main(string[] args)
    {
        // Npgsql 6+ strict UTC mode: DateTime values must have Kind=Utc.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

        var builder = WebApplication.CreateBuilder(args);

        // Vercel (and Render) inject PORT at runtime; fall back to 5000 for local dev.
        var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        // ── Database ──────────────────────────────────────────────────────────
        // Accepts Neon postgres:// URI or standard key=value connection string.
        var rawConnection =
            builder.Configuration.GetConnectionString("DefaultConnection")
            ?? builder.Configuration["DATABASE_URL"]
            ?? string.Empty;

        var connectionString = string.IsNullOrWhiteSpace(rawConnection)
            ? "Host=localhost;Database=urlshortener_placeholder;Username=postgres;Password=postgres"
            : NormalizeConnectionString(rawConnection);

        builder.Services.AddDbContext<UrlShortenerContext>(options =>
            options.UseNpgsql(connectionString));

        // ── CORS ──────────────────────────────────────────────────────────────
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        // ── Authentication / Authorization ────────────────────────────────────
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);
        builder.Services.AddAuthorization();

        // ── Rate limiting: 10 POST /api/urls per IP per minute ────────────────
        var permitLimit = builder.Configuration.GetValue("RateLimit:PermitLimit", 10);
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("link-creation", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // ── Application services ──────────────────────────────────────────────
        builder.Services.AddScoped<ISlugGenerator, SlugGenerator>();
        builder.Services.AddSingleton<IClickEventQueue, ClickEventQueue>();
        builder.Services.AddHostedService<ClickIngestionService>();
        builder.Services.AddHttpClient<IUrlSafetyChecker, GoogleSafeBrowsingChecker>(client =>
        {
            client.BaseAddress = new Uri("https://safebrowsing.googleapis.com/");
            client.Timeout = TimeSpan.FromSeconds(2);
        });
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // ── Pipeline ──────────────────────────────────────────────────────────
        var app = builder.Build();

        // Run EF migrations only in Development (tests) or when RUN_MIGRATIONS=1 is set.
        // In production (Vercel), skip migrations on boot for a fast cold start.
        // To migrate manually: set RUN_MIGRATIONS=1 and redeploy, then remove it.
        var runMigrations =
            app.Environment.IsDevelopment() ||
            Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "1";

        if (runMigrations)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<UrlShortenerContext>();
                db.Database.Migrate();
                if (builder.Configuration.GetValue("App:SeedDemoData", true))
                    DataSeeder.Seed(db, builder.Configuration);
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Migration failed. Ensure DATABASE_URL is configured correctly.");
                throw;
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // ── Health check ──────────────────────────────────────────────────────
        app.MapGet("/health", async (UrlShortenerContext db) =>
        {
            try
            {
                await db.Database.CanConnectAsync();
                return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
            }
            catch
            {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        });

        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapControllers();
        app.Run();
    }

    // Converts a postgres:// URI (as issued by Neon/Heroku/etc.) to the
    // key=value format Npgsql expects, and enforces SSL.
    private static string NormalizeConnectionString(string raw)
    {
        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return raw;

        var uri = new Uri(raw);
        var userParts = uri.UserInfo.Split(':', 2);
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userParts[0]),
            Password = userParts.Length > 1 ? Uri.UnescapeDataString(userParts[1]) : null,
            // Prefer = use SSL when the server supports it (Neon does; local Docker may not).
            SslMode = SslMode.Prefer,
            TrustServerCertificate = true,
        };
        return csb.ConnectionString;
    }
}
