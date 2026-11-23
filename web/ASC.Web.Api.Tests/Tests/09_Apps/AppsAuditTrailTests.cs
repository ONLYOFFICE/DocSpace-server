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
        var events = await PollAuditEventsAsync(all => all.Any(e => IsAppEvent(e, expectedAction, id)));
        events.Should().ContainSingle(e => IsAppEvent(e, expectedAction, id));
    }

    [Fact]
    public async Task SetEnabled_OffThenOn_RecordsBothSwitchesInOrder()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];

        // Act
        await SaveEnabledAsync(id, false);
        // The audit date is stored to the second and same-second events are written by parallel consumers,
        // so the order of two events is defined only when they fall into different seconds.
        await Task.Delay(TimeSpan.FromSeconds(1.1), Ct);
        await SaveEnabledAsync(id, true);

        // Assert — the trail lists the newest event first
        var events = await PollAuditEventsAsync(all => all.Any(e => IsAppEvent(e, AppEnabled, id)) && all.Any(e => IsAppEvent(e, AppDisabled, id)));
        var switches = events
            .Where(e => IsAppEvent(e, AppEnabled, id) || IsAppEvent(e, AppDisabled, id))
            .Select(e => (int)e.ActionId!.Value)
            .ToList();

        switches.Should().Equal(AppEnabled, AppDisabled);
    }

    [Fact]
    public async Task SetEnabled_SameValueTwice_IsRecordedBothTimes()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];

        // Act — the second call repeats the value the first one stored
        await SaveEnabledAsync(id, false);
        await SaveEnabledAsync(id, false);

        // Assert
        var events = await PollAuditEventsAsync(all => all.Count(e => IsAppEvent(e, AppDisabled, id)) >= 2);
        events.Where(e => IsAppEvent(e, AppDisabled, id)).Should().HaveCount(2);
    }

    [Fact]
    public async Task SetEnabled_RefusedCallAndUnknownId_LeaveNoEvent()
    {
        // Arrange
        var ids = await PickAppsAsync(2);
        var (refusedId, markerId) = (ids[0], ids[1]);
        const string unknownId = "no-such-app-audit";
        var member = await InviteMember(EmployeeType.User);

        // Act — a call the member may not make, then an id the owner cannot switch
        await _webApiClient.Authenticate(member);
        var refused = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetEnabledAsync(refusedId, new SetAppEnabledRequest(false), Ct));
        await _webApiClient.Authenticate(Owner);
        var unknown = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetEnabledAsync(unknownId, new SetAppEnabledRequest(false), Ct));
        await SaveEnabledAsync(markerId, false);

        // Assert — once the later, accepted switch is in the trail, the two refused calls are not
        refused.ErrorCode.Should().Be(403);
        unknown.ErrorCode.Should().Be(404);
        var events = await PollUntilSettledAsync(markerId);
        events.Should().NotContain(e => e.Action.Contains(refusedId) || e.Action.Contains(unknownId));
    }

    [Fact]
    public async Task SetSettings_SaveAndDrop_LeaveNoEvent()
    {
        // Arrange
        var ids = await PickAppsAsync(2);
        var (settingsId, markerId) = (ids[0], ids[1]);

        // Act
        await SaveSettingsAsync(settingsId, """{"audit":"probe"}""");
        await SaveSettingsAsync(settingsId, "null");
        await SaveEnabledAsync(markerId, false);

        // Assert — once the later switch is in the trail, nothing names the application whose document changed
        var events = await PollUntilSettledAsync(markerId);
        events.Should().NotContain(e => e.Action.Contains(settingsId));
    }

    /// <summary>
    /// Waits for the marker switch, then a little longer: the events are written by parallel consumers, so an event
    /// published before the marker could still land after it.
    /// </summary>
    private async Task<List<AuditEventDto>> PollUntilSettledAsync(string markerId)
    {
        var withMarker = await PollAuditEventsAsync(all => all.Any(e => IsAppEvent(e, AppDisabled, markerId)));
        withMarker.Should().Contain(e => IsAppEvent(e, AppDisabled, markerId), "the accepted switch has to reach the trail first");

        await Task.Delay(TimeSpan.FromSeconds(3), Ct);

        return (await _auditTrailDataApi.GetAuditEventsByFilterAsync(cancellationToken: Ct)).Response ?? [];
    }

    private static bool IsAppEvent(AuditEventDto auditEvent, int action, string id)
    {
        return auditEvent.ActionId is { } actionId && (int)actionId == action && auditEvent.Action.Contains(id);
    }

    private async Task<List<AuditEventDto>> PollAuditEventsAsync(Func<List<AuditEventDto>, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var events = (await _auditTrailDataApi.GetAuditEventsByFilterAsync(cancellationToken: Ct)).Response ?? [];

            if (until(events) || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(1000, Ct);
        }
    }
}
