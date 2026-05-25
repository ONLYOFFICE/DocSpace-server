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

[OpenSearchType(RelationName = Tables.Tree)]
public class DbFolderTree : BaseEntity
{
    public int FolderId { get; set; }
    public int ParentId { get; set; }
    public int Level { get; set; }

    public DbFolder Folder { get; set; }

    public override object[] GetKeys()
    {
        return [ParentId, FolderId];
    }
}

public static class DbFolderTreeExtension
{
    public static ModelBuilderWrapper AddDbFolderTree(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFolderTree>().Navigation(e => e.Folder).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFolderTree, Provider.MySql)
            .Add(PgSqlAddDbFolderTree, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFolderTree()
        {
            modelBuilder.Entity<DbFolderTree>(entity =>
            {
                entity.HasKey(e => new { e.ParentId, e.FolderId })
                    .HasName("PRIMARY");

                entity.ToTable("files_folder_tree")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.FolderId)
                    .HasDatabaseName("folder_id");

                entity.Property(e => e.ParentId).HasColumnName("parent_id");

                entity.Property(e => e.FolderId).HasColumnName("folder_id");

                entity.Property(e => e.Level).HasColumnName("level");
            });
        }

        public void PgSqlAddDbFolderTree()
        {
            modelBuilder.Entity<DbFolderTree>(entity =>
            {
                entity.HasKey(e => new { e.ParentId, e.FolderId })
                    .HasName("pk_files_folder_tree");

                entity.ToTable("files_folder_tree");

                entity.HasIndex(e => e.FolderId)
                    .HasDatabaseName("ix_folder_id");

                entity.Property(e => e.ParentId).HasColumnName("parent_id");

                entity.Property(e => e.FolderId).HasColumnName("folder_id");

                entity.Property(e => e.Level).HasColumnName("level");
            });
        }
    }
}