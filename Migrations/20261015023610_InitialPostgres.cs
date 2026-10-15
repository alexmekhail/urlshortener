using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace urlshortener.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Urls",
                columns: table => new
                {
                    UrlId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OriginalUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ShortenedUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Urls", x => x.UrlId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Urls_OriginalUrl_IsActive",
                table: "Urls",
                columns: new[] { "OriginalUrl", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Urls");
        }
    }
}
