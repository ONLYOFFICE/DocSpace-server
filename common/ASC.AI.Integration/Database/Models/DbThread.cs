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

public class DbThread : BaseEntity
{
    public Guid Id { get; init; }
    public int TenantId { get; init; }

    [MaxLength(255)]
    [Required]
    public required string Title { get; set; }

    public int? ProfileId { get; set; }
    public DateTime LastEditDate { get; set; }
    public DateTime CreatedAt { get; init; }

    public DbTenant Tenant { get; init; } = null!;

    public override object[] GetKeys()
    {
        return [TenantId, Id];
    }
}

public static class DbThreadExtension
{
    public static ModelBuilderWrapper AddDbThreads(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbThread>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbThreads, Provider.MySql)
            .Add(PgSqlAddDbThreads, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbThreads(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbThread>(entity =>
        {
            entity.ToTable("ai_integration_threads")
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

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasColumnType("varchar(255)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.ProfileId)
                .HasColumnName("profile_id");

            entity.Property(e => e.LastEditDate)
                .HasColumnName("last_edit_date")
                .HasColumnType("datetime");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime");

            entity.HasIndex(e => new { e.TenantId, e.LastEditDate })
                .HasDatabaseName("IX_tenant_id_last_edit_date");

            entity.HasIndex(e => new { e.TenantId, e.ProfileId })
                .HasDatabaseName("IX_tenant_id_profile_id");
        });
    }

    public static void PgSqlAddDbThreads(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbThread>(entity =>
        {
            entity.ToTable("ai_integration_threads");

            entity.HasKey(e => new { e.TenantId, e.Id })
                .HasName("pk_ai_integration_threads");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("integer");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasColumnType("character varying")
                .HasMaxLength(255);

            entity.Property(e => e.ProfileId)
                .HasColumnName("profile_id");

            entity.Property(e => e.LastEditDate)
                .HasColumnName("last_edit_date")
                .HasColumnType("timestamp without time zone");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone");

            entity.HasIndex(e => new { e.TenantId, e.LastEditDate })
                .HasDatabaseName("IX_ai_integration_threads_tenant_id_last_edit_date");

            entity.HasIndex(e => new { e.TenantId, e.ProfileId })
                .HasDatabaseName("IX_ai_integration_threads_tenant_id_profile_id");
        });
    }
}
