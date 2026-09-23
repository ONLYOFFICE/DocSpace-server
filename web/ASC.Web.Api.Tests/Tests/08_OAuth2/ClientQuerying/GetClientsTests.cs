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
/// GET /api/2.0/clients — the caller's own OAuth2 clients, full details included. A
/// <c>DocSpaceAdmin</c> sees every client registered on the tenant; a plain <c>User</c> or a
/// <c>RoomAdmin</c> sees only the clients they registered themselves.
///
/// The positive cases read raw JSON: the generated <c>PageableResponse.LastCreatedOn</c> is a
/// non-nullable <c>DateTime</c>, but the field is <c>null</c> on the wire whenever the caller has
/// no last-seen cursor — every case here — so the typed <c>GetClientsAsync</c> throws during
/// deserialization before returning. See <see cref="ClientQueryingTestBase"/> for the full
/// explanation; this is an SDK/OpenAPI generation defect worth reporting.
/// </summary>
[Trait("Category", "OAuth2")]
public class GetClientsTests(
    AspireAppFixture fixture)
    : ClientQueryingTestBase(fixture)
{
    [Fact]
    public async Task GetClients_Owner_ContainsOwnClient()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var clientId = await CreateOAuthClientAsync("Autotest GetClients Owner");
        await ApplySignatureAsync(Owner);

        // Act
        var page = await GetPageAsync("api/2.0/clients?limit=50");
        var ids = page.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("client_id").GetString());

        // Assert
        ids.Should().Contain(clientId);
    }

    [Fact]
    public async Task GetClients_ReturnsFullClientDataIncludingSecret()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var clientId = await CreateOAuthClientAsync("Autotest GetClients Fields");
        await ApplySignatureAsync(Owner);

        // Act
        var page = await GetPageAsync("api/2.0/clients?limit=50");
        var client = page.GetProperty("data").EnumerateArray()
            .Single(c => c.GetProperty("client_id").GetString() == clientId);

        // Assert
        client.GetProperty("name").GetString().Should().Be("Autotest GetClients Fields");
        client.GetProperty("client_secret").GetString().Should().NotBeNullOrEmpty();
        client.GetProperty("website_url").GetString().Should().Be("https://example.com");
        client.GetProperty("scopes").EnumerateArray().Select(s => s.GetString()).Should().Contain("accounts.self:read");
    }

    [Fact]
    public async Task GetClients_UserWithNoClients_ReturnsEmptyList()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await ApplySignatureAsync(user);

        // Act
        var page = await GetPageAsync("api/2.0/clients?limit=50");

        // Assert
        page.GetProperty("data").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task GetClients_MultipleClients_AllAppearInList()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var clientId1 = await CreateOAuthClientAsync("Autotest GetClients Multi 1");
        await ApplySignatureAsync(Owner);
        var clientId2 = await CreateOAuthClientAsync("Autotest GetClients Multi 2");
        await ApplySignatureAsync(Owner);

        // Act
        var page = await GetPageAsync("api/2.0/clients?limit=50");
        var ids = page.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("client_id").GetString()).ToList();

        // Assert
        ids.Should().Contain([clientId1, clientId2]);
    }

    [Fact]
    public async Task GetClients_ReturnsPaginationFields()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        await CreateOAuthClientAsync("Autotest GetClients Pagination");
        await ApplySignatureAsync(Owner);

        // Act
        var page = await GetPageAsync("api/2.0/clients?limit=50");

        // Assert
        page.GetProperty("limit").GetInt32().Should().Be(50);
    }

    [Fact]
    public async Task GetClients_DocSpaceAdmin_SeesAllTenantClients()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var ownerClientId = await CreateOAuthClientAsync("Autotest GetClients DSAdmin Visibility");

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await ApplySignatureAsync(admin);

        // Act
        var page = await GetPageAsync("api/2.0/clients?limit=50");
        var ids = page.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("client_id").GetString());

        // Assert
        ids.Should().Contain(ownerClientId);
    }

    [Fact]
    public async Task GetClients_User_SeesOnlyOwnClients()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var ownerClientId = await CreateOAuthClientAsync("Autotest GetClients Owner Only");

        var user = await InviteContact(EmployeeType.User);
        await ApplySignatureAsync(user);

        // Act
        var page = await GetPageAsync("api/2.0/clients?limit=50");
        var ids = page.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("client_id").GetString());

        // Assert
        ids.Should().NotContain(ownerClientId);
    }

    [Fact]
    public async Task GetClients_RoomAdmin_SeesOnlyOwnClients()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var ownerClientId = await CreateOAuthClientAsync("Autotest GetClients RoomAdmin Visibility");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await ApplySignatureAsync(roomAdmin);

        // Act
        var page = await GetPageAsync("api/2.0/clients?limit=50");
        var ids = page.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("client_id").GetString());

        // Assert
        ids.Should().NotContain(ownerClientId);
    }

    [Fact]
    public async Task GetClients_WithoutSignature_ThrowsForbidden()
    {
        // Arrange
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientQueryingApi.GetClientsAsync(50, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetClients_Guest_ThrowsForbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await ApplySignatureAsync(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientQueryingApi.GetClientsAsync(50, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
