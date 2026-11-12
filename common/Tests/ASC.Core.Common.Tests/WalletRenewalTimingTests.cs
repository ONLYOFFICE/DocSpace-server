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
/// The two intervals of the wallet renewal service (<see cref="WalletRenewalTiming"/>) and the relation between
/// them (<c>.claude/rules/configuration.md</c> §6): the advance a subscription is renewed by is never shorter than
/// the period the service runs at, or a row that comes due between two runs is renewed past its due date.
/// </summary>
public class WalletRenewalTimingTests
{
    /// <summary>
    /// The configured period and advance, the two intervals read from them, and whether a warning was written about
    /// the period and about the advance.
    /// </summary>
    public static TheoryData<string?, string?, TimeSpan, TimeSpan, bool, bool> Timings => new()
    {
        // nothing set: a minute, and the advance follows the period
        { null, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), false, false },
        { "00:00:05", null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), false, false },
        { "00:10:00", "00:30:00", TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(30), false, false },
        { null, "00:05:00", TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), false, false },

        // an advance below the period is raised to it
        { "00:10:00", "00:00:30", TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), false, true },
        { "00:10:00", "0", TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), false, true },
        { "00:10:00", "-00:05:00", TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), false, true },

        // an advance that cannot be read, or is above its maximum
        { "00:10:00", "soon", TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), false, true },
        { "00:10:00", "2.00:00:00", TimeSpan.FromMinutes(10), TimeSpan.FromDays(1), false, true },

        // a period out of its bounds or unreadable, and the advance still follows what the period became
        { "0", null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), true, false },
        { "-00:01:00", null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), true, false },
        { "02:00:00", null, TimeSpan.FromHours(1), TimeSpan.FromHours(1), true, false },
        { "every minute", null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), true, false },
        { "02:00:00", "00:30:00", TimeSpan.FromHours(1), TimeSpan.FromHours(1), true, true }
    };

    [Theory]
    [MemberData(nameof(Timings))]
    public void Read_ReturnsThePeriodWithinItsBounds_AndAnAdvanceNotBelowIt(string? period, string? advance, TimeSpan expectedPeriod, TimeSpan expectedAdvance, bool periodWarned, bool advanceWarned)
    {
        var logger = new RecordingLogger();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WalletRenewalTiming.PeriodKey] = period,
                [WalletRenewalTiming.AdvanceKey] = advance
            })
            .Build();

        var timing = WalletRenewalTiming.Read(configuration, logger);

        timing.Period.Should().Be(expectedPeriod);
        timing.Advance.Should().Be(expectedAdvance);
        timing.Advance.Should().BeGreaterThanOrEqualTo(timing.Period);
        logger.Warnings.Count(warning => warning.Contains(WalletRenewalTiming.PeriodKey, StringComparison.Ordinal)).Should().Be(periodWarned ? 1 : 0);
        logger.Warnings.Count(warning => warning.Contains(WalletRenewalTiming.AdvanceKey, StringComparison.Ordinal)).Should().Be(advanceWarned ? 1 : 0);
        logger.Warnings.Should().HaveCount((periodWarned ? 1 : 0) + (advanceWarned ? 1 : 0));
    }
}
