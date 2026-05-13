// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.AI.Integration.Database.Models;

public class DbAttachment : BaseEntity
{
    public Guid Id { get; init; }
    public int TenantId { get; init; }

    public AttachmentKind Kind { get; init; }

    [MaxLength(255)]
    [Required]
    public required string Title { get; init; }

    public string? Content { get; init; }

    public Guid? MessageId { get; set; }
    public Guid? ThreadId { get; set; }
    public int? EntryId { get; init; }

    [MaxLength(32)]
    public string? ThirdpartyEntryId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DbTenant Tenant { get; init; } = null!;
    public DbThread? Thread { get; init; }
    public DbMessage? Message { get; init; }

    public override object[] GetKeys()
    {
        return [TenantId, Id];
    }
}

public static class DbAttachmentExtension
{
    public static ModelBuilderWrapper AddDbAttachments(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbAttachment>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder.Entity<DbAttachment>().Navigation(e => e.Thread).AutoInclude(false);
        modelBuilder.Entity<DbAttachment>().Navigation(e => e.Message).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbAttachments, Provider.MySql)
            .Add(PgSqlAddDbAttachments, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbAttachments(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbAttachment>(entity =>
        {
            entity.ToTable("ai_integration_attachments")
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

            entity.Property(e => e.Kind)
                .HasColumnName("kind")
                .HasColumnType("int");

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasColumnType("varchar(255)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .HasColumnType("longtext")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.MessageId)
                .HasColumnName("message_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.ThreadId)
                .HasColumnName("thread_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.EntryId)
                .HasColumnName("entry_id")
                .HasColumnType("int");

            entity.Property(e => e.ThirdpartyEntryId)
                .HasColumnName("thirdparty_entry_id")
                .HasColumnType("char(32)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime");

            entity.HasOne(e => e.Thread)
                .WithMany()
                .HasForeignKey(e => new { e.TenantId, e.ThreadId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Message)
                .WithMany()
                .HasForeignKey(e => new { e.TenantId, e.MessageId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.MessageId })
                .HasDatabaseName("IX_tenant_id_message_id");

            entity.HasIndex(e => new { e.TenantId, e.ThreadId })
                .HasDatabaseName("IX_tenant_id_thread_id");

            entity.HasIndex(e => new { e.TenantId, e.EntryId })
                .HasDatabaseName("IX_tenant_id_entry_id");

            entity.HasIndex(e => new { e.TenantId, e.ThirdpartyEntryId })
                .HasDatabaseName("IX_tenant_id_thirdparty_entry_id");
        });
    }

    public static void PgSqlAddDbAttachments(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbAttachment>(entity =>
        {
            entity.ToTable("ai_integration_attachments");

            entity.HasKey(e => new { e.TenantId, e.Id })
                .HasName("pk_ai_integration_attachments");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("integer");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            entity.Property(e => e.Kind)
                .HasColumnName("kind")
                .HasColumnType("integer");

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasColumnType("character varying")
                .HasMaxLength(255);

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .HasColumnType("text");

            entity.Property(e => e.MessageId)
                .HasColumnName("message_id")
                .HasColumnType("uuid");

            entity.Property(e => e.ThreadId)
                .HasColumnName("thread_id")
                .HasColumnType("uuid");

            entity.Property(e => e.EntryId)
                .HasColumnName("entry_id")
                .HasColumnType("integer");

            entity.Property(e => e.ThirdpartyEntryId)
                .HasColumnName("thirdparty_entry_id")
                .HasColumnType("char(32)");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(e => e.Thread)
                .WithMany()
                .HasForeignKey(e => new { e.TenantId, e.ThreadId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Message)
                .WithMany()
                .HasForeignKey(e => new { e.TenantId, e.MessageId })
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TenantId, e.MessageId })
                .HasDatabaseName("ix_ai_integration_attachments_tenant_id_message_id");

            entity.HasIndex(e => new { e.TenantId, e.ThreadId })
                .HasDatabaseName("ix_ai_integration_attachments_tenant_id_thread_id");

            entity.HasIndex(e => new { e.TenantId, e.EntryId })
                .HasDatabaseName("ix_ai_integration_attachments_tenant_id_entry_id");

            entity.HasIndex(e => new { e.TenantId, e.ThirdpartyEntryId })
                .HasDatabaseName("ix_ai_integration_attachments_tenant_id_thirdparty_entry_id");
        });
    }
}
