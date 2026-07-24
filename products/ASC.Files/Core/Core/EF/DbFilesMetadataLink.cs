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

public class DbFilesMetadataLink : BaseEntity, IDbFile
{
    public int TenantId { get; set; }
    public int TemplateId { get; set; }
    public int EntryId { get; set; }
    public FileEntryType EntryType { get; set; }
    public bool Cascade { get; set; }
    public int? SourceFolderId { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime CreateOn { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, TemplateId, EntryId, EntryType];
    }
}

public static class DbFilesMetadataLinkExtension
{
    public static ModelBuilderWrapper AddDbFilesMetadataLink(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesMetadataLink>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesMetadataLink, Provider.MySql)
            .Add(PgSqlAddDbFilesMetadataLink, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesMetadataLink()
        {
            modelBuilder.Entity<DbFilesMetadataLink>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.TemplateId, e.EntryId, e.EntryType })
                    .HasName("PRIMARY");

                entity.ToTable("files_metadata_link")
                    .HasCharSet("utf8");

                entity.HasIndex(e => new { e.TenantId, e.EntryId, e.EntryType })
                    .HasDatabaseName("entry_id");

                entity.HasIndex(e => new { e.TenantId, e.SourceFolderId })
                    .HasDatabaseName("source_folder_id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.TemplateId).HasColumnName("template_id");

                entity.Property(e => e.EntryId).HasColumnName("entry_id");

                entity.Property(e => e.EntryType).HasColumnName("entry_type");

                entity.Property(e => e.Cascade)
                    .HasColumnName("is_cascade")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.SourceFolderId).HasColumnName("source_folder_id");

                entity.Property(e => e.CreateBy)
                    .IsRequired()
                    .HasColumnName("create_by")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.CreateOn)
                    .HasColumnName("create_on")
                    .HasColumnType("datetime");
            });
        }

        public void PgSqlAddDbFilesMetadataLink()
        {
            modelBuilder.Entity<DbFilesMetadataLink>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.TemplateId, e.EntryId, e.EntryType })
                    .HasName("pk_files_metadata_link");

                entity.ToTable("files_metadata_link");

                entity.HasIndex(e => new { e.TenantId, e.EntryId, e.EntryType })
                    .HasDatabaseName("idx_files_metadata_link_entry_id");

                entity.HasIndex(e => new { e.TenantId, e.SourceFolderId })
                    .HasDatabaseName("idx_files_metadata_link_source_folder_id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.TemplateId).HasColumnName("template_id");

                entity.Property(e => e.EntryId).HasColumnName("entry_id");

                entity.Property(e => e.EntryType).HasColumnName("entry_type");

                entity.Property(e => e.Cascade)
                    .HasColumnName("is_cascade")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.SourceFolderId).HasColumnName("source_folder_id");

                entity.Property(e => e.CreateBy)
                    .IsRequired()
                    .HasColumnName("create_by")
                    .HasColumnType("uuid");

                entity.Property(e => e.CreateOn)
                    .HasColumnName("create_on")
                    .HasColumnType("timestamptz");
            });
        }
    }
}