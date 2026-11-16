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

namespace ASC.Files.Tests.Tests._01_Files.Forms.BuiltinDb;

/// <summary>
/// Every move into the portal's own database fences the portal's writes while it copies, so a move that keeps failing
/// is tried again after a short wait only a few times in a row, then once a day
/// (<see cref="FormsDbProvisioningService.NextIsolationFailure"/>). Pure logic, no portal.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class BuiltinDbIsolationRetryTests
{
    private static readonly DateTime _now = new(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FailuresInARow_WaitMinutes_ThenADay()
    {
        var waits = new List<TimeSpan>();
        FormsDbIsolationFailure? failure = null;
        for (var attempt = 1; attempt <= FormsDbProvisioningService.IsolationAttempts + 2; attempt++)
        {
            failure = FormsDbProvisioningService.NextIsolationFailure(failure, _now);

            failure.Attempts.Should().Be(attempt);
            waits.Add(failure.RetryAt - _now);
        }

        waits.Take(FormsDbProvisioningService.IsolationAttempts - 1).Should().AllSatisfy(w => w.Should().Be(TimeSpan.FromMinutes(5)));
        waits.Skip(FormsDbProvisioningService.IsolationAttempts - 1).Should().AllSatisfy(w => w.Should().Be(TimeSpan.FromDays(1)));
    }
}
