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
/// One service the portal can pay for out of its wallet: a quota sold per unit rather than per period.
/// </summary>
/// <example>
/// {
///   "innerServices": [{"serviceName": "docs-cloud-dev", "title": "Developer pack"}],
///   "serviceName": "backup"
/// }
/// </example>
public class WalletServiceDto : QuotaDto
{
    /// <summary>
    /// The variants of this service that are folded into it, so a client renders one card per group instead of
    /// one per variant. It is empty when the service has no variants, and always empty in the answer of
    /// `GET api/2.0/portal/payment/walletservice`, which looks one service up on its own.
    /// </summary>
    /// <example>[{"serviceName": "docs-cloud-dev", "title": "Developer pack"}]</example>
    public List<WalletServiceDto> InnerServices { get; set; }

    /// <summary>
    /// The stable key of the service, which is what the wallet operations take as their `service` argument and
    /// what the usage reports key their entries by.
    /// </summary>
    /// <example>backup</example>
    public string ServiceName { get; set; }
}

/// <summary>
/// A quota - a plan, an add-on or a wallet service - with its price, the features it switches on and their limits.
/// </summary>
public class QuotaDto
{
    /// <summary>
    /// The identifier of the quota, which is what the tariff reports as a quota `id` and what a purchase names.
    /// A negative value belongs to a built-in quota rather than one on the price list.
    /// </summary>
    /// <example>1</example>
    public required int Id { get; set; }

    /// <summary>
    /// The quota name in the portal language, for printing rather than matching. It is empty when this build
    /// ships no wording for the quota, which is normal for a quota that is not on the public price list.
    /// </summary>
    /// <example>Basic Plan</example>
    public string Title { get; set; }

    /// <summary>
    /// What the quota costs, in the currency resolved for the request. Its `value` is empty for a quota that is
    /// not sold for money, which is what `free`, `trial` and `nonProfit` describe.
    /// </summary>
    /// <example>{"value": 99.99, "currencySymbol": "$", "iSOCurrencySymbol": "USD"}</example>
    public required PriceDto Price { get; set; }

    /// <summary>
    /// Whether this is the non-profit quota, which is granted rather than bought. A portal on it cannot buy any
    /// other plan, so a catalogue asked for plans returns this one alone.
    /// </summary>
    /// <example>false</example>
    public required bool NonProfit { get; set; }

    /// <summary>
    /// Whether this is the free quota a portal falls back to when nothing is paid for. It has no end date and
    /// the tightest limits of any quota.
    /// </summary>
    /// <example>true</example>
    public required bool Free { get; set; }

    /// <summary>
    /// Whether this is the trial quota, which grants the paid limits for a while and then expires. A trial is not
    /// extended by paying - a plan has to be bought instead.
    /// </summary>
    /// <example>false</example>
    public required bool Trial { get; set; }

    /// <summary>
    /// The features the quota switches on, each with the limit it grants and, on the quota the portal is
    /// actually on, how much of that limit is already used. A feature that is absent is off, so the list is the
    /// whole truth about what the quota includes.
    /// </summary>
    /// <example>[{"id": "00000000-0000-0000-0000-000000000001", "title": "Premium Storage"}]</example>
    public required IEnumerable<TenantQuotaFeatureDto> Features { get; set; }

    /// <summary>
    /// The per-member storage allowance an administrator has set on top of the quota, and whether it is applied
    /// at all. It describes the live portal rather than this quota, so every entry of a catalogue listing repeats
    /// the same values, and it is empty unless the portal is a server installation or its plan includes
    /// statistics.
    /// </summary>
    /// <example>{"enableQuota": true, "defaultQuota": 1073741824}</example>
    public TenantEntityQuotaSettings UsersQuota { get; set; }

    /// <summary>
    /// The same kind of per-room storage override, filled in and read the same way as `usersQuota`.
    /// </summary>
    /// <example>{"enableQuota": true, "defaultQuota": 1073741824}</example>
    public TenantEntityQuotaSettings RoomsQuota { get; set; }

    /// <summary>
    /// The same kind of per-agent storage override for AI agents, filled in and read the same way as
    /// `usersQuota`.
    /// </summary>
    /// <example>{"enableQuota": true, "defaultQuota": 1073741824}</example>
    public TenantEntityQuotaSettings AiAgentsQuota { get; set; }

