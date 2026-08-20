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

namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// A letter the daily tariff job sends. These have no <c>Init</c>: a periodic letter is handed the portal
/// state it goes out for and builds its tags from that, so the equivalent call is
/// <see cref="BasePeriodicNotifyAction.BuildTagsAsync"/>.
///
/// The portal state is an input rather than a tag, which is what keeps this cheap: it is assembled in
/// memory by <see cref="PeriodicLetterContexts"/> instead of being arranged in the database, so a letter
/// about an expiring tariff does not need a portal whose tariff actually expires.
/// </summary>
/// <typeparam name="TAction">The periodic notify action that sends this letter in production.</typeparam>
public abstract class PeriodicLetterTestBase<TAction> : PortalLetterTestBase<TAction>
    where TAction : BasePeriodicNotifyAction
{
    protected override async Task InitAsync(TAction action, LetterScope scope)
    {
        action.Tags = await action.BuildTagsAsync(BuildContext(scope), scope.Recipient, scope.Culture);
    }

    /// <summary>
    /// The portal this letter goes out for. The default is a portal on a paid tariff with nothing unusual
    /// about it; a letter that quotes a date — how long the grace period has left, when the tariff lapsed
    /// — overrides this with the state it is actually sent for.
    /// </summary>
    protected virtual PeriodicLetterContext BuildContext(LetterScope scope)
    {
        return PeriodicLetterContexts.Paid(scope.Tenant);
    }
}
