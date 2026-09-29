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
/// Represents a Docs Connect tenant of a portal.
/// </summary>
public class DocsCloudTenantDto
{
    /// <summary>
    /// The external ID of the dedicated resource the tenant is hosted on.
    /// </summary>
    /// <example>12345</example>
    public int DedicatedResourceExId { get; init; }

    /// <summary>
    /// The tenant alias.
    /// </summary>
    /// <example>my-portal</example>
    public string Alias { get; init; }

    /// <summary>
    /// The tenant name.
    /// </summary>
    /// <example>My Portal</example>
    public string Name { get; init; }

    /// <summary>
    /// The date and time when the tenant was last modified.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime ModifiedDate { get; init; }

    /// <summary>
    /// The customer ID.
    /// </summary>
    /// <example>CustomerId</example>
    public string CustomerId { get; init; }

    /// <summary>
    /// The customer name.
    /// </summary>
    /// <example>CustomerName</example>
    public string CustomerName { get; init; }

    /// <summary>
    /// The date and time when the tenant subscription ends.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// The resource type.
    /// </summary>
    /// <example>1</example>
    public int ResourceType { get; init; }

    /// <summary>
    /// Whether the tenant is active (the end date is in the future).
    /// </summary>
    /// <example>false</example>
    public bool IsActive { get; init; }

    /// <summary>
    /// The tenant address.
    /// </summary>
    /// <example>https://my-portal.onlyoffice.com</example>
    public string Address { get; init; }

    /// <summary>
    /// The tenant payment information.
    /// </summary>
    public DocsCloudPaymentDto Payment { get; init; }
}

/// <summary>
/// Represents the payment information of a Docs Connect tenant.
/// </summary>
public class DocsCloudPaymentDto
{
    /// <summary>
    /// The cart ID.
    /// </summary>
    /// <example>CartId</example>
    public string CartId { get; init; }

    /// <summary>
    /// The product ID.
    /// </summary>
    /// <example>12345</example>
    public int ProductId { get; init; }

    /// <summary>
    /// The payment status.
    /// </summary>
    /// <example>1</example>
    public int Status { get; init; }

    /// <summary>
    /// The interval unit.
    /// </summary>
    /// <example>1</example>
    public int IntervalUnit { get; init; }

    /// <summary>
    /// Whether the payment interval is yearly.
    /// </summary>
    /// <example>false</example>
    public bool IsYear { get; init; }

    /// <summary>
    /// Whether the payment is prepaid.
    /// </summary>
    /// <example>false</example>
    public bool IsPrepaid { get; init; }

    /// <summary>
    /// The quantity.
    /// </summary>
    /// <example>10</example>
    public int Quantity { get; init; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol of the payment.
    /// </summary>
    /// <example>USD</example>
    public string Currency { get; init; }
}

/// <summary>
/// Represents the configuration of a Docs Connect tenant.
/// </summary>
public class DocsCloudConfigDto
{
    /// <summary>
    /// The tenant name.
    /// </summary>
    /// <example>My Portal</example>
    [StringLength(255)]
    public string TenantName { get; init; }

    /// <summary>
    /// The security configuration.
    /// </summary>
    public DocsCloudSecurityConfigDto Security { get; init; }

    /// <summary>
    /// The server configuration.
    /// </summary>
    public DocsCloudServerConfigDto Server { get; init; }

    /// <summary>
    /// The WOPI configuration.
    /// </summary>
    public DocsCloudWopiConfigDto Wopi { get; init; }

    /// <summary>
    /// The IP filter configuration.
    /// </summary>
    public DocsCloudIpFilterConfigDto IpFilter { get; init; }
}

/// <summary>
/// Represents the security configuration of a Docs Connect tenant.
/// </summary>
public class DocsCloudSecurityConfigDto
{
    /// <summary>
    /// The security secret.
    /// </summary>
    /// <example>abc123</example>
    [StringLength(255)]
    public string Secret { get; init; }

    /// <summary>
    /// The security header name.
    /// </summary>
    /// <example>Authorization</example>
    [StringLength(255)]
    public string Header { get; init; }
}

/// <summary>
/// Represents the server configuration of a Docs Connect tenant.
/// </summary>
public class DocsCloudServerConfigDto
{
    /// <summary>
    /// Whether anonymous access is supported.
    /// </summary>
    /// <example>false</example>
    public bool IsAnonymousSupport { get; init; }

    [Range(typeof(long), "0", "209715200")]
    public long FileSizeLimit { get; init; }
}

/// <summary>
/// Represents the WOPI configuration of a Docs Connect tenant.
/// </summary>
public class DocsCloudWopiConfigDto
{
    /// <summary>
    /// Whether WOPI is enabled.
    /// </summary>
    /// <example>false</example>
    public bool Enable { get; init; }
}

/// <summary>
/// Represents the IP filter configuration of a Docs Connect tenant.
/// </summary>
public class DocsCloudIpFilterConfigDto
{
    /// <summary>
    /// The IP filter rules.
    /// </summary>
    /// <example>[{"address": "127.0.0.1", "allowed": true}]</example>
    public List<DocsCloudIpFilterRuleDto> Rules { get; init; }
}

/// <summary>
/// Represents the IP filter rule of a Docs Connect tenant.
/// </summary>
public class DocsCloudIpFilterRuleDto
{
    [StringLength(255)]
    public string Address { get; init; }

