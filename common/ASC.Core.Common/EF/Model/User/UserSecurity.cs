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

public class UserSecurity : BaseEntity
{
    public int TenantId { get; set; }
    public Guid UserId { get; set; }
    [MaxLength(512)]
    public string PwdHash { get; set; }
    public DateTime? LastModified { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [UserId];
    }
}

public static class UserSecurityExtension
{
    public static ModelBuilderWrapper AddUserSecurity(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<UserSecurity>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddUserSecurity, Provider.MySql)
            .Add(PgSqlAddUserSecurity, Provider.PostgreSql)
            .HasData(
            new UserSecurity
            {
                TenantId = 1,
                UserId = Guid.Parse("66faa6e4-f133-11ea-b126-00ffeec8b4ef"),
                PwdHash = "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=",
                LastModified = new DateTime(2022, 7, 8, 0, 0, 0, DateTimeKind.Utc)
            });

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddUserSecurity()
        {
            modelBuilder.Entity<UserSecurity>(entity =>
            {
                entity.HasKey(e => e.UserId)
                    .HasName("PRIMARY");

                entity.ToTable("core_usersecurity")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.PwdHash)
                    .HasDatabaseName("pwdhash");

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("tenant");

                entity.Property(e => e.UserId)
                    .HasColumnName("userid")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.LastModified)
                    .HasColumnType("timestamp")
                    .IsRequired();

                entity.Property(e => e.PwdHash)
                    .HasColumnName("pwdhash")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.TenantId).HasColumnName("tenant");
            });
        }

        public void PgSqlAddUserSecurity()
        {
            modelBuilder.Entity<UserSecurity>(entity =>
            {
                // Define the primary key
                entity.HasKey(e => e.UserId)
                    .HasName("pk_usersecurity");

                // Map the table name
                entity.ToTable("core_usersecurity");

                // Define indexes
                entity.HasIndex(e => e.PwdHash)
                    .HasDatabaseName("idx_pwdhash");

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("idx_tenant");

                // Map the columns and types
                entity.Property(e => e.UserId)
                    .HasColumnName("userid") // Column name in the database
                    .HasColumnType("uuid"); // PostgreSQL uses "uuid" for GUIDs

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant");

                entity.Property(e => e.PwdHash)
                    .HasColumnName("pwdhash")
                    .HasColumnType("varchar");

                entity.Property(e => e.LastModified)
                    .HasColumnName("lastmodified")
                    .HasColumnType("timestamptz")
                    .IsRequired(false); // LastModified can be null
            });
        }
    }
}