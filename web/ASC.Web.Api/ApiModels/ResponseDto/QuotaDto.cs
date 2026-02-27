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
/// The wallet service information.
/// </summary>
/// <example>
/// {
///   "innerServices": [{"title": "File Storage", "size": 1073741824}]
/// }
/// </example>
public class WalletServiceDto : QuotaDto
{
    /// <summary>
    /// The list of inner services.
    /// </summary>
    /// <example>[{"title": "File Storage", "size": 1073741824}]</example>
    public List<QuotaDto> InnerServices { get; set; }
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