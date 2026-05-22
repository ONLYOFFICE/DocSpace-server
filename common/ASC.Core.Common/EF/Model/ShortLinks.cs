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
public class ShortLink
{
    public ulong Id { get; set; }
    public int TenantId { get; set; }
    [MaxLength(15)]
    public string Short { get; set; }
    public string Link { get; set; }

    public DbTenant Tenant { get; set; }
}

public static class ShortLinksExtension
{
    public static ModelBuilderWrapper AddShortLinks(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<ShortLink>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder
            .Add(MySqlAddShortLinks, Provider.MySql)
            .Add(PgSqlAddShortLinks, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddShortLinks()
        {
            modelBuilder.Entity<ShortLink>(entity =>
            {
                entity.ToTable("short_links")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.HasIndex(e => e.Short)
                    .IsUnique();

                entity.HasKey(e => e.Id)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("tenant_id");

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.Short)
                    .HasColumnName("short")
                    .HasColumnType("char")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci")
                    .IsRequired(false);

                entity.Property(e => e.TenantId)
                    .IsRequired()
                    .HasColumnName("tenant_id")
                    .HasColumnType("int(10)")
                    .HasDefaultValue("-1");

                entity.Property(e => e.Link)
                    .HasColumnName("link")
                    .HasColumnType("text")
                    .UseCollation("utf8_bin")
                    .IsRequired(false);
            });
        }

        public void PgSqlAddShortLinks()
        {
            modelBuilder.Entity<ShortLink>(entity =>
            {
                entity.ToTable("short_links");

                entity.HasKey(e => e.Id)
                    .HasName("PK_short_links");

                entity.HasIndex(e => e.Short)
                    .IsUnique();

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_short_links_tenant_id");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("bigint");

                entity.Property(e => e.Short)
                    .HasColumnName("short")
                    .HasColumnType("char(15)")
                    .IsRequired(false);

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id")
                    .HasColumnType("integer")
                    .IsRequired()
                    .HasDefaultValue(-1);

                entity.Property(e => e.Link)
                    .HasColumnName("link")
                    .HasColumnType("text")
                    .IsRequired(false);

                // Configure relationship if required
                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}