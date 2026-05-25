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
public class DbFilesProperties : BaseEntity
{
    public int TenantId { get; set; }
    [MaxLength(32)]
    public string EntryId { get; set; }
    public string Data { get; set; }

    public DbTenant Tenant { get; set; }

    public bool? StartFilling { get; private set; }
    public override object[] GetKeys()
    {
        return [TenantId, EntryId];
    }
}

public static class DbFilesPropertiesExtension
{
    public static ModelBuilderWrapper AddDbFilesProperties(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesProperties>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlDbFilesProperties, Provider.MySql)
            .Add(PgSqlDbFilesProperties, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlDbFilesProperties()
        {
            modelBuilder.Entity<DbFilesProperties>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.EntryId })
                    .HasName("PRIMARY");

                entity.ToTable("files_properties");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id");

                entity.Property(e => e.EntryId)
                    .HasColumnName("entry_id")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Data)
                    .HasColumnName("data")
                    .HasColumnType("mediumtext")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.StartFilling)
                    .HasColumnName("start_filling")
                    .HasColumnType("tinyint(1)")
                    .HasComputedColumnSql(
                        "IF(JSON_EXTRACT(`data`, '$.FormFilling.StartFilling') IS NULL, NULL, JSON_EXTRACT(`data`, '$.FormFilling.StartFilling'))",
                        stored: true);

                entity.HasIndex(e => new { e.TenantId, e.StartFilling, e.EntryId })
                    .HasDatabaseName("idx_tenant_start_entry");

            });
        }

        public void PgSqlDbFilesProperties()
        {
            modelBuilder.Entity<DbFilesProperties>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.EntryId })
                    .HasName("pk_files_properties");

                entity.ToTable("files_properties");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id");

                entity.Property(e => e.EntryId)
                    .HasColumnName("entry_id")
                    .HasColumnType("varchar(32)");

                entity.Property(e => e.Data)
                    .HasColumnName("data")
                    .HasColumnType("text");

                entity.Property(e => e.StartFilling)
                    .HasColumnName("start_filling")
                    .HasComputedColumnSql(
                        "CASE WHEN data->'FormFilling'->>'StartFilling' IS NULL THEN NULL ELSE (data->'FormFilling'->>'StartFilling')::boolean END",
                        stored: true);

                entity.HasIndex(e => new { e.TenantId, e.StartFilling, e.EntryId })
                    .HasDatabaseName("idx_tenant_start_entry");
            });
        }
    }
}