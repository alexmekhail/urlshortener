using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace urlshortener.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkSafetyCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSafetyCheckAt",
                table: "Urls",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSafetyCheckAt",
                table: "Urls");
        }
    }
}
