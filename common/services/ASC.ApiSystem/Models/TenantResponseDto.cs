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

namespace ASC.ApiSystem.Models;

/// <summary>
/// Tenant information response
/// </summary>
public sealed record TenantResponseDto
{
    /// <summary>
    /// Gets the date and time when the tenant was created.
    /// </summary>
    /// <example>2023-01-01T00:00:00Z</example>
    public required DateTime Created { get; init; }

    /// <summary>
    /// Gets the tenant domain name.
    /// </summary>
    /// <example>localhost</example>
    public required string Domain { get; init; }

    /// <summary>
    /// Gets or initializes the custom domain name that is mapped to this tenant.
    /// </summary>
    /// <example>mydomain.com</example>
    public string MappedDomain { get; init; }

    /// <summary>
    /// Gets or initializes the region where the tenant is hosted.
    /// </summary>
    /// <example>localhost</example>
    public string HostedRegion { get; init; }

    /// <summary>
    /// Gets the industry type associated with the tenant.
    /// Represents the business or organizational sector that the tenant operates in.
    /// </summary>
    /// <example>0</example>
    public required TenantIndustry Industry { get; init; }

    /// <summary>
    /// Gets the language code of the tenant.
    /// </summary>
    /// <example>en-US</example>
    public required string Language { get; init; }

    /// <summary>
    /// Gets or sets the name of the tenant.
    /// </summary>
    /// <example>my portal</example>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the unique identifier of the tenant owner.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required Guid OwnerId { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier for the payment associated with the tenant.
    /// </summary>
    /// <example>1234567890</example>
    public string PaymentId { get; init; }

    /// <summary>
    /// Gets or initializes the identifier of the partner associated with the tenant.
    /// </summary>
    /// <example>partner123</example>
    public string PartnerId { get; init; }

    /// <summary>
    /// Gets the portal alias name used to identify and access the tenant.
    /// This is the subdomain or unique identifier portion of the portal URL.
    /// </summary>
    /// <example>myportal</example>
    public required string PortalName { get; init; }

    /// <summary>
    /// Gets the tenant status.
    /// </summary>
    /// <example>Active</example>
    public required string Status { get; init; }

    /// <summary>
    /// Gets the unique identifier of the tenant.
    /// </summary>
    /// <example>1</example>
    public required int TenantId { get; init; }

    /// <summary>
    /// Gets or initializes the IANA time zone identifier for the tenant.
    /// </summary>
    /// <example>UTC</example>
    public required string TimeZoneId { get; init; }

    /// <summary>
    /// Gets the display name of the tenant's time zone.
    /// </summary>
    /// <example>UTC</example>
    public required string TimeZoneName { get; init; }

    /// <summary>
    /// Gets or sets the quota usage information for the tenant.
    /// Contains details about storage, users, rooms, AI agents, and their respective limits.
    /// </summary>
    /// <example>{}</example>
    public QuotaUsageDto QuotaUsage { get; init; }

    /// <summary>
    /// Gets the custom quota value for the tenant in bytes.
    /// Returns -1 if the quota is unlimited (when MaxTotalSize is long.MaxValue).
    /// Returns the custom quota if it's enabled and less than or equal to the tariff's maximum total size.
    /// Otherwise, returns the tariff's maximum total size.
    /// </summary>
    /// <example>10737418240</example>
    public required long CustomQuota { get; init; }

    /// <summary>
    /// Gets or initializes the tenant owner information.
    /// </summary>
    /// <example>{}</example>
    public TenantOwnerDto Owner { get; init; }

    /// <summary>
    /// Gets or initializes the wizard settings for the tenant.
    /// Contains information about the initial setup wizard completion status and configuration.
    /// </summary>
    /// <example>{}</example>
    public WizardSettings WizardSettings { get; init; }
}
