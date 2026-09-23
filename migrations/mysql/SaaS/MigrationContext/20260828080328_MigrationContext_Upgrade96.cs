using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade96 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_created_by_entry_id_last_edit_date_id",
                table: "ai_integration_threads",
                columns: new[] { "tenant_id", "created_by", "entry_id", "last_edit_date", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_thread_id_timestamp_id",
                table: "ai_integration_messages",
                columns: new[] { "tenant_id", "thread_id", "timestamp", "id" });

            migrationBuilder.DropIndex(
                name: "IX_tenant_id_created_by",
                table: "ai_integration_threads");

            migrationBuilder.DropIndex(
                name: "IX_tenant_id_thread_id_timestamp",
                table: "ai_integration_messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_created_by",
                table: "ai_integration_threads",
                columns: new[] { "tenant_id", "created_by" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_thread_id_timestamp",
                table: "ai_integration_messages",
                columns: new[] { "tenant_id", "thread_id", "timestamp" });

            migrationBuilder.DropIndex(
                name: "IX_tenant_id_created_by_entry_id_last_edit_date_id",
                table: "ai_integration_threads");

            migrationBuilder.DropIndex(
                name: "IX_tenant_id_thread_id_timestamp_id",
                table: "ai_integration_messages");
        }
    }
}
