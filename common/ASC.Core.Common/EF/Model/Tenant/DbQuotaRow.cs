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

public class DbQuotaRow : BaseEntity
{
    public int TenantId { get; set; }
    [MaxLength(255)]
    public string Path { get; set; }
    public long Counter { get; set; }
    [MaxLength(1024)]
    public string Tag { get; set; }
    public DateTime LastModified { get; set; }
    public Guid UserId { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, UserId, Path];
    }
}

public static class DbQuotaRowExtension
{
    public static ModelBuilderWrapper AddDbQuotaRow(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbQuotaRow>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbQuotaRow, Provider.MySql)
            .Add(PgSqlAddDbQuotaRow, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbQuotaRow()
        {
            modelBuilder.Entity<DbQuotaRow>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.UserId, e.Path })
                    .HasName("PRIMARY");

                entity.ToTable("tenants_quotarow")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.LastModified)
                    .HasDatabaseName("last_modified");

                entity.Property(e => e.TenantId).HasColumnName("tenant");

                entity.Property(e => e.Path)
                    .HasColumnName("path")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Counter)
                    .HasColumnName("counter")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamp");

                entity.Property(e => e.Tag)
                    .HasColumnName("tag")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasColumnType("char(36)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");
            });
        }

        public void PgSqlAddDbQuotaRow()
        {
            modelBuilder.Entity<DbQuotaRow>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.UserId, e.Path });

                entity.ToTable("tenants_quotarow");

                entity.HasIndex(e => e.LastModified)
                    .HasDatabaseName("last_modified");

                entity.Property(e => e.TenantId).HasColumnName("tenant");

                entity.Property(e => e.Path)
                    .HasColumnName("path")
                    .HasColumnType("varchar(255)");

                entity.Property(e => e.Counter)
                    .HasColumnName("counter")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.Tag)
                    .HasColumnName("tag")
                    .HasColumnType("varchar(1024)");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasColumnType("uuid");
            });
        }
    }
}