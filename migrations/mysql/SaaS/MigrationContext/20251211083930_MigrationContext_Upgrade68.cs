using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade68 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                WITH cte AS (
                    SELECT 
                        id,
                        ROW_NUMBER() OVER (
                            PARTITION BY user_id, tenant_id, firebase_device_token, application
                            ORDER BY id DESC
                        ) AS rn
                    FROM firebase_users
                )
                DELETE f
                FROM firebase_users f
                JOIN cte ON cte.id = f.id
                WHERE cte.rn > 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
