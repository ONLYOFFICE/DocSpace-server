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

namespace ASC.Web.Api.Tests.Tests._05_Security.ActiveConnections;

/// <summary>
/// What ending connections leaves behind: the audit event of each logout endpoint, that a bearer-token
/// call of "all except this" keeps no connection open, and that the password-change logout leaves the
/// password as it was.
/// </summary>
[Trait("Category", "Security")]
public class ConnectionLogoutAuditTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task LogOutActiveConnection_RecordsUserLogoutActiveConnection()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        var loginEventId = (await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken)).Response.Items![0].Id;
        await _webApiClient.Authenticate(Owner);

        // Act
        await _activeConnectionsApi.LogOutActiveConnectionAsync(loginEventId, TestContext.Current.CancellationToken);

        // Assert
        var events = await PollAuditEventsAsync(MessageAction.UserLogoutActiveConnection);
        events.Should().ContainSingle().Which.UserId.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task LogOutAllExceptThisConnection_BearerToken_ClosesTheCallersConnectionToo()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        await _activeConnectionsApi.LogOutAllExceptThisConnectionAsync(TestContext.Current.CancellationToken);

        // Assert: no cookie names a survivor, so the token of the call itself is closed as well
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(401);

        await _webApiClient.Authenticate(Owner);
        var events = await PollAuditEventsAsync(MessageAction.UserLogoutActiveConnections);
        events.Should().ContainSingle().Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task LogOutAllActiveConnectionsForUser_Owner_RecordsUserLogoutActiveConnectionsForUser()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        await _webApiClient.Authenticate(Owner);

        // Act
        await _activeConnectionsApi.LogOutAllActiveConnectionsForUserAsync(user.Id, TestContext.Current.CancellationToken);

        // Assert
        var events = await PollAuditEventsAsync(MessageAction.UserLogoutActiveConnectionsForUser);
        var logout = events.Should().ContainSingle().Subject;
        logout.UserId.Should().Be(Owner.Id);
        logout.Target.Should().Contain(user.Id.ToString());
    }

    /// <summary>
    /// H7: the controller signs the caller out (<c>securityContext.Logout()</c>) before it sends
    /// <c>UserSentPasswordChangeInstructions</c>, so the audit event names the anonymous account as the one
    /// who acted instead of the user.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task LogOutAllActiveConnectionsChangePassword_RecordsInstructionsAndKeepsThePassword()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        await _activeConnectionsApi.LogOutAllActiveConnectionsChangePasswordAsync(TestContext.Current.CancellationToken);

        // Assert
        await _webApiClient.Authenticate(null);
        var signIn = await _authenticationApi.AuthenticateMeAsync(new AuthRequestDto(userName: user.Email, password: user.Password), TestContext.Current.CancellationToken);
        signIn.Response.Token.Should().NotBeNullOrEmpty();

        await _webApiClient.Authenticate(Owner);
        var events = await PollAuditEventsAsync(MessageAction.UserSentPasswordChangeInstructions);
        events.Should().ContainSingle().Which.UserId.Should().Be(user.Id);
    }

    private async Task<List<AuditEventDto>> PollAuditEventsAsync(MessageAction action)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var events = (await _auditTrailDataApi.GetAuditEventsByFilterAsync(action: action, cancellationToken: TestContext.Current.CancellationToken)).Response ?? [];

            if (events.Count > 0 || DateTime.UtcNow >= deadline)
            {
                return events;
            }

            await Task.Delay(1_000, TestContext.Current.CancellationToken);
        }
    }
}
