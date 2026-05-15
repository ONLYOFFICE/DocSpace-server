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

public class NotifyQueue
{
    public int NotifyId { get; set; }
    public int TenantId { get; set; }
    [MaxLength(255)]
    public string Sender { get; set; }
    [MaxLength(255)]
    public string Reciever { get; set; }
    [MaxLength(1024)]
    public string Subject { get; set; }
    [MaxLength(64)]
    public string ContentType { get; set; }
    public string Content { get; set; }
    [MaxLength(64)]
    public string SenderType { get; set; }
    [MaxLength(1024)]
    public string ReplyTo { get; set; }
    public DateTime CreationDate { get; set; }
    public string Attachments { get; set; }
    [MaxLength(64)]
    public string AutoSubmitted { get; set; }

    public DbTenant Tenant { get; set; }
}
public static class NotifyQueueExtension
{
    public static ModelBuilderWrapper AddNotifyQueue(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<NotifyQueue>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddNotifyQueue, Provider.MySql)
            .Add(PgSqlAddNotifyQueue, Provider.PostgreSql);

        return modelBuilder;
    }
    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddNotifyQueue()
        {
            modelBuilder.Entity<NotifyQueue>(entity =>
            {
                entity.HasKey(e => e.NotifyId)
                    .HasName("PRIMARY");

                entity.ToTable("notify_queue")
                    .HasCharSet("utf8");

                entity.Property(e => e.NotifyId).HasColumnName("notify_id");

                entity.Property(e => e.Attachments)
                    .HasColumnName("attachments")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.AutoSubmitted)
                    .HasColumnName("auto_submitted")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Content)
                    .HasColumnName("content")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ContentType)
                    .HasColumnName("content_type")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.CreationDate)
                    .HasColumnName("creation_date")
                    .HasColumnType("datetime");

                entity.HasIndex(e => e.CreationDate)
                    .HasDatabaseName("creation_date");

                entity.Property(e => e.Reciever)
                    .HasColumnName("reciever")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ReplyTo)
                    .HasColumnName("reply_to")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Sender)
                    .HasColumnName("sender")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.SenderType)
                    .HasColumnName("sender_type")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Subject)
                    .HasColumnName("subject")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            });
        }

        public void PgSqlAddNotifyQueue()
        {
            modelBuilder.Entity<NotifyQueue>(entity =>
            {
                entity.HasKey(e => e.NotifyId)
                    .HasName("pk_notify_queue");

                entity.ToTable("notify_queue");

                entity.Property(e => e.NotifyId).HasColumnName("notify_id");

                entity.Property(e => e.Attachments)
                    .HasColumnName("attachments");

                entity.Property(e => e.AutoSubmitted)
                    .HasColumnName("auto_submitted");

                entity.Property(e => e.Content)
                    .HasColumnName("content");

                entity.Property(e => e.ContentType)
                    .HasColumnName("content_type");

                entity.Property(e => e.CreationDate)
                    .HasColumnName("creation_date")
                    .HasColumnType("timestamptz");

                entity.HasIndex(e => e.CreationDate)
                    .HasDatabaseName("idx_creation_date");

                entity.Property(e => e.Reciever)
                    .HasColumnName("reciever");

                entity.Property(e => e.ReplyTo)
                    .HasColumnName("reply_to");

                entity.Property(e => e.Sender)
                    .HasColumnName("sender");

                entity.Property(e => e.SenderType)
                    .HasColumnName("sender_type");

                entity.Property(e => e.Subject)
                    .HasColumnName("subject");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            });

        }
    }
}