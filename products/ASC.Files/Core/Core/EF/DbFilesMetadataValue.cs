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

public class DbFilesMetadataValue : BaseEntity, IDbFile
{
    public int TenantId { get; set; }
    public int EntryId { get; set; }
    public FileEntryType EntryType { get; set; }
    public int FieldId { get; set; }
    [MaxLength(36)]
    public string OptionId { get; set; }
    public string ValueString { get; set; }
    public long? ValueNumber { get; set; }
    public DateTime? ValueDate { get; set; }
    public Guid CreateBy { get; set; }
    public DateTime CreateOn { get; set; }
    public Guid ModifiedBy { get; set; }
    public DateTime ModifiedOn { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, EntryId, EntryType, FieldId, OptionId];
    }
}

public static class DbFilesMetadataValueExtension
{
    public static ModelBuilderWrapper AddDbFilesMetadataValue(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesMetadataValue>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesMetadataValue, Provider.MySql)
            .Add(PgSqlAddDbFilesMetadataValue, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesMetadataValue()
        {
            modelBuilder.Entity<DbFilesMetadataValue>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.EntryId, e.EntryType, e.FieldId, e.OptionId })
                    .HasName("PRIMARY");

                entity.ToTable("files_metadata_value")
                    .HasCharSet("utf8");

                entity.HasIndex(e => new { e.TenantId, e.FieldId, e.ValueDate })
                    .HasDatabaseName("field_id_value_date");

                entity.HasIndex(e => new { e.TenantId, e.FieldId, e.ValueNumber })
                    .HasDatabaseName("field_id_value_number");

                entity.HasIndex(e => new { e.TenantId, e.FieldId, e.OptionId })
                    .HasDatabaseName("field_id_option_id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.EntryId).HasColumnName("entry_id");

                entity.Property(e => e.EntryType).HasColumnName("entry_type");

                entity.Property(e => e.FieldId).HasColumnName("field_id");

                entity.Property(e => e.OptionId)
                    .HasColumnName("option_id")
                    .HasColumnType("varchar")
                    .HasDefaultValueSql("''")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ValueString)
                    .HasColumnName("value_string")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ValueNumber).HasColumnName("value_number");

                entity.Property(e => e.ValueDate)
                    .HasColumnName("value_date")
                    .HasColumnType("datetime");

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

        public void PgSqlAddDbFilesMetadataValue()
        {
            modelBuilder.Entity<DbFilesMetadataValue>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.EntryId, e.EntryType, e.FieldId, e.OptionId })
                    .HasName("pk_files_metadata_value");

                entity.ToTable("files_metadata_value");

                entity.HasIndex(e => new { e.TenantId, e.FieldId, e.ValueDate })
                    .HasDatabaseName("idx_files_metadata_value_field_id_value_date");

                entity.HasIndex(e => new { e.TenantId, e.FieldId, e.ValueNumber })
                    .HasDatabaseName("idx_files_metadata_value_field_id_value_number");

                entity.HasIndex(e => new { e.TenantId, e.FieldId, e.OptionId })
                    .HasDatabaseName("idx_files_metadata_value_field_id_option_id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.EntryId).HasColumnName("entry_id");

                entity.Property(e => e.EntryType).HasColumnName("entry_type");

                entity.Property(e => e.FieldId).HasColumnName("field_id");

                entity.Property(e => e.OptionId)
                    .HasColumnName("option_id")
                    .HasColumnType("varchar(36)")
                    .HasDefaultValueSql("''");

                entity.Property(e => e.ValueString)
                    .HasColumnName("value_string")
                    .HasColumnType("text");

                entity.Property(e => e.ValueNumber).HasColumnName("value_number");

                entity.Property(e => e.ValueDate)
                    .HasColumnName("value_date")
                    .HasColumnType("timestamptz");

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