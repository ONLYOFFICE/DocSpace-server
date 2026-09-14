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

namespace ASC.People.Tests.Tests._09_Theme;

/// <summary>
/// <c>GET/PUT /people/theme</c>: the portal theme is a portal-wide setting, so every role can both
/// read and change it — there is no per-role restriction on this endpoint.
/// </summary>
public class ThemeTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetPortalTheme_Owner_ReturnsSystem()
    {
        var result = await _themeApi.GetPortalThemeAsync(TestContext.Current.CancellationToken);

        result.Response.Theme.Should().Be(DarkThemeSettingsType.System);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetPortalTheme_Member_ReturnsSystem(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await _peopleClient.Authenticate(member);

        var result = await _themeApi.GetPortalThemeAsync(TestContext.Current.CancellationToken);

        result.Response.Theme.Should().Be(DarkThemeSettingsType.System);
    }

    [Fact]
    public async Task ChangePortalTheme_Owner_CyclesThroughBaseDarkAndSystem()
    {
        await AssertCyclesThroughAllThemesAsync();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task ChangePortalTheme_Member_CyclesThroughBaseDarkAndSystem(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await _peopleClient.Authenticate(member);

        await AssertCyclesThroughAllThemesAsync();
    }

    /// <summary>
    /// Changes the portal theme Base -> Dark -> System as the currently-authenticated actor,
    /// asserting each transition took, which is what the TS suite's three <c>test.step</c> calls
    /// check in sequence.
    /// </summary>
    private async Task AssertCyclesThroughAllThemesAsync()
    {
        foreach (var theme in new[] { DarkThemeSettingsType.Base, DarkThemeSettingsType.Dark, DarkThemeSettingsType.System })
        {
            var result = await _themeApi.ChangePortalThemeAsync(
                new DarkThemeSettingsRequestDto(theme), TestContext.Current.CancellationToken);

            result.Response.Theme.Should().Be(theme);
        }
    }
}
