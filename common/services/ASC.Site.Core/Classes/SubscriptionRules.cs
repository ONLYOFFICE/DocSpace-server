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

namespace ASC.Site.Core.Classes;

/// <summary>What <c>POST /subscription/unsubscribe</c> does with the address.</summary>
public enum UnsubscribeAction
{
    /// <summary>Remove the address from <c>template_unsubscribe</c>; the DocSpace accounts stay as they are.</summary>
    Resubscribe,

    /// <summary>Add the address to <c>template_unsubscribe</c>.</summary>
    UnsubscribeNews,

    /// <summary>Add the address to <c>template_unsubscribe</c> and mark its DocSpace accounts as not activated.</summary>
    UnsubscribeAll
}

/// <summary>The decisions of the subscription endpoints, kept apart from the database work so they can be tested.</summary>
public static class SubscriptionRules
{
    /// <summary>
    /// Maps the flags of the request to the action, as the site did: <c>subscribe</c> wins over <c>newsOnly</c>, and
    /// a re-subscription never activates the DocSpace accounts again.
    /// </summary>
    public static UnsubscribeAction GetUnsubscribeAction(bool subscribe, bool newsOnly)
    {
        if (subscribe)
        {
            return UnsubscribeAction.Resubscribe;
        }

        return newsOnly ? UnsubscribeAction.UnsubscribeNews : UnsubscribeAction.UnsubscribeAll;
    }

    /// <summary>
    /// Picks the list a subscription request confirms: the <c>type</c> of the id, or <see cref="SubscribeType.Common"/>
    /// when the id names none, as the site did (<c>type ?? 0</c>).
    /// </summary>
    /// <returns>
    /// The list, or <see langword="null"/> when the id is not a subscription request (a plain address: the id of a
    /// login letter or of an unsubscription, which asked for nothing) or names a list that is not a <see cref="SubscribeType"/>.
    /// </returns>
    public static SubscribeType? GetSubscribeType(SubscriptionId subscriptionId)
    {
        if (subscriptionId is not { IsSubscriptionRequest: true, HasUnknownType: false })
        {
            return null;
        }

        return subscriptionId.Type ?? SubscribeType.Common;
    }

    /// <summary>
    /// Fits a first name into <c>template_subscribe.firstname</c>: <see langword="null"/> becomes empty, a character
    /// outside the Basic Multilingual Plane becomes <c>?</c> (the table is <c>utf8mb3_general_ci</c> by the Workspace
    /// DDL, which cannot store it, and a strict-mode server would refuse the insert), and the result is cut to
    /// <see cref="DbTemplateSubscribe.FirstNameMaxLength"/> characters.
    /// </summary>
    public static string NormalizeFirstName(string firstName)
    {
        if (string.IsNullOrEmpty(firstName))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(firstName.Length);

        foreach (var rune in firstName.EnumerateRunes())
        {
            if (builder.Length == DbTemplateSubscribe.FirstNameMaxLength)
            {
                break;
            }

            builder.Append(rune.IsBmp ? (char)rune.Value : '?');
        }

        return builder.ToString();
    }
}
