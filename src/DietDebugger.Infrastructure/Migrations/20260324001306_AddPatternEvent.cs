using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietDebugger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatternEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatternEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatternKey = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Acknowledged = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatternEvents_UserId_Acknowledged_TriggeredAt",
                table: "PatternEvents",
                columns: new[] { "UserId", "Acknowledged", "TriggeredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatternEvents");
        }
    }
}
