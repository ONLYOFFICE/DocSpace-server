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

namespace ASC.Core.Common.EF.Model;

public class MobileAppInstall
{
    [MaxLength(255)]
    public string UserEmail { get; set; }
    public int AppType { get; set; }
    public DateTime RegisteredOn { get; set; }
    public DateTime? LastSign { get; set; }
}

public static class MobileAppInstallExtension
{
    public static ModelBuilderWrapper AddMobileAppInstall(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddMobileAppInstall, Provider.MySql)
            .Add(PgSqlAddMobileAppInstall, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddMobileAppInstall()
        {
            modelBuilder.Entity<MobileAppInstall>(entity =>
            {
                entity.HasKey(e => new { e.UserEmail, e.AppType })
                    .HasName("PRIMARY");

                entity.ToTable("mobile_app_install")
                    .HasCharSet("utf8");

                entity.Property(e => e.UserEmail)
                    .HasColumnName("user_email")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.AppType)
                    .HasColumnName("app_type");

                entity.Property(e => e.LastSign)
                    .HasColumnName("last_sign")
                    .IsRequired(false)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("NULL");

                entity.Property(e => e.RegisteredOn)
                    .HasColumnName("registered_on")
                    .HasColumnType("datetime");
            });
        }

        public void PgSqlAddMobileAppInstall()
        {
            modelBuilder.Entity<MobileAppInstall>(entity =>
            {
                // Define the composite primary key for PostgreSQL
                entity.HasKey(e => new { e.UserEmail, e.AppType })
                    .HasName("pk_mobile_app_install");

                // Map the table name in PostgreSQL
                entity.ToTable("mobile_app_install");

                // Configure the UserEmail property
                entity.Property(e => e.UserEmail)
                    .HasColumnName("user_email")
                    .HasColumnType("varchar") // Use varchar for strings
                    .HasMaxLength(255) // Respect the MaxLength attribute
                    .IsRequired(); // Ensure it is not null

                // Configure the AppType property
                entity.Property(e => e.AppType)
                    .HasColumnName("app_type")
                    .IsRequired(); // Ensure it is not null

                // Configure the RegisteredOn property
                entity.Property(e => e.RegisteredOn)
                    .HasColumnName("registered_on")
                    .HasColumnType("timestamptz") // Use timestamp for date-time in PostgreSQL
                    .IsRequired(); // Ensure it is not null

                // Configure the LastSign property
                entity.Property(e => e.LastSign)
                    .HasColumnName("last_sign")
                    .HasColumnType("timestamptz") // Optional date-time column
                    .IsRequired(false) // Not required (nullable)
                    .HasDefaultValue(null); // Default value is NULL
            });
        }
    }
}