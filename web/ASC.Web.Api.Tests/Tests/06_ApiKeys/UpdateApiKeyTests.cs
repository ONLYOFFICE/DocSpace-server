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
/// PUT /api/2.0/keys/{keyId} — every role updates its own key's name and permissions, a
/// DocSpaceAdmin updates the portal owner's key, and the owner toggles a key's active state.
/// </summary>
[Trait("Category", "ApiKeys")]
public class UpdateApiKeyTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(ApiKeyActorData.AllRoles), MemberType = typeof(ApiKeyActorData))]
    public async Task UpdateApiKey_ByRole_UpdatesOwnKey(ApiKeyActor actor)
    {
        // Arrange
        await AuthenticateAsAsync(actor);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Update Key"), TestContext.Current.CancellationToken);

        // Act
        var result = await _apiKeysApi.UpdateApiKeyWithHttpInfoAsync(
            created.Response.Id,
            new UpdateApiKeyRequest("Autotest Renamed Key", ["files:read", "rooms:read"]),
            TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Should().BeTrue();
        result.Data.Count.Should().Be(1);
        result.Data.Links.Should().ContainSingle(link => link.Action == "PUT" && link.Href.Contains(created.Response.Id.ToString()));
    }

    [Fact]
    public async Task UpdateApiKey_DocSpaceAdmin_UpdatesOwnersKey()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _peopleClient.Authenticate(Owner);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Owner Key"), TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(admin);

        // Act
        var result = await _apiKeysApi.UpdateApiKeyWithHttpInfoAsync(
            created.Response.Id,
            new UpdateApiKeyRequest("Autotest Renamed Key", ["files:read", "rooms:read"]),
            TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Should().BeTrue();
        result.Data.Count.Should().Be(1);
        result.Data.Links.Should().ContainSingle(link => link.Action == "PUT" && link.Href.Contains(created.Response.Id.ToString()));
    }

    [Fact]
    public async Task UpdateApiKey_Owner_DeactivatesOwnKey()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Deactivate Key"), TestContext.Current.CancellationToken);

        // Act
        var result = await _apiKeysApi.UpdateApiKeyWithHttpInfoAsync(
            created.Response.Id, new UpdateApiKeyRequest(isActive: false), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Should().BeTrue();

        var keys = await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken);
        keys.Response.Should().ContainSingle(k => k.Id == created.Response.Id && !k.IsActive);
    }

    [Fact]
    public async Task UpdateApiKey_Owner_ActivatesDeactivatedKey()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Activate Key"), TestContext.Current.CancellationToken);

        await _apiKeysApi.UpdateApiKeyAsync(
            created.Response.Id, new UpdateApiKeyRequest(isActive: false), TestContext.Current.CancellationToken);

        // Act
        var result = await _apiKeysApi.UpdateApiKeyWithHttpInfoAsync(
            created.Response.Id, new UpdateApiKeyRequest(isActive: true), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Should().BeTrue();

        var keys = await _apiKeysApi.GetApiKeysAsync(TestContext.Current.CancellationToken);
        keys.Response.Should().ContainSingle(k => k.Id == created.Response.Id && k.IsActive);
    }
}
