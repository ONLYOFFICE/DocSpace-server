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
/// POST /api/2.0/keys — every role that can create its own API key, with or without an expiration
/// and with or without a permission scope. Owner/DocSpaceAdmin/RoomAdmin request the full
/// permission catalog in the "all scopes" cases; User requests a reduced one with no
/// <c>accounts:*</c> scope, matching the TS suite.
/// </summary>
[Trait("Category", "ApiKeys")]
public class CreateApiKeyTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    public static TheoryData<ApiKeyActor, List<string>?, int?> Cases()
    {
        var data = new TheoryData<ApiKeyActor, List<string>?, int?>();

        foreach (var actor in new[] { ApiKeyActor.Owner, ApiKeyActor.DocSpaceAdmin, ApiKeyActor.RoomAdmin })
        {
            data.Add(actor, null, null);
            data.Add(actor, null, 7);
            data.Add(actor, ["*:read"], null);
            data.Add(actor, ["*:read"], 7);
            data.Add(actor, ["files:read", "rooms:read", "accounts.self:read", "accounts:read"], null);
            data.Add(actor, ["files:write", "files:read", "rooms:write", "rooms:read", "accounts.self:write", "accounts.self:read", "accounts:write", "accounts:read"], null);
        }

        data.Add(ApiKeyActor.User, null, null);
        data.Add(ApiKeyActor.User, null, 7);
        data.Add(ApiKeyActor.User, ["*:read"], null);
        data.Add(ApiKeyActor.User, ["*:read"], 7);
        data.Add(ApiKeyActor.User, ["files:read", "rooms:read", "accounts.self:read"], null);
        data.Add(ApiKeyActor.User, ["files:write", "files:read", "rooms:write", "rooms:read", "accounts.self:write", "accounts.self:read"], null);

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task CreateApiKey_ByRole_ReturnsCreatedKey(ApiKeyActor actor, List<string>? permissions, int? expiresInDays)
    {
        // Arrange
        await AuthenticateAsAsync(actor);
        var profile = await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken);
        var keyName = $"Autotest {Guid.NewGuid():N}"[..20];

        // Act
        var result = await _apiKeysApi.CreateApiKeyWithHttpInfoAsync(
            new CreateApiKeyRequestDto(keyName, permissions!, expiresInDays), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        var key = result.Data.Response;
        key.Id.Should().NotBeEmpty();
        key.Name.Should().Be(keyName);
        key.Key.Should().NotBeNullOrEmpty();
        key.Permissions.Should().Equal(permissions ?? []);
        key.CreateBy.Id.Should().Be(profile.Response.Id);
        key.CreateBy.DisplayName.Should().Be(profile.Response.DisplayName);

        if (expiresInDays.HasValue)
        {
            key.ExpiresAt.Should().NotBeNull();
        }
        else
        {
            key.ExpiresAt.Should().BeNull();
        }
    }
}
