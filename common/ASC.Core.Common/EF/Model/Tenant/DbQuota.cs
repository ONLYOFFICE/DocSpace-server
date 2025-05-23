﻿// (c) Copyright Ascensio System SIA 2009-2025
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

using Profile = AutoMapper.Profile;

namespace ASC.Core.Common.EF;

public class DbQuota : BaseEntity, IMapFrom<TenantQuota>
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
    public bool Visible { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId];
    }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<TenantQuota, DbQuota>();
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
                    Features = "trial,audit,ldap,sso,customization,thirdparty,restore,oauth,total_size:107374182400,file_size:100,manager:1,statistic",
                    Price = 0,
                    ProductId = null,
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -2,
                    Name = "admin",
                    Description = "until 01.04.2024",
                    Features = "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:107374182400,file_size:1024,manager:1,statistic",
                    Price = 15,
                    ProductId = "1002",
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -3,
                    Name = "startup",
                    Description = null,
                    Features = "free,oauth,total_size:2147483648,manager:3,room:12",
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
                    Features = "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,file_size:1024,statistic",
                    Price = 0,
                    ProductId = "1001",
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -7,
                    Name = "nonprofit",
                    Description = null,
                    Features = "non-profit,audit,ldap,sso,thirdparty,restore,oauth,contentsearch,total_size:2147483648,file_size:1024,manager:20,statistic",
                    Price = 0,
                    ProductId = "1007",
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -8,
                    Name = "zoom",
                    Description = null,
                    Features = "free,oauth,total_size:107374182400,manager:100,room:100",
                    Price = 0,
                    ProductId = null,
                    Visible = false
                },
                new DbQuota
                {
                    TenantId = -9,
                    Name = "admin",
                    Description = "since 01.04.2024",
                    Features = "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic",
                    Price = 20,
                    ProductId = "1006",
                    Visible = true
                },
                new DbQuota
                {
                    TenantId = -10,
                    Name = "adminyear",
                    Description = "since 10.02.2025",
                    Features = "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic,year",
                    Price = 200,
                    ProductId = "1009",
                    Visible = true
                }
                );
        return modelBuilder;
    }
    
    public static void MySqlAddDbQuota(this ModelBuilder modelBuilder)
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
                .HasColumnType("decimal(10,2)");

            entity.Property(e => e.Visible)
                .HasColumnName("visible")
                .HasColumnType("tinyint(1)")
                .HasDefaultValueSql("'0'");
        });
    }
    public static void PgSqlAddDbQuota(this ModelBuilder modelBuilder)
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
                .HasColumnType("decimal(10,2)");

            entity.Property(e => e.Visible)
                .HasColumnName("visible")
                .HasColumnType("boolean")
                .HasDefaultValue(false);
        });
        
    }
}
