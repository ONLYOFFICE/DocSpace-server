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
/// /api/2.0/apps scenarios that span several endpoints.
/// </summary>
[Trait("Category", "Apps")]
public class AppsScenariosTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    [Fact]
    public async Task Lifecycle_SettingsThenDisableThenEnableThenRestore()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        const string document = """{"lifecycle":true,"steps":[1,2,3]}""";
        var original = await SnapshotAsync(id);

        // Save a new document
        await SaveSettingsAsync(id, document);
        (await ReadSettingsAsync(id)).Should().Be(Canonical(document));

        // Disable: flag flips, document stays
        await SaveEnabledAsync(id, false);
        (await SnapshotAsync(id)).Should().Be(new AppState(id, false, Canonical(document)));

        // Enable: flag flips back, document stays
        await SaveEnabledAsync(id, true);
        (await SnapshotAsync(id)).Should().Be(new AppState(id, true, Canonical(document)));

        // Restore the original state and confirm it
        await SaveEnabledAsync(id, original.Enabled);
        await SaveSettingsAsync(id, "null");
        (await SnapshotAsync(id)).Should().Be(original);
    }

    [Fact]
    public async Task UnknownApplication_IsRejectedEverywhereAndNeverAppears()
    {
        // Arrange
        var id = $"missing-{Guid.NewGuid():N}";
        var before = await ListStatesAsync();
        before.Select(app => app.Id).Should().NotContain(id);

        // Act
        var get = await Assert.ThrowsAsync<ApiException>(async () => await AppsApi.GetAsync(id, Ct));
        var getSettings = await Assert.ThrowsAsync<ApiException>(async () => await AppsApi.GetSettingsAsync(id, Ct));
        var setEnabled = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetEnabledAsync(id, new SetAppEnabledRequest(true), Ct));
        var setSettings = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetSettingsAsync(id, new SetAppSettingsRequest(new { a = 1 }), Ct));

        // Assert
        new[] { get.ErrorCode, getSettings.ErrorCode, setEnabled.ErrorCode, setSettings.ErrorCode }.Should().OnlyContain(code => code == 404);
        (await ListStatesAsync()).Should().Equal(before);
    }

    [Fact]
    public async Task GetGetSettingsAndGetAll_StayConsistentAfterEveryChange()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];

        async Task AssertConsistentAsync(string label)
        {
            var fromGet = await SnapshotAsync(id);
            var fromList = (await ListStatesAsync()).Single(app => app.Id == id);
            var fromSettings = await ReadSettingsAsync(id);

            fromList.Should().Be(fromGet, $"{label}: get vs getAll");
            fromSettings.Should().Be(fromGet.Settings, $"{label}: getSettings vs get");
        }

        // Act & Assert
        await AssertConsistentAsync("initial");

        await SaveEnabledAsync(id, !(await SnapshotAsync(id)).Enabled);
        await AssertConsistentAsync("after setEnabled");

        await SaveSettingsAsync(id, """{"consistent":[1,2]}""");
        await AssertConsistentAsync("after setSettings");

        await SaveSettingsAsync(id, "null");
        await AssertConsistentAsync("after reset");
    }

    [Fact]
    public async Task EveryModule_IsReachableAndCanBeSwitchedAndConfigured()
    {
        // Arrange
        var apps = await ListStatesAsync();
        apps.Should().NotBeEmpty("the installation has to expose applications");

        foreach (var app in apps)
        {
            // Act
            var typed = await AppsApi.GetWithHttpInfoAsync(app.Id, Ct);
            const string probe = """{"probe":true}""";
            await SaveSettingsAsync(app.Id, probe);
            await SaveEnabledAsync(app.Id, !app.Enabled);
            var switched = await SnapshotAsync(app.Id);
            await SaveEnabledAsync(app.Id, app.Enabled);
            var restored = await SnapshotAsync(app.Id);

            // Assert
            typed.Data.Response.Id.Should().Be(app.Id);
            typed.Data.Response.Enabled.Should().Be(app.Enabled);
            switched.Enabled.Should().Be(!app.Enabled, app.Id);
            restored.Should().Be(new AppState(app.Id, app.Enabled, Canonical(probe)), app.Id);
        }
    }
}
