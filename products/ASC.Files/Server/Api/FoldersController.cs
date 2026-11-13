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
    AuthContext authContext,
    MetadataFilterHelper metadataFilterHelper
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
        apiContext,
        metadataFilterHelper)
{
    private readonly FileStorageService _fileStorageServiceInternal = fileStorageService;
    /// <remarks>
    /// Returns the history of a folder and of the entries inside it. Each record names the action, its time, and the
    /// user who did it. The records of one action form one entry, and its other records are in `related`. The list
    /// starts with the most recent entry. Paging with `startIndex` and `count` counts records, so a page can hold
    /// fewer entries. The response headers carry the total number of records, and the body does not. Requires read
    /// access to the folder. Other callers get 403. A caller who only fills forms in a form-filling folder sees only
    /// the entries available to them. The call is read-only. If the folder does not exist,
    /// the call returns 404. The call returns 400 if `count` is out of range or a date has a wrong format. To get
    /// the history as a file, use `POST api/2.0/files/folder/{folderId}/log/report`.
    /// </remarks>
    /// <summary>
    /// Get folder history
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/log</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "One page of the folder history, the most recent record first", typeof(IAsyncEnumerable<HistoryDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, or `fromDate` or `toDate` is not a date and time ending in `Z` or a UTC offset")]
    [SwaggerResponse(403, "The caller may not read this folder")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpGet("folder/{folderId:int}/log")]
    public IAsyncEnumerable<HistoryDto> GetFolderHistory(HistoryFolderRequestDto inDto)
    {
        return historyApiHelper.GetFolderHistoryAsync(inDto.FolderId, inDto.FromDate, inDto.ToDate, inDto.StartIndex, inDto.Count);
    }

    /// <remarks>
    /// Starts a job that saves the history of a folder as a report file in the caller's "My documents". The file is
    /// an XLSX spreadsheet, or a CSV file if `format` is `Csv`. If both `from` and `to` are left out, the report
    /// covers the whole history. Any user with read access to the folder can call it, except a guest. Other callers
    /// get 403. The call queues work and returns the task, not the report. Repeat
    /// `GET api/2.0/files/folder/{folderId}/log/report` until `isCompleted` is true. Then take the file from
    /// `resultFileId` and `resultFileUrl`. For a CSV file too large for the editor, the URL downloads the file. You can
    /// repeat the call. While a report of the same folder and caller runs, the call returns that task. It starts no
    /// second report. After the report ends, a repeat starts a new report and saves one more file. An XLSX report keeps
    /// at most the 200,000 most recent events by default, and fewer for long events. Its header says how many events it
    /// left out. A CSV report has every event of the period. Only a DocSpace admin gets the IP address, browser, and
    /// platform columns. To stop a running report, use `DELETE api/2.0/files/folder/{folderId}/log/report`. If the
    /// folder does not exist, the call returns 404. It returns 402 only if the login history and audit trail section is
    /// turned off for the portal.
    /// </remarks>
    /// <summary>
    /// Start the folder history report generation
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/log/report</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The queued report task", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(402, "The login history and audit trail section is turned off for this portal")]
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
    /// Returns the state of the history report that the caller started for a folder. Each caller sees only their own
    /// report. Any user with read access to the folder can call it, except a guest. Other callers get 403. The call
    /// is read-only. This is the call to poll after `POST api/2.0/files/folder/{folderId}/log/report`. Repeat it
    /// until `isCompleted` is true. It becomes true however the job ends. If the job fails, `error` gives the reason.
    /// After a success, `resultFileId`, `resultFileName`, and `resultFileUrl` name the file in "My
    /// documents". If the caller has no report for this folder, the response is empty. If the folder does not exist,
    /// the call returns 404. It returns 402 if the login history and audit trail section is turned off for the
    /// portal.
    /// </remarks>
    /// <summary>
    /// Get the folder history report generation status
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/log/report</path>
    /// <param name="folderId">The ID of the folder that was passed to
    /// `POST api/2.0/files/folder/{folderId}/log/report`.</param>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The state of the report task, or nothing when there is none", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(402, "The login history and audit trail section is turned off for this portal")]
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
    /// Stops the history report that the caller started for a folder with
    /// `POST api/2.0/files/folder/{folderId}/log/report`. Each caller can stop only their own report. Any user with
    /// read access to the folder can call it, except a guest. Other callers get 403. The call only queues a request
    /// to stop the job and returns no body. To see the task end as cancelled, use
    /// `GET api/2.0/files/folder/{folderId}/log/report`. If no report runs, the call changes nothing, so you can
    /// repeat it. A finished report stays, and its file remains in "My documents". If the folder does not exist, the
    /// call returns 404. It returns 402 if the login history and audit trail section is turned off for the portal.
    /// </remarks>
    /// <summary>
    /// Terminate the folder history report generation
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/log/report</path>
    /// <param name="folderId">The ID of the folder that was passed to
    /// `POST api/2.0/files/folder/{folderId}/log/report`.</param>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The request to stop the report was accepted")]
    [SwaggerResponse(402, "The login history and audit trail section is turned off for this portal")]
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
    /// Returns the fields of the completed forms in the "Complete" folder of a form-filling room. Each field has a
    /// key and a value type. To filter by a field, pass a pair as `formsItemKey` and `formsItemType`
    /// of `GET api/2.0/files/{folderId}`. That call then keeps only the completed forms whose field has a value.
    /// Anyone can call it without signing in. The call is read-only. It reads the fields from the search index of
    /// one completed form. So the list fills only after the portal indexes the first submitted form. For any other
    /// folder, an unknown folder, or a folder without completed forms, the call returns an empty list.
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
    /// Rebuilds the spreadsheet with the answers to a form from the "Complete" folder of a form-filling room. Requires
    /// the right to update the results of the form, which a room manager has. Other callers get 403. The call changes
    /// data and queues work. You can repeat it. A repeat during a run returns that run. A later repeat rewrites the same
    /// spreadsheet. If that spreadsheet was deleted or moved, it creates a new one. The call returns the original form
    /// and the queued job in `task`. In `isNewFile`, it says whether it creates a new spreadsheet or rewrites the
    /// existing one. The file is not ready yet. Repeat `GET api/2.0/files/file/{fileId}/xlsx` with the ID of the form
    /// until the task is completed. The call returns 404 if the folder does not exist, holds no completed form, or the
    /// original form is deleted. It also returns 403 for another kind of folder or room, or a form whose filling is not
    /// started.
    /// </remarks>
    /// <summary>
    /// Generate XLSX report by folder
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/xlsx</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The queued report task together with the form the answers belong to", typeof(XlsxReportResponseDto))]
    [SwaggerResponse(403, "The folder is not a completed-forms folder, the room of the submitted copy is not a form-filling room, the caller may not maintain the form, or the filling of the form is not started")]
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
    ApiContext apiContext,
    MetadataFilterHelper metadataFilterHelper)
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
        apiContext,
        metadataFilterHelper);

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
    ApiContext apiContext,
    MetadataFilterHelper metadataFilterHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Creates a folder in the parent folder named in the path. Requires the right to create content in the parent
    /// folder. Other callers get 403. The room manager, a content creator, and the owner of a personal section have
    /// this right. The call changes data. Do not repeat it. Each call creates one more folder. In portal storage, every
    /// new folder has the same title. In a connected storage, a taken title gets a free name instead. The portal
    /// notifies the room members about each new folder. Take the ID of the new folder from `id`. To upload files into
    /// it, use `POST api/2.0/files/{folderId}/upload`. To create a room, use `POST api/2.0/files/rooms`. The call
    /// returns 400 if the title is empty, has only spaces, or is too long. It returns 403 if the parent does not exist
    /// or is in the archive. It also returns 403 in the Rooms, Forms, and AI agents section roots, which hold only
    /// rooms. For a string ID that is not a folder of a connected storage, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Create a folder
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}</path>
    [RaisesWebhook(WebhookTrigger.FolderCreated)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The folder that was created", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title`, or the title is empty, blank or longer than 165 characters")]
    [SwaggerResponse(403, "The caller may not create content in the parent folder, or the parent does not exist, lies in the archive or is a section root that holds only rooms")]
    [SwaggerResponse(404, "The parent folder id is a string that is not the id of a folder in a known third-party storage")]
    [HttpPost("folder/{folderId}")]
    public async Task<FolderDto<T>> CreateFolder(CreateFolderRequestDto<T> inDto)
    {
        var folder = await fileStorageService.CreateFolderAsync(inDto.FolderId, inDto.Folder.Title);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <remarks>
    /// Queues a job that deletes a folder with all its contents. It returns the file operations of the caller,
    /// including the new job. Requires the right to delete the folder. Other callers get 403. The room manager and a
    /// content creator who owns the folder have this right. Editing access is not enough, and a guest also gets 403.
    /// The call queues work and changes data. Do not repeat it. A repeat deletes for good the folder that the first
    /// call moved to the trash. After a deletion for good, a repeated call returns 404. The folder still exists when
    /// the response arrives. Repeat `GET api/2.0/files/fileops` until the job has `finished` set to true. Then read its
    /// `error` to see if the deletion failed. If `deleteAfter` is true, `GET api/2.0/files/fileops` does not list the
    /// job after it finishes. By default, the job moves the folder to the trash, where you can restore it. If
    /// `immediately` is true, the deletion is final. In a room and in the trash, the deletion is always final. The same
    /// applies to the root folder of a connected storage. If a file inside is blocked, for example open for editing,
    /// the job deletes nothing. The call also returns 403 for a room if `immediately` is not true. If the folder does
    /// not exist, the call returns 404. To delete several items in one call, use `PUT api/2.0/files/fileops/delete`.
    /// </remarks>
    /// <summary>
    /// Delete a folder
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.FolderTrashed, When = "for the folder only, when the queued job moves it to the trash")]
    [RaisesWebhook(WebhookTrigger.FolderDeleted, When = "instead when the job deletes the folder for good")]
    [RaisesWebhook(WebhookTrigger.RoomDeleted, When = "instead when the folder is a room")]
    [RaisesWebhook(WebhookTrigger.AgentDeleted, When = "instead when it is an AI agent")]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The file operations of the caller, including the deletion just queued", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "The caller may not delete the folder, or the folder is a room and `immediately` is not set")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpDelete("folder/{folderId}")]
    public async IAsyncEnumerable<FileOperationDto> DeleteFolder(DeleteFolderRequestDto<T> inDto)
    {
        await fileOperationsManager.Publish([inDto.FolderId], [], false, !inDto.Delete.DeleteAfter, inDto.Delete.Immediately);

        foreach (var e in await fileOperationsManager.GetOperationResults())
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Moves a folder to a position among the entries of its parent folder. Requires editing access to the folder.
    /// Other callers get 403. The room manager and content creators have this access. The call changes data. You can
    /// repeat it with the same body. The entry at that position and all entries after it move down by one. A
    /// position after the end puts the folder last. The order matters only in rooms with indexing turned on.
    /// Elsewhere the call saves it, but nothing uses it. The response has the new position in `order`. The call
    /// returns 400 if `order` is below 1 or has a wrong format. If the folder does not exist, the call returns 404.
    /// To move several entries in one call, use `PUT api/2.0/files/order`.
    /// </remarks>
    /// <summary>
    /// Set folder order
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}/order</path>
    [RaisesWebhook(WebhookTrigger.FolderUpdated, When = "when the order changes")]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The folder with the position it now holds", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read, or `order` is below 1 or is neither a number nor a dotted path ending in one")]
    [SwaggerResponse(403, "The caller may not reorder this folder")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpPut("folder/{folderId}/order")]
    public async Task<FolderDto<T>> SetFolderOrder(OrderFolderRequestDto<T> inDto)
    {
        var folder = await fileStorageService.SetFolderOrder(inDto.FolderId, inDto.Order.Order);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <remarks>
    /// Returns one page of the contents of a folder. The response has the subfolders in `folders`, the files in
    /// `files`, and the folder itself in `current`. The chain of parent folders is in `pathParts`. Requires read
    /// access to the folder. Other callers get 403. Without signing in, a caller needs an external link to the
    /// folder. The call changes data of the caller only. It clears the new-item marks of the folder. It also saves a
    /// valid `sortBy` and `sortOrder` as the default order of the account. You can repeat it. With a filter, the
    /// call searches the whole subtree by default. The text filter also searches indexed document content. Indexing
    /// runs in the background, so a new file can be missing from the results at first. Use `count` and `startIndex`
    /// to page through the result. The `total` field counts all matching entries. To filter by metadata with a typed
    /// body, use `POST api/2.0/files/{folderId}/search`. The call returns 400 for a wrong parameter type, a `count`
    /// out of range, or a negative `startIndex`. An invalid metadata filter also returns 400. It also returns 400 if `roomId` is not a
    /// number but the folder ID is. It also returns 403 for a folder in the trash. If the folder does not exist, the
    /// call returns 404. For an anonymous caller, it returns 403 instead.
    /// </remarks>
    /// <summary>
    /// Get a folder by ID
    /// </summary>
    /// <path>api/2.0/files/{folderId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "One page of the folder contents, with the folder itself and the chain of its parents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, or the `roomId` is not a number while the folder id is one")]
    [SwaggerResponse(403, "The caller may not read this folder, the folder lies inside Trash, or an anonymous caller asks for a folder that does not exist")]
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

        MetadataFilter metadataFilter = null;

        if (inDto.MetadataTemplateId.HasValue || !string.IsNullOrEmpty(inDto.MetadataFilters) || inDto.IncludeHiddenMetadata == true)
        {
            // the endpoint is anonymous and the filter names the templates and the fields of the tenant: the access to the
            // folder is checked before the filter is parsed, so a caller without it cannot probe them through the validation errors
            await fileStorageService.GetFolderAsync(inDto.FolderId);

            metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);
        }

        var folder = await folderContentDtoHelper.GetAsync(inDto.FolderId, inDto.UserIdOrGroupId, inDto.SharedBy, inDto.FilterType, inDto.RoomId, true, inDto.WithSubFolders ?? true, inDto.ExcludeSubject, inDto.ApplyFilterOption, inDto.SearchArea, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, split, formsItemDto, inDto.Location, inDto.FolderType, metadataFilter);
        return folder.NotFoundIfNull();
    }

    /// <remarks>
    /// Returns one page of the contents of a folder, filtered by metadata. The response has the same shape as for
    /// `GET api/2.0/files/{folderId}`. Requires read access to the folder. Other callers get 403. The call changes
    /// data of the caller only. It clears the new-item marks of the folder. It also saves a valid `sortBy` as the
    /// default order of the account. The call ignores an unknown `sortBy`. You can repeat it. All conditions of
    /// `metadataFilters` must match. Without a template, conditions, and `filterValue`, the call lists the folder
    /// unfiltered. By default, the search covers the whole subtree. Use `count` and `startIndex` to page through the
    /// result. The `total` field counts all matches. Take the template and field IDs from
    /// `GET api/2.0/files/metadata/templates`. A folder of a connected storage has no metadata, so a filter there
    /// returns an empty page. The call returns 400 for an unknown template or field, or a field outside the named
    /// template. It also returns 400 for more than 20 conditions or a filter in the Templates or Private section. If
    /// the folder does not exist, the call returns 404. To combine metadata with the user, extension, or form filters,
    /// use `GET api/2.0/files/{folderId}`.
    /// </remarks>
    /// <summary>
    /// Search a folder by metadata
    /// </summary>
    /// <path>api/2.0/files/{folderId}/search</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "One page of the folder contents that match the filter, with the folder itself and the chain of its parents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "Invalid metadata filter, or a metadata filter on a section that cannot apply it")]
    [SwaggerResponse(403, "You don't have enough permission to view the folder content")]
    [SwaggerResponse(404, "The required folder was not found")]
    [HttpPost("{folderId}/search")]
    public async Task<FolderContentDto<T>> SearchFolder(SearchFolderRequestDto<T> inDto)
    {
        var search = inDto.Search;

        var metadataFilter = await metadataFilterHelper.ParseAsync(search.MetadataTemplateId, search.MetadataFilters, search.IncludeHiddenMetadata ?? false);

        var folder = await folderContentDtoHelper.GetAsync(inDto.FolderId, null, null, search.FilterType, default, true, search.WithSubFolders ?? true, null, null, null, search.SortBy, search.SortOrder, search.StartIndex, search.Count, search.FilterValue, metadataFilter: metadataFilter);
        return folder.NotFoundIfNull();
    }

    /// <remarks>
    /// Returns the properties of one folder or room, without its contents. Requires read access to the folder. Other
    /// callers get 403. Without signing in, a caller needs an external link to the folder. Without such a link, the
    /// call returns 401. The call is read-only. It does not clear the new-item marks of the folder. If the folder
    /// does not exist, the call returns 404. To list the contents, use `GET api/2.0/files/{folderId}`. To get the
    /// chain of parent folders, use `GET api/2.0/files/folder/{folderId}/path`.
    /// </remarks>
    /// <summary>
    /// Get folder information
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The folder itself - its title, its parent, the moments it was created and changed, the access the caller has to it, how many items in it are new for them, and the room settings when the folder is a room; nothing about the items it holds", typeof(FolderDto<int>))]
    [SwaggerResponse(401, "An anonymous caller has no external link that grants access to the folder")]
    [SwaggerResponse(403, "The caller may not read this folder")]
    [SwaggerResponse(404, "The folder does not exist")]
    [AllowAnonymous]
    [HttpGet("folder/{folderId}")]
    public async Task<FolderDto<T>> GetFolderInfo(FolderIdRequestDto<T> inDto)
    {
        var folder = (await fileStorageService.GetFolderAsync(inDto.FolderId)).NotFoundIfNull("Folder not found");

        var result = await _folderDtoHelper.GetAsync(folder, contextFolder: folder);

        // the row a client re-reads after a socket event carries the same metadata the listing shows
        await _folderDtoHelper.SetAssignedMetadataTemplatesAsync(result);

        return result;
    }

    /// <remarks>
    /// Returns the chain of folders from the section root down to the given folder. The folder itself is the last
    /// entry. Use it for a breadcrumb trail or to find the section of a folder. Requires read access to the folder.
    /// Other callers get 403. The call is read-only. The chain has only the folders that the caller can see. So a user
    /// with access to a deep folder gets a shorter chain than the room manager. The call returns the whole chain,
    /// without paging. If the folder does not exist, the call returns 404. To list the contents of a folder, use
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
    [SwaggerResponse(404, "The folder does not exist")]
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
    /// Returns the subfolders directly inside a folder, ordered by title. The list has no files and no contents of the
    /// subfolders. Requires read access to the folder. Other callers get 403. The call is read-only, and it does not
    /// clear the new-item marks. The list has only the subfolders that the caller can see. The call returns the
    /// whole list in one response, without paging and filters. If the folder does not exist or was deleted for good, the call
    /// returns 404. To page, filter, or get the files too, use `GET api/2.0/files/{folderId}`.
    /// </remarks>
    /// <summary>
    /// Get subfolders
    /// </summary>
    /// <path>api/2.0/files/{folderId}/subfolders</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The direct subfolders of the folder, ordered by title", typeof(IAsyncEnumerable<FileEntryBaseDto>))]
    [SwaggerResponse(403, "The caller may not read this folder")]
    [SwaggerResponse(404, "The folder does not exist")]
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
    /// Returns the entries of a folder that are new for the caller. These are files and folders created or changed
    /// since the caller last opened the folder. The list starts with the most recently changed entry. Each user gets
    /// their own list. Requires read access to the folder. Other callers get 403. The call is read-only. It does not
    /// clear the new-item marks, so a repeated call returns the same entries. To clear them, open the folder with
    /// `GET api/2.0/files/{folderId}`. A mark disappears when its entry is deleted or moved out of reach. The call
    /// returns the whole list in one response, without paging and filters. It also returns 403 for a folder in the trash. If
    /// the folder does not exist, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Get new folder items
    /// </summary>
    /// <path>api/2.0/files/{folderId}/news</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The entries of the folder that are new for the caller", typeof(IAsyncEnumerable<FileEntryBaseDto>))]
    [SwaggerResponse(403, "The caller may not read this folder, or the folder lies inside Trash")]
    [SwaggerResponse(404, "The folder does not exist")]
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
    /// Renames a folder or a room. Requires the right to rename the folder. Other callers get 403. The room manager, a
    /// content creator who owns the folder, and the owner of a personal section have this right. For a room, the
    /// call requires the right to edit the room. A guest gets 403 in any case. The call changes data. If the title is
    /// the same as the current one, the call changes nothing, so you can repeat it. In a connected storage, the
    /// folder ID can change with the title. So take the ID from the response. The call returns 400 if the title is
    /// empty, has only spaces, or is too long. It also returns 403 for a folder in the trash or in the archive. If
    /// the folder does not exist, the call returns 404. To change the other settings of a room, use
    /// `PUT api/2.0/files/rooms/{id}`.
    /// </remarks>
    /// <summary>
    /// Rename a folder
    /// </summary>
    /// <path>api/2.0/files/folder/{folderId}</path>
    [RaisesWebhook(WebhookTrigger.FolderUpdated, When = "when the title changes")]
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "instead when the folder is a room")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead when it is an AI agent")]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The folder with its new title", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title`, or the title is empty, blank or longer than 165 characters")]
    [SwaggerResponse(403, "The caller may not rename this folder, or the folder lies in Trash or in the archive")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpPut("folder/{folderId}")]
    public async Task<FolderDto<T>> RenameFolder(CreateFolderRequestDto<T> inDto)
    {
        var folder = await fileStorageService.FolderRenameAsync(inDto.FolderId, inDto.Folder.Title);

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <remarks>
    /// Returns the storage space that documents take in each section of the portal, in bytes. The sections are My
    /// documents, Trash, Rooms, Archive, and AI agents if that feature is on. They come in a fixed order. The
    /// figures cover the whole portal, not the account of the caller. A file in the trash still takes space until the
    /// trash is emptied. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. The call is read-only. The response does not include the portal quota or storage
    /// outside documents.
    /// </remarks>
    /// <summary>
    /// Get used space of files
    /// </summary>
    /// <path>api/2.0/files/filesusedspace</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The space taken by documents in each section, in bytes", typeof(FilesStatisticsResultDto))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [HttpGet("filesusedspace")]
    public async Task<FilesStatisticsResultDto> GetFilesUsedSpace()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        return await fileStorageService.GetFilesUsedSpace();
    }

    /// <remarks>
    /// Returns the primary external link of a folder or a room, and creates it if it does not exist. The call uses
    /// the body only when it creates the link. If the link exists, the call returns it unchanged. Requires the right
    /// to manage the links of the folder. Other callers get 403. The room manager has this right, but a content
    /// creator does not. The call changes data only when it creates the link. You can repeat it, and later calls
    /// return the same link. To change a link, use `PUT api/2.0/files/folder/{id}/links`. If `access` grants nothing,
    /// the call creates no link and returns an empty response. Read the address from `sharedTo.shareLink`. The call
    /// returns 400 if the title or password is too long, or the password breaks the portal policy. It also returns
    /// 400 if `expirationDate` is more than 10 years ahead. It also returns 403 if folder links cannot have this
    /// access level. The link limit and a ban on external links outside public rooms also return 403. If the folder
    /// does not exist, the call returns 404. If the primary link was revoked, the call creates a new one with a new
    /// ID. Until then, `GET api/2.0/files/folder/{id}/link` returns 404.
    /// </remarks>
    /// <summary>
    /// Create the folder primary external link
    /// </summary>
    /// <path>api/2.0/files/folder/{id}/link</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The primary external link of the folder", typeof(FileShareDto))]
    [SwaggerResponse(400, "The title or password is longer than 255 characters, the password does not meet the portal password policy, or `expirationDate` lies more than 10 years ahead")]
    [SwaggerResponse(403, "The caller may not manage the links of this folder, the access level is not available for links to this folder, the link limit is reached, or the admin restricts external links to public rooms")]
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
            password: inDto.FolderLink.Password,
            recreateRevoked: true);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Returns the primary external link of a folder or a room, and creates it if the folder has none. A new link
    /// gives read access, with no password and no expiry. Read the address from `sharedTo.shareLink`. Requires the
    /// right to manage the links of the folder. Other callers get 403. The room manager has this right, but a user
    /// with read access does not. A caller who is not signed in and has no external link gets 401. The first call
    /// can change data. Later calls are read-only and return the same link. If the primary link was revoked, the call
    /// returns 404 and creates no new one. If the folder does not exist, the call also returns 404. If a string `id`
    /// points to a storage account that is not connected, the call returns 400. To list all external links of the
    /// folder, use `GET api/2.0/files/folder/{id}/links`.
    /// </remarks>
    /// <summary>
    /// Get the folder primary external link
    /// </summary>
    /// <path>api/2.0/files/folder/{id}/link</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The primary external link of the folder", typeof(FileShareDto))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(401, "An anonymous caller has no external link")]
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
    /// Creates or changes an external link of a folder or a room, or revokes it. The `linkId` field selects the link. A new
    /// or empty ID creates a link, and an existing ID changes that link. The call applies all fields every time, so a
    /// missing field gets its default value. Requires the right to manage the links of the folder. Other callers
    /// get 403. The room manager has this right. The call changes data. A repeated call with an existing `linkId`
    /// changes nothing. A repeated call with an empty `linkId` creates one more link. If `access` grants nothing, the
    /// call revokes the link and returns an empty response. A later read does not recreate a revoked primary link.
    /// In a public room or a form-filling room, revoking the primary link creates a new one with default settings.
    /// The call then returns the new link. A repeated revocation finds no link and returns an empty response. If the
    /// link limit is reached, it returns 403 instead. The call returns 400 if the title or password is too long, or the
    /// password breaks the portal policy. It also returns 400 if `expirationDate` is more than 10 years ahead. It also
    /// returns 403 if folder links cannot have this access level or the link limit is reached. The admin's restriction
    /// on external links can also return 403. If the folder does not exist, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Set the folder external link
    /// </summary>
    /// <path>api/2.0/files/folder/{id}/links</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The link as it now stands, or nothing when it was revoked", typeof(FileShareDto))]
    [SwaggerResponse(400, "The title or password is longer than 255 characters, the password does not meet the portal password policy, or `expirationDate` lies more than 10 years ahead")]
    [SwaggerResponse(403, "The caller may not manage the links of this folder, the access level is not available for links to this folder, the link limit is reached, or the admin's restriction on external links forbids the change")]
    [SwaggerResponse(404, "The folder does not exist")]
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
    /// Returns the external links of a folder or a room, including the primary link. The call returns at most the
    /// first 100 links, without paging. Requires read access if the folder is a room. Other callers get 403. A caller
    /// who can read the room but may not see its links gets an empty list. For other folders, a caller who cannot
    /// manage the links gets an empty list instead of 403. So an empty list does not prove that the folder has no
    /// links. The call is read-only. If the folder does not exist, the call returns 404. To change or revoke a link,
    /// pass its ID to `PUT api/2.0/files/folder/{id}/links`. To get only the primary link, use
    /// `GET api/2.0/files/folder/{id}/link`.
    /// </remarks>
    /// <summary>
    /// Get folder external links
    /// </summary>
    /// <path>api/2.0/files/folder/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The external links of the folder, or an empty list when the caller may not see them", typeof(IAsyncEnumerable<FileShareDto>))]
    [SwaggerResponse(403, "The folder is a room that the caller cannot read")]
    [SwaggerResponse(404, "The folder does not exist")]
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
    SettingsManager settingsManager,
    MetadataFilterHelper metadataFilterHelper,
    AppSettingsService appSettingsService,
    TenantManager tenantManager)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    private const string PrivateRoomsAppId = "e2e-rooms";

    /// <remarks>
    /// Returns one page of the files and folders in the "Common" section. Any signed-in user can call it. The call
    /// is read-only, but a valid `sortBy` becomes the account's default order. The `total` field counts
    /// all matching entries. The call returns 400 if `metadataTemplateId` or
    /// `metadataFilters` names an unknown template or field, or has a malformed condition.
    /// </remarks>
    /// <summary>Get the "Common" section</summary>
    /// <path>api/2.0/files/@common</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Common\" section with one page of its contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, or the metadata filter names an unknown template or field")]
    [SwaggerResponse(403, "The caller is not allowed to read the \"Common\" section")]
    [SwaggerResponse(404, "The \"Common\" section does not exist on the portal")]
    [HttpGet("@common")]
    public async Task<FolderContentDto<int>> GetCommonFolder(GetCommonFolderRequestDto inDto)
    {
        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderCommonAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, metadataFilter: metadataFilter);
    }

    /// <remarks>
    /// Returns one page of the files and folders that the caller marked as favorite. Each account has its own
    /// favorites, so the marks of other members do not show here. Any signed-in user can call it. The call is
    /// read-only, but a valid `sortBy` becomes the account's default order. The list shows folders
    /// first, then files, each in the `sortBy` order or the saved default. The `total` field
    /// counts all matching entries. The list skips entries in the trash and entries that the caller can no longer
    /// read. Their favorite marks stay. The call returns 400 if `metadataTemplateId` or `metadataFilters` names an
    /// unknown template or field, or has a malformed condition. To add or remove favorites, use
    /// `POST api/2.0/files/favorites` or `DELETE api/2.0/files/favorites`. To mark one file, you can also use
    /// `GET api/2.0/files/favorites/{fileId}`.
    /// </remarks>
    /// <summary>Get the "Favorites" section</summary>
    /// <path>api/2.0/files/@favorites</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Favorites\" section with one page of the entries the caller marked as favorite", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, or the metadata filter names an unknown template or field")]
    [HttpGet("@favorites")]
    public async Task<FolderContentDto<int>> GetFavoritesFolder(GetFavoritesFolderRequestDto inDto)
    {
        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderFavoritesAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, metadataFilter: metadataFilter);
    }

    /// <remarks>
    /// Returns one page of the caller's personal "My documents" section. Other members
    /// do not see its entries until the owner shares them. Any signed-in user can call it. A guest usually has no
    /// personal section, and the call then returns 404. The call is read-only, but a valid `sortBy` becomes
    /// the account's default order. Without filters, the call lists only the top level of the section. With
    /// `filterType`, `userIdOrGroupId`, `filterValue`, or a metadata filter, it searches the whole section. It
    /// matches `filterValue` against titles and indexed document content. A new file can be missing from that
    /// search until the portal indexes it. The `total` field counts all matching entries.
    /// The call returns 400 if `metadataTemplateId` or `metadataFilters` names an unknown template or field, or has
    /// a malformed condition. To open a folder of the section, use `GET api/2.0/files/{folderId}`.
    /// </remarks>
    /// <summary>Get the "My documents" section</summary>
    /// <path>api/2.0/files/@my</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"My documents\" section with one page of its contents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, or the metadata filter names an unknown template or field")]
    [SwaggerResponse(404, "This account has no personal section")]
    [HttpGet("@my")]
    public async Task<FolderContentDto<int>> GetMyFolder(GetMyTrashFolderRequestDto inDto)
    {
        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderMyAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, inDto.ApplyFilterOption, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, metadataFilter: metadataFilter);
    }

    /// <remarks>
    /// Returns one page of the files and folders in the "In projects" section. The section exists only while the
    /// Projects module is turned on. Any signed-in user can call it. The call is read-only, but a valid
    /// `sortBy` becomes the account's default order. The `total` field counts all matching entries. The call
    /// ignores the metadata parameters.
    /// </remarks>
    /// <summary>Get the "In projects" section</summary>
    /// <path>api/2.0/files/@projects</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"In projects\" section with one page of its contents", typeof(FolderContentDto<string>))]
    [SwaggerResponse(403, "The caller is not allowed to read the \"In projects\" section")]
    [SwaggerResponse(404, "The \"In projects\" section does not exist on the portal")]
    [HttpGet("@projects")]
    public async Task<FolderContentDto<string>> GetProjectsFolder(GetCommonFolderRequestDto inDto)
    {
        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.GetFolderProjectsAsync<string>(), inDto.UserIdOrGroupId, null, inDto.FilterType, null, true, true, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
    }

    /// <remarks>
    /// Returns one page of the files that the caller opened recently. Each account has its own history, and the
    /// section holds files only, so `folders` is empty. Any signed-in user can call it. The call is read-only, but
    /// a valid `sortBy` becomes the account's default order. The call always sorts the list by the last
    /// opening time, newest first. So `sortBy` and `sortOrder` do not change the order here. Without `searchArea`, or
    /// with `Active`, the call lists files from personal sections and rooms, except form-filling rooms. With
    /// `searchArea` set to `Forms`, it lists only files from form-filling rooms. If `searchArea` is `PrivateRooms`,
    /// the call returns 400. Any other `searchArea` value lists all recent files. The call ignores
    /// `applyFilterOption`. The `total` field counts all matching files. The call also
    /// returns 400 if `metadataTemplateId` or `metadataFilters` names an unknown template or field, or has a
    /// malformed condition. The portal adds a file to the section when the caller opens it. To add a file without
    /// opening it, use `POST api/2.0/files/file/{fileId}/recent`. To clear the history, use
    /// `DELETE api/2.0/files/recent`. This call works whether `PUT api/2.0/files/displayrecent` shows or hides the
    /// section.
    /// </remarks>
    /// <summary>Get the "Recent" section</summary>
    /// <path>api/2.0/files/recent</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Recent\" section with one page of the files the caller opened lately", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, `searchArea` is `PrivateRooms`, or the metadata filter names an unknown template or field")]
    [HttpGet("@recent")]
    [HttpGet("recent")]
    public async Task<FolderContentDto<int>> GetRecentFolder(GetRecentFolderRequestDto inDto)
    {
        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderRecentAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, inDto.ExcludeSubject, inDto.ApplyFilterOption, inDto.SearchArea, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, inDto.Extension, metadataFilter: metadataFilter);
    }

    /// <remarks>
    /// Returns every top-level section that the caller can see, each with its first page of content. Sections come
    /// in this order: "Favorites", "Recent", "Shared with me", "My documents", "Trash", "Rooms", "Forms", and
    /// "Archive". While the private rooms app is on, "Private rooms" comes before "Archive". While AI access is on for
    /// the portal, the AI agents and AI hub sections follow. The list has
    /// "Recent" only after `PUT api/2.0/files/displayrecent` turns it on. It skips "My documents" if the account has
    /// none, which is usual for a guest. To skip "Trash", set `withoutTrash` to `true`. Any signed-in user can call
    /// it. The call is read-only, but a valid `sortBy` becomes the account's default order. It does not clear
    /// new-item marks. The filters, `count`, and `startIndex` apply to each section. So `count=1` returns one entry
    /// per section, and each section has its own `total`. This is the most expensive listing of the module. When you
    /// need one section, read it directly, for example with `GET api/2.0/files/@my`. The call returns 400 if `count`
    /// is outside its allowed range.
    /// </remarks>
    /// <summary>Get filtered sections</summary>
    /// <path>api/2.0/files/@root</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The sections available to the caller, each with one page of its content", typeof(IAsyncEnumerable<FolderContentDto<int>>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, or the `startIndex` is negative")]
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
    /// Returns one page of the "Shared with me" section. It holds the files and folders that other members shared with
    /// the caller. Any signed-in user can call it. The call is read-only, but a valid `sortBy` becomes the
    /// account's default order. If the order is `DateAndTime`, the call sorts by `New` instead. The `total` field
    /// counts all matching entries. The call returns 400 if `metadataTemplateId` or `metadataFilters` names an
    /// unknown template or field, or has a malformed condition.
    /// </remarks>
    /// <summary>Get the "Shared with me" section</summary>
    /// <path>api/2.0/files/@share</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Shared with me\" section with one page of the entries shared with the caller", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, or the metadata filter names an unknown template or field")]
    [SwaggerResponse(403, "The caller is not allowed to read the \"Shared with me\" section")]
    [SwaggerResponse(404, "This account has no \"Shared with me\" section")]
    [HttpGet("@share")]
    public async Task<FolderContentDto<int>> GetShareFolder(GetCommonFolderRequestDto inDto)
    {
        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderShareAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, metadataFilter: metadataFilter);
    }

    /// <remarks>
    /// Returns one page of the files in the "Templates" section. Any signed-in user can call it, except a guest. A
    /// guest has no such section and gets 404. The call is read-only, but a valid `sortBy` becomes the
    /// account's default order. The `total` field counts all matching files. The section cannot apply a
    /// metadata filter. So if `metadataTemplateId` or `metadataFilters` is set, the call returns 400.
    /// </remarks>
    /// <summary>Get the "Templates" section</summary>
    /// <path>api/2.0/files/@templates</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Templates\" section with one page of its files", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, or a metadata filter is set on a section that cannot apply it")]
    [SwaggerResponse(403, "The caller is not allowed to read the \"Templates\" section")]
    [SwaggerResponse(404, "This account has no \"Templates\" section, as for a guest")]
    [HttpGet("@templates")]
    public async Task<FolderContentDto<int>> GetTemplatesFolder(GetCommonFolderRequestDto inDto)
    {
        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderTemplatesAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, metadataFilter: metadataFilter);
    }

    /// <remarks>
    /// Returns one page of the caller's "Trash" section, which holds the entries that this account deleted. Each
    /// account has its own trash. Any signed-in user can call it. An account without its own trash gets 404. The call
    /// is read-only, but a valid `sortBy` becomes the account's default order. The call lists only the
    /// top level, so it does not expand deleted folders. It matches `filterValue` against titles only, not document
    /// content. The `total` field counts all matching entries. The call returns 400 if `metadataTemplateId` or
    /// `metadataFilters` names an unknown template or field, or has a malformed condition. To restore an entry, move
    /// it with `PUT api/2.0/files/fileops/move`. To delete all entries for good, use
    /// `PUT api/2.0/files/fileops/emptytrash`. Both calls start a background job. Then repeat
    /// `GET api/2.0/files/fileops` until `finished` is true.
    /// </remarks>
    /// <summary>Get the "Trash" section</summary>
    /// <path>api/2.0/files/@trash</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Trash\" section with one page of the entries the caller deleted", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, or the metadata filter names an unknown template or field")]
    [SwaggerResponse(404, "This account has no \"Trash\" section")]
    [HttpGet("@trash")]
    public async Task<FolderContentDto<int>> GetTrashFolder(GetMyTrashFolderRequestDto inDto)
    {
        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await folderContentDtoHelper.GetAsync(Convert.ToInt32(await globalFolderHelper.FolderTrashAsync), inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, true, false, inDto.ApplyFilterOption, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, metadataFilter: metadataFilter);
    }

    /// <remarks>
    /// Returns one page of the "Forms" section, the list of form-filling rooms that the caller can read. The
    /// call does not open the rooms, so `files` is empty. To read a room, use `GET api/2.0/files/{folderId}` with
    /// its ID. Any signed-in user can call it. The call is read-only, but a valid `sortBy` becomes the
    /// account's default order. The `total` field counts all matching rooms. The call returns 400 if
    /// `metadataTemplateId` or `metadataFilters` names an unknown template or field, or has a malformed condition. In
    /// `GET api/2.0/files/rooms`, these rooms show only when `searchArea` is `Forms`.
    /// </remarks>
    /// <summary>Get the "Forms" section</summary>
    /// <path>api/2.0/files/@forms</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The \"Forms\" section with one page of the form-filling rooms available to the caller", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, or the metadata filter names an unknown template or field")]
    [HttpGet("@forms")]
    public async Task<FolderContentDto<int>> GetFormsFolder(GetCommonFolderRequestDto inDto)
    {
        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderFormsAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, false, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, metadataFilter: metadataFilter);
    }

    /// <remarks>
    /// Returns the private rooms section: the flat list of end-to-end encrypted rooms the caller is a member of. Such
    /// rooms are stored under the "Rooms" tree but are surfaced only here, so `GET api/2.0/files/rooms` leaves them out of
    /// the active area and lists them when `searchArea` names the private rooms area instead. An administrator who is not
    /// a member does not see a private room here either. The section is not expanded into room content, so `folders`
    /// carries the rooms while `files` comes back empty; to read what is inside one of them, call
    /// `GET api/2.0/files/{folderId}` with the room identifier. Nothing is modified, though passing `sortBy` saves the
    /// requested order as the default order for this account. `filterType`, `filterValue`, `userIdOrGroupId` and the
    /// sorting parameters narrow and order the room list, `metadataTemplateId` and `metadataFilters` keep only the rooms
    /// carrying the template and matching the conditions, `count` and `startIndex` page through it, `total` reports how
    /// many rooms match the request in full, and `current` describes the section folder itself. Archived private rooms
    /// stay in the archive area; narrow it with `privacyFilter` of `GET api/2.0/files/rooms`.
    /// </remarks>
    /// <summary>Get the private rooms section</summary>
    /// <path>api/2.0/files/@private</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The private rooms section with one page of the private rooms the caller is a member of", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, or the metadata filter names an unknown template or field")]
    [SwaggerResponse(403, "The caller is not allowed to read the private rooms section")]
    [SwaggerResponse(404, "The end-to-end encrypted rooms app is turned off on this portal")]
    [HttpGet("@private")]
    public async Task<FolderContentDto<int>> GetPrivateRoomsFolder(GetCommonFolderRequestDto inDto)
    {
        if (!await IsPrivateRoomsAppEnabledAsync())
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await folderContentDtoHelper.GetAsync(await globalFolderHelper.FolderPrivateRoomsAsync, inDto.UserIdOrGroupId, null, inDto.FilterType, 0, true, false, false, ApplyFilterOption.All, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, metadataFilter: metadataFilter);
    }

    private async Task<bool> IsPrivateRoomsAppEnabledAsync()
    {
        var app = await appSettingsService.GetAppAsync(tenantManager.GetCurrentTenantId(), PrivateRoomsAppId);

        return app is { Enabled: true };
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

        if (await IsPrivateRoomsAppEnabledAsync())
        {
            yield return await globalFolderHelper.FolderPrivateRoomsAsync;
        }

        yield return await globalFolderHelper.FolderArchiveAsync;

        var aiAccessSettings = await aiAccessSettingsTask;
        if (aiAccessSettings.Enabled)
        {
            yield return await globalFolderHelper.FolderAiAgentsAsync;
            yield return await globalFolderHelper.FolderAiHubAsync;
        }
    }
}
