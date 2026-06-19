using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade86 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_integration_assignments_ai_integration_profiles_profile_id",
                table: "ai_integration_assignments");

            migrationBuilder.DropIndex(
                name: "IX_ai_integration_assignments_profile_id",
                table: "ai_integration_assignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ai_integration_assignments_profile_id",
                table: "ai_integration_assignments",
                column: "profile_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_integration_assignments_ai_integration_profiles_profile_id",
                table: "ai_integration_assignments",
                column: "profile_id",
                principalTable: "ai_integration_profiles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
