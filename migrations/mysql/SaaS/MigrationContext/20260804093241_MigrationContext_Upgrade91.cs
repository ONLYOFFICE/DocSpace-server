using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade91 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_identity_consent_scopes_principal_id",
                table: "identity_consent_scopes");

            migrationBuilder.DropIndex(
                name: "idx_identity_consent_scopes_registered_client_id",
                table: "identity_consent_scopes");

            migrationBuilder.DropIndex(
                name: "UK_client_id",
                table: "identity_clients");

            migrationBuilder.RenameIndex(
                name: "IX_identity_authorizations_tenant_id",
                table: "identity_authorizations",
                newName: "idx_identity_authorizations_tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_principal_id",
                table: "identity_consents",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_access_token_hash",
                table: "identity_authorizations",
                column: "access_token_hash")
                .Annotation("MySql:IndexPrefixLength", new[] { 64 });

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_authorization_code_value",
                table: "identity_authorizations",
                column: "authorization_code_value")
                .Annotation("MySql:IndexPrefixLength", new[] { 255 });

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_refresh_token_hash",
                table: "identity_authorizations",
                column: "refresh_token_hash")
                .Annotation("MySql:IndexPrefixLength", new[] { 64 });

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_registered_client_id",
                table: "identity_authorizations",
                column: "registered_client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_state",
                table: "identity_authorizations",
                column: "state");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_identity_consents_principal_id",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_access_token_hash",
                table: "identity_authorizations");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_authorization_code_value",
                table: "identity_authorizations");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_refresh_token_hash",
                table: "identity_authorizations");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_registered_client_id",
                table: "identity_authorizations");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_state",
                table: "identity_authorizations");

            migrationBuilder.RenameIndex(
                name: "idx_identity_authorizations_tenant_id",
                table: "identity_authorizations",
                newName: "IX_identity_authorizations_tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consent_scopes_principal_id",
                table: "identity_consent_scopes",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consent_scopes_registered_client_id",
                table: "identity_consent_scopes",
                column: "registered_client_id");

            migrationBuilder.CreateIndex(
                name: "UK_client_id",
                table: "identity_clients",
                column: "client_id",
                unique: true);
        }
    }
}
