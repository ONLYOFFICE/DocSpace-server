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

namespace ASC.Web.Api.Tests.Tests._09_Apps;

/// <summary>
/// PUT /api/2.0/apps/{id}/enabled — switching an application on or off is recorded in the audit trail.
/// Bug 84747: the switch used to leave no trace there; the endpoint now writes AppEnabled (6119) or
/// AppDisabled (6120) with the application id as the target. Neither value is in the pinned SDK enum yet,
/// so they are compared as numbers.
/// </summary>
[Trait("Category", "Apps")]
[Trait("Category", "Bug")]
[Trait("Bug", "84747")]
public class AppsAuditTrailTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    private const int AppEnabled = 6119;
    private const int AppDisabled = 6120;

    [Theory]
    [InlineData(false, AppDisabled)]
    [InlineData(true, AppEnabled)]
    public async Task SetEnabled_Owner_IsRecordedInTheAuditTrail(bool enabled, int expectedAction)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];

        // Act
        await SaveEnabledAsync(id, enabled);

        // Assert
        var events = await PollAuditEventsAsync(e => IsAppEvent(e, expectedAction, id));
        events.Should().ContainSingle(e => IsAppEvent(e, expectedAction, id));
    }

    [Fact]
    public async Task SetEnabled_OffThenOn_RecordsBothSwitchesInOrder()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];

        // Act
        await SaveEnabledAsync(id, false);
        await SaveEnabledAsync(id, true);

        // Assert — the trail lists the newest event first
        var events = await PollAuditEventsAsync(e => IsAppEvent(e, AppEnabled, id));
        var switches = events
            .Where(e => IsAppEvent(e, AppEnabled, id) || IsAppEvent(e, AppDisabled, id))
            .Select(e => (int)e.ActionId!.Value)
            .ToList();

        switches.Should().Equal(AppEnabled, AppDisabled);
    }

    private static bool IsAppEvent(AuditEventDto auditEvent, int action, string id)
    {
        return auditEvent.ActionId is { } actionId && (int)actionId == action && auditEvent.Action.Contains(id);
    }

    private async Task<List<AuditEventDto>> PollAuditEventsAsync(Func<AuditEventDto, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var events = (await _auditTrailDataApi.GetAuditEventsByFilterAsync(cancellationToken: Ct)).Response ?? [];

            if (events.Any(until) || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(1000, Ct);
        }
    }
}
