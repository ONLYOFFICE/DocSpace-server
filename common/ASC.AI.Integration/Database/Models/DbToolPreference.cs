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

public class DbToolPreference : BaseEntity
{
    public Guid Id { get; init; }
    public int TenantId { get; init; }
    public Guid CreatedBy { get; init; }

    [MaxLength(128)]
    [Required]
    public required string ServerType { get; init; }

    public int? EntryId { get; init; }

    public HashSet<string>? Disabled { get; set; }
    public HashSet<string>? AllowAlways { get; set; }

    public DateTime CreatedAt { get; init; }

    public DbTenant Tenant { get; init; } = null!;

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbToolPrefsExtension
{
    public static ModelBuilderWrapper AddDbToolPrefs(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbToolPreference>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbToolPrefs, Provider.MySql)
            .Add(PgSqlAddDbToolPrefs, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbToolPrefs(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbToolPreference>(entity =>
        {
            entity.ToTable("ai_integration_tool_preferences")
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

            entity.Property(e => e.ServerType)
                .HasColumnName("server_type")
                .HasColumnType("varchar(128)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.EntryId)
                .HasColumnName("entry_id")
                .HasColumnType("int");

            entity.PrimitiveCollection(e => e.Disabled)
                .HasColumnName("disabled")
                .HasColumnType("json");

            entity.PrimitiveCollection(e => e.AllowAlways)
                .HasColumnName("allow_always")
                .HasColumnType("json");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime");

            entity.HasIndex(e => new { e.TenantId, e.CreatedBy, e.ServerType, e.EntryId })
                .HasDatabaseName("IX_tenant_id_created_by_server_type_entry_id");
        });
    }

    public static void PgSqlAddDbToolPrefs(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbToolPreference>(entity =>
        {
            entity.ToTable("ai_integration_tool_preferences");

            entity.HasKey(e => e.Id)
                .HasName("pk_ai_integration_tool_preferences");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("integer");

            entity.Property(e => e.CreatedBy)
                .HasColumnName("created_by")
                .HasColumnType("uuid");

            entity.Property(e => e.ServerType)
                .HasColumnName("server_type")
                .HasColumnType("character varying")
                .HasMaxLength(128);

            entity.Property(e => e.EntryId)
                .HasColumnName("entry_id")
                .HasColumnType("integer");

            entity.PrimitiveCollection(e => e.Disabled)
                .HasColumnName("disabled")
                .HasColumnType("jsonb");

            entity.PrimitiveCollection(e => e.AllowAlways)
                .HasColumnName("allow_always")
                .HasColumnType("jsonb");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone");

            entity.HasIndex(e => new { e.TenantId, e.CreatedBy, e.ServerType, e.EntryId })
                .HasDatabaseName("ix_ai_integration_tool_preferences_tenant_id_created_by_server_type_entry_id");
        });
    }
}
