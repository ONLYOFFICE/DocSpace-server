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

public class DbAssignment : BaseEntity
{
    public Guid Id { get; init; }
    public int TenantId { get; init; }

    public required ActionType ActionType { get; init; }

    public Guid ProfileId { get; init; }
    public int? EntryId { get; init; }
    public DateTime CreatedAt { get; init; }

    public DbTenant Tenant { get; init; } = null!;
    public DbProfile Profile { get; init; } = null!;

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbAssignmentExtension
{
    public static ModelBuilderWrapper AddDbAssignments(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbAssignment>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder.Entity<DbAssignment>().Navigation(e => e.Profile).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbAssignments, Provider.MySql)
            .Add(PgSqlAddDbAssignments, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbAssignments(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbAssignment>(entity =>
        {
            entity.ToTable("ai_integration_assignments")
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

            entity.Property(e => e.ActionType)
                .HasColumnName("action_type")
                .HasColumnType("int");

            entity.Property(e => e.ProfileId)
                .HasColumnName("profile_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.EntryId)
                .HasColumnName("entry_id")
                .HasColumnType("int");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime");

            entity.HasIndex(e => new { e.TenantId, e.ActionType, e.EntryId })
                .HasDatabaseName("IX_tenant_id_action_type_entry_id");
        });
    }

    public static void PgSqlAddDbAssignments(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbAssignment>(entity =>
        {
            entity.ToTable("ai_integration_assignments");

            entity.HasKey(e => e.Id)
                .HasName("pk_ai_integration_assignments");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("integer");

            entity.Property(e => e.ActionType)
                .HasColumnName("action_type")
                .HasColumnType("integer");

            entity.Property(e => e.ProfileId)
                .HasColumnName("profile_id")
                .HasColumnType("uuid");

            entity.Property(e => e.EntryId)
                .HasColumnName("entry_id")
                .HasColumnType("integer");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp without time zone");

            entity.HasIndex(e => new { e.TenantId, e.ActionType, e.EntryId })
                .HasDatabaseName("ix_ai_integration_assignments_tenant_id_action_type_entry_id");
        });
    }
}
