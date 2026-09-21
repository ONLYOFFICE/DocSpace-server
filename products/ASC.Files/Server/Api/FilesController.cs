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

namespace ASC.Files.Api;

[ConstraintRoute("int")]
public class FilesControllerInternal(
    FilesControllerHelper filesControllerHelper,
    FileStorageService fileStorageService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper,
    HistoryApiHelper historyApiHelper,
    IFusionCache hybridCache,
    EditHistoryMapper editHistoryMapper,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    PathProvider pathProvider,
    UserManager userManager,
    AuthContext authContext,
    GlobalStore globalStore,
    BaseCommonLinkUtility baseCommonLinkUtility,
    EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper)
    : FilesController<int>(
        filesControllerHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper,
        hybridCache,
        editHistoryMapper,
        daoFactory,
        fileSecurity,
        documentServiceHelper,
        documentServiceConnector,
        pathProvider,
        userManager,
        authContext,
        globalStore,
        baseCommonLinkUtility,
        encryptionKeyPairDtoHelper)
{
    /// <remarks>
    /// Returns the activity log of a single file - who renamed, moved, shared, converted, locked or edited it, and
    /// when - as the portal recorded it in its audit trail. Entries arrive newest first, and the events that belong
    /// to one action are folded into a single entry whose `related` list carries the rest of them. `fromDate` and
    /// `toDate` are read in the portal's time zone and narrow the range; `startIndex` and `count` page through the
    /// result, and the number of matching entries is reported in the response headers rather than in the body. The
    /// caller needs read access to the file, so a member of the room it lies in, the admin of that room and a
    /// DocSpace admin all see the same log, while a caller without access to the room is refused with 403 and an
    /// unknown id is answered with 404. The operation is read-only. Only files stored in the portal itself have a log
    /// here - a file kept in a connected third-party storage has none. For the log of a folder or a room use
    /// `GET api/2.0/files/folder/{folderId}/log`.
    /// </remarks>
    /// <summary>
    /// Get file history
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/log</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The activity entries of the file, newest first", typeof(IAsyncEnumerable<HistoryDto>))]
    [SwaggerResponse(403, "The caller has no read access to the file")]
    [SwaggerResponse(404, "No file with this identifier exists")]
    [HttpGet("file/{fileId:int}/log")]
    public IAsyncEnumerable<HistoryDto> GetFileHistory(HistoryRequestDto inDto)
    {
        return historyApiHelper.GetFileHistoryAsync(inDto.FileId, inDto.FromDate, inDto.ToDate, inDto.StartIndex, inDto.Count);
    }
}

public class FilesControllerThirdparty(
    FilesControllerHelper filesControllerHelper,
    FileStorageService fileStorageService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper,
    IFusionCache hybridCache,
    EditHistoryMapper editHistoryMapper,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    PathProvider pathProvider,
    UserManager userManager,
    AuthContext authContext,
    GlobalStore globalStore,
    BaseCommonLinkUtility baseCommonLinkUtility,
    EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper)
    : FilesController<string>(
        filesControllerHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper,
        hybridCache,
        editHistoryMapper,
        daoFactory,
        fileSecurity,
        documentServiceHelper,
        documentServiceConnector,
        pathProvider,
        userManager,
        authContext,
        globalStore,
        baseCommonLinkUtility,
        encryptionKeyPairDtoHelper);

