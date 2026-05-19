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

namespace ASC.Files.Core.EF;

public class DbFilesRoomGroup : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int GroupId { get; set; }
    public int? InternalRoomId { get; set; }
    public string ThirdpartyRoomId { get; set; }

    public DbTenant Tenant { get; set; }
    public DbFilesGroup Group { get; set; }
    public DbFolder InternalRoom { get; set; }

    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbFilesRoomGroupExtension
{
    public static ModelBuilderWrapper AddDbFilesRoomGroup(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesRoomGroup>()
            .Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder.Entity<DbFilesRoomGroup>()
            .Navigation(e => e.Group).AutoInclude(false);

        modelBuilder.Entity<DbFilesRoomGroup>()
            .Navigation(e => e.InternalRoom).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesRoomGroup, Provider.MySql)
            .Add(PgSqlAddDbFilesRoomGroup, Provider.PostgreSql);

        return modelBuilder;
    }

    private static void MySqlAddDbFilesRoomGroup(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFilesRoomGroup>(entity =>
        {
            entity.ToTable("files_roomgroup")
                .HasCharSet("utf8");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.InternalRoomId).HasColumnName("internal_room_id");

            entity.Property(e => e.ThirdpartyRoomId)
                .HasColumnName("thirdparty_room_id")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .HasMaxLength(200)
                .UseCollation("utf8_general_ci");

            entity.HasIndex(e => e.GroupId).HasDatabaseName("idx_group");
            entity.HasIndex(e => e.InternalRoomId).HasDatabaseName("idx_internal_room");
            entity.HasIndex(e => e.ThirdpartyRoomId).HasDatabaseName("idx_thirdparty_room");

            entity.HasIndex(e => new { e.TenantId, e.GroupId, e.InternalRoomId })
                .IsUnique()
                .HasDatabaseName("uq_roomgroup_internal");

            entity.HasIndex(e => new { e.TenantId, e.GroupId, e.ThirdpartyRoomId })
                .IsUnique()
                .HasDatabaseName("uq_roomgroup_thirdparty");
        });
    }

    private static void PgSqlAddDbFilesRoomGroup(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFilesRoomGroup>(entity =>
        {
            entity.ToTable("files_roomgroup");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasColumnType("integer");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("integer");

            entity.Property(e => e.GroupId)
                .HasColumnName("group_id")
                .HasColumnType("integer");

            entity.Property(e => e.InternalRoomId)
                .HasColumnName("internal_room_id")
                .HasColumnType("integer");

            entity.Property(e => e.ThirdpartyRoomId)
                .HasColumnName("thirdparty_room_id")
                .HasColumnType("varchar(200)");

            entity.HasIndex(e => e.GroupId).HasDatabaseName("idx_group");
            entity.HasIndex(e => e.InternalRoomId).HasDatabaseName("idx_internal_room");
            entity.HasIndex(e => e.ThirdpartyRoomId).HasDatabaseName("idx_thirdparty_room");

            entity.HasIndex(e => new { e.TenantId, e.GroupId, e.InternalRoomId })
                .IsUnique()
                .HasDatabaseName("uq_roomgroup_internal");

            entity.HasIndex(e => new { e.TenantId, e.GroupId, e.ThirdpartyRoomId })
                .IsUnique()
                .HasDatabaseName("uq_roomgroup_thirdparty");
        });
    }
}