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