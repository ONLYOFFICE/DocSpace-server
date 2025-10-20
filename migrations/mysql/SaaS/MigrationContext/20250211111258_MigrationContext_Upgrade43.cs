using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade43 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "id_token_claims",
                table: "identity_authorizations",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "id_token_expires_at",
                table: "identity_authorizations",
                type: "datetime(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "id_token_issued_at",
                table: "identity_authorizations",
                type: "datetime(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_token_metadata",
                table: "identity_authorizations",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "id_token_value",
                table: "identity_authorizations",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "id_token_claims",
                table: "identity_authorizations");

            migrationBuilder.DropColumn(
                name: "id_token_expires_at",
                table: "identity_authorizations");

            migrationBuilder.DropColumn(
                name: "id_token_issued_at",
                table: "identity_authorizations");

            migrationBuilder.DropColumn(
                name: "id_token_metadata",
                table: "identity_authorizations");

            migrationBuilder.DropColumn(
                name: "id_token_value",
                table: "identity_authorizations");
        }
    }
}