    /// <summary>
    /// Whether the IP address is allowed.
    /// </summary>
    /// <example>true</example>
    public bool Allowed { get; init; }
}

/// <summary>
/// Represents the usage statistics of a Docs Connect tenant.
/// </summary>
public class DocsCloudUsageDto
{
    /// <summary>
    /// The date and time the usage statistics are counted from.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Since { get; init; }

    /// <summary>
    /// The number of active users.
    /// </summary>
    /// <example>10</example>
    public int ActiveCount { get; init; }
}

/// <summary>
/// Represents the license and server information of a Docs Connect tenant, with usage statistics for the current period.
/// </summary>
public class DocsCloudTenantInfoDto
{
    /// <summary>
    /// The license information.
    /// </summary>
    public DocsCloudLicenseInfoDto License { get; init; }

    /// <summary>
    /// The Docs Connect server information.
    /// </summary>
    public DocsCloudServerInfoDto Server { get; init; }

    /// <summary>
    /// The user limits of the license.
    /// </summary>
    public DocsCloudUsersLimitDto UsersLimit { get; init; }

    /// <summary>
    /// The usage statistics for the current period.
    /// </summary>
    public DocsCloudStatsDto Stats { get; init; }
}

/// <summary>
/// Represents the license information of a Docs Connect tenant.
/// </summary>
public class DocsCloudLicenseInfoDto
{
    /// <summary>
    /// The date and time until which the license is valid.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Valid { get; init; }

    /// <summary>
    /// Whether the license is a trial.
    /// </summary>
    /// <example>false</example>
    public bool Trial { get; init; }

    /// <summary>
    /// The license build date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime BuildDate { get; init; }
}

/// <summary>
/// Represents the Docs Connect server information.
/// </summary>
public class DocsCloudServerInfoDto
{
    /// <summary>
    /// The server version.
    /// </summary>
    /// <example>8.0.0</example>
    public string Version { get; init; }

    /// <summary>
    /// The server package type ("Open Source", "Enterprise Edition" or "Developer Edition").
    /// </summary>
    /// <example>Enterprise Edition</example>
    public string PackageType { get; init; }

    /// <summary>
    /// The server build date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Date { get; init; }
}

/// <summary>
/// Represents the user limits of a Docs Connect license.
/// </summary>
public class DocsCloudUsersLimitDto
{
    /// <summary>
    /// The maximum number of users who can edit documents.
    /// </summary>
    /// <example>100</example>
    public int Edit { get; init; }

    /// <summary>
    /// The maximum number of users who can view documents.
    /// </summary>
    /// <example>100</example>
    public int View { get; init; }
}

/// <summary>
/// Represents the usage statistics of a Docs Connect tenant for the current period.
/// </summary>
public class DocsCloudStatsDto
{
    /// <summary>
    /// The length of the statistics period in days.
    /// </summary>
    /// <example>30</example>
    public int PeriodDay { get; init; }

    /// <summary>
    /// The statistics for editor users.
    /// </summary>
    public DocsCloudUserStatsDto Editor { get; init; }

    /// <summary>
    /// The statistics for viewer users.
    /// </summary>
    public DocsCloudUserStatsDto Viewer { get; init; }
}

/// <summary>
/// Represents the usage statistics of a single Docs Connect user category (editor or viewer).
/// </summary>
public class DocsCloudUserStatsDto
{
    /// <summary>
    /// The number of active users.
    /// </summary>
    /// <example>10</example>
    public int Active { get; init; }

    /// <summary>
    /// The number of internal users.
    /// </summary>
    /// <example>8</example>
    public int Internal { get; init; }

    /// <summary>
    /// The number of external users.
    /// </summary>
    /// <example>2</example>
    public int External { get; init; }

    /// <summary>
    /// The number of remaining users before the limit is reached.
    /// </summary>
    /// <example>90</example>
    public int Remaining { get; init; }

    /// <summary>
    /// Whether the number of remaining users is critically low.
    /// </summary>
    /// <example>false</example>
    public bool CriticalRemaining { get; init; }
}

/// <summary>
/// Represents the current user quota of a Docs Connect tenant.
/// </summary>
public class DocsCloudQuotaDto
{
    /// <summary>
    /// The editor users.
    /// </summary>
    /// <example>[{"userid": "00000000-0000-0000-0000-000000000000", "expire": "2024-01-15T10:30:00Z"}]</example>
    public List<DocsCloudQuotaUserDto> Users { get; init; }

    /// <summary>
    /// The viewer users.
    /// </summary>
    /// <example>[{"userid": "00000000-0000-0000-0000-000000000000", "expire": "2024-01-15T10:30:00Z"}]</example>
    public List<DocsCloudQuotaUserDto> UsersView { get; init; }
}

/// <summary>
/// Represents a single user entry of a Docs Connect quota.
/// </summary>
public class DocsCloudQuotaUserDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public string UserId { get; init; }

    /// <summary>
    /// The expiration date of the user.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public string Expire { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class DocsCloudDtoMapper
{
    public static partial DocsCloudTenantDto Map(this DocsCloudTenant source);

    public static partial DocsCloudTenantInfoDto Map(this DocsCloudTenantInfo source);

    public static partial DocsCloudConfigDto Map(this DocsCloudConfig source);

    public static partial DocsCloudQuotaDto Map(this DocsCloudQuota source);

    public static partial DocsCloudUsageDto Map(this DocsCloudUsage source);
}
