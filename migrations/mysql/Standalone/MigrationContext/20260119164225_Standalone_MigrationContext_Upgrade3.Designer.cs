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
    [Migration("20260119164225_Standalone_MigrationContext_Upgrade3")]
    partial class MigrationContext_Upgrade3
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .HasAnnotation("ProductVersion", "9.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("ASC.Core.Common.EF.DbQuota", b =>
            {
                b.Property<int>("TenantId")
                    .HasColumnType("int")
                    .HasColumnName("tenant");

                b.Property<string>("Description")
                    .HasMaxLength(128)
                    .HasColumnType("varchar")
                    .HasColumnName("description")
                    .UseCollation("utf8_general_ci")
                    .HasAnnotation("MySql:CharSet", "utf8");

                b.Property<string>("Features")
                    .HasColumnType("text")
                    .HasColumnName("features");

                b.Property<string>("Name")
                    .HasMaxLength(128)
                    .HasColumnType("varchar")
                    .HasColumnName("name")
                    .UseCollation("utf8_general_ci")
                    .HasAnnotation("MySql:CharSet", "utf8");

                b.Property<decimal>("Price")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("decimal(10,4)")
                    .HasColumnName("price")
                    .HasDefaultValueSql("'0.00'");

                b.Property<string>("ProductId")
                    .HasMaxLength(128)
                    .HasColumnType("varchar")
                    .HasColumnName("product_id")
                    .UseCollation("utf8_general_ci")
                    .HasAnnotation("MySql:CharSet", "utf8");

                b.Property<string>("ServiceGroup")
                    .HasMaxLength(128)
                    .HasColumnType("varchar")
                    .HasColumnName("service_group")
                    .UseCollation("utf8_general_ci")
                    .HasAnnotation("MySql:CharSet", "utf8");

                b.Property<string>("ServiceName")
                    .HasMaxLength(128)
                    .HasColumnType("varchar")
                    .HasColumnName("service_name")
                    .UseCollation("utf8_general_ci")
                    .HasAnnotation("MySql:CharSet", "utf8");

                b.Property<bool>("Visible")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("tinyint(1)")
                    .HasColumnName("visible")
                    .HasDefaultValueSql("'0'");

                b.Property<bool>("Wallet")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("tinyint(1)")
                    .HasColumnName("wallet")
                    .HasDefaultValueSql("'0'");

                b.HasKey("TenantId")
                    .HasName("PRIMARY");

                b.ToTable("tenants_quota", (string)null);

                b.HasAnnotation("MySql:CharSet", "utf8");
            });

#pragma warning restore 612, 618
        }
    }
}