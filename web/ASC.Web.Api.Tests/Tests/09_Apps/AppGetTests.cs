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
/// GET /api/2.0/apps/{id} — one application by its identifier.
/// </summary>
[Trait("Category", "Apps")]
public class AppGetTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    [Fact]
    public async Task Get_Owner_ReturnsApplicationById()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        var listed = (await ListStatesAsync()).Single(app => app.Id == id);

        // Act
        var result = await AppsApi.GetWithHttpInfoAsync(id, Ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Count.Should().Be(1);
        result.Data.Response.Id.Should().Be(id);
        (await SnapshotAsync(id)).Should().Be(listed);
    }

    [Fact]
    public async Task Get_Reading_DoesNotChangeEnabledOrSettings()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, """{"keep":"me"}""");
        var before = await SnapshotAsync(id);

        // Act
        await AppsApi.GetAsync(id, Ct);
        await AppsApi.GetAsync(id, Ct);
        await AppsApi.GetSettingsAsync(id, Ct);
        await AppsApi.GetAllAsync(Ct);

        // Assert
        (await SnapshotAsync(id)).Should().Be(before);
    }

    [Fact]
    public async Task Get_ReturnsEnabledAndDisabledApplication()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];

        // Act
        await SaveEnabledAsync(id, true);
        var enabled = await AppsApi.GetAsync(id, Ct);
        await SaveEnabledAsync(id, false);
        var disabled = await AppsApi.GetAsync(id, Ct);

        // Assert
        enabled.Response.Enabled.Should().BeTrue();
        disabled.Response.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task Get_ReturnsSavedSettingsDocument()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        const string document = """{"theme":"dark","limits":[1,2]}""";
        await SaveSettingsAsync(id, document);

        // Act
        var state = await SnapshotAsync(id);

        // Assert
        state.Settings.Should().Be(Canonical(document));
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.GetAsync("no-such-app", Ct));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("no-such-app");
    }

    // Every one of these is addressed as a literal application key, so none may resolve to a real
    // application or fail with a server error. The typed client escapes the path segment with
    // Uri.EscapeDataString, so "a/b" and "../settings" reach the server as one encoded segment.
    [Theory]
    [InlineData("   ")]
    [InlineData("приложение-😀")]
    [InlineData("a<b>&c'd\"e")]
    [InlineData("a/b")]
    [InlineData("../settings")]
    public async Task Get_IdThatCannotBeAnApplication_ReturnsNotFound(string id)
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.GetAsync(id, Ct));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Get_VeryLongId_ReturnsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.GetAsync(new string('x', 2000), Ct));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Get_IdIsCaseSensitive()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        var upper = id.ToUpperInvariant();

        if (upper == id)
        {
            // The id has no lower-case letters to change, so there is no other-case spelling to try.
            return;
        }

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.GetAsync(upper, Ct));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Get_EmptyId_IsRoutedToTheListNotToAnApplication()
    {
        // Arrange
        var list = await SendAsync(HttpMethod.Get, "api/2.0/apps");

        // Act
        var (status, body) = await SendAsync(HttpMethod.Get, "api/2.0/apps/");

        // Assert
        status.Should().Be(HttpStatusCode.OK, body);
        ResponseOf(body)!.Value.ValueKind.Should().Be(JsonValueKind.Array);
        Canonical(ResponseOf(body)!.Value).Should().Be(Canonical(ResponseOf(list.Body)!.Value));
    }
}
