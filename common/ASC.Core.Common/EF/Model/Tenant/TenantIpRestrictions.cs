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

public class TenantIpRestrictions
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    [MaxLength(50)]
    public string Ip { get; set; }
    public bool ForAdmin { get; set; }

    public DbTenant Tenant { get; set; }
}

public static class TenantIpRestrictionsExtension
{
    public static ModelBuilderWrapper AddTenantIpRestrictions(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder.Entity<TenantIpRestrictions>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder
            .Add(MySqlAddTenantIpRestrictions, Provider.MySql)
            .Add(PgSqlAddTenantIpRestrictions, Provider.PostgreSql);

        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddTenantIpRestrictions()
        {
            modelBuilder.Entity<TenantIpRestrictions>(entity =>
            {
                entity.ToTable("tenants_iprestrictions")
                    .HasCharSet("utf8");

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("tenant");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Ip)
                    .IsRequired()
                    .HasColumnName("ip")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ForAdmin)
                    .IsRequired()
                    .HasColumnName("for_admin")
                    .HasColumnType("TINYINT(1)");

                entity.Property(e => e.TenantId).HasColumnName("tenant");
            });
        }

        public void PgSqlAddTenantIpRestrictions()
        {
            modelBuilder.Entity<TenantIpRestrictions>(entity =>
            {
                entity.ToTable("tenants_iprestrictions");

                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("tenant");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Ip)
                    .IsRequired()
                    .HasColumnName("ip")
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.ForAdmin)
                    .IsRequired()
                    .HasColumnName("for_admin")
                    .HasColumnType("boolean");

                entity.Property(e => e.TenantId).HasColumnName("tenant");
            });
        }
    }
}