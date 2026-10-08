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

namespace ASC.Notify.Tests.Periodic.Retention;

/// <summary>
/// A letter of the retention policy. The job hands each one the portal's category and the decision it
/// carries out, and the dates in the decision are what the letter discloses - so, unlike the other
/// periodic letters, these are rendered against a decision of their own, with dates no culture can
/// render by accident.
/// </summary>
public abstract class PortalRetentionLetterTestBase<TAction> : LetterTestBase<TAction>
    where TAction : PortalRetentionNotifyAction
{
    protected static DateTime DueOn { get; } = new(2026, 8, 4);

    protected static DateTime BlockOn { get; } = new(2026, 11, 2);

    protected static DateTime DeleteOn { get; } = new(2026, 12, 3);

    protected abstract PortalRetentionCategory Category { get; }

    protected virtual PortalRetentionLetter? Letter => PortalRetentionLetter.FirstNotice;

    /// <summary>The address the job hands over for a portal it has just removed; null for every other letter.</summary>
    protected virtual string? PortalDomain => null;

    protected override async Task InitAsync(TAction action, LetterScope scope)
    {
        // A lapsed tariff, so the letters that quote the due date have one to quote.
        var context = PeriodicLetterContexts.Lapsed(PeriodicLetterContexts.Fresh(scope.Tenant, DateTime.UtcNow.Date), DueOn);

        action.Init(Category, new PortalRetentionDecision(PortalRetentionStep.Notify, Letter, BlockOn, DeleteOn), PortalDomain);
        action.Tags = await action.BuildTagsAsync(context, scope.Recipient, scope.Culture);
    }

    /// <summary>A day as the letter writes it, in the culture under test.</summary>
    protected static string Day(DateTime date, LetterScope scope)
    {
        return date.ToString("D", scope.Culture);
    }

    /// <summary>A day as the letter writes it in short, month and day only, in the culture under test.</summary>
    protected static string ShortDay(DateTime date, LetterScope scope)
    {
        return date.ToString("M", scope.Culture);
    }

    /// <summary>A button caption with the branding resolved, the way the letter shows it.</summary>
    protected static string Caption(string key, LetterScope scope)
    {
        return Resource(key, scope.Culture).Replace("${" + CommonTags.LetterLogoText + "}", LetterEnvironment.LogoText);
    }
}