public abstract class FilesController<T>(
    FilesControllerHelper filesControllerHelper,
    FileStorageService fileStorageService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper,
    IFusionCache hybridCache,
    EditHistoryMapper editHistoryMapper,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    PathProvider pathProvider,
    UserManager userManager,
    AuthContext authContext,
    GlobalStore globalStore,
    BaseCommonLinkUtility baseCommonLinkUtility,
    EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Closes or reopens a revision group in the version history of a file and answers with every stored version of
    /// that file, newest first. With `continueVersion=false` the named version is completed: its content is stored
    /// again as a fresh version that opens a new revision group, so the editing that follows no longer extends the
    /// previous one. With `continueVersion=true` the last revision group is folded back into the group before it, so
    /// the next save continues that revision instead of becoming a version of its own; a file that has only one group
    /// is left as it is. A `version` of 0 means the current version. The caller needs the right to edit the history
    /// of the file, which the room admin, a DocSpace admin acting as room manager and a member with content-creator
    /// rights have; plain editing access is refused with 403, as are a guest and a member without access to the room.
    /// The call is mutating and not idempotent. A file that is locked, lies in Trash, is open in an editing session
    /// or is kept in a connected third-party storage is refused.
    /// </remarks>
    /// <summary>
    /// Change version history
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The versions of the file after the change", typeof(IAsyncEnumerable<FileDto<int>>))]
    [SwaggerResponse(403, "The caller may not change the version history of the file")]
    [HttpPut("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> ChangeVersionHistory(ChangeHistoryRequestDto<T> inDto)
    {
        return filesControllerHelper.ChangeHistoryAsync(inDto.FileId, inDto.File.Version, inDto.File.ContinueVersion);
    }

    /// <remarks>
    /// Reports how far the conversion of a file has got, as a list that holds one entry while the portal still knows
    /// about that conversion and nothing once it is over. Read `progress`, which counts from 0 to 100, `error` for
    /// the reason a conversion failed, and `file`, which carries the converted file as soon as it exists. Queue the
    /// conversion with `PUT api/2.0/files/file/{fileId}/checkconversion` and poll this operation until the entry
    /// reaches 100 or disappears: a finished entry is handed out once and then dropped, and an entry whose conversion
    /// stopped is discarded a few minutes later, so an empty list means either "already reported" or "never started"
    /// rather than an error. The same empty list is the answer for an identifier no file matches. Passing
    /// `start=true` starts the conversion as well, with the format from the portal settings and no password, which
    /// makes that one flag mutating; without it the operation is read-only. The caller needs read access to the file,
    /// and anyone else is refused.
    /// </remarks>
    /// <summary>
    /// Get conversion status
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The conversion entry of the file, or an empty list when the portal has none", typeof(IAsyncEnumerable<ConversationResultDto>))]
    [HttpGet("file/{fileId}/checkconversion")]
    public IAsyncEnumerable<ConversationResultDto> CheckConversionStatus(CheckConversionStatusRequestDto<T> inDto)
    {
        return filesControllerHelper.CheckConversionAsync(new CheckConversionRequestDto<T>
        {
            FileId = inDto.FileId,
            StartConvert = inDto.Start
        });
    }

    /// <remarks>
    /// Builds a download address for the current version of a file and answers with it as a plain string. The address
    /// points at the portal's own file handler and carries the file identifier, the version it was built for and a
    /// time-limited authentication key, so it can be handed to a downloader that cannot sign in to the portal itself;
    /// it stops working once that key has expired, and it keeps naming the version that was current when it was built
    /// rather than following later edits. The caller needs read access to the file: a member of the room it lies in
    /// gets an address, a caller without access to the room is refused, an unknown identifier is answered as not
    /// found and an anonymous caller is rejected. The operation is read-only and safe to repeat, though every call
    /// mints a new key. Nothing is downloaded here - follow the address to fetch the bytes. For the variant the
    /// document service signs, which comes back as an object with the file type and a token, use
    /// `GET api/2.0/files/file/{fileId}/presigned`.
    /// </remarks>
    /// <summary>
    /// Get file download link
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/presigneduri</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The download address of the current file version", typeof(string))]
    [HttpGet("file/{fileId}/presigneduri")]
    public async Task<string> GetPresignedUri(FileIdRequestDto<T> inDto)
    {
        return await filesControllerHelper.GetPresignedUri(inDto.FileId);
    }

    /// <remarks>
    /// Tells whether a file is a PDF form that can be filled out in the portal, and answers with a single boolean.
    /// The check is by content, not by extension: the beginning of the file is read and the answer is `true` only
    /// when it carries the marker the editors write into the forms they produce, so an ordinary PDF, and a PDF form
    /// made in other software, both answer `false`. A file whose name is not a PDF at all answers `false` without
    /// being read. Use it before offering the form-filling operations on a file, because a document that answers
    /// `false` cannot be started for filling. The caller needs read access to the file, and read access is enough - a
    /// member of the room with read-only rights gets the answer; a caller without access to the room is refused and
    /// an anonymous caller is rejected. The operation is read-only and idempotent. It says nothing about the state of
    /// the filling - for that read `GET api/2.0/files/file/{fileId}/formroles`.
    /// </remarks>
    /// <summary>
    /// Check the PDF file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/isformpdf</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "True when the file is a PDF form made in the editors, false otherwise", typeof(bool))]
    [HttpGet("file/{fileId}/isformpdf")]
    public async Task<bool> isFormPDF(FileIdRequestDto<T> inDto)
    {
        return await filesControllerHelper.isFormPDF(inDto.FileId);
    }

    /// <remarks>
    /// Copies one file into another folder under a new title, converting its content when the new title names a
    /// different format, and answers with the copy that was created. The extension of `destTitle` decides what
    /// happens: the same extension as the source copies the bytes as they are, a different one has the document
    /// service convert them first, and `toForm=true` converts a document into a PDF form. `password` unlocks a source
    /// file that is protected by one. `destFolderId` is read as a number for a folder inside the portal and as a
    /// string for a folder in a connected third-party storage; anything else is answered with an empty body and
    /// nothing is copied. The caller needs read access to the source file and the right to create files in the
    /// destination folder, and is otherwise refused with 403; a missing file or folder is answered with 404, and a
    /// format that cannot be converted with 400. The call is mutating and not idempotent - each call adds another
    /// copy. To copy many items at once, and without converting, use `PUT api/2.0/files/fileops/copy`.
    /// </remarks>
    /// <summary>
    /// Copy a file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/copyas</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The copy that was created", typeof(FileEntryBaseDto))]
    [SwaggerResponse(400, "The content cannot be converted into the format of the new title")]
    [SwaggerResponse(403, "The caller may not read the file or may not create files in the destination folder")]
    [SwaggerResponse(404, "The file or the destination folder does not exist")]
    [HttpPost("file/{fileId}/copyas")]
    public async Task<FileEntryBaseDto> CopyFileAs(CopyAsRequestDto<T> inDto)
    {
        return inDto.File.DestFolderId.ValueKind switch
        {
            JsonValueKind.Number => await filesControllerHelper.CopyFileAsAsync(inDto.FileId, inDto.File.DestFolderId.GetInt32(), inDto.File.DestTitle, inDto.File.Password, inDto.File.ToForm),
            JsonValueKind.String => await filesControllerHelper.CopyFileAsAsync(inDto.FileId, inDto.File.DestFolderId.GetString(), inDto.File.DestTitle, inDto.File.Password, inDto.File.ToForm),
            _ => null
        };
    }

    /// <remarks>
    /// Creates a file in the folder named in the route and answers with the stored file. The extension in the title
    /// decides the format: an extension of a known text, spreadsheet or presentation format is rewritten to the
    /// portal's own DOCX, XLSX or PPTX, a title with no extension at all gets DOCX added, while an unknown extension
    /// and the few formats the portal keeps as they are stay untouched; `enableExternalExt=true` stores the title
    /// verbatim and skips that rewriting. The content comes from one of three sources, tried in this order: `formId`
    /// copies a ready form out of the form gallery, `templateId` copies an existing file the caller can read - a
    /// number for a file in the portal, a string for one in a connected third-party storage - and with neither of
    /// them the portal's blank template for that format and the caller's language is used. The caller needs the right
    /// to create files in the folder, and the room roots, Archive and the template sections are refused even to an
    /// admin. The call is mutating and not idempotent. To create the file in the caller's own section use
    /// `POST api/2.0/files/@my/file`.
    /// </remarks>
    /// <summary>
    /// Create a file
    /// </summary>
    /// <path>api/2.0/files/{folderId}/file</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created file", typeof(FileDto<int>))]
    [HttpPost("{folderId}/file")]
    public async Task<FileDto<T>> CreateFile(CreateFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.TemplateId, inDto.File.FormId, inDto.File.EnableExternalExt);
    }

    /// <remarks>
    /// Creates an HTML file in the folder named in the route out of the markup passed as the content, and answers
    /// with the stored file. The `.html` extension is added to the title unless the title already ends with it, and a
    /// request carrying no content is rejected as an invalid request. `createNewIfExist` acts the other way round
    /// than its name reads: with `true` the file that already carries this title is updated, the markup replacing its
    /// content and a version appearing in its history, while with `false`, which is also the default, another file is
    /// created and its title made unique, as in "Notes (1).html". Updating needs the existing file to be editable by
    /// the caller, so one that is locked, open in an editing session, encrypted or in Trash is left alone and a new
    /// file appears beside it instead. The caller needs the right to create files in the folder and is otherwise
    /// refused with 403. The call is mutating. To create the file in the caller's own section use
    /// `POST api/2.0/files/@my/html`.
    /// </remarks>
    /// <summary>
    /// Create an HTML file
    /// </summary>
    /// <path>api/2.0/files/{folderId}/html</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created or updated HTML file", typeof(FileDto<int>))]
    [SwaggerResponse(403, "The caller may not create files in this folder")]
    [HttpPost("{folderId}/html")]
    public async Task<FileDto<T>> CreateHtmlFile(CreateTextOrHtmlFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateHtmlFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.Content, !inDto.File.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates a text file in the folder named in the route out of the text passed as the content, and answers with
    /// the stored file. The extension follows the content rather than the request: `.txt` normally, but `.html` as
    /// soon as the text contains something shaped like an HTML tag, so a snippet of markup sent here ends up as an
    /// HTML file; the extension is added to the title unless the title already ends with it. A request carrying no
    /// content is rejected as an invalid request. `createNewIfExist` acts the other way round than its name reads:
    /// with `true` the file that already carries this title is updated and a version appears in its history, while
    /// with `false`, which is also the default, another file is created and its title made unique, as in "Notes
    /// (1).txt". A file that is locked, open in an editing session, encrypted or in Trash is not updated - a new file
    /// appears beside it instead. The caller needs the right to create files in the folder. The call is mutating. To
    /// create the file in the caller's own section use `POST api/2.0/files/@my/text`.
    /// </remarks>
    /// <summary>
    /// Create a text file
    /// </summary>
    /// <path>api/2.0/files/{folderId}/text</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created or updated text file", typeof(FileDto<int>))]
    [HttpPost("{folderId}/text")]
    public async Task<FileDto<T>> CreateTextFile(CreateTextOrHtmlFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateTextFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.Content, !inDto.File.CreateNewIfExist);
    }

    /// <remarks>
    /// Queues the deletion of one file and answers with the caller's file operations, the one just created among
    /// them. The file is not gone when the response arrives: poll `GET api/2.0/files/fileops` until the operation
    /// reports `finished`, and read its `error` to learn whether the deletion succeeded. By default the file is moved
    /// to Trash, from where it can be restored; `immediately=true` deletes it for good instead, and inside a room,
    /// where there is no Trash, deletion is always final. `deleteAfter=true` postpones the deletion until the editing
    /// session on the file has ended, so a file somebody is working on is not pulled away.
    /// `returnSingleOperation=true` narrows the answer to this deletion instead of listing every active operation of
    /// the caller. The caller needs the right to delete the file, which the room admin, a DocSpace admin acting as
    /// room manager and a content creator acting on their own file have; editing access alone, read access, a guest
    /// and a member without access to the room are all refused. The call is destructive. To delete several items at
    /// once use `PUT api/2.0/files/fileops/delete`.
    /// </remarks>
    /// <summary>
    /// Delete a file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file operations of the caller, including the deletion just queued", typeof(IAsyncEnumerable<FileOperationDto>))]
    [HttpDelete("file/{fileId}")]
    public async IAsyncEnumerable<FileOperationDto> DeleteFile(DeleteRequestDto<T> inDto)
    {
        var taskId = await fileOperationsManager.Publish([], [inDto.FileId], false, !inDto.File.DeleteAfter, inDto.File.Immediately);

        foreach (var e in await fileOperationsManager.GetOperationResults(inDto.ReturnSingleOperation ? taskId : null))
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Answers with the outcome of one completed form-filling session: the filled copy of the form, the original form
    /// it was made from, the number this submission was given inside the room, the identifier of the room and the
    /// account that started the filling. `isRoomMember` says whether the caller is a member of that room, which a
    /// client uses to decide whether the room can be offered for opening. The session is named by `fillingSessionId`,
    /// the value the document service reports when the filling ends; the portal remembers it only for a while after
    /// that, so a session that was never completed, one already forgotten and a value of the wrong shape are all
    /// answered as not found, while omitting the parameter is rejected as an invalid request. The operation is
    /// read-only and needs no sign-in: it is meant for the caller that has just finished filling the form through an
    /// external link, and the session identifier is the only secret involved. The filled copy itself is an ordinary
    /// file - read it with `GET api/2.0/files/file/{fileId}`.
    /// </remarks>
    /// <summary>
    /// Get form-filling result
    /// </summary>
    /// <path>api/2.0/files/file/fillresult</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The result of the completed form-filling session", typeof(FillingFormResultDto<int>))]
    [AllowAnonymous]
    [HttpGet("file/fillresult")]
    public async Task<FillingFormResultDto<T>> GetFillResult(GetFillResultRequestDto inDto)
    {
        var completedFormId = await hybridCache.GetOrDefaultAsync<string>(inDto.FillingSessionId);

        if (completedFormId != null)
        {
            return await filesControllerHelper.GetFillResultAsync((T)Convert.ChangeType(completedFormId, typeof(T)));
        }
        throw new ItemNotFoundException();
    }

    /// <remarks>
    /// Answers with everything an editor needs in order to show what changed in one version of a file: the address of
    /// the version itself, its document key and format, the address of the recorded changes, the same trio for the
    /// version it is compared against, and a token that signs the whole answer for the document service. `version`
    /// picks the version, and 0, the default, means the current one. `changesUrl` and `previous` are filled in only
    /// when the portal has stored the changes of that version, which is the case for versions written by an editing
    /// session; for a version uploaded as a whole they stay empty and only the file itself can be shown. The
    /// addresses are meant for the document service and carry their own time-limited keys. The caller needs the right
    /// to read the history of the file, which editing access and above grant: read-only access, commenting access, a
    /// guest and an anonymous caller are all refused, as is a file kept in a connected third-party storage. The
    /// operation is read-only. For the list of versions themselves use
    /// `GET api/2.0/files/file/{fileId}/edit/history`.
    /// </remarks>
    /// <summary>
    /// Get changes URL
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/edit/diff</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The addresses and keys the editor needs to show the changes", typeof(EditHistoryDataDto))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/diff")]
    public async Task<EditHistoryDataDto> GetEditDiffUrl(EditDiffUrlRequestDto<T> inDto)
    {
        var version = inDto.Version;
        var fileId = inDto.FileId;

        var fileDao = daoFactory.GetFileDao<T>();

        var file = version > 0
            ? await fileDao.GetFileAsync(fileId, version)
            : await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
    }

        if (!await fileSecurity.CanReadHistoryAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        if (file.ProviderEntry)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        var result = new EditHistoryDataDto
        {
            FileType = file.ConvertedExtension.Trim('.'),
            Key = await documentServiceHelper.GetDocKeyAsync(file),
            Url = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileStreamUrl(file)),
            Version = version
        };

        if (await fileDao.ContainChangesAsync(file.Id, file.Version))
        {
            string previousKey;
            string sourceFileUrl;
            string sourceExt;

            var history = await fileDao.GetFileHistoryAsync(file.Id).ToListAsync();
            var previousFileStable = history.OrderByDescending(r => r.Version).FirstOrDefault(r => r.Version < file.Version);
            if (previousFileStable != null)
            {
                sourceFileUrl = pathProvider.GetFileStreamUrl(previousFileStable);
                sourceExt = previousFileStable.ConvertedExtension;

                previousKey = await documentServiceHelper.GetDocKeyAsync(previousFileStable);
            }
            else
            {
                var culture = (await userManager.GetUsersAsync(authContext.CurrentAccount.ID)).GetCulture();
                var storeTemplate = await globalStore.GetStoreTemplateAsync();
                var fileExt = FileUtility.GetFileExtension(file.Title);
                var path = await globalStore.GetNewDocTemplatePath(storeTemplate, fileExt, culture);
                var uri = await storeTemplate.GetUriAsync("", path);

                sourceFileUrl = baseCommonLinkUtility.GetFullAbsolutePath(uri.ToString());
                sourceExt = fileExt.Trim('.');

                previousKey = DocumentServiceConnector.GenerateRevisionId(Guid.NewGuid().ToString());
            }

            result.Previous = new EditHistoryUrl { Key = previousKey, Url = documentServiceConnector.ReplaceCommunityAddress(sourceFileUrl), FileType = sourceExt.Trim('.') };

            result.ChangesUrl = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileChangesUrl(file));
        }

        result.Token = documentServiceHelper.GetSignature(result);

        return result;
    }

    /// <remarks>
    /// Returns the editing revisions of a file, oldest first, as the document service understands them: each entry
    /// carries the version and the revision group it belongs to, the account that saved it, when it was saved, the
    /// comment left on it, the document key of that revision and, where the portal stored them, the changes it
    /// introduced. Only the revisions a person saved are listed - the autosaves an editing session writes in between
    /// are left out, which is what separates this list from the plain version list of
    /// `GET api/2.0/files/file/{fileId}/history`. The caller needs the right to read the history of the file, which
    /// editing access and above grant: commenting access, read-only access, a guest, a member without access to the
    /// room and an anonymous caller are all refused, and so is a file kept in a connected third-party storage, which
    /// keeps no history in the portal. The operation is read-only. Take one entry to
    /// `GET api/2.0/files/file/{fileId}/edit/diff` to show its changes, or to
    /// `POST api/2.0/files/file/{fileId}/restoreversion` to bring it back.
    /// </remarks>
    /// <summary>
    /// Get version history
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/edit/history</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The editing revisions of the file, oldest first", typeof(IAsyncEnumerable<EditHistoryDto>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/history")]
    public IAsyncEnumerable<EditHistoryDto> GetEditHistory(FileIdRequestDto<T> inDto)
    {
        return fileStorageService.GetEditHistoryAsync(inDto.FileId).Select(editHistoryMapper.MapToDto);
    }

    /// <remarks>
    /// Returns one file as the portal stores it, together with the state it has for the caller: the title, the folder
    /// it lies in, the size, the current version and revision group, the addresses for viewing and editing it, the
    /// actions the caller is allowed to perform on it, the sharing rights it was reached through, and the thumbnail
    /// state. `version` picks an older version instead of the current one; the default of -1 means the current
    /// version. When the file belongs to another person's own section and the caller cannot read the folder holding
    /// it, the answer reports the "Shared with me" section as its folder, so that a client can show it in a place the
    /// caller can actually open. The caller needs read access to the file, which any member of the room it lies in
    /// has; a caller without access to the room is refused and an anonymous caller without an external share link is
    /// rejected. The operation is read-only. For every version at once use `GET api/2.0/files/file/{fileId}/history`.
    /// </remarks>
    /// <summary>
    /// Get file information
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file as it is stored, with the state it has for the caller", typeof(FileDto<int>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}")]
    public async Task<FileDto<T>> GetFileInfo(FileInfoRequestDto<T> inDto)
    {
        return await filesControllerHelper.GetFileInfoAsync(inDto.FileId, inDto.Version);
    }


    /// <remarks>
    /// Returns every stored version of a file, newest first, each of them shaped like the file itself - the version
    /// and the revision group it belongs to, the size, the comment saved with it, the addresses for viewing it, and
    /// the thumbnail and lock state. Unlike the editing revisions of `GET api/2.0/files/file/{fileId}/edit/history`,
    /// this list also holds the autosave revisions an editing session writes, so it is the fuller of the two, and it
    /// is the shape a client already knows how to render. The caller needs the right to read the history of the file,
    /// which is a stricter rule than reading the file: in a room only its managers and content creators may read the
    /// history, and in a personal section editing access is enough, so a member with read access to somebody else's
    /// file, and even a DocSpace admin in that position, are refused, as is an anonymous caller. The operation is
    /// read-only. To restore one of the versions use `POST api/2.0/files/file/{fileId}/restoreversion`, and to close
    /// or reopen a revision group `PUT api/2.0/files/file/{fileId}/history`.
    /// </remarks>
    /// <summary>
    /// Get file versions
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Every stored version of the file, newest first", typeof(IAsyncEnumerable<FileDto<int>>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> GetFileVersionInfo(FileIdRequestDto<T> inDto)
    {
        return filesControllerHelper.GetFileVersionInfoAsync(inDto.FileId);
    }

    /// <remarks>
    /// Locks a file so that nobody else can change it, or releases that lock, and answers with the file as it now
    /// stands. With `lockFile=true` the lock is put on the file and everybody else who is editing it at that moment
    /// is dropped out of the session, the caller excepted; the lock then blocks editing, renaming and deleting for
    /// everybody but the account that set it and the room admins. With `lockFile=false` the lock is removed and a
    /// note about the unlocking is appended to the current version comment, unless the file lives in a connected
    /// third-party storage. Locking a file that is already locked, or unlocking one that is not, changes nothing and
    /// still answers with the file, so the call is idempotent in effect while remaining a mutating one. The caller
    /// needs the right to lock the file, which the room admin, a DocSpace admin acting as room manager and a member
    /// with content-creator rights have; a member without access to the room and a guest are refused, and so is a
    /// file in Trash. A lock set by somebody else can only be released by a room manager.
    /// </remarks>
    /// <summary>
    /// Lock a file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/lock</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file with its lock state as it now stands", typeof(FileDto<int>))]
    [HttpPut("file/{fileId}/lock")]
    public async Task<FileDto<T>> LockFile(LockFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.LockFileAsync(inDto.FileId, inDto.File.LockFile);
    }

    /// <remarks>
    /// Turns the Custom Filter editing mode of a spreadsheet on or off and answers with the file as it now stands. In
    /// that mode the sorting and filtering one person applies to the sheet is visible to that person alone, so that
    /// several people can work on the same data without moving the rows under each other; with the mode off,
    /// filtering is shared again, as everywhere else. Turning it on also drops everybody else out of the running
    /// editing session, the caller excepted, because the mode has to be established before the sheet is opened. Only
    /// formats that support the mode are accepted; anything else is rejected as an invalid request. The caller needs
    /// the right to use the mode in the room, which the room admin and a DocSpace admin acting as room manager have;
    /// read-only access, a member without access to the room and an anonymous caller are refused. Once the mode has
    /// been switched on by one person, only that person, a room manager or a DocSpace admin can switch it off again.
    /// The call is mutating and, called twice with the same value, changes nothing the second time.
    /// </remarks>
    /// <summary>
    /// Set the Custom Filter editing mode
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/customfilter</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The spreadsheet with its Custom Filter state as it now stands", typeof(FileDto<int>))]
    [HttpPut("file/{fileId}/customfilter")]
    public async Task<FileDto<T>> SetCustomFilterTag(FileCustomFilterRequestDto<T> inDto)
    {
        var result = await fileStorageService.SetCustomFilterTagAsync(inDto.FileId, inDto.Parameters.Enabled);

        return await _fileDtoHelper.GetAsync(result);
    }

    /// <remarks>
    /// Brings an earlier version of a file back and answers with the editing revisions of the file after the restore.
    /// Nothing is overwritten: the content of the chosen version is stored again as a new version on top of the
    /// history, carrying a comment that says which version it was reverted to, so the intervening versions stay
    /// readable. `url` changes the source - with it the content is fetched from that address, which is how the
    /// document service returns a document with a set of changes rolled back, and the new version records that
    /// instead. Any links that pointed at drafts of the file are dropped, and the file is marked as new for the other
    /// people who can read it. `version` has to name an existing version and is refused with 400 when it is missing
    /// or already the current one. The caller needs the right to edit the history of the file and is otherwise
    /// refused with 403, an anonymous caller included. The call is mutating and not idempotent. A locked file, one in
    /// Trash, one being edited, an encrypted one and one kept in a connected third-party storage are all refused.
    /// </remarks>
    /// <summary>
    /// Restore a file version
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/restoreversion</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The editing revisions of the file after the restore", typeof(IAsyncEnumerable<EditHistoryDto>))]
    [SwaggerResponse(400, "The version is missing or is already the current one")]
    [SwaggerResponse(403, "The caller may not change the version history of the file")]
    [AllowAnonymous]
    [HttpPost("file/{fileId}/restoreversion")]
    public IAsyncEnumerable<EditHistoryDto> RestoreFileVersion(RestoreVersionRequestDto<T> inDto)
    {
        return fileStorageService.RestoreVersionAsync(inDto.FileId, inDto.Version, inDto.Url).Select(editHistoryMapper.MapToDto);
    }

    /// <remarks>
    /// Queues the conversion of a file into the portal's own editable format and answers with the conversion entry
    /// the caller is to poll. The whole body may be omitted, in which case the defaults apply. `outputType` names the
    /// target format and, left empty, the portal's default for that kind of document is used; `password` unlocks a
    /// protected source file; `version` converts an older version instead of the current one. `createNewIfExist`
    /// decides where the result goes: with `true` a new file is created beside the source, while with `false`, the
    /// default, the converted file that already exists is replaced. `sync=true` converts inside the request and
    /// answers with the finished result instead of a queue entry, which is only sensible for small documents.
    /// Otherwise poll `GET api/2.0/files/file/{fileId}/checkconversion` until `progress` reaches 100 and take the
    /// converted file from `file`. Only formats the portal has to convert are accepted; anything already editable,
    /// and anything it cannot convert, is answered without work being queued or rejected as an invalid request. The
    /// caller needs read access to the file. The call is mutating and not idempotent.
    /// </remarks>
    /// <summary>
    /// Start file conversion
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The conversion entry to poll, or the finished result when the conversion is synchronous", typeof(IAsyncEnumerable<ConversationResultDto>))]
    [HttpPut("file/{fileId}/checkconversion")]
    public IAsyncEnumerable<ConversationResultDto> StartFileConversion(StartConversionRequestDto<T> inDto)
    {
        inDto.CheckConversion ??= new CheckConversionRequestDto<T>();
        inDto.CheckConversion.FileId = inDto.FileId;

        return filesControllerHelper.StartConversionAsync(inDto.CheckConversion);
    }

    /// <remarks>
    /// Replaces the comment stored on one version of a file - the note that explains what changed in it - and answers
    /// with the comment as it was stored, which is the text cut to the length the portal keeps. `version` names the
    /// version and has to be an existing one: a version that does not exist is rejected as an invalid request, while
    /// a file that does not exist at all is answered as not found. Sending an empty comment clears the note. The
    /// caller needs the right to edit the history of the file, which the room admin, a DocSpace admin acting as room
    /// manager and a member with content-creator rights have; a member with editing access to somebody else's file,
    /// read-only access, a guest and an anonymous caller are all refused. A file that is locked by somebody else or
    /// lies in Trash is refused as well. The call is mutating and idempotent - repeating it with the same text leaves
    /// the same comment. The comments of all versions come back with `GET api/2.0/files/file/{fileId}/edit/history`.
    /// </remarks>
    /// <summary>
    /// Update a comment
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/comment</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The comment as it was stored", typeof(string))]
    [HttpPut("file/{fileId}/comment")]
    public async Task<string> UpdateFileComment(UpdateCommentRequestDto<T> inDto)
    {
        return await filesControllerHelper.UpdateCommentAsync(inDto.FileId, inDto.File.Version, inDto.File.Comment);
    }

    /// <remarks>
    /// Renames a file, restores one of its versions, or both at once, and answers with the file as it now stands. A
    /// non-empty `title` renames the file, keeping the stored extension whatever the new title says, so a rename
    /// cannot change the format; an empty or missing title leaves the name alone. A `lastVersion` above 0 restores
    /// that version the way `POST api/2.0/files/file/{fileId}/restoreversion` does, storing its content again on top
    /// of the history, while 0 or less leaves the versions untouched and answers with the file as it is - which makes
    /// this operation a read of the file when both fields are left out. The caller needs edit access, and renaming
    /// somebody else's file additionally needs room-manager rights: a member or room admin with plain editing access,
    /// read-only access, a guest and a DocSpace admin who is not a member of the room are all refused with 403, while
    /// a content creator may rename a file of their own. The call is mutating. Renaming marks the file as new for
    /// everybody else who can read it.
    /// </remarks>
    /// <summary>
    /// Update a file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file after the rename, the restore, or both", typeof(FileDto<int>))]
    [SwaggerResponse(403, "The caller may not rename the file or change its version")]
    [AllowAnonymous]
    [HttpPut("file/{fileId}")]
    public async Task<FileDto<T>> UpdateFile(UpdateFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.UpdateFileAsync(inDto.FileId, inDto.File.Title, inDto.File.LastVersion);
    }

    /// <remarks>
    /// Updates the contents of a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Update file contents</summary>
    /// <path>api/2.0/files/{fileId}/update</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [SwaggerResponse(404, "File not found")]
    [HttpPut("{fileId}/update")]
    public async Task<FileDto<T>> UpdateFileStreamFromForm(FileStreamRequestDto<T> inDto)
    {
        IEnumerable<IFormFile> files = Request.Form.Files;
        var file = files.Any() ? files.First() : inDto.File;

        return await filesControllerHelper.UpdateFileStreamAsync(file.OpenReadStream(), inDto.FileId, inDto.FileExtension, inDto.Encrypted, inDto.Forcesave);
    }

    /// <remarks>
    /// Answers with the primary external link of a file, creating it on the first call and returning the one that
    /// already exists afterwards, so the operation is idempotent in effect: a second call with other parameters does
    /// not reconfigure the existing link, and changing one is the business of `PUT api/2.0/files/file/{id}/links`.
    /// The parameters therefore only shape the link at the moment it is born - `access` its rights, `expirationDate`
    /// its lifetime, which for a file in a personal section is unlimited here rather than the default of a few days,
    /// `internal` whether only signed-in members may follow it, `denyDownload` whether the content may only be
    /// viewed, and `password` a secret to be asked for. A PDF form gets the rights it needs for filling out whatever
    /// was asked for, and a form in a form-filling room is answered with the link of the room instead. The caller
    /// needs the right to share the file and is otherwise refused with 403; a link that was deliberately revoked is
    /// not recreated but answered with 404. Read the address from `sharedTo.shareLink`.
    /// </remarks>
    /// <summary>
    /// Create the file primary external link
    /// </summary>
    /// <path>api/2.0/files/file/{id}/link</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The primary external link of the file", typeof(FileShareDto))]
    [SwaggerResponse(403, "The caller may not share the file")]
    [SwaggerResponse(404, "The file does not exist, or its primary link was revoked")]
    [HttpPost("file/{id}/link")]
    public async Task<FileShareDto> CreateFilePrimaryExternalLink(FileLinkRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(
            inDto.Id,
            FileEntryType.File,
            inDto.File.Access,
            expirationDate: inDto.File.ExpirationDate,
            requiredAuth: inDto.File.Internal,
            allowUnlimitedDate: true,
            denyDownload: inDto.File.DenyDownload,
            password: inDto.File.Password);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Answers with the primary external link of a file - the one the "Copy link" action of a client hands out - with
    /// its address in `sharedTo.shareLink`, its rights in `access`, and its expiration date, password flag and
    /// download restriction beside them. The link is created on the first read if the file has none, with read
    /// rights, no password and no expiry, so this operation mutates on that first call and is a plain read
    /// afterwards; repeated calls answer with the same link identifier. A PDF form in a form-filling room is answered
    /// with the link of that room, carried over to the form. The caller needs the right to share the file, which its
    /// creator, the room admin and a DocSpace admin acting as room manager have; a caller without access to the file
    /// is refused with 403 and an anonymous caller is rejected, while a link that was deliberately revoked is
    /// answered with 404 rather than being recreated. The custom links of the same file, the primary one excepted,
    /// are listed by `GET api/2.0/files/file/{id}/links`.
    /// </remarks>
    /// <summary>
    /// Get the file primary external link
    /// </summary>
    /// <path>api/2.0/files/file/{id}/link</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The primary external link of the file", typeof(FileShareDto))]
    [SwaggerResponse(403, "The caller may not share the file")]
    [SwaggerResponse(404, "The file does not exist, or its primary link was revoked")]
    [AllowAnonymous]
    [HttpGet("file/{id}/link")]
    public async Task<FileShareDto> GetFilePrimaryExternalLink(FilePrimaryIdRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.File, allowUnlimitedDate: true);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Lists the external links of a file, each with its identifier, title, address, rights, expiration date and
    /// download restriction. `startIndex` and `count` page through the list, and the total number of links is
    /// reported in the response headers rather than in the body. A file that has never been shared by link answers
    /// with an empty list; the primary link is part of this list once it exists, and it is the only one that is
    /// created on demand, by `GET api/2.0/files/file/{id}/link`. For a PDF form kept in a form-filling room the link
    /// of the room is appended to the answer, because that is the address through which the form is filled out. The
    /// caller needs the right to share the file, which its creator, the room admin and a DocSpace admin acting as
    /// room manager have; a caller without access to the file is refused and an anonymous caller is rejected. The
    /// operation is read-only. Take an identifier from here to `PUT api/2.0/files/file/{id}/links` to change or
    /// remove that link.
    /// </remarks>
    /// <summary>
    /// Get file external links
    /// </summary>
    /// <path>api/2.0/files/file/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The external links of the file", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpGet("file/{id}/links")]
    public async IAsyncEnumerable<FileShareDto> GetFileLinks(FilePrimaryIdRequestDto<T> inDto)
    {
        var offset = inDto.StartIndex;
        var count = inDto.Count;

        var totalCount = await fileStorageService.GetPureSharesCountAsync(inDto.Id, FileEntryType.File, ShareFilterType.ExternalLink, null);

        apiContext.SetCount(Math.Min(totalCount - offset, count)).SetTotalCount(totalCount);

        await foreach (var ace in fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.File, ShareFilterType.ExternalLink, null, offset, count))
        {
            yield return await fileShareDtoHelper.Get(ace);
        }
    }

    /// <remarks>
    /// Creates an external link to a file, or changes or revokes an existing one, and answers with the link as it now
    /// stands. `linkId` decides which: an identifier that is not yet in use, the empty one included, creates a link,
    /// while the identifier of an existing link rewrites it, so the whole set of parameters is applied every time and
    /// a field left out is reset rather than kept. `access` carries the rights the link grants, and `access` set to
    /// the value that denies everything revokes the link instead - the answer is then empty, and a revoked primary
    /// link is not recreated by a later read. `title` names the link for the people who manage it, `expirationDate`
    /// limits its lifetime and is refused when it lies more than a few years ahead, `password` asks visitors for a
    /// secret, `denyDownload` leaves them with viewing only, `internal` admits signed-in members alone, and
    /// `primary=true` makes it the primary link of the file. The caller needs the right to share the file and is
    /// otherwise refused, an unknown file being answered as not found. The call is mutating.
    /// </remarks>
    /// <summary>
    /// Set a file external link
    /// </summary>
    /// <path>api/2.0/files/file/{id}/links</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The link as it now stands, or nothing when it was revoked", typeof(FileShareDto))]
    [HttpPut("file/{id}/links")]
    public async Task<FileShareDto> SetFileExternalLink(FileLinkRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.SetExternalLinkAsync(
            inDto.Id,
            FileEntryType.File,
            inDto.File.LinkId,
            inDto.File.Title,
            inDto.File.Access,
            inDto.File.ExpirationDate,
            inDto.File.Password,
            inDto.File.DenyDownload,
            inDto.File.Internal,
            inDto.File.Primary);

        return linkAce is not null ? await fileShareDtoHelper.Get(linkAce) : null;
    }

    /// <remarks>
    /// Puts a file at a given position inside its folder and answers with the file, its `order` reporting where it
    /// now stands. Positions count from 1, and the file that held the wanted position, together with everything after
    /// it, is shifted to make room, so the numbering of a folder stays without gaps; a position beyond the end of the
    /// folder places the file last. The value may also be sent as a dotted path, as in "1.2.3", in which case only
    /// its last segment is read. Ordering is what the manual sorting of a room is built on, and it only means
    /// something in rooms whose contents are indexed - elsewhere the value is stored and ignored. The caller needs
    /// edit access to the file, which room managers, content creators and members with editing rights have; a member
    /// acting on somebody else's file, a guest and an anonymous caller are refused with 403, and an unknown file is
    /// answered with 404. The call is mutating and idempotent. To move several items in one go use
    /// `PUT api/2.0/files/order`.
    /// </remarks>
    /// <summary>
    /// Set file order
    /// </summary>
    /// <path>api/2.0/files/{fileId}/order</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file with the position it now holds", typeof(FileDto<int>))]
    [SwaggerResponse(403, "The caller may not reorder this file")]
    [SwaggerResponse(404, "The file does not exist")]
    [HttpPut("{fileId}/order")]
    public async Task<FileDto<T>> SetFileOrder(OrderFileRequestDto<T> inDto)
    {
        var file = await fileStorageService.SetFileOrder(inDto.FileId, inDto.Order.Order);

        return await _fileDtoHelper.GetAsync(file);
    }

    /// <remarks>
    /// Puts several files and folders at given positions in one go and answers with the entries that were moved, each
    /// with the position it now holds. Every item of `items` names an entry by its identifier and its kind - a file
    /// or a folder - and the position it is to take, counting from 1; a position may also be sent as a dotted path,
    /// as in "1.2.3", of which only the last segment is read. The items are applied one after another in the order
    /// they are sent, and each of them shifts its neighbours, so the result depends on that order; the whole request
    /// is not one transaction, and a failure in the middle leaves the items before it moved. Every item has to lie in
    /// a room the caller may administer, which the room admin and a DocSpace admin acting as room manager do:
    /// read-only access, a guest and an anonymous caller are refused, and an identifier that matches nothing is
    /// answered as not found. Ordering only means something in rooms whose contents are indexed. The call is
    /// mutating. For a single file use `PUT api/2.0/files/{fileId}/order`.
    /// </remarks>
    /// <summary>
    /// Set order of files
    /// </summary>
    /// <path>api/2.0/files/order</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The files and folders that were moved, with the positions they now hold", typeof(IAsyncEnumerable<FileEntryDto<int>>))]
    [HttpPut("order")]
    public IAsyncEnumerable<FileEntryDto<T>> SetFilesOrder(OrdersRequestDto<T> inDto)
    {
        return fileStorageService.SetOrderAsync(inDto.Items).Select<FileEntry<T>, FileEntryDto<T>>(
            async (e, _) => e.FileEntryType == FileEntryType.Folder ?
                await _folderDtoHelper.GetAsync(e as Folder<T>) :
                await _fileDtoHelper.GetAsync(e as File<T>));
    }

    /// <remarks>
    /// Converts a file into a PDF, stores that PDF as a new file in the folder named in the body, and answers with
    /// the file that was created. The source is left untouched, so the two files then live side by side. `title`
    /// names the result without an extension - the `.pdf` extension is added to it - and an empty title reuses the
    /// name of the source with its extension replaced. The conversion is done by the document service while the
    /// request waits, so the call takes as long as the document needs and answers with the finished file rather than
    /// with a queue entry. The caller needs read access to the source file and the right to create files in the
    /// destination folder, and is otherwise refused; a source file or a destination folder that does not exist is
    /// answered with 404. The call is mutating and not idempotent: each call adds another PDF, its title made unique
    /// when one of that name is already there. The result is marked as new for the room, and for a form the portal
    /// recognises it is stored as a PDF form. To convert in place instead use
    /// `PUT api/2.0/files/file/{fileId}/checkconversion`.
    /// </remarks>
    /// <summary>
    /// Save a file as PDF
    /// </summary>
    /// <path>api/2.0/files/file/{id}/saveaspdf</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The PDF file that was created", typeof(FileDto<int>))]
    [SwaggerResponse(404, "The source file or the destination folder does not exist")]
    [HttpPost("file/{id}/saveaspdf")]
    public async Task<FileDto<T>> SaveFileAsPdf(SaveAsPdfRequestDto<T> inDto)
    {
        return await filesControllerHelper.SaveAsPdf(inDto.Id, inDto.File.FolderId, inDto.File.Title);
    }

    /// <remarks>
    /// Assigns the roles of a PDF form to the people who are to fill them in, and starts the filling: the form is
    /// marked as being filled out, the account that called is recorded as the one who started it, everybody named in
    /// a role is notified, and the form becomes visible to the members whose room rights are limited to filling
    /// forms. Each role carries its name, the account that takes it and the sequence number that decides the turn, so
    /// the same sequence means the roles may be filled in parallel and different ones make a queue. Sending an empty
    /// role list resets the filling instead, dropping the assignment altogether. The whole set is replaced on every
    /// call, so the call is idempotent for a given set of roles but not additive. The file has to be a PDF form lying
    /// in a room; the caller needs the right to start the filling of that form, which the room admin and a member
    /// with content-creator rights have, and is otherwise refused with 403. Read back what was stored with
    /// `GET api/2.0/files/file/{fileId}/formroles`.
    /// </remarks>
    /// <summary>
    /// Save form role mapping
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/formrolemapping</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The roles were stored and the filling was started or reset")]
    [SwaggerResponse(403, "The caller may not start or reset the filling of this form")]
    // The handler reads the id from the body (`formId`), not from the route, so nothing binds this
    // placeholder; the client sends the same value in both places.
    [SwaggerPathParameter("fileId", "The form the role mapping belongs to. Send the same value as the `formId` of the request body, which is the one the handler reads.")]
    [HttpPost("file/{fileId}/formrolemapping")]
    public async Task SaveFormRoleMapping(SaveFormRoleMappingDto<T> inDto)
    {
        await fileStorageService.SaveFormRoleMapping(inDto.FormId, inDto.Roles);
    }

    /// <remarks>
    /// Returns the roles of a PDF form together with the state each of them is in, which is how a client shows who is
    /// expected to fill the form next. Every entry carries the name of the role, the account holding it, the sequence
    /// number that decides the turn and a status: the roles of earlier turns are reported as complete, those of later
    /// turns as waiting, and the role whose turn it is as either yours to fill or already in progress, depending on
    /// whether that person has opened the form; when the filling has been stopped, the role it was interrupted at is
    /// reported as stopped instead. A form whose filling was never started answers with an empty list. The file has
    /// to be a PDF form, or the completed copy of one, and anything else is refused. Read access to the form is
    /// enough, so every member of the room sees the roles, while a caller without access to the room and a guest
    /// outside it are refused with 403 and an unknown file is answered with 404. The operation is read-only. The
    /// assignment itself is written by `POST api/2.0/files/file/{fileId}/formrolemapping`.
    /// </remarks>
    /// <summary>
    /// Get form roles
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/formroles</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The roles of the form with the state of each", typeof(IEnumerable<FormRoleDto>))]
    [SwaggerResponse(403, "The caller has no read access to the form")]
    [SwaggerResponse(404, "No file with this identifier exists")]
    [HttpGet("file/{fileId}/formroles")]
    public IAsyncEnumerable<FormRoleDto> GetAllFormRoles(FileIdRequestDto<T> inDto)
    {
        return fileStorageService.GetAllFormRoles(inDto.FileId);
    }

    /// <remarks>
    /// Drives the filling of a PDF form through its states, the action deciding which way. Action 2 starts the
    /// filling: in a form-filling room the form is opened for filling, the members whose rights are limited to
    /// filling forms are let in, and a form that has been changed since it was last started has the drafts of its
    /// previous round dropped. Action 0 stops it, which in a virtual data room records who interrupted it and at
    /// which role and notifies the people who held the other roles, and in a form-filling room closes the form for
    /// filling. Action 1 resumes a filling that was stopped, clearing that record. Action 3 puts the form back into
    /// editing, closing it for filling and remembering the version it was edited from. The file has to be a PDF form
    /// lying in a room. Starting needs the right to start the filling, which the room admin and a member with
    /// content-creator rights have, while stopping a filling that somebody else started belongs to room managers
    /// alone, so a content creator is refused with 403 there. The call is mutating; the state that resulted is read
    /// with `GET api/2.0/files/file/{fileId}/formroles`.
    /// </remarks>
    /// <summary>
    /// Perform form filling action
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/manageformfilling</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The action was applied to the form")]
    [SwaggerResponse(403, "The caller may not start, stop or resume the filling of this form")]
    // Same shape as formrolemapping above: the id travels in the body, the route placeholder is
    // unbound, and the client fills both with the same value.
    [SwaggerPathParameter("fileId", "The form the action applies to. Send the same value as the `formId` of the request body, which is the one the handler reads.")]
    [HttpPut("file/{fileId}/manageformfilling")]
    public async Task ManageFormFilling(ManageFormFillingDto<T> inDto)
    {
        await fileStorageService.ManageFormFilling(inDto.FormId, inDto.Action);
    }

    /// <remarks>
    /// Returns everything that has been submitted against one PDF form: `metadata` describes the fields of the form,
    /// in the order they are laid out, and `submissions` carries one record per completed copy, each of them holding
    /// the values that were entered. It is the data behind the results table a client shows for a form, and the same
    /// data the spreadsheet report of `POST api/2.0/files/file/{fileId}/xlsx` is built from. Only the submissions of
    /// the version that is currently being filled are reported. The form has to be a PDF form whose filling has been
    /// started and which is still the original form of its room; a form that was never started, a copy of a form and
    /// a form whose room has been moved away are all refused. Read access to the form is enough, so every member of
    /// the room can read the results, while a caller without access to it is refused with 403. The operation is
    /// read-only. The list of roles and whose turn it is comes from `GET api/2.0/files/file/{fileId}/formroles`
    /// instead.
    /// </remarks>
    /// <summary>
    /// Get form submission results
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/submissions</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The submissions collected for the form, with the description of its fields", typeof(FormSubmissionsDto))]
    [SwaggerResponse(403, "The caller has no read access to the form")]
    [HttpGet("file/{fileId}/submissions")]
    public Task<FormSubmissionsDto> GetFormSubmissions(FileIdRequestDto<int> inDto)
    {
        return fileStorageService.GetSubmissionsByFormId(inDto.FileId);
    }

    /// <remarks>
    /// Returns what the caller needs in order to decrypt one file of an end-to-end encrypted private room: `userKeys`
    /// holds the key pairs of the calling account, the private half of each of them encrypted with that person's own
    /// password, and `fileKeys` holds the file keys that were issued to this account for this file, each naming the
    /// public key it was encrypted for. Only the keys of the calling account are ever returned, never those of the
    /// other people in the room. An account that holds no key pair yet, and a file no key was issued for, answer with
    /// empty lists rather than with an error, so an empty `fileKeys` means the caller cannot open that file rather
    /// than that the file is unencrypted. The caller needs read access to the file; a caller without it, and a file
    /// that does not exist, are both refused with 403. The operation is read-only. Keys are issued by
    /// `PUT api/2.0/files/{fileId}/access`, and the personal key pairs are managed under `api/2.0/privacyroom/keys`.
    /// </remarks>
    /// <summary>
    /// Get file encryption information
    /// </summary>
    /// <path>api/2.0/files/{fileId}/access</path>
    /// <param name="fileId">The file whose encryption keys are read. Only a file in an end-to-end encrypted
    /// private room has any.</param>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The key pairs of the caller and the file keys issued to them", typeof(FileEncryptionInfoDto))]
    [SwaggerResponse(400, "The file cannot carry encryption keys")]
    [SwaggerResponse(403, "The caller has no read access to the file")]
    [SwaggerResponse(404, "The file does not exist")]
    [HttpGet("{fileId}/access")]
    public async Task<FileEncryptionInfoDto> GetEncryptionInfoAsync(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        if (!await fileSecurity.CanReadAsync(file))
        {
            throw new InvalidOperationException( FilesCommonResource.ErrorMessage_SecurityException);
        }

        var userKeys = await encryptionKeyPairDtoHelper.GetKeyPairAsync();
        var fileKeys = await fileDao.GetFileKeys(fileId, authContext.CurrentAccount.ID);

        return new FileEncryptionInfoDto
        {
            UserKeys = userKeys,
            FileKeys = fileKeys
        };
    }

    /// <remarks>
    /// Issues the file keys that let the named people open one file of an end-to-end encrypted private room. Each
    /// entry of the body names the account the key is for, the public key it was encrypted with and the encrypted key
    /// itself, so the plain key never reaches the portal: the client encrypts it once per recipient with the public
    /// key that `GET api/2.0/files/file/{fileId}/publickeys` reports for them. The keys of the accounts named in the
    /// request are replaced, and the keys of everybody else are left as they are, which makes the call idempotent for
    /// a given set of recipients while remaining a mutating one; sending no entry for a person does not revoke that
    /// person's key. The file has to lie in a private room, and every account named in the request has to have read
    /// access to it. The caller needs read access to the file and the right to create content in that room, which its
    /// members with editing rights and its admins have; a caller without those rights, a file outside a private room
    /// and a file that does not exist are all refused with 403. Read the result back with
    /// `GET api/2.0/files/{fileId}/access`.
    /// </remarks>
    /// <summary>
    /// Set file encryption information
    /// </summary>
    /// <path>api/2.0/files/{fileId}/access</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file keys were stored")]
    [SwaggerResponse(403, "The caller may not issue keys for this file, or the file is not in a private room")]
    [SwaggerResponse(404, "The file does not exist")]
    [HttpPut("{fileId}/access")]
    public async Task SetEncryptionInfoAsync(AccessRequestDto<T> inDto)
    {
        await fileStorageService.SetEncryptionInfoAsync(inDto.FileId, inDto.Keys.Project());
    }
}

