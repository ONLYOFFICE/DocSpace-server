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
/// GET /api/2.0/apps — the applications configured on the installation, each with the enabled flag and
/// the settings of the current portal.
/// </summary>
[Trait("Category", "Apps")]
public class AppsGetAllTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    [Fact]
    public async Task GetAll_Owner_ReturnsApplications()
    {
        // Act
        var result = await AppsApi.GetAllWithHttpInfoAsync(Ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Should().NotBeNull();
        result.Data.Count.Should().Be(result.Data.Response.Count);
        result.Data.Response.Should().OnlyContain(app => !string.IsNullOrEmpty(app.Id));
        result.Data.Response.Select(app => app.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetAll_PortalThatNeverSavedSettings_ReportsNone()
    {
        // Act
        var result = await AppsApi.GetAllAsync(Ct);

        // Assert
        result.Response.Should().OnlyContain(app => app.Settings == null);
    }

    [Fact]
    public async Task GetAll_RepeatedCalls_ReturnSameListAndChangeNothing()
    {
        // Act
        var first = await ListStatesAsync();
        var second = await ListStatesAsync();
        var third = await ListStatesAsync();

        // Assert
        second.Should().Equal(first);
        third.Should().Equal(first);
    }

    [Fact]
    public async Task GetAll_EveryListedApplication_MatchesGetAndGetSettings()
    {
        // Arrange
        var listed = await ListStatesAsync();

        foreach (var state in listed)
        {
            // Act
            var typed = await AppsApi.GetWithHttpInfoAsync(state.Id, Ct);

            // Assert
            typed.StatusCode.Should().Be(HttpStatusCode.OK);
            typed.Data.Response.Id.Should().Be(state.Id);
            typed.Data.Response.Enabled.Should().Be(state.Enabled);
            (await SnapshotAsync(state.Id)).Should().Be(state);
            (await ReadSettingsAsync(state.Id)).Should().Be(state.Settings);
        }
    }

    [Fact]
    public async Task GetAll_UnknownQueryParameters_AreIgnored()
    {
        // Act
        var plain = await SendAsync(HttpMethod.Get, "api/2.0/apps");
        var noisy = await SendAsync(HttpMethod.Get, "api/2.0/apps?foo=bar&x[]=1&count=-1&startIndex=%FF");

        // Assert
        plain.Status.Should().Be(HttpStatusCode.OK);
        noisy.Status.Should().Be(HttpStatusCode.OK);
        Canonical(ResponseOf(noisy.Body)!.Value).Should().Be(Canonical(ResponseOf(plain.Body)!.Value));
    }

    [Fact]
    public async Task GetAll_AfterChange_ShowsEnabledAndSettings()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        var before = await SnapshotAsync(id);
        var countBefore = (await ListStatesAsync()).Count;

        // Act
        await SaveEnabledAsync(id, !before.Enabled);
        await SaveSettingsAsync(id, """{"marker":"list-check"}""");

        // Assert
        var apps = await ListStatesAsync();
        apps.Should().HaveCount(countBefore);
        apps.Single(app => app.Id == id).Should().Be(new AppState(id, !before.Enabled, Canonical("""{"marker":"list-check"}""")));
    }
}
