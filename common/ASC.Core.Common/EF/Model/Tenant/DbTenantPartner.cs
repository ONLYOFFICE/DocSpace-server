﻿// (c) Copyright Ascensio System SIA 2009-2024
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

using Swashbuckle.AspNetCore.Annotations;

namespace ASC.Core.Common.EF.Model;

public class DbTenantPartner : BaseEntity
{
    /// <summary>
    /// Tenant id
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Partner id
    /// </summary>
    [MaxLength(36)]
    public string PartnerId { get; set; }

    /// <summary>
    /// Affiliate id
    /// </summary>
    [MaxLength(50)]
    public string AffiliateId { get; set; }

    /// <summary>
    /// Campaign
    /// </summary>
    [MaxLength(50)]
    public string Campaign { get; set; }

    [SwaggerIgnore]
    public DbTenant Tenant { get; set; }

    public override object[] GetKeys()
    {
        return [TenantId];
    }
}

public static class DbTenantPartnerExtension
{
    public static ModelBuilderWrapper AddDbTenantPartner(this ModelBuilderWrapper modelBuilder)
    {
        modelBuilder
            .Add(MySqlAddDbTenantPartner, Provider.MySql)
            .Add(PgSqlAddDbTenantPartner, Provider.PostgreSql);

        return modelBuilder;
    }

    public static void MySqlAddDbTenantPartner(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTenantPartner>().Navigation(e => e.Tenant).AutoInclude(false);

        modelBuilder.Entity<DbTenantPartner>(entity =>
        {
            entity.HasKey(e => new { e.TenantId })
                .HasName("PRIMARY");

            entity.ToTable("tenants_partners")
                .HasCharSet("utf8");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .ValueGeneratedNever();

            entity.Property(e => e.AffiliateId)
                .HasColumnName("affiliate_id")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.Campaign)
                .HasColumnName("campaign")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");

            entity.Property(e => e.PartnerId)
                .HasColumnName("partner_id")
                .HasColumnType("varchar")
                .HasCharSet("utf8")
                .UseCollation("utf8_general_ci");
        });

    }
    public static void PgSqlAddDbTenantPartner(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTenantPartner>(entity =>
        {
            entity.HasKey(e => e.TenantId)
                .HasName("tenant_partner_pkey");

            entity.ToTable("tenants_partners");

            entity.Property(e => e.TenantId)
                .HasColumnName("tenant_id")
                .ValueGeneratedNever();

            entity.Property(e => e.AffiliateId)
                .HasColumnName("affiliate_id")
                .HasColumnType("character varying")
                .HasMaxLength(50);

            entity.Property(e => e.Campaign)
                .HasColumnName("campaign")
                .HasColumnType("character varying")
                .HasMaxLength(50);

            entity.Property(e => e.PartnerId)
                .HasColumnName("partner_id")
                .HasColumnType("character varying")
                .HasMaxLength(36);
        });
        
    }
}