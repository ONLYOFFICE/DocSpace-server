using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade105 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "creation_time",
                table: "webhooks_logs",
                column: "creation_time");

            // Every row written before this update is in the old payload format, which carried whole REST objects (the
            // password of a shared link among them), and must never be sent, manually retried included. A row the
            // previous sender processed is recognised by its request headers; one it never got to (its in-memory
            // queue was lost with the update) looks like a row of this version that was stopped before any attempt,
            // so it gets the note that ASC.Webhooks.Core.DbWorker.PreviousVersionNote holds; keep the two texts equal.
            // Rows still on the schedule of this version, or leased, are left alone.
            migrationBuilder.Sql(
                "UPDATE webhooks_logs SET response_payload = 'Not sent: the event was published by a service of the previous version, in the old payload format' " +
                "WHERE retry_of IS NULL AND attempts = 0 AND next_attempt_on IS NULL AND lease_id IS NULL " +
                "AND request_headers IS NULL AND response_payload IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "creation_time",
                table: "webhooks_logs");
        }
    }
}
