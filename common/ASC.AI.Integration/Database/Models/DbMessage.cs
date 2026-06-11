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

namespace ASC.AI.Integration.Database.Models;

public class DbMessage : BaseEntity
{
    public Guid Id { get; init; }
    public int TenantId { get; init; }
    public Guid ThreadId { get; init; }

    [Required]
    public required string Contents { get; set; }

    public DateTime Timestamp { get; set; }

    public DbThread Thread { get; init; } = null!;

    public override object[] GetKeys()
    {
        return [TenantId, Id];
    }
}

public static class DbMessageExtension
{
    public static ModelBuilderWrapper AddDbMessages(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbMessage>().Navigation(e => e.Thread).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbMessages, Provider.MySql)
            .Add(PgSqlAddDbMessages, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbMessages(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMessage>(entity =>
        {
            entity.ToTable("ai_integration_messages")
                .HasCharSet("utf8");

            entity.HasKey(e => new { e.TenantId, e.Id })
                .HasName("PRIMARY");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.ThreadId)
                .HasColumnName("thread_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Contents)
                .HasColumnName("contents")
                .HasColumnType("json");

            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp")
                .HasColumnType("datetime(6)");

            entity.HasOne(e => e.Thread)
                .WithMany()
                .HasForeignKey(e => new { e.TenantId, e.ThreadId })
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ai_integration_messages_thread");

            entity.HasIndex(e => new { e.TenantId, e.ThreadId, e.Timestamp })
                .HasDatabaseName("IX_tenant_id_thread_id_timestamp");
        });
    }

    public static void PgSqlAddDbMessages(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMessage>(entity =>
        {
            entity.ToTable("ai_integration_messages");

            entity.HasKey(e => new { e.TenantId, e.Id })
                .HasName("pk_ai_integration_messages");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("integer");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            entity.Property(e => e.ThreadId)
                .HasColumnName("thread_id")
                .HasColumnType("uuid");

            entity.Property(e => e.Contents)
                .HasColumnName("contents")
                .HasColumnType("jsonb");

            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(e => e.Thread)
                .WithMany()
                .HasForeignKey(e => new { e.TenantId, e.ThreadId })
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ai_integration_messages_thread");

            entity.HasIndex(e => new { e.TenantId, e.ThreadId, e.Timestamp })
                .HasDatabaseName("IX_ai_integration_messages_tenant_id_thread_id_timestamp");
        });
    }
}
