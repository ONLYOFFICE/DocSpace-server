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

using ASC.Files.Core.Configuration;

namespace ASC.Files.Api;

public class SettingsController(
    FilesSettingsHelper filesSettingsHelper,
    ProductEntryPoint productEntryPoint,
    FilesSettingsDtoConverter settingsDtoConverter,
    CompressToArchive compressToArchive,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    DefaultTemplateSettingsConverter defaultTemplateSettingsConverter,
    DefaultTemplateSettingsHelper defaultTemplateSettingsHelper,
    PermissionContext permissionContext,
    AuthContext authContext,
    ExternalShare externalShare)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Turns the connection of third-party storages on or off for the whole portal. It returns the value that is now
    /// stored. Requires a DocSpace admin. Other callers get 403. The call changes data, and you can repeat it with
    /// the same body. While the setting is off, `POST api/2.0/files/thirdparty` returns 403. Reading a folder of a
    /// connected storage also returns 403, but the connections stay. After the setting is turned on again, they work
    /// again. To list the storages the portal offers, use `GET api/2.0/files/thirdparty/capabilities`.
    /// </remarks>
    /// <summary>Change the third-party settings access</summary>
    /// <path>api/2.0/files/thirdparty</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if third-party storages may be connected in this portal", typeof(bool))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpPut("thirdparty")]
    public async Task<bool> ChangeAccessToThirdparty(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetEnableThirdParty(inDto.Set);

        return await filesSettingsHelper.GetEnableThirdParty();
    }

    /// <remarks>
    /// Changes whether the calling account is asked to confirm a deletion of files and folders. It returns the value
    /// that is now stored. Any signed-in user can call it. The call changes data, and you can repeat it with the same
    /// body. The setting only guides the app. The delete operations under `api/2.0/files/fileops` ignore it. To read
    /// the value, use the `confirmDelete` field of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Ask for delete confirmation</summary>
    /// <path>api/2.0/files/changedeleteconfrim</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if the caller is asked to confirm a deletion", typeof(bool))]
    [HttpPut("changedeleteconfrim")]
    public async Task<bool> ChangeDeleteConfirm(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetConfirmDelete(inDto.Set);
        return await filesSettingsHelper.GetConfirmDelete();
    }

    /// <remarks>
    /// Changes the archive format of multi-item downloads for the calling account. If `set` is true, the portal packs
    /// them as `.tar.gz`. If it is false, the portal uses `.zip`. Any signed-in user can call it. The call changes
    /// data, and you can repeat it with the same body. The format applies to the archives that
    /// `PUT api/2.0/files/fileops/bulkdownload` builds after this call. The response is an object without fields. To
    /// read the format, use the `downloadTarGz` field of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Change the download archive format</summary>
    /// <path>api/2.0/files/settings/downloadtargz</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The archive helper for the format that is now selected", typeof(ICompress))]
    [HttpPut("settings/downloadtargz")]
    public async Task<ICompress> ChangeDownloadZip(DisplayRequestDto inDto)
    {
        await filesSettingsHelper.SetDownloadTarGz(inDto.Set);
        return compressToArchive;
    }

    /// <remarks>
    /// Shows or hides the Favorites section for the calling account. It returns `true` if the section is shown. Any
    /// signed-in user can call it. The call changes data, and you can repeat it with the same body. To read the
    /// value, use the `favoritesSection` field of `GET api/2.0/files/settings`.
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
    /// Shows or hides the Recent section for the calling account. It returns `true` if the section is shown. Any
    /// signed-in user can call it. The call changes data, and you can repeat it with the same body. A hidden section
    /// no longer appears in `GET api/2.0/files/@root` and in the document editor. The portal still records recent
    /// files, and `GET api/2.0/files/recent` still returns them. To read the value, use the `recentSection` field of
    /// `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Show the Recent section</summary>
    /// <path>api/2.0/files/displayrecent</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if the Recent section is offered to the caller", typeof(bool))]
    [HttpPut("displayRecent")]
    public async Task<bool> DisplayRecent(DisplayRequestDto inDto)
    {
        await filesSettingsHelper.SetRecentSection(inDto.Set);
        return await filesSettingsHelper.GetRecentSection();
    }

    /// <remarks>
    /// Shows or hides the Templates section for the calling account. It returns `true` if the section is shown. Any
    /// signed-in user can call it. The call changes data, and you can repeat it with the same body. To read the
    /// value, use the `templatesSection` field of `GET api/2.0/files/settings`.
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
    /// Turns external links on or off for the whole portal. It returns the value that is now stored. Requires a
    /// DocSpace admin. Other callers get 403. The call changes data, and you can repeat it with the same body.
    /// Turning external links off also turns off sharing on social networks. The call does not delete existing links.
    /// The `blockExistingLinksOnRestrict` field decides if they keep working. To set it and the rest of the sharing
    /// policy, use `PUT api/2.0/files/settings/externalsharingsettings`.
    /// </remarks>
    /// <summary>Change the external sharing ability</summary>
    /// <path>api/2.0/files/settings/external</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if external links may be created in this portal", typeof(bool))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpPut("settings/external")]
    public async Task<bool> ExternalShare(DisplayRequestDto inDto)
    {
        return await filesSettingsHelper.ChangeExternalShareSettingsAsync(inDto.Set);
    }

    /// <remarks>
    /// Changes the whole external sharing policy of the portal. It returns the policy that is now in force. Requires
    /// a DocSpace admin. Other callers get 403. The call changes data, and you can repeat it with the same body. The
    /// call writes every field, so an omitted field becomes false. Send the whole policy, not only the changed field.
    /// If `externalShare` is false, the portal sets `defaultShareLinkInternal` to true. It also turns off sharing on
    /// social networks. The `blockExistingLinksOnRestrict` field decides if existing links keep working while
    /// external sharing is off.
    /// </remarks>
    /// <summary>Configure external sharing</summary>
    /// <path>api/2.0/files/settings/externalsharingsettings</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The external sharing policy that is now in force", typeof(ExternalSharingSettingsDto))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpPut("settings/externalsharingsettings")]
    public async Task<ExternalSharingSettingsDto> ChangeExternalSharingSettings(ExternalSharingSettingsRequestDto inDto)
    {
        return await filesSettingsHelper.ChangeExternalSharingSettingsAsync(inDto);
    }

    /// <remarks>
    /// Turns sharing on social networks on or off for the whole portal. It returns the value that is now in force. It
    /// needs external links turned on with `PUT api/2.0/files/settings/external`. Requires a DocSpace admin. Other
    /// callers get 403. The call changes data, and you can repeat it with the same body. While external links are
    /// off, the call stores false and returns false. If external links are turned off later, this setting is turned
    /// off too. The setting does not create or delete links.
    /// </remarks>
    /// <summary>Change the external sharing ability on social networks</summary>
    /// <path>api/2.0/files/settings/externalsocialmedia</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if sharing on social networks is now in force", typeof(bool))]
    [SwaggerResponse(403, "The caller is not a DocSpace administrator")]
    [HttpPut("settings/externalsocialmedia")]
    public async Task<bool> ExternalShareSocialMedia(DisplayRequestDto inDto)
    {
        return await filesSettingsHelper.ChangeExternalShareSocialMediaSettingsAsync(inDto.Set);
    }

    /// <remarks>
    /// Changes nothing and always returns `true`. Forcesaving is on in every portal, and the API cannot turn it off.
    /// With forcesaving, the editor saves a document to storage while it is still open. The call is kept for
    /// compatibility and takes no body. Any signed-in user can call it. You can repeat it. The `forcesave` field of
    /// `GET api/2.0/files/settings` returns the same value.
    /// </remarks>
    /// <summary>Change the forcesaving ability</summary>
    /// <path>api/2.0/files/forcesave</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Always true: forcesaving is on for every portal", typeof(bool))]
    [HttpPut("forcesave")]
    public bool Forcesave()
    {
        return true;
        //return _fileStorageServiceString.Forcesave(inDto.Set);
    }

    /// <remarks>
    /// Returns the Files settings of the caller and the portal in one object. The object has the caller's
    /// preferences, the portal switches, the format tables, the editor URLs, and the upload limits. Any signed-in
    /// user can call it. A visitor with an external link can also call it without signing in. Other callers get 401.
    /// The call is read-only. Most setting calls of this section return only the value they changed. Read the other
    /// values here.
    /// </remarks>
    /// <summary>Get file settings</summary>
    /// <path>api/2.0/files/settings</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The full set of file settings for the caller and the portal", typeof(FilesSettingsDto))]
    [SwaggerResponse(401, "The caller is neither signed in nor opening an external link")]
    [AllowAnonymous]
    [HttpGet("settings")]
    public async Task<FilesSettingsDto> GetFilesSettings()
    {
        // [AllowAnonymous] stays for external-link viewers (the share page reads the extension
        // tables before authenticating); a caller with neither a session nor a link key gets 401.
        if (!authContext.IsAuthenticated && await externalShare.GetLinkIdAsync() == Guid.Empty)
        {
            throw new AuthenticationException();
        }

        return await settingsDtoConverter.Get();
    }

    /// <remarks>
    /// Returns the descriptor of the Documents module of the portal. Any signed-in user can call it. The call is
    /// read-only. The title and the description come in the language of the caller. The response is the same for
    /// every user of the portal. For the format tables, the editor URLs, and the upload limits, use
    /// `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Get the Documents module information</summary>
    /// <path>api/2.0/files/info</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The descriptor of the Documents module: identifier, title, description, icon and start addresses", typeof(ASC.Api.Core.Module))]
    [HttpGet("info")]
    public ASC.Api.Core.Module GetFilesModule()
    {
        productEntryPoint.Init();
        return new ASC.Api.Core.Module(productEntryPoint);
    }

    /// <remarks>
    /// Hides or shows the confirmation that the app asks before it cancels a running file operation. The setting is
    /// for the calling account. If `set` is true, the app hides the confirmation. If it is false, the app shows it
    /// again. Any signed-in user can call it. The call changes data, and you can repeat it with the same body. It
    /// returns the value that is now stored. The setting does not change how the API cancels operations. To read it,
    /// use the `hideConfirmCancelOperation` field of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Hide confirmation dialog when canceling operations</summary>
    /// <path>api/2.0/files/hideconfirmcanceloperation</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if the cancel confirmation is now hidden for the caller", typeof(bool))]
    [HttpPut("hideconfirmcanceloperation")]
    public async Task<bool> HideConfirmCancelOperation(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetHideConfirmCancelOperation(inDto.Set);
    }

    /// <remarks>
    /// Hides one of two conversion prompts for the calling account. If `save` is true, the call hides the prompt to
    /// keep a copy in the original format. If `save` is false, it hides the prompt to open the converted file. Any
    /// signed-in user can call it. The call changes data, and you can repeat it with the same body. No call of the
    /// API shows a hidden prompt again. The call always returns `true`. To read both flags, use
    /// `hideConfirmConvertSave` and `hideConfirmConvertOpen` of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Hide the confirmation dialog when converting</summary>
    /// <path>api/2.0/files/hideconfirmconvert</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Always true: the chosen conversion prompt is now hidden", typeof(bool))]
    [HttpPut("hideconfirmconvert")]
    public async Task<bool> HideConfirmConvert(HideConfirmConvertRequestDto inDto)
    {
        return await filesSettingsHelper.HideConfirmConvert(inDto.Save);
    }

    /// <remarks>
    /// Hides or shows the warning that the app shows before it changes the lifetime of a room. The setting is for the
    /// calling account. If `set` is true, the app hides the warning. If it is false, the app shows it again. Any
    /// signed-in user can call it. The call changes data, and you can repeat it with the same body. It returns the
    /// value that is now stored. The setting does not change room lifetimes or `PUT api/2.0/files/rooms/{id}`. To
    /// read it, use the `hideConfirmRoomLifetime` field of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Hide confirmation dialog when changing room lifetime settings</summary>
    /// <path>api/2.0/files/hideconfirmroomlifetime</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if the room lifetime warning is now hidden for the caller", typeof(bool))]
    [HttpPut("hideconfirmroomlifetime")]
    public async Task<bool> HideConfirmRoomLifetime(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetHideConfirmRoomLifetime(inDto.Set);
    }

    /// <remarks>
    /// Changes nothing and always returns `false`. The value means that the portal does not keep forcesaved states as
    /// separate file versions. The call is kept for compatibility and takes no body. Any signed-in user can call it.
    /// You can repeat it. The `storeForcesave` field of `GET api/2.0/files/settings` returns the same value.
    /// </remarks>
    /// <summary>Change the ability to store the forcesaved files</summary>
    /// <path>api/2.0/files/storeforcesave</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Always false: forcesaved versions are not kept separately", typeof(bool))]
    [HttpPut("storeforcesave")]
    public bool StoreForcesave()
    {
        return false;
        //return _fileStorageServiceString.StoreForcesave(inDto.Set);
    }

    /// <remarks>
    /// Changes whether a conversion on upload keeps the original file for the calling account. It returns the value
    /// that is now stored. If `set` is true, the portal saves the converted file next to the original. If it is
    /// false, the converted file replaces the upload as its new version. This happens only when the caller can edit
    /// the file. Any signed-in user can call it. The call changes data, and you can repeat it with the same body. The
    /// setting also applies to `PUT api/2.0/files/file/{fileId}/checkconversion`. To read it, use the
    /// `storeOriginalFiles` field of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Change the ability to upload original formats</summary>
    /// <path>api/2.0/files/storeoriginal</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if the original file is kept when an upload is converted", typeof(bool))]
    [HttpPut("storeoriginal")]
    public async Task<bool> StoreOriginal(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetStoreOriginalFiles(inDto.Set);
        return await filesSettingsHelper.GetStoreOriginalFiles();
    }

    /// <remarks>
    /// Changes whether new documents get the default name without asking the user. The setting is for the calling
    /// account and only guides the app. It returns the value that is now stored. Any signed-in user can call it. The
    /// call changes data, and you can repeat it with the same body. Calls that create files, such as `POST
    /// api/2.0/files/{folderId}/file`, never pick a default name, but may change the extension. To read the value, use
    /// the `keepNewFileName` field of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Keep the default file name</summary>
    /// <path>api/2.0/files/keepnewfilename</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if new documents are created with the default name", typeof(bool))]
    [HttpPut("keepnewfilename")]
    public async Task<bool> KeepNewFileName(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetKeepNewFileName(inDto.Set);
    }

    /// <remarks>
    /// Changes whether the calling account sees file names with their extension. It returns the value that is now
    /// stored. Any signed-in user can call it. The call changes data, and you can repeat it with the same body. The
    /// setting only guides the app. File titles in the API always include the extension. So send the full title when
    /// you rename or search. To read the value, use the `displayFileExtension` field of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Display a file extension</summary>
    /// <path>api/2.0/files/displayfileextension</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if file titles are shown with their extension", typeof(bool))]
    [HttpPut("displayfileextension")]
    public async Task<bool> DisplayFileExtension(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetDisplayFileExtension(inDto.Set);
    }

    /// <remarks>
    /// Turns the quick action buttons next to file names on or off for the calling account. Any signed-in user can
    /// call it. The call changes data, and you can repeat it with the same body. An account that never set the value
    /// has the buttons on. The response repeats the sent value and does not read the stored one. To read the stored
    /// value, use the `showQuickActions` field of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Display quick actions</summary>
    /// <path>api/2.0/files/showquickactions</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if quick action buttons are now shown to the caller next to a file name", typeof(bool))]
    [HttpPut("showquickactions")]
    public async Task<bool> ShowQuickActions(SettingsRequestDto inDto)
    {
        return await filesSettingsHelper.SetShowQuickActions(inDto.Set);
    }

    /// <remarks>
    /// Changes nothing and always returns `false`. The call is kept for compatibility and ignores the body. Any
    /// signed-in user can call it. You can repeat it. The call does not make an upload update a file with the same
    /// name. The parameters of the upload call decide what happens on a name clash. To add a version to a file, use
    /// the update calls under `api/2.0/files/file/{fileId}`.
    /// </remarks>
    /// <summary>Update a file version if it exists</summary>
    /// <path>api/2.0/files/updateifexist</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "Always false: an upload does not update an existing file by name", typeof(bool))]
    [HttpPut("updateifexist")]
    public Task<bool> UpdateFileIfExist(SettingsRequestDto inDto)
    {
        return Task.FromResult(false);
    }

    /// <remarks>
    /// Returns the trash auto-clearing setting of the calling account. Any signed-in user can call it. If the account
    /// has no setting yet, the call stores the default and returns it. The default turns clearing on with an interval
    /// of thirty days. After this, the call is read-only. Each entry in the trash shows in its `autoDelete` field
    /// when the portal deletes it. To change the setting, use `PUT api/2.0/files/settings/autocleanup`.
    /// </remarks>
    /// <summary>Get the trash bin auto-clearing setting</summary>
    /// <path>api/2.0/files/settings/autocleanup</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The trash auto-clearing setting of the caller: the on/off flag and the interval", typeof(AutoCleanUpDataDto))]
    [HttpGet("settings/autocleanup")]
    public async Task<AutoCleanUpDataDto> GetAutomaticallyCleanUp()
    {
        return (await filesSettingsHelper.GetAutomaticallyCleanUp())?.Map();
    }

    /// <remarks>
    /// Changes the trash auto-clearing setting of the calling account. It returns the setting that is now stored. Any
    /// signed-in user can call it. The call changes data, and you can repeat it with the same body. The call writes
    /// `set` and `gap` together. If you omit `gap`, the call stores 0, which is not a valid interval. So send `gap`
    /// even when `set` is false. While clearing is on, the portal deletes an item after it stays in the trash longer
    /// than `gap`. This deletion cannot be undone. Turning clearing off keeps the items in the trash.
    /// </remarks>
    /// <summary>Update the trash bin auto-clearing setting</summary>
    /// <path>api/2.0/files/settings/autocleanup</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The trash auto-clearing setting that is now stored for the caller", typeof(AutoCleanUpDataDto))]
    [HttpPut("settings/autocleanup")]
    public async Task<AutoCleanUpDataDto> ChangeAutomaticallyCleanUp(AutoCleanupRequestDto inDto)
    {
        await filesSettingsHelper.SetAutomaticallyCleanUp(new AutoCleanUpData { IsAutoCleanUp = inDto.Set, Gap = inDto.Gap });
        return (await filesSettingsHelper.GetAutomaticallyCleanUp())?.Map();
    }

    /// <remarks>
    /// Changes the access rights that the sharing dialog offers the calling account by default. The body is an array
    /// of access rights, not an object. Any signed-in user can call it. The call changes data, and you can repeat it
    /// with the same body. The portal keeps `FillForms`, `CustomFilter`, and `Review` from the array. Then it adds
    /// `ReadWrite` or `Comment`, the first one the array has. If the array has neither, it adds `Read`. So the
    /// response can be shorter than the request and have another order. An empty array clears the setting, and the
    /// call then returns `Read` alone. If a value is not an access right, the call returns 400. Existing shares do
    /// not change.
    /// </remarks>
    /// <summary>Change the default access rights</summary>
    /// <path>api/2.0/files/settings/dafaultaccessrights</path>
    /// <collection>list</collection>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The normalised set of default access rights stored for the caller", typeof(List<FileShare>))]
    [SwaggerResponse(400, "The request body cannot be read or holds a number outside the published list of access rights")]
    [HttpPut("settings/dafaultaccessrights")]
    public async Task<List<FileShare>> ChangeDefaultAccessRights(DefaultAccessRightsRequestDto inDto)
    {
        await filesSettingsHelper.SetDefaultSharingAccessRights(inDto.Value);
        return await filesSettingsHelper.GetDefaultSharingAccessRights();
    }

    /// <remarks>
    /// Changes whether documents open in the current browser tab for the calling account. It returns the value that
    /// is now stored. Any signed-in user can call it. The call changes data, and you can repeat it with the same
    /// body. The setting only guides the app. The editor URLs that the API returns are the same either way. To read
    /// the value, use the `openEditorInSameTab` field of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Open document in the same browser tab</summary>
    /// <path>api/2.0/files/settings/openeditorinsametab</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if documents are opened in the current browser tab", typeof(bool))]
    [HttpPut("settings/openeditorinsametab")]
    public async Task<bool> SetOpenEditorInSameTab(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetOpenEditorInSameTabAsync(inDto.Set);
        return await filesSettingsHelper.GetOpenEditorInSameTabAsync();
    }

    /// <remarks>
    /// Returns the blank documents that the portal uses for new files, one entry per extension. The list has every
    /// extension of the built-in template set. Requires the EditPortalSettings permission, which the portal owner and
    /// DocSpace admins have. Other callers get 403. The call is read-only. If `selectedFile` is null, the portal uses
    /// its built-in blank. The other file fields are filled only for a custom blank. The list starts with `.docx`,
    /// `.xlsx`, `.pptx`, and `.pdf`, and the rest follow by extension. To set a portal file as a blank, use
    /// `PUT api/2.0/files/settings/defaulttemplate`. To upload one, use
    /// `POST api/2.0/files/settings/defaulttemplate`.
    /// </remarks>
    /// <summary>Get the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The blank document configured for each supported extension", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("settings/defaulttemplate")]
    public async Task<DefaultTemplateSettingsDto> GetDefaultTemplates()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await defaultTemplateSettingsHelper.GetSettingsAsync();
        return await defaultTemplateSettingsConverter.ConvertToDtoAsync(settings);
    }

    /// <remarks>
    /// Makes a copy of an existing file the blank for new files of one extension. It returns all blanks after the
    /// change. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other
    /// callers get 403. The call changes data. Later edits of the original file do not change the blank. The call
    /// deletes the previous custom blank of that extension. So a repeated call makes a new copy and deletes the
    /// previous one. If the extension is not in the built-in template set, the call changes nothing. If the caller
    /// cannot copy the file, the call returns 403. The call returns 400 if `selectedFile` is neither a number nor a
    /// string. It also returns 400 if the file extension is not exactly `fileExtension`, including case. If the file
    /// does not exist, the call returns 404.
    /// </remarks>
    /// <summary>Change the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The blank document configured for each supported extension after the change", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `selectedFile` or `fileExtension`, the file identifier is of an unsupported kind, or its extension is not the one requested")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or may not copy the selected file")]
    [SwaggerResponse(404, "The selected file does not exist")]
    [HttpPut("settings/defaulttemplate")]
    public async Task<DefaultTemplateSettingsDto> SetDefaultTemplate(DefaultTemplateSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await defaultTemplateSettingsHelper.SetTemplateAsync(inDto.FileExtension, inDto.SelectedFile);
        return await defaultTemplateSettingsConverter.ConvertToDtoAsync(settings);
    }

    /// <remarks>
    /// Deletes the custom blank of one extension. New files of that extension then start from the built-in blank. It
    /// returns all blanks after the reset. The extension comes in the request body. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call changes data. It
    /// deletes the stored copy of the blank, but not the file it was copied from. Files already created from the
    /// blank do not change. You can repeat it. Without a custom blank, the call changes nothing. It also changes
    /// nothing for an extension outside the built-in template set. If the body has no `fileExtension`, the call
    /// returns 400.
    /// </remarks>
    /// <summary>Reset the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The blank document configured for each supported extension after the reset", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `fileExtension`")]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpDelete("settings/defaulttemplate")]
    public async Task<DefaultTemplateSettingsDto> ResetDefaultTemplate(DefaultTemplateSettingsResetRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await defaultTemplateSettingsHelper.SetTemplateAsync(inDto.FileExtension, null);
        return await defaultTemplateSettingsConverter.ConvertToDtoAsync(settings);
    }

    /// <remarks>
    /// Uploads a file and makes it the blank for new files of one extension. It returns all blanks after the upload.
    /// Send the file as multipart form data and the extension in the `fileExtension` query parameter. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. The
    /// call changes data and deletes the previous custom blank of that extension. So a repeated call stores a new
    /// copy and deletes the previous one. If the extension is not in the built-in template set, the call changes
    /// nothing. The call returns 400 if the file is missing, is larger than 100 MB, or has another extension. The
    /// extension comparison is case-sensitive. If a PDF is not a fillable form, the call returns 403. To use a file
    /// that is already in the portal, use `PUT api/2.0/files/settings/defaulttemplate`.
    /// </remarks>
    /// <summary>Upload a file as the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The blank document configured for each supported extension after the upload", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(400, "The uploaded file is missing or larger than the 100 MB limit, or its file name does not end with the requested extension")]
    [SwaggerResponse(403, "The caller has no portal-settings right, or the uploaded PDF is not a fillable form")]
    [HttpPost("settings/defaulttemplate")]
    // Kestrel's global 100 MB limit aborts the connection mid-upload, so the caller would see a
    // reset instead of a reason. The form limit below takes over: the multipart reader rejects the
    // oversized section while it streams in, and [FormLimitError] turns that into a readable 400.
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxDefaultTemplateSize)]
    [FormLimitError]
    public async Task<DefaultTemplateSettingsDto> UploadDefaultTemplate(DefaultTemplateSettingsUploadRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await defaultTemplateSettingsHelper.SetTemplateAsync(inDto.FileExtension, inDto.File.FileName, inDto.File.OpenReadStream());
        return await defaultTemplateSettingsConverter.ConvertToDtoAsync(settings);
    }

    /// <summary>Matches the Kestrel-wide body limit the [DisableRequestSizeLimit] above bypasses.</summary>
    private const long MaxDefaultTemplateSize = 100 * 1024 * 1024;

    /// <remarks>
    /// Changes whether the calling account sees rooms arranged by room groups. It returns the value that is now
    /// stored. Any signed-in user can call it. The call changes data, and you can repeat it with the same body. The
    /// setting only guides the app. The room groups under `api/2.0/files/group` and the list of
    /// `GET api/2.0/files/rooms` stay the same either way. To read the value, use the `organizeRoomsGrouping` field
    /// of `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Organize rooms grouping</summary>
    /// <path>api/2.0/files/settings/organizegrouping</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if the caller sees rooms arranged by room groups", typeof(bool))]
    [HttpPut("settings/organizegrouping")]
    public async Task<bool> SetOrganizeRoomsGrouping(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetOrganizeRoomsGroupingAsync(inDto.Set);
        return await filesSettingsHelper.GetOrganizeRoomsGroupingAsync();
    }
}
