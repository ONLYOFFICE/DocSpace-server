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
/// PUT /api/2.0/keys/{keyId} — what an update leaves alone and what it changes: fields left out keep their values,
/// <c>["*"]</c> makes the key unrestricted, and an expired key is left untouched with <c>false</c>. Also the two open
/// defects of the endpoint: <c>["*", "files:read"]</c> stored as two scopes while the key acts unrestricted (S1), and
/// the expired answer given before the ownership check (S2).
/// </summary>
[Trait("Category", "ApiKeys")]
public class UpdateApiKeyFieldTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    [Fact]
    public async Task UpdateApiKey_OnlyName_KeepsPermissionsAndActiveState()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = await CreateKeyAsync(["files:read", "rooms:read"]);

        // Act
        await _apiKeysApi.UpdateApiKeyAsync(created.Id, new UpdateApiKeyRequest("Autotest Renamed"), TestContext.Current.CancellationToken);

        // Assert
        var stored = await ListedAsync(created.Id);
        stored.Name.Should().Be("Autotest Renamed");
        stored.Permissions.Should().BeEquivalentTo(["files:read", "rooms:read"]);
        stored.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateApiKey_OnlyIsActive_KeepsNameAndPermissions()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = await CreateKeyAsync(["files:read"]);

        // Act
        await _apiKeysApi.UpdateApiKeyAsync(created.Id, new UpdateApiKeyRequest(isActive: false), TestContext.Current.CancellationToken);

        // Assert
        var stored = await ListedAsync(created.Id);
        stored.Name.Should().Be(created.Name);
        stored.Permissions.Should().BeEquivalentTo(["files:read"]);
        stored.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// S13: the update stores <c>["*"]</c> as no list at all (<c>null</c>), where the creation stores an empty one, so
    /// the listing then leaves the required <c>permissions</c> field out and the generated client cannot read the
    /// listing of the portal any more ("Required property 'permissions' not found"). The key itself does become
    /// unrestricted; the listing has to carry an empty list as it does after a creation with <c>["*"]</c>.
    /// </summary>
    [Trait("Category", "Bug")]
    [Fact]
    public async Task UpdateApiKey_StarPermission_MakesTheKeyUnrestricted()
    {
        // Arrange: a files:read key cannot read the portal settings, which need *:read
        await _peopleClient.Authenticate(Owner);
        var created = await CreateKeyAsync(["files:read"]);

        // Act
        await _apiKeysApi.UpdateApiKeyAsync(created.Id, new UpdateApiKeyRequest(permissions: ["*"]), TestContext.Current.CancellationToken);

        // Assert
        var settings = await AsApiKeyOnWebApiAsync(created.Key, () =>
            _accessToDevToolsApi.GetTenantAccessDevToolsSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken));
        settings.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListedAsync(created.Id)).Permissions.Should().BeEmpty("the listing has to stay readable, with no scopes as after a creation");
    }

    [Fact]
    public async Task UpdateApiKey_ExpiredKey_ReturnsFalseAndLeavesItUntouched()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = await CreateKeyAsync(["files:read"], expiresInDays: 1);
        await ExpireAsync(created.Id);

        // Act
        var result = await _apiKeysApi.UpdateApiKeyAsync(
            created.Id, new UpdateApiKeyRequest("Autotest Renamed", ["rooms:read"], false), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeFalse();
        var stored = await ListedAsync(created.Id);
        stored.Name.Should().Be(created.Name);
        stored.Permissions.Should().BeEquivalentTo(["files:read"]);
        stored.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// S1: an update with <c>["*", "files:read"]</c> keeps <c>*</c> in the stored list (the run shows the listing
    /// with <c>*</c> as a scope), while the authentication treats any key holding <c>*</c> as unrestricted — so the
    /// listing and <c>keys/@self</c> show a scoped key that can in fact write everywhere. The creation normalises the same list to an unrestricted key with no scopes; the
    /// update has to do the same, so what is shown is what the key may do.
    /// </summary>
    [Trait("Category", "Bug")]
    [Fact]
    public async Task UpdateApiKey_StarAmongOtherScopes_IsStoredAsUnrestrictedLikeOnCreation()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = await CreateKeyAsync(["files:read"]);

        // Act
        await _apiKeysApi.UpdateApiKeyAsync(created.Id, new UpdateApiKeyRequest(permissions: ["*", "files:read"]), TestContext.Current.CancellationToken);

        // Assert: the key writes outside files...
        var write = await AsApiKeyOnWebApiAsync(created.Key, () =>
            _securityAccessToDevToolsApi.SetTenantDevToolsAccessSettingsWithHttpInfoAsync(
                new TenantDevToolsAccessSettingsRequestDto(false), TestContext.Current.CancellationToken));
        write.StatusCode.Should().Be(HttpStatusCode.OK);

        // ...so it must not be listed as a files:read key
        (await ListedAsync(created.Id)).Permissions.Should().BeNullOrEmpty("the key acts unrestricted, as a creation with the same list stores it");
    }

    /// <summary>
    /// S2: for an expired key the endpoint answers <c>200 false</c> before it checks who owns the key, so a Room admin
    /// or User learns that another member's key id exists and has expired. A foreign key has to be refused with 403
    /// whatever its state, as it is while it has not expired.
    /// </summary>
    [Trait("Category", "Bug")]
    [Theory]
    [InlineData(ApiKeyActor.RoomAdmin)]
    [InlineData(ApiKeyActor.User)]
    public async Task UpdateApiKey_AnotherMembersExpiredKey_ThrowsAccessDenied(ApiKeyActor actor)
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = await CreateKeyAsync(["files:read"], expiresInDays: 1);
        await ExpireAsync(created.Id);
        await AuthenticateAsAsync(actor);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.UpdateApiKeyAsync(
                created.Id, new UpdateApiKeyRequest("Autotest Taken Over"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    private async Task<ApiKeyResponseDto> CreateKeyAsync(List<string> permissions, int? expiresInDays = null)
    {
        return (await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Field Key", permissions, expiresInDays), TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>The key as the owner's listing shows it.</summary>
    private async Task<ApiKeyResponseDto> ListedAsync(Guid keyId)
    {
        await _peopleClient.Authenticate(Owner);

        return (await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken)).Response.Single(k => k.Id == keyId);
    }
}
