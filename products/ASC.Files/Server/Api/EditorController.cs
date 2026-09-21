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
    EditorToolCallStateStore editorToolCallStateStore)
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
        editorToolCallStateStore);

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
    EditorToolCallStateStore editorToolCallStateStore)
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
        editorToolCallStateStore);

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
        EditorToolCallStateStore editorToolCallStateStore)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{

    /// <remarks>
    /// Replaces the content of an existing file with an edited copy and answers with the file as it now stands. The
    /// content is the `File` part of a `multipart/form-data` body, and when no such part is sent the raw request body
    /// is saved instead, so an empty body empties the file. The `DownloadUri` query parameter does not supply content
    /// here; it is only read for the extension when `FileExtension` is empty. `fileExtension` names the format of the
    /// content being sent, and when it differs from the stored format the portal converts the content, or keeps it
    /// under a renamed copy when a third-party storage cannot convert it. The caller needs edit access to the file.
    /// The call is mutating and not idempotent: an ordinary call adds a version to the file history, while
    /// `forcesave=true` records an editor autosave, which overwrites the previous autosave revision instead of adding
    /// another version and leaves a running editing session in place. It is refused with 403 when the file is locked,
    /// lies in Trash, or is open in an editing session started by somebody else, and an unknown file id is reported
    /// as missing. For content too large to post in one request use `POST api/2.0/files/file/{fileId}/edit_session`.
    /// </remarks>
    /// <summary>
    /// Save edited file content
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/saveediting</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file is saved and the stored version is returned", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The file id cannot be resolved to a storage that could accept the content")]
    [SwaggerResponse(403, "The caller cannot edit the file, or it is locked, in Trash, or open in somebody else's editing session")]
    [HttpPut("{fileId}/saveediting")]
    public async Task<FileDto<T>> SaveEditingFileFromForm(SaveEditingRequestDto<T> inDto)
    {
        var stream = inDto.File?.OpenReadStream() ?? httpContextAccessor.HttpContext?.Request.Body;
        return await _fileDtoHelper.GetAsync(await fileStorageService.SaveEditingAsync(inDto.FileId, inDto.FileExtension, inDto.DownloadUri, stream, inDto.Forcesave));
    }

    /// <remarks>
    /// Opens an editing session on the file and answers with the document key that identifies it, the value an editor
    /// client passes to the document service in order to join the co-editing session for that exact revision. The
    /// file is marked as being edited for as long as the session lasts, which keeps it from being deleted or moved.
    /// With `editingAlone=false` the portal builds the editor configuration, requires write mode plus at least one of
    /// the edit, review, comment, form-filling or filter permissions, and asks the document service to start tracking
    /// the document. With `editingAlone=true` the caller claims the file for itself, and the call is refused with 403
    /// when anybody is already editing it. The caller needs edit access: a member with read access, a guest and an
    /// anonymous caller whose external link does not grant editing are all refused. The call is mutating and not
    /// idempotent. Keep the session alive with `GET api/2.0/files/file/{fileId}/trackeditfile`, and end it by calling
    /// that operation with `isFinish=true`.
    /// </remarks>
    /// <summary>
    /// Open an editing session
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/startedit</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The document key of the editing session", typeof(string))]
    [SwaggerResponse(403, "The caller cannot edit the file, or the file is already being edited and the session was claimed alone")]
    [AllowAnonymous]
    [HttpPost("{fileId}/startedit")]
    public async Task<string> StartEditFile(StartEditRequestDto<T> inDto)
    {
        return await fileStorageService.StartEditAsync(inDto.FileId, inDto.File.EditingAlone);
    }

    /// <remarks>
    /// Marks a PDF form in a form-filling room as open for filling out and answers with the form file. The portal
    /// stores the filling properties on it - the room it belongs to, its title, the account that started it and the
    /// id it keeps as the original form - so that later submissions are collected against this form. The file has to
    /// be a PDF whose parent folder is a form-filling room; anything else is answered unchanged and nothing is
    /// stored. Access follows room membership rather than portal role: a member holding only form-filling access on
    /// the room may not start filling, and a caller with no access to the room at all is refused with 403 unless they
    /// can manage it, which the room owner, a room administrator and a DocSpace administrator can. The call is
    /// mutating and safe to repeat, since a repeat rewrites the same properties. Once a form is started, the answers
    /// submitted for it can be collected into a spreadsheet with `POST api/2.0/files/file/{fileId}/xlsx`.
    /// </remarks>
    /// <summary>
    /// Start filling a form
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/startfilling</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The form file, with the filling properties now stored on it", typeof(FileDto<int>))]
    [SwaggerResponse(403, "The caller holds only form-filling access on the room, or no access to it at all")]
    [HttpPut("{fileId}/startfilling")]
    public async Task<FileDto<T>> StartFillingFile(StartFillingRequestDto<T> inDto)
    {
        var file = await fileStorageService.StartFillingAsync(inDto.FileId);

        return await _fileDtoHelper.GetAsync(file);
    }

    /// <remarks>
    /// Keeps an editing session on the file alive, or ends it; an editor client calls it repeatedly while a document
    /// is open. `docKeyForTrack` has to be the document key of the file as it currently stands, the value
    /// `POST api/2.0/files/file/{fileId}/startedit` returned, and a key matching neither the current revision nor the
    /// one being edited is refused with 403. `tabId` names the client tab that holds the session, so several tabs and
    /// several users are tracked on one file independently. Refreshing an entry requires one of the editing rights on
    /// the file - editing, reviewing, commenting, filling or filter editing - so a reader is refused. With
    /// `isFinish=false` the entry is refreshed and the file stays marked as being edited; with `isFinish=true` the
    /// entry for that tab is dropped and the other clients are told that editing has stopped. The call changes the
    /// tracking state and never the document, and repeating it is safe. It answers `key` true with an empty `value`
    /// whenever it succeeds, so a failure arrives as an error rather than as a false key. An anonymous caller is
    /// accepted only through an external share link.
    /// </remarks>
    /// <summary>
    /// Track an editing session
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/trackeditfile</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The session was refreshed or closed", typeof(ItemKeyValuePair<bool, string>))]
    [SwaggerResponse(403, "The document key does not match the revision being edited")]
    [AllowAnonymous]
    [HttpGet("{fileId}/trackeditfile")]
    public async Task<ItemKeyValuePair<bool, string>> TrackEditFile(TrackEditFileRequestDto<T> inDto)
    {
        var result = await fileStorageService.TrackEditFileAsync(inDto.FileId, inDto.TabId, inDto.DocKeyForTrack, inDto.IsFinish);

        return new ItemKeyValuePair<bool, string> { Key = result.Key, Value = result.Value };
    }

    /// <remarks>
    /// Builds everything an editor client needs to open the file: the document descriptor with its download address,
    /// title, type and document key, the editor configuration with the mode, the caller's permissions, the user and
    /// the customization, the callback the editors report back to, and the signature token the document service
    /// validates. `version` opens one entry of the file history and requires access to that history; left out, the
    /// current revision is opened. `view`, `edit` and `fill` say what the client intends to do, and `editorType`
    /// picks the desktop, mobile or embedded layout. For a PDF form the room decides the outcome and may overrule the
    /// request: a form-filling room, a virtual data room, a public room and a user folder each produce their own
    /// mode, and a form opened from the templates folder is read-only and, outside the mobile layout, framed as
    /// embedded. When the portal is over its storage quota the configuration comes back read-only with the exceeded
    /// scope named. In a private room the caller's encryption keys are added to the editor configuration. Payment is
    /// not required and an anonymous caller opens through an external link.
    /// </remarks>
    /// <summary>
    /// Get the editor configuration
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/openedit</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The editor configuration for the requested file and mode", typeof(ConfigurationDto<int>))]
    [SwaggerResponse(403, "The caller cannot read the file, or asked for a past version without access to the file history")]
    [AllowAnonymous]
    [AllowNotPayment]
    [HttpGet("{fileId}/openedit")]
    public async Task<ConfigurationDto<T>> OpenEditFile(OpenEditRequestDto<T> inDto)
    {
        var (file, lastVersion) = await documentServiceHelper.GetCurFileInfoAsync(inDto.FileId, inDto.Version);
        FormOpenSetup<T> formOpenSetup = null;

        var rootFolder = await documentServiceHelper.GetRootFolderAsync(file);
        if (file.IsForm && rootFolder.RootFolderType != FolderType.RoomTemplates)
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

            if (inDto.Edit && rootFolder.FolderType == FolderType.FillingFormsRoom)
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

        }

        if (!string.IsNullOrEmpty(formOpenSetup?.FillingSessionId))
        {
            result.FillingSessionId = formOpenSetup.FillingSessionId;
        }

        if (rootFolder.RootFolderType == FolderType.RoomTemplates)
        {
            result.File.CanShare = false;
        }

        if (rootFolder.FolderType is FolderType.ResultStorage && file.Id is int fileId)
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
    /// Returns a direct download address for the current content of the file together with the signature token that
    /// the document service validates, which is what the portal hands over when the editors have to fetch the
    /// document themselves. The address points at the portal's file stream endpoint and is rewritten to the host the
    /// document service can reach, so on a deployment where the editors sit behind a private address it is not the
    /// address a browser should follow. The answer also carries the extension of the stored document, leading dot
    /// included. The caller needs read access to the file, and an unknown file id is reported as missing. The call
    /// only reads, and each call mints a fresh address and token rather than reusing the previous one, so the value
    /// is worth requesting again once a token has expired. For a link meant for a person, a plain address with no
    /// token to put behind a download button, use `GET api/2.0/files/file/{fileId}/presigneduri` instead.
    /// </remarks>
    /// <summary>
    /// Get a signed download address
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/presigned</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The download address of the file with its signature token", typeof(DocumentService.FileLink))]
    [HttpGet("{fileId}/presigned")]
    public async Task<DocumentService.FileLink> GetPresignedFileUri(FileIdRequestDto<T> inDto)
    {
        return await fileStorageService.GetPresignedUriAsync(inDto.FileId);
    }

    /// <remarks>
    /// Lists the portal members who can read the file, which is what an editor client offers when somebody types a
    /// mention. The set holds the readers of the file plus everyone who reads it by role rather than by share - the
    /// portal owner, the DocSpace administrators and the author of the file - while the caller themselves, the
    /// subjects standing behind external links and deactivated accounts are left out. It is ordered by display name
    /// as the portal renders it. A guest receives a single entry, the owner of the file, because a guest is not a
    /// portal member and may not learn who else works on the document. The caller needs read access to the file, and
    /// an unknown file id is reported as missing. The call only reads. A caller who reached the file through an
    /// external link instead of an account is answered with nothing at all. For the users to offer when protecting a
    /// document use `GET api/2.0/files/file/{fileId}/protectusers`.
    /// </remarks>
    /// <summary>
    /// Get users to mention in a file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/sharedusers</path>
    /// <collection>list</collection>
    [Tags("Files / Sharing")]
    [SwaggerResponse(200, "The portal members who can read the file, ordered by display name", typeof(List<MentionWrapper>))]
    [HttpGet("{fileId}/sharedusers")]
    public Task<List<MentionWrapper>> GetSharedUsers(FileIdRequestDto<T> inDto)
    {
        if (!securityContext.IsAuthenticated)
        {
            return Task.FromResult<List<MentionWrapper>>(null);
        }

        return fileStorageService.SharedUsersAsync(inDto.FileId);
    }

    /// <remarks>
    /// Returns a list of users with their access rights to the file.
    /// </remarks>
    /// <summary>Get user access rights</summary>
    /// <path>api/2.0/files/infousers</path>
    /// <collection>list</collection>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of users with their access rights to the file", typeof(List<MentionWrapper>))]
    [HttpPost("infousers")]
    public async Task<List<MentionWrapper>> GetInfoUsers(GetInfoUsersRequestDto inDto)
    {
        return await fileStorageService.GetInfoUsersAsync(inDto.UserIds);
    }

    /// <remarks>
    /// Resolves a reference that a formula in one spreadsheet makes to another document, and answers with the
    /// descriptor the document service needs in order to read it: the title, the download address, the file type, the
    /// document key of the co-editing session, the web editor link and the signature token. Three ways of naming the
    /// target are tried in order, and the first that resolves wins: `fileKey` as a file id inside the portal named by
    /// `instanceId`, then `path` looked up among the files sitting next to `sourceFileId`, then `link`, short links
    /// included, from which the file id is read out. A link that points outside this portal is not resolved at all
    /// and comes back unchanged as the address to follow. The caller needs read access to the source file and to its
    /// folder, otherwise the call is refused. The call only reads. A reference that resolves to nothing is still
    /// answered with 200, with the error text filled in and the rest of the descriptor empty, so read the error
    /// before using any other field.
    /// </remarks>
    /// <summary>
    /// Resolve a spreadsheet reference
    /// </summary>
    /// <path>api/2.0/files/file/referencedata</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The reference descriptor, or the same object with the error text set when nothing resolved", typeof(FileReference))]
    [HttpPost("referencedata")]
    public async Task<FileReference> GetReferenceData(GetReferenceDataDto<T> inDto)
    {
        return await fileStorageService.GetReferenceDataAsync(inDto.FileKey, inDto.InstanceId, inDto.SourceFileId, inDto.Path, inDto.Link);
    }

    /// <remarks>
    /// Lists the users the file is shared with, which is what a client offers when the author protects a document and
    /// picks who may still edit it. The list is built from the whole access list of the file: every entry that is not
    /// an explicit denial, with groups expanded into their members, the caller themselves and deleted accounts left
    /// out, ordered by display name. Access inherited from the room counts, so a member who never received a share on
    /// the file itself is listed too. A file kept in the legacy project storage always answers with an empty list
    /// rather than with its team. The call only reads. A guest is refused, an anonymous caller is answered with
    /// nothing, and a file id that resolves to nothing is refused as well instead of being reported as missing. For
    /// the readers to offer as mentions inside the editor use `GET api/2.0/files/file/{fileId}/sharedusers`.
    /// </remarks>
    /// <summary>
    /// Get users for document protection
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/protectusers</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The users the file is shared with, ordered by display name", typeof(List<MentionWrapper>))]
    [HttpGet("{fileId}/protectusers")]
    public async Task<List<MentionWrapper>> GetProtectedFileUsers(FileIdRequestDto<T> inDto)
    {
        return await fileStorageService.ProtectUsersAsync(inDto.FileId);
    }

    /// <remarks>
    /// Queues generation of the spreadsheet that collects every answer submitted for a PDF form in a form-filling
    /// room, and answers at once with the queued task, the original form and a flag telling whether the report file
    /// is being created now or an existing one refreshed in place. Either identifier works: the id of the original
    /// form, or the id of an XLSX or CSV result file inside the room's Complete folder, from which the portal
    /// resolves the form behind it. The form must already have been opened for filling with
    /// `PUT api/2.0/files/file/{fileId}/startfilling` and must still live in the form-filling room that started it.
    /// The caller must be allowed to update that form's report. The call is mutating and asynchronous: the
    /// spreadsheet is not ready when the response arrives, so poll `GET api/2.0/files/file/{fileId}/xlsx` with the
    /// original form's id until the task reports completion, then take the produced file from the task. Calling it
    /// again while a run is still going answers with that run instead of starting a second one.
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
    /// Reports how far the spreadsheet of submitted form answers has got, the one queued by
    /// `POST api/2.0/files/file/{fileId}/xlsx`. A run is kept per portal, per caller and per form, so this reports
    /// the caller's own run and not one started by another member of the room; address it with the id of the original
    /// form rather than with the id of the produced spreadsheet. The answer carries the completion flag, the progress
    /// percentage, the error text when the run failed, and the id, name and address of the produced file once it is
    /// there. Nothing at all comes back when no run is on record for this caller and form, which is the normal answer
    /// before the first run and not an error. The call only reads and is meant to be polled until completion is
    /// reported. Any authenticated caller may ask; whether the report may be built is decided when the run is queued,
    /// not here.
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
    /// Writes the portal-wide ONLYOFFICE Docs connection settings - the public Document Server address, its address
    /// inside the private network, the address it calls this portal back on, the request signature secret and header,
    /// and SSL verification - then verifies them against the running Document Server before keeping them. Every
    /// address is optional: an empty value drops the portal's own setting so that the deployment default takes over
    /// again. An address gets `http://` prepended when it carries no scheme, while an absolute address with a query
    /// string is rejected with 400, as is a signature secret sent without its header. Only the portal owner and a
    /// DocSpace administrator may call this; a room administrator, a user and a guest are refused with 403. The call
    /// is mutating and safe to repeat with the same body. Verification is live - the editor api script, the
    /// healthcheck, a test conversion, the command service and the document builder are all exercised - and when it
    /// fails the previous settings are restored in full and nothing is changed. The answer is what
    /// `GET api/2.0/files/docservice` returns with no version requested, so `version` comes back empty and the
    /// signature secret is not echoed back.
    /// </remarks>
    /// <summary>
    /// Set the document service address
    /// </summary>
    /// <path>api/2.0/files/docservice</path>
    [Tags("Files / Settings")]
    [SwaggerResponse(200, "The settings are stored and the Document Server answered the verification requests", typeof(DocServiceUrlDto))]
    [SwaggerResponse(400, "An address cannot be parsed or carries a query string, the signature secret is sent without its header, or an http address is given for a portal served over https")]
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
    /// Reports where this portal expects ONLYOFFICE Docs to be: the public Document Server address, the URL of the
    /// editor api script and of the preload page a client loads before opening a document, the address used inside
    /// the private network, the address the Document Server calls this portal back on, the name of the request
    /// signature header, whether SSL verification is on, and whether all of it is still at the deployment default.
    /// The call is read-only and needs no authorization: an anonymous caller and every role from the portal owner
    /// down to a guest read the same values. Pass `version=true` to have the editor version of the running Document
    /// Server included in `version`; left out, `version` comes back empty and the portal answers without contacting
    /// the Document Server at all. A version request never fails the call - when the Document Server does not answer,
    /// a fallback version string is reported instead of an error, so the value is no proof that the server is
    /// reachable. The signature secret is not part of the answer, only the header name it travels in. To change any
    /// of these settings use `PUT api/2.0/files/docservice`.
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
