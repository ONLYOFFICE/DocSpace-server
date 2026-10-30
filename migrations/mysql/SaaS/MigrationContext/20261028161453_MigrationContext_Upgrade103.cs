using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade103 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempts",
                table: "webhooks_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "lease_id",
                table: "webhooks_logs",
                type: "varchar(36)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_on",
                table: "webhooks_logs",
                type: "datetime",
                nullable: true);

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
                name: "next_attempt_on",
                table: "webhooks_logs",
                column: "next_attempt_on");

            migrationBuilder.CreateIndex(
                name: "tenant_id_retry_of",
                table: "webhooks_logs",
                columns: new[] { "tenant_id", "retry_of" });

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
            // at most, so the undo log stays small.
            // Publishers of the previous version keep inserting into the journal during a rolling update. Under the
            // default REPEATABLE READ a scan without an index takes next-key locks on every row it reads, the gap after
            // the last one included, and holds them to the end of the statement: every insert would wait for the whole
            // scan. READ COMMITTED takes no gap locks and lets go of each row that does not match as soon as it is read,
            // so only the few marked rows stay locked, and none of them is one a publisher writes. The level is set for
            // the session (the DDL above has committed whatever transaction was open, so the UPDATE starts a
            // new one under it) and put back as it was right after. READ COMMITTED needs row-based binary logging
            // (binlog_format ROW, the default, or MIXED); a server that logs by statement refuses the UPDATE.
            // PostgreSQL: this branch carries the webhook migrations for MySQL SaaS only. When the PostgreSQL migration
            // (and any other flavour) is generated or squashed from the model, it gets the creation_time index from the
            // model but NOT this UPDATE, which is hand-written: carry it over by hand, with the same note text, or the old
            // rows there stay unmarked. The manual retry also refuses an old-format body by its shape
            // (DbWorker.IsPreviousVersionBody), but the journal would still show those rows as never sent with no note.
            // PostgreSQL takes no gap locks at any level, so the isolation statements are MySQL only.
            migrationBuilder.Sql("SET @upgrade103_isolation = @@SESSION.transaction_isolation");
            migrationBuilder.Sql("SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED");
            migrationBuilder.Sql(
                "UPDATE webhooks_logs SET response_payload = 'Not sent: the event was published by a service of the previous version, in the old payload format' " +
                "WHERE retry_of IS NULL AND attempts = 0 AND next_attempt_on IS NULL AND lease_id IS NULL " +
                "AND request_headers IS NULL AND response_payload IS NULL");
            migrationBuilder.Sql("SET SESSION transaction_isolation = @upgrade103_isolation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "creation_time",
                table: "webhooks_logs");

            migrationBuilder.DropIndex(
                name: "tenant_id_retry_of",
                table: "webhooks_logs");

            migrationBuilder.DropIndex(
                name: "next_attempt_on",
                table: "webhooks_logs");

            migrationBuilder.DropColumn(
                name: "attempts",
                table: "webhooks_logs");

            migrationBuilder.DropColumn(
                name: "lease_id",
                table: "webhooks_logs");

            migrationBuilder.DropColumn(
                name: "next_attempt_on",
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
