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

namespace ASC.Webhooks.Core.EF.Model;

public class DbWebhooksLog
{
    public int Id { get; set; }
    public int ConfigId { get; set; }
    public WebhookTrigger Trigger { get; set; }
    public DateTime CreationTime { get; set; }
    public int WebhookId { get; set; } // TODO: Deprecated
    public string RequestHeaders { get; set; }
    public string RequestPayload { get; set; }
    public string ResponseHeaders { get; set; }
    public string ResponsePayload { get; set; }
    public int Status { get; set; }
    public int TenantId { get; set; }
    public Guid Uid { get; set; }
    public DateTime? Delivery { get; set; }
    public int Attempts { get; set; }
    public DateTime? NextAttemptOn { get; set; }
    // Set by the worker instance that claimed the row for delivery, cleared when that attempt is recorded.
    // While it is set and NextAttemptOn is in the future, an instance is delivering the row right now.
    public Guid? LeaseId { get; set; }
    // A manual retry is a row of its own that points to the original row of the event (never to another retry),
    // so the earlier outcome stays in the log. The receiver sees the original row's id as the event id either way.
    public int? RetryOf { get; set; }
    public Guid? RetriedBy { get; set; }
    public int EventId => RetryOf ?? Id;
    public DbWebhooksConfig Config { get; set; }
    public DbTenant Tenant { get; set; }
}

public static class WebhooksPayloadExtension
{
    public static ModelBuilderWrapper AddWebhooksLog(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbWebhooksLog>().Navigation(e => e.Tenant).AutoInclude();

        modelBuilder
            .Add(MySqlAddWebhooksLog, Provider.MySql)
            .Add(PgSqlAddWebhooksLog, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        private void MySqlAddWebhooksLog()
        {
            modelBuilder.Entity<DbWebhooksLog>(entity =>
            {
                entity.HasKey(e => new { e.Id })
                    .HasName("PRIMARY");

                entity.ToTable("webhooks_logs")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("tenant_id");

                entity.HasIndex(e => e.NextAttemptOn)
                    .HasDatabaseName("next_attempt_on");

                // the retention of webhooks:journalRetentionDays walks the oldest rows (ExpiredWebhooksLogIdsAsync)
                entity.HasIndex(e => e.CreationTime)
                    .HasDatabaseName("creation_time");

                entity.Property(e => e.Id)
                    .HasColumnType("int")
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ConfigId)
                    .HasColumnType("int")
                    .HasColumnName("config_id");

                entity.Property(e => e.Trigger)
                    .HasColumnName("trigger")
                    .HasColumnType("bigint")
                    .IsRequired();

                entity.Property(e => e.Uid)
                    .HasColumnName("uid")
                    .HasColumnType("varchar(36)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id");

                entity.Property(e => e.RequestPayload)
                    .IsRequired()
                    .HasColumnName("request_payload")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.RequestHeaders)
                    .HasColumnName("request_headers")
                    .HasColumnType("json");

                entity.Property(e => e.ResponsePayload)
                    .HasColumnName("response_payload")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ResponseHeaders)
                    .HasColumnName("response_headers")
                    .HasColumnType("json");

                entity.Property(e => e.WebhookId)
                    .HasColumnType("int")
                    .HasColumnName("webhook_id")
                    .IsRequired();

                entity.Property(e => e.CreationTime)
                    .HasColumnType("datetime")
                    .HasColumnName("creation_time");

                entity.Property(e => e.Delivery)
                    .HasColumnType("datetime")
                    .HasColumnName("delivery");

                entity.Property(e => e.Status)
                    .HasColumnType("int")
                    .HasColumnName("status");

                entity.Property(e => e.Attempts)
                    .HasColumnType("int")
                    .HasColumnName("attempts")
                    .HasDefaultValue(0);

                entity.Property(e => e.NextAttemptOn)
                    .HasColumnType("datetime")
                    .HasColumnName("next_attempt_on");

                entity.Property(e => e.LeaseId)
                    .HasColumnName("lease_id")
                    .HasColumnType("varchar(36)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.HasIndex(e => new { e.TenantId, e.RetryOf })
                    .HasDatabaseName("tenant_id_retry_of");

                entity.Property(e => e.RetryOf)
                    .HasColumnType("int")
                    .HasColumnName("retry_of");

                entity.Property(e => e.RetriedBy)
                    .HasColumnName("retried_by")
                    .HasColumnType("varchar(36)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Ignore(e => e.EventId);
            });
        }

        private void PgSqlAddWebhooksLog()
        {
            modelBuilder.Entity<DbWebhooksLog>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("pk_webhooks_logs");

                entity.ToTable("webhooks_logs");

                // Specify the columns and their mappings
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ConfigId)
                    .HasColumnName("config_id");

                entity.Property(e => e.Trigger)
                    .HasColumnName("trigger")
                    .HasColumnType("bigint")
                    .IsRequired();

                entity.Property(e => e.Uid)
                    .HasColumnName("uid");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id");

                entity.Property(e => e.RequestPayload)
                    .HasColumnName("request_payload")
                    .HasColumnType("text");

                entity.Property(e => e.RequestHeaders)
                    .HasColumnName("request_headers")
                    .HasColumnType("json");

                entity.Property(e => e.ResponsePayload)
                    .HasColumnName("response_payload")
                    .HasColumnType("text");

                entity.Property(e => e.ResponseHeaders)
                    .HasColumnName("response_headers")
                    .HasColumnType("json");

                entity.Property(e => e.WebhookId)
                    .IsRequired()
                    .HasColumnName("webhook_id");

                entity.Property(e => e.CreationTime)
                    .HasColumnName("creation_time");

                entity.Property(e => e.Delivery)
                    .HasColumnName("delivery");

                entity.Property(e => e.Status)
                    .HasColumnName("status");

                entity.Property(e => e.Attempts)
                    .HasColumnName("attempts")
                    .HasDefaultValue(0);

                entity.Property(e => e.NextAttemptOn)
                    .HasColumnName("next_attempt_on");

                entity.Property(e => e.LeaseId)
                    .HasColumnName("lease_id");

                entity.Property(e => e.RetryOf)
                    .HasColumnName("retry_of");

                entity.Property(e => e.RetriedBy)
                    .HasColumnName("retried_by");

                entity.Ignore(e => e.EventId);

                // Add indexes (PostgreSQL-specific naming)
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("ix_webhooks_logs_tenant_id");

                entity.HasIndex(e => e.NextAttemptOn)
                    .HasDatabaseName("ix_webhooks_logs_next_attempt_on");

                entity.HasIndex(e => e.CreationTime)
                    .HasDatabaseName("ix_webhooks_logs_creation_time");

                entity.HasIndex(e => new { e.TenantId, e.RetryOf })
                    .HasDatabaseName("ix_webhooks_logs_tenant_id_retry_of");

                // Relationships (optional depending upon requirements)
                entity.HasOne(e => e.Config)
                    .WithMany()
                    .HasForeignKey(e => e.ConfigId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Tenant)
                    .WithMany()
                    .HasForeignKey(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}