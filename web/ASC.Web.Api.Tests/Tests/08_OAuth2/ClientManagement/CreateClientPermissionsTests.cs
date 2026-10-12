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
/// POST /api/2.0/clients — callers without an identity signature are refused, and a client that
/// fails field validation comes back as a 400 with a matching <c>errors</c> entry.
/// </summary>
[Trait("Category", "OAuth2")]
public class CreateClientPermissionsTests(
    AspireAppFixture fixture)
    : ClientManagementTestBase(fixture)
{
    [Fact]
    public async Task CreateClient_Anonymous_ThrowsForbidden()
    {
        // Arrange
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateClient_Guest_ThrowsForbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateClient_NameExceeds256Characters_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(new string('a', 257)), TestContext.Current.CancellationToken));

        // Assert
        AssertValidationError(exception, "name", "ErrorName",
            "client name length is expected to be between 3 and 256 characters");
    }

    [Fact]
    public async Task CreateClient_NameShorterThan3Characters_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest("ab"), TestContext.Current.CancellationToken));

        // Assert
        AssertValidationError(exception, "name", "ErrorName",
            "client name length is expected to be between 3 and 256 characters");
    }

    [Fact]
    public async Task CreateClient_WithoutScopes_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(scopes: []), TestContext.Current.CancellationToken));

        // Assert
        AssertValidationError(exception, "scopes", "EmptyFieldError", "scopes field can not be empty");
    }

    [Fact]
    public async Task CreateClient_InvalidRedirectUri_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(redirectUris: ["not-a-url"]), TestContext.Current.CancellationToken));

        // Assert
        AssertValidationError(exception, "redirect_uris", "ErrorWrongURL", "url collection has invalid entries");
    }

    [Fact]
    public async Task CreateClient_InvalidAllowedOrigins_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(allowedOrigins: ["not-a-url"]), TestContext.Current.CancellationToken));

        // Assert
        AssertValidationError(exception, "allowed_origins", "ErrorWrongURL", "url collection has invalid entries");
    }

    [Fact]
    public async Task CreateClient_InvalidWebsiteUrl_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(websiteUrl: "not-a-url"), TestContext.Current.CancellationToken));

        // Assert
        AssertValidationError(exception, "website_url", "ErrorWrongURL", "website url is expected to be passed as url");
    }

    [Fact]
    public async Task CreateClient_InvalidTermsUrl_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(termsUrl: "not-a-url"), TestContext.Current.CancellationToken));

        // Assert
        AssertValidationError(exception, "terms_url", "ErrorWrongURL", "terms url is expected to be passed as url");
    }

    [Fact]
    public async Task CreateClient_InvalidPolicyUrl_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(policyUrl: "not-a-url"), TestContext.Current.CancellationToken));

        // Assert
        AssertValidationError(exception, "policy_url", "ErrorWrongURL", "policy url is expected to be passed as url");
    }

    [Fact]
    public async Task CreateClient_InvalidLogoutRedirectUri_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(logoutRedirectUri: "not-a-url"), TestContext.Current.CancellationToken));

        // Assert
        AssertValidationError(exception, "logout_redirect_uri", "ErrorWrongURL",
            "logout redirect uri is expected to be passed as url");
    }

    [Fact]
    public async Task CreateClient_WithoutLogo_ThrowsValidationError()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientManagementApi.CreateClientAsync(
                ValidCreateClientRequest(logo: null), TestContext.Current.CancellationToken));

        // Assert
        AssertValidationError(exception, "logo", "EmptyFieldError", "client logo must not be empty");
    }
}
