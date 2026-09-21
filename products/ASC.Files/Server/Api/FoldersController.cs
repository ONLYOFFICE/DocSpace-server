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
public class FoldersControllerInternal(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    BreadCrumbsManager breadCrumbsManager,
    FolderContentDtoHelper folderContentDtoHelper,
    FileStorageService fileStorageService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    PermissionContext permissionContext,
    FileShareDtoHelper fileShareDtoHelper,
    HistoryApiHelper historyApiHelper,
    FormFillingReportCreator formFillingReportCreator,
    ApiContext apiContext,
    TenantManager tenantManager,
    DocumentBuilderTaskManager<AuditReportTask, int, AuditReportTaskData> documentBuilderTaskManager,
    IEventBus eventBus,
    IServiceProvider serviceProvider,
    CommonLinkUtility commonLinkUtility,
    AuthContext authContext
    )
    : FoldersController<int>(
        daoFactory,
        fileSecurity,
        breadCrumbsManager,
        folderContentDtoHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        permissionContext,
        fileShareDtoHelper,
        apiContext)
{
    private readonly FileStorageService _fileStorageServiceInternal = fileStorageService;
    /// <remarks>
    /// Lists what has happened to a folder and to the entries inside it - creations, renames, uploads, moves,
    /// deletions and changes of access - each record naming the action, the moment it happened and the member behind
    /// it. Records that belong to one action are grouped, so a batch arrives as a single entry carrying the rest of
    /// itself in `related`, and the list runs from the most recent record backwards. `fromDate` and `toDate` narrow
    /// the period, `startIndex` and `count` page through the result, and the number of records matching the request
    /// is reported in the response headers rather than in the body. Any member who can read the folder may read its
    /// history; a caller without access is answered with 403 and a folder that does not exist with 404. When the
    /// folder is a form-filling folder the caller reached through a filling invitation, the history is narrowed to
    /// what that caller may see. The call is read-only. To take the same history away as a spreadsheet, start a
    /// report with `POST api/2.0/files/folder/{folderId}/log/report`.
    /// </remarks>
    /// <summary>
    /// Get folder history
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/log</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "One page of the folder history, the most recent record first", typeof(IAsyncEnumerable<HistoryDto>))]
    [SwaggerResponse(403, "The caller may not read this folder")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpGet("folder/{folderId:int}/log")]
    public IAsyncEnumerable<HistoryDto> GetFolderHistory(HistoryFolderRequestDto inDto)
    {
        return historyApiHelper.GetFolderHistoryAsync(inDto.FolderId, inDto.FromDate, inDto.ToDate, inDto.StartIndex, inDto.Count);
    }

    /// <remarks>
    /// Queues a background job that renders the history of a folder into a spreadsheet, or into a CSV file when
    /// `format` asks for one, and saves the result in the caller's "My documents". The answer is the queued task, not
    /// the report: poll `GET api/2.0/files/folder/{folderId}/log/report` until `isCompleted` is true, then take the
    /// file from `resultFileId`, `resultFileName` and `resultFileUrl`, of which a CSV report fills only the last two.
    /// `from` and `to` limit the exported period; leaving both out exports the whole history. While a report for the
    /// same folder and caller is still running, this call joins it and answers with the running task instead of
    /// starting a second one, so retrying is safe. The caller needs read access to the folder and may not be a guest,
    /// and the portal plan has to include the audit feature - otherwise the call is refused, with 403 for the access
    /// rule and 404 for a folder that does not exist. Only a portal administrator gets the address, browser and
    /// platform columns. Give up a running report with `DELETE api/2.0/files/folder/{folderId}/log/report`.
    /// </remarks>
    /// <summary>
    /// Start the folder history report generation
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/log/report</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The queued report task", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller may not export the history of this folder")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpPost("folder/{folderId:int}/log/report")]
    public async Task<DocumentBuilderTaskDto> CreateReportFolderHistoryAsync(FolderHistoryReportRequestDto inDto)
    {
        await historyApiHelper.DemandFolderHistoryReportPermissionAsync(inDto.FolderId);

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        var task = serviceProvider.GetRequiredService<AuditReportTask>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null, DocumentBuilderTaskManager.GetTaskId(tenantId, userId, AuditReportTask.GetTaskDiscriminator(AuditReportKind.FolderHistory, inDto.FolderId)));

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request)?
            .ToDictionary(x => x.Key, x => x.Value.ToString()) ?? [];

        var evt = new AuditReportIntegrationEvent(userId, tenantId, baseUri, AuditReportKind.FolderHistory, inDto.Format, inDto.From, inDto.To, headers, folderId: inDto.FolderId);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    /// <remarks>
    /// Reports how far the history report of a folder has got, and is the operation to poll after
    /// `POST api/2.0/files/folder/{folderId}/log/report` has queued one. `percentage` climbs to 100, `isCompleted`
    /// turns true when the job is over however it ended, `error` carries the reason when it failed, and
    /// `resultFileId`, `resultFileName` and `resultFileUrl` name the file that was saved in the caller's "My
    /// documents" - a CSV report leaving the identifier empty. An empty answer means there is no report for this
    /// folder and caller, either because none was started or because a finished one has already been picked up by an
    /// earlier poll. The caller needs read access to the folder and may not be a guest, and the portal plan has to
    /// include the audit feature; a caller who fails the access rule is answered with 403 and a folder that does not
    /// exist with 404. The call is read-only, and each caller sees only their own report.
    /// </remarks>
    /// <summary>
    /// Get the folder history report generation status
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/log/report</path>
    /// <param name="folderId">The folder whose history report is being polled. It is the folder that was
    /// passed to the operation that started the report.</param>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The state of the report task, or nothing when there is none", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller may not export the history of this folder")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpGet("folder/{folderId:int}/log/report")]
    public async Task<DocumentBuilderTaskDto> GetReportFolderHistoryAsync(int folderId)
    {
        await historyApiHelper.DemandFolderHistoryReportPermissionAsync(folderId);

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        var task = await documentBuilderTaskManager.GetTask(tenantId, userId, AuditReportTask.GetTaskDiscriminator(AuditReportKind.FolderHistory, folderId));

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Gives up the history report the caller has started for a folder with
    /// `POST api/2.0/files/folder/{folderId}/log/report`. The request only asks the background worker to stop, and
    /// the answer carries no body, so a following `GET api/2.0/files/folder/{folderId}/log/report` is what shows the
    /// task ending as cancelled. Asking to terminate when nothing is running is accepted and changes nothing, which
    /// makes the call safe to repeat. A report that has already finished is not undone by this call and its file
    /// stays in "My documents". The caller needs read access to the folder and may not be a guest, and the portal
    /// plan has to include the audit feature; a caller who fails the access rule is answered with 403 and a folder
    /// that does not exist with 404. Each caller can only terminate their own report.
    /// </remarks>
    /// <summary>
    /// Terminate the folder history report generation
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/log/report</path>
    /// <param name="folderId">The folder whose running history report is to be given up. It is the folder that
    /// was passed to the operation that started the report.</param>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The request to stop the report was accepted")]
    [SwaggerResponse(403, "The caller may not export the history of this folder")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpDelete("folder/{folderId:int}/log/report")]
    public async Task TerminateReportFolderHistoryAsync(int folderId)
    {
        await historyApiHelper.DemandFolderHistoryReportPermissionAsync(folderId);

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        var evt = new AuditReportIntegrationEvent(userId, tenantId, null, AuditReportKind.FolderHistory, AuditReportFormat.Xlsx, null, null, terminate: true, folderId: folderId);

        await eventBus.PublishAsync(evt);
    }

    /// <remarks>
    /// Lists the fields the completed forms of a form-filling room carry, each of them a key and the kind of value
    /// behind it, so that a client can offer them as filters. Feed a pair from this list back as `formsItemKey` and
    /// `formsItemType` of `GET api/2.0/files/{folderId}` to keep only the completed forms whose field of that name
    /// holds a value. The fields are read from the search index of one of the forms already gathered, so they appear
    /// once indexing has caught up with the first submission. Only the "Complete" folder of a form-filling room
    /// carries such fields: for any other folder, for a folder that does not exist and for one that has been deleted
    /// the answer is an empty list rather than a refusal, and the same holds while nothing has been submitted yet.
    /// The operation reads the index alone, changes nothing and needs no authorization.
    /// </remarks>
    /// <summary>
    /// Get folder form filter
    /// </summary>
    /// <path>api/2.0/files/{folderId}/formfilter</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [AllowAnonymous]
    [SwaggerResponse(200, "The form fields that can be used as filters, empty when the folder carries none", typeof(IEnumerable<FormsItemDto>))]
    [HttpGet("{folderId:int}/formfilter")]
    public async Task<IEnumerable<FormsItemDto>> GetFolder(FolderIdRequestDto<int> inDto)
    {
        return (await formFillingReportCreator.GetFormsFields(inDto.FolderId)).Select(r => new FormsItemDto(r.Key, r.Type));
    }

    /// <remarks>
    /// Rebuilds the spreadsheet that gathers the answers submitted to a form, starting from the "Complete" folder
    /// that holds the filled copies. The answer names the original form the results belong to, says in `isNewFile`
    /// whether the spreadsheet is being created or an existing one rewritten in place, and carries the queued job in
    /// `task`; the file itself is not ready yet, so poll `GET api/2.0/files/file/{fileId}/xlsx` with the identifier
    /// of the form until the task reports completion. The folder has to be the "Complete" folder of a form-filling
    /// room and has to hold at least one submitted copy whose original form still exists, and the caller needs the
    /// right to maintain that form, which the room manager has. A folder that does not exist, or one that holds
    /// nothing to report on, is answered with 404, and a folder of the wrong kind or a caller without those rights
    /// with 403. The call is mutating: it writes the results file of the form.
    /// </remarks>
    /// <summary>
    /// Generate XLSX report by folder
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/xlsx</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The queued report task together with the form the answers belong to", typeof(XlsxReportResponseDto))]
    [SwaggerResponse(403, "The folder is not a completed-forms folder, or the caller may not maintain the form")]
    [SwaggerResponse(404, "The folder, the submitted copy or the original form was not found")]
    [HttpPost("folder/{folderId:int}/xlsx")]
    public async Task<XlsxReportResponseDto> GenerateXlsxByFolder(FolderIdRequestDto<int> inDto)
    {
        var (task, form, isNewFile) = await _fileStorageServiceInternal.GenerateXlsxByFolderAsync(inDto.FolderId);

        return new XlsxReportResponseDto
        {
            Form = await _fileDtoHelper.GetAsync(form),
            Task = DocumentBuilderTaskDto.Get(task),
            IsNewFile = isNewFile
        };
    }
}

