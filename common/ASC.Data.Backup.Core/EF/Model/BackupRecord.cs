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

namespace ASC.Data.Backup.EF.Model;

public class BackupRecord : BaseEntity
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }
    public bool IsScheduled { get; set; }
    [MaxLength(255)]
    public string Name { get; set; }
    [MaxLength(64)]
    public string Hash { get; set; }
    public BackupStorageType StorageType { get; set; }
    [MaxLength(255)]
    public string StorageBasePath { get; set; }
    [MaxLength(255)]
    public string StoragePath { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ExpiresOn { get; set; }
    public string StorageParams { get; set; }
    public bool Removed { get; set; }
    public bool Paid { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class BackupRecordExtension
{
    public static ModelBuilderWrapper AddBackupRecord(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<BackupRecord>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddBackupRecord, Provider.MySql)
            .Add(PgSqlAddBackupRecord, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddBackupRecord()
        {
            modelBuilder.Entity<BackupRecord>(entity =>
            {
                entity.ToTable("backup_backup")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("tenant_id");

                entity.HasIndex(e => e.ExpiresOn)
                    .HasDatabaseName("expires_on");

                entity.HasIndex(e => e.IsScheduled)
                    .HasDatabaseName("is_scheduled");

                entity.HasKey(e => new { e.Id })
                    .HasName("PRIMARY");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("char(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.TenantId)
                    .IsRequired()
                    .HasColumnName("tenant_id")
                    .HasColumnType("int(10)");

                entity.Property(e => e.IsScheduled)
                    .IsRequired()
                    .HasColumnName("is_scheduled")
                    .HasColumnType("tinyint(1)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.StorageType)
                    .IsRequired()
                    .HasColumnName("storage_type")
                    .HasColumnType("int(10)");

                entity.Property(e => e.StorageBasePath)
                    .HasColumnName("storage_base_path")
                    .HasColumnType("varchar")
                    .HasDefaultValueSql("NULL")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.StoragePath)
                    .IsRequired()
                    .HasColumnName("storage_path")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.CreatedOn)
                    .IsRequired()
                    .HasColumnName("created_on")
                    .HasColumnType("datetime");

                entity.Property(e => e.ExpiresOn)
                    .HasColumnName("expires_on")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("'0001-01-01 00:00:00'");

                entity.Property(e => e.StorageParams)
                    .HasColumnName("storage_params")
                    .HasColumnType("text")
                    .HasDefaultValueSql("NULL")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Hash)
                    .IsRequired()
                    .HasColumnName("hash")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Removed)
                    .HasColumnName("removed")
                    .HasColumnType("tinyint(1)")
                    .IsRequired();

                entity.Property(e => e.Paid)
                    .HasColumnName("paid")
                    .HasColumnType("tinyint(1)")
                    .IsRequired();
            });
        }

        public void PgSqlAddBackupRecord()
        {
            modelBuilder.Entity<BackupRecord>(entity =>
            {
                entity.ToTable("backup_backup");

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("tenant_id");

                entity.HasIndex(e => e.ExpiresOn)
                    .HasDatabaseName("expires_on");

                entity.HasIndex(e => e.IsScheduled)
                    .HasDatabaseName("is_scheduled");

                entity.HasKey(e => new { e.Id })
                    .HasName("PK_backup_backup");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("uuid");

                entity.Property(e => e.TenantId)
                    .IsRequired()
                    .HasColumnName("tenant_id")
                    .HasColumnType("integer");

                entity.Property(e => e.IsScheduled)
                    .IsRequired()
                    .HasColumnName("is_scheduled")
                    .HasColumnType("boolean");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("character varying")
                    .HasMaxLength(255);

                entity.Property(e => e.StorageType)
                    .IsRequired()
                    .HasColumnName("storage_type")
                    .HasColumnType("integer");

                entity.Property(e => e.StorageBasePath)
                    .HasColumnName("storage_base_path")
                    .HasColumnType("character varying")
                    .HasMaxLength(255)
                    .HasDefaultValueSql("NULL");

                entity.Property(e => e.StoragePath)
                    .IsRequired()
                    .HasColumnName("storage_path")
                    .HasColumnType("character varying")
                    .HasMaxLength(255);

                entity.Property(e => e.CreatedOn)
                    .IsRequired()
                    .HasColumnName("created_on")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.ExpiresOn)
                    .HasColumnName("expires_on")
                    .HasColumnType("timestamptz")
                    .HasDefaultValueSql("'0001-01-01 00:00:00'");

                entity.Property(e => e.StorageParams)
                    .HasColumnName("storage_params")
                    .HasColumnType("text")
                    .HasDefaultValueSql("NULL");

                entity.Property(e => e.Hash)
                    .IsRequired()
                    .HasColumnName("hash")
                    .HasColumnType("character varying")
                    .HasMaxLength(64);

                entity.Property(e => e.Removed)
                    .HasColumnName("removed")
                    .IsRequired()
                    .HasColumnType("boolean");

                entity.Property(e => e.Paid)
                    .HasColumnName("paid")
                    .IsRequired()
                    .HasColumnType("boolean");
            });
        }
    }
}