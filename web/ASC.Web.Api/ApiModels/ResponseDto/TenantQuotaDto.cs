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
/// The current tenant quota.
/// </summary>
public class TenantQuotaDto
{
    /// <summary>
    /// The tenant ID.
    /// </summary>
    /// <example>1</example>
    public int TenantId { get; init; }

    /// <summary>
    /// The tenant name.
    /// </summary>
    /// <example>Default</example>
    public string Name { get; init; }

    /// <summary>
    /// The tenant price.
    /// </summary>
    /// <example>10.0</example>
    public decimal Price { get; init; }

    /// <summary>
    /// The tenant price currency symbol.
    /// </summary>
    /// <example>$</example>
    public string PriceCurrencySymbol { get; init; }

    /// <summary>
    /// The tenant price three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>USD</example>
    public string PriceISOCurrencySymbol { get; init; }

    /// <summary>
    /// The tenant product ID.
    /// </summary>
    /// <example>64</example>
    public string ProductId { get; init; }

    /// <summary>
    /// The service name.
    /// </summary>
    /// <example>backup</example>
    public string ServiceName { get; init; }

    /// <summary>
    /// The service group.
    /// </summary>
    /// <example>services</example>
    public string ServiceGroup { get; init; }

    /// <summary>
    /// Specifies if the tenant quota is visible or not.
    /// </summary>
    /// <example>true</example>
    public bool Visible { get; init; }

    /// <summary>
    /// Specifies if the tenant quota applies to the wallet or not
    /// </summary>
    /// <example>true</example>
    public bool Wallet { get; init; }

    /// <summary>
    /// Specifies if the tenant quota is primary or additional.
    /// </summary>
    /// <example>false</example>
    public bool Additional { get; init; }

    /// <summary>
    /// The quota due date.
    /// </summary>
    /// <example>2021-01-01T00:00:00</example>
    public DateTime? DueDate { get; init; }

    /// <summary>
    /// The tenant quota features.
    /// </summary>
    /// <example>audit,ldap,sso</example>
    public string Features { get; init; }

    /// <summary>
    /// The tenant maximum file size.
    /// </summary>
    /// <example>25000000</example>
    public long MaxFileSize { get; init; }

    /// <summary>
    /// The tenant maximum total size.
    /// </summary>
    /// <example>25000000000</example>
    public long MaxTotalSize { get; init; }

    /// <summary>
    /// The number of portal users.
    /// </summary>
    /// <example>100</example>
    public int CountUser { get; init; }

    /// <summary>
    /// The number of portal room administrators.
    /// </summary>
    /// <example>10</example>
    public int CountRoomAdmin { get; init; }

    /// <summary>
    /// The number of room users.
    /// </summary>
    /// <example>50</example>
    public int UsersInRoom { get; init; }

    /// <summary>
    /// The number of rooms.
    /// </summary>
    /// <example>500</example>
    public int CountRoom { get; init; }

    /// <summary>
    /// Specifies if the tenant quota is nonprofit or not.
    /// </summary>
    /// <example>false</example>
    public bool NonProfit { get; init; }

    /// <summary>
    /// Specifies if the tenant quota is trial or not.
    /// </summary>
    /// <example>false</example>
    public bool Trial { get; init; }

    /// <summary>
    /// Specifies if the tenant quota is free or not.
    /// </summary>
    /// <example>false</example>
    public bool Free { get; init; }

    /// <summary>
    /// Specifies if the tenant quota is updated or not.
    /// </summary>
    /// <example>false</example>
    public bool Update { get; init; }

    /// <summary>
    /// Specifies if the audit trail is available or not.
    /// </summary>
    /// <example>true</example>
    public bool Audit { get; init; }

    /// <summary>
    /// Specifies if ONLYOFFICE Docs is included in the tenant quota or not.
    /// </summary>
    /// <example>true</example>
    public bool DocsEdition { get; init; }

    /// <summary>
    /// Specifies if the LDAP settings are available or not.
    /// </summary>
    /// <example>true</example>
    public bool Ldap { get; init; }

    /// <summary>
    /// Specifies if the SSO settings are available or not.
    /// </summary>
    /// <example>true</example>
    public bool Sso { get; init; }

    /// <summary>
    /// Specifies if the statistics settings are available or not.
    /// </summary>
    /// <example>true</example>
    public bool Statistic { get; init; }

    /// <summary>
    /// Specifies if the branding settings are available or not.
    /// </summary>
    /// <example>true</example>
    public bool Branding { get; init; }

    /// <summary>
    /// Specifies if the customization settings are available or not.
    /// </summary>
    /// <example>true</example>
    public bool Customization { get; init; }

    /// <summary>
    /// Specifies if the license has the lifetime settings or not.
    /// </summary>
    /// <example>false</example>
    public bool Lifetime { get; init; }

    /// <summary>
    /// Specifies if the Automation API is available or not.
    /// </summary>
    /// <example>true</example>
    public bool AutomationApi { get; init; }

    /// <summary>
    /// Specifies if the custom domain URL is available or not.
    /// </summary>
    /// <example>false</example>
    public bool Custom { get; init; }

    /// <summary>
    /// Specifies if the restore is enabled or not.
    /// </summary>
    /// <example>true</example>
    public bool Restore { get; init; }

    /// <summary>
    /// Specifies if Oauth is available or not.
    /// </summary>
    /// <example>true</example>
    public bool Oauth { get; init; }

    /// <summary>
    /// Specifies if the content search is available or not.
    /// </summary>
    /// <example>true</example>
    public bool ContentSearch { get; init; }

    /// <summary>
    /// Specifies if the third-party accounts linking is available or not.
    /// </summary>
    /// <example>true</example>
    public bool ThirdParty { get; init; }

    /// <summary>
    /// Specifies if the tenant quota is yearly subscription or not.
    /// </summary>
    /// <example>true</example>
    public bool Year { get; init; }

    /// <summary>
    /// The number of free backups within a month.
    /// </summary>
    /// <example>1</example>
    public int CountFreeBackup { get; init; }

    /// <summary>
    /// Specifies if the backup enabled as a wallet service or not.
    /// </summary>
    /// <example>true</example>
    public bool Backup { get; init; }

    /// <summary>
    /// The number of AI agents.
    /// </summary>
    /// <example>5</example>
    public int CountAIAgent { get; init; }

    /// <summary>
    /// Specifies if the AI tools enabled as a wallet service or not.
    /// </summary>
    /// <example>true</example>
    public bool AITools { get; init; }

    /// <summary>
    /// Specifies if the AI search enabled as a wallet service or not.
    /// </summary>
    /// <example>true</example>
    public bool AISearch { get; init; }

    /// <summary>
    /// The number of Docs Connect users.
    /// </summary>
    /// <example>true</example>
    public int DocsCloud { get; init; }

    /// <summary>
    /// Specifies if the Docs Connect Dev Pack enabled or not.
    /// </summary>
    /// <example>true</example>
    public bool DocsCloudDevPack { get; init; }

    /// <summary>
    /// Specifies if the Docs Connect trial enabled or not.
    /// </summary>
    /// <example>true</example>
    public bool DocsCloudTrial { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class TenantQuotaDtoMapper
{
    public static partial TenantQuotaDto Map(this TenantQuota source);
}
