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

namespace ASC.Core.Common.EF.Model.Ai;

public class DbChat : BaseEntity
{
    public Guid Id { get; set; }
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public Guid UserId { get; set; }
    
    [MaxLength(255)]
    public required string Title { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    public DateTime? DeletedOn { get; set; }

    public DbTenant Tenant { get; set; }
    public List<DbChatMessage> Messages { get; set; }
    
    public override object[] GetKeys()
    {
        return [Id];
    }
}

public static class DbChatSessionExtensions
{
    public static ModelBuilderWrapper AddDbChats(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbChat>().Navigation(x => x.Messages).AutoInclude(false);
        modelBuilder.Entity<DbChat>().Navigation(e => e.Tenant).AutoInclude(false);
        
        modelBuilder.Add(MySqlAddDbChatSession, Provider.MySql);
        
        return modelBuilder;
    }

    private static void MySqlAddDbChatSession(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbChat>(entity =>
        {
            entity.ToTable("ai_chats")
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
            
            entity.Property(e => e.RoomId)
                .HasColumnName("room_id");
            
            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasColumnType("varchar(255)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("char(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.CreatedOn)
                .HasColumnName("created_on")
                .HasColumnType("datetime");
            
            entity.Property(e => e.ModifiedOn)
                .HasColumnName("modified_on")
                .HasColumnType("datetime");

            entity.Property(e => e.DeletedOn)
                .HasColumnName("deleted_on")
                .HasColumnType("datetime");

            entity.HasIndex(e => new { e.TenantId, e.Id })
                .HasDatabaseName("IX_tenant_id_id");

            entity.HasIndex(e => e.DeletedOn)
                .HasDatabaseName("IX_deleted_on");
            
            entity.HasIndex(e => new { e.TenantId, e.RoomId, e.UserId, e.ModifiedOn })
                .HasDatabaseName("IX_tenant_id_room_id_user_id_modified_on");
        });
    }
}