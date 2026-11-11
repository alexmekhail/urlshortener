using Microsoft.EntityFrameworkCore;

namespace UrlShortener.Models
{
    public class UrlShortenerContext : DbContext
    {
        public UrlShortenerContext(DbContextOptions<UrlShortenerContext> options) : base(options) { }

        public DbSet<Url> Urls { get; set; }
        public DbSet<Click> Clicks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Url>(entity =>
            {
                // Speed up duplicate-detection query (POST /api/urls checks OriginalUrl + IsActive)
                entity.HasIndex(u => new { u.OriginalUrl, u.IsActive });
            });

            modelBuilder.Entity<Click>(entity =>
            {
                // Speeds up both the stats endpoint (filter by UrlId + date range) and the
                // per-URL click count used by the list endpoint.
                entity.HasIndex(c => new { c.UrlId, c.ClickedAt });
                entity.HasOne<Url>()
                    .WithMany()
                    .HasForeignKey(c => c.UrlId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}