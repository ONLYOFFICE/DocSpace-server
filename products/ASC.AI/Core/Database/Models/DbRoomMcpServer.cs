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

namespace ASC.AI.Core.Database.Models;

public class DbRoomMcpServer : BaseEntity
{
    public int TenantId { get; init; }
    public int RoomId { get; init; }
    public Guid ServerId { get; init; }
    
    public DbFolder Room { get; init; } = null!;
    public DbTenant Tenant { get; init; } = null!;

    public override object[] GetKeys()
    {
        return [TenantId, RoomId, ServerId];
    }
}

public static class DbMcpRoomMapExtensions
{
    public static ModelBuilderWrapper AddDbRoomMcpServers(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbRoomMcpServer>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder.Entity<DbRoomMcpServer>().Navigation(e => e.Room).AutoInclude(false);
        return modelBuilder.Add(MySqlAddMcpRoomMap, ASC.Core.Common.EF.Provider.MySql);
    }

    private static void MySqlAddMcpRoomMap(ModelBuilder builder)
    {
        builder.Entity<DbRoomMcpServer>(entity =>
        {
            entity.ToTable("ai_mcp_room_servers")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
            
            entity.HasKey(e => new { e.TenantId, e.RoomId, e.ServerId } )
                .HasName("PRIMARY");
            
            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");
            
            entity.Property(e => e.RoomId)
                .HasColumnName("room_id");
            
            entity.Property(e => e.ServerId)
                .HasColumnName("server_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
        });
    }
}