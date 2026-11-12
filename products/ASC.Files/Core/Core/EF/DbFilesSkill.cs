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

public class DbFilesSkill : BaseEntity
{
    public int TenantId { get; set; }
    public int FileId { get; set; }

    [MaxLength(64)]
    public required string Name { get; set; }

    [MaxLength(1024)]
    public required string Description { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, FileId];
    }
}

public static class DbFilesSkillExtension
{
    public static ModelBuilderWrapper AddDbFilesSkill(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesSkill>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesSkill, Provider.MySql)
            .Add(PgSqlAddDbFilesSkill, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesSkill()
        {
            modelBuilder.Entity<DbFilesSkill>(entity =>
            {
                entity.ToTable("files_skill")
                    .HasCharSet("utf8");

                entity.HasKey(e => new { e.TenantId, e.FileId })
                    .HasName("PRIMARY");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.FileId).HasColumnName("file_id");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8mb4")
                    .UseCollation("utf8mb4_general_ci")
                    .IsRequired();

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("varchar(1024)")
                    .HasCharSet("utf8mb4")
                    .UseCollation("utf8mb4_general_ci")
                    .IsRequired();
            });
        }

        public void PgSqlAddDbFilesSkill()
        {
            modelBuilder.Entity<DbFilesSkill>(entity =>
            {
                entity.ToTable("files_skill");

                entity.HasKey(e => new { e.TenantId, e.FileId })
                    .HasName("pk_files_skill");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.FileId).HasColumnName("file_id");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasMaxLength(1024)
                    .IsRequired();
            });
        }
    }
}
