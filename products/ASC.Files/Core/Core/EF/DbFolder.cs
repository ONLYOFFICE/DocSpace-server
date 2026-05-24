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

[Transient]
[OpenSearchType(RelationName = Tables.Folder)]
public class DbFolder : IDbFile, IDbSearch, ISearchItem
{
    public int Id { get; set; }
    public int ParentId { get; set; }

    [Text(Analyzer = "whitespacecustom")]
    [MaxLength(400)]
    public string Title { get; set; }
    public FolderType FolderType { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime CreateOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public DateTime ModifiedOn { get; set; }
    public int TenantId { get; set; }
    public int FoldersCount { get; set; }
    public int FilesCount { get; set; }
    public long Counter { get; set; }

    [Ignore]
    public DbRoomSettings Settings { get; set; }

    [Ignore]
    public DbTenant Tenant { get; set; }

    [Ignore]
    public string IndexName => Tables.Folder;

    public Expression<Func<ISearchItem, object[]>> GetSearchContentFields(SearchSettingsHelper searchSettings)
    {
        return a => new object[] { Title };
    }
}

public static class DbFolderExtension
{
    public static ModelBuilderWrapper AddDbFolder(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFolder>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFolder, Provider.MySql)
                .Add(PgSqlAddDbFolder, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFolder()
        {
            modelBuilder.Entity<DbFolder>(entity =>
            {
                entity.ToTable("files_folder")
                    .HasCharSet("utf8");

                entity.Ignore(r => r.IndexName);

                entity.HasIndex(e => e.ModifiedOn)
                    .HasDatabaseName("modified_on");

                entity.HasIndex(e => new { e.TenantId, e.ParentId })
                    .HasDatabaseName("parent_id");

                entity.HasIndex(e => new { e.TenantId, e.ParentId, e.Title })
                    .HasDatabaseName("tenant_id_parent_id_title");

                entity.HasIndex(e => new { e.TenantId, e.ParentId, e.ModifiedOn })
                    .HasDatabaseName("tenant_id_parent_id_modified_on");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreateBy)
                    .IsRequired()
                    .HasColumnName("create_by")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.CreateOn)
                    .HasColumnName("create_on")
                    .HasColumnType("datetime");

                entity.Property(e => e.FilesCount)
                    .HasColumnName("filesCount")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.FolderType)
                    .HasColumnName("folder_type")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.FoldersCount)
                    .HasColumnName("foldersCount")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.ModifiedBy)
                    .IsRequired()
                    .HasColumnName("modified_by")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ModifiedOn)
                    .HasColumnName("modified_on")
                    .HasColumnType("datetime");

                entity.Property(e => e.ParentId)
                    .HasColumnName("parent_id")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("title")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Counter)
                    .HasColumnName("counter")
                    .HasDefaultValueSql("'0'");
            });
        }

        public void PgSqlAddDbFolder()
        {
            modelBuilder.Entity<DbFolder>(entity =>
            {
                entity.ToTable("files_folder");

                entity.HasKey(e => e.Id)
                    .HasName("PK_files_folder");

                entity.Ignore(r => r.IndexName);

                entity.HasIndex(e => e.ModifiedOn)
                    .HasDatabaseName("IX_files_folder_modified_on");

                entity.HasIndex(e => new { e.TenantId, e.ParentId })
                    .HasDatabaseName("parent_id");

                entity.HasIndex(e => new { e.TenantId, e.ParentId, e.Title })
                    .HasDatabaseName("tenant_id_parent_id_title");

                entity.HasIndex(e => new { e.TenantId, e.ParentId, e.ModifiedOn })
                    .HasDatabaseName("tenant_id_parent_id_modified_on");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreateBy)
                    .IsRequired()
                    .HasColumnName("create_by")
                    .HasColumnType("uuid");

                entity.Property(e => e.CreateOn)
                    .HasColumnName("create_on")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.FilesCount)
                    .HasColumnName("filesCount")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.FolderType)
                    .HasColumnName("folder_type")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.FoldersCount)
                    .HasColumnName("foldersCount")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.ModifiedBy)
                    .IsRequired()
                    .HasColumnName("modified_by")
                    .HasColumnType("uuid");

                entity.Property(e => e.ModifiedOn)
                    .HasColumnName("modified_on")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.ParentId)
                    .HasColumnName("parent_id")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("title")
                    .HasColumnType("character varying");

                entity.Property(e => e.Counter)
                    .HasColumnName("counter")
                    .HasDefaultValueSql("0");
            });
        }
    }
}