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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Login;

/// <summary>
/// POST /api/2.0/authentication — a user is locked out of correct sign-ins after exhausting the
/// configured <c>attemptCount</c> of failed logins (<see cref="LoginSettingsDto.AttemptCount"/>),
/// while other accounts on the same portal are unaffected. Uses a locally constructed
/// <c>LoginHistoryApi</c> (fully qualified — the Security API namespace is not one of the ones
/// wired onto <see cref="ASC.Web.Api.Tests.ApiFactories.PortalClients"/>) to verify the lockout is
/// recorded in the audit log.
/// </summary>
[Trait("Category", "Settings")]
public class LoginLockoutTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task AuthenticateMe_TooManyFailedAttempts_LocksOutOnlyThatAccount()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var settings = await _loginSettingsApi.GetLoginSettingsAsync(TestContext.Current.CancellationToken);
        var attemptCount = settings.Response.AttemptCount;

        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(null);

        // Act — exhaust the attempt count with wrong passwords
        for (var i = 0; i < attemptCount; i++)
        {
            var attempt = await Assert.ThrowsAsync<ApiException>(
                async () => await _authenticationApi.AuthenticateMeAsync(
                    new AuthRequestsDto(userName: user.Email, password: "definitely-wrong-password"),
                    TestContext.Current.CancellationToken));

            attempt.ErrorCode.Should().Be(401);
        }

        // Assert — even the correct password is now rejected as locked out
        var lockedOut = await Assert.ThrowsAsync<ApiException>(
            async () => await _authenticationApi.AuthenticateMeAsync(
                new AuthRequestsDto(userName: user.Email, password: user.Password),
                TestContext.Current.CancellationToken));

        lockedOut.ErrorCode.Should().Be(403);
        lockedOut.ErrorContent?.ToString().Should().Contain("Too many login attempts. Please try again later");

        // An unrelated account on the same portal can still log in normally
        var ownerLogin = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: Owner.Email, password: Owner.Password),
            TestContext.Current.CancellationToken);
        ownerLogin.Response.Token.Should().NotBeNullOrEmpty();

        // The lockout is recorded in the login history as LoginFailBruteForce
        await _webApiClient.Authenticate(Owner);

        var loginHistoryApi = new DocSpace.API.SDK.Api.Security.LoginHistoryApi(
            _webApiClient, new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });

        var recorded = await PollForBruteForceEventAsync(loginHistoryApi);

        recorded.Should().Contain(e => e.ActionId == MessageAction.LoginFailBruteForce);
    }

    private static async Task<List<LoginEventDto>> PollForBruteForceEventAsync(
        DocSpace.API.SDK.Api.Security.LoginHistoryApi loginHistoryApi)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        var events = new List<LoginEventDto>();

        while (true)
        {
            var result = await loginHistoryApi.GetLoginEventsByFilterAsync(
                action: MessageAction.LoginFailBruteForce,
                cancellationToken: TestContext.Current.CancellationToken);
            events = result.Response ?? [];

            if (events.Exists(e => e.ActionId == MessageAction.LoginFailBruteForce) || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
