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

namespace ASC.Site.Core.Dtos;

/// <summary>The body of <c>POST /subscription/unsubscribe</c>.</summary>
/// <param name="UnsubscribeId">The id from the link of a letter: an <c>emailKey</c> of <c>/key/generate</c>, an <c>unsubscribeId</c> of <c>/key/generateunsubscribeid</c>, or the id of the footer link of DocSpace letters.</param>
/// <param name="Subscribe"><see langword="true"/> to subscribe the address to the marketing letters again, <see langword="false"/> to unsubscribe it.</param>
/// <param name="NewsOnly">With <c>Subscribe = false</c>: <see langword="true"/> unsubscribes from the marketing letters only, <see langword="false"/> also from the letters of every DocSpace portal.</param>
public record UnsubscribeRequestDto(string UnsubscribeId, bool? Subscribe, bool? NewsOnly);

/// <summary>The answer of <c>POST /subscription/unsubscribe</c>.</summary>
/// <param name="Email">The address the id holds, lowercased; the site shows it on the page.</param>
public record UnsubscribeResponseDto(string Email);

/// <summary>The body of <c>POST /subscription/subscribe</c>.</summary>
/// <param name="UnsubscribeId">The id from the confirmation letter: an <c>unsubscribeId</c> of <c>/key/generateunsubscribeid</c> whose data is the JSON <c>{email, firstname, type}</c>.</param>
/// <param name="Type">The list, by name or number; when omitted, the <c>type</c> of the id, and <see cref="SubscribeType.Common"/> when the id has none.</param>
public record SubscribeRequestDto(string UnsubscribeId, SubscribeType? Type);

/// <summary>The answer of <c>POST /subscription/subscribe</c>.</summary>
/// <param name="Email">The address the id holds, lowercased.</param>
/// <param name="Type">The list the subscription was written for.</param>
/// <param name="Result">Whether the subscription was added or was already there.</param>
public record SubscribeResponseDto(string Email, SubscribeType Type, SubscribeResult Result);

/// <summary>What <c>POST /subscription/subscribe</c> did.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<SubscribeResult>))]
public enum SubscribeResult
{
    /// <summary>The subscription was written.</summary>
    [JsonStringEnumMemberName("added")]
    Added,

    /// <summary>The subscription was already there; the address was removed from the unsubscribed list.</summary>
    [JsonStringEnumMemberName("updated")]
    Updated
}

/// <summary>
/// The mailing list of a subscription, stored as the <c>type</c> column of <c>template_subscribe</c>.
/// The names and values are the ones the site uses.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SubscribeType>))]
public enum SubscribeType
{
    /// <summary>The list every subscription form of the site uses.</summary>
    Common = 0,

    /// <summary>A list the site declares but never writes; kept because the mailing system may still read it.</summary>
    AppServer = 1,

    /// <summary>A list the site declares but never writes; kept because the mailing system may still read it.</summary>
    Opensource = 2
}
