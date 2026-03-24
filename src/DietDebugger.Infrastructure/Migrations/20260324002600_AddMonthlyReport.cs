using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietDebugger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Assessment = table.Column<string>(type: "jsonb", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReports_UserId_MonthStart",
                table: "MonthlyReports",
                columns: new[] { "UserId", "MonthStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyReports");
        }
    }
}
