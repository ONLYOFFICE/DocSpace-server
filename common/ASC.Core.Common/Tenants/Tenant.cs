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

namespace ASC.Core.Tenants;

/// <summary>
/// The tenant parameters.
/// </summary>
[ProtoContract]
public class Tenant
{
    public const int DefaultTenant = -1;

    public static readonly string HostName = Dns.GetHostName().ToLowerInvariant();
    public const string LocalHost = "localhost";

    public Tenant()
    {
        Id = DefaultTenant;
        TimeZone = TimeZoneInfo.Utc.Id;
        Language = CultureInfo.CurrentCulture.Name;
        TrustedDomains = [];
        TrustedDomainsType = TenantTrustedDomainsType.None;
        CreationDateTime = DateTime.UtcNow;
        Status = TenantStatus.Active;
        StatusChangeDate = DateTime.UtcNow;
        VersionChanged = DateTime.UtcNow;
        Industry = TenantIndustry.Other;
    }

    public Tenant(string alias)
        : this()
    {
        Alias = alias.ToLowerInvariant();
    }

    public Tenant(int id, string alias)
        : this(alias)
    {
        Id = id;
    }

    [ProtoMember(1)]
    public string AffiliateId { get; set; }

    [ProtoMember(2)]
    public string Alias { get; set; }

    [ProtoMember(3)]
    public bool Calls { get; set; }

    [ProtoMember(4)]
    public string Campaign { get; set; }

    [ProtoMember(5)]
    public DateTime CreationDateTime { get; set; }

    [ProtoMember(6)]
    public string HostedRegion { get; set; }

    [ProtoMember(7)]
    public int Id { get; set; }

    [ProtoMember(8)]
    public TenantIndustry Industry { get; set; }

    [ProtoMember(9)]
    public string Language { get; set; }

    [ProtoMember(10)]
    public DateTime LastModified { get; set; }

    [ProtoMember(11)]
    public string MappedDomain { get; set; }

    [ProtoMember(12)]
    public string Name { get; set; }

    [ProtoMember(13)]
    public Guid OwnerId { get; set; }

    [ProtoMember(14)]
    public string PartnerId { get; set; }

    [ProtoMember(15)]
    public string PaymentId { get; set; }

    [ProtoMember(16)]
    public TenantStatus Status { get; set; }

    [ProtoMember(17)]
    public DateTime StatusChangeDate { get; set; }

    [ProtoMember(18)]
    public string TimeZone { get; set; }

    [ProtoMember(19)]
    public List<string> TrustedDomains
    {
        get
        {
            if (field.Count == 0 && !string.IsNullOrEmpty(TrustedDomainsRaw))
            {
                field = TrustedDomainsRaw.Split(['|'],
                    StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return field;
        }

        set;
    }

    [ProtoMember(20)]
    public string TrustedDomainsRaw { get; set; }

    [ProtoMember(21)]
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    [ProtoMember(22)]
    public int Version { get; set; }

    [ProtoMember(23)]
    public DateTime VersionChanged { get; set; }

    public override bool Equals(object obj)
    {
        return obj is Tenant t && t.Id == Id;
    }

    public CultureInfo GetCulture() => !string.IsNullOrEmpty(Language) ? CultureInfo.GetCultureInfo(Language.Trim()) : CultureInfo.CurrentCulture;

    public override int GetHashCode()
    {
        return Id;
    }

    public string GetTenantDomain(CoreSettings coreSettings, bool allowMappedDomain = true)
    {
        var baseHost = coreSettings.GetBaseDomain(HostedRegion);

        if (string.IsNullOrEmpty(baseHost) && !string.IsNullOrEmpty(HostedRegion))
        {
            baseHost = HostedRegion;
        }

        string result;
        if (baseHost == "localhost" || Alias == "localhost")
        {
            //single tenant on local host
            Alias = "localhost";
            result = Alias;
        }
        else
        {
            result = $"{Alias}.{baseHost}".TrimEnd('.').ToLowerInvariant();
        }

        if (string.IsNullOrEmpty(MappedDomain) || !allowMappedDomain)
        {
            return result;
        }

        if (MappedDomain.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
        {
            MappedDomain = MappedDomain[7..];
        }
        if (MappedDomain.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
        {
            MappedDomain = MappedDomain[8..];
        }

        result = MappedDomain.ToLowerInvariant();

        return result;
    }

    public void SetStatus(TenantStatus status)
    {
        Status = status;
        StatusChangeDate = DateTime.UtcNow;
    }

    public override string ToString()
    {
        return Alias;
    }

    internal string GetTrustedDomains()
    {
        TrustedDomains.RemoveAll(string.IsNullOrEmpty);
        if (TrustedDomains.Count == 0)
        {
            return null;
        }

        return string.Join("|", TrustedDomains.ToArray());
    }

    internal void SetTrustedDomains(string trustedDomains)
    {
        if (string.IsNullOrEmpty(trustedDomains))
        {
            TrustedDomains.Clear();
        }
        else
        {
            TrustedDomains.AddRange(trustedDomains.Split(['|'], StringSplitOptions.RemoveEmptyEntries));
        }
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class TenantMapper
{
    [MapProperty(nameof(DbTenant.TrustedDomainsEnabled), nameof(Tenant.TrustedDomainsType))]
    [MapNestedProperties(nameof(DbTenant.Partner))]
    public static partial Tenant Map(this DbTenant source);

    [MapNestedProperties(nameof(TenantUserSecurity.DbTenant))]
    public static partial Tenant Map(this TenantUserSecurity source);

    public static partial IQueryable<Tenant> Project(this IQueryable<DbTenant> source);
    public static partial IQueryable<Tenant> Project(this IQueryable<TenantUserSecurity> source);
}