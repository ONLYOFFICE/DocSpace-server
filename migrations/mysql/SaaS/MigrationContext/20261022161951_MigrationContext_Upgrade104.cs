using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade104 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "retried_by",
                table: "webhooks_logs",
                type: "varchar(36)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<int>(
                name: "retry_of",
                table: "webhooks_logs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "tenant_id_retry_of",
                table: "webhooks_logs",
                columns: new[] { "tenant_id", "retry_of" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "tenant_id_retry_of",
                table: "webhooks_logs");

            migrationBuilder.DropColumn(
                name: "retried_by",
                table: "webhooks_logs");

            migrationBuilder.DropColumn(
                name: "retry_of",
                table: "webhooks_logs");
        }
    }
}
