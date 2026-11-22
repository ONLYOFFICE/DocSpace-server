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

public class AiAgentsMigrationPlanTests
{
    private const int MaxAttempts = 3;

    private static readonly DateTime _lastPassStart = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void GetWindowStart_NoLastPassStart_IsNull()
    {
        AiAgentsMigrationPlan.GetWindowStart(null, TimeSpan.FromMinutes(5)).Should().BeNull();
    }

    [Fact]
    public void GetWindowStart_OverlapZero_IsTheLastPassStart()
    {
        AiAgentsMigrationPlan.GetWindowStart(_lastPassStart, TimeSpan.Zero).Should().Be(_lastPassStart);
    }

    [Fact]
    public void GetWindowStart_OverlapAboveZero_OpensThatMuchEarlier()
    {
        AiAgentsMigrationPlan.GetWindowStart(_lastPassStart, TimeSpan.FromMinutes(5)).Should().Be(_lastPassStart.AddMinutes(-5));
    }

    [Fact]
    public void GetCandidates_ChangedPortalAtTheAttemptLimit_IsLeftOut()
    {
        var attempts = new Dictionary<int, int> { [7] = MaxAttempts };

        AiAgentsMigrationPlan.GetCandidates([5, 7], attempts, MaxAttempts).Should().Equal(5);
    }

    [Fact]
    public void GetCandidates_PortalOneBelowTheLimit_IsKeptAlsoWhenItDidNotChange()
    {
        var attempts = new Dictionary<int, int> { [7] = MaxAttempts - 1 };

        AiAgentsMigrationPlan.GetCandidates([5], attempts, MaxAttempts).Should().Equal(5, 7);
    }

    [Fact]
    public void GetCandidates_UnchangedPortalAtTheLimit_IsNotRetried()
    {
        var attempts = new Dictionary<int, int> { [7] = MaxAttempts };

        AiAgentsMigrationPlan.GetCandidates([], attempts, MaxAttempts).Should().BeEmpty();
    }

    [Fact]
    public void GetCandidates_PortalBothChangedAndRetried_IsListedOnce()
    {
        var attempts = new Dictionary<int, int> { [5] = 1 };

        AiAgentsMigrationPlan.GetCandidates([5, 5], attempts, MaxAttempts).Should().Equal(5);
    }

    [Fact]
    public void GetNextAttempts_SuccessAfterAFailure_RemovesTheEntry()
    {
        var attempts = new Dictionary<int, int> { [7] = 2, [9] = 1 };

        var next = AiAgentsMigrationPlan.GetNextAttempts(attempts, 7, AiAgentsMigrationPortalOutcome.Done);

        next.Should().Equal(new Dictionary<int, int> { [9] = 1 });
        attempts.Should().ContainKey(7, "the function returns a new dictionary and leaves its input alone");
    }

    [Fact]
    public void GetNextAttempts_FirstFailure_StartsFromOne()
    {
        var next = AiAgentsMigrationPlan.GetNextAttempts(new Dictionary<int, int>(), 7, AiAgentsMigrationPortalOutcome.Failed);

        next[7].Should().Be(1);
        AiAgentsMigrationPlan.IsLeftAlone(next, 7, MaxAttempts).Should().BeFalse();
    }

    [Fact]
    public void GetNextAttempts_FailureAtLimitMinusOne_ReachesTheLimit()
    {
        var attempts = new Dictionary<int, int> { [7] = MaxAttempts - 1 };

        var next = AiAgentsMigrationPlan.GetNextAttempts(attempts, 7, AiAgentsMigrationPortalOutcome.Failed);

        next[7].Should().Be(MaxAttempts);
        AiAgentsMigrationPlan.IsLeftAlone(next, 7, MaxAttempts).Should().BeTrue();
        AiAgentsMigrationPlan.GetCandidates([7], next, MaxAttempts).Should().BeEmpty();
    }

    [Fact]
    public void GetNextAttempts_BusyUnchangedPortal_IsACandidateOfTheNextPass()
    {
        var next = AiAgentsMigrationPlan.GetNextAttempts(new Dictionary<int, int>(), 7, AiAgentsMigrationPortalOutcome.Busy);

        AiAgentsMigrationPlan.GetCandidates([], next, MaxAttempts).Should().Equal(7);
        AiAgentsMigrationPlan.IsLeftAlone(next, 7, MaxAttempts).Should().BeFalse();
    }

    [Fact]
    public void GetNextAttempts_BusyPortalWithFailures_KeepsItsCount()
    {
        var attempts = new Dictionary<int, int> { [7] = MaxAttempts - 1 };

        var next = AiAgentsMigrationPlan.GetNextAttempts(attempts, 7, AiAgentsMigrationPortalOutcome.Busy);

        next[7].Should().Be(MaxAttempts - 1);
        AiAgentsMigrationPlan.GetCandidates([], next, MaxAttempts).Should().Equal(7);
    }

    [Fact]
    public void GetNextAttempts_SuccessAfterBusy_RemovesTheEntry()
    {
        var busy = AiAgentsMigrationPlan.GetNextAttempts(new Dictionary<int, int>(), 7, AiAgentsMigrationPortalOutcome.Busy);

        AiAgentsMigrationPlan.GetNextAttempts(busy, 7, AiAgentsMigrationPortalOutcome.Done).Should().BeEmpty();
    }

    [Fact]
    public void GetPassStart_NoPassPending_IsNow()
    {
        var now = _lastPassStart.AddHours(1);

        AiAgentsMigrationPlan.GetPassStart(null, now).Should().Be(now);
    }

    [Fact]
    public void GetPassStart_InterruptedPass_IsTheStartOfItsFirstPart()
    {
        AiAgentsMigrationPlan.GetPassStart(_lastPassStart, _lastPassStart.AddHours(1)).Should().Be(_lastPassStart);
    }

    [Fact]
    public void GetCandidates_AfterTheCursor_LeavesOutThePortalsUpToIt()
    {
        var attempts = new Dictionary<int, int> { [4] = 1, [8] = 1 };

        AiAgentsMigrationPlan.GetCandidates([3, 5, 9], attempts, MaxAttempts, afterId: 5).Should().Equal(8, 9);
    }

    [Fact]
    public void GetCheckpoint_KeepsTheWindowAndRecordsTheProgress()
    {
        var passStart = _lastPassStart.AddHours(1);
        var attempts = new Dictionary<int, int> { [5] = 1, [9] = 1 };

        var checkpoint = AiAgentsMigrationPlan.GetCheckpoint(_lastPassStart, passStart, 5, attempts);

        checkpoint.LastPassStart.Should().Be(_lastPassStart);
        checkpoint.PendingPassStart.Should().Be(passStart);
        checkpoint.Cursor.Should().Be(5);
        checkpoint.Attempts.Should().Equal(attempts);
    }

    [Fact]
    public void GetCompleted_ResumedPass_OpensTheNextWindowAtTheStartOfItsFirstPart()
    {
        var firstPartStart = _lastPassStart.AddHours(1);
        var checkpoint = AiAgentsMigrationPlan.GetCheckpoint(_lastPassStart, firstPartStart, 5, new Dictionary<int, int>());

        var passStart = AiAgentsMigrationPlan.GetPassStart(checkpoint.PendingPassStart, firstPartStart.AddHours(1));
        var completed = AiAgentsMigrationPlan.GetCompleted(passStart, new Dictionary<int, int>());

        completed.LastPassStart.Should().Be(firstPartStart);
        completed.PendingPassStart.Should().BeNull();
        completed.Cursor.Should().BeNull();
    }
}
