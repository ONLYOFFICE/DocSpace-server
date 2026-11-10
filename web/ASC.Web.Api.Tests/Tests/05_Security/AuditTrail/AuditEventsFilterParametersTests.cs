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


namespace ASC.Web.Api.Tests.Tests._05_Security.AuditTrail;

/// <summary>
/// GET /api/2.0/security/audit/events/filter with its filter parameters: the module, the action, the
/// user and the page length, and the split between audit events and login events (an action below
/// 2000 is a login event and is stored in the login history, never in the audit trail).
/// </summary>
[Trait("Category", "Security")]
public class AuditEventsFilterParametersTests(
    AspireAppFixture fixture)
    : AuditTrailTestBase(fixture)
{
    /// <summary>
    /// H1: a <c>moduleType</c> that one of the products does not provide (the Login product has only the
    /// <c>None</c> location) made the repository dereference a missing module mapper, so the filter
    /// answered 500 instead of the filtered list.
    /// </summary>
    [Theory]
    [Trait("Category", "Bug")]
    [InlineData(LocationType.Files)]
    [InlineData(LocationType.Contacts)]
    public async Task GetAuditEventsByFilter_ModuleType_ReturnsFilteredList(LocationType moduleType)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var events = await _auditTrailDataApi.GetAuditEventsByFilterAsync(moduleType: moduleType, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        events.StatusCode.Should().Be(200);
        events.Response.Should().OnlyContain(e => e.Location == moduleType);
    }

    [Fact]
    public async Task GetAuditEventsByFilter_Action_ReturnsOnlyThatAction()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var member = await InviteContact(EmployeeType.User);
        await TriggerAuditEventAsync(member);
        await PollAuditEventsByFilterAsync(e => e.Any(x => x.ActionId == MessageAction.UserSentPasswordChangeInstructions));

        // Act
        var events = (await _auditTrailDataApi.GetAuditEventsByFilterAsync(
            action: MessageAction.UserSentPasswordChangeInstructions,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        events.Should().NotBeEmpty();
        events.Should().OnlyContain(e => e.ActionId == MessageAction.UserSentPasswordChangeInstructions);
    }

    [Fact]
    public async Task GetAuditEventsByFilter_UserId_ReturnsOnlyThatUsersEvents()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await InviteContact(EmployeeType.User, admin);
        await InviteContact(EmployeeType.User);
        var all = await PollAuditEventsByFilterAsync(e => e.Any(x => x.UserId == admin.Id) && e.Any(x => x.UserId == Owner.Id));
        all.Should().Contain(e => e.UserId == Owner.Id, "the owner's events are what the filter has to drop");

        // Act
        var events = (await _auditTrailDataApi.GetAuditEventsByFilterAsync(
            userId: admin.Id,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        events.Should().NotBeEmpty();
        events.Should().OnlyContain(e => e.UserId == admin.Id);
    }

    [Fact]
    public async Task GetAuditEventsByFilter_Count_LimitsThePageAndStartIndexSkips()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var member = await InviteContact(EmployeeType.User);
        await TriggerAuditEventAsync(member);
        await TriggerAuditEventAsync(member);
        var all = await PollAuditEventsByFilterAsync(e => e.Count(x => x.ActionId == MessageAction.UserSentPasswordChangeInstructions) >= 2);

        // Act
        var first = (await _auditTrailDataApi.GetAuditEventsByFilterAsync(count: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var second = (await _auditTrailDataApi.GetAuditEventsByFilterAsync(count: 1, startIndex: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        all.Count.Should().BeGreaterThanOrEqualTo(2);
        first.Should().ContainSingle().Which.Id.Should().Be(all[0].Id);
        second.Should().ContainSingle().Which.Id.Should().Be(all[1].Id);
    }

    [Fact]
    public async Task GetLastAuditEvents_MoreThanTwentyStored_ReturnsTheTwentyNewest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _peopleClient.Authenticate(Owner);
        var member = await InviteContact(EmployeeType.User);

        for (var i = 0; i < 21; i++)
        {
            await TriggerAuditEventAsync(member);
        }

        var all = await PollAuditEventsByFilterAsync(e => e.Count(x => x.ActionId == MessageAction.UserSentPasswordChangeInstructions) >= 21);

        // Act
        var last = await PollLastAuditEventsAsync(e => e.Count >= 20);

        // Assert
        all.Count.Should().BeGreaterThan(20);
        last.Should().HaveCount(20);
        last.Select(e => e.Id).Should().Equal(all.Take(20).Select(e => e.Id));
    }

    [Fact]
    public async Task GetAuditEventsByFilter_NewestFirst()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var member = await InviteContact(EmployeeType.User);
        await TriggerAuditEventAsync(member);

        // Act
        var events = await PollAuditEventsByFilterAsync(e => e.Count >= 2);

        // Assert
        events.Select(e => e.Date.UtcTime).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task AuditEvent_IsNotALoginEvent_AndLoginEventIsNotAnAuditEvent()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var member = await InviteContact(EmployeeType.User);
        await TriggerAuditEventAsync(member);
        var auditEvents = await PollAuditEventsByFilterAsync(e => e.Any(x => x.ActionId == MessageAction.UserSentPasswordChangeInstructions));

        // Act
        var loginEvents = (await _loginHistoryApi.GetLoginEventsByFilterAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        auditEvents.Should().OnlyContain(e => (int)e.ActionId! >= 2000);
        loginEvents.Should().NotBeEmpty();
        loginEvents.Should().OnlyContain(e => (int)e.ActionId! < 2000);
    }

    [Fact]
    public async Task AuditEvent_Fields_IpHasNoPort_UserIsNamed()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _peopleClient.Authenticate(Owner);
        var member = await InviteContact(EmployeeType.User);
        await TriggerAuditEventAsync(member);

        // Act
        var events = await PollAuditEventsByFilterAsync(e => e.Any(x => x.ActionId == MessageAction.UserSentPasswordChangeInstructions));
        var reminder = events.First(e => e.ActionId == MessageAction.UserSentPasswordChangeInstructions);

        // Assert
        reminder.UserId.Should().Be(Owner.Id);
        reminder.User.Should().NotBeNullOrEmpty();
        reminder.Action.Should().NotBeNullOrEmpty();
        reminder.Ip.Should().NotBeNull();
        reminder.Ip.Should().NotMatchRegex(@"^\d{1,3}(\.\d{1,3}){3}:\d+$");
        reminder.Date.UtcTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }
}
