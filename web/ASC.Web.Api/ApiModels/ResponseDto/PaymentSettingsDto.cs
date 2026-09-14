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
/// Where to buy or extend the portal's subscription, and what the subscription in force looks like.
/// </summary>
/// <example>
/// {
///   "salesEmail": "sales@example.com",
///   "buyUrl": "https://example.com/buy",
///   "standalone": false,
///   "currentLicense": {"trial": false, "dueDate": "2025-06-15T10:30:00.0000000Z"},
///   "max": 999
/// }
/// </example>
public class PaymentSettingsDto
{
    /// <summary>
    /// The vendor mailbox to write to about buying, extending or changing the subscription, picked for the portal
    /// language. It is not the portal's own support address.
    /// </summary>
    /// <example>sales@example.com</example>
    public required string SalesEmail { get; set; }

    /// <summary>
    /// Not populated: nothing fills this field in, so it always comes back empty. The help and support addresses
    /// live in `externalResources` of `GET api/2.0/settings` instead.
    /// </summary>
    /// <example>https://example.com</example>
    public string FeedbackAndSupportUrl { get; set; }

    /// <summary>
    /// The vendor page for buying or extending the subscription, chosen for the licence kind the installation was
    /// built for and for the portal language. It is a page for a person to open, not an API to call.
    /// </summary>
    /// <example>https://example.com/buy</example>
    public required string BuyUrl { get; set; }

    /// <summary>
    /// Whether this is a server installation someone administers themselves rather than a portal in the cloud,
    /// which decides whether payment means uploading a licence file or a subscription in the vendor's store.
    /// </summary>
    /// <example>false</example>
    public required bool Standalone { get; set; }

    /// <summary>
    /// The subscription in force, reduced to the two facts a payment page needs.
    /// </summary>
    /// <example>{"trial": false, "dueDate": "2025-06-15T10:30:00.0000000Z"}</example>
    public required CurrentLicenseInfo CurrentLicense { get; set; }

    /// <summary>
    /// The largest quantity of a paid item - members, storage - that may be bought in one go, `999` unless the
    /// installation configures another cap. It bounds a single purchase, not the total a portal may hold.
    /// </summary>
    /// <example>999</example>
    public required int Max { get; set; }
}

/// <summary>
/// The two facts about the subscription in force that a payment page needs.
/// </summary>
public class CurrentLicenseInfo
{
    /// <summary>
    /// Whether the portal is on a trial rather than a paid subscription. A trial expires at `dueDate` and is not
    /// extended by paying - a plan has to be bought instead.
    /// </summary>
    /// <example>false</example>
    public required bool Trial { get; set; }

    /// <summary>
    /// The day the subscription runs out, with the time of day cut off. The largest value a date can hold means
    /// it never runs out, which is how a free or unlimited plan is expressed.
    /// </summary>
    /// <example>2025-06-15T10:30:00.0000000Z</example>
    public required DateTime DueDate { get; set; }
}