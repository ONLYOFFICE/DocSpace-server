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

public class DbWebstudioSettings : BaseEntity
{
    public int TenantId { get; set; }
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Data { get; set; }
    public DateTime LastModified { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, Id, UserId];
    }
}

public static class WebstudioSettingsExtension
{
    public static ModelBuilderWrapper AddWebstudioSettings(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbWebstudioSettings>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddWebstudioSettings, Provider.MySql)
            .Add(PgSqlAddWebstudioSettings, Provider.PostgreSql)
            .HasData(
            new DbWebstudioSettings
            {
                TenantId = 1,
                Id = Guid.Parse("9a925891-1f92-4ed7-b277-d6f649739f06"),
                UserId = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                Data = "{\"Completed\":false}"
            });

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddWebstudioSettings()
        {
            modelBuilder.Entity<DbWebstudioSettings>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.Id, e.UserId })
                    .HasName("PRIMARY");

                entity.ToTable("webstudio_settings")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.Id)
                    .HasDatabaseName("ID");

                entity.Property(e => e.TenantId).HasColumnName("TenantID");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.UserId)
                    .HasColumnName("UserID")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Data)
                    .IsRequired()
                    .HasColumnType("mediumtext")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("datetime");
            });
        }

        public void PgSqlAddWebstudioSettings()
        {
            modelBuilder.Entity<DbWebstudioSettings>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.Id, e.UserId })
                    .HasName("PK_webstudio_settings");

                entity.ToTable("webstudio_settings");

                entity.HasIndex(e => e.Id)
                    .HasDatabaseName("IX_webstudio_settings_Id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.Data)
                    .IsRequired()
                    .HasColumnName("data")
                    .HasColumnType("jsonb");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamptz");
            });
        }
    }
}