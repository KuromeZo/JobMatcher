using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobMatcher.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_Login",
                table: "Users",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoredJobs_Guid",
                table: "ScoredJobs",
                column: "Guid");

            // Before the unique index existed, two concurrent requests could cache the same score twice.
            // Scores are only a cache, so the duplicates are dropped and the newest one is kept.
            migrationBuilder.Sql("""
                DELETE FROM "ScoredJobs" a
                USING "ScoredJobs" b
                WHERE a."UserId" = b."UserId" AND a."Guid" = b."Guid" AND a."Id" < b."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ScoredJobs_UserId_Guid",
                table: "ScoredJobs",
                columns: new[] { "UserId", "Guid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_Category_ExperienceLevel",
                table: "JobOffers",
                columns: new[] { "Category", "ExperienceLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_FetchedAt",
                table: "JobOffers",
                column: "FetchedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Login",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_ScoredJobs_Guid",
                table: "ScoredJobs");

            migrationBuilder.DropIndex(
                name: "IX_ScoredJobs_UserId_Guid",
                table: "ScoredJobs");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_Category_ExperienceLevel",
                table: "JobOffers");

            migrationBuilder.DropIndex(
                name: "IX_JobOffers_FetchedAt",
                table: "JobOffers");
        }
    }
}
