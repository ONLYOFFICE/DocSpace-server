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
/// PUT /api/2.0/security/activeconnections/logoutallexceptthis — every role can log itself out of
/// every connection except the one making the call.
/// </summary>
[Trait("Category", "Security")]
public class LogOutAllExceptThisTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task LogOutAllExceptThisConnection_Owner_Succeeds()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _activeConnectionsApi.LogOutAllExceptThisConnectionAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Count.Should().Be(1);
        result.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LogOutAllExceptThisConnection_OtherSession_IsInvalidatedAfterCall()
    {
        // Arrange — sign the same member in twice, keeping the first (now second-oldest) token so
        // it can be probed after the call.
        var member = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(member);
        var firstToken = member.Token;

        await _webApiClient.Authenticate(member, forceRefresh: true);

        var connections = await PollForConnectionCountAsync(2);
        connections.Should().HaveCount(2);

        // Act
        var result = await _activeConnectionsApi.LogOutAllExceptThisConnectionAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Count.Should().Be(1);
        result.Response.Should().NotBeNullOrEmpty();

        _webApiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", firstToken);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task LogOutAllExceptThisConnection_Member_Succeeds(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var result = await _activeConnectionsApi.LogOutAllExceptThisConnectionAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Count.Should().Be(1);
        result.Response.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Login events are recorded asynchronously — poll until both sign-ins show up (or the
    /// deadline passes) and hand back whatever was last observed.
    /// </summary>
    private async Task<List<ActiveConnectionsItemDto>> PollForConnectionCountAsync(int expectedCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        var items = new List<ActiveConnectionsItemDto>();

        while (true)
        {
            var result = await _activeConnectionsApi.GetAllActiveConnectionsAsync(TestContext.Current.CancellationToken);
            items = result.Response.Items ?? [];

            if (items.Count >= expectedCount || DateTime.UtcNow >= deadline)
            {
                return items;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
