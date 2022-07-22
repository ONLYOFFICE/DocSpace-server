// <auto-generated />
using ASC.Core.Common.EF.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ASC.Core.Common.Migrations.PostgreSql.UrlShortenerFakeDbContextPostgreSql
{
    [DbContext(typeof(PostgreSqlUrlShortenerFakeDbContext))]
    partial class PostgreSqlUrlShortenerFakeDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "6.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("ASC.Core.Common.EF.Model.ShortLinks", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int(10)")
                        .HasColumnName("id")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Link")
                        .HasColumnType("text")
                        .HasColumnName("link")
                        .UseCollation("utf8_bin");

                    b.Property<string>("Short")
                        .HasColumnType("varchar(12)")
                        .HasColumnName("short")
                        .UseCollation("utf8_general_ci")
                        .HasAnnotation("MySql:CharSet", "utf8");

                    b.HasKey("Id")
                        .HasName("PRIMARY");

                    b.HasIndex("Short")
                        .IsUnique();

                    b.ToTable("short_links", (string)null);

                    b
                        .HasAnnotation("MySql:CharSet", "utf8")
                        .HasAnnotation("Relational:Collation", "utf8_general_ci");
                });
#pragma warning restore 612, 618
        }
    }
}
