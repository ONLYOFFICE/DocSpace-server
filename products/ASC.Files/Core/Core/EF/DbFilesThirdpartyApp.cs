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

public class DbFilesThirdpartyApp : BaseEntity, IDbFile
{
    public Guid UserId { get; set; }
    [MaxLength(50)]
    public string App { get; set; }
    public string Token { get; set; }
    public int TenantId { get; set; }
    public DateTime ModifiedOn { get; set; }

    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [UserId, App];
    }
}

public static class DbFilesThirdpartyAppExtension
{
    public static ModelBuilderWrapper AddDbDbFilesThirdpartyApp(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<DbFilesThirdpartyApp>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddDbFilesThirdpartyApp, Provider.MySql)
            .Add(PgSqlAddDbFilesThirdpartyApp, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbFilesThirdpartyApp()
        {
            modelBuilder.Entity<DbFilesThirdpartyApp>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.App })
                    .HasName("PRIMARY");

                entity.ToTable("files_thirdparty_app")
                    .HasCharSet("utf8");

                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasColumnType("varchar(38)")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.App)
                    .HasColumnName("app")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ModifiedOn)
                    .HasColumnName("modified_on")
                    .HasColumnType("timestamp");

                entity.Property(e => e.TenantId).HasColumnName("tenant_id");

                entity.Property(e => e.Token)
                    .HasColumnName("token")
                    .HasColumnType("text")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");
            });
        }

        public void PgSqlAddDbFilesThirdpartyApp()
        {
            modelBuilder.Entity<DbFilesThirdpartyApp>(entity =>
            {
                // Define the composite key for PostgreSQL
                entity.HasKey(e => new { e.UserId, e.App })
                    .HasName("pk_files_thirdparty_app");

                // Map the table name
                entity.ToTable("files_thirdparty_app");

                // Map the UserId property
                entity.Property(e => e.UserId)
                    .HasColumnName("user_id")
                    .HasColumnType("uuid"); // PostgreSQL uses 'uuid' for GUIDs

                // Map the App property
                entity.Property(e => e.App)
                    .HasColumnName("app")
                    .HasColumnType("character varying (50)"); // VARCHAR with max length 50

                // Map the Token property
                entity.Property(e => e.Token)
                    .HasColumnName("token")
                    .HasColumnType("text"); // TEXT type in PostgreSQL

                // Map the TenantId property
                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id");

                // Map the ModifiedOn property
                entity.Property(e => e.ModifiedOn)
                    .HasColumnName("modified_on")
                    .HasColumnType("timestamptz"); // TIMESTAMP type in PostgreSQL
            });
        }
    }
}