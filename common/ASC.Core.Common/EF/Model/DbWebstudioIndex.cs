﻿// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Core.Common.EF.Model;

public class DbWebstudioIndex : BaseEntity
{
    [MaxLength(50)]
    public string IndexName { get; set; }
    public DateTime LastModified { get; set; }
    public override object[] GetKeys()
    {
        return [IndexName];
    }
}

public static class DbWebstudioIndexExtension
{
    public static ModelBuilderWrapper AddDbWebstudioIndex(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddDbWebstudioIndex, Provider.MySql)
            .Add(PgSqlAddDbWebstudioIndex, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbWebstudioIndex(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbWebstudioIndex>(entity =>
        {
            entity.HasKey(e => e.IndexName)
                .HasName("PRIMARY");

            entity.ToTable("webstudio_index")
                .HasCharSet("utf8");

            entity.Property(e => e.IndexName)
                .HasColumnName("index_name")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.LastModified)
                .HasColumnName("last_modified")
                .HasColumnType("timestamp");
        });
    }
    public static void PgSqlAddDbWebstudioIndex(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbWebstudioIndex>(entity =>
        {
            entity.HasKey(e => e.IndexName)
                .HasName("pk_webstudio_index");

            entity.ToTable("webstudio_index");

            entity.Property(e => e.IndexName)
                .HasColumnName("index_name")
                .HasColumnType("varchar(50)");

            entity.Property(e => e.LastModified)
                .HasColumnName("last_modified")
                .HasColumnType("timestamptz");
        });
        
    }
}
