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

public class BackupSchedule : BaseEntity
{
    public int TenantId { get; set; }
    [MaxLength(255)]
    public string Cron { get; set; }
    public int BackupsStored { get; set; }
    public BackupStorageType StorageType { get; set; }
    [MaxLength(255)]
    public string StorageBasePath { get; set; }
    public DateTime LastBackupTime { get; set; }
    public string StorageParams { get; set; }
    public bool Dump { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId];
    }
}

public static class BackupScheduleExtension
{
    public static ModelBuilderWrapper AddBackupSchedule(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<BackupSchedule>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddBackupSchedule, Provider.MySql)
            .Add(PgSqlAddBackupSchedule, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddBackupSchedule()
        {
            modelBuilder.Entity<BackupSchedule>(entity =>
            {
                entity.HasKey(e => new { e.TenantId })
                    .HasName("PRIMARY");

                entity.ToTable("backup_schedule")
                    .HasCharSet("utf8");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id")
                    .HasColumnType("int(10)")
                    .ValueGeneratedNever();

                entity.Property(e => e.Cron)
                    .IsRequired()
                    .HasColumnName("cron")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.BackupsStored)
                    .IsRequired()
                    .HasColumnName("backups_stored")
                    .HasColumnType("int(10)");

                entity.Property(e => e.StorageType)
                    .IsRequired()
                    .HasColumnName("storage_type")
                    .HasColumnType("int(10)");

                entity.Property(e => e.StorageBasePath)
                    .HasColumnName("storage_base_path")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci")
                    .HasDefaultValueSql("NULL");

                entity.Property(e => e.LastBackupTime)
                    .IsRequired()
                    .HasColumnName("last_backup_time")
                    .HasColumnType("datetime");

                entity.Property(e => e.StorageParams)
                    .HasColumnName("storage_params")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci")
                    .HasDefaultValueSql("NULL");

                entity.Property(e => e.Dump)
                    .HasColumnName("dump")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValueSql("'0'");

                entity.HasOne(e => e.Tenant)
                    .WithOne()
                    .HasForeignKey<BackupSchedule>(b => b.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public void PgSqlAddBackupSchedule()
        {
            modelBuilder.Entity<BackupSchedule>(entity =>
            {
                entity.HasKey(e => e.TenantId)
                    .HasName("PK_backup_schedule");

                entity.ToTable("backup_schedule");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id")
                    .HasColumnType("integer")
                    .ValueGeneratedNever();

                entity.Property(e => e.Cron)
                    .IsRequired()
                    .HasColumnName("cron")
                    .HasColumnType("varchar(255)");

                entity.Property(e => e.BackupsStored)
                    .IsRequired()
                    .HasColumnName("backups_stored")
                    .HasColumnType("integer");

                entity.Property(e => e.StorageType)
                    .IsRequired()
                    .HasColumnName("storage_type")
                    .HasColumnType("integer");

                entity.Property(e => e.StorageBasePath)
                    .HasColumnName("storage_base_path")
                    .HasColumnType("varchar(255)");

                entity.Property(e => e.LastBackupTime)
                    .IsRequired()
                    .HasColumnName("last_backup_time")
                    .HasColumnType("timestamptz");

                entity.Property(e => e.StorageParams)
                    .HasColumnName("storage_params")
                    .HasColumnType("text");

                entity.Property(e => e.Dump)
                    .HasColumnName("dump")
                    .HasColumnType("boolean");

                entity.HasOne(e => e.Tenant)
                    .WithOne()
                    .HasForeignKey<BackupSchedule>(b => b.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}