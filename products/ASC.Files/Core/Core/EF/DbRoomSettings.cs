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

public class DbRoomSettings
{
    public int RoomId { get; set; }
    public int TenantId { get; set; }
    public bool Private { get; set; }
    public bool HasLogo { get; set; }
    public string Color { get; set; }
    public bool Indexing { get; set; }
    public long Quota { get; set; }
    public string Watermark { get; set; }
    public DbTenant Tenant { get; set; }
    public DbFolder Room { get; set; }
}

public static class DbRoomSettingsExtension
{
    public static ModelBuilderWrapper AddDbRoomSettings(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbRoomSettings>().Navigation(e => e.Tenant).AutoInclude(false);
        modelBuilder.Entity<DbRoomSettings>().Navigation(e => e.Room).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbRoomSettings, Provider.MySql)
            .Add(PgSqlAddDbRoomSettings, Provider.PostgreSql);

        return modelBuilder;
    }

    private static void MySqlAddDbRoomSettings(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbRoomSettings>(entity =>
        {
            entity.ToTable("files_room_settings")
                .HasCharSet("utf8");

            entity.HasKey(e => new { e.TenantId, e.RoomId })
                .HasName("primary");

            entity.Property(e => e.RoomId).HasColumnName("room_id");

            entity.Property(e => e.Private)
                .HasColumnName("private")
                .HasDefaultValueSql("'0'");

            entity.Property(e => e.HasLogo).HasColumnName("has_logo").HasDefaultValueSql("0");
            
            entity.Property(e => e.Indexing).HasColumnName("indexing").HasDefaultValueSql("0");

            entity.Property(e => e.Watermark).HasColumnName("watermark").HasColumnType("text");

            entity.Property(e => e.Color)
                .HasColumnName("color")
                .HasColumnType("char(6)")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.TenantId).HasColumnName("tenant_id");

            entity.Property(e => e.Quota)
                .HasColumnName("quota")
                .HasDefaultValueSql("'-2'");
        });
    }

    private static void PgSqlAddDbRoomSettings(this ModelBuilder modelBuilder)
    {
        throw new NotImplementedException();
    }
}