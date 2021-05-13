﻿// <auto-generated />
using System;
using ASC.Core.Common.EF.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ASC.Core.Common.Migrations.MySql.AccountLinkContextMySql
{
    [DbContext(typeof(MySqlAccountLinkContext))]
    [Migration("20210309093101_AccountLinkContextMySql")]
    partial class AccountLinkContextMySql
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 64)
                .HasAnnotation("ProductVersion", "5.0.3");

            modelBuilder.Entity("ASC.Core.Common.EF.Model.AccountLinks", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("varchar(200)")
                        .HasColumnName("id")
                        .UseCollation("utf8_general_ci")
                        .HasCharSet("utf8");

                    b.Property<string>("UId")
                        .HasColumnType("varchar(200)")
                        .HasColumnName("uid")
                        .UseCollation("utf8_general_ci")
                        .HasCharSet("utf8");

                    b.Property<DateTime>("Linked")
                        .HasColumnType("datetime")
                        .HasColumnName("linked");

                    b.Property<string>("Profile")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("profile")
                        .UseCollation("utf8_general_ci")
                        .HasCharSet("utf8");

                    b.Property<string>("Provider")
                        .HasColumnType("char(60)")
                        .HasColumnName("provider")
                        .UseCollation("utf8_general_ci")
                        .HasCharSet("utf8");

                    b.HasKey("Id", "UId")
                        .HasName("PRIMARY");

                    b.HasIndex("UId")
                        .HasDatabaseName("uid");

                    b.ToTable("account_links");
                });
#pragma warning restore 612, 618
        }
    }
}
