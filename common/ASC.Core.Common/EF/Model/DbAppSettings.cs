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

namespace ASC.Core.Common.EF.Model;

public class DbAppSettings : BaseEntity
{
    public int TenantId { get; set; }

    [MaxLength(64)]
    public string Id { get; set; }

    public bool Enabled { get; set; }

    public string Settings { get; set; }

    public DateTime LastModified { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId, Id];
    }
}

public static class AppSettingsExtension
{
    public static ModelBuilderWrapper AddAppSettings(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbAppSettings>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddAppSettings, Provider.MySql)
            .Add(PgSqlAddAppSettings, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddAppSettings()
        {
            modelBuilder.Entity<DbAppSettings>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.Id })
                    .HasName("PRIMARY");

                entity.ToTable("app_settings")
                    .HasCharSet("utf8");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("varchar(64)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Enabled)
                    .HasColumnName("enabled")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Settings)
                    .HasColumnName("settings")
                    .HasColumnType("json");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("datetime");
            });
        }

        public void PgSqlAddAppSettings()
        {
            modelBuilder.Entity<DbAppSettings>(entity =>
            {
                entity.HasKey(e => new { e.TenantId, e.Id })
                    .HasName("PK_app_settings");

                entity.ToTable("app_settings");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("character varying")
                    .HasMaxLength(64);

                entity.Property(e => e.Enabled)
                    .HasColumnName("enabled")
                    .HasDefaultValue(false);

                entity.Property(e => e.Settings)
                    .HasColumnName("settings")
                    .HasColumnType("jsonb");

                entity.Property(e => e.LastModified)
                    .HasColumnName("last_modified")
                    .HasColumnType("timestamptz");
            });
        }
    }
}
