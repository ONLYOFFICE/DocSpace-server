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
/// PUT /api/2.0/keys/{keyId} — permission checks. Anonymous is a plain 401; RoomAdmin, User and
/// Guest updating the portal owner's own key are all "Bug 81616": the endpoint currently lets a
/// non-owner rename/reconfigure the owner's key instead of returning 403.
/// </summary>
[Trait("Category", "ApiKeys")]
public class UpdateApiKeyPermissionsTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    private async Task<Guid> CreateOwnerKeyAsync()
    {
        await _peopleClient.Authenticate(Owner);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Owner Key"), TestContext.Current.CancellationToken);

        return created.Response.Id;
    }

    [Fact]
    public async Task UpdateApiKey_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        var keyId = await CreateOwnerKeyAsync();
        await _peopleClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.UpdateApiKeyAsync(
                keyId, new UpdateApiKeyRequest("Autotest Renamed"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Trait("Bug", "81616")]
    [Theory]
    [MemberData(nameof(ApiKeyActorData.NonOwnerRoles), MemberType = typeof(ApiKeyActorData))]
    public async Task UpdateApiKey_NonOwnerRole_CannotUpdateOwnersKey(ApiKeyActor actor)
    {
        // Arrange
        var keyId = await CreateOwnerKeyAsync();
        await AuthenticateAsAsync(actor);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.UpdateApiKeyAsync(
                keyId, new UpdateApiKeyRequest("Autotest Renamed"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
