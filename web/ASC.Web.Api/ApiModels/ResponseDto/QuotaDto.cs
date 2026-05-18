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
/// The wallet service information.
/// </summary>
/// <example>
/// {
///   "innerServices": [{"title": "File Storage", "size": 1073741824}],
///   "serviceName": "backup"
/// }
/// </example>
public class WalletServiceDto : QuotaDto
{
    /// <summary>
    /// The list of inner services.
    /// </summary>
    /// <example>[{"title": "File Storage", "size": 1073741824}]</example>
    public List<WalletServiceDto> InnerServices { get; set; }

    /// <summary>
    /// The service name.
    /// </summary>
    /// <example>backup</example>
    public string ServiceName { get; set; }
}

/// <summary>
/// The quota information.
/// </summary>
public class QuotaDto
{
    /// <summary>
    /// The quota ID.
    /// </summary>
    /// <example>1</example>
    public required int Id { get; set; }

    /// <summary>
    /// The quota title.
    /// </summary>
    /// <example>Basic Plan</example>
    public required string Title { get; set; }

    /// <summary>
    /// The price parameters.
    /// </summary>
    /// <example>{"value": 99.99, "currencySymbol": "$", "iSOCurrencySymbol": "USD"}</example>
    public required PriceDto Price { get; set; }

    /// <summary>
    /// Specifies if the quota is nonprofit or not.
    /// </summary>
    /// <example>false</example>
    public required bool NonProfit { get; set; }

    /// <summary>
    /// Specifies if the quota is free or not.
    /// </summary>
    /// <example>true</example>
    public required bool Free { get; set; }

    /// <summary>
    /// Specifies if the quota is trial or not.
    /// </summary>
    /// <example>false</example>
    public required bool Trial { get; set; }

    /// <summary>
    /// The list of tenant quota features.
    /// </summary>
    /// <example>[{"id": "00000000-0000-0000-0000-000000000001", "title": "Premium Storage"}]</example>
    public required IEnumerable<TenantQuotaFeatureDto> Features { get; set; }

    /// <summary>
    /// The user quota.
    /// </summary>
    /// <example>{}</example>
    public TenantEntityQuotaSettings UsersQuota { get; set; }

    /// <summary>
    /// The room quota.
    /// </summary>
    /// <example>{}</example>
    public TenantEntityQuotaSettings RoomsQuota { get; set; }

    /// <summary>
    /// The ai agent quota.
    /// </summary>
    /// <example>{}</example>
    public TenantEntityQuotaSettings AiAgentsQuota { get; set; }

    /// <summary>
    /// The tenant custom quota.
    /// </summary>
    /// <example>{}</example>
    public TenantQuotaSettings TenantCustomQuota { get; set; }

    /// <summary>
    /// The due date.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// The tenant quota feature parameters.
/// </summary>
public class TenantQuotaFeatureDto : IEquatable<TenantQuotaFeatureDto>
{
    /// <summary>
    /// The ID of the tenant quota feature.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public string Id { get; set; }

    /// <summary>
    /// The title of the tenant quota feature.
    /// </summary>
    /// <example>Premium Storage</example>
    public string Title { get; set; }

    /// <summary>
    /// The image URL of the tenant quota feature.
    /// </summary>
    /// <example>/images/premium-storage.png</example>
    public string Image { get; set; }

    /// <summary>
    /// The value of the tenant quota feature.
    /// </summary>
    /// <example>{}</example>
    public object Value { get; set; }

    /// <summary>
    /// The type of the tenant quota feature.
    /// </summary>
    /// <example>Storage</example>
    public string Type { get; set; }

    /// <summary>
    /// The used space parameters of the tenant quota feature.
    /// </summary>
    /// <example>{}</example>
    public FeatureUsedDto Used { get; set; }

    /// <summary>
    /// The price title of the tenant quota feature.
    /// </summary>
    /// <example>$9.99/month</example>
    public string PriceTitle { get; set; }

    public bool Equals(TenantQuotaFeatureDto other)
    {
        if (other is null)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override bool Equals(object obj) => Equals(obj as TenantQuotaFeatureDto);
    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>
/// The price parameters.
/// </summary>
public class PriceDto
{
    /// <summary>
    /// The price value.
    /// </summary>
    /// <example>99.99</example>
    public decimal? Value { get; set; }

    /// <summary>
    /// The currency symbol.
    /// </summary>
    /// <example>$</example>
    public string CurrencySymbol { get; set; }

    /// <summary>
    /// The three-character ISO 4217 currency symbol.
    /// </summary>
    /// <example>USD</example>
    public string ISOCurrencySymbol { get; set; }
}

/// <summary>
/// The used space parameters of the tenant quota feature.
/// </summary>
public class FeatureUsedDto
{
    /// <summary>
    /// The used space value.
    /// </summary>
    /// <example>{}</example>
    public required object Value { get; set; }

    /// <summary>
    /// The used space title.
    /// </summary>
    /// <example>50 GB used</example>
    public string Title { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class WalletServiceDtoMapper
{
    public static partial WalletServiceDto MapToWalletServiceDto(this QuotaDto source);
}