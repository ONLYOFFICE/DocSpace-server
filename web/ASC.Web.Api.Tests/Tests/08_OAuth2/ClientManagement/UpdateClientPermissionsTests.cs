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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2.ClientManagement;

/// <summary>
/// PUT /api/2.0/clients/{clientId} — no signature means no update, another user's client is
/// invisible (404), and the name-length bound that create enforces should also apply on update.
/// </summary>
[Trait("Category", "OAuth2")]
public class UpdateClientPermissionsTests(
    AspireAppFixture fixture)
    : ClientManagementTestBase(fixture)
{
    [Fact]
    public async Task UpdateClient_Anonymous_ThrowsForbidden()
    {
        // Arrange
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.UpdateClientAsync(
                "00000000-0000-0000-0000-000000000000",
                new UpdateClientRequest(name: "Updated Client"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateClient_Guest_ThrowsForbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.UpdateClientAsync(
                "00000000-0000-0000-0000-000000000000",
                new UpdateClientRequest(name: "Updated Client"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateClient_AnotherUsersClient_ThrowsNotFound()
    {
        // Arrange
        var ownerClient = await CreateClientAsAsync();

        var user = await InviteContact(EmployeeType.User);
        await ApplySignatureAsync(user);

        // Act — an otherwise-valid body, so the 404 comes from the ownership check, not validation.
        var result = await UpdateClientRawAsync(ownerClient.ClientId, ValidUpdateClientBody(name: "Hacked Name"));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // BUG 81670: the server accepts a name outside [3, 256] characters on update (200), while
    // create rejects the same name with 400 — the two endpoints should enforce the same bound.
    [Trait("Bug", "81670")]
    [Fact]
    public async Task UpdateClient_NameExceeds256Characters_ThrowsValidationError()
    {
        // Arrange
        var created = await CreateClientAsAsync();

        // Act — everything but the name is valid, so a 400 here can only be the name bound.
        var result = await UpdateClientRawAsync(created.ClientId, ValidUpdateClientBody(name: new string('a', 257)));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // BUG 81670: same defect as above, for the lower bound.
    [Trait("Bug", "81670")]
    [Fact]
    public async Task UpdateClient_NameShorterThan3Characters_ThrowsValidationError()
    {
        // Arrange
        var created = await CreateClientAsAsync();

        // Act
        var result = await UpdateClientRawAsync(created.ClientId, ValidUpdateClientBody(name: "ab"));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateClient_InvalidAllowedOrigins_ThrowsValidationError()
    {
        // Arrange
        var created = await CreateClientAsAsync();

        // Act — redirect_uris/scopes stay valid, so only allowed_origins is under test.
        var result = await UpdateClientRawAsync(
            created.ClientId, ValidUpdateClientBody(name: "Test Client", allowedOrigins: ["not-a-url"]));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateClient_NonExistentClient_ThrowsBadRequest()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.UpdateClientAsync(
                "00000000-0000-0000-0000-000000000000",
                new UpdateClientRequest(name: "Updated Client"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }
}
