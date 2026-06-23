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

using Swashbuckle.AspNetCore.Annotations;

namespace ASC.Core.Common.EF.Model;

/// <summary>
/// The database tenant partner parameters.
/// </summary>
public class DbTenantPartner : BaseEntity
{
    /// <summary>
    /// The tenant ID.
    /// </summary>
    /// <example>1</example>
    public int TenantId { get; set; }

    /// <summary>
    /// The partner ID.
    /// </summary>
    /// <example>partner_123</example>
    [MaxLength(36)]
    public string PartnerId { get; set; }

    /// <summary>
    /// The affiliate ID.
    /// </summary>
    /// <example>artifact_123</example>
    [MaxLength(50)]
    public string AffiliateId { get; set; }

    /// <summary>
    /// The tenant partner campaign.
    /// </summary>
    /// <example>campaigh</example>
    [MaxLength(50)]
    public string Campaign { get; set; }

    /// <summary>
    /// The database tenant parameters.
    /// </summary>
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

    extension(ModelBuilder modelBuilder)
    {
        public void MySqlAddDbTenantPartner()
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

        public void PgSqlAddDbTenantPartner()
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
}