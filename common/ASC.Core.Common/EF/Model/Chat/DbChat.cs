// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Core.Common.EF.Model.Chat;

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
            
            entity.HasIndex(e => new { e.TenantId, e.Id })
                .HasDatabaseName("IX_tenant_id_id");
            
            entity.HasIndex(e => new { e.TenantId, e.RoomId, e.UserId, e.ModifiedOn })
                .HasDatabaseName("IX_tenant_id_room_id_user_id_modified_on");
        });
    }
}