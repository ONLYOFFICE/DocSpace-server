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

public class DbPreference : BaseEntity
{
    public Guid Id { get; init; }
    public int TenantId { get; init; }
    public Guid CreatedBy { get; init; }
    public int? EntryId { get; init; }

    public bool? DeepMode { get; init; }

    public DbTenant Tenant { get; init; } = null!;

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbPreferencesExtension
{
    public static ModelBuilderWrapper AddDbPreferences(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbPreference>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbPreferences, Provider.MySql)
            .Add(PgSqlAddDbPreferences, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbPreferences(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbPreference>(entity =>
        {
            entity.ToTable("ai_integration_preferences")
                .HasCharSet("utf8");

            entity.HasKey(e => e.Id)
                .HasName("PRIMARY");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");

            entity.Property(e => e.CreatedBy)
                .HasColumnName("created_by")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.EntryId)
                .HasColumnName("entry_id")
                .HasColumnType("int");

            entity.Property(e => e.DeepMode)
                .HasColumnName("deep_mode")
                .HasColumnType("tinyint(1)");

            entity.HasIndex(e => new { e.TenantId, e.CreatedBy, e.EntryId })
                .HasDatabaseName("IX_tenant_id_created_by_entry_id");
        });
    }

    public static void PgSqlAddDbPreferences(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbPreference>(entity =>
        {
            entity.ToTable("ai_integration_preferences");

            entity.HasKey(e => e.Id)
                .HasName("pk_ai_integration_preferences");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("integer");

            entity.Property(e => e.CreatedBy)
                .HasColumnName("created_by")
                .HasColumnType("uuid");

            entity.Property(e => e.EntryId)
                .HasColumnName("entry_id")
                .HasColumnType("integer");

            entity.Property(e => e.DeepMode)
                .HasColumnName("deep_mode")
                .HasColumnType("boolean");

            entity.HasIndex(e => new { e.TenantId, e.CreatedBy, e.EntryId })
                .HasDatabaseName("ix_ai_integration_preferences_tenant_id_created_by_entry_id");
        });
    }
}
