// (c) Copyright Ascensio System SIA 2009-2024
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

using Module = ASC.Api.Core.Module;

namespace ASC.Files.Api;

public class SettingsController(
    FilesSettingsHelper filesSettingsHelper,
    ProductEntryPoint productEntryPoint,
    FilesSettingsDtoConverter settingsDtoConverter,
    CompressToArchive compressToArchive,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Changes the access to the third-party settings.
    /// </summary>
    /// <short>Change the third-party settings access</short>
    /// <path>api/2.0/files/thirdparty</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("thirdparty")]
    public async Task<bool> ChangeAccessToThirdpartyAsync(SettingsRequestDto inDto)
    {        
        await filesSettingsHelper.SetEnableThirdParty(inDto.Set);

        return await filesSettingsHelper.GetEnableThirdParty();
    }

    /// <summary>
    /// Specifies whether to confirm the file deletion or not.
    /// </summary>
    /// <short>Confirm the file deletion</short>
    /// <path>api/2.0/files/changedeleteconfim</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("changedeleteconfrim")]
    public async Task<bool> ChangeDeleteConfirm(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetConfirmDelete(inDto.Set);
        return await filesSettingsHelper.GetConfirmDelete();
    }

    /// <summary>
    /// Changes the format of the downloaded archive from .zip to .tar.gz. This method uses the body parameters.
    /// </summary>
    /// <short>Change the archive format (using body parameters)</short>
    /// <path>api/2.0/files/settings/downloadtargz</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Archive", typeof(ICompress))]
    [HttpPut("settings/downloadtargz")]
    public async Task<ICompress> ChangeDownloadZipFromBody([FromBody] DisplayRequestDto inDto)
    {        
        await filesSettingsHelper.SetDownloadTarGz(inDto.Set);
        return compressToArchive;
    }

    /// <summary>
    /// Changes the format of the downloaded archive from .zip to .tar.gz. This method uses the form parameters.
    /// </summary>
    /// <short>Change the archive format (using form parameters)</short>
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

    /// <summary>
    /// Displays the "Favorites" folder.
    /// </summary>
    /// <short>Display the "Favorites" folder</short>
    /// <path>api/2.0/files/settings/favorites</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("settings/favorites")]
    public async Task<bool> DisplayFavorite(DisplayRequestDto inDto)
    {
        await filesSettingsHelper.SetFavoritesSection(inDto.Set);
        return await filesSettingsHelper.GetFavoritesSection();
    }

    /// <summary>
    /// Displays the "Recent" folder.
    /// </summary>
    /// <short>Display the "Recent" folder</short>
    /// <path>api/2.0/files/displayRecent</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("displayRecent")]
    public async Task<bool> DisplayRecent(DisplayRequestDto inDto)
    {
        await filesSettingsHelper.SetRecentSection(inDto.Set);
        return  await filesSettingsHelper.GetRecentSection();
    }

    /// <summary>
    /// Displays the "Templates" folder.
    /// </summary>
    /// <short>Display the "Templates" folder</short>
    /// <path>api/2.0/files/settings/templates</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("settings/templates")]
    public async Task<bool> DisplayTemplates(DisplayRequestDto inDto)
    {        
        await filesSettingsHelper.SetTemplatesSection(inDto.Set);
        return await filesSettingsHelper.GetTemplatesSection();
    }

    /// <summary>
    /// Changes the ability to share a file externally.
    /// </summary>
    /// <short>Change the external sharing ability</short>
    /// <path>api/2.0/files/settings/external</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("settings/external")]
    public async Task<bool> ExternalShareAsync(DisplayRequestDto inDto)
    {
        return await filesSettingsHelper.ChangeExternalShareSettingsAsync(inDto.Set);
    }

    /// <summary>
    /// Changes the ability to share a file externally on social networks.
    /// </summary>
    /// <short>Change the external sharing ability on social networks</short>
    /// <path>api/2.0/files/settings/externalsocialmedia</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("settings/externalsocialmedia")]
    public async Task<bool> ExternalShareSocialMediaAsync(DisplayRequestDto inDto)
    {
        return await filesSettingsHelper.ChangeExternalShareSocialMediaSettingsAsync(inDto.Set);
    }

    /// <summary>
    /// Changes the ability to force save a file.
    /// </summary>
    /// <short>Change the forcasaving ability</short>
    /// <path>api/2.0/files/forcesave</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("forcesave")]
    public bool Forcesave()
    {
        return true;
        //return _fileStorageServiceString.Forcesave(inDto.Set);
    }

    /// <summary>
    /// Returns all the file settings.
    /// </summary>
    /// <short>Get file settings</short>
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

    /// <summary>
    /// Returns the information about the Documents module.
    /// </summary>
    /// <short>Get the Documents information</short>
    /// <path>api/2.0/files/info</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Module information: ID, product class name, title, description, icon URL, large icon URL, start URL, primary or nor, help URL", typeof(Module))]
    [HttpGet("info")]
    public Module GetModule()
    {
        productEntryPoint.Init();
        return new Module(productEntryPoint);
    }

    /// <summary>
    /// Hides the confirmation dialog for saving the file copy in the original format when converting a file.
    /// </summary>
    /// <short>Hide the confirmation dialog when converting</short>
    /// <path>api/2.0/files/hideconfirmconvert</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(Module))]
    [HttpPut("hideconfirmconvert")]
    public async Task<bool> HideConfirmConvert(HideConfirmConvertRequestDto inDto)
    {
        return await filesSettingsHelper.HideConfirmConvert(inDto.Save);
    }

    /// <summary>
    /// Checks if the Private Room settings are available or not.
    /// </summary>
    /// <short>Check the Private Room availability</short>
    /// <path>api/2.0/files/@privacy/available</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the Private Room settings are available", typeof(bool))]
    [HttpGet("@privacy/available")]
    public bool IsAvailablePrivacyRoomSettings()
    {
        return PrivacyRoomSettings.IsAvailable();
    }

    /// <summary>
    /// Changes the ability to store the forcesaved file versions.
    /// </summary>
    /// <short>Change the ability to store the forcesaved files</short>
    /// <path>api/2.0/files/storeforcesave</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("storeforcesave")]
    public bool StoreForcesave()
    {
        return false;
        //return _fileStorageServiceString.StoreForcesave(inDto.Set);
    }

    /// <summary>
    /// Changes the ability to upload documents in the original formats as well.
    /// </summary>
    /// <short>Change the ability to upload original formats</short>
    /// <path>api/2.0/files/storeoriginal</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("storeoriginal")]
    public async Task<bool> StoreOriginalAsync(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetStoreOriginalFiles(inDto.Set);
        return await filesSettingsHelper.GetStoreOriginalFiles();
    }

    /// <summary>
    /// Specifies whether to ask a user for a file name on creation or not.
    /// </summary>
    /// <short>Ask a new file name</short>
    /// <path>api/2.0/files/keepnewfilename</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("keepnewfilename")]
    public async Task<bool> KeepNewFileNameAsync(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetKeepNewFileName(inDto.Set);
    }

    /// <summary>
    /// Specifies whether to display a file extension or not.
    /// </summary>
    /// <short>Display a file extension</short>
    /// <path>api/2.0/files/displayfileextension</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("displayfileextension")]
    public async Task<bool> DisplayFileExtension(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetDisplayFileExtension(inDto.Set);
    }

    /// <summary>
    /// Updates a file version if a file with such a name already exists.
    /// </summary>
    /// <short>Update a file version if it exists</short>
    /// <path>api/2.0/files/updateifexist</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("updateifexist")]
    public Task<bool> UpdateIfExistAsync(SettingsRequestDto inDto)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    /// Returns the trash bin auto-clearing setting.
    /// </summary>
    /// <short>Get the trash bin auto-clearing setting</short>
    /// <path>api/2.0/files/settings/autocleanup</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The auto-clearing setting properties: auto-clearing or not, a time interval when the auto-clearing will be performed", typeof(AutoCleanUpData))]
    [HttpGet("settings/autocleanup")]
    public async Task<AutoCleanUpData> GetAutomaticallyCleanUp()
    {
        return await filesSettingsHelper.GetAutomaticallyCleanUp();
    }

    /// <summary>
    /// Updates the trash bin auto-clearing setting.
    /// </summary>
    /// <short>Update the trash bin auto-clearing setting</short>
    /// <path>api/2.0/files/settings/autocleanup</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The auto-clearing setting properties: auto-clearing or not, a time interval when the auto-clearing will be performed", typeof(AutoCleanUpData))]
    [HttpPut("settings/autocleanup")]
    public async Task<AutoCleanUpData> ChangeAutomaticallyCleanUp(AutoCleanupRequestDto inDto)
    {
        await filesSettingsHelper.SetAutomaticallyCleanUp(new AutoCleanUpData { IsAutoCleanUp = inDto.Set, Gap = inDto.Gap });
        return await filesSettingsHelper.GetAutomaticallyCleanUp();
    }

    /// <summary>
    /// Changes the default access rights in the sharing settings.
    /// </summary>
    /// <short>Change the default access rights</short>
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

    /// <summary>
    /// Change the ability to open in a document in the same browser tab
    /// </summary>
    /// <short>Open document in same browser tab</short>
    /// <path>api/2.0/files/settings/openeditorinsametab</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("settings/openeditorinsametab")]
    public async Task<bool> SetOpenEditorInSameTabAsync(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetOpenEditorInSameTabAsync(inDto.Set);
        return await filesSettingsHelper.GetOpenEditorInSameTabAsync();
    }
}
