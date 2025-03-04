using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade46 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE identity_clients
                    ADD COLUMN creator_id VARCHAR(36),
                    ADD COLUMN modifier_id VARCHAR(36);"
            );
            
            migrationBuilder.Sql(
                @"UPDATE identity_clients c
	                JOIN core_user u ON c.created_by = u.email
	                SET c.creator_id = u.id;"
            );

            migrationBuilder.Sql(
                @"UPDATE identity_clients c
	                JOIN core_user u ON c.modified_by = u.email
	                SET c.modifier_id = u.id;"
            );

            migrationBuilder.Sql(
                @"UPDATE identity_clients c
	                JOIN core_user u ON c.created_by = u.email
	                SET c.creator_id = u.id;"
            );

            migrationBuilder.Sql(
                @"UPDATE identity_clients c
	                JOIN core_user u ON c.modified_by = u.email
	                SET c.modifier_id = u.id;"
            );


            migrationBuilder.Sql(
                @"ALTER TABLE identity_clients
                    DROP COLUMN created_by,
                    DROP COLUMN modified_by;"
            );
            
            migrationBuilder.Sql(
                @"ALTER TABLE identity_clients
                    RENAME COLUMN creator_id TO created_by,
                    RENAME COLUMN modifier_id TO modified_by;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
