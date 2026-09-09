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

namespace ASC.Core.Common.EF.Model;

public class DbAppSettings : BaseEntity
{
    public int TenantId { get; set; }

    [MaxLength(64)]
    public string Id { get; set; }

    public bool Enabled { get; set; }

    public string Settings { get; set; }

    public DateTime LastModified { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, Id];
    }
}

public static class AppSettingsExtension
{
    public static ModelBuilderWrapper AddAppSettings(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbAppSettings>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddAppSettings, Provider.MySql)
            .Add(PgSqlAddAppSettings, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddAppSettings()
        {
            modelBuilder.Entity<DbAppSettings>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.Id })
                    .HasName("PRIMARY");

                entity.ToTable("app_settings")
                    .HasCharSet("utf8");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Enabled)
                    .HasColumnName("enabled")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Settings)
                    .HasColumnName("settings")
                    .HasColumnType("json");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("datetime");
            });
        }

        public void PgSqlAddAppSettings()
        {
            modelBuilder.Entity<DbAppSettings>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.Id })
                    .HasName("PK_app_settings");

                entity.ToTable("app_settings");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("character varying")
                    .HasMaxLength(64);

                entity.Property(e => e.Enabled)
                    .HasColumnName("enabled")
                    .HasDefaultValue(false);

                entity.Property(e => e.Settings)
                    .HasColumnName("settings")
                    .HasColumnType("jsonb");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamptz");
            });
        }
    }
}
