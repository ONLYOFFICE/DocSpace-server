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

namespace ASC.Core.Common.Tests;

/// <summary>
/// The decisions the setup wizard and its page take on the editing service's trial. The paths that take them need an
/// Enterprise installation next to an editing service on its trial, which no test profile starts, so the decisions are
/// tested here as functions of what was read.
/// </summary>
public sealed class DocsTrialDecisionTests
{
    private static readonly DateTime _now = new(2026, 11, 17, 15, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(DocsTrialState.Unknown, false, true)]
    [InlineData(DocsTrialState.NotTrial, false, true)]
    [InlineData(DocsTrialState.Expired, false, true)]
    [InlineData(DocsTrialState.Active, false, true)]
    [InlineData(DocsTrialState.Active, true, false)]
    // Only an active trial stands in for the file, whatever the caller says about it
    [InlineData(DocsTrialState.NotTrial, true, true)]
    [InlineData(DocsTrialState.Expired, true, true)]
    [InlineData(DocsTrialState.Unknown, true, true)]
    public void IsLicenseFileRequired_Answer_AsksForAFileUnlessTheTrialWouldBeInForce(DocsTrialState state, bool trialWouldBeInForce, bool expected)
    {
        var answer = new DocsTrialCheck(state, state is DocsTrialState.Active or DocsTrialState.Expired ? new DocsTrialLicense(_now.AddDays(10)) : null);

        DocsTrialDecision.IsLicenseFileRequired(answer, trialWouldBeInForce).Should().Be(expected);
    }

    [Theory]
    // No own trial, or one that ended: a file is required whatever the editing service last said
    [InlineData(null, null, null, false)]
    [InlineData(-1, null, null, false)]
    [InlineData(-1, DocsTrialState.Active, 20, false)]
    // No answer known, or only a failure: the own trial in force through today is enough, by decision
    [InlineData(0, null, null, true)]
    [InlineData(0, DocsTrialState.Active, 20, true)]
    [InlineData(0, DocsTrialState.Expired, -3, false)]
    [InlineData(null, DocsTrialState.Active, 20, false)]
    [InlineData(-1, DocsTrialState.NotTrial, null, false)]
    [InlineData(10, null, null, true)]
    [InlineData(10, DocsTrialState.Unknown, null, true)]
    // The last answer asked for a file: no trial, an ended one
    [InlineData(10, DocsTrialState.NotTrial, null, false)]
    [InlineData(10, DocsTrialState.Expired, -3, false)]
    // The last answer was an active trial: only one that would still be in force once applied
    [InlineData(10, DocsTrialState.Active, 20, true)]
    [InlineData(10, DocsTrialState.Active, 0, true)]
    [InlineData(10, DocsTrialState.Active, -1, false)]
    public void CompletesOnOwnTrial_SilentService_DecidesByTheEnds(int? ownTrialDays, DocsTrialState? lastKnownState, int? lastKnownTrialDays, bool expected)
    {
        DateTime? ownTrialEnd = ownTrialDays.HasValue ? _now.AddDays(ownTrialDays.Value) : null;
        var lastKnown = lastKnownState.HasValue
            ? new DocsTrialCheck(lastKnownState.Value, lastKnownTrialDays.HasValue ? new DocsTrialLicense(_now.Date.AddDays(lastKnownTrialDays.Value)) : null)
            : null;

        DocsTrialDecision.CompletesOnOwnTrial(ownTrialEnd, lastKnown, _now).Should().Be(expected);
    }
}
