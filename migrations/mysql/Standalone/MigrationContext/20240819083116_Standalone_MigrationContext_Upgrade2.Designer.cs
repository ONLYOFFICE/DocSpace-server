using System;
using ASC.Core.Common.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ASC.Migrations.MySql.Migrations.CoreDb
{
    [DbContext(typeof(MigrationContext))]
    [Migration("20240819083116_Standalone_MigrationContext_Upgrade2")]
    partial class MigrationContext_Upgrade2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("ASC.Core.Common.EF.DbQuota", b =>
            {
                b.Property<int>("Tenant")
                    .HasColumnType("int")
                    .HasColumnName("tenant");

                b.Property<string>("Description")
                    .HasColumnType("varchar(128)")
                    .HasColumnName("description")
                    .UseCollation("utf8_general_ci")
                    .HasAnnotation("MySql:CharSet", "utf8");

                b.Property<string>("Features")
                    .HasColumnType("text")
                    .HasColumnName("features");

                b.Property<string>("Name")
                    .HasColumnType("varchar(128)")
                    .HasColumnName("name")
                    .UseCollation("utf8_general_ci")
                    .HasAnnotation("MySql:CharSet", "utf8");

                b.Property<decimal>("Price")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("decimal(10,2)")
                    .HasColumnName("price")
                    .HasDefaultValueSql("'0.00'");

                b.Property<string>("ProductId")
                    .HasColumnType("varchar(128)")
                    .HasColumnName("product_id")
                    .UseCollation("utf8_general_ci")
                    .HasAnnotation("MySql:CharSet", "utf8");

                b.Property<bool>("Visible")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("tinyint(1)")
                    .HasColumnName("visible")
                    .HasDefaultValueSql("'0'");

                b.HasKey("Tenant")
                    .HasName("PRIMARY");

                b.ToTable("tenants_quota", (string)null);

                b.HasAnnotation("MySql:CharSet", "utf8");

                b.HasData(
                    new
                    {
                        Tenant = -1,
                        Features = "audit,ldap,sso,thirdparty,restore,oauth,contentsearch,file_size:102400,statistic",
                        Name = "default",
                        Price = 0m,
                        Visible = false
                    });
            });
        }
    }
}