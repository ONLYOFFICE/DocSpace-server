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

public class DbTenantForbiden
{
    [MaxLength(50)]
    public string Address { get; set; }
}

public static class DbTenantForbidenExtension
{
    public static ModelBuilderWrapper AddDbTenantForbiden(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddDbTenantForbiden, Provider.MySql)
            .Add(PgSqlAddDbTenantForbiden, Provider.PostgreSql)
            .HasData(
            new DbTenantForbiden { Address = "controlpanel" },
            new DbTenantForbiden { Address = "localhost" },
            new DbTenantForbiden { Address = "settings" },
            new DbTenantForbiden { Address = "api-system-eu-central-1" },
            new DbTenantForbiden { Address = "api-system-us-east-2" },
            new DbTenantForbiden { Address = "identity-eu-central-1" },
            new DbTenantForbiden { Address = "identity-us-east-2" },
            new DbTenantForbiden { Address = "oauth" }
            );

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbTenantForbiden()
        {
            modelBuilder.Entity<DbTenantForbiden>(entity =>
            {
                entity.HasKey(e => e.Address)
                    .HasName("PRIMARY");

                entity.ToTable("tenants_forbiden")
                    .HasCharSet("utf8");

                entity.Property(e => e.Address)
                    .HasColumnName("address")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");
            });
        }

        public void PgSqlAddDbTenantForbiden()
        {
            modelBuilder.Entity<DbTenantForbiden>(entity =>
            {
                entity.HasKey(e => e.Address)
                    .HasName("PK_tenants_forbiden");

                entity.ToTable("tenants_forbiden");

                entity.Property(e => e.Address)
                    .HasColumnName("address")
                    .HasColumnType("varchar")
                    .IsRequired();
            });
        }
    }
}