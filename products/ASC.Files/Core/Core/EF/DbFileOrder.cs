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

namespace ASC.Files.Core.EF;

public class DbFileOrder
{
    public int TenantId { get; set; }
    public int ParentFolderId { get; set; }
    public int EntryId { get; set; }
    public FileEntryType EntryType { get; set; }
    public int Order { get; set; }

    public DbTenant Tenant { get; set; }
}

public static class DbFileOrderExtension
{
    public static ModelBuilderWrapper AddDbFileOrder(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFileOrder>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFileOrder, Provider.MySql)
            .Add(PgSqlAddDbFileOrder, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFileOrder()
        {
            modelBuilder.Entity<DbFileOrder>(entity =>
            {
                entity.ToTable("files_order")
                    .HasCharSet("utf8");

                entity.HasKey(e => new { e.TenantId, e.EntryId, e.EntryType })
                    .HasName("primary");

                entity.HasIndex(e => new { e.TenantId, e.ParentFolderId, e.EntryType })
                    .HasDatabaseName("parent_folder_id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.EntryId).HasColumnName("entry_id");

                entity.Property(e => e.EntryType)
                    .HasColumnName("entry_type")
                    .HasColumnType("tinyint");

                entity.Property(e => e.ParentFolderId).HasColumnName("parent_folder_id");

                entity.Property(e => e.Order).HasColumnName("order");
            });
        }

        public void PgSqlAddDbFileOrder()
        {
            modelBuilder.Entity<DbFileOrder>(entity =>
            {
                entity.ToTable("files_order");

                entity.HasKey(e => new { e.TenantId, e.EntryId, e.EntryType })
                    .HasName("PK_files_order");

                entity.HasIndex(e => new { e.TenantId, e.ParentFolderId, e.EntryType })
                    .HasDatabaseName("parent_folder_id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.EntryId).HasColumnName("entry_id");

                entity.Property(e => e.EntryType)
                    .HasColumnName("entry_type")
                    .HasColumnType("smallint");

                entity.Property(e => e.ParentFolderId).HasColumnName("parent_folder_id");

                entity.Property(e => e.Order).HasColumnName("order");
            });
        }
    }
}