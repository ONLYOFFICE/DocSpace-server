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

public class IdentityPendingMessage
{
    public long Id { get; set; }

    public string ExchangeName { get; set; } = null!;

    public string RoutingKey { get; set; } = null!;

    public string Headers { get; set; } = null!;

    public byte[] Body { get; set; } = null!;

    public int Attempts { get; set; }

    public DateTime DeliverAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

public static class IdentityPendingMessageExtension
{
    public static ModelBuilderWrapper AddIdentityPendingMessage(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddIdentityPendingMessage, Provider.MySql)
            .Add(PgSqlAddIdentityPendingMessage, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddIdentityPendingMessage()
        {
            modelBuilder.Entity<IdentityPendingMessage>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("PRIMARY");

                entity.ToTable("identity_pending_messages");

                entity.HasIndex(e => e.DeliverAt, "idx_identity_pending_messages_deliver_at");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.ExchangeName)
                    .HasMaxLength(255)
                    .HasColumnName("exchange_name")
                    .IsRequired();
                entity.Property(e => e.RoutingKey)
                    .HasMaxLength(255)
                    .HasColumnName("routing_key")
                    .IsRequired();
                entity.Property(e => e.Headers)
                    .HasColumnType("text")
                    .HasColumnName("headers")
                    .IsRequired();
                entity.Property(e => e.Body)
                    .HasColumnType("mediumblob")
                    .HasColumnName("body")
                    .IsRequired();
                entity.Property(e => e.Attempts)
                    .HasDefaultValue(0)
                    .HasColumnName("attempts");
                entity.Property(e => e.DeliverAt)
                    .HasMaxLength(6)
                    .HasColumnName("deliver_at");
                entity.Property(e => e.CreatedAt)
                    .HasMaxLength(6)
                    .HasColumnName("created_at");
            });
        }

        public void PgSqlAddIdentityPendingMessage()
        {
            modelBuilder.Entity<IdentityPendingMessage>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("identity_pending_messages_pkey");

                entity.ToTable("identity_pending_messages");

                entity.HasIndex(e => e.DeliverAt, "idx_identity_pending_messages_deliver_at");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.ExchangeName)
                    .HasMaxLength(255)
                    .HasColumnName("exchange_name")
                    .IsRequired();
                entity.Property(e => e.RoutingKey)
                    .HasMaxLength(255)
                    .HasColumnName("routing_key")
                    .IsRequired();
                entity.Property(e => e.Headers)
                    .HasColumnType("text")
                    .HasColumnName("headers")
                    .IsRequired();
                entity.Property(e => e.Body)
                    .HasColumnType("bytea")
                    .HasColumnName("body")
                    .IsRequired();
                entity.Property(e => e.Attempts)
                    .HasDefaultValue(0)
                    .HasColumnName("attempts");
                entity.Property(e => e.DeliverAt)
                    .HasColumnType("timestamptz")
                    .HasColumnName("deliver_at");
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("timestamptz")
                    .HasColumnName("created_at");
            });
        }
    }
}
