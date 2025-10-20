using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade42 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_authorization_client_id",
                table: "identity_authorizations");

            migrationBuilder.DropForeignKey(
                name: "identity_consents_ibfk_1",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_consents_is_invalidated",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_consents_principal_id",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_consents_registered_client_id",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_clients_is_invalidated",
                table: "identity_clients");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_grant_type",
                table: "identity_authorizations");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_is_invalidated",
                table: "identity_authorizations");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_principal_id",
                table: "identity_authorizations");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_registered_client_id",
                table: "identity_authorizations");

            migrationBuilder.RenameColumn(
                name: "scope_name",
                table: "identity_consent_scopes",
                newName: "scopes");

            migrationBuilder.RenameIndex(
                name: "idx_identity_consent_scopes_scope_name",
                table: "identity_consent_scopes",
                newName: "idx_identity_consent_scopes_scopes");

            migrationBuilder.AlterColumn<string>(
                name: "registered_client_id",
                table: "identity_authorizations",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(36)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_client_secret",
                table: "identity_clients",
                column: "client_secret");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_id",
                table: "identity_authorizations",
                column: "id");

            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_old_authorizations\r\nON SCHEDULE EVERY 1 DAY\r\nON COMPLETION PRESERVE\r\nDO\r\nDELETE FROM identity_authorizations\r\nWHERE modified_at < NOW() - INTERVAL 30 DAY;");
            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_old_consents\r\nON SCHEDULE EVERY 1 DAY\r\nON COMPLETION PRESERVE\r\nDO\r\nDELETE FROM identity_consents\r\nWHERE modified_at < NOW() - INTERVAL 30 DAY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_client_secret",
                table: "identity_clients");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_id",
                table: "identity_authorizations");

            migrationBuilder.RenameColumn(
                name: "scopes",
                table: "identity_consent_scopes",
                newName: "scope_name");

            migrationBuilder.RenameIndex(
                name: "idx_identity_consent_scopes_scopes",
                table: "identity_consent_scopes",
                newName: "idx_identity_consent_scopes_scope_name");

            migrationBuilder.AlterColumn<string>(
                name: "registered_client_id",
                table: "identity_authorizations",
                type: "varchar(36)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_is_invalidated",
                table: "identity_consents",
                column: "is_invalidated");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_principal_id",
                table: "identity_consents",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_registered_client_id",
                table: "identity_consents",
                column: "registered_client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_clients_is_invalidated",
                table: "identity_clients",
                column: "is_invalidated");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_grant_type",
                table: "identity_authorizations",
                column: "authorization_grant_type");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_is_invalidated",
                table: "identity_authorizations",
                column: "is_invalidated");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_principal_id",
                table: "identity_authorizations",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_registered_client_id",
                table: "identity_authorizations",
                column: "registered_client_id");

            migrationBuilder.AddForeignKey(
                name: "FK_authorization_client_id",
                table: "identity_authorizations",
                column: "registered_client_id",
                principalTable: "identity_clients",
                principalColumn: "client_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "identity_consents_ibfk_1",
                table: "identity_consents",
                column: "registered_client_id",
                principalTable: "identity_clients",
                principalColumn: "client_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}