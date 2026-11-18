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
/// The ways in and out that the login history records: a sign-in with a password hash, with a plain
/// password through the API, a failed sign-in and a logout, each with its own action.
/// </summary>
[Trait("Category", "Security")]
public class LoginWaysTests(
    AspireAppFixture fixture)
    : LoginHistoryTestBase(fixture)
{
    [Fact]
    public async Task SignInWithPasswordHash_RecordsLoginSuccessViaPassword()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);

        // Act: the harness signs in with the client-side password hash
        await _peopleClient.Authenticate(user);

        // Assert
        await _webApiClient.Authenticate(Owner);
        var events = await PollLoginEventsByFilterAsync(e => e.Count > 0, userId: user.Id, action: MessageAction.LoginSuccessViaPassword);
        var signIn = events.Should().ContainSingle().Subject;
        signIn.UserId.Should().Be(user.Id);
        signIn.Ip.Should().NotMatchRegex(@"^\d{1,3}(\.\d{1,3}){3}:\d+$");
        signIn.Date.UtcTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task SignInWithPlainPassword_RecordsLoginSuccessViaApi()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(null);

        // Act
        await _authenticationApi.AuthenticateMeAsync(new AuthRequestDto(userName: user.Email, password: user.Password), TestContext.Current.CancellationToken);

        // Assert
        await _webApiClient.Authenticate(Owner);
        var events = await PollLoginEventsByFilterAsync(e => e.Count > 0, userId: user.Id, action: MessageAction.LoginSuccessViaApi);
        events.Should().ContainSingle().Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task SignInWithWrongPassword_RecordsLoginFailViaApiWithTheTypedLogin()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _authenticationApi.AuthenticateMeAsync(new AuthRequestDto(userName: user.Email, password: "definitely-wrong-password"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
        await _webApiClient.Authenticate(Owner);
        var events = await PollLoginEventsByFilterAsync(e => e.Any(x => x.Login == user.Email), action: MessageAction.LoginFailViaApi);
        events.Should().Contain(e => e.Login == user.Email);
    }

    [Fact]
    public async Task GetLastLoginEvents_MoreThanTwentyStored_ReturnsTheTwentyNewest()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);

        for (var i = 0; i < 21; i++)
        {
            await _peopleClient.Authenticate(user, forceRefresh: true);
        }

        await _webApiClient.Authenticate(Owner);
        var all = await PollLoginEventsByFilterAsync(e => e.Count >= 22, count: 100);

        // Act
        var last = await PollLastLoginEventsAsync(e => e.Count >= 20);

        // Assert
        all.Count.Should().BeGreaterThan(20);
        last.Should().HaveCount(20);
        last.Select(e => e.Id).Should().Equal(all.Take(20).Select(e => e.Id));
    }

    [Fact]
    public async Task Logout_RecordsLogoutEvent()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        await _authenticationApi.LogoutAsync(TestContext.Current.CancellationToken);

        // Assert
        await _webApiClient.Authenticate(Owner);
        var events = await PollLoginEventsByFilterAsync(e => e.Count > 0, userId: user.Id, action: MessageAction.Logout);
        events.Should().ContainSingle().Which.UserId.Should().Be(user.Id);
    }
}
