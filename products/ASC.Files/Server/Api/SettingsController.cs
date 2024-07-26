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
    /// <category>Settings</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SettingsRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/files/thirdparty</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <category>Settings</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SettingsRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/files/changedeleteconfim</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DisplayRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <category>Settings</category>
    /// <returns type="ASC.Web.Files.Core.Compress.ICompress, ASC.Files.Core">Archive</returns>
    /// <path>api/2.0/files/settings/downloadtargz</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DisplayRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <category>Settings</category>
    /// <returns type="ASC.Web.Files.Core.Compress.ICompress, ASC.Files.Core">Archive</returns>
    /// <path>api/2.0/files/settings/downloadtargz</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DisplayRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the parameter is enabled</returns>
    /// <path>api/2.0/files/settings/favorites</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DisplayRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the parameter is enabled</returns>
    /// <path>api/2.0/files/displayRecent</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DisplayRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the parameter is enabled</returns>
    /// <path>api/2.0/files/settings/templates</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DisplayRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the parameter is enabled</returns>
    /// <path>api/2.0/files/settings/external</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DisplayRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the parameter is enabled</returns>
    /// <path>api/2.0/files/settings/externalsocialmedia</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/files/forcesave</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <category>Settings</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FilesSettingsDto, ASC.Files.Core">File settings</returns>
    /// <path>api/2.0/files/settings</path>
    /// <httpMethod>GET</httpMethod>
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
    /// <category>Settings</category>
    /// <returns type="ASC.Api.Core.Module, ASC.Api.Core">Module information: ID, product class name, title, description, icon URL, large icon URL, start URL, primary or nor, help URL</returns>
    /// <path>api/2.0/files/info</path>
    /// <httpMethod>GET</httpMethod>
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
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.HideConfirmConvertRequestDto, ASC.Files.Core" name="inDto">Request parameters for hiding the confirmation dialog</param>
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/files/hideconfirmconvert</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the Private Room settings are available</returns>
    /// <path>api/2.0/files/@privacy/available</path>
    /// <httpMethod>GET</httpMethod>
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
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/files/storeforcesave</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <category>Settings</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SettingsRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <returns type="System.Boolean, System">Boolean value: true if the operation is successful</returns>
    /// <path>api/2.0/files/storeoriginal</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SettingsRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the parameter is enabled</returns>
    /// <path>api/2.0/files/keepnewfilename</path>
    /// <httpMethod>PUT</httpMethod>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [HttpPut("keepnewfilename")]
    public async Task<bool> KeepNewFileNameAsync(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetKeepNewFileName(inDto.Set);
    }

    /// <summary>
    /// Updates a file version if a file with such a name already exists.
    /// </summary>
    /// <short>Update a file version if it exists</short>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SettingsRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the parameter is enabled</returns>
    /// <path>api/2.0/files/updateifexist</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <category>Settings</category>
    /// <returns type="ASC.Files.Core.AutoCleanUpData, ASC.Files.Core">The auto-clearing setting properties: auto-clearing or not, a time interval when the auto-clearing will be performed</returns>
    /// <path>api/2.0/files/settings/autocleanup</path>
    /// <httpMethod>GET</httpMethod>
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
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.AutoCleanupRequestDto, ASC.Files.Core" name="inDto">Auto-clearing request parameters</param>
    /// <category>Settings</category>
    /// <returns type="ASC.Files.Core.AutoCleanUpData, ASC.Files.Core">The auto-clearing setting properties: auto-clearing or not, a time interval when the auto-clearing will be performed</returns>
    /// <path>api/2.0/files/settings/autocleanup</path>
    /// <httpMethod>PUT</httpMethod>
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
    /// <param type="System.Collections.Generic.List{ASC.Files.Core.Security.FileShare}, System.Collections.Generic" name="value" example="None">Sharing rights (None, ReadWrite, Read, Restrict, Varies, Review, Comment, FillForms, CustomFilter, RoomAdmin, Editing, Collaborator)</param>
    /// <category>Settings</category>
    /// <returns type="ASC.Files.Core.Security.FileShare, ASC.Files.Core">Updated sharing rights (None, ReadWrite, Read, Restrict, Varies, Review, Comment, FillForms, CustomFilter, RoomAdmin, Editing, Collaborator)</returns>
    /// <path>api/2.0/files/settings/dafaultaccessrights</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Updated sharing rights (None, ReadWrite, Read, Restrict, Varies, Review, Comment, FillForms, CustomFilter, RoomAdmin, Editing, Collaborator)", typeof(List<FileShare>))]
    [HttpPut("settings/dafaultaccessrights")]
    public async Task<List<FileShare>> ChangeDefaultAccessRights(List<FileShare> value)
    {        
        await filesSettingsHelper.SetDefaultSharingAccessRights(value);
        return await filesSettingsHelper.GetDefaultSharingAccessRights();
    }

    /// <summary>
    /// Change the ability to open in a document in the same browser tab
    /// </summary>
    /// <short>Open document in same browser tab</short>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SettingsRequestDto, ASC.Files.Core" name="inDto">Settings request parameters</param>
    /// <category>Settings</category>
    /// <returns type="System.Boolean, System">Boolean value: true if the parameter is enabled</returns>
    /// <path>api/2.0/files/settings/openeditorinsametab</path>
    /// <httpMethod>PUT</httpMethod>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Boolean value: true if the parameter is enabled", typeof(bool))]
    [HttpPut("settings/openeditorinsametab")]
    public async Task<bool> SetOpenEditorInSameTabAsync(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetOpenEditorInSameTabAsync(inDto.Set);
        return await filesSettingsHelper.GetOpenEditorInSameTabAsync();
    }
}
