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
/// GET /api/2.0/apps/{id}/settings — the settings document saved for an application, or an empty result
/// when the portal never saved one.
/// </summary>
[Trait("Category", "Apps")]
public class AppSettingsGetTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    [Fact]
    public async Task GetSettings_NeverSaved_ComesBackEmpty()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];

        // Act
        var result = await AppsApi.GetSettingsWithHttpInfoAsync(id, Ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Should().BeNull();
        result.Data.Count.Should().Be(0);
    }

    // A falsy document is stored and returned as it is, never treated as "nothing saved".
    [Theory]
    [InlineData("0")]
    [InlineData("false")]
    [InlineData("\"\"")]
    [InlineData("[]")]
    [InlineData("{}")]
    public async Task GetSettings_FalsyDocument_IsKeptNotTreatedAsEmpty(string document)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, document);

        // Act
        var (status, body) = await SendAsync(HttpMethod.Get, $"{AppPath(id)}/settings");

        // Assert
        status.Should().Be(HttpStatusCode.OK, body);
        Canonical(ResponseOf(body)!.Value).Should().Be(Canonical(document));
        using var envelope = JsonDocument.Parse(body);
        envelope.RootElement.GetProperty("count").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetSettings_WhileApplicationIsDisabled_StillReadable()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, """{"still":"readable"}""");
        await SaveEnabledAsync(id, false);

        // Act
        var settings = await ReadSettingsAsync(id);

        // Assert
        settings.Should().Be(Canonical("""{"still":"readable"}"""));
        (await SnapshotAsync(id)).Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetSettings_UnknownId_ReturnsNotFound()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.GetSettingsAsync("no-such-app", Ct));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("приложение-😀")]
    [InlineData("a<b>&c'd\"e")]
    public async Task GetSettings_IdThatCannotBeAnApplication_ReturnsNotFound(string id)
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.GetSettingsAsync(id, Ct));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }
}
