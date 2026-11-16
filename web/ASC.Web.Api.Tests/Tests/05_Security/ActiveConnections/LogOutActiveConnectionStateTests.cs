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
/// PUT /api/2.0/security/activeconnections/logout/{loginEventId} for a connection that is no longer,
/// or never was, an open connection of the caller, and GET /api/2.0/security/activeconnections marking
/// the connection the request is authenticated by.
/// </summary>
[Trait("Category", "Security")]
public class LogOutActiveConnectionStateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task LogOutActiveConnection_AlreadyClosed_ReturnsFalse()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);
        var loginEventId = (await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken)).Response.Items![0].Id;
        await _webApiClient.Authenticate(Owner);
        (await _activeConnectionsApi.LogOutActiveConnectionAsync(loginEventId, TestContext.Current.CancellationToken)).Response.Should().BeTrue();

        // Act
        var result = await _activeConnectionsApi.LogOutActiveConnectionAsync(loginEventId, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().BeFalse();
    }

    [Fact]
    public async Task LogOutActiveConnection_UnknownId_ReturnsFalse()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _activeConnectionsApi.LogOutActiveConnectionAsync(int.MaxValue, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.Should().BeFalse();
    }

    /// <summary>
    /// H3 probe: the owner check runs only when the login event has a user id. A failed sign-in under an
    /// unknown login is the only login event an API caller can create without signing in as somebody;
    /// a member who is not its owner must not close it. It passes: such an event is not stored without
    /// a user id, so H3 is not reachable through it.
    /// </summary>
    [Fact]
    public async Task LogOutActiveConnection_FailedSignInOfUnknownLogin_UserCannotCloseIt()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var unknownLogin = $"nobody-{Guid.NewGuid():N}@example.com";
        await _webApiClient.Authenticate(null);
        await Assert.ThrowsAsync<ApiException>(
            async () => await _authenticationApi.AuthenticateMeAsync(new AuthRequestDto(userName: unknownLogin, password: "definitely-wrong-password"), TestContext.Current.CancellationToken));

        await _webApiClient.Authenticate(Owner);
        var failed = await PollFailedSignInAsync(unknownLogin);
        failed.Should().NotBeNull("the failed sign-in must be in the login history");

        // Act
        await _webApiClient.Authenticate(user);
        ApiException? exception = null;
        bool? closed = null;

        try
        {
            closed = (await _activeConnectionsApi.LogOutActiveConnectionAsync(failed!.Id, TestContext.Current.CancellationToken)).Response;
        }
        catch (ApiException e)
        {
            exception = e;
        }

        // Assert: either refused (403) or reported as not an open connection (false), never closed
        if (exception is not null)
        {
            exception.ErrorCode.Should().Be(403);
        }
        else
        {
            closed.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GetAllActiveConnections_BearerToken_MarksNoItemAsCurrent()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var connections = (await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        connections.LoginEvent.Should().Be(0);
        connections.Items.Should().NotBeEmpty();
        connections.Items.Should().OnlyContain(i => i.UserId == user.Id);
    }

    private async Task<LoginEventDto?> PollFailedSignInAsync(string login)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var events = (await _loginHistoryApi.GetLoginEventsByFilterAsync(cancellationToken: TestContext.Current.CancellationToken)).Response ?? [];
            var found = events.FirstOrDefault(e => e.Login == login);

            if (found is not null || DateTime.UtcNow >= deadline)
            {
                return found;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
