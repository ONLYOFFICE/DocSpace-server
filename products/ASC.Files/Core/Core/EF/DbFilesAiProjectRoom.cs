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

public class DbFilesAiProjectRoom : BaseEntity
{
    public int TenantId { get; set; }
    public int ProjectId { get; set; }
    public int RoomId { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, ProjectId, RoomId];
    }
}

public static class DbFilesAiProjectRoomExtension
{
    public static ModelBuilderWrapper AddDbFilesAiProjectRoom(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesAiProjectRoom>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesAiProjectRoom, Provider.MySql)
            .Add(PgSqlAddDbFilesAiProjectRoom, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesAiProjectRoom()
        {
            modelBuilder.Entity<DbFilesAiProjectRoom>(entity =>
            {
                entity.ToTable("files_ai_project_room")
                    .HasCharSet("utf8");

                entity.HasKey(e => new { e.TenantId, e.ProjectId, e.RoomId })
                    .HasName("PRIMARY");

                entity.HasIndex(e => new { e.TenantId, e.RoomId })
                    .HasDatabaseName("IX_tenant_id_room_id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.ProjectId).HasColumnName("project_id");

                entity.Property(e => e.RoomId).HasColumnName("room_id");
            });
        }

        public void PgSqlAddDbFilesAiProjectRoom()
        {
            modelBuilder.Entity<DbFilesAiProjectRoom>(entity =>
            {
                entity.ToTable("files_ai_project_room");

                entity.HasKey(e => new { e.TenantId, e.ProjectId, e.RoomId })
                    .HasName("pk_files_ai_project_room");

                entity.HasIndex(e => new { e.TenantId, e.RoomId })
                    .HasDatabaseName("ix_files_ai_project_room_tenant_id_room_id");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.ProjectId).HasColumnName("project_id");

                entity.Property(e => e.RoomId).HasColumnName("room_id");
            });
        }
    }
}
