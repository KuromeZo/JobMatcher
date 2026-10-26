using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobMatcher.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferExpiredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAt",
                table: "JobOffers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_ExpiredAt",
                table: "JobOffers",
                column: "ExpiredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobOffers_ExpiredAt",
                table: "JobOffers");

            migrationBuilder.DropColumn(
                name: "ExpiredAt",
                table: "JobOffers");
        }
    }
}
