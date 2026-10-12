using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade92 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing grants are cleared rather than backfilled, because the owner could only be derived by
            // joining identity_clients, which is not the source of truth in SaaS, where clients live in DynamoDB.
            // The cost is that users re-authorize once; in exchange no grant is left behind with an owner that
            // cannot be resolved. Consents are deleted rather than truncated because identity_consent_scopes
            // references them, and the delete cascades.
            migrationBuilder.Sql("DELETE FROM identity_consents;");

            migrationBuilder.Sql("TRUNCATE TABLE identity_authorizations;");

            migrationBuilder.AddColumn<long>(
                name: "owner_tenant_id",
                table: "identity_consents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "owner_user_id",
                table: "identity_consents",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "owner_tenant_id",
                table: "identity_authorizations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "owner_user_id",
                table: "identity_authorizations",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_owner",
                table: "identity_consents",
                columns: new[] { "owner_tenant_id", "owner_user_id" });

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_owner",
                table: "identity_authorizations",
                columns: new[] { "owner_tenant_id", "owner_user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_identity_consents_owner",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_owner",
                table: "identity_authorizations");

            migrationBuilder.DropColumn(
                name: "owner_tenant_id",
                table: "identity_consents");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "identity_consents");

            migrationBuilder.DropColumn(
                name: "owner_tenant_id",
                table: "identity_authorizations");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "identity_authorizations");
        }
    }
}
