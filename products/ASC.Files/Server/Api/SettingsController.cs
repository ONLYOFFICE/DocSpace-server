// (c) Copyright Ascensio System SIA 2009-2026
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

using ASC.Files.Core.Configuration;

using Module = ASC.Api.Core.Module;

namespace ASC.Files.Api;

public class SettingsController(
    FilesSettingsHelper filesSettingsHelper,
    ProductEntryPoint productEntryPoint,
    FilesSettingsDtoConverter settingsDtoConverter,
    CompressToArchive compressToArchive,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    DefaultTemplateSettingsHelper defaultTemplateSettingsHelper,
    PermissionContext permissionContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Changes the access to the third-party settings.
    /// </remarks>
    /// <summary>Change the third-party settings access</summary>
    /// <path>api/2.0/files/thirdparty</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("thirdparty")]
    public async Task<bool> ChangeAccessToThirdparty(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetEnableThirdParty(inDto.Set);

        return await filesSettingsHelper.GetEnableThirdParty();
    }

    /// <remarks>
    /// Specifies whether to confirm the file deletion or not.
    /// </remarks>
    /// <summary>Confirm the file deletion</summary>
    /// <path>api/2.0/files/changedeleteconfim</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("changedeleteconfrim")]
    public async Task<bool> ChangeDeleteConfirm(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetConfirmDelete(inDto.Set);
        return await filesSettingsHelper.GetConfirmDelete();
    }

    /// <remarks>
    /// Changes the format of the downloaded archive from .zip to .tar.gz. This method uses the body parameters.
    /// </remarks>
    /// <summary>Change the archive format (using body parameters)</summary>
    /// <path>api/2.0/files/settings/downloadtargz</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Archive", typeof(ICompress))]
    [HttpPut("settings/downloadtargz")]
    public async Task<ICompress> ChangeDownloadZipFromBody([FromBody] DisplayRequestDto inDto)
    {
        await filesSettingsHelper.SetDownloadTarGz(inDto.Set);
        return compressToArchive;
    }

    /// <remarks>
    /// Changes the format of the downloaded archive from .zip to .tar.gz. This method uses the form parameters.
    /// </remarks>
    /// <summary>Change the archive format (using form parameters)</summary>
    /// <path>api/2.0/files/settings/downloadtargz</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Archive", typeof(ICompress))]
    [HttpPut("settings/downloadtargz")]
    public async Task<ICompress> ChangeDownloadZipFromForm([FromForm] DisplayRequestDto inDto)
    {
        await filesSettingsHelper.SetDownloadTarGz(inDto.Set);
        return compressToArchive;
    }

    /// <remarks>
    /// Displays the "Favorites" folder.
    /// </remarks>
    /// <summary>Display the "Favorites" folder</summary>
    /// <path>api/2.0/files/settings/favorites</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpPut("settings/favorites")]
    public async Task<bool> DisplayFavorite(DisplayRequestDto inDto)
    {
        await filesSettingsHelper.SetFavoritesSection(inDto.Set);
        return await filesSettingsHelper.GetFavoritesSection();
    }

    /// <remarks>
    /// Displays the "Recent" folder.
    /// </remarks>
    /// <summary>Display the "Recent" folder</summary>
    /// <path>api/2.0/files/displayRecent</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpPut("displayRecent")]
    public async Task<bool> DisplayRecent(DisplayRequestDto inDto)
    {
        await filesSettingsHelper.SetRecentSection(inDto.Set);
        return await filesSettingsHelper.GetRecentSection();
    }

    /// <remarks>
    /// Displays the "Templates" folder.
    /// </remarks>
    /// <summary>Display the "Templates" folder</summary>
    /// <path>api/2.0/files/settings/templates</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpPut("settings/templates")]
    public async Task<bool> DisplayTemplates(DisplayRequestDto inDto)
    {
        await filesSettingsHelper.SetTemplatesSection(inDto.Set);
        return await filesSettingsHelper.GetTemplatesSection();
    }

    /// <remarks>
    /// Changes the ability to share a file externally.
    /// </remarks>
    /// <summary>Change the external sharing ability</summary>
    /// <path>api/2.0/files/settings/external</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("settings/external")]
    public async Task<bool> ExternalShare(DisplayRequestDto inDto)
    {
        return await filesSettingsHelper.ChangeExternalShareSettingsAsync(inDto.Set);
    }

    /// <remarks>
    /// Changes the ability to share a file externally on social networks.
    /// </remarks>
    /// <summary>Change the external sharing ability on social networks</summary>
    /// <path>api/2.0/files/settings/externalsocialmedia</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("settings/externalsocialmedia")]
    public async Task<bool> ExternalShareSocialMedia(DisplayRequestDto inDto)
    {
        return await filesSettingsHelper.ChangeExternalShareSocialMediaSettingsAsync(inDto.Set);
    }

    /// <remarks>
    /// Specifies if the file forcesaving is enabled or not.
    /// </remarks>
    /// <summary>Change the forcesaving ability</summary>
    /// <path>api/2.0/files/forcesave</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("forcesave")]
    public bool Forcesave()
    {
        return true;
        //return _fileStorageServiceString.Forcesave(inDto.Set);
    }

    /// <remarks>
    /// Returns all the file settings.
    /// </remarks>
    /// <summary>Get file settings</summary>
    /// <path>api/2.0/files/settings</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "File settings", typeof(FilesSettingsDto))]
    [AllowAnonymous]
    [HttpGet("settings")]
    public async Task<FilesSettingsDto> GetFilesSettings()
    {
        return await settingsDtoConverter.Get();
    }

    /// <remarks>
    /// Returns the information about the "Documents" module.
    /// </remarks>
    /// <summary>Get the "Documents" information</summary>
    /// <path>api/2.0/files/info</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Module information: ID, product class name, title, description, icon URL, large icon URL, start URL, primary or nor, help URL", typeof(Module))]
    [HttpGet("info")]
    public Module GetFilesModule()
    {
        productEntryPoint.Init();
        return new Module(productEntryPoint);
    }

    /// <remarks>
    /// Hides the confirmation dialog when canceling operations.
    /// </remarks>
    /// <summary>Hide confirmation dialog when canceling operations</summary>
    /// <path>api/2.0/files/hideconfirmcanceloperation</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("hideconfirmcanceloperation")]
    public async Task<bool> HideConfirmCancelOperation(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetHideConfirmCancelOperation(inDto.Set);
    }

    /// <remarks>
    /// Hides the confirmation dialog for saving the file copy in the original format when converting a file.
    /// </remarks>
    /// <summary>Hide the confirmation dialog when converting</summary>
    /// <path>api/2.0/files/hideconfirmconvert</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(Module))]
    [HttpPut("hideconfirmconvert")]
    public async Task<bool> HideConfirmConvert(HideConfirmConvertRequestDto inDto)
    {
        return await filesSettingsHelper.HideConfirmConvert(inDto.Save);
    }

    /// <remarks>
    /// Hides the confirmation dialog when changing the room lifetime settings.
    /// </remarks>
    /// <summary>Hide confirmation dialog when changing room lifetime settings</summary>
    /// <path>api/2.0/files/hideconfirmroomlifetime</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("hideconfirmroomlifetime")]
    public async Task<bool> HideConfirmRoomLifetime(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetHideConfirmRoomLifetime(inDto.Set);
    }

    /// <remarks>
    /// Checks if the "Private Room" settings are available or not.
    /// </remarks>
    /// <summary>Check the "Private Room" availability</summary>
    /// <path>api/2.0/files/@privacy/available</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the Private Room settings are available", typeof(bool))]
    [HttpGet("@privacy/available")]
    public bool IsAvailablePrivacyRoomSettings()
    {
        return PrivacyRoomSettings.IsAvailable();
    }

    /// <remarks>
    /// Changes the ability to store the forcesaved file versions.
    /// </remarks>
    /// <summary>Change the ability to store the forcesaved files</summary>
    /// <path>api/2.0/files/storeforcesave</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("storeforcesave")]
    public bool StoreForcesave()
    {
        return false;
        //return _fileStorageServiceString.StoreForcesave(inDto.Set);
    }

    /// <remarks>
    /// Changes the ability to upload documents in the original formats as well.
    /// </remarks>
    /// <summary>Change the ability to upload original formats</summary>
    /// <path>api/2.0/files/storeoriginal</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("storeoriginal")]
    public async Task<bool> StoreOriginal(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetStoreOriginalFiles(inDto.Set);
        return await filesSettingsHelper.GetStoreOriginalFiles();
    }

    /// <remarks>
    /// Specifies whether to ask a user for a file name on creation or not.
    /// </remarks>
    /// <summary>Ask a new file name</summary>
    /// <path>api/2.0/files/keepnewfilename</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("keepnewfilename")]
    public async Task<bool> KeepNewFileName(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetKeepNewFileName(inDto.Set);
    }

    /// <remarks>
    /// Specifies whether to display a file extension or not.
    /// </remarks>
    /// <summary>Display a file extension</summary>
    /// <path>api/2.0/files/displayfileextension</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("displayfileextension")]
    public async Task<bool> DisplayFileExtension(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetDisplayFileExtension(inDto.Set);
    }

    /// <remarks>
    /// Updates a file version if a file with such a name already exists.
    /// </remarks>
    /// <summary>Update a file version if it exists</summary>
    /// <path>api/2.0/files/updateifexist</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("updateifexist")]
    public Task<bool> UpdateFileIfExist(SettingsRequestDto inDto)
    {
        return Task.FromResult(false);
    }

    /// <remarks>
    /// Returns the trash bin auto-clearing setting.
    /// </remarks>
    /// <summary>Get the trash bin auto-clearing setting</summary>
    /// <path>api/2.0/files/settings/autocleanup</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The auto-clearing setting properties: auto-clearing or not, a time interval when the auto-clearing will be performed", typeof(AutoCleanUpData))]
    [HttpGet("settings/autocleanup")]
    public async Task<AutoCleanUpData> GetAutomaticallyCleanUp()
    {
        return await filesSettingsHelper.GetAutomaticallyCleanUp();
    }

    /// <remarks>
    /// Updates the trash bin auto-clearing setting.
    /// </remarks>
    /// <summary>Update the trash bin auto-clearing setting</summary>
    /// <path>api/2.0/files/settings/autocleanup</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The auto-clearing setting properties: auto-clearing or not, a time interval when the auto-clearing will be performed", typeof(AutoCleanUpData))]
    [HttpPut("settings/autocleanup")]
    public async Task<AutoCleanUpData> ChangeAutomaticallyCleanUp(AutoCleanupRequestDto inDto)
    {
        await filesSettingsHelper.SetAutomaticallyCleanUp(new AutoCleanUpData { IsAutoCleanUp = inDto.Set, Gap = inDto.Gap });
        return await filesSettingsHelper.GetAutomaticallyCleanUp();
    }

    /// <remarks>
    /// Changes the default access rights in the sharing settings.
    /// </remarks>
    /// <summary>Change the default access rights</summary>
    /// <path>api/2.0/files/settings/dafaultaccessrights</path>
    /// <collection>list</collection>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Updated sharing rights (None, ReadWrite, Read, Restrict, Varies, Review, Comment, FillForms, CustomFilter, RoomAdmin, Editing, Collaborator)", typeof(List<FileShare>))]
    [HttpPut("settings/dafaultaccessrights")]
    public async Task<List<FileShare>> ChangeDefaultAccessRights(DefaultAccessRightsrequestDto inDto)
    {
        await filesSettingsHelper.SetDefaultSharingAccessRights(inDto.Value);
        return await filesSettingsHelper.GetDefaultSharingAccessRights();
    }

    /// <remarks>
    /// Changes the ability to open the document in the same browser tab.
    /// </remarks>
    /// <summary>Open document in the same browser tab</summary>
    /// <path>api/2.0/files/settings/openeditorinsametab</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("settings/openeditorinsametab")]
    public async Task<bool> SetOpenEditorInSameTab(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetOpenEditorInSameTabAsync(inDto.Set);
        return await filesSettingsHelper.GetOpenEditorInSameTabAsync();
    }

    /// <remarks>
    /// Returns the default template setting.
    /// </remarks>
    /// <summary>Get the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Default template settings", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpGet("settings/defaulttemplate")]
    public async Task<DefaultTemplateSettingsDto> GetDefaultTemplates()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await defaultTemplateSettingsHelper.GetSettingsAsync();
        return await defaultTemplateSettingsHelper.ConvertToDtoAsync(settings);
    }

    /// <remarks>
    /// Changes the default template setting.
    /// </remarks>
    /// <summary>Change the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "New default template settings", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(400, "Incorrect or missing file")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpPut("settings/defaulttemplate")]
    public async Task<DefaultTemplateSettingsDto> SetDefaultTemplate(DefaultTemplateSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await defaultTemplateSettingsHelper.SetTemplateAsync(inDto.FileExtension, inDto.SelectedFile);
        return await defaultTemplateSettingsHelper.ConvertToDtoAsync(settings);
    }

    /// <remarks>
    /// Uploads a file to use as the default template setting.
    /// </remarks>
    /// <summary>Upload a file as the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "New default template settings", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(400, "Incorrect or missing file")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpPost("settings/defaulttemplate")]
    public async Task<DefaultTemplateSettingsDto> UploadDefaultTemplate(DefaultTemplateSettingsUploadRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await defaultTemplateSettingsHelper.SetTemplateAsync(inDto.FileExtension, inDto.File.FileName, inDto?.File.OpenReadStream());
        return await defaultTemplateSettingsHelper.ConvertToDtoAsync(settings);
    }

    /// <remarks>
    /// Changes the setting that allows the user to organize the grouping of rooms.
    /// </remarks>
    /// <summary>Organize rooms grouping</summary>
    /// <path>api/2.0/settings/organizegrouping</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("settings/organizegrouping")]
    public async Task<bool> SetOrganizeRoomsGrouping(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetOrganizeRoomsGroupingAsync(inDto.Set);
        return await filesSettingsHelper.GetOrganizeRoomsGroupingAsync();
    }
}