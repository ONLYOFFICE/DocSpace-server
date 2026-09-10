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
    /// Turns the portal-wide permission to connect third-party storages such as Google Drive, Dropbox or Nextcloud on
    /// or off, and returns the value that is now stored. Only the portal owner and a DocSpace administrator may
    /// change it: a room administrator, a member or a guest is refused, and so is an unauthenticated caller. This is
    /// a single setting for the whole portal rather than a preference of the caller, so it changes what every account
    /// sees. While it is off, connecting an account through `POST api/2.0/files/thirdparty` is refused and the
    /// contents of an already connected provider folder cannot be listed; the stored connections themselves survive
    /// and work again once it is turned back on. The providers this portal can offer are listed by
    /// `GET api/2.0/files/thirdparty/capabilities`. The same value is published as `enableThirdParty` by
    /// `GET api/2.0/files/settings`. Sending the same value again is safe. The response is the value read back from
    /// the portal, not a success flag.
    /// </remarks>
    /// <summary>Change the third-party settings access</summary>
    /// <path>api/2.0/files/thirdparty</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if third-party storages may be connected in this portal", typeof(bool))]
    [HttpPut("thirdparty")]
    public async Task<bool> ChangeAccessToThirdparty(SettingsRequestDto inDto)
    {
        await filesSettingsHelper.SetEnableThirdParty(inDto.Set);

        return await filesSettingsHelper.GetEnableThirdParty();
    }

    /// <remarks>
    /// Stores whether the caller wants to be asked for confirmation before files and folders are deleted, and returns
    /// the value that is now stored. The setting belongs to the calling account alone: every authenticated role down
    /// to a guest may change its own copy, one member's choice never affects another, and an unauthenticated caller
    /// is refused. It is a hint for the interface, not a server-side guard: the delete operations under
    /// `api/2.0/files/fileops` remove whatever they are given regardless of this value, so a client that skips its
    /// own prompt loses nothing but the prompt. Pass `set=true` to be asked again, `set=false` to delete without a
    /// prompt. The same value is published as `confirmDelete` by `GET api/2.0/files/settings`, which is the only way
    /// to read it back. Repeating the call with the same value writes it again and is safe. A new account starts with
    /// the confirmation switched on, and the value says nothing about where deleted items land: they go to the trash
    /// and are cleared from there according to `GET api/2.0/files/settings/autocleanup`.
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
    /// Selects the archive format the portal packs the caller's multi-item downloads into: `set=true` switches to
    /// `.tar.gz`, `set=false` back to `.zip`. The choice is stored for the calling account only, so every
    /// authenticated role down to a guest may set its own, while an unauthenticated caller is refused. It takes
    /// effect on the archives built by `PUT api/2.0/files/fileops/bulkdownload` and by the download links that
    /// operation returns; archives already produced keep the format they were packed with. The returned archive
    /// object carries no readable fields of its own, so it cannot be used to confirm the change: read `downloadTarGz`
    /// from `GET api/2.0/files/settings` instead. Writing the same value again is safe and changes nothing else. A
    /// new account starts on `.zip`. The format decides only how the archive is packed: which items go into it, and
    /// the access needed to take them, are decided by the bulk-download operation itself, and a single file is
    /// downloaded as it is whatever is stored here.
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
    /// Stores whether the "Recent" section is offered to the calling account, and returns the value that is now
    /// stored. The setting belongs to that account alone: every authenticated role down to a guest may change its own
    /// copy, and an unauthenticated caller is refused. Hiding the section removes it from the list of section roots
    /// returned by `GET api/2.0/files/@root`, and the document editor stops offering the recent-files entry; the
    /// section itself keeps being maintained, and `GET api/2.0/files/recent` still returns its contents. Pass
    /// `set=true` to show it again. The same value is published as `recentSection` by `GET api/2.0/files/settings`,
    /// which is the only way to read it back. Repeating the call with the same value writes it again and is safe. A
    /// new account starts with the section shown. Hiding it neither clears the recent history nor stops it being
    /// recorded, so showing the section again brings the same entries back.
    /// </remarks>
    /// <summary>Show the Recent section</summary>
    /// <path>api/2.0/files/displayrecent</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if the Recent section is offered to the caller", typeof(bool))]
    [SwaggerResponse(403, "The caller is not allowed to change this setting")]
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
    /// Turns external (public) links on or off for the whole portal and returns the value that is now stored. Only
    /// the portal owner and a DocSpace administrator may change it: a room administrator, a member or a guest is
    /// refused, and so is an unauthenticated caller. Turning it off also turns sharing on social networks off, so a
    /// following read of `externalShareSocialMedia` reports false without a separate call. This operation sets one
    /// flag; to write the whole external-sharing policy in one request - the default link type, the sections the
    /// restriction applies to and whether existing links are blocked at once - use
    /// `PUT api/2.0/files/settings/externalsharingsettings`. The value is published as `externalShare` by
    /// `GET api/2.0/files/settings`. Sending the same value again is safe. The response is the value read back from
    /// the portal rather than a success flag. External links are allowed in a new portal. Turning them off does not
    /// delete the links that already exist - whether those stop working at once is decided by the
    /// `blockExistingLinksOnRestrict` field of the settings operation named above.
    /// </remarks>
    /// <summary>Change the external sharing ability</summary>
    /// <path>api/2.0/files/settings/external</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if external links may be created in this portal", typeof(bool))]
    [HttpPut("settings/external")]
    public async Task<bool> ExternalShare(DisplayRequestDto inDto)
    {
        return await filesSettingsHelper.ChangeExternalShareSettingsAsync(inDto.Set);
    }

    /// <remarks>
    /// Writes the portal's whole external-sharing policy in one request and returns the set that is now in force.
    /// Only the portal owner and a DocSpace administrator may call it; everyone else is refused, including an
    /// unauthenticated caller. Every field of the request is applied, so send the complete set rather than the field
    /// being changed - an omitted boolean is read as false. The portal keeps the set consistent: with `externalShare`
    /// false the default link type is forced to "users of this portal only" and sharing on social networks is turned
    /// off, and the three restriction fields only matter while external sharing is off.
    /// `blockExistingLinksOnRestrict` decides what happens to links that already exist, so it is the field that
    /// changes access to data already shared. The new set is pushed to the connected clients of the portal as well,
    /// and is published field by field by `GET api/2.0/files/settings`. Sending the same set again is safe.
    /// </remarks>
    /// <summary>Configure external sharing</summary>
    /// <path>api/2.0/files/settings/externalsharingsettings</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The external sharing policy that is now in force", typeof(ExternalSharingSettingsDto))]
    [HttpPut("settings/externalsharingsettings")]
    public async Task<ExternalSharingSettingsDto> ChangeExternalSharingSettings(ExternalSharingSettingsRequestDto inDto)
    {
        return await filesSettingsHelper.ChangeExternalSharingSettingsAsync(inDto);
    }

    /// <remarks>
    /// Turns the social-network sharing buttons on or off for the whole portal and returns the value that is now in
    /// force. Only the portal owner and a DocSpace administrator may change it; a room administrator, a member or a
    /// guest is refused, and so is an unauthenticated caller. The requested value is combined with the state of
    /// external sharing itself: while that is off, enabling this setting has no effect and the response comes back
    /// false, so turn external sharing on with `PUT api/2.0/files/settings/external` first and only then this one.
    /// Turning external sharing off later switches this setting off again on its own. The value is published as
    /// `externalShareSocialMedia` by `GET api/2.0/files/settings`. Sending the same value again is safe. Read the
    /// response instead of assuming the requested value was stored. The setting governs the share-to-network buttons
    /// offered next to an external link; it neither creates nor revokes links, and the links themselves keep working
    /// either way.
    /// </remarks>
    /// <summary>Change the external sharing ability on social networks</summary>
    /// <path>api/2.0/files/settings/externalsocialmedia</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "true if sharing on social networks is now in force", typeof(bool))]
    [HttpPut("settings/externalsocialmedia")]
    public async Task<bool> ExternalShareSocialMedia(DisplayRequestDto inDto)
    {
        return await filesSettingsHelper.ChangeExternalShareSocialMediaSettingsAsync(inDto.Set);
    }

    /// <remarks>
    /// Reports that forcesaving is on for this portal. The operation is a stub kept for compatibility: it takes no
    /// request body, stores nothing and always answers true, so calling it neither turns forcesaving on nor off and
    /// repeating it changes nothing. Forcesaving itself - the editor writing the document back to storage while the
    /// session is still open - is on for every portal and cannot be switched off through the API. Any authenticated
    /// role down to a guest may call it; an unauthenticated caller is refused. The same constant is published as
    /// `forcesave` by `GET api/2.0/files/settings`, which is the cheaper way to read it together with the rest of the
    /// settings. A companion stub, `PUT api/2.0/files/storeforcesave`, answers for the storing of forcesaved versions
    /// in the same way. Nothing in this call reaches a document: to have the current state of an editing session
    /// written to storage, drive the document through the editor operations of the file itself rather than through
    /// this setting.
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
    /// Returns the whole Files configuration in one object: the caller's own preferences (trash auto-clearing,
    /// default sharing rights, hidden confirmation dialogs, archive format, section visibility), the portal-wide
    /// switches an administrator controls (third-party storages, external sharing), and the static tables a client
    /// needs to work with documents - which extensions can be viewed, edited, converted or uploaded, the URL
    /// templates for the viewer, editor and thumbnails, and the upload limits. This is the read side of the setting
    /// operations in this section: each of those answers with the one value it wrote, and only the trash
    /// auto-clearing and default-template settings have a GET of their own. Marked as allowing anonymous access
    /// because the external-link pages read the extension tables before signing in, but a caller with neither a
    /// session nor a valid link key is still rejected. The result is not filtered by role and is not paginated; fetch
    /// it once per session rather than before each file action.
    /// </remarks>
    /// <summary>Get file settings</summary>
    /// <path>api/2.0/files/settings</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The full set of file settings for the caller and the portal", typeof(FilesSettingsDto))]
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
    /// Returns the descriptor of the Documents module of this portal: its identifier, display title and description,
    /// the address of its start page, the icon and image addresses, the address of its help section, and whether it
    /// is the portal's primary module. It is meant for building navigation to the module, not for working with
    /// documents: nothing about files, rooms or permissions comes back, and nothing is changed by the call. The
    /// values follow the portal's own configuration and branding, so the title and the description arrive already
    /// translated for the caller. Any authenticated role down to a guest may read it; an unauthenticated caller is
    /// refused. The content is the same for everyone in the portal and changes only when the portal is reconfigured,
    /// so it can be fetched once and cached rather than requested per screen. Only the Documents module is described
    /// here; this document carries no listing of the other modules of the portal. The file-related configuration a
    /// client needs alongside it - the format tables, the editor addresses and the upload limits - comes from
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
    /// Stores whether the caller is asked to confirm cancelling a running file operation, and returns the value that
    /// is now stored. The setting belongs to the calling account alone: every authenticated role down to a guest may
    /// change its own copy, and an unauthenticated caller is refused. Unlike the conversion prompt of
    /// `PUT api/2.0/files/hideconfirmconvert`, this one works in both directions - `set=true` hides the confirmation,
    /// `set=false` brings it back. It is a hint for the interface only: cancelling an operation through the API is
    /// unaffected, and the operations themselves keep being reported by `GET api/2.0/files/fileops`. The value is
    /// published as `hideConfirmCancelOperation` by `GET api/2.0/files/settings`, which is the only way to read it
    /// back. Writing a value that is already stored is accepted and leaves the setting untouched. A new account
    /// starts with the confirmation shown. The prompt it hides is the one raised when a running copy, move or
    /// download is about to be abandoned, not the one raised before a deletion - that one is
    /// `PUT api/2.0/files/changedeleteconfrim`.
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
    /// Hides one of the two prompts the interface shows around file conversion, for the calling account only. The
    /// `save` field chooses which prompt, and is not the value being written: `save=true` hides the prompt that
    /// offers to keep a copy in the original format when a file is converted, `save=false` hides the prompt that
    /// offers to open the conversion result. Both flags are one-way - the operation can only hide a prompt, and there
    /// is no API to show it again - so the answer is always true and repeating the call changes nothing. The two
    /// flags are independent: hiding one leaves the other as it was. Every authenticated role down to a guest may set
    /// its own, and an unauthenticated caller is refused. The stored flags are published as `hideConfirmConvertSave`
    /// and `hideConfirmConvertOpen` by `GET api/2.0/files/settings`. Conversion itself is started by
    /// `PUT api/2.0/files/file/{fileId}/checkconversion` and is not affected by either flag.
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
    /// Stores whether the caller is warned before the lifetime settings of a room are changed, and returns the value
    /// that is now stored. A room lifetime moves the files of the room to the trash once they reach the configured
    /// age, which is why the interface confirms the change; this setting decides whether that confirmation is shown
    /// to the calling account. It belongs to that account alone: every authenticated role down to a guest may change
    /// its own copy, and an unauthenticated caller is refused. It works in both directions - `set=true` hides the
    /// warning, `set=false` brings it back - and is a hint for the interface only, so changing a room lifetime
    /// through `PUT api/2.0/files/rooms/{id}` is unaffected. The value is published as `hideConfirmRoomLifetime` by
    /// `GET api/2.0/files/settings`, which is the only way to read it back. A new account starts with the warning
    /// shown, and writing a value that is already stored is accepted and leaves the setting untouched. Hiding the
    /// warning does not shorten or extend any lifetime: what a room does with ageing files is decided by the room
    /// itself.
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
    /// Reports that forcesaved versions are not kept as separate file versions in this portal. The operation is a
    /// stub kept for compatibility: it takes no request body, stores nothing and always answers false, so it neither
    /// turns the behaviour on nor off and repeating it changes nothing. What it describes is what happens to the
    /// intermediate saves the editor makes while a document is still open - they update the current version instead
    /// of piling up as new ones in `GET api/2.0/files/file/{fileId}/history`. Any authenticated role down to a guest
    /// may call it; an unauthenticated caller is refused. The same constant is published as `storeForcesave` by
    /// `GET api/2.0/files/settings`, which is the cheaper way to read it. Its companion stub
    /// `PUT api/2.0/files/forcesave` answers for forcesaving itself in the same way. Version history is not affected
    /// by this call either: the versions a document really has are the ones the file history operation lists, and a
    /// new one appears when the editing session is closed.
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
    /// Stores whether the caller's uploads keep the original file when the portal converts them into an editable
    /// format, and returns the value that is now stored. With `set=true` the converted document is saved as a new
    /// file next to the upload, so both the original and the converted copy stay in the folder; with `set=false` the
    /// conversion replaces the uploaded file with a new version of it whenever the caller may edit that file. The
    /// setting belongs to the calling account alone: every authenticated role down to a guest may change its own
    /// copy, and an unauthenticated caller is refused. It applies to conversion on upload and to
    /// `PUT api/2.0/files/file/{fileId}/checkconversion`, not to files already stored. The value is published as
    /// `storeOriginalFiles` by `GET api/2.0/files/settings`, which is the only way to read it back. The change is
    /// recorded in the portal audit trail.
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
    /// Stores whether the caller wants new documents created with the default name instead of being asked for one,
    /// and returns the value that is now stored. It is a preference of the calling account: every authenticated role
    /// down to a guest may change its own copy, one member's choice never affects another, and an unauthenticated
    /// caller is refused. The portal only keeps the value and reports it - the creation operations,
    /// `POST api/2.0/files/{folderId}/file` among them, always use the title they are given, so this setting changes
    /// what an interface asks for rather than what the server does. Writing a value that is already stored is
    /// accepted and leaves the setting and the audit trail untouched. The value is published as `keepNewFileName` by
    /// `GET api/2.0/files/settings`, which is the only way to read it back. A new account starts with the prompt in
    /// place. The title a created document actually gets, and how a clash with an existing title is resolved, are
    /// decided by the creation request rather than here.
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
    /// Stores whether file titles are shown to the caller with their extension, and returns the value that is now
    /// stored. It is a preference of the calling account: every authenticated role down to a guest may change its own
    /// copy, and an unauthenticated caller is refused. Only the presentation changes - the titles kept by the portal
    /// always include the extension, and the listing and file operations keep returning them in full, so a client
    /// that trims the extension for display must add it back before it renames or searches for anything. Writing a
    /// value that is already stored is accepted and leaves the setting untouched. The value is published as
    /// `displayFileExtension` by `GET api/2.0/files/settings`, which is the only way to read it back. A new account
    /// starts with extensions hidden. This governs display alone: which extensions may be uploaded, viewed or edited
    /// at all is published by the same settings operation as separate format lists.
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
    /// Reports that uploading a file under a name that already exists does not update the existing file. The
    /// operation is a stub kept for compatibility: the request body is read but ignored, nothing is stored, and the
    /// answer is always false, so calling it changes no behaviour and repeating it changes nothing. What actually
    /// decides the outcome of a name clash is the parameter of the upload itself - see the `createNewIfExist` and
    /// conflict-resolution parameters of the operations under `api/2.0/files/{folderId}/upload` and of
    /// `PUT api/2.0/files/fileops/copy`. Any authenticated role down to a guest may call it; an unauthenticated
    /// caller is refused. Because the value is a constant, there is nothing to read back afterwards, and
    /// `GET api/2.0/files/settings` does not publish it. To add a version to a document that is already stored,
    /// address the file directly through the update operations under `api/2.0/files/file/{fileId}` instead of
    /// uploading under the same name and relying on this setting.
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
    /// Returns the trash auto-clearing setting of the calling account: whether it is on, and after which interval an
    /// item that sits in the trash is removed for good. The setting belongs to that account alone, so every
    /// authenticated role down to a guest reads its own value and an unauthenticated caller is refused. The first
    /// call for an account is not read-only: when nothing has been stored yet the portal writes the default -
    /// clearing on, thirty days - and returns it, so the answer never comes back empty and a following call reports
    /// the same pair. The interval is the age of an entry in the trash, not a schedule; each trashed entry also
    /// reports the moment it is due to disappear in its own `autoDelete` field. Use
    /// `PUT api/2.0/files/settings/autocleanup` to change the pair, or read it together with the rest of the
    /// configuration from `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Get the trash bin auto-clearing setting</summary>
    /// <path>api/2.0/files/settings/autocleanup</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The trash auto-clearing setting of the caller: the on/off flag and the interval", typeof(AutoCleanUpData))]
    [HttpGet("settings/autocleanup")]
    public async Task<AutoCleanUpData> GetAutomaticallyCleanUp()
    {
        return await filesSettingsHelper.GetAutomaticallyCleanUp();
    }

    /// <remarks>
    /// Writes the trash auto-clearing setting of the calling account and returns the pair that is now stored. Both
    /// fields are written together from the request, so a call that omits `gap` stores an interval outside the
    /// published list rather than keeping the previous one - always send the interval, including when `set` is false.
    /// While clearing is on, an item is removed from the caller's trash for good once it has been there longer than
    /// the interval, and each trashed entry reports the moment it is due to disappear in its own `autoDelete` field;
    /// switching clearing off stops that and leaves whatever is in the trash. The setting belongs to the calling
    /// account alone: every authenticated role down to a guest may change its own, one member's choice never affects
    /// another, and an unauthenticated caller is refused. Items already removed are not recoverable. Read the pair
    /// back with `GET api/2.0/files/settings/autocleanup`.
    /// </remarks>
    /// <summary>Update the trash bin auto-clearing setting</summary>
    /// <path>api/2.0/files/settings/autocleanup</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The trash auto-clearing setting that is now stored for the caller", typeof(AutoCleanUpData))]
    [HttpPut("settings/autocleanup")]
    public async Task<AutoCleanUpData> ChangeAutomaticallyCleanUp(AutoCleanupRequestDto inDto)
    {
        await filesSettingsHelper.SetAutomaticallyCleanUp(new AutoCleanUpData { IsAutoCleanUp = inDto.Set, Gap = inDto.Gap });
        return await filesSettingsHelper.GetAutomaticallyCleanUp();
    }

    /// <remarks>
    /// Stores the access rights the sharing dialog offers the calling account by default, and returns the set that
    /// was actually stored. The body is a bare array of access-right values, not an object. The portal normalises the
    /// array instead of keeping it as sent: it keeps the fill-forms, custom-filter and review entries, then adds
    /// read-and-write or comment - whichever is present, in that order - and stops there, and it falls back to read
    /// alone when nothing else applies, so the response can be shorter than the request and its order can differ. An
    /// empty array clears the setting, after which read alone is reported. A value outside the published list is
    /// rejected as an invalid request. The set belongs to the calling account alone: every authenticated role down to
    /// a guest may store its own, and an unauthenticated caller is refused. Nothing already shared is changed. The
    /// stored set is published as `defaultSharingAccessRights` by `GET api/2.0/files/settings`.
    /// </remarks>
    /// <summary>Change the default access rights</summary>
    /// <path>api/2.0/files/settings/dafaultaccessrights</path>
    /// <collection>list</collection>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The normalised set of default access rights stored for the caller", typeof(List<FileShare>))]
    [HttpPut("settings/dafaultaccessrights")]
    public async Task<List<FileShare>> ChangeDefaultAccessRights(DefaultAccessRightsrequestDto inDto)
    {
        await filesSettingsHelper.SetDefaultSharingAccessRights(inDto.Value);
        return await filesSettingsHelper.GetDefaultSharingAccessRights();
    }

    /// <remarks>
    /// Stores whether the caller wants documents opened in the current browser tab instead of a new one, and returns
    /// the value that is now stored. It is a preference of the calling account: every authenticated role down to a
    /// guest may change its own copy, and an unauthenticated caller is refused. The portal only keeps the value - the
    /// editor addresses returned by the file operations are the same either way, so this setting changes how a client
    /// opens them rather than what it receives. Writing a value that is already stored is accepted and leaves the
    /// setting untouched. The value is published as `openEditorInSameTab` by `GET api/2.0/files/settings`, which is
    /// the only way to read it back. A new account starts with documents opening in a new tab. Nothing about the
    /// document changes with it: the editing session, the access rights that apply and the addresses handed out are
    /// the same whichever tab a client uses.
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
    /// Returns the blank document the portal creates for each format: one entry per extension the built-in template
    /// set covers, with the file that has been chosen as the blank for it, if any. An entry whose `selectedFile` is
    /// null means no custom template has been set and the built-in blank is used; the remaining fields - title, size,
    /// modification moment and view address - are filled only for a custom one. The list is assembled from the
    /// portal's built-in template set on every call, so an extension the set no longer covers disappears from it.
    /// Entries come in the order the interface shows them: the text document, spreadsheet, presentation and PDF
    /// formats first, the rest by extension. Reading the setting requires the portal settings permission, so only the
    /// portal owner and a DocSpace administrator may call it. Use `PUT api/2.0/files/settings/defaulttemplate` to
    /// choose an existing file and the matching POST to upload one.
    /// </remarks>
    /// <summary>Get the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The blank document configured for each supported extension", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(403, "The caller may not read the portal settings")]
    [HttpGet("settings/defaulttemplate")]
    public async Task<DefaultTemplateSettingsDto> GetDefaultTemplates()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await defaultTemplateSettingsHelper.GetSettingsAsync();
        return await defaultTemplateSettingsConverter.ConvertToDtoAsync(settings);
    }

    /// <remarks>
    /// Makes an existing document the blank the portal creates for one extension, and returns the full set of
    /// templates as it now stands. The file is copied into the portal's template storage, so later edits of the
    /// original do not change the blank, and the file that served as the previous custom blank for that extension is
    /// deleted. `selectedFile` takes the identifier of a file the caller may copy - a number for a document stored in
    /// the portal, a string for one in a connected third-party storage - and its extension must be the one named in
    /// `fileExtension`; a mismatch or an identifier of another kind answers 400, a file the caller may not copy
    /// answers 403, and a file that is not there is answered as missing. An extension the built-in template set does
    /// not cover is not an error: the call succeeds and changes nothing, so compare the answer with what was asked
    /// for. Requires the portal settings permission.
    /// </remarks>
    /// <summary>Change the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The blank document configured for each supported extension after the change", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(400, "The file identifier is of an unsupported kind, or its extension is not the one requested")]
    [SwaggerResponse(403, "The caller may not read the portal settings, or may not copy the selected file")]
    [HttpPut("settings/defaulttemplate")]
    public async Task<DefaultTemplateSettingsDto> SetDefaultTemplate(DefaultTemplateSettingsRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await defaultTemplateSettingsHelper.SetTemplateAsync(inDto.FileExtension, inDto.SelectedFile);
        return await defaultTemplateSettingsConverter.ConvertToDtoAsync(settings);
    }

    /// <remarks>
    /// Drops the custom blank document configured for one extension and returns the full set of templates as it now
    /// stands. New documents of that extension are created from the portal's built-in blank again, and the file that
    /// served as the custom one is deleted from the template storage - the original the template was copied from is
    /// untouched. The extension is named in the request body, and the entry for it comes back with `selectedFile`
    /// null. Resetting an extension that has no custom blank is accepted and changes nothing, which makes a repeated
    /// call safe; an extension the built-in template set does not cover is ignored in the same way. Requires the
    /// portal settings permission, so only the portal owner and a DocSpace administrator may call it. To set a blank
    /// instead of dropping it, use `PUT api/2.0/files/settings/defaulttemplate`. Documents already created from the
    /// custom blank are left as they are - the reset only decides what the next new document of that extension starts
    /// from. The set as it stands can also be read with `GET api/2.0/files/settings/defaulttemplate`.
    /// </remarks>
    /// <summary>Reset the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The blank document configured for each supported extension after the reset", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(403, "The caller may not read the portal settings")]
    [HttpDelete("settings/defaulttemplate")]
    public async Task<DefaultTemplateSettingsDto> ResetDefaultTemplate(DefaultTemplateSettingsResetRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);
        var settings = await defaultTemplateSettingsHelper.SetTemplateAsync(inDto.FileExtension, null);
        return await defaultTemplateSettingsConverter.ConvertToDtoAsync(settings);
    }

    /// <remarks>
    /// Uploads a document and makes it the blank the portal creates for one extension, and returns the full set of
    /// templates as it now stands. The request is multipart form data carrying the file, while the extension travels
    /// in the `FileExtension` query parameter; the extension of the uploaded file name must be exactly that one, or
    /// the call answers 403. A PDF is additionally checked to be a fillable form, and answers 403 as well when it is
    /// not one. The upload is capped at 100 MB and a larger body answers 400 while it is still streaming in. The file
    /// is stored in the portal's template storage and the file that served as the previous custom blank for that
    /// extension is deleted; an extension the built-in template set does not cover leaves everything unchanged.
    /// Requires the portal settings permission. Use `PUT api/2.0/files/settings/defaulttemplate` to reuse a document
    /// that is already in the portal.
    /// </remarks>
    /// <summary>Upload a file as the default template setting</summary>
    /// <path>api/2.0/files/settings/defaulttemplate</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The blank document configured for each supported extension after the upload", typeof(DefaultTemplateSettingsDto))]
    [SwaggerResponse(400, "The uploaded file is missing or larger than the 100 MB limit")]
    [SwaggerResponse(403, "The caller may not read the portal settings, or the file does not match the requested extension")]
    [HttpPost("settings/defaulttemplate")]
    // Kestrel's global 100 MB limit aborts the connection mid-upload, so the caller would see a
    // reset instead of a reason. The form limit below takes over: the multipart reader rejects the
    // oversized section while it streams in, and the caller gets a readable 400.
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxDefaultTemplateSize)]
    public async Task<DefaultTemplateSettingsDto> UploadDefaultTemplate(DefaultTemplateSettingsUploadRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (inDto.File.Length > MaxDefaultTemplateSize)
        {
            throw new ArgumentException(FileSizeComment.GetFileSizeExceptionString(MaxDefaultTemplateSize));
        }

        var settings = await defaultTemplateSettingsHelper.SetTemplateAsync(inDto.FileExtension, inDto.File.FileName, inDto.File.OpenReadStream());
        return await defaultTemplateSettingsConverter.ConvertToDtoAsync(settings);
    }

    /// <summary>Matches the Kestrel-wide body limit the [DisableRequestSizeLimit] above bypasses.</summary>
    private const long MaxDefaultTemplateSize = 100 * 1024 * 1024;

    /// <remarks>
    /// Stores whether the caller sees rooms arranged by the groups they belong to instead of one flat list, and
    /// returns the value that is now stored. It is a preference of the calling account: every authenticated role down
    /// to a guest may change its own copy, and an unauthenticated caller is refused. The groups themselves are the
    /// room groups managed under `api/2.0/files/group`, and they exist whether or not this setting is on - the portal
    /// only records the preference, while `GET api/2.0/files/rooms` keeps returning the same rooms either way, so the
    /// arrangement is done by the client. Writing a value that is already stored is accepted and leaves the setting
    /// untouched. The value is published as `organizeRoomsGrouping` by `GET api/2.0/files/settings`, which is the
    /// only way to read it back. A new account starts with the grouping on. Turning it off changes no group: the
    /// groups, the rooms in them and who may see them stay exactly as they were, and are still read through the room
    /// group operations.
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
