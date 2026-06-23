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

namespace ASC.Core.Common.EF.Model;

public class NotifyInfo
{
    public int NotifyId { get; set; }
    public int State { get; set; }
    public int Attempts { get; set; }
    public DateTime ModifyDate { get; set; }
    public int Priority { get; set; }
}
public static class NotifyInfoExtension
{
    public static ModelBuilderWrapper AddNotifyInfo(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddNotifyInfo, Provider.MySql)
            .Add(PgSqlAddNotifyInfo, Provider.PostgreSql);

        return modelBuilder;
    }
    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddNotifyInfo()
        {
            modelBuilder.Entity<NotifyInfo>(entity =>
            {
                entity.HasKey(e => e.NotifyId)
                    .HasName("PRIMARY");

                entity.ToTable("notify_info")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.State)
                    .HasDatabaseName("state");

                entity.Property(e => e.NotifyId)
                    .HasColumnName("notify_id")
                    .ValueGeneratedNever();

                entity.Property(e => e.Attempts)
                    .HasColumnName("attempts")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.ModifyDate)
                    .HasColumnName("modify_date")
                    .HasColumnType("datetime");

                entity.Property(e => e.Priority)
                    .HasColumnName("priority")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.State)
                    .HasColumnName("state")
                    .HasDefaultValueSql("'0'");
            });
        }

        public void PgSqlAddNotifyInfo()
        {
            modelBuilder.Entity<NotifyInfo>(entity =>
            {
                entity.HasKey(e => e.NotifyId)
                    .HasName("PK_notify_info"); // PostgreSQL constraint name standard

                entity.ToTable("notify_info"); // Specify the table name

                entity.HasIndex(e => e.State)
                    .HasDatabaseName("IX_notify_info_state"); // Define an index for the "state" column

                entity.Property(e => e.NotifyId)
                    .HasColumnName("notify_id"); // Map NotifyId to "notify_id"

                entity.Property(e => e.Attempts)
                    .HasColumnName("attempts")
                    .HasDefaultValue(0); // Default value for PostgreSQL

                entity.Property(e => e.ModifyDate)
                    .HasColumnName("modify_date")
                    .HasColumnType("timestamptz"); // Typical timestamp configuration for PostgreSQL

                entity.Property(e => e.Priority)
                    .HasColumnName("priority")
                    .HasDefaultValue(0); // Default value for Priority

                entity.Property(e => e.State)
                    .HasColumnName("state")
                    .HasDefaultValue(0); // Default value for State
            });
        }
    }
}