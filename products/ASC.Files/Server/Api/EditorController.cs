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

using ASC.Files.ApiModels.ResponseDto;

using EditorToolCallStateMapper = ASC.Files.ApiModels.ResponseDto.EditorToolCallStateMapper;

namespace ASC.Files.Api;

[ConstraintRoute("int")]
[ApiEndpoint(Template = "file")]
public class EditorControllerInternal(
    FileStorageService fileStorageService,
    DocumentServiceHelper documentServiceHelper,
    EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper,
    EntryManager entryManager,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ConfigurationConverter<int> configurationConverter,
    SecurityContext securityContext,
    IHttpContextAccessor httpContextAccessor,
    EditorToolCallStateStore editorToolCallStateStore,
    DocumentServiceLicense documentServiceLicense)
    : EditorController<int>(
        fileStorageService,
        documentServiceHelper,
        encryptionKeyPairDtoHelper,
        entryManager,
        folderDtoHelper,
        fileDtoHelper,
        configurationConverter,
        securityContext,
        httpContextAccessor,
        editorToolCallStateStore,
        documentServiceLicense);

[ApiEndpoint(Template = "file")]
public class EditorControllerThirdparty(
    FileStorageService fileStorageService,
    DocumentServiceHelper documentServiceHelper,
    EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper,
    EntryManager entryManager,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ConfigurationConverter<string> configurationConverter,
    SecurityContext securityContext,
    IHttpContextAccessor httpContextAccessor,
    EditorToolCallStateStore editorToolCallStateStore,
    DocumentServiceLicense documentServiceLicense)
    : EditorController<string>(
        fileStorageService,
        documentServiceHelper,
        encryptionKeyPairDtoHelper,
        entryManager,
        folderDtoHelper,
        fileDtoHelper,
        configurationConverter,
        securityContext,
        httpContextAccessor,
        editorToolCallStateStore,
        documentServiceLicense);

