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
/// End-to-end scenarios that authenticate <em>with the API key itself</em>
/// (<c>Authorization: Bearer &lt;key&gt;</c>) and then call another endpoint through it, ported
/// from <c>apiKeys.scenarios.spec.ts</c>.
///
/// The TS suite's fourth scenario ("API key with contacts:read scope cannot access rooms") is not
/// ported: it creates a room and a file to exercise the check, and this project has no Files/Rooms
/// client wired into its Aspire resources (only People and Web.Api are started — see
/// <c>RoomsNotificationStatusTests</c> for the same constraint on this project). Porting it would
/// need a Files/Rooms-capable suite (<c>products/ASC.Files/Tests</c>), not this one.
/// </summary>
[Trait("Category", "ApiKeys")]
public class ApiKeyAuthScenariosTests(
    AspireAppFixture fixture)
    : ApiKeysTestBase(fixture)
{
    [Fact]
    public async Task UserApiKey_WithAccountsPermissions_CannotAccessGetAllProfiles()
    {
        // Arrange
        await AuthenticateAsAsync(ApiKeyActor.User);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest User Accounts Key", ["accounts:write", "accounts:read"]),
            TestContext.Current.CancellationToken);

        // Act
        var exception = await AsApiKeyAsync(created.Response.Key, () => Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.GetAllProfilesAsync(cancellationToken: TestContext.Current.CancellationToken)));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Trait("Bug", "81238")]
    [Fact]
    public async Task ApiKeyWithFilesReadScope_CannotCreateAnotherApiKey()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("read-only key", ["files:read"]), TestContext.Current.CancellationToken);

        // Act
        var exception = await AsApiKeyAsync(created.Response.Key, () => Assert.ThrowsAsync<ApiException>(
            async () => await _apiKeysApi.CreateApiKeyAsync(
                new CreateApiKeyRequestDto("full access key"), TestContext.Current.CancellationToken)));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Trait("Bug", "74914")]
    [Fact]
    public async Task ApiKeyWithScopedPermissions_CanGetOwnKeyInfo()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var created = await _apiKeysApi.CreateApiKeyAsync(
            new CreateApiKeyRequestDto("Autotest Scoped Key", ["files:read", "accounts:read"]),
            TestContext.Current.CancellationToken);

        // Act
        var result = await AsApiKeyAsync(created.Response.Key, () =>
            _apiKeysApi.GetApiKeyWithHttpInfoAsync(TestContext.Current.CancellationToken));

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Id.Should().Be(created.Response.Id);
        result.Data.Response.Permissions.Should().Contain(["files:read", "accounts:read"]);
    }
}
