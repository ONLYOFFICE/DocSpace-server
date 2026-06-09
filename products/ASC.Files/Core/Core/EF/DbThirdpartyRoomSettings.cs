// (c) Copyright Ascensio System SIA 2009-2026
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

public class DbThirdpartyRoomSettings
{
    [MaxLength(32)]
    public string HashId { get; set; }

    public int TenantId { get; set; }
    public bool Indexing { get; set; }
    public bool DenyDownload { get; set; }
    public DbRoomWatermark Watermark { get; set; }
    public DbRoomDataLifetime Lifetime { get; set; }
    public bool SendFormToExternalDB { get; set; }
    public bool SaveFormAsXLSX { get; set; }

    public DbTenant Tenant { get; set; }
}

public static class DbThirdpartyRoomSettingsExtension
{
    public static ModelBuilderWrapper AddDbThirdpartyRoomSettings(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbThirdpartyRoomSettings>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbThirdpartyRoomSettings, Provider.MySql)
            .Add(PgSqlAddDbThirdpartyRoomSettings, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        private void MySqlAddDbThirdpartyRoomSettings()
        {
            modelBuilder.Entity<DbThirdpartyRoomSettings>(entity =>
            {
                entity.ToTable("files_thirdparty_room_settings")
                    .HasCharSet("utf8");

                entity.HasKey(e => new { e.TenantId, e.HashId })
                    .HasName("primary");

                entity.Property(e => e.HashId)
                    .HasColumnName("hash_id")
                    .HasColumnType("char")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Indexing)
                    .HasColumnName("indexing")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.DenyDownload)
                    .HasColumnName("deny_download")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Watermark)
                    .HasColumnName("watermark")
                    .HasColumnType("json")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Lifetime)
                    .HasColumnName("lifetime")
                    .HasColumnType("json")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.SendFormToExternalDB)
                    .HasColumnName("send_form_to_external_db")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.SaveFormAsXLSX)
                    .HasColumnName("save_form_as_xlsx")
                    .HasDefaultValueSql("'1'");
            });
        }

        private void PgSqlAddDbThirdpartyRoomSettings()
        {
            modelBuilder.Entity<DbThirdpartyRoomSettings>(entity =>
            {
                entity.ToTable("files_thirdparty_room_settings");

                entity.HasKey(e => new { e.TenantId, e.HashId })
                    .HasName("pk_files_thirdparty_room_settings");

                entity.Property(e => e.HashId)
                    .HasColumnName("hash_id")
                    .HasColumnType("char(32)");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Indexing)
                    .HasColumnName("indexing")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.DenyDownload)
                    .HasColumnName("deny_download")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.Watermark)
                    .HasColumnName("watermark")
                    .HasColumnType("jsonb");

                entity.Property(e => e.Lifetime)
                    .HasColumnName("lifetime")
                    .HasColumnType("jsonb");

                entity.Property(e => e.SendFormToExternalDB)
                    .HasColumnName("send_form_to_external_db")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.SaveFormAsXLSX)
                    .HasColumnName("save_form_as_xlsx")
                    .HasDefaultValueSql("true");
            });
        }
    }
}
