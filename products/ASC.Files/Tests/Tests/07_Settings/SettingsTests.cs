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

namespace ASC.Files.Tests.Tests._07_Settings;

[Trait("Category", "Settings")]
public class SettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task ChangeAccessToThirdparty_ShouldChangeThirdPartySettings()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);
        var initialState = true;

        // Act
        var response = await _filesSettingsApi.ChangeAccessToThirdpartyAsync(new SettingsRequestDto { Set = initialState }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(initialState);

        // Change setting
        var newState = false;
        response = await _filesSettingsApi.ChangeAccessToThirdpartyAsync(new SettingsRequestDto { Set = newState }, TestContext.Current.CancellationToken);

        // Assert changed state
        response.Response.Should().Be(newState);
    }

    [Fact]
    public async Task ChangeDeleteConfirm_ShouldChangeDeleteConfirmSetting()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);
        var initialState = true;

        // Act
        var response = await _filesSettingsApi.ChangeDeleteConfirmAsync(new SettingsRequestDto { Set = initialState }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(initialState);

        // Change setting
        var newState = false;
        response = await _filesSettingsApi.ChangeDeleteConfirmAsync(new SettingsRequestDto { Set = newState }, TestContext.Current.CancellationToken);

        // Assert changed state
        response.Response.Should().Be(newState);
    }

    [Fact]
    public async Task HideConfirmCancelOperation_ShouldChangeConfirmCancelSetting()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);
        var initialState = true;

        // Act
        var response = await _filesSettingsApi.HideConfirmCancelOperationAsync(new SettingsRequestDto { Set = initialState }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(initialState);

        // Change setting
        var newState = false;
        response = await _filesSettingsApi.HideConfirmCancelOperationAsync(new SettingsRequestDto { Set = newState }, TestContext.Current.CancellationToken);

        // Assert changed state
        response.Response.Should().Be(newState);
    }

    [Fact]
    public async Task HideConfirmRoomLifetime_ShouldChangeRoomLifetimeSetting()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);
        var initialState = true;

        // Act
        var response = await _filesSettingsApi.HideConfirmRoomLifetimeAsync(new SettingsRequestDto { Set = initialState }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(initialState);

        // Change setting
        var newState = false;
        response = await _filesSettingsApi.HideConfirmRoomLifetimeAsync(new SettingsRequestDto { Set = newState }, TestContext.Current.CancellationToken);

        // Assert changed state
        response.Response.Should().Be(newState);
    }

    [Fact]
    public async Task StoreOriginal_ShouldChangeStoreOriginalSetting()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);
        var initialState = true;

        // Act
        var response = await _filesSettingsApi.StoreOriginalAsync(new SettingsRequestDto { Set = initialState }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(initialState);

        // Change setting
        var newState = false;
        response = await _filesSettingsApi.StoreOriginalAsync(new SettingsRequestDto { Set = newState }, TestContext.Current.CancellationToken);

        // Assert changed state
        response.Response.Should().Be(newState);
    }

    [Fact]
    public async Task KeepNewFileName_ShouldChangeKeepNewFileNameSetting()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);
        var initialState = true;

        // Act
        var response = await _filesSettingsApi.KeepNewFileNameAsync(new SettingsRequestDto { Set = initialState }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(initialState);

        // Change setting
        var newState = false;
        response = await _filesSettingsApi.KeepNewFileNameAsync(new SettingsRequestDto { Set = newState }, TestContext.Current.CancellationToken);

        // Assert changed state
        response.Response.Should().Be(newState);
    }

    [Fact]
    public async Task DisplayFileExtension_ShouldChangeDisplayExtensionSetting()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);
        var initialState = true;

        // Act
        var response = await _filesSettingsApi.DisplayFileExtensionAsync(new SettingsRequestDto { Set = true }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(initialState);

        // Change setting
        var newState = false;
        response = await _filesSettingsApi.DisplayFileExtensionAsync(new SettingsRequestDto { Set = newState }, TestContext.Current.CancellationToken);

        // Assert changed state
        response.Response.Should().Be(newState);
    }

    [Fact]
    public async Task SetOpenEditorInSameTab_ShouldChangeOpenEditorSetting()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);
        var initialState = true;

        // Act
        var response = await _filesSettingsApi.SetOpenEditorInSameTabAsync(new SettingsRequestDto { Set = initialState }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(initialState);

        // Change setting
        var newState = false;
        response = await _filesSettingsApi.SetOpenEditorInSameTabAsync(new SettingsRequestDto { Set = newState }, TestContext.Current.CancellationToken);

        // Assert changed state
        response.Response.Should().Be(newState);
    }

    // [Fact]
    // public async Task ChangeDownloadZip_ShouldChangeDownloadFormat()
    // {
    //     // Arrange
    //     await InviteContact(EmployeeType.DocSpaceAdmin);
    //
    //     // Act
    //     var response = await _filesSettingsApi.ChangeDownloadZipFromBodyAsync(new DisplayRequestDto { Set = true }, TestContext.Current.CancellationToken);
    //
    //     // Assert
    //     response.Response.Should().NotBeNull();
    // }

    // [Fact]
    // public async Task HideConfirmConvert_ShouldChangeConfirmConvertSetting()
    // {
    //     // Arrange
    //     await InviteContact(EmployeeType.DocSpaceAdmin);
    //
    //     // Act
    //     var response = await _filesSettingsApi.HideConfirmConvertAsync(new HideConfirmConvertRequestDto { Save = true }, TestContext.Current.CancellationToken);
    //
    //     // Assert
    //     response.Response.Should().BeTrue();
    // }

    [Fact]
    public async Task GetAutomaticallyCleanUp_ShouldReturnAutoCleanupSettings()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);

        // Act
        var response = await _filesSettingsApi.GetAutomaticallyCleanUpAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeAutomaticallyCleanUp_ShouldUpdateAutoCleanupSettings()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);
        var setting = new AutoCleanupRequestDto
        {
            Set = true,
            Gap = DateToAutoCleanUp.ThirtyDays
        };

        // Act
        var response = await _filesSettingsApi.ChangeAutomaticallyCleanUpAsync(setting, TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().NotBeNull();
        response.Response.IsAutoCleanUp.Should().BeTrue();
        response.Response.Gap.Should().Be(DateToAutoCleanUp.ThirtyDays);
    }

    [Fact]
    public async Task GetFilesSettings_ShouldReturnSettings()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);

        // Act
        var response = await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFilesModule_ShouldReturnModuleInfo()
    {
        // Arrange
        await InviteContact(EmployeeType.DocSpaceAdmin);

        // Act
        var response = await _filesSettingsApi.GetFilesModuleAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().NotBeNull();
        response.Response.Id.Should().NotBeEmpty();
    }

    // [Fact]
    // public async Task ChangeDefaultAccessRights_ShouldUpdateAccessRights()
    // {
    //     // Arrange
    //     await InviteContact(EmployeeType.DocSpaceAdmin);
    //     var defaultRights = new List<int> { (int)FileShare.Read };
    //
    //     // Act
    //     var response = await _filesSettingsApi.ChangeDefaultAccessRightsAsync(defaultRights, TestContext.Current.CancellationToken);
    //
    //     // Assert
    //     response.Should().NotBeNull();
    //     response.Response.Should().NotBeNull();
    //     response.Response.Should().Contain(r => r.Access == FileShare.Read);
    // }
}