[ConstraintRoute("thirdparty", AffectsOrder = false)]
public class FoldersControllerThirdparty(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    BreadCrumbsManager breadCrumbsManager,
    FolderContentDtoHelper folderContentDtoHelper,
    FileStorageService fileStorageService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    PermissionContext permissionContext,
    FileShareDtoHelper fileShareDtoHelper,
    ApiContext apiContext)
    : FoldersController<string>(
        daoFactory,
        fileSecurity,
        breadCrumbsManager,
        folderContentDtoHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        permissionContext,
        fileShareDtoHelper,
        apiContext);

public abstract class FoldersController<T>(
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    BreadCrumbsManager breadCrumbsManager,
    FolderContentDtoHelper folderContentDtoHelper,
    FileStorageService fileStorageService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    PermissionContext permissionContext,
    FileShareDtoHelper fileShareDtoHelper,
    ApiContext apiContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Creates a folder inside the folder named in the path and answers with the folder as it was stored. The title
    /// is trimmed, may not be blank and is refused when it is longer than the limit the schema prints; titles are not
    /// required to be unique, so creating the same title twice leaves two folders side by side, which makes the call
    /// mutating and not idempotent. The caller needs the right to create content in the parent, which the room
    /// manager, a content creator and the owner of a personal section have; a member without that right, an archived
    /// parent, and a section root that only holds rooms - "Rooms", "Forms" and "AI agents" - are all refused, as is a
    /// parent that does not exist. Rooms are not created here: use `POST api/2.0/files/rooms` for those, and this
    /// operation for ordinary folders within them. Members of the room are notified of the new folder. Read the
    /// identifier of the new folder from `id` and fill it with `POST api/2.0/files/{folderId}/upload`.
    /// </remarks>
    /// <summary>
    /// Create a folder
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The folder that was created", typeof(FolderDto<int>))]
    [HttpPost("folder/{folderId}")]
    public async Task<FolderDto<T>> CreateFolder(CreateFolderRequestDto<T> inDto)
    {
        var folder = await fileStorageService.CreateFolderAsync(inDto.FolderId, inDto.Folder.Title);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <remarks>
    /// Queues the deletion of one folder together with everything inside it, and answers with the file operations of
    /// the caller, the one just created among them. The folder is not gone when the response arrives: poll
    /// `GET api/2.0/files/fileops` until the operation reports `finished`, and read its `error` to learn whether the
    /// deletion succeeded. By default the folder is moved to the "Trash" section, from where it can be restored;
    /// `immediately=true` discards it for good instead, and inside a room, where there is no Trash, deletion is
    /// always final. `deleteAfter=true` postpones the deletion until the editing sessions on the contents have ended,
    /// so files somebody is working on are not pulled away. The caller needs the right to delete the folder, which
    /// the room manager, a portal administrator acting as room manager and a content creator acting on a folder of
    /// their own have; editing access alone, read access and a guest are refused. The call is destructive. To delete
    /// several items at once use `PUT api/2.0/files/fileops/delete`.
    /// </remarks>
    /// <summary>
    /// Delete a folder
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The file operations of the caller, including the deletion just queued", typeof(IAsyncEnumerable<FileOperationDto>))]
    [HttpDelete("folder/{folderId}")]
    public async IAsyncEnumerable<FileOperationDto> DeleteFolder(DeleteFolder<T> inDto)
    {
        await fileOperationsManager.Publish([inDto.FolderId], [], false, !inDto.Delete.DeleteAfter, inDto.Delete.Immediately);

        foreach (var e in await fileOperationsManager.GetOperationResults())
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Puts a folder at a given position among the entries of its parent and answers with the folder, its `order`
    /// reporting where it now stands. Positions count from 1, and the entry that held the wanted position, together
    /// with everything after it, is shifted to make room, so the numbering of the parent stays without gaps; a
    /// position beyond the end places the folder last. The value may also be sent as a dotted path, as in "1.2.3", in
    /// which case only its last segment is read. Ordering is what the manual arrangement of a room is built on, and
    /// it only means something in rooms whose contents are indexed - elsewhere the value is stored and ignored. The
    /// caller needs edit access to the folder, which room managers and content creators have, and a member without it
    /// is refused, while a folder that does not exist is answered as not found. The call is mutating and idempotent.
    /// To move several entries in one go use `PUT api/2.0/files/order`.
    /// </remarks>
    /// <summary>
    /// Set folder order
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/order</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The folder with the position it now holds", typeof(FolderDto<int>))]
    [HttpPut("folder/{folderId}/order")]
    public async Task<FolderDto<T>> SetFolderOrder(OrderFolderRequestDto<T> inDto)
    {
        var folder = await fileStorageService.SetFolderOrder(inDto.FolderId, inDto.Order.Order);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <remarks>
    /// Returns one page of the contents of a folder - its subfolders in `folders`, its files in `files`, the folder
    /// itself in `current` and the chain of parents in `pathParts` - and is the operation a client browses the file
    /// tree with. `filterType`, `filterValue`, `extension`, `userIdOrGroupId`, `sharedBy` and `folderType` narrow
    /// what is listed, `applyFilterOption` decides whether those filters bite on the files, on the folders or on
    /// both, and `withSubFolders`, which is on unless it is switched off, lets a narrowed request descend through the
    /// whole subtree instead of the top level alone. `filterValue` is matched against titles and against indexed
    /// document content, and indexing is asynchronous, so a file uploaded a moment ago can be missing from a search
    /// for a short while. `count` and `startIndex` page through the result while `total` counts everything that
    /// matches, and `sortBy` with `sortOrder` both order the page and are saved as the default order of the account.
    /// Reading a room or an ordinary folder clears its new-item marks for the caller. A caller who may not read the
    /// folder is answered with 403, and a folder that does not exist with 404.
    /// </remarks>
    /// <summary>
    /// Get a folder by ID
    /// </summary>
    /// <path>api/2.0/files/{folderId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "One page of the folder contents, with the folder itself and the chain of its parents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "The caller may not read this folder")]
    [SwaggerResponse(404, "The folder does not exist")]
    [AllowAnonymous]
    [HttpGet("{folderId}")]
    public async Task<FolderContentDto<T>> GetFolderByFolderId(GetFolderRequestDto<T> inDto)
    {
        var split = inDto.Extension == null ? [] : inDto.Extension.Split(",");
        FormsItemDto formsItemDto = null;
        if (!string.IsNullOrEmpty(inDto.FormsItemKey) || !string.IsNullOrEmpty(inDto.FormsItemType))
        {
            formsItemDto = new FormsItemDto(inDto.FormsItemKey, inDto.FormsItemType);
        }

        var folder = await folderContentDtoHelper.GetAsync(inDto.FolderId, inDto.UserIdOrGroupId, inDto.SharedBy, inDto.FilterType, inDto.RoomId, true, inDto.WithSubFolders ?? true, inDto.ExcludeSubject, inDto.ApplyFilterOption, inDto.SearchArea, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, split, formsItemDto, inDto.Location, inDto.FolderType);
        return folder.NotFoundIfNull();
    }

    /// <remarks>
    /// Returns one folder as an object - its title, its parent, the moments it was created and last changed, the
    /// access the caller has to it, the number of items that are new for them, and the room settings when the folder
    /// is a room - without listing anything inside it. Use it to resolve a folder identifier into something
    /// displayable, and `GET api/2.0/files/{folderId}` when the contents are what is wanted; unlike that operation,
    /// this one leaves the new-item marks of the folder alone. Any member who can read the folder may call it, and an
    /// anonymous caller only through an external link that grants access, everybody else being refused; a folder that
    /// does not exist is answered as not found. The call is read-only. The chain of parents above the folder is not
    /// part of the answer and is read with `GET api/2.0/files/folder/{folderId}/path`.
    /// </remarks>
    /// <summary>
    /// Get folder information
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The folder", typeof(FolderDto<int>))]
    [AllowAnonymous]
    [HttpGet("folder/{folderId}")]
    public async Task<FolderDto<T>> GetFolderInfo(FolderIdRequestDto<T> inDto)
    {
        var folder = (await fileStorageService.GetFolderAsync(inDto.FolderId)).NotFoundIfNull("Folder not found");

        return await _folderDtoHelper.GetAsync(folder, contextFolder: folder);
    }

    /// <remarks>
    /// Returns the chain of folders that leads to the folder named in the path, ordered from the section root down to
    /// the folder itself, which is the last entry. It is what a breadcrumb trail is built from, and it also tells a
    /// client which section - a room, the personal section, the archive - a bare folder identifier belongs to. Only
    /// the folders the caller may see are part of the chain, so a member who was given access to a folder deep inside
    /// a room gets a shorter path than the room manager does. The caller needs read access to the folder and is
    /// otherwise answered with 403, while a folder that does not exist is answered as not found. The call is
    /// read-only and takes no paging parameters. To go the other way, from a folder down into its contents, call
    /// `GET api/2.0/files/{folderId}`.
    /// </remarks>
    /// <summary>
    /// Get the folder path
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/path</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The chain of folders leading to the folder, the section root first", typeof(IAsyncEnumerable<FileEntryBaseDto>))]
    [SwaggerResponse(403, "The caller may not read this folder")]
    [HttpGet("folder/{folderId}/path")]
    public async IAsyncEnumerable<FileEntryBaseDto> GetFolderPath(FolderIdRequestDto<T> inDto)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(inDto.FolderId);

        if (folder == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanReadAsync(folder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFolder);
        }

        var breadCrumbs = await breadCrumbsManager.GetBreadCrumbsAsync(inDto.FolderId);

        foreach (var e in breadCrumbs)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }

    /// <remarks>
    /// Lists the folders that sit directly inside the folder named in the path, ordered by title, without their own
    /// contents and without the files that lie beside them. The whole list arrives at once - there are no paging or
    /// filtering parameters here - so for a large folder, or when the files are wanted as well, use
    /// `GET api/2.0/files/{folderId}`, which pages and filters. A folder that holds no subfolders answers with an
    /// empty list. The caller needs read access to the folder, and only the subfolders they may see are listed, so a
    /// member of a room can get fewer entries than its manager; a caller without access is answered with 403, and a
    /// folder that does not exist, or one that has been deleted for good, is answered as not found. The call is
    /// read-only and leaves the new-item marks of the folder alone.
    /// </remarks>
    /// <summary>
    /// Get subfolders
    /// </summary>
    /// <path>api/2.0/files/{folderId}/subfolders</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The direct subfolders of the folder, ordered by title", typeof(IAsyncEnumerable<FileEntryBaseDto>))]
    [SwaggerResponse(403, "The caller may not read this folder")]
    [HttpGet("{folderId}/subfolders")]
    public async IAsyncEnumerable<FileEntryBaseDto> GetFolders(FolderIdRequestDto<T> inDto)
    {
        var folders = await fileStorageService.GetFoldersAsync(inDto.FolderId);
        foreach (var folder in folders)
        {
            yield return await GetFileEntryWrapperAsync(folder);
        }
    }

    /// <remarks>
    /// Lists the entries of a folder that are new for the calling member - the files and folders created or changed
    /// there since they last opened it - ordered from the most recently changed backwards. It is what the badge of a
    /// room is filled from, and it is personal: two members of the same room get different answers. Reading this list
    /// does not clear the marks, so the same entries come back until the folder itself is opened with
    /// `GET api/2.0/files/{folderId}`, which does clear them. A folder with nothing new answers with an empty list,
    /// and marks disappear on their own when the entry behind them is deleted or moved out of reach. The caller needs
    /// read access to the folder and is otherwise answered with 403. The whole list arrives at once, without paging
    /// or filtering, and the call is read-only.
    /// </remarks>
    /// <summary>
    /// Get new folder items
    /// </summary>
    /// <path>api/2.0/files/{folderId}/news</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The entries of the folder that are new for the caller", typeof(IAsyncEnumerable<FileEntryBaseDto>))]
    [SwaggerResponse(403, "The caller may not read this folder")]
    [HttpGet("{folderId}/news")]
    public async IAsyncEnumerable<FileEntryBaseDto> GetNewFolderItems(FolderIdRequestDto<T> inDto)
    {
        var newItems = await fileStorageService.GetNewItemsAsync(inDto.FolderId);

        foreach (var e in newItems)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }

    /// <remarks>
    /// Gives a folder a new title and answers with the folder as it now stands. The title is trimmed, may not be
    /// blank and is refused when it is longer than the limit the schema prints; a title that matches the current one
    /// leaves the folder untouched, and titles need not be unique among the neighbours. The caller needs the right to
    /// rename the folder, which the room manager, a content creator acting on a folder of their own and the owner of
    /// a personal section have, while a guest is refused with 403 whatever their access; a folder in the "Trash"
    /// section or in an archived room cannot be renamed either, and a folder that does not exist is answered as
    /// not found. A room may be renamed here as well, in which case the caller needs the right to edit the
    /// room, and `PUT api/2.0/files/rooms/{id}` is the operation that changes its other settings. The call is
    /// mutating and idempotent; on a folder stored in a connected third-party account the identifier of the folder
    /// may change with the title.
    /// </remarks>
    /// <summary>
    /// Rename a folder
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The folder with its new title", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "The caller may not rename this folder")]
    [HttpPut("folder/{folderId}")]
    public async Task<FolderDto<T>> RenameFolder(CreateFolderRequestDto<T> inDto)
    {
        var folder = await fileStorageService.FolderRenameAsync(inDto.FolderId, inDto.Folder.Title);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <remarks>
    /// Reports how much storage the portal spends on documents, split by section - "My documents", "Trash", "Rooms",
    /// "Archive" and, where the feature is on, "AI agents" - each entry naming the section and the space it takes in
    /// bytes. The figures cover the whole portal rather than the calling account, and moving an entry between
    /// sections moves its space with it, which is why deleting a file to the Trash does not free anything until the
    /// Trash is emptied. Only a caller who may change portal settings, that is the owner and the portal
    /// administrators, is allowed here; a room administrator, an ordinary member and a guest are all refused. The
    /// call is read-only, takes no parameters and answers with the sections in a fixed order. The quota of the portal
    /// as a whole, storage outside documents included, is not part of this answer.
    /// </remarks>
    /// <summary>
    /// Get used space of files
    /// </summary>
    /// <path>api/2.0/files/filesusedspace</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The space taken by documents in each section, in bytes", typeof(FilesStatisticsResultDto))]
    [HttpGet("filesusedspace")]
    public async Task<FilesStatisticsResultDto> GetFilesUsedSpace()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await fileStorageService.GetFilesUsedSpace();
    }

    /// <remarks>
    /// Answers with the primary external link of a folder or a room, creating it on the first call and returning the
    /// one that already exists afterwards, so the operation is idempotent in effect: a second call with other
    /// parameters does not reconfigure the existing link, and changing one is the business of
    /// `PUT api/2.0/files/folder/{id}/links`. The parameters therefore only shape the link at the moment it is born -
    /// `access` its rights, `title` its name, `expirationDate` its lifetime, which is unlimited here unless one is
    /// given, `internal` whether only signed-in members may follow it, `denyDownload` whether the contents may only
    /// be viewed, and `password` a secret to be asked for. Sending `access` with the value that grants nothing
    /// creates no link and answers with nothing. The caller needs the right to manage the links of the room the
    /// folder belongs to, which its manager and a portal administrator acting as room manager have, and a member with
    /// content-creator or read access is refused with 403; an unknown folder is answered with 404. Read the address
    /// from `sharedTo.shareLink`.
    /// </remarks>
    /// <summary>
    /// Create the folder primary external link
    /// </summary>
    /// <path>api/2.0/files/folder/{id}/link</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The primary external link of the folder", typeof(FileShareDto))]
    [SwaggerResponse(403, "The caller may not manage the links of this folder")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpPost("folder/{id}/link")]
    public async Task<FileShareDto> CreateFolderPrimaryExternalLink(FolderLinkRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(
            inDto.Id,
            FileEntryType.Folder,
            inDto.FolderLink.Access,
            string.IsNullOrWhiteSpace(inDto.FolderLink.Title) ? null : inDto.FolderLink.Title.Trim(),
            expirationDate: inDto.FolderLink.ExpirationDate,
            requiredAuth: inDto.FolderLink.Internal,
            allowUnlimitedDate: true,
            denyDownload: inDto.FolderLink.DenyDownload,
            password: inDto.FolderLink.Password);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Answers with the primary external link of a folder or a room - the one the "Copy link" action of a client
    /// hands out - with its address in `sharedTo.shareLink`, its rights in `access`, and its title, expiration date,
    /// password flag and download restriction beside them. The link is created on the first read if the folder has
    /// none, with read rights, no password and no expiry, so this operation mutates on that first call and is a plain
    /// read afterwards; repeated calls answer with the same link identifier. The caller needs the right to manage the
    /// links of the room the folder belongs to, which its manager and a portal administrator acting as room manager
    /// have; a member with read access alone is refused with 403 and an anonymous caller is rejected, while a link
    /// that was deliberately revoked is answered with 404 rather than being recreated. The paging parameters are
    /// accepted for compatibility and leave the single link answered here unchanged. Every external link of the same
    /// folder is listed by `GET api/2.0/files/folder/{id}/links`.
    /// </remarks>
    /// <summary>
    /// Get the folder primary external link
    /// </summary>
    /// <path>api/2.0/files/folder/{id}/link</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The primary external link of the folder", typeof(FileShareDto))]
    [SwaggerResponse(403, "The caller may not manage the links of this folder")]
    [SwaggerResponse(404, "The folder does not exist, or its primary link was revoked")]
    [AllowAnonymous]
    [HttpGet("folder/{id}/link")]
    public async Task<FileShareDto> GetFolderPrimaryExternalLink(FolderPrimaryIdRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.Folder, allowUnlimitedDate: true);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Creates an external link to a folder or a room, or changes or revokes an existing one, and answers with the
    /// link as it now stands. `linkId` decides which: an identifier that is not yet in use, the empty one included,
    /// creates a link, while the identifier of an existing link rewrites it, so the whole set of parameters is
    /// applied every time and a field left out is reset rather than kept. `access` carries the rights the link
    /// grants, and `access` set to the value that denies everything revokes the link instead - the answer is then
    /// empty, and a revoked primary link is not recreated by a later read. `title` names the link for the people who
    /// manage it, `expirationDate` limits its lifetime and is ignored when it lies in the past, `password` asks
    /// visitors for a secret, `denyDownload` leaves them with viewing only, `internal` admits signed-in members
    /// alone, and `primary=true` makes it the primary link of the folder. The caller needs the right to manage the
    /// links of the room, which its manager and a portal administrator acting as room manager have; anyone else is
    /// refused and an unknown folder is answered as not found. The call is mutating.
    /// </remarks>
    /// <summary>
    /// Set the folder external link
    /// </summary>
    /// <path>api/2.0/files/folder/{id}/links</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The link as it now stands, or nothing when it was revoked", typeof(FileShareDto))]
    [HttpPut("folder/{id}/links")]
    public async Task<FileShareDto> SetFolderPrimaryExternalLink(FolderLinkRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.SetExternalLinkAsync(
            inDto.Id,
            FileEntryType.Folder,
            inDto.FolderLink.LinkId,
            inDto.FolderLink.Title,
            inDto.FolderLink.Access,
            inDto.FolderLink.ExpirationDate,
            inDto.FolderLink.Password?.Trim(),
            inDto.FolderLink.DenyDownload,
            inDto.FolderLink.Internal,
            inDto.FolderLink.Primary);

        if (linkAce == null)
        {
            return null;
        }

        var result = await fileShareDtoHelper.Get(linkAce);

        if (inDto.FolderLink.LinkId != Guid.Empty && linkAce.Id != inDto.FolderLink.LinkId && result.SharedLink != null)
        {
            result.SharedLink.RequestToken = null;
        }

        return result;
    }

    /// <remarks>
    /// Lists the external links of a folder or a room, each with its identifier, title, address, rights, expiration
    /// date, password flag and download restriction, the primary link among them once it exists. At most the first
    /// hundred links are answered and the number returned is reported in the response headers; there are no paging
    /// parameters here. A folder that has never been shared by link answers with an empty list, and so does a member
    /// who may read the folder but not manage its links - the empty answer therefore means "nothing to show you"
    /// rather than "no links exist". A member without access to the room is refused, an anonymous caller is rejected,
    /// and a folder that does not exist is answered as not found. The call is read-only. Take an identifier from here
    /// to `PUT api/2.0/files/folder/{id}/links` to change or remove that link, and read the primary one alone with
    /// `GET api/2.0/files/folder/{id}/link`.
    /// </remarks>
    /// <summary>
    /// Get folder external links
    /// </summary>
    /// <path>api/2.0/files/folder/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The external links of the folder the caller may manage", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpGet("folder/{id}/links")]
    public async IAsyncEnumerable<FileShareDto> GetFolderLinks(GetFolderLinksRequestDto<T> inDto)
    {
        var counter = 0;

        await foreach (var ace in fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.Folder, ShareFilterType.ExternalLink, null, 0, 100))
        {
            counter++;

            yield return await fileShareDtoHelper.Get(ace);
        }

        apiContext.SetCount(counter);
    }
}

public class FoldersControllerCommon(
    GlobalFolderHelper globalFolderHelper,
    FolderContentDtoHelper folderContentDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    UserManager userManager,
    SecurityContext securityContext,
    FilesSettingsHelper filesSettingsHelper,
    SettingsManager settingsManager)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Returns the detailed list of files and folders located in the "Common" section.
    /// </remarks>
    /// <summary>Get the "Common" section</summary>
    /// <path>api/2.0/files/@common</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Common\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@common")]
    public async Task<FolderContentDto<int>> GetCommonFolder(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderCommonAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
    }

    /// <remarks>
    /// Returns the caller's own "Favorites" section: the files and folders this account has marked as favorite,
    /// together with the section folder itself. Favorites are per-account, so the entries another member marked are
    /// not listed here, and a guest sees only their own, usually empty, list. Mark a single file with
    /// `GET api/2.0/files/favorites/{fileId}`, or add and remove batches of files and folders with
    /// `POST api/2.0/files/favorites` and `DELETE api/2.0/files/favorites`. Nothing in the section is modified,
    /// though passing `sortBy` saves the requested order as the default order for this account. Entries the caller
    /// can no longer read, and entries that have been moved to the "Trash" section, drop out of the listing even
    /// though their favorite mark stays, so the section can shrink without an explicit unmark. `folders` and `files`
    /// hold one page of the section, `total` counts the entries matching the request before `count` and `startIndex`
    /// are applied, and `current` describes the section folder itself.
    /// </remarks>
    /// <summary>Get the "Favorites" section</summary>
    /// <path>api/2.0/files/@favorites</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Favorites\" section with one page of the entries the caller marked as favorite", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "The caller is not allowed to read the \"Favorites\" section")]
    [SwaggerResponse(404, "The \"Favorites\" section could not be resolved for this account")]
    [HttpGet("@favorites")]
    public async Task<FolderContentDto<int>> GetFavoritesFolder(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderFavoritesAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
    }

    /// <remarks>
    /// Returns the contents of the caller's "My documents" section, the personal storage that belongs to this account
    /// alone and stays invisible to other members until something in it is shared explicitly. Any authenticated
    /// member that has a personal section can read it; guest accounts are not given one, and the call then answers
    /// 404. Nothing in the section is modified, though passing `sortBy` saves the requested order as the default
    /// order for this account. Without a filter only the top level of the section is listed; as soon as `filterType`,
    /// `userIdOrGroupId` or `filterValue` narrows the request, the search descends through the whole subtree.
    /// `filterValue` is matched against titles and against indexed document content, and the index is written
    /// asynchronously, so a file uploaded a moment ago can be missing from a search for a short while. `folders` and
    /// `files` hold one page of the result, `total` counts everything that matches before `count` and `startIndex`
    /// are applied, and `current` describes the section folder. To open a folder inside the section, call
    /// `GET api/2.0/files/{folderId}` with its identifier.
    /// </remarks>
    /// <summary>Get the "My documents" section</summary>
    /// <path>api/2.0/files/@my</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"My documents\" section with one page of its contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "The caller is not allowed to read the \"My documents\" section")]
    [SwaggerResponse(404, "This account has no personal section")]
    [HttpGet("@my")]
    public async Task<FolderContentDto<int>> GetMyFolder(GetMyTrashFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderMyAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, inDto.ApplyFilterOption, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
    }

    /// <remarks>
    /// Returns the detailed list of files and folders located in the "In projects" section.
    /// </remarks>
    /// <summary>Get the "In projects" section</summary>
    /// <path>api/2.0/files/@projects</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"In projects\" section contents", typeof(FolderContentDto<string>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@projects")]
    public async Task<FolderContentDto<string>> GetProjectsFolder(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.GetFolderProjectsAsync<string>(), inDto.UserIdOrGroupId, null, inDto.FilterType, null, true, true, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
    }

    /// <remarks>
    /// Returns the "Recent" section: the files the calling account has opened lately. The section holds files only,
    /// so `folders` comes back empty, and it is personal, so another member's history is not visible here. A file is
    /// added when it is opened and can also be added explicitly with `POST api/2.0/files/file/{fileId}/recent`;
    /// `DELETE api/2.0/files/recent` clears the whole history, and `PUT api/2.0/files/displayrecent` switches the
    /// section on and off for the account, which also decides whether `GET api/2.0/files/@root` includes it. Nothing
    /// in the section is modified, though passing `sortBy` saves the requested order as the default order for this
    /// account. The listing is ordered by the moment the caller last opened each file, newest first, and `sortBy` and
    /// `sortOrder` do not change that order. `files` holds one page, `total` counts the files matching the request
    /// before `count` and `startIndex` are applied, and `current` describes the section folder itself.
    /// </remarks>
    /// <summary>Get the "Recent" section</summary>
    /// <path>api/2.0/files/recent</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Recent\" section with one page of the files the caller opened lately", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "The caller is not allowed to read the \"Recent\" section")]
    [SwaggerResponse(404, "The \"Recent\" section could not be resolved for this account")]
    [HttpGet("@recent")]
    [HttpGet("recent")]
    public async Task<FolderContentDto<int>> GetRecentFolder(GetRecentFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderRecentAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, inDto.ExcludeSubject, inDto.ApplyFilterOption, inDto.SearchArea, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, inDto.Extension);
    }

    /// <remarks>
    /// Returns every top-level section the calling account can see in one response, each of them a full section
    /// object carrying its own first page of content: "Favorites", "Recent", "Shared with me", "My documents",
    /// "Trash", "Rooms", "Forms", "Archive" and, while AI access is enabled for the portal, "AI agents". A section is
    /// left out when the account has none of it, which is why a guest gets no personal section, and "Recent" is
    /// listed only while it is switched on with `PUT api/2.0/files/displayrecent`. Pass `withoutTrash=true` to drop
    /// the "Trash" section. The filters, `count` and `startIndex` are applied to each section separately, so
    /// `count=1` returns one entry per section and every section reports its own `total`. Because it builds the
    /// content of all of them, this is the most expensive listing in the module: when a single section is enough,
    /// read it directly, for example with `GET api/2.0/files/@my`. The call modifies nothing in the sections and
    /// leaves their new-item badges untouched, though passing `sortBy` saves the requested order as the default order
    /// for this account.
    /// </remarks>
    /// <summary>Get filtered sections</summary>
    /// <path>api/2.0/files/@root</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The sections available to the caller, each with one page of its content", typeof(IAsyncEnumerable<FolderContentDto<int>>))]
    [SwaggerResponse(403, "The caller is not allowed to read one of the sections")]
    [SwaggerResponse(404, "One of the sections could not be resolved for this account")]
    [HttpGet("@root")]
    public async IAsyncEnumerable<FolderContentDto<int>> GetRootFolders(GetRootFolderRequestDto inDto)
    {
        var foldersIds = GetRootFoldersIdsAsync(inDto.WithoutTrash ?? false);

        await foreach (var folder in foldersIds)
        {
            yield return await folderContentDtoHelper.GetAsync(folder, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, false, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
        }
    }

    /// <remarks>
    /// Returns the detailed list of files and folders located in the "Shared with me" section.
    /// </remarks>
    /// <summary>Get the "Shared with me" section</summary>
    /// <path>api/2.0/files/@share</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Shared with me\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@share")]
    public async Task<FolderContentDto<int>> GetShareFolder(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderShareAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
    }

    /// <remarks>
    /// Returns the detailed list of files located in the "Templates" section.
    /// </remarks>
    /// <summary>Get the "Templates" section</summary>
    /// <path>api/2.0/files/@templates</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Templates\" section contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpGet("@templates")]
    public async Task<FolderContentDto<int>> GetTemplatesFolder(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderTemplatesAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
    }

    /// <remarks>
    /// Returns the caller's "Trash" section: the files and folders this account has deleted, kept there until they
    /// are restored or discarded. Each member has a Trash of their own and sees only what they deleted themselves.
    /// Restore an entry by moving it back with `PUT api/2.0/files/fileops/move`, or discard the whole section with
    /// `PUT api/2.0/files/fileops/emptytrash`; both start a background operation that is polled through
    /// `GET api/2.0/files/fileops`. This call itself modifies nothing, though passing `sortBy` saves the requested
    /// order as the default order for this account. Only the top level of the section is listed, so the contents of a
    /// deleted folder are not expanded into it, and `filterValue` is matched against titles alone here rather than
    /// against document content. `folders` and `files` hold one page of the result, `total` counts everything that
    /// matches before `count` and `startIndex` are applied, and `current` describes the section folder. An account
    /// that is given no Trash of its own, an outsider for instance, receives 404.
    /// </remarks>
    /// <summary>Get the "Trash" section</summary>
    /// <path>api/2.0/files/@trash</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Trash\" section with one page of the entries the caller deleted", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "The caller is not allowed to read the \"Trash\" section")]
    [SwaggerResponse(404, "This account has no \"Trash\" section")]
    [HttpGet("@trash")]
    public async Task<FolderContentDto<int>> GetTrashFolder(GetMyTrashFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(Convert.ToInt32(await globalFolderHelper.FolderTrashAsync), inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, inDto.ApplyFilterOption, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
    }

    /// <remarks>
    /// Returns the "Forms" section: the flat list of form-filling rooms the caller may read. Such rooms are stored
    /// under the "Rooms" tree but are surfaced only here, so `GET api/2.0/files/rooms` leaves them out of the active
    /// area and lists them when `searchArea` names the forms area instead. The section is not expanded into room
    /// content, so `folders` carries the rooms while `files` comes back empty; to read what is inside one of them,
    /// call `GET api/2.0/files/{folderId}` with the room identifier. Nothing is modified, though passing `sortBy`
    /// saves the requested order as the default order for this account. `filterType`, `filterValue`,
    /// `userIdOrGroupId` and the sorting parameters narrow and order the room list, `count` and `startIndex` page
    /// through it, `total` reports how many rooms match the request in full, and `current` describes the section
    /// folder itself.
    /// </remarks>
    /// <summary>Get the "Forms" section</summary>
    /// <path>api/2.0/files/@forms</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Forms\" section with one page of the form-filling rooms available to the caller", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "The caller is not allowed to read the \"Forms\" section")]
    [SwaggerResponse(404, "The \"Forms\" section could not be resolved for this account")]
    [HttpGet("@forms")]
    public async Task<FolderContentDto<int>> GetFormsFolder(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderFormsAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, false, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
    }

    private async IAsyncEnumerable<int> GetRootFoldersIdsAsync(bool withoutTrash)
    {
        var aiAccessSettingsTask = settingsManager.LoadAsync<TenantAiAccessSettings>();
        var isOutsider = await userManager.IsOutsiderAsync(securityContext.CurrentAccount.ID);

        if (isOutsider)
        {
            withoutTrash = true;
        }

        yield return await globalFolderHelper.FolderFavoritesAsync;

        if (await filesSettingsHelper.GetRecentSection())
        {
            yield return await globalFolderHelper.FolderRecentAsync;
        }
        yield return await globalFolderHelper.FolderShareAsync;

        var my = await globalFolderHelper.FolderMyAsync;
        if (my != 0)
        {
            yield return my;
        }

        if (!withoutTrash)
        {
            yield return await globalFolderHelper.FolderTrashAsync;
        }

        yield return await globalFolderHelper.FolderVirtualRoomsAsync;
        yield return await globalFolderHelper.FolderFormsAsync;
        yield return await globalFolderHelper.FolderArchiveAsync;

        var aiAccessSettings = await aiAccessSettingsTask;
        if (aiAccessSettings.Enabled)
        {
            yield return await globalFolderHelper.FolderAiAgentsAsync;
        }
    }
}
