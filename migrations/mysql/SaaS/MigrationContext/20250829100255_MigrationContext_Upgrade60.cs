using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade60 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "admin_notify", "cd84e66b-b803-40fc-99f9-b2969a54a1de", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "periodic_notify", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "send_whats_new", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender|telegram.sender");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "admin_notify", "cd84e66b-b803-40fc-99f9-b2969a54a1de", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "periodic_notify", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "send_whats_new", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender");
        }
    }
}
