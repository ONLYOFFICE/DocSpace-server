using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade40 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE webstudio_settings
                SET Data=REPLACE(Data, '""UsersType"":""Collaborator""', '""UsersType"":""User""')
                WHERE ID='197149b3-fbc9-44c2-b42a-232f7e729c16';"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE webstudio_settings
                SET Data=REPLACE(Data, '""UsersType"":""User""', '""UsersType"":""Collaborator""')
                WHERE ID='197149b3-fbc9-44c2-b42a-232f7e729c16';"
            );
        }
    }
}
