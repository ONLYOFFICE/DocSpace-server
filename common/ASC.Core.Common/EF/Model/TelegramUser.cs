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

public class TelegramUser : BaseEntity
{
    public Guid PortalUserId { get; set; }
    public int TenantId { get; set; }
    public long TelegramUserId { get; set; }
    public string TelegramUsername { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, PortalUserId];
    }
}

public static class TelegramUsersExtension
{
    public static ModelBuilderWrapper AddTelegramUsers(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<TelegramUser>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddTelegramUsers, Provider.MySql)
            .Add(PgSqlAddTelegramUsers, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddTelegramUsers()
        {
            modelBuilder.Entity<TelegramUser>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.PortalUserId })
                    .HasName("PRIMARY");

                entity.ToTable("telegram_users")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.TelegramUserId)
                    .HasDatabaseName("tgId");

                entity.HasIndex(e => e.TelegramUsername)
                    .HasDatabaseName("tgUsername");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.PortalUserId)
                    .HasColumnName("portal_user_id")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.TelegramUserId)
                    .HasColumnName("telegram_user_id")
                    .HasColumnType("int");

                entity.Property(e => e.TelegramUsername)
                    .HasColumnName("telegram_username")
                    .HasColumnType("varchar(35)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");
            });
        }

        public void PgSqlAddTelegramUsers()
        {
            modelBuilder.Entity<TelegramUser>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.PortalUserId })
                    .HasName("pk_telegram_user");

                entity.ToTable("telegram_users");

                entity.HasIndex(e => e.TelegramUserId)
                    .HasDatabaseName("ix_telegram_user_id");

                entity.HasIndex(e => e.TelegramUsername)
                    .HasDatabaseName("ix_telegram_username");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.PortalUserId)
                    .HasColumnName("portal_user_id")
                    .HasColumnType("uuid");

                entity.Property(e => e.TelegramUserId)
                    .HasColumnName("telegram_user_id")
                    .HasColumnType("bigint");

                entity.Property(e => e.TelegramUsername)
                    .HasColumnName("telegram_username")
                    .HasColumnType("char(35)");
            });

        }
    }
}