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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2.ClientQuerying;

/// <summary>
/// GET /api/2.0/clients/{clientId}/info — the public subset of a client's details, still gated
/// behind a signature (unlike the <c>/public/info</c> endpoint below). <see cref="ClientInfoResponse"/>
/// carries no <c>client_secret</c> property at all, so — unlike the scopes endpoint — there is no
/// SDK/wire mismatch to work around here.
/// </summary>
[Trait("Category", "OAuth2")]
public class GetClientInfoTests(
    AspireAppFixture fixture)
    : ClientQueryingTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task GetClientInfo_Member_ReturnsPublicInfo(EmployeeType? employeeType)
    {
        // Arrange
        var user = employeeType is null ? Owner : await InviteContact(employeeType.Value);
        var name = $"Autotest GetClientInfo {employeeType?.ToString() ?? "Owner"}";
        await ApplySignatureAsync(user);
        var clientId = await CreateOAuthClientAsync(name);
        await ApplySignatureAsync(user);

        // Act
        var info = await _clientQueryingApi.GetClientInfoAsync(clientId, TestContext.Current.CancellationToken);

        // Assert
        info.ClientId.Should().Be(clientId);
        info.Name.Should().Be(name);
        info.WebsiteUrl.Should().Be("https://example.com");
        info.Scopes.Should().Contain("accounts.self:read");
    }

    [Fact]
    public async Task GetClientInfo_OwnerRetrievesAnotherUsersClient_ReturnsPublicInfo()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await ApplySignatureAsync(user);
        var clientId = await CreateOAuthClientAsync("Autotest GetClientInfo Other User");
        await ApplySignatureAsync(Owner);

        // Act
        var info = await _clientQueryingApi.GetClientInfoAsync(clientId, TestContext.Current.CancellationToken);

        // Assert
        info.ClientId.Should().Be(clientId);
    }

    [Fact]
    public async Task GetClientInfo_WithoutSignature_ThrowsForbidden()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var clientId = await CreateOAuthClientAsync();
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientQueryingApi.GetClientInfoAsync(clientId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetClientInfo_Guest_ThrowsForbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await ApplySignatureAsync(Owner);
        var clientId = await CreateOAuthClientAsync();
        await ApplySignatureAsync(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientQueryingApi.GetClientInfoAsync(clientId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetClientInfo_NonExistentClientId_ThrowsNotFound()
    {
        // Arrange
        await ApplySignatureAsync(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientQueryingApi.GetClientInfoAsync(
                "00000000-0000-0000-0000-000000000000", TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetClientInfo_AnotherUsersClient_ThrowsNotFound()
    {
        // Arrange
        var user1 = await InviteContact(EmployeeType.User);
        var user2 = await InviteContact(EmployeeType.User);
        await ApplySignatureAsync(user1);
        var clientId = await CreateOAuthClientAsync();
        await ApplySignatureAsync(user2);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientQueryingApi.GetClientInfoAsync(clientId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}