    /// <summary>
    /// The storage allowance an administrator has set for the portal as a whole, which caps it below what the
    /// quota grants. Filled in under the same conditions as `usersQuota`.
    /// </summary>
    /// <example>{"enableQuota": true, "quota": 10737418240}</example>
    public TenantQuotaSettings TenantCustomQuota { get; set; }

    /// <summary>
    /// When the quota runs out, in UTC. It is empty on a quota from the catalogue, which has no date until it is
    /// bought, and on a quota that never expires.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// One feature a quota switches on, with the limit it grants and how much of that limit is used.
/// </summary>
public class TenantQuotaFeatureDto : IEquatable<TenantQuotaFeatureDto>
{
    /// <summary>
    /// The stable key of the feature - `total_size`, `manager`, `room`, `backup` and so on. It is the value to
    /// branch on, since `title` is prose in the portal language.
    /// </summary>
    /// <example>total_size</example>
    public string Id { get; set; }

    /// <summary>
    /// The feature described in the portal language, with its limit already substituted into the sentence, so it
    /// can be printed as it is. It is empty when this build ships no wording for the feature.
    /// </summary>
    /// <example>Premium Storage</example>
    public string Title { get; set; }

    /// <summary>
    /// The feature's icon as SVG markup to render inline - not a URL to fetch. It is filled in only when the
    /// quota comes from the catalogue, and left empty on the quota the portal is actually on, on a feature that
    /// this quota switches off, and on a feature that ships no icon.
    /// </summary>
    /// <example>&lt;svg viewBox="0 0 24 24"&gt;&lt;path d="..."/&gt;&lt;/svg&gt;</example>
    public string Image { get; set; }

    /// <summary>
    /// The limit the feature grants, whose shape follows `type`: a byte count for `size`, a whole number for
    /// `count`, and a boolean for `flag`. On the two numeric kinds `-1` means the limit is unbounded.
    /// </summary>
    /// <example>107374182400</example>
    public object Value { get; set; }

    /// <summary>
    /// How to read `value` and `used`: `size` for bytes, `count` for a number of things, `flag` for a feature
    /// that is merely on or off.
    /// </summary>
    /// <example>size</example>
    public string Type { get; set; }

    /// <summary>
    /// How much of the limit is already used. It is present only on the quota the portal is actually on, and
    /// only for a feature whose consumption is counted; a guest is shown none of these figures and a plain member
    /// only the one for total size, so an absent value can mean the caller may not see it rather than that
    /// nothing is used.
    /// </summary>
    /// <example>{"value": 53687091200, "title": "50 GB used"}</example>
    public FeatureUsedDto Used { get; set; }

    /// <summary>
    /// What the feature is charged as, in the portal language - for instance the per-unit price of an add-on. It
    /// is filled in only for a feature that costs money on top of the plan.
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
/// What a quota costs, and the currency that amount is in.
/// </summary>
public class PriceDto
{
    /// <summary>
    /// The amount for one billing period, per unit for a quota sold by the unit. It is empty for a quota that is
    /// not sold for money - the free, trial and non-profit ones - and for a quota this installation has no price
    /// list entry for.
    /// </summary>
    /// <example>99.99</example>
    public decimal? Value { get; set; }

    /// <summary>
    /// The symbol to print in front of `value`, such as `$`. It is chosen for the currency, not for the portal
    /// language, so it is not a localised format.
    /// </summary>
    /// <example>$</example>
    public string CurrencySymbol { get; set; }

    /// <summary>
    /// The currency as a three-letter ISO 4217 code, which is the value to compare on when `currencySymbol` is
    /// ambiguous between currencies that share a sign.
    /// </summary>
    /// <example>USD</example>
    public string ISOCurrencySymbol { get; set; }
}

/// <summary>
/// How much of one quota feature the portal has already consumed.
/// </summary>
public class FeatureUsedDto
{
    /// <summary>
    /// The amount consumed, in the same shape as the feature's own `value` - a byte count for a `size` feature,
    /// a whole number for a `count` one. It is a portal-wide figure, not the caller's own share.
    /// </summary>
    /// <example>53687091200</example>
    public required object Value { get; set; }

    /// <summary>
    /// The same figure as a sentence in the portal language, ready to print. It is empty when this build ships no
    /// wording for the feature.
    /// </summary>
    /// <example>50 GB used</example>
    public string Title { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class WalletServiceDtoMapper
{
    public static partial WalletServiceDto MapToWalletServiceDto(this QuotaDto source);
}
