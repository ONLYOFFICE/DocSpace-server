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
/// GET/PUT /api/2.0/apps — the catalogue comes from the <c>apps</c> section of the installation's configuration
/// (<c>buildtools/config/appsettings.json</c>, which every service of the stand reads), and a portal stores only
/// its own overrides of it. The tests read that section themselves, because the claim is "the list is the
/// configuration": its order, its ids and its default flags.
/// </summary>
[Trait("Category", "Apps")]
public class AppsConfiguredDefaultsTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    [Fact]
    public async Task GetAll_FreshPortal_ListsTheConfiguredApplicationsInOrderWithTheirDefaultFlags()
    {
        // Arrange
        var configured = ConfiguredApps();

        // Act
        var states = await ListStatesAsync();

        // Assert
        states.Select(app => (app.Id, app.Enabled)).Should().Equal(configured);
        states.Should().OnlyContain(app => app.Settings == null);

        foreach (var (id, enabled) in configured)
        {
            (await SnapshotAsync(id)).Should().Be(new AppState(id, enabled, null));
        }
    }

    [Fact]
    public async Task SetEnabled_Overrides_ChangeTheFlagsButNotTheConfiguredOrder()
    {
        // Arrange
        var configured = ConfiguredApps();
        configured.Should().HaveCountGreaterThanOrEqualTo(2, "the configuration has to name at least two applications");
        var (lastId, lastEnabled) = configured[^1];
        var (firstId, firstEnabled) = configured[0];

        // Act
        await SaveEnabledAsync(lastId, !lastEnabled);
        await SaveEnabledAsync(firstId, !firstEnabled);

        // Assert
        var expected = configured
            .Select(app => app.Id == lastId || app.Id == firstId ? (app.Id, !app.Enabled) : app)
            .ToList();
        (await ListStatesAsync()).Select(app => (app.Id, app.Enabled)).Should().Equal(expected);
    }

    [Fact]
    public async Task SetSettings_FirstWrite_KeepsTheConfiguredFlag_AndADropKeepsAnOverriddenFlag()
    {
        // Arrange
        var (id, configuredEnabled) = ConfiguredApps()[0];
        var (otherId, otherConfiguredEnabled) = ConfiguredApps()[1];

        // Act — the first write of each application is a settings document
        await SaveSettingsAsync(id, """{"first":"write"}""");
        await SaveSettingsAsync(otherId, """{"first":"write"}""");
        await SaveEnabledAsync(otherId, !otherConfiguredEnabled);
        await SaveSettingsAsync(otherId, "null");

        // Assert — the stored row carries the configured flag; a later drop leaves the override in place
        (await SnapshotAsync(id)).Should().Be(new AppState(id, configuredEnabled, Canonical("""{"first":"write"}""")));
        (await SnapshotAsync(otherId)).Should().Be(new AppState(otherId, !otherConfiguredEnabled, null));
    }

    [Fact]
    public async Task TwoPortals_WritesInOne_DoNotReachTheOther()
    {
        // Arrange
        var (id, configuredEnabled) = ConfiguredApps()[0];
        using var other = await Fixture.CreatePortalAsync(Ct);
        await other.WebApiHttpClient.Authenticate(other.Owner);
        var otherApps = new DocSpace.API.SDK.Api.Apps.AppsApi(
            other.WebApiHttpClient, new Configuration { BasePath = other.WebApiHttpClient.BaseAddress!.ToString().TrimEnd('/') });

        // Act — this portal overrides both the flag and the document
        await SaveEnabledAsync(id, !configuredEnabled);
        await SaveSettingsAsync(id, """{"portal":"mine"}""");

        // Assert — the other portal still reads the configuration
        var seenByOther = (await otherApps.GetAsync(id, Ct)).Response;
        seenByOther.Enabled.Should().Be(configuredEnabled);
        seenByOther.Settings.Should().BeNull();
        (await otherApps.GetSettingsAsync(id, Ct)).Count.Should().Be(0);
        (await otherApps.GetAllAsync(Ct)).Response.Select(app => (app.Id, app.Enabled)).Should().Equal(ConfiguredApps());

        // Act — and a write there does not touch this portal
        await otherApps.SetEnabledAsync(id, new SetAppEnabledRequest(configuredEnabled), Ct);
        await otherApps.SetSettingsAsync(id, new SetAppSettingsRequest(new { portal = "other" }), Ct);

        // Assert
        (await SnapshotAsync(id)).Should().Be(new AppState(id, !configuredEnabled, Canonical("""{"portal":"mine"}""")));
    }

    /// <summary>The <c>apps</c> section of the configuration the stand's services read, in its order.</summary>
    private static List<(string Id, bool Enabled)> ConfiguredApps()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(ConfigPath(directory)))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the stand's buildtools/config/appsettings.json has to be found above the test binaries");

        using var document = JsonDocument.Parse(
            File.ReadAllText(ConfigPath(directory!)),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        return
        [
            .. document.RootElement.GetProperty("apps").EnumerateArray().Select(app => (
                app.GetProperty("id").GetString()!,
                app.TryGetProperty("enabled", out var enabled) && enabled.GetBoolean()))
        ];
    }

    private static string ConfigPath(DirectoryInfo directory) => Path.Combine(directory.FullName, "buildtools", "config", "appsettings.json");
}
