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

namespace ASC.Web.Api.Tests.Tests._06_ApiKeys.Scenarios;

/// <summary>
/// Requests authenticated by an API key (<c>Authorization: Bearer sk-...</c>): only an active, unexpired key the
/// portal knows is accepted, the request runs as the key's creator and stamps <c>lastUsed</c>, and the route map
/// decides which scopes a route needs — a GET takes the read or the write scope, a route outside the map takes
/// <c>*:read</c>/<c>*:write</c>, and an anonymous endpoint called with a key is checked the same way.
/// </summary>
[Trait("Category", "ApiKeys")]
public class ApiKeyAuthenticationTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    [Fact]
    public async Task UnknownKey_ReturnsUnauthorized()
    {
        // Arrange
        var unknown = "sk-" + new string('0', 64);

        // Act
        var exception = await AsApiKeyAsync(unknown, () => Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task DeactivatedKey_ReturnsUnauthorized()
    {
        // Arrange
        var key = await CreateOwnersKeyAsync(null);
        await _apiKeysApi.UpdateApiKeyAsync(key.Id, new UpdateApiKeyRequest(isActive: false), TestContext.Current.CancellationToken);

        // Act
        var exception = await AsApiKeyAsync(key.Key, () => Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task ExpiredKey_ReturnsUnauthorizedBeforeTheDeactivationJob()
    {
        // Arrange: the key is still active; only its expiry has passed
        var key = await CreateOwnersKeyAsync(null, expiresInDays: 1);
        await ExpireAsync(key.Id);

        // Act
        var exception = await AsApiKeyAsync(key.Key, () => Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)));

        // Assert
        exception.ErrorCode.Should().Be(401);
        (await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken)).Response.Single(k => k.Id == key.Id).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ValidKey_RunsAsTheCreatorAndStampsLastUsed()
    {
        // Arrange
        var user = await AuthenticateAsAsync(ApiKeyActor.User);
        var key = (await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Used Key"), TestContext.Current.CancellationToken)).Response;
        var lastUsedBefore = key.LastUsed;

        // Act
        var self = await AsApiKeyAsync(key.Key, () => _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken));

        // Assert
        self.Response.Id.Should().Be(user.Id);
        lastUsedBefore.Should().BeNull();
        (await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken)).Response
            .Single(k => k.Id == key.Id).LastUsed.Should().NotBeNull();
    }

    [Fact]
    public async Task SelfReadScope_ReadsOwnProfile()
    {
        // Arrange
        var key = await CreateOwnersKeyAsync(["accounts.self:read"]);

        // Act
        var self = await AsApiKeyAsync(key.Key, () => _profilesApi.GetSelfProfileWithHttpInfoAsync(TestContext.Current.CancellationToken));

        // Assert
        self.StatusCode.Should().Be(HttpStatusCode.OK);
        self.Data.Response.Id.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task FilesReadScope_CannotReadOwnProfile()
    {
        // Arrange
        var key = await CreateOwnersKeyAsync(["files:read"]);

        // Act
        var exception = await AsApiKeyAsync(key.Key, () => Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task AccountsWriteScope_ReadsThePeopleList()
    {
        // Arrange: a GET of a mapped route takes the write scope as well as the read one
        var key = await CreateOwnersKeyAsync(["accounts:write"]);

        // Act
        var list = await AsApiKeyAsync(key.Key, () =>
            _profilesApi.GetAllProfilesWithHttpInfoAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        list.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FilesReadScope_CannotReadARouteOutsideTheMap()
    {
        // Arrange: settings/devtoolsaccess is in no entry of the map, so it needs *:read or *:write
        var key = await CreateOwnersKeyAsync(["files:read"]);

        // Act
        var exception = await AsApiKeyOnWebApiAsync(key.Key, () => Assert.ThrowsAsync<ApiException>(
            async () => await _accessToDevToolsApi.GetTenantAccessDevToolsSettingsAsync(TestContext.Current.CancellationToken)));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GlobalReadScope_ReadsButCannotWriteARouteOutsideTheMap()
    {
        // Arrange
        var key = await CreateOwnersKeyAsync(["*:read"]);

        // Act
        var read = await AsApiKeyOnWebApiAsync(key.Key, () =>
            _accessToDevToolsApi.GetTenantAccessDevToolsSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken));
        var write = await AsApiKeyOnWebApiAsync(key.Key, () => Assert.ThrowsAsync<ApiException>(
            async () => await _securityAccessToDevToolsApi.SetTenantDevToolsAccessSettingsAsync(
                new TenantDevToolsAccessSettingsRequestDto(true), TestContext.Current.CancellationToken)));

        // Assert
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        write.ErrorCode.Should().Be(403);
        await _webApiClient.Authenticate(Owner);
        (await _accessToDevToolsApi.GetTenantAccessDevToolsSettingsAsync(TestContext.Current.CancellationToken))
            .Response.LimitedAccessForUsers.Should().BeFalse();
    }

    [Fact]
    public async Task UnrestrictedKey_WritesARouteOutsideTheMap()
    {
        // Arrange
        var key = await CreateOwnersKeyAsync(null);

        // Act
        var write = await AsApiKeyOnWebApiAsync(key.Key, () =>
            _securityAccessToDevToolsApi.SetTenantDevToolsAccessSettingsWithHttpInfoAsync(
                new TenantDevToolsAccessSettingsRequestDto(false), TestContext.Current.CancellationToken));

        // Assert
        write.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("files:read", 403)]
    [InlineData("*:read", 200)]
    public async Task AnonymousEndpointCalledWithAKey_IsCheckedAgainstTheScopes(string scope, int expectedStatus)
    {
        // Arrange: settings/cultures is open to anonymous callers
        var key = await CreateOwnersKeyAsync([scope]);

        // Act
        var status = await AsApiKeyOnWebApiAsync(key.Key, async () =>
        {
            try
            {
                return (int)(await _commonSettingsApi.GetSupportedCulturesWithHttpInfoAsync(TestContext.Current.CancellationToken)).StatusCode;
            }
            catch (ApiException exception)
            {
                return exception.ErrorCode;
            }
        });

        // Assert
        status.Should().Be(expectedStatus);
    }

    private async Task<ApiKeyResponseDto> CreateOwnersKeyAsync(List<string>? permissions, int? expiresInDays = null)
    {
        await _peopleClient.Authenticate(Owner);

        var request = permissions is null
            ? new CreateApiKeyRequestDto("Autotest Auth Key", expiresInDays: expiresInDays)
            : new CreateApiKeyRequestDto("Autotest Auth Key", permissions, expiresInDays);

        return (await _apiKeysApi.CreateApiKeyAsync(request, TestContext.Current.CancellationToken)).Response;
    }
}