public abstract class EditorController<T>(
    FileStorageService fileStorageService,
        DocumentServiceHelper documentServiceHelper,
        EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper,
        EntryManager entryManager,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ConfigurationConverter<T> configurationConverter,
        SecurityContext securityContext,
        IHttpContextAccessor httpContextAccessor,
        EditorToolCallStateStore editorToolCallStateStore,
        DocumentServiceLicense documentServiceLicense)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{

    /// <remarks>
    /// Replaces the content of a file with an edited copy and returns the file. Send the content as the `File` part
    /// of a `multipart/form-data` body. Without that part, the call saves the raw request body. An empty body then
    /// empties the file. The call does not download content from `downloadUri`. It reads only the extension from it,
    /// when `fileExtension` is empty. If `fileExtension` differs from the stored format, the portal converts the
    /// content. If a third-party storage cannot convert it, the portal saves a renamed copy. Requires editing access
    /// to the file. Other callers get 403. The call changes data. Do not repeat it. Each call adds a version. If the
    /// last save of the file was an autosave, the call replaces that version instead. With `forcesave` true, the call
    /// saves an autosave and keeps the editing session open. Without it, the call closes a session opened with
    /// `editingAlone` true. The call returns 403 if the file is locked by another user, is in Trash, or is encrypted.
    /// It also returns 403 for a file in a private room. Without `forcesave`, it also returns 403 while the file is
    /// open for co-editing. If the file does not exist, the call returns 404. If a string `fileId` points to a storage
    /// account that is not connected, the call returns 400. If the content exceeds the storage quota, it returns 402.
    /// For large content, use `POST api/2.0/files/file/{fileId}/edit_session`.
    /// </remarks>
    /// <summary>
    /// Save edited file content
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/saveediting</path>
    [RaisesWebhook(WebhookTrigger.FileUpdated, When = "for the saved file, also for a renamed copy in third-party storage, never `file.created`")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file is saved and the stored version is returned", typeof(FileDto<int>))]
    [SwaggerResponse(400, "A form field has the wrong type, or a third-party file identifier refers to a storage account that is not connected")]
    [SwaggerResponse(402, "The content does not fit into the portal's storage quota, even with the overshoot allowed for editor saves")]
    [SwaggerResponse(403, "The caller cannot edit the file, or it is locked, in Trash, or open in somebody else's editing session")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpPut("{fileId}/saveediting")]
    public async Task<FileDto<T>> SaveEditingFileFromForm(SaveEditingRequestDto<T> inDto)
    {
        var stream = inDto.File?.OpenReadStream() ?? httpContextAccessor.HttpContext?.Request.Body;
        return await _fileDtoHelper.GetAsync(await fileStorageService.SaveEditingAsync(inDto.FileId, inDto.FileExtension, inDto.DownloadUri, stream, inDto.Forcesave));
    }

    /// <remarks>
    /// Opens an editing session on the file and returns its document key. An editor app passes this key to the document
    /// service to join co-editing of this revision. While the session lasts, nobody can delete or move the file. With
    /// `editingAlone` false, the portal asks the document service to track the document. The caller then needs the
    /// right to edit, review, comment, fill forms, or edit filters. With `editingAlone` true, the caller claims the
    /// file alone. If anybody already edits the file, the call returns 403. Requires editing access to the file. Other
    /// callers get 403. With `editingAlone` true, an anonymous caller without editing access gets 401. The call changes
    /// data, but not the document. While the session lasts, a repeated call with `editingAlone` true returns 403. That
    /// call also deletes the portal's record of every editing session of the file. With `editingAlone` false, each call
    /// returns the same key until the file gets a new version. A version from an autosave keeps the key. The call also
    /// returns 403 if the file is locked or in Trash, or if the document service rejects tracking. If the file does not
    /// exist, it returns 404. With `editingAlone` false, a format that the editors cannot open returns 415. To keep the
    /// session alive, call `GET api/2.0/files/file/{fileId}/trackeditfile`. To end it, call that operation with
    /// `isFinish` true.
    /// </remarks>
    /// <summary>
    /// Open an editing session
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/startedit</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The document key of the editing session", typeof(string))]
    [SwaggerResponse(401, "An anonymous caller who may not edit the file claims the session with `editingAlone=true`")]
    [SwaggerResponse(403, "The caller cannot edit the file, the file is locked or in Trash, somebody is already editing it and the session was claimed alone, or the document service did not accept the tracking request")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [SwaggerResponse(415, "The file is in a format the editors can neither edit nor open for viewing")]
    [AllowAnonymous]
    [HttpPost("{fileId}/startedit")]
    public async Task<string> StartEditFile(StartEditRequestDto<T> inDto)
    {
        return await fileStorageService.StartEditAsync(inDto.FileId, inDto.File.EditingAlone);
    }

    /// <remarks>
    /// Starts filling of a PDF form in a form-filling room and returns the form. The portal saves the room, the
    /// title, the starting account, and the original form id on the file. The portal then collects later submissions
    /// for this form. For any other file, the call changes nothing and returns the file. A caller who cannot read the
    /// file gets 403, whatever the file is. Requires a role in the room other than `FillForms`, or the right to edit
    /// the room. Other callers get 403. The call changes data, and you can repeat it. A repeated call makes the caller
    /// the starting account and takes the title from the current file name. The call also returns 403 for an encrypted
    /// file. If the file does not exist, the call returns 404. To build a spreadsheet of the submitted answers, use
    /// `POST api/2.0/files/file/{fileId}/xlsx`.
    /// </remarks>
    /// <summary>
    /// Start filling a form
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/startfilling</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The form file, with the filling properties now stored on it", typeof(FileDto<int>))]
    [SwaggerResponse(403, "The caller cannot read the file, or holds only form-filling access on the room, or no access to it at all")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpPut("{fileId}/startfilling")]
    public async Task<FileDto<T>> StartFillingFile(StartFillingRequestDto<T> inDto)
    {
        var file = await fileStorageService.StartFillingAsync(inDto.FileId);

        return await _fileDtoHelper.GetAsync(file);
    }

    /// <remarks>
    /// Keeps an editing session on the file alive, or ends it. An editor app calls it repeatedly while a document is
    /// open. Pass in `docKeyForTrack` the key that `POST api/2.0/files/file/{fileId}/startedit` returned. If the key
    /// matches neither the current revision nor the edited one, the call returns 403. The call tracks each `tabId`
    /// separately, so several tabs and users can edit one file. With `isFinish` false, the call refreshes the entry,
    /// and the file stays marked as edited. With `isFinish` true, the call deletes the entry of this tab. A refresh
    /// requires the right to edit, review, comment, fill forms, or edit filters. Other callers get 403. An
    /// anonymous caller needs an external link. Without a link, it gets 401. A refresh through a link that does not
    /// allow editing also returns 401. The call changes data of the session, not the document. You can repeat it. On
    /// success, `key` is true and `value` is empty. A failure returns an error code, not a false `key`. If the file
    /// does not exist, the call returns 404. A refresh of a file that another user locked returns 403.
    /// </remarks>
    /// <summary>
    /// Track an editing session
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/trackeditfile</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The session was refreshed or closed", typeof(ItemKeyValuePair<bool, string>))]
    [SwaggerResponse(401, "An anonymous caller has no external link, or refreshes the session through a link that does not grant editing")]
    [SwaggerResponse(403, "The document key does not match the revision being edited, the caller has none of the editing rights on the file, or the session is refreshed while the file is locked by somebody else")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [AllowAnonymous]
    [HttpGet("{fileId}/trackeditfile")]
    public async Task<ItemKeyValuePair<bool, string>> TrackEditFile(TrackEditFileRequestDto<T> inDto)
    {
        var result = await fileStorageService.TrackEditFileAsync(inDto.FileId, inDto.TabId, inDto.DocKeyForTrack, inDto.IsFinish);

        return new ItemKeyValuePair<bool, string> { Key = result.Key, Value = result.Value };
    }

    /// <remarks>
    /// Returns the editor configuration of the file. It holds the document data, the editor mode, the caller's
    /// permissions, the callback URL, and the signature token. To open a past version, pass `version`. Without access
    /// to the file history, the call then returns 403. For a PDF form, the room type sets the mode and can overrule
    /// the request. A form from the templates folder opens read-only. Outside the mobile layout, it also opens
    /// embedded. In a form-filling room, `edit` true stops the filling of the form for a caller who can edit it. If a
    /// storage quota is exceeded, the configuration is read-only and names the exceeded scope. In a private room, the
    /// configuration includes the encryption keys of the caller. Requires read access to the file through an
    /// account or an external link. Other callers get 403. The call works without payment. The call is read-only,
    /// except when it stops form filling. The call also returns 403 if the file is in Trash. If the file or version does
    /// not exist, it returns 404. If the editors cannot open the format, it returns 415.
    /// </remarks>
    /// <summary>
    /// Get the editor configuration
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/openedit</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [RaisesWebhook(WebhookTrigger.FormStopped, When = "when `edit` true stops the filling of a form in a form-filling room")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The editor configuration for the requested file and mode", typeof(ConfigurationDto<int>))]
    [SwaggerResponse(403, "The caller cannot read the file, asked for a past version without access to the file history, or the file is in Trash")]
    [SwaggerResponse(404, "The file id, or the requested version of it, resolves to nothing")]
    [SwaggerResponse(415, "The file is in a format the editors can neither edit nor open for viewing, or it is an encrypted file of a private room and the connected document server cannot work with encrypted files")]
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("{fileId}/openedit")]
    public async Task<ConfigurationDto<T>> OpenEditFile(OpenEditRequestDto<T> inDto)
    {
        var (file, lastVersion) = await documentServiceHelper.GetCurFileInfoAsync(inDto.FileId, inDto.Version);
        FormOpenSetup<T> formOpenSetup = null;

        var rootFolder = await documentServiceHelper.GetRootFolderAsync(file);
        if (file.IsPdf && rootFolder.RootFolderType != FolderType.RoomTemplates)
        {
            formOpenSetup = rootFolder.FolderType switch
            {
                FolderType.FillingFormsRoom => await documentServiceHelper.GetFormOpenSetupForFillingRoomAsync(file, rootFolder, inDto.EditorType, inDto.Edit, entryManager),
                FolderType.FormFillingFolderInProgress => documentServiceHelper.GetFormOpenSetupForFolderInProgress(file, inDto.EditorType),
                FolderType.FormFillingFolderDone => documentServiceHelper.GetFormOpenSetupForFolderDone<T>(inDto.EditorType),
                FolderType.VirtualDataRoom => await documentServiceHelper.GetFormOpenSetupForVirtualDataRoomAsync(file, rootFolder, inDto.EditorType),
                FolderType.PublicRoom => await documentServiceHelper.GetFormOpenSetupForPublicRoomAsync(file, inDto.EditorType),
                FolderType.USER => await documentServiceHelper.GetFormOpenSetupForUserFolderAsync(file, inDto.EditorType, inDto.Edit, inDto.Fill),
                FolderType.DefaultTemplates => new FormOpenSetup<T>
                {
                    CanEdit = false,
                    CanFill = false,
                    CanStartFilling = false,
                    EditorType = inDto.EditorType != EditorType.Mobile
                        ? EditorType.Embedded
                        : inDto.EditorType
                },
                _ => new FormOpenSetup<T>
                {
                    CanEdit = !inDto.Fill,
                    CanFill = inDto.Fill
                }
            };

            formOpenSetup.RootFolder = rootFolder;

            // the editor re-requests the configuration in its own mode ("edit" even while filling), so only a
            // caller who may edit the form takes it off filling
            if (inDto.Edit && formOpenSetup.CanEdit && rootFolder.FolderType == FolderType.FillingFormsRoom)
            {
                await fileStorageService.ManageFormFilling(file.Id, FormFillingManageAction.Edit);
            }
        }
        var quotaExceededScope = await documentServiceHelper.CheckCustomQuotaAsync(rootFolder);

        var canEdit =
            quotaExceededScope == null &&
            (formOpenSetup?.CanEdit ?? !file.IsCompletedForm);

        var docParams = await documentServiceHelper.GetParamsAsync(
            formOpenSetup is { Draft: not null } ? formOpenSetup.Draft : file,
            lastVersion,
            canEdit,
            !inDto.View,
            true, formOpenSetup == null || formOpenSetup.CanFill,
            formOpenSetup?.EditorType ?? inDto.EditorType,
            formOpenSetup is { IsSubmitOnly: true });

        var configuration = docParams.Configuration;
        file = docParams.File;

        if (docParams.LocatedInPrivateRoom)
        {
            configuration.EditorConfig.EncryptionKeys = await encryptionKeyPairDtoHelper.GetKeyPairAsync();
        }

        if (!string.IsNullOrEmpty(formOpenSetup?.FillingSessionId))
        {
            file.FormInfo = new FormInfo<T> { FillingSessionId = formOpenSetup.FillingSessionId };
        }

        var result = await configurationConverter.Convert(configuration, file);
        if (quotaExceededScope != null)
        {
            result.QuotaExceededScope = quotaExceededScope;
        }
        if (formOpenSetup is { DisableEmbeddedConfig: true } && result.EditorConfig.Embedded != null)
        {
            result.EditorConfig.Embedded.EmbedUrl = "";
            result.EditorConfig.Embedded.ShareUrl = "";
            result.EditorConfig.Customization.Goback = await configuration.EditorConfig?.Customization.GetGoBack(inDto.EditorType, file);
        }

        if (formOpenSetup != null)
        {
            // the editor needs an explicit null in user.roles to let the form be filled without role restrictions
            result.EditorConfig.User ??= new UserConfigDto();

            if (formOpenSetup.RootFolder.FolderType is FolderType.VirtualDataRoom)
            {
                result.StartFilling = file.Security[FileSecurity.FilesSecurityActions.StartFilling];
                result.StartFillingMode = StartFillingMode.StartFilling;
                result.Document.ReferenceData.RoomId = formOpenSetup.RootFolder.Id.ToString();
                result.Document.ReferenceData.CanEditRoom = formOpenSetup.CanEditRoom;

                result.EditorConfig.Customization.StartFillingForm = new StartFillingForm { Text = FilesCommonResource.StartFillingModeEnum_StartFilling };
                if (!string.IsNullOrEmpty(formOpenSetup.RoleName))
                {
                    result.EditorConfig.User.Roles = [formOpenSetup.RoleName];
                    result.FillingStatus = true;
                }
                if (!formOpenSetup.HasRole)
                {
                    result.EditorConfig.Customization.SubmitForm.Visible = false;
                }
            }
            else
            {
                if (result.Document.Permissions.Copy && !securityContext.CurrentAccount.ID.Equals(ASC.Core.Configuration.Constants.Guest.ID))
                {
                    result.StartFillingMode = rootFolder.FolderType == FolderType.FillingFormsRoom ? StartFillingMode.StartFillingFormRoom : StartFillingMode.ShareToFillOut;
                    result.StartFilling = formOpenSetup.CanStartFilling;
                    result.EditorConfig.Customization.StartFillingForm = new StartFillingForm { Text = rootFolder.FolderType == FolderType.FillingFormsRoom ? FilesCommonResource.StartFillingModeEnum_StartFilling : FilesCommonResource.StartFillingModeEnum_ShareToFillOut };
                }
            }

            // The same right PUT fieldchanges demands, so the editor does not send reports that are refused.
            if (rootFolder.FolderType == FolderType.FillingFormsRoom && canEdit && !inDto.View)
            {
                result.TrackFormFieldChanges = await fileStorageService.CanReportFormFieldChangesAsync(file) && !await documentServiceLicense.IsFreeEditionAsync();
            }

        }

        if (!string.IsNullOrEmpty(formOpenSetup?.FillingSessionId))
        {
            result.FillingSessionId = formOpenSetup.FillingSessionId;
        }

        if (rootFolder.RootFolderType == FolderType.RoomTemplates)
        {
            result.File.CanShare = false;
        }

        if (rootFolder.FolderType is FolderType.ChatOutputs && file.Id is int fileId)
        {
            var toolCallState = await editorToolCallStateStore.GetAsync(fileId);
            if (toolCallState is not null)
            {
                result.GenerationToolCallState = EditorToolCallStateMapper.MapToDto(toolCallState);
            }
        }

        return result;
    }

    /// <remarks>
    /// Returns a direct download URL of the current file content and a signature token for the document service. The
    /// portal rewrites the URL to the host that the document service can reach. So a browser may not reach this URL.
    /// The response also has the file extension, with the leading dot. Requires read access to the file. Other
    /// callers get 403. The call is read-only. Each call creates a new URL and token. Request them again after a
    /// token expires. If the file does not exist, the call returns 404. To get a link for a person, use
    /// `GET api/2.0/files/file/{fileId}/presigneduri`.
    /// </remarks>
    /// <summary>
    /// Get a signed download address
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/presigned</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The download address of the file with its signature token", typeof(FileLinkDto))]
    [SwaggerResponse(403, "The caller cannot read the file")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpGet("{fileId}/presigned")]
    public async Task<FileLinkDto> GetPresignedFileUri(FileIdRequestDto<T> inDto)
    {
        return await fileStorageService.GetPresignedUriAsync(inDto.FileId);
    }

    /// <remarks>
    /// Returns the portal members who can read the file, for mentions in the editor. The list has the readers of the
    /// file, the portal owner, the DocSpace admins, and the file author. It excludes the caller, external links, and
    /// deactivated accounts. The list is sorted by display name. A guest gets only the file owner. Requires read
    /// access to the file. Other callers get 403. The call is read-only. A caller who opened the file through an
    /// external link without an account gets an empty response. If the file does not exist, the call returns 404. To
    /// get the users for document protection, use `GET api/2.0/files/file/{fileId}/protectusers`.
    /// </remarks>
    /// <summary>
    /// Get users to mention in a file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/sharedusers</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The portal members who can read the file, ordered by display name", typeof(List<MentionDto>))]
    [SwaggerResponse(403, "The caller cannot read the file")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpGet("{fileId}/sharedusers")]
    public async Task<List<MentionDto>> GetSharedUsers(FileIdRequestDto<T> inDto)
    {
        if (!securityContext.IsAuthenticated)
        {
            return null;
        }

        return (await fileStorageService.SharedUsersAsync(inDto.FileId))?.Select(r => r.Map()).ToList();
    }

    /// <remarks>
    /// Returns a list of users with their access rights to the file.
    /// </remarks>
    /// <summary>Get user access rights</summary>
    /// <path>api/2.0/files/infousers</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of users with their access rights to the file", typeof(List<MentionDto>))]
    [HttpPost("infousers")]
    public async Task<List<MentionDto>> GetInfoUsers(GetInfoUsersRequestDto inDto)
    {
        return (await fileStorageService.GetInfoUsersAsync(inDto.UserIds))?.Select(r => r.Map()).ToList();
    }

    /// <remarks>
    /// Resolves a reference from a formula in one spreadsheet to another document. It returns the data that the
    /// document service needs to read that document. The data has the title, download URL, file type, document key,
    /// editor link, and signature token. First, the call looks up `fileKey` as a file id in the portal that
    /// `instanceId` names. Then, it looks for `path` among the files in the folder of `sourceFileId`. Then, it reads
    /// the file id from `link`, including short links. The first match wins. The call does not resolve a link outside
    /// this portal and returns it as the URL. Requires read access to the referenced file. Other callers get 403. For a
    /// `path` lookup, the call also checks read access to the source file and its folder. It also returns 403
    /// for an encrypted file or a file in a private room. The call is read-only. If nothing resolves, the call
    /// returns 200 with `error` set and the other fields empty. So read `error` before you use other fields. The call
    /// returns 400 if the body has no `fileKey` or `instanceId`.
    /// </remarks>
    /// <summary>
    /// Resolve a spreadsheet reference
    /// </summary>
    /// <path>api/2.0/files/file/referencedata</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The reference descriptor, or the same object with the error text set when nothing resolved", typeof(FileReferenceDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `fileKey` or `instanceId`")]
    [SwaggerResponse(403, "The caller cannot read the source file, its folder or the referenced file")]
    [HttpPost("referencedata")]
    public async Task<FileReferenceDto> GetReferenceData(GetReferenceDataDto<T> inDto)
    {
        return await fileStorageService.GetReferenceDataAsync(inDto.FileKey, inDto.InstanceId, inDto.SourceFileId, inDto.Path, inDto.Link);
    }

    /// <remarks>
    /// Returns the users the file is shared with, to pick who can still edit a protected document. The list comes
    /// from the whole access list of the file, including access from the room. Groups expand into their members. The
    /// list excludes explicit denials, the caller, and deleted accounts. It is sorted by display name. For a file in
    /// the legacy project storage, the list is empty. Any signed-in user can call it, except a guest. A guest
    /// gets 403. An anonymous caller gets an empty response. The call is read-only. If the file does not exist, the
    /// call also returns 403. To get the readers to mention in the editor, use
    /// `GET api/2.0/files/file/{fileId}/sharedusers`.
    /// </remarks>
    /// <summary>
    /// Get users for document protection
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/protectusers</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The users the file is shared with, ordered by display name", typeof(List<MentionDto>))]
    [SwaggerResponse(403, "The caller is a guest, or the file id resolves to nothing")]
    [HttpGet("{fileId}/protectusers")]
    public async Task<List<MentionDto>> GetProtectedFileUsers(FileIdRequestDto<T> inDto)
    {
        return (await fileStorageService.ProtectUsersAsync(inDto.FileId))?.Select(r => r.Map()).ToList();
    }

    /// <remarks>
    /// Queues a spreadsheet with all answers to a PDF form in a form-filling room. It returns the
    /// task, the original form, and whether the report file is new. Pass the id of the original form, or of an XLSX
    /// or CSV result file in the Complete folder. For a result file, the portal finds the form behind it. Before this
    /// call, start filling the form with `PUT api/2.0/files/file/{fileId}/startfilling`. The form must still be in
    /// the room that started it. Requires a DocSpace admin, or the room role room manager or content creator. Other
    /// callers get 403. It also returns 403 if form filling has not started or the room is not a form-filling room.
    /// The call changes data and queues work. A repeated call during a run returns that run. After the run, a repeated
    /// call rebuilds the spreadsheet in the same file. If that file was deleted or moved, the call creates a new one.
    /// Then `isNewFile` is true. The spreadsheet is not ready when the response arrives. To track it, poll
    /// `GET api/2.0/files/file/{fileId}/xlsx` with the id of the original form. When the task completes, take the file
    /// from the task. If the file or the form behind a result file does not exist, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Generate a form answers report
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/xlsx</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The generation task, the original form and the new-file flag", typeof(XlsxReportResponseDto))]
    [SwaggerResponse(403, "The caller may not update the report, or the file is not a started form of a form-filling room")]
    [SwaggerResponse(404, "The file id, or the original form behind a result file, resolves to nothing")]
    [HttpPost("{fileId}/xlsx")]
    public async Task<XlsxReportResponseDto> GenerateXlsx(FileIdRequestDto<int> inDto)
    {
        var (task, form, isNewFile) = await fileStorageService.GenerateXlsxAsync(inDto.FileId);

        return new XlsxReportResponseDto
        {
            Form = await _fileDtoHelper.GetAsync(form),
            Task = DocumentBuilderTaskDto.Get(task),
            IsNewFile = isNewFile
        };
    }

    /// <remarks>
    /// Returns the state of the form answers spreadsheet that `POST api/2.0/files/file/{fileId}/xlsx` queued. Pass
    /// the id of the original form, not of the spreadsheet. The portal keeps one run per portal, caller, and form. So
    /// the call returns only the own run of the caller. The response has the completion flag, the progress, and the
    /// error text of a failed run. Once the file exists, it also has its id, name, and URL. If no run exists for this
    /// caller and form, the response is empty. This is normal before the first run. Any signed-in user can call it.
    /// The portal checks access when it queues the run, not here. The call is read-only. This is the call to poll.
    /// Repeat it until `isCompleted` is true.
    /// </remarks>
    /// <summary>
    /// Get form report generation status
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/xlsx</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The state of the report generation task, or nothing when no run is on record", typeof(DocumentBuilderTaskDto))]
    [HttpGet("{fileId}/xlsx")]
    public async Task<DocumentBuilderTaskDto> GetXlsx(FileIdRequestDto<int> inDto)
    {
        var task = await fileStorageService.GetXlsxTaskAsync(inDto.FileId);

        return DocumentBuilderTaskDto.Get(task);
    }
}

public class EditorController(FilesLinkUtility filesLinkUtility,
        MessageService messageService,
        DocumentServiceConnector documentServiceConnector,
        CommonLinkUtility commonLinkUtility,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        CspSettingsHelper cspSettingsHelper,
        PermissionContext permissionContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Changes the ONLYOFFICE Docs connection settings of the portal and checks them on the running Document Server.
    /// An empty address deletes the portal's own value, and the deployment default applies again. The portal adds
    /// `http://` to an address without a scheme. Requires the EditPortalSettings permission, which the portal owner
    /// and DocSpace admins have. Other callers get 403. The call changes data, and you can repeat it with the same
    /// body. The check calls the editor api script, the healthcheck, the converter, the command service, and the
    /// document builder. If one fails, the portal restores all previous settings. Then the call fails. On success,
    /// it returns what `GET api/2.0/files/docservice` returns without `version`. The call returns 400 if an address
    /// cannot be parsed or is an absolute address with a query string. It also returns 400 for a secret without a
    /// header. It also returns 400 for an `http` public address on a portal served over `https`.
    /// </remarks>
    /// <summary>
    /// Set the document service address
    /// </summary>
    /// <path>api/2.0/files/docservice</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The settings are stored and the Document Server answered the verification requests", typeof(DocServiceUrlDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `docServiceUrl`, an address cannot be parsed or carries a query string, the signature secret is sent without its header, or an http address is given for a portal served over https")]
    [SwaggerResponse(403, "The caller is not the portal owner or a DocSpace administrator")]
    //[SwaggerResponse(503, "Unable to establish a connection with the Document Server")]
    [HttpPut("docservice")]
    public async Task<DocServiceUrlDto> CheckDocServiceUrl(CheckDocServiceUrlRequestDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var currentDocServiceUrl = filesLinkUtility.GetDocServiceUrl();
        var currentDocServiceUrlInternal = filesLinkUtility.GetDocServiceUrlInternal();
        var currentDocServicePortalUrl = filesLinkUtility.GetDocServicePortalUrl();
        var currentDocServiceSecretValue = await filesLinkUtility.GetDocServiceSignatureSecretAsync();
        var currentDocServiceSecretHeader = await filesLinkUtility.GetDocServiceSignatureHeaderAsync();
        var currentDocServiceSslVerification = await filesLinkUtility.GetDocServiceSslVerificationAsync();

        if (!ValidateUrl(inDto.DocServiceUrl) ||
            !ValidateUrl(inDto.DocServiceUrlInternal) ||
            !ValidateUrl(inDto.DocServiceUrlPortal))
        {
            throw new ArgumentException("Invalid input urls");
        }

        if (!string.IsNullOrEmpty(inDto.DocServiceSignatureSecret) &&
            string.IsNullOrEmpty(inDto.DocServiceSignatureHeader))
        {
            throw new ArgumentException("Invalid signature header");
        }

        var https = new Regex(@"^https://", RegexOptions.IgnoreCase);
        var http = new Regex(@"^http://", RegexOptions.IgnoreCase);

        try
        {
            await filesLinkUtility.SetDocServiceUrlAsync(inDto.DocServiceUrl);
            await filesLinkUtility.SetDocServiceUrlInternalAsync(inDto.DocServiceUrlInternal);
            await filesLinkUtility.SetDocServicePortalUrlAsync(inDto.DocServiceUrlPortal);
            await filesLinkUtility.SetDocServiceSignatureSecretAsync(inDto.DocServiceSignatureSecret);
            await filesLinkUtility.SetDocServiceSignatureHeaderAsync(inDto.DocServiceSignatureHeader);
            await filesLinkUtility.SetDocServiceSslVerificationAsync(inDto.DocServiceSslVerification ?? true);

            if (https.IsMatch(commonLinkUtility.GetFullAbsolutePath("")) && http.IsMatch(filesLinkUtility.GetDocServiceUrl()))
            {
                throw new ArgumentException("Mixed Active Content is not allowed. HTTPS address for Document Server is required.");
            }
        }
        catch
        {
            await RestoreSettingsAsync();
            throw;
        }

        try
        {
            await documentServiceConnector.CheckDocServiceUrlAsync();

            messageService.Send(MessageAction.DocumentServiceLocationSetting);

            var settings = await cspSettingsHelper.LoadAsync();

            _ = await cspSettingsHelper.SaveAsync(settings.Domains ?? []);
        }
        catch (Exception ex)
        {
            await RestoreSettingsAsync();
            throw new Exception("Unable to establish a connection with the Document Server.", ex);
        }

        var version = new DocServiceUrlRequestDto { Version = false };
        return await GetDocServiceUrl(version);

        async Task RestoreSettingsAsync()
        {
            await filesLinkUtility.SetDocServiceUrlAsync(currentDocServiceUrl);
            await filesLinkUtility.SetDocServiceUrlInternalAsync(currentDocServiceUrlInternal);
            await filesLinkUtility.SetDocServicePortalUrlAsync(currentDocServicePortalUrl);
            await filesLinkUtility.SetDocServiceSignatureSecretAsync(currentDocServiceSecretValue);
            await filesLinkUtility.SetDocServiceSignatureHeaderAsync(currentDocServiceSecretHeader);
            await filesLinkUtility.SetDocServiceSslVerificationAsync(currentDocServiceSslVerification);
        }

        bool ValidateUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return true;
            }

            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                return false;
            }

            return !(uri.IsAbsoluteUri && !string.IsNullOrEmpty(uri.Query));
        }
    }

    /// <remarks>
    /// Returns the ONLYOFFICE Docs connection settings of the portal and the editor script URLs. Anyone can call it
    /// without signing in, and every caller gets the same values. The call is read-only. With `version` true, the
    /// portal asks the Document Server for its editor version. Otherwise, `version` is empty and the call does not
    /// contact the Document Server. If the Document Server does not answer, `version` holds a fallback value instead
    /// of an error. So a version does not prove that the Document Server works. To change the settings, use
    /// `PUT api/2.0/files/docservice`.
    /// </remarks>
    /// <summary>
    /// Get the document service address
    /// </summary>
    /// <path>api/2.0/files/docservice</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The document service location, with the editor version filled in when it was requested", typeof(DocServiceUrlDto))]
    [AllowAnonymous]
    [HttpGet("docservice")]
    public async Task<DocServiceUrlDto> GetDocServiceUrl(DocServiceUrlRequestDto inDto)
    {
        var url = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.DocServiceApiUrl);
        var preloadUrl = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.DocServicePreloadUrl);

        var dsVersion = "";

        if (inDto.Version)
        {
            dsVersion = await documentServiceConnector.GetVersionAsync();
        }

        return new DocServiceUrlDto
        {
            Version = dsVersion,
            DocServiceUrlApi = url,
            DocServicePreloadUrl = preloadUrl,
            DocServiceUrl = filesLinkUtility.GetDocServiceUrl(),
            DocServiceUrlInternal = filesLinkUtility.GetDocServiceUrlInternal(),
            DocServicePortalUrl = filesLinkUtility.GetDocServicePortalUrl(),
            DocServiceSignatureHeader = await filesLinkUtility.GetDocServiceSignatureHeaderAsync(),
            DocServiceSslVerification = await filesLinkUtility.GetDocServiceSslVerificationAsync(),
            IsDefault = await filesLinkUtility.IsDefaultAsync()
        };
    }
}
