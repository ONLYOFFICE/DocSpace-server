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

public class DbFilesMetadataField : IDbFile
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int TemplateId { get; set; }
    [MaxLength(255)]
    public string Name { get; set; }
    public MetadataFieldType Type { get; set; }
    public string Options { get; set; }
    public int Order { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime CreateOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public DateTime ModifiedOn { get; set; }

    public DbTenant Tenant { get; set; }
}

public static class DbFilesMetadataFieldExtension
{
    public static ModelBuilderWrapper AddDbFilesMetadataField(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesMetadataField>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesMetadataField, Provider.MySql)
            .Add(PgSqlAddDbFilesMetadataField, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesMetadataField()
        {
            modelBuilder.Entity<DbFilesMetadataField>(entity =>
            {
                entity.ToTable("files_metadata_field")
                    .HasCharSet("utf8");

                entity.HasIndex(e => new { e.TenantId, e.TemplateId })
                    .HasDatabaseName("tenant_id_template_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.TemplateId).HasColumnName("template_id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Options)
                    .HasColumnName("options")
                    .HasColumnType("json")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Order)
                    .HasColumnName("order")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.CreateBy)
                    .IsRequired()
                    .HasColumnName("create_by")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.CreateOn)
                    .HasColumnName("create_on")
                    .HasColumnType("datetime");

                entity.Property(e => e.ModifiedBy)
                    .IsRequired()
                    .HasColumnName("modified_by")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ModifiedOn)
                    .HasColumnName("modified_on")
                    .HasColumnType("datetime");
            });
        }

        public void PgSqlAddDbFilesMetadataField()
        {
            modelBuilder.Entity<DbFilesMetadataField>(entity =>
            {
                entity.ToTable("files_metadata_field");

                entity.HasIndex(e => new { e.TenantId, e.TemplateId })
                    .HasDatabaseName("idx_files_metadata_field_tenant_id_template_id");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.TemplateId).HasColumnName("template_id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar");

                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.Options)
                    .HasColumnName("options")
                    .HasColumnType("jsonb");

                entity.Property(e => e.Order)
                    .HasColumnName("order")
                    .HasDefaultValueSql("0");

                entity.Property(e => e.CreateBy)
                    .IsRequired()
                    .HasColumnName("create_by")
                    .HasColumnType("uuid");

                entity.Property(e => e.CreateOn)
                    .HasColumnName("create_on")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.ModifiedBy)
                    .IsRequired()
                    .HasColumnName("modified_by")
                    .HasColumnType("uuid");

                entity.Property(e => e.ModifiedOn)
                    .HasColumnName("modified_on")
                    .HasColumnType("timestamptz");
            });
        }
    }
}