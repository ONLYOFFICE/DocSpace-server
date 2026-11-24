// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Core.Common.EF;

public class User : BaseEntity
{
    public int TenantId { get; set; }

    [MaxLength(255)]
    public string UserName { get; set; }

    [MaxLength(64)]
    public string FirstName { get; set; }

    [MaxLength(64)]
    public string LastName { get; set; }
    public Guid Id { get; set; }
    public bool? Sex { get; set; }
    public DateTime? BirthDate { get; set; }
    public EmployeeStatus Status { get; set; }
    public EmployeeActivationStatus ActivationStatus { get; set; }

    [MaxLength(255)]
    public string Email { get; set; }
    public DateTime? WorkFromDate { get; set; }
    public DateTime? TerminatedDate { get; set; }

    [MaxLength(64)]
    public string Title { get; set; }

    [MaxLength(20)]
    public string CultureName { get; set; }

    [MaxLength(1024)]
    public string Contacts { get; set; }

    [MaxLength(255)]
    public string MobilePhone { get; set; }
    public MobilePhoneActivationStatus MobilePhoneActivation { get; set; }

    [MaxLength(255)]
    public string Location { get; set; }

    [MaxLength(512)]
    public string Notes { get; set; }

    [MaxLength(512)]
    public string Sid { get; set; }

    [MaxLength(512)]
    public string SsoNameId { get; set; }

    [MaxLength(512)]
    public string SsoSessionId { get; set; }
    public bool Removed { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime LastModified { get; set; }
    public Guid? CreatedBy { get; set; }
    public bool? Spam { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbUserExtension
{
    public static ModelBuilderWrapper AddUser(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<User>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddUser, Provider.MySql)
            .Add(PgSqlAddUser, Provider.PostgreSql)
            .HasData(
            new User
            {
                Id = Guid.Parse("66faa6e4-f133-11ea-b126-00ffeec8b4ef"),
                FirstName = "Administrator",
                LastName = "",
                UserName = "administrator",
                TenantId = 1,
                Email = "",
                Status = (EmployeeStatus)1,
                ActivationStatus = 0,
                WorkFromDate = new DateTime(2021, 3, 9, 9, 52, 55, 764, DateTimeKind.Utc).AddTicks(9157),
                LastModified = new DateTime(2021, 3, 9, 9, 52, 55, 765, DateTimeKind.Utc).AddTicks(1420),
                CreateDate = new DateTime(2022, 7, 8, 0, 0, 0, DateTimeKind.Utc)
            });

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        private void MySqlAddUser()
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("core_user")
                    .HasCharSet("utf8");

                entity.HasKey(e => e.Id)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.Email)
                    .HasDatabaseName("email");

                entity.HasIndex(e => e.LastModified)
                    .HasDatabaseName("last_modified");

                entity.HasIndex(e => new { e.TenantId, e.UserName })
                    .HasDatabaseName("username");

                entity.HasIndex(e => new { e.TenantId, e.ActivationStatus, e.FirstName })
                    .HasDatabaseName("tenant_activation_status_firstname");

                entity.HasIndex(e => new { e.TenantId, e.ActivationStatus, e.LastName })
                    .HasDatabaseName("tenant_activation_status_lastname");

                entity.HasIndex(e => new { e.TenantId, e.ActivationStatus, e.Email })
                    .HasDatabaseName("tenant_activation_status_email");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ActivationStatus)
                    .HasColumnName("activation_status")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.BirthDate)
                    .HasColumnName("bithdate")
                    .HasColumnType("datetime");

