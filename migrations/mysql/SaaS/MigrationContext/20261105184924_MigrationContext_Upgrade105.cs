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
            // Mirrors the Flyway scripts V1_16 and V1_17. MySQL commits DDL statement by statement, so every step is
            // guarded: with the short lock timeout below a run can fail half way, and the next one has to get through.
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS identity_pending_messages (
    id bigint NOT NULL AUTO_INCREMENT,
    exchange_name varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    routing_key varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    headers text CHARACTER SET utf8mb4 NOT NULL,
    body mediumblob NOT NULL,
    attempts int NOT NULL DEFAULT 0,
    deliver_at datetime(6) NOT NULL,
    created_at datetime(6) NOT NULL,
    PRIMARY KEY (id),
    KEY idx_identity_pending_messages_deliver_at (deliver_at)
) CHARACTER SET = utf8mb4;");

            // Fail fast rather than queue every request on these tables behind a waiting metadata lock.
            migrationBuilder.Sql("SET SESSION lock_wait_timeout = 10;");

            RunIf(migrationBuilder, ColumnExists("identity_authorizations", "purge_at") + " = 0",
                "ALTER TABLE identity_authorizations ADD COLUMN purge_at datetime(6) AS (GREATEST(COALESCE(modified_at, TIMESTAMP''1000-01-01 00:00:00''), COALESCE(authorization_code_expires_at, TIMESTAMP''1000-01-01 00:00:00''), COALESCE(access_token_expires_at, TIMESTAMP''1000-01-01 00:00:00''), COALESCE(refresh_token_expires_at, TIMESTAMP''1000-01-01 00:00:00''), COALESCE(id_token_expires_at, TIMESTAMP''1000-01-01 00:00:00''))) VIRTUAL");

            RunIf(migrationBuilder, IndexExists("identity_authorizations", "idx_identity_authorizations_purge_at") + " = 0",
                "ALTER TABLE identity_authorizations ADD INDEX idx_identity_authorizations_purge_at (purge_at), ALGORITHM=INPLACE, LOCK=NONE");

            RunIf(migrationBuilder, IndexExists("identity_consents", "idx_identity_consents_modified_at") + " = 0",
                "ALTER TABLE identity_consents ADD INDEX idx_identity_consents_modified_at (modified_at), ALGORITHM=INPLACE, LOCK=NONE");

            // Not in V1_17: the primary key here starts with principal_id, so it cannot serve the per-client deletes.
            RunIf(migrationBuilder, IndexExists("identity_consents", "idx_identity_consents_registered_client_id") + " = 0",
                "ALTER TABLE identity_consents ADD INDEX idx_identity_consents_registered_client_id (registered_client_id), ALGORITHM=INPLACE, LOCK=NONE");

            migrationBuilder.Sql("SET SESSION lock_wait_timeout = DEFAULT;");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS identity_purge_cursors (
    name varchar(32) CHARACTER SET ascii NOT NULL,
    position datetime(6) NOT NULL,
    PRIMARY KEY (name)
) CHARACTER SET = utf8mb4;");

            migrationBuilder.Sql("INSERT IGNORE INTO identity_purge_cursors (name, position) VALUES ('consents', TIMESTAMP'1000-01-01 00:00:00');");

            migrationBuilder.Sql("DROP EVENT IF EXISTS identity_purge_stale_grants;");

            migrationBuilder.Sql(@"
CREATE EVENT identity_purge_stale_grants
    ON SCHEDULE EVERY 1 HOUR
    DO
    BEGIN
        DECLARE cutoff DATETIME(6) DEFAULT UTC_TIMESTAMP(6) - INTERVAL 30 DAY;
        DECLARE origin DATETIME(6) DEFAULT TIMESTAMP'1000-01-01 00:00:00';
        DECLARE removed INT;
        DECLARE chunks INT;
        DECLARE budget INT DEFAULT 100000;
        DECLARE client VARCHAR(36) CHARACTER SET ascii;
        DECLARE resume_at DATETIME(6);
        DECLARE started DATETIME(6);
        DECLARE slice_end DATETIME(6);
        DECLARE lapped INT DEFAULT 0;
        DECLARE finished INT DEFAULT 0;
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
                WHERE purge_at < cutoff
                ORDER BY purge_at
                LIMIT 1000;
                SET removed = ROW_COUNT(), chunks = chunks + 1;
            UNTIL removed < 1000 OR chunks = 200 END REPEAT;

            DELETE FROM identity_consents
            WHERE modified_at IS NULL
              AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                              WHERE a.registered_client_id = identity_consents.registered_client_id
                                AND a.principal_id = identity_consents.principal_id)
            LIMIT 1000;

            SET resume_at = COALESCE(
                (SELECT p.position FROM identity_purge_cursors p WHERE p.name = 'consents'), origin);
            IF resume_at >= cutoff THEN
                SET resume_at = origin;
            END IF;
            SET started = resume_at, chunks = 0;
            REPEAT
                SET slice_end = (SELECT c.modified_at FROM identity_consents c
                             WHERE c.modified_at > resume_at AND c.modified_at < cutoff
                             ORDER BY c.modified_at
                             LIMIT 1 OFFSET 999);
                IF lapped = 1 AND (slice_end IS NULL OR slice_end >= started) THEN
                    SET slice_end = started, finished = 1;
                END IF;

                IF slice_end IS NULL THEN
                    DELETE FROM identity_consents
                    WHERE modified_at > resume_at AND modified_at < cutoff
                      AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                                      WHERE a.registered_client_id = identity_consents.registered_client_id
                                        AND a.principal_id = identity_consents.principal_id);
                    IF started = origin THEN
                        SET finished = 1;
                    END IF;
                    SET resume_at = origin, lapped = 1;
                ELSE
                    DELETE FROM identity_consents
                    WHERE modified_at > resume_at AND modified_at <= slice_end
                      AND NOT EXISTS (SELECT 1 FROM identity_authorizations a
                                      WHERE a.registered_client_id = identity_consents.registered_client_id
                                        AND a.principal_id = identity_consents.principal_id);
                    SET resume_at = slice_end;
                END IF;
                SET chunks = chunks + 1;
            UNTIL finished = 1 OR chunks = 100 END REPEAT;

            UPDATE identity_purge_cursors SET position = resume_at WHERE name = 'consents';

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

            migrationBuilder.Sql("DROP TABLE IF EXISTS identity_purge_cursors;");

            RunIf(migrationBuilder, IndexExists("identity_consents", "idx_identity_consents_registered_client_id") + " > 0",
                "ALTER TABLE identity_consents DROP INDEX idx_identity_consents_registered_client_id");

            RunIf(migrationBuilder, IndexExists("identity_consents", "idx_identity_consents_modified_at") + " > 0",
                "ALTER TABLE identity_consents DROP INDEX idx_identity_consents_modified_at");

            RunIf(migrationBuilder, IndexExists("identity_authorizations", "idx_identity_authorizations_purge_at") + " > 0",
                "ALTER TABLE identity_authorizations DROP INDEX idx_identity_authorizations_purge_at");

            RunIf(migrationBuilder, ColumnExists("identity_authorizations", "purge_at") + " > 0",
                "ALTER TABLE identity_authorizations DROP COLUMN purge_at");

            // An earlier revision of this migration created this index; a stand that applied it rolls back through here.
            RunIf(migrationBuilder, IndexExists("identity_authorizations", "idx_identity_authorizations_modified_at") + " > 0",
                "ALTER TABLE identity_authorizations DROP INDEX idx_identity_authorizations_modified_at");

            migrationBuilder.Sql("DROP TABLE IF EXISTS identity_pending_messages;");
        }

        private static string ColumnExists(string table, string column)
        {
            return $"(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = '{table}' AND column_name = '{column}')";
        }

        private static string IndexExists(string table, string index)
        {
            return $"(SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = '{table}' AND index_name = '{index}')";
        }

        private static void RunIf(MigrationBuilder migrationBuilder, string condition, string statement)
        {
            migrationBuilder.Sql($"SET @upgrade105_ddl = IF({condition}, '{statement}', 'DO 0');");
            migrationBuilder.Sql("PREPARE upgrade105_ddl FROM @upgrade105_ddl;");
            migrationBuilder.Sql("EXECUTE upgrade105_ddl;");
            migrationBuilder.Sql("DEALLOCATE PREPARE upgrade105_ddl;");
        }
    }
}
