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

namespace ASC.Migrations.Core.Identity;

public class IdentityShedlock
{
    public string Name { get; set; } = null!;

    public DateTime LockUntil { get; set; }

    public DateTime LockedAt { get; set; }

    public string LockedBy { get; set; } = null!;
}

public static class IdentityShedlockExtension
{
    public static ModelBuilderWrapper AddIdentityShedlock(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityShedlock, Provider.MySql)
            .Add(PgSqlAddIdentityShedlock, Provider.PostgreSql)
            ;

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityShedlock()
        {
            modelBuilder.Entity<IdentityShedlock>(entity =>
            {
                entity.HasKey(e => e.Name).HasName("PRIMARY");

                entity.ToTable("identity_shedlock");

                entity.Property(e => e.Name)
                    .HasMaxLength(64)
                    .HasColumnName("name");
                entity.Property(e => e.LockUntil)
                    .HasColumnType("timestamp(3)")
                    .HasColumnName("lock_until");
                entity.Property(e => e.LockedAt)
                    .HasColumnType("timestamp(3)")
                    .HasColumnName("locked_at");
                entity.Property(e => e.LockedBy)
                    .HasMaxLength(255)
                    .HasColumnName("locked_by")
                    .IsRequired();
            });
        }

        public void PgSqlAddIdentityShedlock()
        {
            modelBuilder.Entity<IdentityShedlock>(entity =>
            {
                // Setting primary key with "name" column
                entity.HasKey(e => e.Name).HasName("identity_shedlock_pkey");

                // Mapping this entity to the PostgreSQL table
                entity.ToTable("identity_shedlock");

                // Configuring the properties
                entity.Property(e => e.Name)
                    .HasMaxLength(64) // Restricting the length
                    .HasColumnName("name");

                entity.Property(e => e.LockUntil)
                    .HasColumnType("timestamptz") // PostgreSQL specific timestamp type
                    .HasColumnName("lock_until");

                entity.Property(e => e.LockedAt)
                    .HasColumnType("timestamptz") // PostgreSQL specific timestamp type
                    .HasColumnName("locked_at");

                entity.Property(e => e.LockedBy)
                    .HasMaxLength(255) // Restricting the length
                    .HasColumnName("locked_by")
                    .IsRequired(); // Configuring the column as NOT NULL
            });
        }
    }
}