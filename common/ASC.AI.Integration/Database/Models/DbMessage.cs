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
