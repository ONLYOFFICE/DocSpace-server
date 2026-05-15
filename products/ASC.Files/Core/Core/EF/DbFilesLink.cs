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

public class DbFilesLink : BaseEntity, IDbFile
{
    public int TenantId { get; set; }
    [MaxLength(32)]
    public string SourceId { get; set; }
    [MaxLength(32)]
    public string LinkedId { get; set; }
    public Guid LinkedFor { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, SourceId, LinkedId];
    }
}

public static class DbFilesLinkExtension
{
    public static ModelBuilderWrapper AddDbFilesLink(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesLink>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesLink, Provider.MySql)
            .Add(PgSqlAddDbFilesLink, Provider.PostgreSql);

        return modelBuilder;
    }
    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesLink()
        {
            modelBuilder.Entity<DbFilesLink>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.SourceId, e.LinkedId })
                    .HasName("PRIMARY");

                entity.ToTable("files_link")
                    .HasCharSet("utf8");

                entity.HasIndex(e => new { e.TenantId, e.SourceId, e.LinkedId, e.LinkedFor })
                    .HasDatabaseName("linked_for");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.SourceId)
                    .HasColumnName("source_id")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.LinkedId)
                    .HasColumnName("linked_id")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.LinkedFor)
                    .HasColumnName("linked_for")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");
            });
        }

        public void PgSqlAddDbFilesLink()
        {
            modelBuilder.Entity<DbFilesLink>(entity =>
            {
                // Define composite primary key
                entity.HasKey(e => new { e.TenantId, e.SourceId, e.LinkedId })
                    .HasName("PK_files_link");

                // Map entity to "files_link" table
                entity.ToTable("files_link");

                // Define index for PostgreSQL
                entity.HasIndex(e => new { e.TenantId, e.SourceId, e.LinkedId, e.LinkedFor })
                    .HasDatabaseName("linked_for");

                // Define column configurations
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.SourceId)
                    .HasColumnName("source_id")
                    .HasColumnType("varchar(32)");

                entity.Property(e => e.LinkedId)
                    .HasColumnName("linked_id")
                    .HasColumnType("varchar(32)");

                entity.Property(e => e.LinkedFor)
                    .HasColumnName("linked_for")
                    .HasColumnType("uuid"); // Guid in PostgreSQL is stored as UUID
            });
        }
    }
}