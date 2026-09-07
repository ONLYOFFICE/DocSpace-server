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
/// GET /api/2.0/clients/{clientId}/public/info — intentionally open, no <c>x-signature</c>
/// required. It backs the OAuth2 consent screen, which must show an app's name, logo and scopes
/// to a visitor who has not signed in yet. <see cref="ClientQueryingApi.GetPublicClientInfoAsync"/>
/// never attaches the signature cookie (confirmed in the SDK source), so anonymous, guest and
/// signed-in callers all take the same code path.
/// </summary>
[Trait("Category", "OAuth2")]
public class GetPublicClientInfoTests(
    AspireAppFixture fixture)
    : ClientQueryingTestBase(fixture)
{
    [Fact]
    public async Task GetPublicClientInfo_ReturnsPublicInfoWithoutSecret()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var clientId = await CreateOAuthClientAsync("Autotest GetPublicClientInfo Fields");

        // Act
        var info = await _clientQueryingApi.GetPublicClientInfoAsync(clientId, TestContext.Current.CancellationToken);

        // Assert
        info.ClientId.Should().Be(clientId);
        info.Name.Should().Be("Autotest GetPublicClientInfo Fields");
        info.WebsiteUrl.Should().Be("https://example.com");
        info.Scopes.Should().Contain("accounts.self:read");
        // ClientInfoResponse has no ClientSecret property at all — the absence is enforced at compile time.
    }

    [Fact]
    public async Task GetPublicClientInfo_Anonymous_ReturnsPublicInfo()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var clientId = await CreateOAuthClientAsync("Autotest GetPublicClientInfo Anonymous");
        ApplySignature(null);

        // Act
        var info = await _clientQueryingApi.GetPublicClientInfoAsync(clientId, TestContext.Current.CancellationToken);

        // Assert
        info.ClientId.Should().Be(clientId);
    }

    [Fact]
    public async Task GetPublicClientInfo_Guest_ReturnsPublicInfo()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var clientId = await CreateOAuthClientAsync("Autotest GetPublicClientInfo Guest");

        var guest = await InviteGuest();
        await ApplySignatureAsync(guest);

        // Act
        var info = await _clientQueryingApi.GetPublicClientInfoAsync(clientId, TestContext.Current.CancellationToken);

        // Assert
        info.ClientId.Should().Be(clientId);
    }

    [Fact]
    public async Task GetPublicClientInfo_AnotherUsersClient_ReturnsPublicInfo()
    {
        // Arrange
        await ApplySignatureAsync(Owner);
        var clientId = await CreateOAuthClientAsync("Autotest GetPublicClientInfo Other User");

        var user = await InviteContact(EmployeeType.User);
        await ApplySignatureAsync(user);

        // Act
        var info = await _clientQueryingApi.GetPublicClientInfoAsync(clientId, TestContext.Current.CancellationToken);

        // Assert
        info.ClientId.Should().Be(clientId);
    }

    // TS ported this as test.fail("BUG 81728"): a non-existent client should be refused with 404,
    // the same as every other lookup in this suite, not silently succeed.
    [Trait("Bug", "81728")]
    [Fact]
    public async Task GetPublicClientInfo_NonExistentClientId_ThrowsNotFound()
    {
        // Arrange
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientQueryingApi.GetPublicClientInfoAsync(
                "00000000-0000-0000-0000-000000000000", TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}
