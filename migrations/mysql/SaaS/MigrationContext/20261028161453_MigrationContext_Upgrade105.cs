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
            // Rows still on the schedule of this version, or leased, are left alone. No index serves the predicate, so the
            // statement reads the whole journal once; it changes only the rows the previous sender never got to, a few
            // at most, so the undo log stays small, and it runs during the update, while the services are stopped.
            // PostgreSQL: this branch carries the webhook migrations for MySQL SaaS only. When the PostgreSQL migration
            // (and any other flavour) is generated or squashed from the model, it gets the creation_time index from the
            // model but NOT this UPDATE, which is hand-written: carry it over by hand, with the same note text, or the old
            // rows there stay unmarked. The manual retry also refuses an old-format body by its shape
            // (DbWorker.IsPreviousVersionBody), but the journal would still show those rows as never sent with no note.
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
