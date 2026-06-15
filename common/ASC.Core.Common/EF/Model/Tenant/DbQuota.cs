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

namespace ASC.Core.Common.EF;

public class DbQuota : BaseEntity
{
    public int TenantId { get; set; }

    [MaxLength(128)]
    public string Name { get; set; }

    [MaxLength(128)]
    public string Description { get; set; }
    public string Features { get; set; }
    public decimal Price { get; set; }

    [MaxLength(128)]
    public string ProductId { get; set; }

    [MaxLength(128)]
    public string ServiceName { get; set; }

    [MaxLength(128)]
    public string ServiceGroup { get; set; }

    public bool Visible { get; set; }
    public bool Wallet { get; set; }
    public override object[] GetKeys()
    {
        return [TenantId];
    }
    public string GetPaymentId()
    {
        return Wallet && !string.IsNullOrEmpty(ServiceName) ? ServiceName : ProductId;
    }
}
public static class DbQuotaExtension
{
    public static ModelBuilderWrapper AddDbQuota(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddDbQuota, Provider.MySql)
            .Add(PgSqlAddDbQuota, Provider.PostgreSql);

        modelBuilder
            .HasData(
                new DbQuota
                {
                    TenantId = -1,
                    Name = "trial",
                    Description = null,
                    Features = "trial,audit,ldap,sso,customization,thirdparty,restore,oauth,total_size:107374182400,file_size:100,manager:1,statistic,automationapi",
                    Price = 0,
                    ProductId = null,
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -2,
                    Name = "admin",
                    Description = "until 01.04.2024",
                    Features = "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:107374182400,file_size:1024,manager:1,statistic,free_backup:2:fixed,automationapi",
                    Price = 15,
                    ProductId = "1002",
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -3,
                    Name = "startup",
                    Description = null,
                    Features = "free,oauth,total_size:2147483648,manager:3,room:12,automationapi",
                    Price = 0,
                    ProductId = null,
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -4,
                    Name = "disk",
                    Description = null,
                    Features = "total_size:1073741824",
                    Price = 0,
                    ProductId = "1004",
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -5,
                    Name = "admin1",
                    Description = null,
                    Features = "manager:1",
                    Price = 0,
                    ProductId = "1005",
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -6,
                    Name = "subscription",
                    Description = null,
                    Features = "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,file_size:1024,statistic,free_backup:2:fixed,automationapi",
                    Price = 0,
                    ProductId = "1001",
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -7,
                    Name = "nonprofit",
                    Description = null,
                    Features = "non-profit,audit,ldap,sso,thirdparty,restore,oauth,contentsearch,total_size:2147483648,file_size:1024,manager:20,statistic,automationapi",
                    Price = 0,
                    ProductId = "1007",
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -8,
                    Name = "zoom",
                    Description = null,
                    Features = "free,oauth,total_size:107374182400,manager:100,room:100,automationapi",
                    Price = 0,
                    ProductId = null,
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -9,
                    Name = "admin",
                    Description = "since 01.04.2024",
                    Features = "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic,free_backup:2:fixed,automationapi",
                    Price = 20,
                    ProductId = "1006",
                    Visible = true
                },
                new DbQuota
                {
                    TenantId = -10,
                    Name = "adminyear",
                    Description = "since 10.02.2025",
                    Features = "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic,year,free_backup:2:fixed,automationapi",
                    Price = 220,
                    ProductId = "1009",
                    Visible = true
                },
                new DbQuota
                {
                    TenantId = -11,
                    Name = "storage",
                    Description = null,
                    Features = "total_size:1073741824",
                    Price = 0.14m,
                    ProductId = "1011",
                    ServiceName = "disk-storage",
                    Visible = true,
                    Wallet = true
                },
                new DbQuota
                {
                    TenantId = -12,
                    Name = "backup",
                    Description = null,
                    Features = "backup",
                    Price = 10,
                    ProductId = null,
                    ServiceName = "backup",
                    Visible = true,
                    Wallet = true
                },
                new DbQuota
                {
                    TenantId = -13,
                    Name = "aitools",
                    Description = null,
                    Features = "aitools",
                    Price = 1,
                    ProductId = null,
                    ServiceName = "ai-tools",
                    ServiceGroup = null,
                    Visible = true,
                    Wallet = true
                },
                // new DbQuota
                // {
                //     TenantId = -14,
                //     Name = "admin",
                //     Description = "since 08.06.2026",
                //     Features = "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic,free_backup:2:fixed,automationapi",
                //     Price = 20,
                //     ProductId = "1013",
                //     ServiceName = "admin",
                //     ServiceGroup = null,
                //     Visible = true,
                //     Wallet = true
                // },
                new DbQuota
                {
                    TenantId = -15,
                    Name = "docscloud",
                    Description = null,
                    Features = "docscloud:1",
                    Price = 8,
                    ProductId = "1014",
                    ServiceName = "docscloud",
                    ServiceGroup = null,
                    Visible = true,
                    Wallet = true
                },
                new DbQuota
                {
                    TenantId = -16,
                    Name = "docsclouddevpack",
                    Description = null,
                    Features = "docscloud:1,docsclouddevpack",
                    Price = 12,
                    ProductId = "1015",
                    ServiceName = "docscloud-devpack",
                    ServiceGroup = null,
                    Visible = false,
                    Wallet = true
                },
                new DbQuota
                {
                    TenantId = -17,
                    Name = "docscloudtrial",
                    Description = null,
                    Features = "docscloud:1000,docsclouddevpack,docscloudtrial",
                    Price = 0,
                    ProductId = "1016",
                    ServiceName = null,
                    ServiceGroup = null,
                    Visible = false,
                    Wallet = false
                }
                );
        return modelBuilder;
    }

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbQuota()
        {
            modelBuilder.Entity<DbQuota>(entity =>
            {
                entity.HasKey(e => e.TenantId)
                    .HasName("PRIMARY");

                entity.ToTable("tenants_quota")
                    .HasCharSet("utf8");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant")
                    .ValueGeneratedNever();

                entity.Property(e => e.ProductId)
                    .HasColumnName("product_id")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ServiceName)
                    .HasColumnName("service_name")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.ServiceGroup)
                    .HasColumnName("service_group")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Features)
                    .HasColumnName("features")
                    .HasColumnType("text");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("varchar")
                    .HasCharSet("utf8")
                    .UseCollation("utf8_general_ci");

                entity.Property(e => e.Price)
                    .HasColumnName("price")
                    .HasDefaultValueSql("'0.00'")
                    .HasColumnType("decimal(10,4)");

                entity.Property(e => e.Visible)
                    .HasColumnName("visible")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Wallet)
                    .HasColumnName("wallet")
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValueSql("'0'");
            });
        }

        public void PgSqlAddDbQuota()
        {
            modelBuilder.Entity<DbQuota>(entity =>
            {
                entity.HasKey(e => e.TenantId);

                entity.ToTable("tenants_quota");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant")
                    .ValueGeneratedNever();

                entity.Property(e => e.ProductId)
                    .HasColumnName("product_id")
                    .HasColumnType("varchar(128)");

                entity.Property(e => e.ServiceName)
                    .HasColumnName("service_name")
                    .HasColumnType("varchar(128)");

                entity.Property(e => e.ServiceGroup)
                    .HasColumnName("service_group")
                    .HasColumnType("varchar(128)");

                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasColumnType("varchar(128)");

                entity.Property(e => e.Features)
                    .HasColumnName("features")
                    .HasColumnType("text");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("varchar(128)");

                entity.Property(e => e.Price)
                    .HasColumnName("price")
                    .HasDefaultValue(0.00m)
                    .HasColumnType("decimal(10,4)");

                entity.Property(e => e.Visible)
                    .HasColumnName("visible")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);

                entity.Property(e => e.Wallet)
                    .HasColumnName("wallet")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);
            });

        }
    }
}
