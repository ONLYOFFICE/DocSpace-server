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


namespace ASC.Web.Api.Tests.Tests._09_Apps;

/// <summary>
/// /api/2.0/apps without credentials — every method is refused with 401 and a write changes nothing.
/// </summary>
[Trait("Category", "Apps")]
public class AppsAnonymousAccessTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    [Fact]
    public async Task GetAll_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await AppsApi.GetAllAsync(Ct));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task Get_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await AppsApi.GetAsync(id, Ct));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetSettings_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await AppsApi.GetSettingsAsync(id, Ct));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SetEnabled_Anonymous_ReturnsUnauthorizedAndChangesNothing()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        var before = await SnapshotAsync(id);
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetEnabledAsync(id, new SetAppEnabledRequest(!before.Enabled), Ct));

        // Assert
        exception.ErrorCode.Should().Be(401);
        await _webApiClient.Authenticate(Owner);
        (await SnapshotAsync(id)).Should().Be(before);
    }

    [Fact]
    public async Task SetSettings_Anonymous_ReturnsUnauthorizedAndChangesNothing()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, """{"survives":true}""");
        var before = await SnapshotAsync(id);
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetSettingsAsync(id, new SetAppSettingsRequest(new { survives = false }), Ct));

        // Assert
        exception.ErrorCode.Should().Be(401);
        await _webApiClient.Authenticate(Owner);
        (await SnapshotAsync(id)).Should().Be(before);
    }

    // Raw HTTP: the same refusal for a body the typed client could also send, kept as the transport-level control.
    [Fact]
    public async Task SetSettings_AnonymousRawRequest_ReturnsUnauthorized()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await _webApiClient.Authenticate(null);

        // Act
        var (status, _) = await SendAsync(HttpMethod.Put, $"{AppPath(id)}/settings", """{"settings":{"a":1}}""");

        // Assert
        status.Should().Be(HttpStatusCode.Unauthorized);
    }
}
