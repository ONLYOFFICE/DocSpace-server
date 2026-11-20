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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Common;

/// <summary>
/// The rules of the portal colour themes: six built-in themes (ids 1-6), the lowest selected by default, a limit of
/// <c>core:themelimit</c> (9 in the shared configuration) that counts the built-ins, and how save and delete treat
/// built-in, custom and unknown ids. Themes are per portal, so every test works on its own portal.
/// </summary>
[Trait("Category", "Settings")]
public class ColorThemeRulesTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private static readonly int[] _builtInIds = [1, 2, 3, 4, 5, 6];

    [Fact]
    public async Task GetPortalColorTheme_AnonymousOnFreshPortal_ReturnsSixBuiltInsWithLowestSelected()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var settings = (await _commonSettingsApi.GetPortalColorThemeAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.Themes.Select(theme => theme.Id).Should().BeEquivalentTo(_builtInIds);
        settings.Selected.Should().Be(1);
        settings.Limit.Should().Be(9);
    }

    [Fact]
    public async Task SavePortalColorTheme_BuiltInId_AddsNewUnnamedThemeWithNextId()
    {
        // Act
        var settings = await SaveTheme(new CustomColorThemeRequestDto(id: 3, name: "custom", main: Colors("#111111"), text: Colors("#FFFFFF")));

        // Assert
        settings.Themes.Should().HaveCount(7);
        settings.Themes.Single(theme => theme.Id == 3).Name.Should().Be("green");
        var added = settings.Themes.Single(theme => theme.Id == 7);
        added.Name.Should().BeEmpty();
        added.Main.Accent.Should().Be("#111111");
    }

    [Fact]
    public async Task SavePortalColorTheme_ExistingCustomId_ReplacesItsColours()
    {
        // Arrange
        await SaveTheme(new CustomColorThemeRequestDto(main: Colors("#111111"), text: Colors("#FFFFFF")));

        // Act
        var settings = await SaveTheme(new CustomColorThemeRequestDto(id: 7, main: Colors("#222222"), text: Colors("#EEEEEE")));

        // Assert
        settings.Themes.Should().HaveCount(7);
        var updated = settings.Themes.Single(theme => theme.Id == 7);
        updated.Main.Accent.Should().Be("#222222");
        updated.Text.Accent.Should().Be("#EEEEEE");
    }

    [Fact]
    public async Task SavePortalColorTheme_ExistingSelected_SwitchesSelection()
    {
        // Act
        var saved = (await _commonSettingsApi.SavePortalColorThemeAsync(
            new CustomColorThemesSettingsRequestDto(selected: 4), TestContext.Current.CancellationToken)).Response;
        var read = (await _commonSettingsApi.GetPortalColorThemeAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        saved.Selected.Should().Be(4);
        read.Selected.Should().Be(4);
    }

    [Fact]
    public async Task SavePortalColorTheme_UnknownSelected_KeepsSelection()
    {
        // Act
        var saved = (await _commonSettingsApi.SavePortalColorThemeAsync(
            new CustomColorThemesSettingsRequestDto(selected: 999), TestContext.Current.CancellationToken)).Response;

        // Assert
        saved.Selected.Should().Be(1);
    }

    [Fact]
    public async Task SavePortalColorTheme_UpToLimit_AcceptsThreeCustomThemesBesideTheSixBuiltIns()
    {
        // Act
        CustomColorThemesSettingsDto settings = null!;
        for (var i = 0; i < 3; i++)
        {
            settings = await SaveTheme(new CustomColorThemeRequestDto(main: Colors($"#00000{i}"), text: Colors("#FFFFFF")));
        }

        // Assert
        settings.Themes.Select(theme => theme.Id).Should().BeEquivalentTo([1, 2, 3, 4, 5, 6, 7, 8, 9]);
    }

    /// <summary>
    /// L6: a theme beyond the limit is dropped and the call answers 200 with the unchanged list, so the client
    /// cannot tell the refusal from a success. The refusal should be an error. Red while the defect is open.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task SavePortalColorTheme_BeyondLimit_IsRefusedWithClientError()
    {
        // Arrange
        for (var i = 0; i < 3; i++)
        {
            await SaveTheme(new CustomColorThemeRequestDto(main: Colors($"#00000{i}"), text: Colors("#FFFFFF")));
        }

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SaveTheme(new CustomColorThemeRequestDto(main: Colors("#000009"), text: Colors("#FFFFFF"))));

        // Assert
        exception.ErrorCode.Should().BeInRange(400, 499);
    }

    [Fact]
    public async Task DeletePortalColorTheme_BuiltInId_LeavesThemesUnchanged()
    {
        // Act
        var settings = (await _commonSettingsApi.DeletePortalColorThemeAsync(2, TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.Themes.Select(theme => theme.Id).Should().BeEquivalentTo(_builtInIds);
    }

    [Fact]
    public async Task DeletePortalColorTheme_SelectedCustomTheme_SelectsLowestRemainingId()
    {
        // Arrange
        await SaveTheme(new CustomColorThemeRequestDto(main: Colors("#111111"), text: Colors("#FFFFFF")));
        await _commonSettingsApi.SavePortalColorThemeAsync(
            new CustomColorThemesSettingsRequestDto(selected: 7), TestContext.Current.CancellationToken);

        // Act
        var settings = (await _commonSettingsApi.DeletePortalColorThemeAsync(7, TestContext.Current.CancellationToken)).Response;

        // Assert
        settings.Themes.Select(theme => theme.Id).Should().BeEquivalentTo(_builtInIds);
        settings.Selected.Should().Be(1);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SavePortalColorTheme_MemberWithoutPortalSettingsRight_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await SaveTheme(new CustomColorThemeRequestDto(main: Colors("#111111"), text: Colors("#FFFFFF"))));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task DeletePortalColorTheme_MemberWithoutPortalSettingsRight_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        await SaveTheme(new CustomColorThemeRequestDto(main: Colors("#111111"), text: Colors("#FFFFFF")));
        await _webApiClient.Authenticate(await InviteMember(employeeType));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.DeletePortalColorThemeAsync(7, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    private async Task<CustomColorThemesSettingsDto> SaveTheme(CustomColorThemeRequestDto theme)
    {
        return (await _commonSettingsApi.SavePortalColorThemeAsync(
            new CustomColorThemesSettingsRequestDto(theme), TestContext.Current.CancellationToken)).Response;
    }

    private static ColorThemeColorsRequestDto Colors(string accent)
    {
        return new ColorThemeColorsRequestDto(accent, "#FFFFFF");
    }
}
