using Microsoft.EntityFrameworkCore;

namespace UrlShortener.Models
{
    public class UrlShortenerContext : DbContext
    {
        public UrlShortenerContext(DbContextOptions<UrlShortenerContext> options) : base(options) { }

        public DbSet<Url> Urls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Url>(entity =>
            {
                // Speed up duplicate-detection query (POST /api/urls checks OriginalUrl + IsActive)
                entity.HasIndex(u => new { u.OriginalUrl, u.IsActive });
            });
        }
    }
}