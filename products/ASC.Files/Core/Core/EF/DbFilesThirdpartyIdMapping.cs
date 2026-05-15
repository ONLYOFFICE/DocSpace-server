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

public class DbFilesThirdpartyIdMapping : BaseEntity, IDbFile
{
    public int TenantId { get; set; }
    [MaxLength(32)]
    public string HashId { get; set; }
    public string Id { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [HashId];
    }
}

public static class DbFilesThirdpartyIdMappingExtension
{
    public static ModelBuilderWrapper AddDbFilesThirdpartyIdMapping(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesThirdpartyIdMapping>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesThirdpartyIdMapping, Provider.MySql)
            .Add(PgSqlAddDbFilesThirdpartyIdMapping, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesThirdpartyIdMapping()
        {
            modelBuilder.Entity<DbFilesThirdpartyIdMapping>(entity =>
            {
                entity.HasKey(e => e.HashId)
                    .HasName("PRIMARY");

                entity.ToTable("files_thirdparty_id_mapping")
                    .HasCharSet("utf8");

                entity.HasIndex(e => new { e.TenantId, e.HashId })
                    .HasDatabaseName("index_1");

                entity.Property(e => e.HashId)
                    .HasColumnName("hash_id")
                    .HasColumnType("char")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasColumnName("id")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            });
        }

        public void PgSqlAddDbFilesThirdpartyIdMapping()
        {
            modelBuilder.Entity<DbFilesThirdpartyIdMapping>(entity =>
            {
                entity.HasKey(e => e.HashId)
                    .HasName("files_thirdparty_id_mapping_pkey"); // Define primary key

                entity.ToTable("files_thirdparty_id_mapping"); // Define table name

                entity.HasIndex(e => new { e.TenantId, e.HashId })
                    .HasDatabaseName("ix_files_thirdparty_id_mapping_tenantid_hashid"); // Define index

                entity.Property(e => e.HashId)
                    .HasColumnName("hash_id")
                    .HasColumnType("char(32)"); // Specify length explicitly since PostgreSQL requires it for char

                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasColumnName("id")
                    .HasColumnType("text"); // Map to PostgreSQL text type

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id");
            });
        }
    }
}