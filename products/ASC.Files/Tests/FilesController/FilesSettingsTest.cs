// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Files.Tests.FilesController;

public class FilesSettingsTest(
    FilesApiFactory filesFactory,
    WepApiFactory apiFactory,
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram)
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task ChangeAccessToThirdparty_ShouldChangeThirdPartySettings()
    {
        // Arrange
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
    //     await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
    //     await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
        
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
        
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
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
        
        // Act
        var response = await _filesSettingsApi.GetFilesModuleAsync(TestContext.Current.CancellationToken);
        
        // Assert
        response.Should().NotBeNull();
        response.Response.Should().NotBeNull();
        response.Response.Id.Should().NotBeEmpty();
    }
    
    [Fact]
    public async Task IsAvailablePrivacyRoomSettings_ShouldReturnAvailability()
    {
        // Arrange
        await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
        
        // Act
        var response = await _filesSettingsApi.IsAvailablePrivacyRoomSettingsAsync(TestContext.Current.CancellationToken);
        
        // Assert
        response.Should().NotBeNull();
    }
    
    // [Fact]
    // public async Task ChangeDefaultAccessRights_ShouldUpdateAccessRights()
    // {
    //     // Arrange
    //     await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
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