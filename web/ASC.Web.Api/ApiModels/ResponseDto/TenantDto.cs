// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The tenant parameters.
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
    /// The affiliate ID.
    /// </summary>
    /// <example>AFF12345</example>
    public string AffiliateId { get; set; }

    /// <summary>
    /// The tenant alias.
    /// </summary>
    /// <example>my-company</example>
    public string TenantAlias { get; set; }

    /// <summary>
    /// Specifies if the calls are available for this tenant or not.
    /// </summary>
    /// <example>true</example>
    public bool Calls { get; set; }

    /// <summary>
    /// The tenant campaign.
    /// </summary>
    /// <example>WINTER2024</example>
    public string Campaign { get; set; }

    /// <summary>
    /// The tenant creation date and time.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreationDateTime { get; internal set; }

    /// <summary>
    /// The hosted region.
    /// </summary>
    /// <example>EU</example>
    public string HostedRegion { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    /// <example>1</example>
    public int TenantId { get; internal set; }

    /// <summary>
    /// The tenant industry.
    /// </summary>
    /// <example>IT</example>
    public TenantIndustry Industry { get; set; }

    /// <summary>
    /// The tenant language.
    /// </summary>
    /// <example>en-US</example>
    public string Language { get; set; }

    /// <summary>
    /// The date and time when the tenant was last modified.
    /// </summary>
    /// <example>2024-02-10T14:20:00Z</example>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// The tenant mapped domain.
    /// </summary>
    /// <example>mycompany.example.com</example>
    public string MappedDomain { get; set; }

    /// <summary>
    /// The tenant name.
    /// </summary>
    /// <example>My Company</example>
    public string Name { get; set; }

    /// <summary>
    /// The tenant owner ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// The tenant payment ID.
    /// </summary>
    /// <example>PAY123456789</example>
    public string PaymentId { get; set; }

    /// <summary>
    /// Specifies if the ONLYOFFICE newsletter is allowed or not.
    /// </summary>
    /// <example>false</example>
    public bool Spam { get; set; }

    /// <summary>
    /// The tenant status.
    /// </summary>
    /// <example>Active</example>
    public TenantStatus Status { get; internal set; }

    /// <summary>
    /// The date and time when the tenant status was changed.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime StatusChangeDate { get; internal set; }

    /// <summary>
    /// The tenant time zone.
    /// </summary>
    /// <example>America/New_York</example>
    public string TimeZone { get; set; }

    /// <summary>
    /// The list of tenant trusted domains.
    /// </summary>
    /// <example>["example.com", "trusted.com"]</example>
    public List<string> TrustedDomains { get; set; }

    /// <summary>
    /// The tenant trusted domains in the string format.
    /// </summary>
    /// <example>example.com,trusted.com</example>
    public string TrustedDomainsRaw { get; set; }

    /// <summary>
    /// The type of the tenant trusted domains.
    /// </summary>
    /// <example>Custom</example>
    public TenantTrustedDomainsType TrustedDomainsType { get; set; }

    /// <summary>
    /// The tenant version
    /// </summary>
    /// <example>2</example>
    public int Version { get; set; }

    /// <summary>
    /// The date and time when the tenant version was changed.
    /// </summary>
    /// <example>2024-02-01T09:00:00Z</example>
    public DateTime VersionChanged { get; set; }

    /// <summary>
    /// The tenant AWS region.
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