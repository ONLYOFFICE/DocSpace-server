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

namespace ASC.Web.Api.Tests.Tests._05_Security.LoginHistory;

/// <summary>
/// GET /api/2.0/security/audit/login/last — the latest login activity for the portal (up to the
/// last 20 events). Only an Owner or a DocSpaceAdmin may call it. Every read below polls on a
/// deadline, because the events it is asserting on are written by the login pipeline after the
/// sign-in request that produced them has already returned.
/// </summary>
[Trait("Category", "Security")]
public class LastLoginEventsTests(
    AspireAppFixture fixture)
    : LoginHistoryTestBase(fixture)
{
    [Fact]
    public async Task GetLastLoginEvents_Owner_ReturnsEvents()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var events = await PollLastLoginEventsAsync(e => e.Count > 0);

        // Assert
        events.Should().NotBeEmpty();

        var loginEvent = events[0];
        loginEvent.Id.Should().BePositive();
        loginEvent.UserId.Should().NotBeEmpty();
        loginEvent.User.Should().NotBeNullOrEmpty();
        loginEvent.Action.Should().NotBeNullOrEmpty();
        loginEvent.ActionId.Should().NotBeNull();
        loginEvent.Ip.Should().NotBeNull();
        loginEvent.Date.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLastLoginEvents_DocSpaceAdmin_ReturnsEvents()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var events = await PollLastLoginEventsAsync(e => e.Count > 0);

        // Assert
        events.Should().NotBeEmpty();

        var loginEvent = events[0];
        loginEvent.Id.Should().BePositive();
        loginEvent.UserId.Should().NotBeEmpty();
        loginEvent.User.Should().NotBeNullOrEmpty();
        loginEvent.Action.Should().NotBeNullOrEmpty();
        loginEvent.ActionId.Should().NotBeNull();
        loginEvent.Ip.Should().NotBeNull();
        loginEvent.Date.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLastLoginEvents_Owner_SeesEventsForAllUserTypes()
    {
        // Arrange — each Authenticate call below is this member's first, so it performs a real
        // sign-in and produces a login event attributed to that member's own Id.
        var docAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(docAdmin);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        await _webApiClient.Authenticate(Owner);

        var memberIds = new[] { docAdmin.Id, roomAdmin.Id, user.Id };

        // Act
        var events = await PollLastLoginEventsAsync(e => memberIds.All(id => e.Any(x => x.UserId == id)));

        // Assert
        var eventUserIds = events.Select(e => e.UserId).ToList();
        foreach (var memberId in memberIds)
        {
            eventUserIds.Should().Contain(memberId);
        }
    }

    [Fact]
    public async Task GetLastLoginEvents_DocSpaceAdmin_SeesEventsForAllUserTypes()
    {
        // Arrange
        var docAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(docAdmin);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _webApiClient.Authenticate(roomAdmin);

        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        await _webApiClient.Authenticate(docAdmin);

        var memberIds = new[] { docAdmin.Id, roomAdmin.Id, user.Id };

        // Act
        var events = await PollLastLoginEventsAsync(e => memberIds.All(id => e.Any(x => x.UserId == id)));

        // Assert
        var eventUserIds = events.Select(e => e.UserId).ToList();
        foreach (var memberId in memberIds)
        {
            eventUserIds.Should().Contain(memberId);
        }
    }

    [Fact]
    public async Task GetLastLoginEvents_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginHistoryApi.GetLastLoginEventsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetLastLoginEvents_ByRole_ReturnsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginHistoryApi.GetLastLoginEventsAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
