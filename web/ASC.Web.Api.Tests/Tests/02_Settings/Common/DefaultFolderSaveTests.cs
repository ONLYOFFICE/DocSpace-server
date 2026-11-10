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
namespace ASC.Web.Api.Tests.Tests._02_Settings.Common;

/// <summary>
/// PUT /api/2.0/settings/defaultfolder — the start page is a per-account preference, so the call changes
/// only the caller's own setting and the portal settings read it back for that caller.
/// </summary>
[Trait("Category", "Settings")]
public class DefaultFolderSaveTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SaveDefaultFolder_Owner_SavesAndPortalSettingsReportIt()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var saved = await _commonSettingsApi.SaveDefaultFolderAsync(
            new DefaultProductRequestDto(FolderType.VirtualRooms), TestContext.Current.CancellationToken);
        var settings = await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var switched = await _commonSettingsApi.SaveDefaultFolderAsync(
            new DefaultProductRequestDto(FolderType.Favorites), TestContext.Current.CancellationToken);
        var settingsAgain = await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
        saved.Response.DefaultFolderType.Should().Be(FolderType.VirtualRooms);
        settings.Response.DefaultFolderType.Should().Be(FolderType.VirtualRooms);

        switched.Response.DefaultFolderType.Should().Be(FolderType.Favorites);
        settingsAgain.Response.DefaultFolderType.Should().Be(FolderType.Favorites);
    }

    [Fact]
    public async Task SaveDefaultFolder_User_DoesNotMoveOwnersLandingPage()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.SaveDefaultFolderAsync(
            new DefaultProductRequestDto(FolderType.VirtualRooms), TestContext.Current.CancellationToken);

        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        // Act
        var saved = await _commonSettingsApi.SaveDefaultFolderAsync(
            new DefaultProductRequestDto(FolderType.USER), TestContext.Current.CancellationToken);
        var userSettings = await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(Owner);
        var ownerSettings = await _commonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        saved.StatusCode.Should().Be(200);
        saved.Response.DefaultFolderType.Should().Be(FolderType.USER);
        userSettings.Response.DefaultFolderType.Should().Be(FolderType.USER);
        ownerSettings.Response.DefaultFolderType.Should().Be(FolderType.VirtualRooms);
    }
}
