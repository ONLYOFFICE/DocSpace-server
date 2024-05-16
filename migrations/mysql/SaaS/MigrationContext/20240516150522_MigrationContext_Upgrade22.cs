using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "files_audit_reference",
                columns: table => new
                {
                    entry_id = table.Column<int>(type: "int", nullable: false),
                    entry_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    audit_event_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.entry_id, x.entry_type, x.audit_event_id });
                    table.ForeignKey(
                        name: "FK_files_audit_reference_audit_events_audit_event_id",
                        column: x => x.audit_event_id,
                        principalTable: "audit_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_files_audit_reference_audit_event_id",
                table: "files_audit_reference",
                column: "audit_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_audit_reference_entry_id_entry_type",
                table: "files_audit_reference",
                columns: new[] { "entry_id", "entry_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files_audit_reference");
        }
    }
}
