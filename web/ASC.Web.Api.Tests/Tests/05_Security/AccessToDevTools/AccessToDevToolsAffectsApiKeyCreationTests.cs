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

namespace ASC.Web.Api.Tests.Tests._05_Security.AccessToDevTools;

/// <summary>
/// Effect of <c>POST /api/2.0/settings/devtoolsaccess</c> on <c>POST /api/2.0/keys</c>
/// (<c>ApiKeysController.CreateApiKey</c>): when <c>limitedAccessForUsers</c> is enabled, the
/// owner and DocSpaceAdmin can still create API keys, and every other role is expected to be
/// rejected with "This operation available only for portal owner/admins" — restored once the
/// restriction is disabled again.
///
/// <c>ApiKeysController</c> lives under ASC.People (<c>api/2.0/keys</c>), so this suite builds its
/// own <c>ApiKeysApi</c> off the already-authenticated <c>_peopleClient</c>, the same way
/// <c>AccessToDevToolsGetTests</c> builds its own Security client off <c>_webApiClient</c> — there
/// is no ApiKeys client wired onto <see cref="BaseTest"/> for this project.
/// </summary>
[Trait("Category", "Security")]
public class AccessToDevToolsAffectsApiKeyCreationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private DocSpace.API.SDK.Api.ApiKeys.ApiKeysApi CreateApiKeysApi()
    {
        return new(_peopleClient, new Configuration { BasePath = _peopleClient.BaseAddress!.ToString().TrimEnd('/') });
    }

    private async Task SetLimitedAccessForUsersAsync(bool limitedAccessForUsers)
    {
        await _webApiClient.Authenticate(Owner);
        await _securityAccessToDevToolsApi.SetTenantDevToolsAccessSettingsAsync(
            new TenantDevToolsAccessSettingsDto(limitedAccessForUsers), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateApiKey_Owner_AllowedWhenLimitedAccessEnabled()
    {
        // Arrange
        await SetLimitedAccessForUsersAsync(true);
        await _peopleClient.Authenticate(Owner);
        var apiKeysApi = CreateApiKeysApi();

        // Act
        var result = await apiKeysApi.CreateApiKeyWithHttpInfoAsync(
            new CreateApiKeyRequestDto("Autotest Owner DevTools Key"), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateApiKey_DocSpaceAdmin_AllowedWhenLimitedAccessEnabled()
    {
        // Arrange
        await SetLimitedAccessForUsersAsync(true);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);
        var apiKeysApi = CreateApiKeysApi();

        // Act
        var result = await apiKeysApi.CreateApiKeyWithHttpInfoAsync(
            new CreateApiKeyRequestDto("Autotest Admin DevTools Key"), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Trait("Bug", "81236")]
    [Fact]
    public async Task CreateApiKey_RoomAdmin_ForbiddenWhenLimitedAccessEnabled()
    {
        // Arrange
        await SetLimitedAccessForUsersAsync(true);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _peopleClient.Authenticate(roomAdmin);
        var apiKeysApi = CreateApiKeysApi();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await apiKeysApi.CreateApiKeyAsync(
                new CreateApiKeyRequestDto("Autotest Room DevTools Key"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        // The denial itself is what bug 81236 asked for and it works — the server just words it
        // as the generic "Access denied", not the TS suite's expected admin-only message.
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Trait("Bug", "81236")]
    [Fact]
    public async Task CreateApiKey_User_ForbiddenWhenLimitedAccessEnabled()
    {
        // Arrange
        await SetLimitedAccessForUsersAsync(true);
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);
        var apiKeysApi = CreateApiKeysApi();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await apiKeysApi.CreateApiKeyAsync(
                new CreateApiKeyRequestDto("Autotest User DevTools Key"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        // The denial itself is what bug 81236 asked for and it works — the server just words it
        // as the generic "Access denied", not the TS suite's expected admin-only message.
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task CreateApiKey_RoomAdmin_AllowedAfterLimitedAccessDisabled()
    {
        // Arrange
        await SetLimitedAccessForUsersAsync(true);
        await SetLimitedAccessForUsersAsync(false);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _peopleClient.Authenticate(roomAdmin);
        var apiKeysApi = CreateApiKeysApi();

        // Act
        var result = await apiKeysApi.CreateApiKeyWithHttpInfoAsync(
            new CreateApiKeyRequestDto("Autotest Room Restored Key"), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateApiKey_User_AllowedAfterLimitedAccessDisabled()
    {
        // Arrange
        await SetLimitedAccessForUsersAsync(true);
        await SetLimitedAccessForUsersAsync(false);
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);
        var apiKeysApi = CreateApiKeysApi();

        // Act
        var result = await apiKeysApi.CreateApiKeyWithHttpInfoAsync(
            new CreateApiKeyRequestDto("Autotest User Restored Key"), TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
