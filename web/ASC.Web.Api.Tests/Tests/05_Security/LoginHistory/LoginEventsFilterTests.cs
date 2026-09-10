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
/// GET /api/2.0/security/audit/login/filter — login events filtered by user, action or paginated
/// by count. Only an Owner or a DocSpaceAdmin may call it. As with <see cref="LastLoginEventsTests"/>,
/// every read below polls on a deadline because the events it filters for are written asynchronously
/// by the login pipeline.
/// </summary>
[Trait("Category", "Security")]
public class LoginEventsFilterTests(
    AspireAppFixture fixture)
    : LoginHistoryTestBase(fixture)
{
    [Fact]
    public async Task GetLoginEventsByFilter_Owner_FiltersByUserId()
    {
        // Arrange — the member's first Authenticate call is a real sign-in.
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        await _webApiClient.Authenticate(Owner);

        // Act
        var events = await PollLoginEventsByFilterAsync(e => e.Any(x => x.UserId == user.Id), userId: user.Id);

        // Assert
        events.Should().NotBeEmpty();
        events.Select(e => e.UserId).Should().Contain(user.Id);
    }

    [Fact]
    public async Task GetLoginEventsByFilter_Owner_FiltersByAction()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        // The harness signs in with a client-side password HASH (LoginType.EmailAndPasswordHash),
        // which AuthenticationController records as LoginSuccessViaPassword — not LoginSuccess.
        var events = await PollLoginEventsByFilterAsync(e => e.Count > 0, action: MessageAction.LoginSuccessViaPassword);

        // Assert
        events.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLoginEventsByFilter_Owner_FiltersWithCountPagination()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var events = await PollLoginEventsByFilterAsync(e => e.Count == 1, count: 1);

        // Assert
        events.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLoginEventsByFilter_DocSpaceAdmin_FiltersByUserId()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        await _webApiClient.Authenticate(admin);

        // Act
        var events = await PollLoginEventsByFilterAsync(e => e.Any(x => x.UserId == user.Id), userId: user.Id);

        // Assert
        events.Should().NotBeEmpty();
        events.Select(e => e.UserId).Should().Contain(user.Id);
    }

    [Fact]
    public async Task GetLoginEventsByFilter_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginHistoryApi.GetLoginEventsByFilterAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetLoginEventsByFilter_ByRole_ReturnsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _loginHistoryApi.GetLoginEventsByFilterAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
