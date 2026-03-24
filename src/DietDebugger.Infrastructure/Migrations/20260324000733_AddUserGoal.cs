using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietDebugger.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserGoals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalType = table.Column<string>(type: "text", nullable: false),
                    DailyCalorieTarget = table.Column<int>(type: "integer", nullable: false),
                    ProteinTargetG = table.Column<int>(type: "integer", nullable: true),
                    FatTargetG = table.Column<int>(type: "integer", nullable: true),
                    CarbsTargetG = table.Column<int>(type: "integer", nullable: true),
                    ActiveSince = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGoals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserGoals_UserId",
                table: "UserGoals",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserGoals");
        }
    }
}
