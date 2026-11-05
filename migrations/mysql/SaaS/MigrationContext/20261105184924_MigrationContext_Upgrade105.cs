using System;
using Microsoft.EntityFrameworkCore.Metadata;
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
            migrationBuilder.CreateTable(
                name: "identity_pending_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    exchange_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    routing_key = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    headers = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body = table.Column<byte[]>(type: "mediumblob", nullable: false),
                    attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    deliver_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", maxLength: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Fail fast rather than queue every request on these tables behind a waiting metadata lock.
            migrationBuilder.Sql("SET SESSION lock_wait_timeout = 10;");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_modified_at",
                table: "identity_consents",
                column: "modified_at");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_registered_client_id",
                table: "identity_consents",
                column: "registered_client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_modified_at",
                table: "identity_authorizations",
                column: "modified_at");

            migrationBuilder.CreateIndex(
                name: "idx_identity_pending_messages_deliver_at",
                table: "identity_pending_messages",
                column: "deliver_at");

            migrationBuilder.Sql("SET SESSION lock_wait_timeout = DEFAULT;");

            migrationBuilder.Sql("DROP EVENT IF EXISTS identity_purge_stale_grants;");

            // The cursor variable is ascii (client ids are GUIDs) because a plain varchar takes the database default
            // collation: with a database default other than the columns' collation, utf8mb4_general_ci for one, comparing
            // it to registered_client_id fails with error 1267 and the event would never remove anything.
            migrationBuilder.Sql(@"
CREATE EVENT identity_purge_stale_grants
    ON SCHEDULE EVERY 10 MINUTE
    DO
    BEGIN
        DECLARE cutoff DATETIME(6) DEFAULT UTC_TIMESTAMP(6) - INTERVAL 30 DAY;
        DECLARE removed INT;
        DECLARE chunks INT;
        DECLARE budget INT DEFAULT 100000;
        DECLARE client VARCHAR(36) CHARACTER SET ascii;
        DECLARE done INT DEFAULT 0;
        DECLARE invalidated CURSOR FOR SELECT client_id FROM identity_clients WHERE is_invalidated = 1;
        DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

        SET SESSION TRANSACTION ISOLATION LEVEL READ COMMITTED;

        IF GET_LOCK('identity_purge_stale_grants', 0) = 1 THEN
            OPEN invalidated;
            clients: LOOP
                FETCH invalidated INTO client;
                IF done = 1 OR budget <= 0 THEN
                    LEAVE clients;
                END IF;
                REPEAT
                    DELETE FROM identity_authorizations WHERE registered_client_id = client LIMIT 1000;
                    SET removed = ROW_COUNT(), budget = budget - removed;
                UNTIL removed < 1000 OR budget <= 0 END REPEAT;
                REPEAT
                    DELETE FROM identity_consents WHERE registered_client_id = client LIMIT 1000;
                    SET removed = ROW_COUNT(), budget = budget - removed;
                UNTIL removed < 1000 OR budget <= 0 END REPEAT;
            END LOOP;
            CLOSE invalidated;

            DELETE FROM identity_clients
            WHERE is_invalidated = 1
              AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                              WHERE a.registered_client_id = identity_clients.client_id)
              AND NOT EXISTS (SELECT 1 FROM identity_consents c
                              WHERE c.registered_client_id = identity_clients.client_id);

            SET chunks = 0;
            REPEAT
                DELETE FROM identity_authorizations
                WHERE (modified_at IS NULL OR modified_at < cutoff)
                  AND GREATEST(
                        COALESCE(authorization_code_expires_at, '1000-01-01'),
                        COALESCE(access_token_expires_at, '1000-01-01'),
                        COALESCE(refresh_token_expires_at, '1000-01-01'),
                        COALESCE(id_token_expires_at, '1000-01-01')) < cutoff
                ORDER BY modified_at
                LIMIT 1000;
                SET removed = ROW_COUNT(), chunks = chunks + 1;
            UNTIL removed < 1000 OR chunks = 50 END REPEAT;

            SET chunks = 0;
            REPEAT
                DELETE FROM identity_consents
                WHERE (modified_at IS NULL OR modified_at < cutoff)
                  AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                                  WHERE a.registered_client_id = identity_consents.registered_client_id
                                    AND a.principal_id = identity_consents.principal_id)
                ORDER BY modified_at
                LIMIT 1000;
                SET removed = ROW_COUNT(), chunks = chunks + 1;
            UNTIL removed < 1000 OR chunks = 50 END REPEAT;

            DO RELEASE_LOCK('identity_purge_stale_grants');
        END IF;
    END;");

            migrationBuilder.Sql("DROP EVENT IF EXISTS identity_delete_old_authorizations;");
            migrationBuilder.Sql("DROP EVENT IF EXISTS identity_delete_old_consents;");
            migrationBuilder.Sql("DROP EVENT IF EXISTS identity_delete_invalidated_clients;");
            migrationBuilder.Sql("DROP EVENT IF EXISTS identity_delete_invalidated_consents;");
            migrationBuilder.Sql("DROP EVENT IF EXISTS identity_delete_invalidated_authorization;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP EVENT IF EXISTS identity_purge_stale_grants;");

            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_invalidated_authorization\r\nON SCHEDULE EVERY 1 hour\r\nON COMPLETION PRESERVE\r\n    DO\r\nDELETE FROM identity_authorizations ia WHERE ia.is_invalidated = 1;");
            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_invalidated_clients\r\nON SCHEDULE EVERY 1 hour\r\nON COMPLETION PRESERVE\r\n    DO\r\nDELETE FROM identity_clients ic WHERE ic.is_invalidated = 1;");
            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_invalidated_consents\r\nON SCHEDULE EVERY 1 hour\r\nON COMPLETION PRESERVE\r\n    DO\r\nDELETE FROM identity_consents ic WHERE ic.is_invalidated = 1;");
            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_old_authorizations\r\nON SCHEDULE EVERY 1 DAY\r\nON COMPLETION PRESERVE\r\nDO\r\nDELETE FROM identity_authorizations\r\nWHERE modified_at < NOW() - INTERVAL 30 DAY;");
            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_old_consents\r\nON SCHEDULE EVERY 1 DAY\r\nON COMPLETION PRESERVE\r\nDO\r\nDELETE FROM identity_consents\r\nWHERE modified_at < NOW() - INTERVAL 30 DAY;");

            migrationBuilder.DropTable(
                name: "identity_pending_messages");

            migrationBuilder.DropIndex(
                name: "idx_identity_consents_modified_at",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_consents_registered_client_id",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_modified_at",
                table: "identity_authorizations");
        }
    }
}
