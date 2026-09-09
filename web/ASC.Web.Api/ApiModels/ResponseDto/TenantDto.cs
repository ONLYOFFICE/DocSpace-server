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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The record of one portal: its name, owner, language, time zone and lifecycle state.
/// </summary>
/// <example>
/// {
///   "tenantId": 1,
///   "tenantAlias": "my-company",
///   "name": "My Company",
///   "ownerId": "00000000-0000-0000-0000-000000000001",
///   "creationDateTime": "2024-01-15T10:30:00Z",
///   "status": "Active",
///   "statusChangeDate": "2024-01-15T10:30:00Z",
///   "language": "en-US",
///   "timeZone": "America/New_York",
///   "calls": true,
///   "region": "us-east-1"
/// }
/// </example>
public class TenantDto
{
    /// <summary>
    /// The partner the portal was signed up through, empty for a portal that came in directly. It is bookkeeping
    /// for the vendor and has no bearing on what the portal may do.
    /// </summary>
    /// <example>AFF12345</example>
    public string AffiliateId { get; set; }

    /// <summary>
    /// The portal's own name within the installation, which together with the installation's base domain forms
    /// the address it is reached at. A caller without the portal-settings right gets `tenantId` alone, so an
    /// empty value here is the sign that the rest of this object was withheld rather than unset.
    /// </summary>
    /// <example>my-company</example>
    public string TenantAlias { get; set; }

    /// <summary>
    /// Whether telephony is switched on for the portal. It is carried over from portal registration and stays
    /// `false` on a DocSpace portal, where the feature does not exist.
    /// </summary>
    /// <example>true</example>
    public bool Calls { get; set; }

    /// <summary>
    /// The marketing campaign the portal was signed up under, empty for a portal that came in outside one. Like
    /// `affiliateId`, it is bookkeeping only.
    /// </summary>
    /// <example>WINTER2024</example>
    public string Campaign { get; set; }

    /// <summary>
    /// When the portal was created, in UTC rather than in the portal time zone.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreationDateTime { get; internal set; }

    /// <summary>
    /// The data-centre region written on the portal record itself, as opposed to `region`, which is looked up
    /// from the hosting service. It is empty on a server installation.
    /// </summary>
    /// <example>EU</example>
    public string HostedRegion { get; set; }

    /// <summary>
    /// The numeric identifier of the portal inside the installation. It is the one field every caller gets,
    /// whatever their rights.
    /// </summary>
    /// <example>1</example>
    public int TenantId { get; internal set; }

    /// <summary>
    /// The line of business chosen when the portal was created. It only steers what the vendor suggests and
    /// restricts nothing.
    /// </summary>
    /// <example>IT</example>
    public TenantIndustry Industry { get; set; }

    /// <summary>
    /// The default language of the portal as a culture name, the same value `GET api/2.0/settings` reports as
    /// `culture`. A member may have a language of their own, which this does not reflect.
    /// </summary>
    /// <example>en-US</example>
    public string Language { get; set; }

    /// <summary>
    /// When any field of this record last changed, in UTC. It does not move when portal settings outside this
    /// record are changed.
    /// </summary>
    /// <example>2024-02-10T14:20:00Z</example>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// The custom domain the portal answers on in addition to its own address, empty when none has been set up.
    /// </summary>
    /// <example>mycompany.example.com</example>
    public string MappedDomain { get; set; }

    /// <summary>
    /// The portal title as shown to people, which is what `GET api/2.0/settings` returns as
    /// `greetingSettings`. It is free text, unlike `tenantAlias`, and empty until someone sets it.
    /// </summary>
    /// <example>My Company</example>
    public string Name { get; set; }

    /// <summary>
    /// The portal owner, the one account that cannot be removed or demoted.
    /// `PUT api/2.0/settings/owner` hands the role over.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// The portal's identifier in the billing system, empty for a portal that has never been billed. The
    /// subscription itself is read with `GET api/2.0/portal/tariff`.
    /// </summary>
    /// <example>PAY123456789</example>
    public string PaymentId { get; set; }

    /// <summary>
    /// Whether the owner agreed to receive the vendor's newsletter. Despite the name it does not mark the portal
    /// as a spammer and affects nothing but marketing mail.
    /// </summary>
    /// <example>false</example>
    public bool Spam { get; set; }

    /// <summary>
    /// The lifecycle state of the portal. Anything other than active means most operations are refused for the
    /// moment, because the portal is being transferred, restored, encrypted or removed.
    /// </summary>
    /// <example>Active</example>
    public TenantStatus Status { get; internal set; }

    /// <summary>
    /// When `status` last changed, in UTC. For a portal pending removal it is the moment the countdown to
    /// deletion started.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime StatusChangeDate { get; internal set; }

    /// <summary>
    /// The portal time zone, which is the zone the dates this API calls "portal time" are expressed in. It may be
    /// stored as a Windows identifier here, while `GET api/2.0/settings` always reports the IANA form.
    /// </summary>
    /// <example>America/New_York</example>
    public string TimeZone { get; set; }

    /// <summary>
    /// The mail domains a new member may register or be invited from without confirming the address. It is empty
    /// whenever `trustedDomainsType` is not `Custom`.
    /// </summary>
    /// <example>["example.com", "trusted.com"]</example>
    public List<string> TrustedDomains { get; set; }

    /// <summary>
    /// The same domains as the single stored string they are kept in, separated by commas. Read
    /// `trustedDomains` instead; this one exists because it is what the record holds.
    /// </summary>
    /// <example>example.com,trusted.com</example>
    public string TrustedDomainsRaw { get; set; }

    /// <summary>
    /// How the mail domains are applied: no domain trusted, every domain trusted, or only the listed ones. Only
    /// the last of the three makes `trustedDomains` meaningful.
    /// </summary>
    /// <example>Custom</example>
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    /// <summary>
    /// The identifier of the portal version the installation pins this portal to, which is an internal number
    /// and not the product version string that `GET api/2.0/settings` reports as `version`.
    /// </summary>
    /// <example>2</example>
    public int Version { get; set; }

    /// <summary>
    /// When `version` last changed, in UTC. It stays at its zero value on a portal whose version has never been
    /// switched.
    /// </summary>
    /// <example>2024-02-01T09:00:00Z</example>
    public DateTime VersionChanged { get; set; }

    /// <summary>
    /// The data-centre region the portal is actually served from, looked up from the hosting service. It is
    /// empty on a server installation and also whenever the installation's portal cache is switched off, so an
    /// empty value does not mean the portal has no region - `hostedRegion` is the value from the record itself.
    /// </summary>
    /// <example>us-east-1</example>
    public string Region { get; set; }
}


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class TenantDtoMapper
{
    [MapProperty(nameof(Tenant.Id), nameof(TenantDto.TenantId))]
    [MapProperty(nameof(Tenant.Alias), nameof(TenantDto.TenantAlias))]
    public static partial TenantDto MapToDto(this Tenant source);
}