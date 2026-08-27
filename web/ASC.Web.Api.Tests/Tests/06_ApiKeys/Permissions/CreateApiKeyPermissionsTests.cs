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

namespace ASC.Web.Api.Tests.Tests._06_ApiKeys.Permissions;

/// <summary>
/// POST /api/2.0/keys — permission checks (anonymous, guest), request validation (blank/missing/
/// too-long name, empty permission list) and the stored-XSS name concern, all on the same
/// create-key endpoint.
/// </summary>
[Trait("Category", "ApiKeys")]
public class CreateApiKeyPermissionsTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    [Fact]
    public async Task CreateApiKey_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _peopleClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.CreateApiKeyAsync(
                new CreateApiKeyRequestDto("test key"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Trait("Bug", "81236")]
    [Fact]
    public async Task CreateApiKey_Guest_ReturnsForbidden()
    {
        // Arrange
        await AuthenticateAsAsync(ApiKeyActor.Guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.CreateApiKeyAsync(
                new CreateApiKeyRequestDto("test key"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("guest role");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateApiKey_Owner_BlankName_ReturnsBadRequest(string name)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.CreateApiKeyAsync(
                new CreateApiKeyRequestDto(name), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("The Name field is required.");
    }

    /// <summary>
    /// <see cref="CreateApiKeyRequestDto"/>'s constructor requires a non-null <c>name</c> and
    /// throws client-side otherwise, so a literally missing "name" property can only be sent as a
    /// raw request — same carve-out as any other required, non-nullable constructor parameter.
    /// </summary>
    [Fact]
    public async Task CreateApiKey_Owner_WithoutNameProperty_ReturnsBadRequest()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        using var response = await _peopleClient.PostAsync("api/2.0/keys", content, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("missing required properties including: 'name'");
    }

    [Trait("Bug", "81237")]
    [Fact]
    public async Task CreateApiKey_Owner_EmptyPermissionsArray_ReturnsBadRequest()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.CreateApiKeyAsync(
                new CreateApiKeyRequestDto("test key", []), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Permissions are not valid.");
    }

    [Fact]
    public async Task CreateApiKey_Owner_NameLongerThan30Characters_ReturnsBadRequest()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var longName = new string('a', 31);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.CreateApiKeyAsync(
                new CreateApiKeyRequestDto(longName), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Incorrect name. Length must be less than 30");
    }

    /// <summary>
    /// Stored HTML injection in the API key name — confirmed via email: key names containing HTML
    /// tags are stored as-is and rendered unescaped in expiry notification emails, enabling
    /// phishing links, CSS injection and tracking pixels. All payloads fit within the 30-char name
    /// limit. Fix: HTML-escape the name field before storing or before including it in email
    /// templates.
    /// </summary>
    [Trait("Bug", "82910")]
    [Theory]
    [InlineData("<a href=//evil.com>LINK</a>")]
    [InlineData("<b style=color:red>TEST</b>")]
    [InlineData("<img src=//1.2.3.4>")]
    public async Task CreateApiKey_Owner_HtmlInName_IsNotStoredUnescaped(string payload)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var result = await _apiKeysApi.CreateApiKeyWithHttpInfoAsync(
            new CreateApiKeyRequestDto(payload, expiresInDays: 1), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Name.Should().NotContain("<");
    }
}
