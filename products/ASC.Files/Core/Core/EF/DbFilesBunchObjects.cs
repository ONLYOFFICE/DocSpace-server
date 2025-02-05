﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Core.EF;

public class DbFilesBunchObjects : BaseEntity, IDbFile
{
    public int TenantId { get; set; }
    [MaxLength(255)]
    public string RightNode { get; set; }
    [MaxLength(255)]
    public string LeftNode { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, RightNode];
    }
}

public static class DbFilesBunchObjectsExtension
{
    public static ModelBuilderWrapper AddDbFilesBunchObjects(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesBunchObjects>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesBunchObjects, Provider.MySql)
            .Add(PgSqlAddDbFilesBunchObjects, Provider.PostgreSql);

        return modelBuilder;
    }
    public static void MySqlAddDbFilesBunchObjects(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFilesBunchObjects>(entity =>
        {
            entity.HasKey(e => new { e.TenantId, e.RightNode })
                .HasName("PRIMARY");

            entity.ToTable("files_bunch_objects")
                .HasCharSet("utf8");

            entity.HasIndex(e => e.LeftNode)
                .HasDatabaseName("left_node");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id");

            entity.Property(e => e.RightNode)
                .HasColumnName("right_node")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.LeftNode)
                .IsRequired()
                .HasColumnName("left_node")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
        });
    }
    
    public static void PgSqlAddDbFilesBunchObjects(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFilesBunchObjects>(entity =>
        {
            // Define composite primary key
            entity.HasKey(e => new { e.TenantId, e.RightNode })
                .HasName("pk_files_bunch_objects");

            // Map to PostgreSQL table
            entity.ToTable("files_bunch_objects");

            // Create an index for LeftNode
            entity.HasIndex(e => e.LeftNode)
                .HasDatabaseName("idx_files_bunch_objects_left_node");

            // Map TenantId column
            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            // Map RightNode column with PostgreSQL-specific type
            entity.Property(e => e.RightNode)
                .HasColumnName("right_node")
                .HasColumnType("varchar")
                .IsRequired();

            // Map LeftNode column with PostgreSQL-specific type
            entity.Property(e => e.LeftNode)
                .HasColumnName("left_node")
                .HasColumnType("varchar")
                .IsRequired();
        });
    }
}
