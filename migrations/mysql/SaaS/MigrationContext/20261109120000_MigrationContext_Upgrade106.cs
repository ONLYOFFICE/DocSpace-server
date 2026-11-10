using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade106 : Migration
    {
        // Mirrors the Flyway script V1_9_1: MySQL Group Replication refuses writes to a table with no primary key, and
        // these four join tables have none. The key is INVISIBLE so SELECT * and column-less INSERTs keep their shape;
        // the EF model stays keyless for the same reason, so this migration changes no snapshot.
        private static readonly string[] _tables =
        [
            "identity_client_authentication_methods",
            "identity_client_redirect_uris",
            "identity_client_allowed_origins",
            "identity_client_scopes"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fail fast rather than queue every request on these tables behind a waiting metadata lock.
            migrationBuilder.Sql("SET SESSION lock_wait_timeout = 10;");

            // Guarded per table on any primary key, not on the column: MySQL commits DDL statement by statement; my_row_id
            // is already there where Flyway ran V1_9_1 or the server generated invisible primary keys at CREATE
            // (sql_generate_invisible_primary_keys); and on Standalone, Standalone_MigrationContext_Upgrade5 (V1_15)
            // gives these tables a composite key, a second one would fail with "Multiple primary key defined".
            foreach (var table in _tables)
            {
                RunIf(migrationBuilder, PrimaryKeyExists(table) + " = 0",
                    $"ALTER TABLE {table} ADD COLUMN my_row_id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT INVISIBLE PRIMARY KEY FIRST");
            }

            migrationBuilder.Sql("SET SESSION lock_wait_timeout = DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in _tables)
            {
                RunIf(migrationBuilder, ColumnExists(table, "my_row_id") + " > 0",
                    $"ALTER TABLE {table} DROP COLUMN my_row_id");
            }
        }

        private static string ColumnExists(string table, string column)
        {
            return $"(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = '{table}' AND column_name = '{column}')";
        }

        private static string PrimaryKeyExists(string table)
        {
            return $"(SELECT COUNT(*) FROM information_schema.table_constraints WHERE table_schema = DATABASE() AND table_name = '{table}' AND constraint_type = 'PRIMARY KEY')";
        }

        private static void RunIf(MigrationBuilder migrationBuilder, string condition, string statement)
        {
            migrationBuilder.Sql($"SET @upgrade106_ddl = IF({condition}, '{statement}', 'DO 0');");
            migrationBuilder.Sql("PREPARE upgrade106_ddl FROM @upgrade106_ddl;");
            migrationBuilder.Sql("EXECUTE upgrade106_ddl;");
            migrationBuilder.Sql("DEALLOCATE PREPARE upgrade106_ddl;");
        }
    }
}
