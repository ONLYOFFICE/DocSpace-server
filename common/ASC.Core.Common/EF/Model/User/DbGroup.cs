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

public class DbGroup : BaseEntity
{
    public int TenantId { get; set; }
    public Guid Id { get; set; }
    [MaxLength(128)]
    public string Name { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ParentId { get; set; }
    [MaxLength(512)]
    public string Sid { get; set; }
    public bool Removed { get; set; }
    public DateTime LastModified { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbGroupExtension
{
    public static ModelBuilderWrapper AddDbGroup(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbGroup>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbGroup, Provider.MySql)
            .Add(PgSqlAddDbGroup, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        private void MySqlAddDbGroup()
        {
            modelBuilder.Entity<DbGroup>(entity =>
            {
                entity.ToTable("core_group")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.LastModified)
                    .HasDatabaseName("last_modified");

                entity.HasIndex(e => new { e.TenantId, e.ParentId })
                    .HasDatabaseName("parentid");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.CategoryId)
                    .HasColumnName("categoryid")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamp");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ParentId)
                    .HasColumnName("parentid")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Removed)
                    .HasColumnName("removed")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Sid)
                    .HasColumnName("sid")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.TenantId).HasColumnName("tenant");
            });
        }

        private void PgSqlAddDbGroup()
        {
            modelBuilder.Entity<DbGroup>(entity =>
            {
                entity.ToTable("core_group");

                entity.HasIndex(e => e.LastModified)
                    .HasDatabaseName("ix_last_modified");

                entity.HasIndex(e => new { e.TenantId, e.ParentId })
                    .HasDatabaseName("ix_parentid");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid");

                entity.Property(e => e.CategoryId)
                    .HasColumnName("categoryid")
                    .HasColumnType("uuid");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(128)");

                entity.Property(e => e.ParentId)
                    .HasColumnName("parentid")
                    .HasColumnType("uuid");

                entity.Property(e => e.Removed)
                    .HasColumnName("removed")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);

                entity.Property(e => e.Sid)
                    .HasColumnName("sid")
                    .HasColumnType("varchar(512)");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant")
                    .HasColumnType("integer");
            });
        }
    }
}