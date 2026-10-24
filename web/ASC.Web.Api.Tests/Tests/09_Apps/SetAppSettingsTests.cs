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
/// PUT /api/2.0/apps/{id}/settings — saving the settings document of an application. A document is
/// replaced as a whole, never merged; <c>null</c> drops it.
/// </summary>
[Trait("Category", "Apps")]
public class SetAppSettingsTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    [Fact]
    public async Task SetSettings_Owner_ResponseCarriesTheStoredDocumentAndEveryReaderAgrees()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        const string document = """{"a":1,"list":[{"x":1}]}""";

        // Act
        var result = await AppsApi.SetSettingsWithHttpInfoAsync(id, new SetAppSettingsRequest(new { a = 1, list = new[] { new { x = 1 } } }), Ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Response.Id.Should().Be(id);
        result.Data.Response.Settings.Should().NotBeNull();

        var expected = Canonical(document);
        (await ReadSettingsAsync(id)).Should().Be(expected);
        (await SnapshotAsync(id)).Settings.Should().Be(expected);
        (await ListStatesAsync()).Single(app => app.Id == id).Settings.Should().Be(expected);
    }

    [Fact]
    public async Task SetSettings_SameDocumentTwice_IsIdempotent()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        const string document = """{"same":"document"}""";
        await SaveSettingsAsync(id, document);
        var first = await SnapshotAsync(id);

        // Act
        var result = await AppsApi.SetSettingsWithHttpInfoAsync(id, new SetAppSettingsRequest(new { same = "document" }), Ct);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SnapshotAsync(id)).Should().Be(first);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetSettings_Saving_DoesNotChangeEnabled(bool enabled)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveEnabledAsync(id, enabled);

        // Act
        var result = await AppsApi.SetSettingsAsync(
            id, new SetAppSettingsRequest(new { enabledWas = enabled }), Ct);

        // Assert
        result.Response.Enabled.Should().Be(enabled);
        (await SnapshotAsync(id)).Enabled.Should().Be(enabled);
    }

    [Fact]
    public async Task SetSettings_SavedWhileDisabled_ApplyAfterEnabling()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveEnabledAsync(id, false);
        await SaveSettingsAsync(id, """{"saved":"while disabled"}""");

        // Act
        await SaveEnabledAsync(id, true);

        // Assert
        (await ReadSettingsAsync(id)).Should().Be(Canonical("""{"saved":"while disabled"}"""));
    }

    [Fact]
    public async Task SetSettings_OneApplication_LeavesAnothersAlone()
    {
        // Arrange
        var ids = await PickAppsAsync(2);
        await SaveSettingsAsync(ids[1], """{"owner":"second"}""");
        var secondBefore = await SnapshotAsync(ids[1]);

        // Act
        await SaveSettingsAsync(ids[0], """{"owner":"first"}""");

        // Assert
        (await SnapshotAsync(ids[1])).Should().Be(secondBefore);
        (await ReadSettingsAsync(ids[0])).Should().Be(Canonical("""{"owner":"first"}"""));
    }

    [Fact]
    public async Task SetSettings_DeeplyNestedDocument_IsStoredWhole()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        var deep = """{"leaf":"bottom"}""";

        for (var level = 0; level < 20; level++)
        {
            deep = $"{{\"level\":{level},\"child\":{deep}}}";
        }

        // Act
        await SaveSettingsAsync(id, deep);

        // Assert
        (await ReadSettingsAsync(id)).Should().Be(Canonical(deep));
    }

    [Theory]
    [InlineData("""{"a":1,"b":2}""", """{"a":3}""")]
    [InlineData("""{"a":1,"b":2,"nested":{"x":1,"y":2}}""", """{"nested":{"z":9}}""")]
    [InlineData("""{"a":1}""", "[1,2,3]")]
    [InlineData("[1,2,3]", """{"b":2}""")]
    [InlineData("""{"a":1}""", "{}")]
    public async Task SetSettings_NewDocument_ReplacesTheOldOneNeverMerges(string first, string second)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, first);

        // Act
        await SaveSettingsAsync(id, second);

        // Assert
        (await ReadSettingsAsync(id)).Should().Be(Canonical(second));
    }

    [Fact]
    public async Task SetSettings_Null_DropsTheDocumentAndIsRepeatable()
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        await SaveSettingsAsync(id, """{"to":"drop"}""");
        var enabled = (await SnapshotAsync(id)).Enabled;

        foreach (var attempt in new[] { 1, 2 })
        {
            // Act
            var result = await AppsApi.SetSettingsWithHttpInfoAsync(id, new SetAppSettingsRequest(), Ct);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK, $"reset #{attempt}");
            result.Data.Response.Id.Should().Be(id);
            result.Data.Response.Settings.Should().BeNull();
            (await ReadSettingsAsync(id)).Should().BeNull();
            (await AppsApi.GetSettingsAsync(id, Ct)).Count.Should().Be(0);

            var state = await SnapshotAsync(id);
            state.Settings.Should().BeNull();
            state.Enabled.Should().Be(enabled);
            (await ListStatesAsync()).Single(app => app.Id == id).Settings.Should().BeNull();
        }

        await SaveSettingsAsync(id, """{"after":"reset"}""");
        (await ReadSettingsAsync(id)).Should().Be(Canonical("""{"after":"reset"}"""));
    }

    [Fact]
    public async Task SetSettings_UnknownId_ReturnsNotFoundAndCreatesNothing()
    {
        // Arrange
        var before = await ListStatesAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AppsApi.SetSettingsAsync("no-such-app", new SetAppSettingsRequest(new { a = 1 }), Ct));

        // Assert
        exception.ErrorCode.Should().Be(404);
        (await ListStatesAsync()).Should().Equal(before);
    }
}
