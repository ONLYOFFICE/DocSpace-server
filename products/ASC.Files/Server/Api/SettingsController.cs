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
    [EndpointName("changeAccessToThirdparty")]
    [EndpointSummary("Change the third-party settings access")]
    [EndpointDescription("Changes the access to the third-party settings.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the operation is successful")]
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
    [EndpointName("changeDeleteConfirm")]
    [EndpointSummary("Confirm the file deletion")]
    [EndpointDescription("Specifies whether to confirm the file deletion or not.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the operation is successful")]
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
    [EndpointName("changeDownloadZipFromBody")]
    [EndpointSummary("Change the archive format (using body parameters)")]
    [EndpointDescription("Changes the format of the downloaded archive from .zip to .tar.gz. This method uses the body parameters.")]
    [OpenApiResponse(typeof(ICompress), 200, "Archive")]
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
    [EndpointName("changeDownloadZipFromForm")]
    [EndpointSummary("Change the archive format (using form parameters)")]
    [EndpointDescription("Changes the format of the downloaded archive from .zip to .tar.gz. This method uses the form parameters.")]
    [OpenApiResponse(typeof(ICompress), 200, "Archive")]
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
    [EndpointName("displayFavorite")]
    [EndpointSummary("Display the \"Favorites\" folder")]
    [EndpointDescription("Displays the \"Favorites\" folder.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the parameter is enabled")]
    [OpenApiResponse(401, "You don't have enough permission to perform the operation")]
    [OpenApiResponse(403, "You don't have enough permission to perform the operation")]
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
    [EndpointName("displayRecent")]
    [EndpointSummary("Display the \"Recent\" folder")]
    [EndpointDescription("Displays the \"Recent\" folder.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the parameter is enabled")]
    [OpenApiResponse(401, "You don't have enough permission to perform the operation")]
    [OpenApiResponse(403, "You don't have enough permission to perform the operation")]
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
    [EndpointName("displayTemplates")]
    [EndpointSummary("Display the \"Templates\" folder")]
    [EndpointDescription("Displays the \"Templates\" folder.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the parameter is enabled")]
    [OpenApiResponse(401, "You don't have enough permission to perform the operation")]
    [OpenApiResponse(403, "You don't have enough permission to perform the operation")]
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
    [EndpointName("externalShare")]
    [EndpointSummary("Change the external sharing ability")]
    [EndpointDescription("Changes the ability to share a file externally.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the parameter is enabled")]
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
    [EndpointName("externalShareSocialMedia")]
    [EndpointSummary("Change the external sharing ability on social networks")]
    [EndpointDescription("Changes the ability to share a file externally on social networks.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the parameter is enabled")]
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
    [EndpointName("forcesave")]
    [EndpointSummary("Change the forcasaving ability")]
    [EndpointDescription("Changes the ability to force save a file.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the parameter is enabled")]
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
    [EndpointName("getFilesSettings")]
    [EndpointSummary("Get file settings")]
    [EndpointDescription("Returns all the file settings.")]
    [OpenApiResponse(typeof(FilesSettingsDto), 200, "File settings")]
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
    [EndpointName("getModule")]
    [EndpointSummary("Get the Documents information")]
    [EndpointDescription("Returns the information about the Documents module.")]
    [OpenApiResponse(typeof(Module), 200, "Module information: ID, product class name, title, description, icon URL, large icon URL, start URL, primary or nor, help URL")]
    [HttpGet("info")]
    public Module GetModule()
    {
        productEntryPoint.Init();
        return new Module(productEntryPoint);
    }

    /// <summary>
    /// Hide confirmation dialog when canceling operation.
    /// </summary>
    /// <short>Hide confirmation dialog when canceling operation</short>
    /// <path>api/2.0/files/hideconfirmroomlifetime</path>
    [Tags("Files / Settings")]
    [EndpointName("hideConfirmCancelOperation")]
    [EndpointSummary("Hide confirmation dialog when canceling operation")]
    [EndpointDescription("Hide confirmation dialog when canceling operation.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the parameter is enabled")]
    [HttpPut("hideconfirmcanceloperation")]
    public async Task<bool> HideConfirmCancelOperation(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetHideConfirmCancelOperation(inDto.Set);
    }
    
    /// <summary>
    /// Hides the confirmation dialog for saving the file copy in the original format when converting a file.
    /// </summary>
    /// <short>Hide the confirmation dialog when converting</short>
    /// <path>api/2.0/files/hideconfirmconvert</path>
    [Tags("Files / Settings")]
    [EndpointName("hideConfirmConvert")]
    [EndpointSummary("Hide the confirmation dialog when converting")]
    [EndpointDescription("Hides the confirmation dialog for saving the file copy in the original format when converting a file.")]
    [OpenApiResponse(typeof(Module), 200, "Boolean value: true if the operation is successful")]
    [HttpPut("hideconfirmconvert")]
    public async Task<bool> HideConfirmConvert(HideConfirmConvertRequestDto inDto)
    {
        return await filesSettingsHelper.HideConfirmConvert(inDto.Save);
    }

    /// <summary>
    /// Hide confirmation dialog when changing room lifetime settings.
    /// </summary>
    /// <short>Hide confirmation dialog when changing room lifetime settings</short>
    /// <path>api/2.0/files/hideconfirmroomlifetime</path>
    [Tags("Files / Settings")]
    [EndpointName("hideConfirmRoomLifetime")]
    [EndpointSummary("Hide confirmation dialog when changing room lifetime settings")]
    [EndpointDescription("Hide confirmation dialog when changing room lifetime settings.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the parameter is enabled")]
    [HttpPut("hideconfirmroomlifetime")]
    public async Task<bool> HideConfirmRoomLifetime(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetHideConfirmRoomLifetime(inDto.Set);
    }

    /// <summary>
    /// Checks if the Private Room settings are available or not.
    /// </summary>
    /// <short>Check the Private Room availability</short>
    /// <path>api/2.0/files/@privacy/available</path>
    [Tags("Files / Settings")]
    [EndpointName("isAvailablePrivacyRoomSettings")]
    [EndpointSummary("Check the Private Room availability")]
    [EndpointDescription("Checks if the Private Room settings are available or not.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the Private Room settings are available")]
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
    [EndpointName("storeForcesave")]
    [EndpointSummary("Change the ability to store the forcesaved files")]
    [EndpointDescription("Changes the ability to store the forcesaved file versions.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the operation is successful")]
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
    [EndpointName("storeOriginal")]
    [EndpointSummary("Change the ability to upload original formats")]
    [EndpointDescription("Changes the ability to upload documents in the original formats as well.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the operation is successful")]
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
    [EndpointName("keepNewFileName")]
    [EndpointSummary("Ask a new file name")]
    [EndpointDescription("Specifies whether to ask a user for a file name on creation or not.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the operation is successful")]
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
    [EndpointName("displayFileExtension")]
    [EndpointSummary("Display a file extension")]
    [EndpointDescription("Specifies whether to display a file extension or not.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the parameter is enabled")]
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
    [EndpointName("updateIfExist")]
    [EndpointSummary("Update a file version if it exists")]
    [EndpointDescription("Updates a file version if a file with such a name already exists.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the operation is successful")]
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
    [EndpointName("getAutomaticallyCleanUp")]
    [EndpointSummary("Get the trash bin auto-clearing setting")]
    [EndpointDescription("Returns the trash bin auto-clearing setting.")]
    [OpenApiResponse(typeof(AutoCleanUpData), 200, "The auto-clearing setting properties: auto-clearing or not, a time interval when the auto-clearing will be performed")]
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
    [EndpointName("changeAutomaticallyCleanUp")]
    [EndpointSummary("Update the trash bin auto-clearing setting")]
    [EndpointDescription("Updates the trash bin auto-clearing setting.")]
    [OpenApiResponse(typeof(AutoCleanUpData), 200, "The auto-clearing setting properties: auto-clearing or not, a time interval when the auto-clearing will be performed")]
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
    [EndpointName("changeDefaultAccessRights")]
    [EndpointSummary("Change the default access rights")]
    [EndpointDescription("Changes the default access rights in the sharing settings.")]
    [OpenApiResponse(typeof(List<FileShare>), 200, "Updated sharing rights (None, ReadWrite, Read, Restrict, Varies, Review, Comment, FillForms, CustomFilter, RoomAdmin, Editing, Collaborator)")]
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
    [EndpointName("setOpenEditorInSameTab")]
    [EndpointSummary("Open document in same browser tab")]
    [EndpointDescription("Change the ability to open in a document in the same browser tab")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true if the parameter is enabled")]
    [HttpPut("settings/openeditorinsametab")]
    public async Task<bool> SetOpenEditorInSameTabAsync(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetOpenEditorInSameTabAsync(inDto.Set);
        return await filesSettingsHelper.GetOpenEditorInSameTabAsync();
    }
}
