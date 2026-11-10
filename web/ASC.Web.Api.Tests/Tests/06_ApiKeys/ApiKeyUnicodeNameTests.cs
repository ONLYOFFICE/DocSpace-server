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


namespace ASC.Web.Api.Tests.Tests._06_ApiKeys;

/// <summary>
/// POST/PUT /api/2.0/keys — a key name with characters outside the Basic Multilingual Plane
/// (emoji, rare CJK) is stored and echoed back unchanged. Bug 84484: such characters were stored
/// with every code point replaced by "?" (the POST answer still carried the original text, GET
/// /keys and GET /keys/@self showed the replaced one). BMP-only names (Cyrillic, CJK, Greek,
/// Arabic, symbols) have always been stored correctly and are covered here as the control.
/// </summary>
[Trait("Category", "ApiKeys")]
public class ApiKeyUnicodeNameTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    [Theory]
    [InlineData("Ключ")]
    [InlineData("漢字")]
    [InlineData("Ελληνικά")]
    [InlineData("مفتاح")]
    [InlineData("✓ ★ ©")]
    public async Task CreateApiKey_BmpName_IsStoredUnchanged(string name)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto(name), TestContext.Current.CancellationToken);

        // Assert
        await AssertStoredNameAsync(created.Response.Id, created.Response.Key, name);
    }

    [Trait("Bug", "84484")]
    [Theory]
    [InlineData("🔑")]
    [InlineData("a🔑b")]
    [InlineData("😀😀")]
    [InlineData("𠀋 𝒳")]
    public async Task CreateApiKey_NonBmpName_IsStoredUnchanged(string name)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto(name), TestContext.Current.CancellationToken);

        // Assert
        await AssertStoredNameAsync(created.Response.Id, created.Response.Key, name);
    }

    [Trait("Bug", "84484")]
    [Fact]
    public async Task UpdateApiKey_NonBmpName_IsStoredUnchanged()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest original"), TestContext.Current.CancellationToken);

        // Act
        var result = await _apiKeysApi.UpdateApiKeyWithHttpInfoAsync(
            created.Response.Id, new UpdateApiKeyRequest("a🔑b"), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var keys = await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken);
        keys.Response.Should().ContainSingle(k => k.Id == created.Response.Id).Which.Name.Should().Be("a🔑b");
    }

    private async Task AssertStoredNameAsync(Guid id, string key, string expectedName)
    {
        var keys = await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken);
        keys.Response.Should().ContainSingle(k => k.Id == id).Which.Name.Should().Be(expectedName);

        var own = await AsApiKeyAsync(key, () => _apiKeysApi.GetApiKeyAsync(TestContext.Current.CancellationToken));
        own.Response.Name.Should().Be(expectedName);
    }
}