public class FilesControllerCommon(
        GlobalFolderHelper globalFolderHelper,
        FileStorageService fileStorageService,
        FilesControllerHelper filesControllerHelperInternal,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Creates a file in the caller's own "My documents" section and answers with the stored file. The extension in
    /// the title decides the format: an extension of a known text, spreadsheet or presentation format is rewritten to
    /// the portal's own DOCX, XLSX or PPTX, a title with no extension at all gets DOCX added, while an unknown
    /// extension and the few formats the portal keeps as they are stay untouched; `enableExternalExt=true` stores the
    /// title verbatim and skips that rewriting. The content comes from one of three sources, tried in this order:
    /// `formId` copies a ready form out of the form gallery, `templateId` copies an existing file the caller can read
    /// - a number for a file in the portal, a string for one in a connected third-party storage - and with neither of
    /// them the portal's blank template for that format and the caller's language is used. The call is mutating and
    /// not idempotent: each call adds another file. A guest has no "My documents" section of their own, so a guest
    /// cannot use this operation at all, and a template the caller cannot read is refused. To create a file in a
    /// room or any other folder use
    /// `POST api/2.0/files/{folderId}/file`.
    /// </remarks>
    /// <summary>
    /// Create a file in My documents
    /// </summary>
    /// <path>api/2.0/files/@my/file</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created file", typeof(FileDto<int>))]
    [HttpPost("@my/file")]
    public async Task<FileDto<int>> CreateFileInMyDocuments(CreateFile<JsonElement> inDto)
    {
        return await filesControllerHelperInternal.CreateFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.TemplateId, inDto.FormId, inDto.EnableExternalExt);
    }

    /// <remarks>
    /// Creates an HTML (.html) file in the "Common" section with the title and contents specified in the request.
    /// </remarks>
    /// <summary>Create an HTML file in the "Common" section</summary>
    /// <path>api/2.0/files/@common/html</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [HttpPost("@common/html")]
    public async Task<FileDto<int>> CreateHtmlFileInCommon(CreateTextOrHtmlFile inDto)
    {
        return await filesControllerHelperInternal.CreateHtmlFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates an HTML file in the caller's own "My documents" section out of the markup passed as the content, and
    /// answers with the stored file. The `.html` extension is added to the title unless the title already ends with
    /// it, and a request carrying no content is rejected as invalid. `createNewIfExist` acts the other way round than
    /// its name reads: with `true` the file that already carries this title is updated, the markup replacing its
    /// content and a version appearing in its history, while with `false`, which is also the default, another file is
    /// created and its title made unique, as in "Notes (1).html". Updating needs the existing file to be editable by
    /// the caller, so one that is locked, open in an editing session, encrypted or in Trash is left alone and a new
    /// file appears beside it instead. The call is mutating: repeating it with `true` keeps a single file and grows
    /// its history, repeating it with `false` fills the section with numbered copies. A guest has no "My documents"
    /// section and is refused. To create the file in a room or another folder use
    /// `POST api/2.0/files/{folderId}/html`.
    /// </remarks>
    /// <summary>
    /// Create an HTML file in My documents
    /// </summary>
    /// <path>api/2.0/files/@my/html</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created or updated HTML file", typeof(FileDto<int>))]
    [SwaggerResponse(403, "The caller may not create a file in this section")]
    [HttpPost("@my/html")]
    public async Task<FileDto<int>> CreateHtmlFileInMyDocuments(CreateTextOrHtmlFile inDto)
    {
        return await filesControllerHelperInternal.CreateHtmlFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates a text (.txt) file in the "Common" section with the title and contents specified in the request.
    /// </remarks>
    /// <summary>Create a text file in the "Common" section</summary>
    /// <path>api/2.0/files/@common/text</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@common/text")]
    public async Task<FileDto<int>> CreateTextFileInCommon(CreateTextOrHtmlFile inDto)
    {
        return await filesControllerHelperInternal.CreateTextFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates a text file in the caller's own "My documents" section out of the text passed as the content, and
    /// answers with the stored file. The extension follows the content rather than the request: `.txt` normally, but
    /// `.html` as soon as the text contains something shaped like an HTML tag, so a snippet of markup sent here ends
    /// up as an HTML file; the extension is added to the title unless the title already ends with it. A request
    /// carrying no content is rejected as invalid. `createNewIfExist` acts the other way round than its name reads:
    /// with `true` the file that already carries this title is updated and a version appears in its history, while
    /// with `false`, which is also the default, another file is created and its title made unique, as in
    /// "Notes (1).txt". A file that is locked, open in an editing session, encrypted or in Trash is not updated - a
    /// new file appears beside it instead. The call is mutating. A guest has no "My documents" section and is
    /// refused. To create the file in a room or another folder use `POST api/2.0/files/{folderId}/text`.
    /// </remarks>
    /// <summary>
    /// Create a text file in My documents
    /// </summary>
    /// <path>api/2.0/files/@my/text</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created or updated text file", typeof(FileDto<int>))]
    [HttpPost("@my/text")]
    public async Task<FileDto<int>> CreateTextFileInMyDocuments(CreateTextOrHtmlFile inDto)
    {
        return await filesControllerHelperInternal.CreateTextFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Asks the portal to build preview thumbnails for the listed files, and answers at once with the same file ids
    /// that were sent. That answer echoes the request and does not confirm that anything was queued: the work is
    /// handed over to a background worker, and a failure on the way there is written to the log rather than reported
    /// to the caller. Only the file ids of the body are read - the folder ids are ignored, and a request naming no
    /// files at all is answered with an empty list. Ids of files kept in a connected third-party storage are dropped
    /// as well, because the worker handles portal storage only. Access to the individual files is not checked here;
    /// the caller has to be signed in or to reach the portal through an external share link, and an anonymous caller
    /// without such a link is refused. The call is asynchronous and safe to repeat. The thumbnails themselves are not
    /// in the answer: read `thumbnailStatus` and `thumbnailUrl` of the file, for instance with
    /// `GET api/2.0/files/file/{fileId}`, until the status reports the thumbnail as created.
    /// </remarks>
    /// <summary>
    /// Queue file thumbnails
    /// </summary>
    /// <path>api/2.0/files/thumbnails</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file ids from the request, echoed back", typeof(IEnumerable<JsonElement>))]
    [AllowAnonymous]
    [HttpPost("thumbnails")]
    public async Task<IEnumerable<JsonElement>> CreateThumbnails(BaseBatchRequestDto inDto)
    {
        // The endpoint only ever queues files; a request naming no files (folders only, or an
        // empty body) is answered with an empty list rather than an ArgumentNullException.
        if (inDto.FileIds == null)
        {
            return [];
        }

        return await fileStorageService.CreateThumbnailsAsync(inDto.FileIds.ToList());
    }
}
