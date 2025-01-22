// (c) Copyright Ascensio System SIA 2009-2024
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

public class DbFilesFormRoleMapping : BaseEntity, IDbFile
{
    public int TenantId { get; set; }
    public int FormId { get; set; }
    public int RoomId { get; set; }
    public int RoleId { get; set; }
    public Guid UserId { get; set; }
    [MaxLength(255)]
    public string RoleName { get; set; }
    public int Sequence { get; set; }
    public bool Submitted { get; set; }

    public DbTenant Tenant { get; set; }
    public DbFolder Room { get; set; }
    public override object[] GetKeys()
    {
        return [TenantId, FormId, RoomId, RoleId, UserId];
    }
}

public static class DbFilesFormRoleMappingExtension
{
    public static ModelBuilderWrapper AddDbFilesFormRoleMapping(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesFormRoleMapping>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder.Entity<DbFilesFormRoleMapping>().Navigation(e => e.Room).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesFormRoleMapping, Provider.MySql)
            .Add(PgSqlAddDbFilesFormRoleMapping, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbFilesFormRoleMapping(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFilesFormRoleMapping>(entity =>
        {
            entity.HasKey(e => new { e.TenantId, e.FormId, e.RoomId, e.RoleId, e.UserId })
                .HasName("PRIMARY");

            entity.ToTable("files_form_role_mapping")
                .HasCharSet("utf8");

            entity.HasIndex(e => new { e.TenantId, e.RoomId, e.FormId})
               .HasDatabaseName("tenant_id_room_id_form_id");

            entity.HasIndex(e => new { e.TenantId, e.RoomId, e.FormId, e.UserId })
                .HasDatabaseName("tenant_id_room_id_form_id_user_id");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.FormId).HasColumnName("form_id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasColumnType("varchar(38)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.RoleName)
               .IsRequired()
               .HasColumnName("role_name")
               .HasColumnType("varchar")
               .HasCharSet("utf8")
               .UseCollation("utf8_general_ci");

            entity.Property(e => e.Sequence).HasColumnName("sequence");

            entity.Property(e => e.Submitted)
                .HasColumnName("submitted")
                .HasColumnType("tinyint(1)");

        });
    }
    public static void PgSqlAddDbFilesFormRoleMapping(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFilesFormRoleMapping>(entity =>
        {
            entity.HasKey(e => new { e.TenantId, e.FormId, e.RoomId, e.RoleId, e.UserId })
                .HasName("files_form_role_mapping_pkey");

            entity.ToTable("files_form_role_mapping", "onlyoffice");

            entity.HasIndex(e => new { e.TenantId, e.RoomId, e.FormId })
                .HasDatabaseName("tenant_id_room_id_form_id");

            entity.HasIndex(e => new { e.TenantId, e.RoomId, e.FormId, e.UserId })
                .HasDatabaseName("tenant_id_room_id_form_id_user_id");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            entity.Property(e => e.FormId).HasColumnName("form_id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(38);

            entity.Property(e => e.RoleName).HasColumnName("role_name");
            entity.Property(e => e.Sequence).HasColumnName("sequence");
            entity.Property(e => e.Submitted).HasColumnName("submitted");

        });
    }
}
