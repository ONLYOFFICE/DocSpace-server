using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class IdentityContextMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_certs",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true),
                    pair_type = table.Column<sbyte>(type: "tinyint", nullable: false),
                    private_key = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    public_key = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_scopes",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    group = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.name);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_shedlock",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    lock_until = table.Column<DateTime>(type: "timestamp(3)", nullable: false),
                    locked_at = table.Column<DateTime>(type: "timestamp(3)", nullable: false),
                    locked_by = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.name);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tenants_tenants",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    alias = table.Column<string>(type: "varchar(100)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    mappeddomain = table.Column<string>(type: "varchar(100)", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    version = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'2'"),
                    version_changed = table.Column<DateTime>(type: "datetime", nullable: true),
                    language = table.Column<string>(type: "char(10)", nullable: false, defaultValueSql: "'en-US'", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    timezone = table.Column<string>(type: "varchar(50)", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    trusteddomains = table.Column<string>(type: "varchar(1024)", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    trusteddomainsenabled = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'1'"),
                    status = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'0'"),
                    statuschanged = table.Column<DateTime>(type: "datetime", nullable: true),
                    creationdatetime = table.Column<DateTime>(type: "datetime", nullable: false),
                    owner_id = table.Column<string>(type: "varchar(38)", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    payment_id = table.Column<string>(type: "varchar(38)", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    industry = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'0'"),
                    last_modified = table.Column<DateTime>(type: "timestamp", nullable: false),
                    spam = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'"),
                    calls = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants_tenants", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "identity_clients",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    client_secret = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    logo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    website_url = table.Column<string>(type: "tinytext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    terms_url = table.Column<string>(type: "tinytext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    policy_url = table.Column<string>(type: "tinytext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    logout_redirect_uri = table.Column<string>(type: "tinytext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_public = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'"),
                    is_enabled = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'1'"),
                    is_invalidated = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'"),
                    created_on = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true),
                    created_by = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_on = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true),
                    modified_by = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.client_id);
                    table.ForeignKey(
                        name: "FK_identity_clients_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "tenants_partners",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    partner_id = table.Column<string>(type: "varchar(36)", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    affiliate_id = table.Column<string>(type: "varchar(50)", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    campaign = table.Column<string>(type: "varchar(50)", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.tenant_id);
                    table.ForeignKey(
                        name: "FK_tenants_partners_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "identity_authorizations",
                columns: table => new
                {
                    registered_client_id = table.Column<string>(type: "varchar(36)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    principal_id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authorization_grant_type = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    id = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    state = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    attributes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authorized_scopes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authorization_code_value = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authorization_code_metadata = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authorization_code_issued_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true),
                    authorization_code_expires_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true),
                    access_token_type = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    access_token_value = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    access_token_hash = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    access_token_scopes = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    access_token_metadata = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    access_token_issued_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true),
                    access_token_expires_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true),
                    refresh_token_value = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refresh_token_hash = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refresh_token_metadata = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refresh_token_issued_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true),
                    refresh_token_expires_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true),
                    is_invalidated = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'"),
                    modified_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.principal_id, x.registered_client_id, x.authorization_grant_type });
                    table.ForeignKey(
                        name: "FK_authorization_client_id",
                        column: x => x.registered_client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_identity_authorizations_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_client_allowed_origins",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    allowed_origin = table.Column<string>(type: "tinytext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "identity_client_allowed_origins_ibfk_1",
                        column: x => x.client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_client_authentication_methods",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    authentication_method = table.Column<string>(type: "enum('client_secret_post','none')", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "identity_client_authentication_methods_ibfk_1",
                        column: x => x.client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_client_redirect_uris",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    redirect_uri = table.Column<string>(type: "tinytext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "identity_client_redirect_uris_ibfk_1",
                        column: x => x.client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_client_scopes",
                columns: table => new
                {
                    client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scope_name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "identity_client_scopes_ibfk_1",
                        column: x => x.client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "identity_client_scopes_ibfk_2",
                        column: x => x.scope_name,
                        principalTable: "identity_scopes",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_consents",
                columns: table => new
                {
                    principal_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    registered_client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_invalidated = table.Column<bool>(type: "tinyint(1)", nullable: true, defaultValueSql: "'0'"),
                    modified_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.principal_id, x.registered_client_id });
                    table.ForeignKey(
                        name: "identity_consents_ibfk_1",
                        column: x => x.registered_client_id,
                        principalTable: "identity_clients",
                        principalColumn: "client_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "identity_consent_scopes",
                columns: table => new
                {
                    principal_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    registered_client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    scope_name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.principal_id, x.registered_client_id, x.scope_name });
                    table.ForeignKey(
                        name: "identity_consent_scopes_ibfk_1",
                        columns: x => new { x.principal_id, x.registered_client_id },
                        principalTable: "identity_consents",
                        principalColumns: new[] { "principal_id", "registered_client_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "identity_consent_scopes_ibfk_2",
                        column: x => x.scope_name,
                        principalTable: "identity_scopes",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "identity_scopes",
                columns: new[] { "name", "group", "type" },
                values: new object[,]
                {
                    { "accounts:read", "accounts", "read" },
                    { "accounts:write", "accounts", "write" },
                    { "accounts.self:read", "profiles", "read" },
                    { "accounts.self:write", "profiles", "write" },
                    { "files:read", "files", "read" },
                    { "files:write", "files", "write" },
                    { "openid", "openid", "openid" },
                    { "rooms:read", "rooms", "read" },
                    { "rooms:write", "rooms", "write" }
                });

            migrationBuilder.InsertData(
                table: "tenants_tenants",
                columns: new[] { "id", "alias", "creationdatetime", "last_modified", "mappeddomain", "name", "owner_id", "payment_id", "status", "statuschanged", "timezone", "trusteddomains", "version_changed" },
                values: new object[] { -1, "settings", new DateTime(2021, 3, 9, 17, 46, 59, 97, DateTimeKind.Utc).AddTicks(4317), new DateTime(2022, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Web Office", "00000000-0000-0000-0000-000000000000", null, 1, null, null, null, null });

            migrationBuilder.InsertData(
                table: "tenants_tenants",
                columns: new[] { "id", "alias", "creationdatetime", "last_modified", "mappeddomain", "name", "owner_id", "payment_id", "statuschanged", "timezone", "trusteddomains", "version_changed" },
                values: new object[] { 1, "localhost", new DateTime(2021, 3, 9, 17, 46, 59, 97, DateTimeKind.Utc).AddTicks(4317), new DateTime(2022, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Web Office", "66faa6e4-f133-11ea-b126-00ffeec8b4ef", null, null, null, null, null });

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

            migrationBuilder.CreateIndex(
                name: "IX_identity_authorizations_tenant_id",
                table: "identity_authorizations",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UK_id",
                table: "identity_authorizations",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_identity_client_allowed_origins_client_id",
                table: "identity_client_allowed_origins",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "idx_client_authentication_methods_client_id",
                table: "identity_client_authentication_methods",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_client_redirect_uris_client_id",
                table: "identity_client_redirect_uris",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_client_scopes_client_id",
                table: "identity_client_scopes",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_client_scopes_scope_name",
                table: "identity_client_scopes",
                column: "scope_name");

            migrationBuilder.CreateIndex(
                name: "idx_identity_clients_is_invalidated",
                table: "identity_clients",
                column: "is_invalidated");

            migrationBuilder.CreateIndex(
                name: "idx_identity_clients_tenant_id",
                table: "identity_clients",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_identity_clients_tenant_id",
                table: "identity_clients",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UK_client_id",
                table: "identity_clients",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_identity_consent_scopes_principal_id",
                table: "identity_consent_scopes",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consent_scopes_registered_client_id",
                table: "identity_consent_scopes",
                column: "registered_client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consent_scopes_scope_name",
                table: "identity_consent_scopes",
                column: "scope_name");

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
                name: "alias",
                table: "tenants_tenants",
                column: "alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "last_modified",
                table: "tenants_tenants",
                column: "last_modified");

            migrationBuilder.CreateIndex(
                name: "mappeddomain",
                table: "tenants_tenants",
                column: "mappeddomain");

            migrationBuilder.CreateIndex(
                name: "version",
                table: "tenants_tenants",
                column: "version");

            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_invalidated_authorization\r\nON SCHEDULE EVERY 1 hour\r\nON COMPLETION PRESERVE\r\n    DO\r\nDELETE FROM identity_authorizations ia WHERE ia.is_invalidated = 1;");
            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_invalidated_clients\r\nON SCHEDULE EVERY 1 hour\r\nON COMPLETION PRESERVE\r\n    DO\r\nDELETE FROM identity_clients ic WHERE ic.is_invalidated = 1;");
            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_invalidated_consents\r\nON SCHEDULE EVERY 1 hour\r\nON COMPLETION PRESERVE\r\n    DO\r\nDELETE FROM identity_consents ic WHERE ic.is_invalidated = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_authorizations");

            migrationBuilder.DropTable(
                name: "identity_certs");

            migrationBuilder.DropTable(
                name: "identity_client_allowed_origins");

            migrationBuilder.DropTable(
                name: "identity_client_authentication_methods");

            migrationBuilder.DropTable(
                name: "identity_client_redirect_uris");

            migrationBuilder.DropTable(
                name: "identity_client_scopes");

            migrationBuilder.DropTable(
                name: "identity_consent_scopes");

            migrationBuilder.DropTable(
                name: "identity_shedlock");

            migrationBuilder.DropTable(
                name: "tenants_partners");

            migrationBuilder.DropTable(
                name: "identity_consents");

            migrationBuilder.DropTable(
                name: "identity_scopes");

            migrationBuilder.DropTable(
                name: "identity_clients");

            migrationBuilder.DropTable(
                name: "tenants_tenants");
        }
    }
}
