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
/// POST /api/2.0/keys and GET /api/2.0/keys — the shape of the secret (<c>sk-</c> and 64 lowercase hex digits, shown
/// in full only by the creation, stored as a hash, listed as <c>sk-***</c> and its last four characters), <c>*</c>
/// stored as an unrestricted key, the 1-365 day range of <c>expiresInDays</c>, unknown scopes, and the listing order.
/// </summary>
[Trait("Category", "ApiKeys")]
public class ApiKeySecretShapeTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    [Fact]
    public async Task CreateApiKey_SecretIsSkAndSixtyFourHexDigits_ShownOnlyOnceAndStoredHashed()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var created = (await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Shape Key"), TestContext.Current.CancellationToken)).Response;

        // Assert
        created.Key.Should().MatchRegex("^sk-[0-9a-f]{64}$");
        created.KeyPostfix.Should().Be(created.Key[^4..]);

        var listed = (await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken)).Response
            .Should().ContainSingle(k => k.Id == created.Id).Subject;
        listed.Key.Should().Be($"sk-***{created.KeyPostfix}");

        (await CountKeysWithStoredHashAsync(created.Key)).Should().Be(0, "the secret itself must not be stored");
    }

    [Theory]
    [InlineData("*")]
    [InlineData("*", "files:read")]
    public async Task CreateApiKey_PermissionsWithStar_StoredAsUnrestricted(params string[] permissions)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var created = (await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Star Key", [.. permissions]), TestContext.Current.CancellationToken)).Response;

        // Assert
        created.Permissions.Should().BeNullOrEmpty();
        var listed = (await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken)).Response.Single(k => k.Id == created.Id);
        listed.Permissions.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    [InlineData(-1)]
    public async Task CreateApiKey_ExpiresInDaysOutsideOneTo365_ReturnsBadRequest(int expiresInDays)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.CreateApiKeyAsync(
                new CreateApiKeyRequestDto("Autotest Expiry Key", expiresInDays: expiresInDays), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken)).Response.Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(365)]
    public async Task CreateApiKey_ExpiresInDaysAtTheBounds_SetsTheExpiry(int expiresInDays)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var created = (await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Expiry Key", expiresInDays: expiresInDays), TestContext.Current.CancellationToken)).Response;

        // Assert
        created.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateApiKey_UnknownScope_ReturnsBadRequest()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.CreateApiKeyAsync(
                new CreateApiKeyRequestDto("Autotest Scope Key", ["files:read", "files:delete"]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Permissions are not valid.");
    }

    [Fact]
    public async Task GetApiKeys_NewestFirst_KeepsInactiveAndExpiredKeys()
    {
        // Arrange: create_on is compared, so the keys are a second apart
        await _peopleClient.Authenticate(Owner);
        var oldest = (await _apiKeysApi.CreateApiKeyAsync(new CreateApiKeyRequestDto("Autotest Oldest"), TestContext.Current.CancellationToken)).Response;
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        var inactive = (await _apiKeysApi.CreateApiKeyAsync(new CreateApiKeyRequestDto("Autotest Inactive"), TestContext.Current.CancellationToken)).Response;
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        var expired = (await _apiKeysApi.CreateApiKeyAsync(new CreateApiKeyRequestDto("Autotest Expired", expiresInDays: 1), TestContext.Current.CancellationToken)).Response;

        await _apiKeysApi.UpdateApiKeyAsync(inactive.Id, new UpdateApiKeyRequest(isActive: false), TestContext.Current.CancellationToken);
        await ExpireAsync(expired.Id);

        // Act
        var keys = (await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        keys.Select(k => k.Id).Should().Equal(expired.Id, inactive.Id, oldest.Id);
        keys.Single(k => k.Id == inactive.Id).IsActive.Should().BeFalse();
        keys.Should().OnlyContain(k => k.Key.StartsWith("sk-***"));
    }
}
