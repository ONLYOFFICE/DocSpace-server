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

using User = ASC.Core.Common.EF.User;

namespace ASC.Files.Core.EF;

public class DbUserChatSettings : BaseEntity
{
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public Guid UserId { get; set; }
    public bool WebSearchEnabled { get; set; }
    public int? ReasoningEffort { get; set; }

    public DbTenant Tenant { get; set; }
    public User User { get; set; }
    public DbFolder Room { get; set; }
    
    public override object[] GetKeys()
    {
        return [TenantId, RoomId, UserId];
    }
}

public static class DbUserChatSettingsExtensions
{
    public static ModelBuilderWrapper AddDbUserChatSettings(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbUserChatSettings>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder.Entity<DbUserChatSettings>().Navigation(e => e.User).AutoInclude(false);
        modelBuilder.Entity<DbUserChatSettings>().Navigation(e => e.Room).AutoInclude(false);
        
        modelBuilder.Add(MySqlAddDbUserChatSettings, Provider.MySql);
        
        return modelBuilder;
    }

    private static void MySqlAddDbUserChatSettings(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbUserChatSettings>(entity =>
        {
            entity.ToTable("ai_user_chat_settings")
                .HasCharSet("utf8");

            entity.HasKey(e => new { e.TenantId, e.RoomId, e.UserId })
                .HasName("PRIMARY");
            
            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id");
            
            entity.Property(e => e.RoomId)
                .HasColumnName("room_id");
            
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("varchar(36)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
            
            entity.Property(e => e.WebSearchEnabled)
                .HasColumnName("web_search_enabled")
                .HasDefaultValue(true);

            entity.Property(e => e.ReasoningEffort)
                .HasColumnName("reasoning_effort")
                .HasColumnType("tinyint");
        });
    }
}