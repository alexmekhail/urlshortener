using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Models;
using UrlShortener.Security;
using UrlShortener.Services;

namespace UrlShortener;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── Database ──────────────────────────────────────────────────────────
        // Use SQL Server in production (when a connection string is supplied).
        // Fall back to a local SQLite file in development so the app works
        // without any external database infrastructure.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Services.AddDbContext<UrlShortenerContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                options.UseSqlite("Data Source=urlshortener-dev.db");
            else
                options.UseSqlServer(connectionString);
        });

        // ── CORS ──────────────────────────────────────────────────────────────
        // Origins are read from Cors:AllowedOrigins in appsettings / env vars so
        // the production Vercel URL can be wired up without a code change.
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

        // ── Application services ──────────────────────────────────────────────
        builder.Services.AddScoped<ISlugGenerator, SlugGenerator>();

        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // ── Pipeline ──────────────────────────────────────────────────────────
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            // Auto-create the SQLite schema when no SQL Server string is configured.
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                using var scope = app.Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<UrlShortenerContext>()
                    .Database.EnsureCreated();
            }
        }

        // HTTPS redirection is intentionally disabled: in production, Cloudflare
        // terminates TLS and forwards plain HTTP to Azure. Enabling this would
        // cause redirect loops since the app has no HTTPS binding on the Free tier.
        // app.UseHttpsRedirection();
        app.UseCors();          // must be before UseAuthentication / UseAuthorization
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}
