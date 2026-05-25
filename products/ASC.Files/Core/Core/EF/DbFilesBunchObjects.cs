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
    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesBunchObjects()
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

        public void PgSqlAddDbFilesBunchObjects()
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
}