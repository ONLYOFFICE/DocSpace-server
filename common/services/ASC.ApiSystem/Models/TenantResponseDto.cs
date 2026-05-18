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