                entity.Property(e => e.Contacts)
                    .HasColumnName("contacts")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.CreateDate)
                    .HasColumnName("create_on")
                    .HasColumnType("timestamp");

                entity.Property(e => e.CultureName)
                    .HasColumnName("culture")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");


                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasColumnName("firstname")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("datetime");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasColumnName("lastname")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Location)
                    .HasColumnName("location")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Notes)
                    .HasColumnName("notes")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.MobilePhone)
                    .HasColumnName("phone")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.MobilePhoneActivation)
                    .HasColumnName("phone_activation")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Removed)
                    .HasColumnName("removed")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Sex)
                    .HasColumnName("sex")
                    .HasColumnType("tinyint(1)");

                entity.Property(e => e.Sid)
                    .HasColumnName("sid")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.SsoNameId)
                    .HasColumnName("sso_name_id")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.SsoSessionId)
                    .HasColumnName("sso_session_id")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.TenantId).HasColumnName("tenant");

                entity.Property(e => e.TerminatedDate)
                    .HasColumnName("terminateddate")
                    .HasColumnType("datetime");

                entity.Property(e => e.Title)
                    .HasColumnName("title")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasColumnName("username")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.WorkFromDate)
                    .HasColumnName("workfromdate")
                    .HasColumnType("datetime");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("created_by")
                    .HasColumnType("varchar(36)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Spam)
                    .HasColumnName("spam")
                    .HasColumnType("tinyint(1)");
            });
        }

        private void PgSqlAddUser()
        {
            // Same columns and indexes as the MySQL section, so unquoted SQL names the same core_user columns on both;
            // string lengths come from the [MaxLength] attributes. Index names carry the table name, since
            // PostgreSQL scopes them to the schema.
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("core_user");

                entity.HasKey(e => e.Id)
                    .HasName("pk_core_user");

                entity.HasIndex(e => e.Email)
                    .HasDatabaseName("core_user_email");

                entity.HasIndex(e => e.LastModified)
                    .HasDatabaseName("core_user_last_modified");

                entity.HasIndex(e => new { e.TenantId, e.UserName })
                    .HasDatabaseName("core_user_username");

                entity.HasIndex(e => new { e.TenantId, e.ActivationStatus, e.FirstName })
                    .HasDatabaseName("core_user_tenant_activation_status_firstname");

                entity.HasIndex(e => new { e.TenantId, e.ActivationStatus, e.LastName })
                    .HasDatabaseName("core_user_tenant_activation_status_lastname");

                entity.HasIndex(e => new { e.TenantId, e.ActivationStatus, e.Email })
                    .HasDatabaseName("core_user_tenant_activation_status_email");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid");

                entity.Property(e => e.ActivationStatus)
                    .HasColumnName("activation_status")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.BirthDate)
                    .HasColumnName("bithdate")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.Contacts)
                    .HasColumnName("contacts");

                entity.Property(e => e.CreateDate)
                    .HasColumnName("create_on")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.CultureName)
                    .HasColumnName("culture");

                entity.Property(e => e.Email)
                    .HasColumnName("email");

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasColumnName("firstname");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasColumnName("lastname");

                entity.Property(e => e.Location)
                    .HasColumnName("location");

                entity.Property(e => e.Notes)
                    .HasColumnName("notes");

                entity.Property(e => e.MobilePhone)
                    .HasColumnName("phone");

                entity.Property(e => e.MobilePhoneActivation)
                    .HasColumnName("phone_activation")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Removed)
                    .HasColumnName("removed")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.Sex)
                    .HasColumnName("sex");

                entity.Property(e => e.Sid)
                    .HasColumnName("sid");

                entity.Property(e => e.SsoNameId)
                    .HasColumnName("sso_name_id");

                entity.Property(e => e.SsoSessionId)
                    .HasColumnName("sso_session_id");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasDefaultValueSql("1");

                entity.Property(e => e.TenantId).HasColumnName("tenant");

                entity.Property(e => e.TerminatedDate)
                    .HasColumnName("terminateddate")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.Title)
                    .HasColumnName("title");

                entity.Property(e => e.UserName)
                    .IsRequired()
                    .HasColumnName("username");

                entity.Property(e => e.WorkFromDate)
                    .HasColumnName("workfromdate")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.CreatedBy)
                    .HasColumnName("created_by")
                    .HasColumnType("uuid");

                entity.Property(e => e.Spam)
                    .HasColumnName("spam");

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}