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
public class VirtualRoomsInternalController(
    GlobalFolderHelper globalFolderHelper,
    FileOperationDtoHelper fileOperationDtoHelper,
    CustomTagsService customTagsService,
    RoomLogoManager roomLogoManager,
    FileDeleteOperationsManager fileDeleteOperationsManager,
    FileMoveCopyOperationsManager fileMoveCopyOperationsManager,
    FileStorageService fileStorageService,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileShareDtoHelper fileShareDtoHelper,
    SocketManager socketManager,
    ApiContext apiContext,
    FilesMessageService filesMessageService,
    SettingsManager settingsManager,
    ApiDateTimeHelper apiDateTimeHelper,
    AuthContext authContext,
    TenantManager tenantManager,
    IEventBus eventBus,
    RoomTemplatesWorker roomTemplatesWorker,
    UserManager userManager,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    FolderContentDtoHelper folderContentDtoHelper)
    : VirtualRoomsController<int>(globalFolderHelper,
        fileOperationDtoHelper,
        customTagsService,
        roomLogoManager,
        fileDeleteOperationsManager,
        fileMoveCopyOperationsManager,
        fileStorageService,
        folderDtoHelper,
        fileDtoHelper,
        fileShareDtoHelper,
        socketManager,
        apiContext,
        filesMessageService,
        settingsManager,
        apiDateTimeHelper,
        userManager,
        authContext,
        daoFactory,
        fileSecurity)
{
    private readonly AuthContext _authContext = authContext;

    /// <remarks>
    /// Creates a room in the Rooms section and returns it. The call creates an Instructions room, a Knowledge room,
    /// or an AI project in the AI rooms section instead. You cannot change `roomType` after this call. Requires a DocSpace admin
    /// or a room admin. Other callers get 403. The call changes data. Do not repeat it. Each call creates another room.
    /// A repeat with the same `logo` returns 404 but still creates a room. That room has no logo, tags, or primary
    /// external link. The call adds the tag names that the portal does not have yet. A public room and a form filling
    /// room get a primary external link. To invite members, use `PUT api/2.0/files/rooms/{id}/share` after this call.
    /// The call returns 400 if `title` is blank, `cover` is unknown, or `share` is not empty. It also returns 400 if
    /// `private` is set together with `indexing`, a lifetime, or a watermark. A room of the AI rooms section also
    /// returns 400 with settings that do not apply to its type. The call returns 402 if the portal reached the room
    /// limit of its plan or its payment is overdue. Rooms of the AI rooms section do not count toward this limit. A
    /// logo that does not fit into the storage quota also returns 402. The call returns 403 if `quota` is set while
    /// room quotas are turned off, or is over the portal limit. It also returns 403 for a public room while the portal
    /// forbids external sharing. A private room also returns 403 if the caller has no encryption keys. A Knowledge room
    /// returns 403 while vectorization is turned off. If `logo.tmpFile` names no uploaded picture, or one already used,
    /// the call returns 404.
    /// </remarks>
    /// <summary>Create a room</summary>
    /// <path>api/2.0/files/rooms</path>
    [RaisesWebhook(WebhookTrigger.RoomCreated, When = "when `roomType` is not `AiRoom`")]
    [RaisesWebhook(WebhookTrigger.AgentCreated, When = "instead when `roomType` is `AiRoom`, which creates an AI agent in the Rooms section")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The created room with its id, type, settings, logo and tags", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title` or `roomType`, `private` is set together with `indexing`, an enabled `lifetime` or an enabled `watermark`, `title` is blank or longer than 170 characters, `roomType` is not a known room type, `color` is not six hexadecimal digits, `cover` is longer than 50 characters or not a known cover, `share` is not empty, `lifetime` has an unknown `period` or a `value` outside 1-999, the `watermark` text is longer than 255 characters, `logo` has no `tmpFile`, a position outside 0-1280, a size outside 1-1280 or a position outside the uploaded picture, or a tag name is empty")]
    [SwaggerResponse(402, "The portal has reached the room limit of its pricing plan or its payment is overdue, or the logo does not fit into the portal storage quota")]
    [SwaggerResponse(403, "The caller is a user or a guest, `roomType` is a public room while the portal forbids external sharing, `private` is set while the caller has no encryption keys or the connected document server cannot work with encrypted files, `quota` is set while the storage quota for rooms is turned off or exceeds the portal storage limit, or `logo.tmpFile` is not a picture the caller uploaded")]
    [SwaggerResponse(404, "`logo.tmpFile` names no uploaded picture, or one already used")]
    [HttpPost("")]
    public async Task<FolderDto<int>> CreateRoom(CreateRoomRequestDto inDto)
    {
        var lifetime = inDto.Lifetime.Map();
        lifetime?.StartDate = DateTime.UtcNow;

        var room = await _fileStorageService.CreateRoomAsync(inDto.Title, inDto.RoomType, inDto.Private,
            inDto.Indexing, inDto.Quota, lifetime, inDto.DenyDownload, inDto.Watermark, inDto.Color, inDto.Cover,
            inDto.Tags, inDto.Logo, inDto.ChatSettings, inDto.SendFormToExternalDB, inDto.SaveFormAsXLSX);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Returns one page of the root folder of an Instructions room. Its Markdown files are the skills of the AI chat.
    /// The response has the same shape as `GET api/2.0/files/{folderId}`. Requires read access to the room. Other
    /// callers get 403. The call is read-only. To find Instructions rooms, use the `withAiFolder` filter of
    /// `GET api/2.0/files/rooms`. The call returns 404 if the room does not exist or is not an Instructions room. It
    /// returns 400 if `count` is out of range or `startIndex` is negative.
    /// </remarks>
    /// <summary>Get the skills folder of an Instructions room</summary>
    /// <path>api/2.0/files/rooms/{id}/ai</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "One page of the Instructions room root, with the room itself and the chain of its parents", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, or the `startIndex` is negative")]
    [SwaggerResponse(403, "The caller may not read this room")]
    [SwaggerResponse(404, "The room does not exist or is not an Instructions room")]
    [HttpGet("{id}/ai")]
    public async Task<FolderContentDto<int>> GetRoomAiFolder(GetRoomAiFolderRequestDto inDto)
    {
        var skillsFolder = await _fileStorageService.GetRoomSkillsFolderAsync(inDto.Id);

        return await folderContentDtoHelper.GetAsync(skillsFolder.Id, null, null, inDto.FilterType, default, true, true, null, null, null, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text);
    }

    /// <remarks>
    /// Starts a job that creates a room in the Rooms section from a room template. Requires read access to the
    /// template and the right to create rooms. Other callers get 403. A user or a guest cannot create rooms. The call
    /// queues work and returns the progress record of the job. The room does not exist yet when the response arrives.
    /// Repeat `GET api/2.0/files/rooms/fromtemplate/status` until `isCompleted` is true. Then take `roomId` from it. A
    /// non-empty `error` means that the job failed. The job fails if the template is a public room while the portal
    /// forbids external sharing. The portal keeps one job record per account. Starting a new job deletes the record of
    /// a finished one. Wait for the running job to end before you start the next one. Settings that the body leaves
    /// out come from the template. With `copyLogo`, the call ignores `logo`. The call returns 400 if `title` is blank.
    /// It returns 403 if `quota` is set while the quota for this kind of room is turned off. If no room template has
    /// the `templateId`, the call returns 404.
    /// </remarks>
    /// <summary>Create a room from the template</summary>
    /// <path>api/2.0/files/rooms/fromtemplate</path>
    [RaisesWebhook(WebhookTrigger.RoomCreated, When = "once the queued operation builds the room")]
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "after it when `quota` is given")]
    [RaisesWebhook(WebhookTrigger.AgentCreated, When = "instead for a room built from an AI agent template")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead for such a room with `quota`")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The progress record of the room creation job", typeof(RoomFromTemplateStatusDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `templateId` or `title`, `title` is blank or longer than 170 characters, `color` is longer than 6 or `cover` longer than 50 characters, `logo` has no `tmpFile`, a position outside 0-1280 or a size outside 1-1280, `lifetime` has an unknown `period` or a `value` outside 1-999, or the `watermark` text is longer than 255 characters")]
    [SwaggerResponse(403, "The caller cannot read the template or is a user or a guest, or `quota` is set while the storage quota for rooms (for an AI agent template, for agents) is turned off")]
    [SwaggerResponse(404, "No room template with `templateId` exists")]
    [HttpPost("fromTemplate")]
    public async Task<RoomFromTemplateStatusDto> CreateRoomFromTemplate(CreateRoomFromTemplateDto dto)
    {
        LogoSettings logo = null;
        if (dto.Logo != null)
        {
            logo = new LogoSettings
            {
                Height = dto.Logo.Height,
                Width = dto.Logo.Width,
                TmpFile = dto.Logo.TmpFile,
                X = dto.Logo.X,
                Y = dto.Logo.Y
            };
        }

        RoomLifetime lifetime = null;
        if (dto.Lifetime != null)
        {
            lifetime = new RoomLifetime { DeletePermanently = dto.Lifetime.DeletePermanently, Enabled = dto.Lifetime.Enabled, Period = dto.Lifetime.Period, Value = dto.Lifetime.Value };
        }

        WatermarkRequest watermark = null;
        if (dto.Watermark != null)
        {
            watermark = new WatermarkRequest
            {
                Additions = dto.Watermark.Additions,
                Enabled = dto.Watermark.Enabled,
                ImageHeight = dto.Watermark.ImageHeight,
                ImageWidth = dto.Watermark.ImageWidth,
                ImageScale = dto.Watermark.ImageScale,
                ImageUrl = dto.Watermark.ImageUrl,
                Rotate = dto.Watermark.Rotate,
                Text = dto.Watermark.Text
            };
        }

        // The room is built by a background operation, so both the access to the template and the
        // right to create rooms at all have to be verified here — otherwise the caller is told the
        // request succeeded and only finds out later, from the operation status, that it could not.
        await _fileStorageService.CheckCanCreateRoomFromTemplateAsync(dto.TemplateId, dto.Quota);

        var taskId = await roomTemplatesWorker.StartCreateRoomAsync(tenantManager.GetCurrentTenantId(), _authContext.CurrentAccount.ID,
            dto.TemplateId,
            dto.Title,
            logo,
            dto.CopyLogo,
            dto.Tags,
            dto.Cover,
            dto.Color,
            dto.Quota,
            dto.Indexing,
            dto.DenyDownload,
            lifetime,
            watermark,
            dto.Private,
            false);

        await eventBus.PublishAsync(new CreateRoomFromTemplateIntegrationEvent(_authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId())
        {
            TemplateId = dto.TemplateId,
            Logo = logo,
            CopyLogo = dto.CopyLogo,
            Title = dto.Title,
            Tags = dto.Tags,
            Cover = dto.Cover,
            Color = dto.Color,
            Quota = dto.Quota,
            Indexing = dto.Indexing,
            DenyDownload = dto.DenyDownload,
            Lifetime = lifetime,
            Watermark = watermark,
            Private = dto.Private,
            TaskId = taskId
        });
        return await GetRoomCreatingStatus();
    }

    /// <remarks>
    /// Returns the progress of the job that `POST api/2.0/files/rooms/fromtemplate` started for your account. Any
    /// signed-in user can call it. The call shows only the job of your own account. The call is read-only. This is
    /// the call to poll. Repeat it until `isCompleted` is true. Then pass `roomId` to `GET api/2.0/files/rooms/{id}`
    /// to read the room. The record stays after the job ends, until you start the next job. The body is empty if
    /// your account has no record. It is also empty if the portal cannot read the job queue. So an empty body does
    /// not prove that no job was started.
    /// </remarks>
    /// <summary>Get the room creation progress</summary>
    /// <path>api/2.0/files/rooms/fromtemplate/status</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The progress record of the caller room creation job, or an empty body when there is none", typeof(RoomFromTemplateStatusDto))]
    [HttpGet("fromTemplate/status")]
    public async Task<RoomFromTemplateStatusDto> GetRoomCreatingStatus()
    {
        try
        {
            var status = await roomTemplatesWorker.GetStatusRoomCreatingAsync(tenantManager.GetCurrentTenantId(), _authContext.CurrentAccount.ID);
            if (status != null)
            {
                var result = new RoomFromTemplateStatusDto { Progress = status.Percentage, Error = status.Exception != null ? status.Exception.Message : "", IsCompleted = status.IsCompleted, RoomId = status.RoomId };
                return result;
            }
        }
        catch
        {
        }

        return null;
    }

    /// <remarks>
    /// Starts a job that exports the data of all original forms of a form filling room to the external
    /// database. Requires editing access to a form filling room. Other callers get 403. Any other room also
    /// returns 403. The call queues work and returns the job record. Repeat
    /// `GET api/2.0/files/rooms/{id}/externaldbsync` until `isCompleted` is true. Then read `forms` for the result of each form. A form that fails does not stop
    /// the others. It gets `success` false and its own `error`. While a job runs, a new call returns that job and
    /// starts no second one. So you can repeat the call. After a job ends, a new call starts a new job. The new job
    /// exports only the form data that the database does not have yet. The room keeps one job record. If no external
    /// database is configured, the call returns 400 and queues nothing. If the room does not exist, the call returns
    /// 404.
    /// </remarks>
    /// <summary>Start external DB sync</summary>
    /// <path>api/2.0/files/rooms/{id}/externaldbsync</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The synchronization job record to poll", typeof(ExternalDbSyncTaskDto))]
    [SwaggerResponse(403, "The room is not a form filling room, or the caller cannot edit it")]
    [SwaggerResponse(404, "No folder with this ID exists")]
    [SwaggerResponse(400, "The portal has no external database configured")]
    [HttpPost("{id}/externalDbSync")]
    public async Task<ExternalDbSyncTaskDto> StartExternalDbSync(RoomIdRequestDto<int> inDto)
    {
        var task = await _fileStorageService.StartExternalDbSyncAsync(inDto.Id);
        return ExternalDbSyncTaskDto.Get(task);
    }

    /// <remarks>
    /// Returns the record of the job that exports form data of a form filling room to the external database. Requires
    /// editing access to a form filling room. Other callers get 403. Any other room also returns 403. The call is
    /// read-only. This is the call to poll after `POST api/2.0/files/rooms/{id}/externaldbsync`. Repeat it until
    /// `isCompleted` is true. Then read the result of each form in `forms`. The body is empty if the room has no job.
    /// The record belongs to the room, so every member with editing access sees the same job. If the room does not
    /// exist, the call returns 404.
    /// </remarks>
    /// <summary>Get external DB sync status</summary>
    /// <path>api/2.0/files/rooms/{id}/externaldbsync</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The synchronization job record, or an empty body when the room has no job", typeof(ExternalDbSyncTaskDto))]
    [SwaggerResponse(403, "The room is not a form filling room, or the caller cannot edit it")]
    [SwaggerResponse(404, "No folder with this ID exists")]
    [HttpGet("{id}/externalDbSync")]
    public async Task<ExternalDbSyncTaskDto> GetExternalDbSyncStatus(RoomIdRequestDto<int> inDto)
    {
        var task = await _fileStorageService.GetExternalDbSyncTaskAsync(inDto.Id);
        return ExternalDbSyncTaskDto.Get(task);
    }
}

public class VirtualRoomsThirdPartyController(
    GlobalFolderHelper globalFolderHelper,
    FileOperationDtoHelper fileOperationDtoHelper,
    CustomTagsService customTagsService,
    RoomLogoManager roomLogoManager,
    FileDeleteOperationsManager fileDeleteOperationsManager,
    FileMoveCopyOperationsManager fileMoveCopyOperationsManager,
    FileStorageService fileStorageService,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileShareDtoHelper fileShareDtoHelper,
    SocketManager socketManager,
    ApiContext apiContext,
    FilesMessageService filesMessageService,
    SettingsManager settingsManager,
    ApiDateTimeHelper apiDateTimeHelper,
    UserManager userManager,
    AuthContext authContext,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity)
    : VirtualRoomsController<string>(globalFolderHelper,
        fileOperationDtoHelper,
        customTagsService,
        roomLogoManager,
        fileDeleteOperationsManager,
        fileMoveCopyOperationsManager,
        fileStorageService,
        folderDtoHelper,
        fileDtoHelper,
        fileShareDtoHelper,
        socketManager,
        apiContext,
        filesMessageService,
        settingsManager,
        apiDateTimeHelper,
        userManager,
        authContext,
        daoFactory,
        fileSecurity)
{
    /// <remarks>
    /// Creates a room in the Rooms section from a folder of a connected third-party storage account. The files of the
    /// room stay in that storage. First, connect the storage account for rooms with `POST api/2.0/files/thirdparty`.
    /// Requires a DocSpace admin or a room admin who connected this storage account. Other callers get 403. The call
    /// changes data. One storage account backs only one room. So a repeated call returns 403. With
    /// `createAsNewFolder`, the room is a new subfolder named after `title`. Otherwise the folder from the path
    /// becomes the room, and the call ignores `tags` and `logo`. The call ignores `indexing` and `denyDownload` in
    /// both modes. A public room and a form filling room get a primary external link. To read it, use
    /// `GET api/2.0/files/rooms/{id}/link`. The call returns 400 if the storage account is not connected. It returns
    /// 402 if the portal reached the room limit of its plan or its payment is overdue. It returns 403 if the storage
    /// account was not connected for rooms. It also returns 403 for a public room while the portal forbids external
    /// sharing. A room type of the AI rooms section also returns 403. With `createAsNewFolder`, a subfolder that
    /// cannot be created also returns 403. If `id` is not a folder of a connected storage account, the call
    /// returns 404.
    /// </remarks>
    /// <summary>Create a third-party room</summary>
    /// <path>api/2.0/files/rooms/thirdparty/{id}</path>
    [RaisesWebhook(WebhookTrigger.RoomCreated, When = "when `roomType` is not `AiRoom`")]
    [RaisesWebhook(WebhookTrigger.AgentCreated, When = "instead when `roomType` is `AiRoom`")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room created out of the third-party folder, with string identifiers", typeof(FolderDto<string>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title` or `roomType`, `private` is set, which a third-party storage does not support, `roomType` is not a known room type, `logo` has no `tmpFile`, a position outside 0-1280 or a size outside 1-1280, or the identifier refers to a storage account that is not connected")]
    [SwaggerResponse(402, "The portal has reached the room limit of its pricing plan or its payment is overdue")]
    [SwaggerResponse(403, "The caller is not the account that connected the storage or may not create rooms, the storage account was not connected for room storage or already backs a room, the room would be public while the portal forbids external sharing, or, with `createAsNewFolder`, the subfolder cannot be created, as when `title` is blank, `cover` is not a known cover, a tag name is empty or the logo cannot be applied")]
    [SwaggerResponse(404, "`id` is not a folder of a connected third-party storage: a plain number, an identifier with a storage type the portal does not know, or a folder the storage does not have")]
    [HttpPost("thirdparty/{id}")]
    public async Task<FolderDto<string>> CreateRoomThirdParty(CreateThirdPartyRoomRequestDto inDto)
    {
        var room = await _fileStorageService.CreateThirdPartyRoomAsync(inDto.Room.Title, inDto.Room.RoomType, inDto.Id, inDto.Room.Private, inDto.Room.Indexing, inDto.Room.CreateAsNewFolder, inDto.Room.DenyDownload, inDto.Room.Color, inDto.Room.Cover, inDto.Room.Tags, inDto.Room.Logo);

        return await _folderDtoHelper.GetAsync(room);
    }
}

[ApiEndpoint(Template = "rooms")]
public abstract class VirtualRoomsController<T>(
    GlobalFolderHelper globalFolderHelper,
    FileOperationDtoHelper fileOperationDtoHelper,
    CustomTagsService customTagsService,
    RoomLogoManager roomLogoManager,
    FileDeleteOperationsManager fileDeleteOperationsManager,
    FileMoveCopyOperationsManager fileMoveCopyOperationsManager,
    FileStorageService fileStorageService,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileShareDtoHelper fileShareDtoHelper,
    SocketManager socketManager,
    ApiContext apiContext,
    FilesMessageService filesMessageService,
    SettingsManager settingsManager,
    ApiDateTimeHelper apiDateTimeHelper,
    UserManager userManager,
    AuthContext authContext,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    protected readonly FileStorageService _fileStorageService = fileStorageService;

    /// <remarks>
    /// Returns one room with its settings and the access level of the caller. Requires read access to the room. Other
    /// callers get 403. A DocSpace admin can read any room without an invitation, except a private room. An anonymous
    /// caller needs a valid external link to the room. Without one, the call returns 401. The call is read-only, except
    /// for the new-item marks. It clears the marks of the caller in this room, so `newForMe` becomes 0. To read the new
    /// items first, use `GET api/2.0/files/rooms/{id}/news`. The call also returns archived rooms. Their root section,
    /// not a flag, shows that they are archived. The call returns 404 for a deleted or unknown room, or a room in a
    /// section the caller cannot see. If a string `id` points to a storage account that is not connected, the call
    /// returns 400. To find room IDs, use `GET api/2.0/files/rooms`.
    /// </remarks>
    /// <summary>Get room information</summary>
    /// <path>api/2.0/files/rooms/{id}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its settings and the access level of the caller", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "A third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(401, "An anonymous caller has no external link that grants access to the room")]
    [SwaggerResponse(403, "The caller may not read this room")]
    [SwaggerResponse(404, "The room does not exist, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<FolderDto<T>> GetRoomInfo(RoomIdRequestDto<T> inDto)
    {
        var folder = (await _fileStorageService.GetRoomInfoAsync(inDto.Id)).NotFoundIfNull("Folder not found");

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <remarks>
    /// Changes the settings of a room and returns the whole room. The call changes only the fields in the body. An
    /// empty body changes nothing. Requires the room manager role in the room. Other callers get 403. A DocSpace
    /// admin without an invitation also gets 403. The call changes data, and you can repeat it with the same body.
    /// The portal deletes an uploaded image after use. So a repeated `logo` or uploaded `watermark.imageUrl` returns
    /// 404. After each call with `lifetime`, the portal waits 6 hours by default before it deletes expired files. The
    /// call trims a `title` and cleans it as at room creation. A blank `title` changes nothing. The `tags` array
    /// replaces all tags of the room, and an empty array removes them. An empty `color` restores the default color. An
    /// empty `cover` removes the cover. A `quota` of `-1` removes the custom limit. Any other negative `quota` restores
    /// the portal default. A positive `quota` works only while the room quota feature is turned on. If you turn on
    /// `indexing`, the call renumbers the room contents. The call returns 400 if the body has a property that the room
    /// update does not define. It also returns 400 for `chatSettings` in a room that is not an AI room. A private room
    /// with `indexing`, `lifetime`, or `watermark` also returns 400. If a new logo or watermark image does not fit the
    /// portal storage quota, the call returns 402. It returns 403 for a room in Trash or in the archive, or a `quota`
    /// over the portal limit. It also returns 403 if `logo.tmpFile` is not an image the caller uploaded. If the room or
    /// the uploaded image does not exist, the call returns 404. To crop and set only a logo, use
    /// `POST api/2.0/files/rooms/{id}/logo`.
    /// </remarks>
    /// <summary>Update a room</summary>
    /// <path>api/2.0/files/rooms/{id}</path>
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "on every successful call, even one that changes nothing")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead when the room is an AI agent")]
    [RaisesWebhook(WebhookTrigger.FolderUpdated, When = "instead when the ID is a folder inside the room, not the room itself")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room as it is after the update", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or holds a property the room update does not define, a private room is given `indexing`, an enabled `lifetime` or an enabled `watermark`, `title` is longer than 170 characters, `color` is not six hexadecimal digits, `cover` is longer than 50 characters or not a known cover, `lifetime` has an unknown `period` or a `value` outside 1-999, `logo` has no `tmpFile`, a position outside 0-1280, a size outside 1-1280 or a position outside the uploaded picture, the `watermark` text is longer than 255 characters, a tag name is empty, `chatSettings` is sent for a room that is not an AI room, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(402, "The new logo or watermark image does not fit into the portal storage quota")]
    [SwaggerResponse(403, "The caller may not edit this room, the room lies in Trash or in the archive, `quota` exceeds the storage limit of the portal, or `logo.tmpFile` or a relative `watermark.imageUrl` is not an image the caller uploaded")]
    [SwaggerResponse(404, "No room has this id, the uploaded image named by `logo.tmpFile` or `watermark.imageUrl` no longer exists, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPut("{id}")]
    public async Task<FolderDto<T>> UpdateRoom(UpdateRoomRequestDto<T> inDto)
    {
        var room = await _fileStorageService.UpdateRoomAsync(inDto.Id, inDto.UpdateRoom);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Sets one custom storage limit, in bytes, on each listed room. It returns the updated rooms in the order of
    /// `roomIds`. Requires the room manager role in each room. Other callers get 403. The call changes data, and you
    /// can repeat it with the same body. A room that already has this limit stays the same and is still returned.
    /// The call skips unknown rooms and rooms in third-party storage. The call is not atomic. If a room fails, the
    /// rooms before it keep the new limit. So check each room after a failed call. The call returns 403 if the room
    /// quota feature is turned off, or `quota` exceeds the portal storage limit. For AI agents, it checks the agent
    /// quota feature instead. It also returns 403 for a room in Trash or in the archive. To restore the portal
    /// default, use `PUT api/2.0/files/rooms/resetquota`. To remove the custom limit, send a `quota` of `-1` to
    /// `PUT api/2.0/files/rooms/{id}`.
    /// </remarks>
    /// <summary>Change the room quota limit</summary>
    /// <path>api/2.0/files/rooms/roomquota</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "for every listed room, even when its quota does not change")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead for a listed AI agent")]
    [RaisesWebhook(WebhookTrigger.FolderUpdated, When = "instead for a listed ID that is a folder, not a room")]
    [Tags("Files / Quota")]
    [SwaggerResponse(200, "The rooms as they are after the new limit was applied", typeof(IAsyncEnumerable<FolderDto<int>>))]
    [SwaggerResponse(403, "The storage quota for rooms (for an AI agent, for agents) is turned off, `quota` exceeds the storage limit of the portal, or the caller may not edit a listed room or it lies in Trash or in the archive")]
    [HttpPut("roomquota")]
    public async IAsyncEnumerable<FolderDto<int>> UpdateRoomsQuota(UpdateRoomsQuotaRequestDto<T> inDto)
    {
        var (folderIntIds, _) = FileOperationsManager.GetIds(inDto.RoomIds);

        var folderTitles = new List<string>();

        foreach (var roomId in folderIntIds)
        {
            var room = await _fileStorageService.FolderQuotaChangeAsync(roomId, inDto.Quota);
            if (room is null)
            {
                continue;
            }

            folderTitles.Add(NotifyConstants.GetRoomTitle(room));
            yield return await _folderDtoHelper.GetAsync(room);
        }

        if (folderTitles.Count == 0)
        {
            yield break;
        }

        if (inDto.Quota >= 0)
        {
            filesMessageService.Send(MessageAction.CustomQuotaPerRoomChanged, inDto.Quota.ToString(), folderTitles.ToArray());
        }
        else
        {
            filesMessageService.Send(MessageAction.CustomQuotaPerRoomDisabled, string.Join(", ", folderTitles.ToArray()));
        }
    }

    /// <remarks>
    /// Restores the default room quota of the portal on each listed room. It returns the updated rooms in the order
    /// of `roomIds`. This does not remove the limit. The room then follows the portal default, which a DocSpace admin
    /// can change at any time. Requires the room manager role in each room. Other callers get 403. The call changes
    /// data, and you can repeat it with the same body. The call skips unknown rooms and rooms in third-party storage.
    /// The call is not atomic. If a room fails, the rooms before it keep the default, and the rest keep their limit.
    /// It returns 403 if the room quota feature is off, or a room is in Trash or the archive. For AI agents, it
    /// checks the agent quota feature instead. To give the rooms their own value, use
    /// `PUT api/2.0/files/rooms/roomquota`. To remove the custom limit, send a `quota` of `-1` to
    /// `PUT api/2.0/files/rooms/{id}`.
    /// </remarks>
    /// <summary>Reset the room quota limit</summary>
    /// <path>api/2.0/files/rooms/resetquota</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "for every listed room, even when its quota does not change")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead for a listed AI agent")]
    [RaisesWebhook(WebhookTrigger.FolderUpdated, When = "instead for a listed ID that is a folder, not a room")]
    [Tags("Files / Quota")]
    [SwaggerResponse(200, "The rooms as they are after the default limit was restored", typeof(IAsyncEnumerable<FolderDto<int>>))]
    [SwaggerResponse(403, "The storage quota for rooms (for an AI agent, for agents) is turned off, or the caller may not edit a listed room or it lies in Trash or in the archive")]
    [HttpPut("resetquota")]
    public async IAsyncEnumerable<FolderDto<int>> ResetRoomQuota(UpdateRoomsRoomIdsRequestDto<T> inDto)
    {
        var (folderIntIds, _) = FileOperationsManager.GetIds(inDto.RoomIds);
        var folderTitles = new List<string>();
        var quotaRoomSettings = await settingsManager.LoadAsync<TenantRoomQuotaSettings>();

        foreach (var roomId in folderIntIds)
        {
            var room = await _fileStorageService.FolderQuotaChangeAsync(roomId, -2);
            if (room is null)
            {
                continue;
            }

            folderTitles.Add(NotifyConstants.GetRoomTitle(room));

            yield return await _folderDtoHelper.GetAsync(room);
        }

        if (folderTitles.Count == 0)
        {
            yield break;
        }

        filesMessageService.Send(MessageAction.CustomQuotaPerRoomDefault, quotaRoomSettings.DefaultQuota.ToString(), folderTitles.ToArray());
    }


    /// <remarks>
    /// Starts a background job that deletes a room with all its contents, and returns the job record. The job does
    /// not move the room to Trash, so the deletion cannot be undone. An archived room is deleted the same way. Requires the right
    /// to delete the room. Other callers get 403. The call checks the right before it queues the job. The job checks
    /// it again, so read the `error` of the finished job. Repeat `GET api/2.0/files/fileops` until `finished` is
    /// true. You can also read the record by its `id`. The portal keeps the record until you read it once after the
    /// job ends. The job ignores `deleteAfter` in the body. If the room does not exist, the call returns 404. A repeat
    /// after the job ends also returns 404. A repeat while the job runs queues a second job. If a string `id` points to
    /// a storage account that is not connected, the call returns 400.
    /// </remarks>
    /// <summary>Remove a room</summary>
    /// <path>api/2.0/files/rooms/{id}</path>
    [RaisesWebhook(WebhookTrigger.RoomDeleted, When = "once the queued operation deletes the room")]
    [RaisesWebhook(WebhookTrigger.AgentDeleted, When = "instead when the room is an AI agent")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The queued delete operation to poll", typeof(FileOperationDto))]
    [SwaggerResponse(400, "The request body cannot be read, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not delete this room")]
    [SwaggerResponse(404, "The room does not exist, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpDelete("{id}")]
    public async Task<FileOperationDto> DeleteRoom(DeleteRoomRequestDto<T> inDto)
    {
        // deleteAfter only means "do not keep the record forever"; the operation must still be
        // trackable at least until the client has polled it once, so the result is always held.
        var taskId = await fileDeleteOperationsManager.Publish([inDto.Id], [], false, true, true);
        var tasks = await fileDeleteOperationsManager.GetOperationResults(id: taskId);

        return await fileOperationDtoHelper.GetAsync(tasks.FirstOrDefault());
    }

    /// <remarks>
    /// Starts a background job that moves a room to the Archive section, and returns the job record. Members can still
    /// read an archived room. Only deleting or unarchiving the room changes it. No one can create, rename, or edit
    /// files in it, or change its settings, tags, logo, or links. Requires the room manager role in the room. Other
    /// callers get 403. A DocSpace admin without an invitation also gets 403. The call queues work. Repeat `GET
    /// api/2.0/files/fileops` until `finished` is true. You can repeat the call on an archived room. The `deleteAfter`
    /// field sets only how long the finished record stays. Instructions and Knowledge rooms cannot be archived and
    /// return 403. To remove one, use `DELETE api/2.0/files/rooms/{id}`. A folder that is not a room also returns 403.
    /// A room with a file that is locked or being edited also returns 403. If the room does not exist or is a room
    /// template, the call returns 404. If a string `id` points to a storage account that is not connected, the call
    /// returns 400. To restore the room, use `PUT api/2.0/files/rooms/{id}/unarchive`.
    /// </remarks>
    /// <summary>Archive a room</summary>
    /// <path>api/2.0/files/rooms/{id}/archive</path>
    [RaisesWebhook(WebhookTrigger.RoomArchived, When = "once the queued operation moves the room to the archive, for an AI agent as well")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The queued archive operation to poll", typeof(FileOperationDto))]
    [SwaggerResponse(400, "The request body cannot be read, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not read or archive this room, the id names a folder that is not a room, the room is an Instructions or Knowledge room, which is never archived, or a file in the room is locked or being edited")]
    [SwaggerResponse(404, "The room does not exist or is a room template, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPut("{id}/archive")]
    public async Task<FileOperationDto> ArchiveRoom(ArchiveRoomRequestDto<T> inDto)
    {
        var room = await _fileStorageService.GetFolderAsync(inDto.Id);
        if (room.RootId is int root && root == await globalFolderHelper.FolderRoomTemplatesAsync)
        {
            throw new ItemNotFoundException();
        }

        if (room.RootFolderType == FolderType.AiHub)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ArchiveRoom);
        }

        var destFolder = JsonSerializer.SerializeToElement(await globalFolderHelper.FolderArchiveAsync);
        var movableRoom = JsonSerializer.SerializeToElement(inDto.Id);

        var taskId = await fileMoveCopyOperationsManager.Publish([movableRoom], [], destFolder, false, FileConflictResolveType.Skip, !inDto.ArchiveRoom.DeleteAfter, false);
        var tasks = await fileMoveCopyOperationsManager.GetOperationResults(id: taskId);

        return await fileOperationDtoHelper.GetAsync(tasks.FirstOrDefault());
    }

    /// <remarks>
    /// Starts a background job that moves a room from the Archive section back to Rooms, and returns the job record.
    /// The room becomes writable again with its members, tags, logo, and links. The job does not restore the pins of
    /// the members. To pin the room again, use `PUT api/2.0/files/rooms/{id}/pin`. Requires the room manager role in
    /// the room. Other callers get 403. The call queues work. Repeat `GET api/2.0/files/fileops` until `finished` is
    /// true. The room may leave the archive before `finished` becomes true. You can repeat the call. A room that is not
    /// archived stays where it is. The `deleteAfter` field sets only how long the finished record stays. A folder that
    /// is not a room returns 403. A room with a file that is locked or being edited also returns 403. The call returns
    /// 404 if the room does not exist, was deleted, or is a room template. If a string `id` points to a storage account
    /// that is not connected, the call returns 400.
    /// </remarks>
    /// <summary>Unarchive a room</summary>
    /// <path>api/2.0/files/rooms/{id}/unarchive</path>
    [RaisesWebhook(WebhookTrigger.RoomRestored, When = "once the queued operation moves the room out of the archive, for AI agents too")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The queued unarchive operation to poll", typeof(FileOperationDto))]
    [SwaggerResponse(400, "The request body cannot be read, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not read or unarchive this room, the id names a folder that is not a room, or a file in the room is locked or being edited")]
    [SwaggerResponse(404, "The room does not exist or is a room template, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPut("{id}/unarchive")]
    public async Task<FileOperationDto> UnarchiveRoom(ArchiveRoomRequestDto<T> inDto)
    {
        var room = await _fileStorageService.GetFolderAsync(inDto.Id);
        if (room.RootId is int root && root == await globalFolderHelper.FolderRoomTemplatesAsync)
        {
            throw new ItemNotFoundException();
        }

        var destFolder = JsonSerializer.SerializeToElement(await globalFolderHelper.FolderVirtualRoomsAsync);
        var movableRoom = JsonSerializer.SerializeToElement(inDto.Id);

        var taskId = await fileMoveCopyOperationsManager.Publish([movableRoom], [], destFolder, false, FileConflictResolveType.Skip, !inDto.ArchiveRoom.DeleteAfter, false);
        var tasks = await fileMoveCopyOperationsManager.GetOperationResults(id: taskId);

        return await fileOperationDtoHelper.GetAsync(tasks.FirstOrDefault());
    }

    /// <remarks>
    /// Changes the members of a room in one call, and returns the access of the listed subjects. The call can add,
    /// change, and remove members. Each entry names an account, a group, or the email of a person without an account.
    /// An `access` of `None` removes the subject from the room. Requires the room manager role in the room. Other
    /// callers get 403. The call changes data, and you can repeat it with the same body. It skips a subject that
    /// already has the listed access, so a repeat sends no email. If `notify` is true, the portal emails the invitation
    /// with `message`. A person who got the same invitation or access email for this room recently gets no new one. A
    /// new `message` does not change this. The call ignores an entry without an access level. If a subject is listed
    /// twice, the last level wins. An empty list changes nothing. The call skips subjects that the caller cannot see,
    /// except current members that the room owner changes. So compare the answer with the request. Without `force`, the
    /// call does not remove a member who has a form role. It reports this in `error` instead. The answer can also carry
    /// a `warning`. The call returns 400 for an invalid or too long `email`, too many email invitations, or an invalid
    /// `culture`. It returns 403 if an entry has an email and the portal forbids guest invitations. It also returns 403
    /// if a subject cannot get the access, such as a user or guest as room manager. In a private room, a subject
    /// without encryption keys also returns 403. If the room does not exist, the call returns 404.
    /// </remarks>
    /// <summary>Set the room access rights</summary>
    /// <path>api/2.0/files/rooms/{id}/share</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The access entries of the named subjects, plus a warning or an error when something was not applied", typeof(RoomSecurityDto))]
    [SwaggerResponse(400, "The request body cannot be read, an `email` in `invitations` is malformed or longer than 255 characters, `invitations` invites more addresses by email than the portal allows at once, `culture` is not a valid culture name while an invitation email is sent, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "Email invitations are sent while the portal forbids inviting guests, the caller may not read the room or change its members, a listed subject cannot be given the requested access in this room, or the room is private and a listed account or invited address has no encryption keys")]
    [SwaggerResponse(404, "The room does not exist, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPut("{id}/share")]
    [EnableRateLimiting(RateLimiterPolicy.EmailInvitationApi)]
    public async Task<RoomSecurityDto> SetRoomSecurity(RoomInvitationRequestDto<T> inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto);

        var result = new RoomSecurityDto();

        if (inDto.RoomInvitation.Invitations == null || inDto.RoomInvitation.Invitations.Count == 0)
        {
            return result;
        }

        var newGuestsInvited =
            inDto.RoomInvitation.Invitations.Any(i => !string.IsNullOrEmpty(i.Email) && i.Access != FileShare.None);

        var guestsInvited =
            await inDto.RoomInvitation.Invitations
                .Where(r => r.Id != Guid.Empty && r.Access != FileShare.None)
                .ToAsyncEnumerable()
                .AnyAsync(async (i, _) => await userManager.IsGuestAsync(i.Id));

        var usersInvited =
            inDto.RoomInvitation.Invitations.Any(i => !string.IsNullOrEmpty(i.Email) && i.Access != FileShare.None) ||
            await inDto.RoomInvitation.Invitations
                .Where(r => r.Id != Guid.Empty && r.Access != FileShare.None)
                .ToAsyncEnumerable()
                .AnyAsync(async (i, _) => await userManager.IsUserAsync(i.Id));

        if (newGuestsInvited)
        {
            var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();
            if (!invitationSettings.AllowInvitingGuests)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        var room = await _fileStorageService.GetFolderAsync(inDto.Id).NotFoundIfNull("Folder not found");

        if (room.RootId is int root && root == await globalFolderHelper.FolderRoomTemplatesAsync)
        {
            if (inDto.RoomInvitation.Invitations.Any(i => i.Access != FileShare.None && i.Access != FileShare.Read) || guestsInvited || newGuestsInvited || usersInvited)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_RoleNotAvailable);
            }

            inDto.RoomInvitation.Notify = false;
        }

        foreach (var invitation in inDto.RoomInvitation.Invitations)
        {
            if (invitation.Access == FileShare.None && !inDto.RoomInvitation.Force &&
                await _fileStorageService.ShouldPreventUserDeletion(room, invitation.Id))
            {
                result.Error = RoomSecurityError.FormRoleBlockingDeletion;
                return result;
            }
        }

        var invitationIds = inDto.RoomInvitation.Invitations.Select(s => s.Id).ToList();
        var currentUsers = await _fileStorageService.GetRoomSharedInfoAsync(inDto.Id, invitationIds).ToListAsync();
        var currentUserId = authContext.CurrentAccount.ID;
        var wrappers = (await inDto.RoomInvitation.Invitations
            .ToAsyncEnumerable()
            .Where(async (s, _) =>
             (room.CreateBy == currentUserId && (s.Access == FileShare.None || currentUsers.Any(c => c.Id == s.Id))) ||
                await userManager.CanUserViewAnotherUserAsync(currentUserId, s.Id))
            .ToListAsync())
            .Map();

        var aceCollection = new AceCollection<T> { Files = [], Folders = [inDto.Id], Aces = wrappers, Message = inDto.RoomInvitation.Message };

        result.Warning = (await _fileStorageService.SetAceObjectAsync(aceCollection, inDto.RoomInvitation.Notify, inDto.RoomInvitation.Culture)).Select(r => r.Warning).FirstOrDefault();
        result.Members = await _fileStorageService.GetRoomSharedInfoAsync(inDto.Id, invitationIds)
            .Select(async (AceWrapper a, CancellationToken _) => await fileShareDtoHelper.Get(a))
            .ToListAsync();

        return result;
    }

    /// <remarks>
    /// Returns one page of the members of a room with their access. The list starts with the owner, then room
    /// managers, groups, other members, guests, and pending invitations. Use `filterType` to select what the call
    /// lists. By default, it lists accounts and groups, without links. To read the links, use
    /// `GET api/2.0/files/rooms/{id}/links`. Use `filterValue` to match the display name. A pending invitation
    /// matches by its email. Requires read access to the room. Other callers get 403. A DocSpace admin can read the
    /// list of any room. The call is read-only. Use `startIndex` and `count` to page through the list. The order
    /// stays the same between calls. The response headers carry the total. To list links here, the caller needs the
    /// right to see the links. Without it, the call returns an empty page. A `count` out of range returns 400. If the
    /// room does not exist, the call returns 404.
    /// </remarks>
    /// <summary>Get the room access rights</summary>
    /// <path>api/2.0/files/rooms/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "One page of the room access entries, ordered by role and then by name", typeof(IAsyncEnumerable<FileShareDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not read the room")]
    [SwaggerResponse(404, "The room does not exist, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpGet("{id}/share")]
    public async IAsyncEnumerable<FileShareDto> GetRoomSecurityInfo(RoomSecurityInfoRequestDto<T> inDto)
    {
        var offset = inDto.StartIndex;
        var count = inDto.Count;
        var text = inDto.Text;

        var totalCountTask = await _fileStorageService.GetPureSharesCountAsync(inDto.Id, FileEntryType.Folder, inDto.FilterType, text);
        apiContext.SetCount(Math.Min(totalCountTask - offset, count)).SetTotalCount(totalCountTask);

        await foreach (var ace in _fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.Folder, inDto.FilterType, text, offset, count))
        {
            yield return await fileShareDtoHelper.Get(ace);
        }
    }

    /// <remarks>
    /// Sets one link of a room and returns it. The call creates, changes, or deletes the link. The `linkType` field
    /// selects the type. An invitation link makes each person who opens it a member with `access`. An external link
    /// opens the room without an account. Without `linkId`, the call creates a link. With the ID of a link, it
    /// changes that link. An unknown ID creates a link with this ID. A change replaces all settings, so send every
    /// field to keep. A change keeps the link type. An `access` of `None` deletes the link. Deleting the primary
    /// external link of a public or form-filling room creates and returns a new one. After any other deletion, the
    /// body is empty. Requires the right to manage the links of the room. Other callers get 403. The call changes
    /// data. You can repeat a call with `linkId`, but each call without it creates another link. The `password`,
    /// `denyDownload`, and `internal` fields apply only to external links, and `maxUseCount` only to invitation
    /// links. Without `linkId`, an `internal` of false gives the portal default. In a public room, external links
    /// are never internal. There, a change with `internal` true saves nothing. The call reads a past
    /// `expirationDate` of an external link as none. It ignores the date for the primary link of a public or
    /// form-filling room. For an invitation link, a past date returns 400. Only custom, public, and form-filling
    /// rooms take external links, and a form-filling room only its primary one. A room has at most one invitation
    /// link, and a second one returns 403. While an admin restricts external links, the call makes each external link
    /// it sets internal. Then, setting an external link in a public room or deleting its primary link returns 403. Deleting the
    /// primary link of a form-filling room also returns 403 if that link is not internal. The call also returns 403
    /// for a private room, a room template, or too many links. It also returns 403 for a link type or access level
    /// the room does not take. The call returns 400 for a too long title or password, a weak password, or an invalid
    /// `maxUseCount` or `expirationDate`. If the room does not exist, the call returns 404.
    /// </remarks>
    /// <summary>Set the room external or invitation link</summary>
    /// <path>api/2.0/files/rooms/{id}/links</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The link as it is after the call, the new primary link that replaced a deleted one, or an empty body after any other deletion", typeof(FileShareDto))]
    [SwaggerResponse(400, "The request body cannot be read, `linkType` or `access` is not a known value, the title or password is longer than 255 characters, `maxUseCount` is outside 1-1000 or below the number of times the invitation link was already used, the password does not meet the portal password policy, `expirationDate` lies more than 10 years ahead or, for an invitation link, in the past, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not manage the links of this room, the room is private or a room template, the room does not take this kind of link or this access level, the room already has its invitation link or the link limit is reached, or the admin's restriction on external links forbids the change in a public or form-filling room")]
    [SwaggerResponse(404, "The room does not exist, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPut("{id}/links")]
    public async Task<FileShareDto> SetRoomLink(RoomLinkRequestDto<T> inDto)
    {
        var linkAce = inDto.RoomLink.LinkType switch
        {
            LinkType.Invitation => await _fileStorageService.SetInvitationLinkAsync(inDto.Id, inDto.RoomLink.LinkId, inDto.RoomLink.Title, inDto.RoomLink.Access, inDto.RoomLink.ExpirationDate, inDto.RoomLink.MaxUseCount),
            LinkType.External => await _fileStorageService.SetExternalLinkAsync(
                inDto.Id,
                FileEntryType.Folder,
                inDto.RoomLink.LinkId,
                inDto.RoomLink.Title,
                inDto.RoomLink.Access,
                inDto.RoomLink.ExpirationDate,
                inDto.RoomLink.Password?.Trim(),
                inDto.RoomLink.DenyDownload,
                inDto.RoomLink.Internal),
            _ => throw new InvalidOperationException()
        };

        if (linkAce == null)
        {
            return null;
        }

        var result = await fileShareDtoHelper.Get(linkAce);

        if (inDto.RoomLink.LinkId != Guid.Empty && linkAce.Id != inDto.RoomLink.LinkId && result.SharedLink != null)
        {
            result.SharedLink.RequestToken = null;
        }

        return result;
    }

    /// <remarks>
    /// Returns the links of a room. If `type` is set, the list has only links of this type. Otherwise, it has
    /// invitation and external links together. Each entry has the address, title, access, expiration, and the flag of the primary external link.
    /// Invitation links also show how many uses are left. Public and form-filling rooms get an external link when
    /// they are created. So an empty list there means that the link was revoked. Other rooms start with no links.
    /// Collaboration and virtual data rooms can have only an invitation link. Requires read access to the room.
    /// Other callers get 403. A member without the right to see the links gets an empty list. The call is
    /// read-only. It returns at most the first 100 links and ignores paging parameters. The count in the response is
    /// the number of entries sent. If the room does not exist, the call returns 404. If a string `id` points to a
    /// storage account that is not connected, the call returns 400.
    /// </remarks>
    /// <summary>Get the room links</summary>
    /// <path>api/2.0/files/rooms/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The sharing links of the room", typeof(IAsyncEnumerable<FileShareDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not read the room")]
    [SwaggerResponse(404, "The room does not exist, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpGet("{id}/links")]
    public async IAsyncEnumerable<FileShareDto> GetRoomLinks(GetRoomLinksRequestDto<T> inDto)
    {
        var filterType = inDto.Type.HasValue
            ? inDto.Type.Value switch
            {
                LinkType.Invitation => ShareFilterType.InvitationLink,
                LinkType.External => ShareFilterType.ExternalLink,
                _ => ShareFilterType.Link
            }
            : ShareFilterType.Link;
        var counter = 0;

        await foreach (var ace in _fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.Folder, filterType, null, 0, 100))
        {
            counter++;

            yield return await fileShareDtoHelper.Get(ace);
        }

        apiContext.SetCount(counter);
    }

    /// <remarks>
    /// Returns the primary external link of a room, the address for people outside the portal. Public and form-filling
    /// rooms get this link when they are created. If the room has no primary link yet, the call creates one. Requires
    /// read access to the room. Other callers get 403. To create the link, the caller also needs the right to manage
    /// the links of the room. Without it, the call returns 403. The call is read-only, except when it creates the link.
    /// A repeated call returns the same link. An archived public room still returns its link to room managers. Other
    /// members get 403. The entry is the same as in `GET api/2.0/files/rooms/{id}/links`, with the primary flag set. It
    /// includes the request token that must go with the address. The call also returns 403 if the room type takes no
    /// external links. An admin rule that allows external links only in public rooms also returns 403. If the link was
    /// revoked, the call returns 404 and creates no new link. If the room does not exist, the call also returns 404. If
    /// a string `id` points to a storage account that is not connected, the call returns 400.
    /// </remarks>
    /// <summary>Get the room primary external link</summary>
    /// <path>api/2.0/files/rooms/{id}/link</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The primary external link of the room", typeof(FileShareDto))]
    [SwaggerResponse(400, "A third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not read the room, or the room has no primary link yet and the caller may not manage its links, its type takes no external links, or the admin restricts external links to public rooms")]
    [SwaggerResponse(404, "The room does not exist or its primary link was revoked, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpGet("{id}/link")]
    public async Task<FileShareDto> GetRoomsPrimaryExternalLink(RoomIdRequestDto<T> inDto)
    {
        var linkAce = await _fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.Folder);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Adds tags to a room and returns the room with all its tags. All rooms of the portal share one set of tags. The
    /// call creates each name that the portal does not have yet. Requires the room manager role in the room, or a
    /// DocSpace admin. Other callers get 403. An archived room also returns 403. The call changes data, and you can
    /// repeat it with the same body. It keeps the tags that the room already has. An empty list changes nothing. A
    /// tag has no ID, so the call finds it by name. To list the existing names, use `GET api/2.0/files/tags`. The
    /// call returns 400 for a null, blank, or too long name. If the room does not exist, the call returns 404. To
    /// remove tags from a room, use `DELETE api/2.0/files/rooms/{id}/tags`. The tags then stay in the portal.
    /// </remarks>
    /// <summary>Attach tags to a room</summary>
    /// <path>api/2.0/files/rooms/{id}/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its tag set after the change", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `names`, a name is null, blank or longer than 255 characters, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not edit this room, or the room is archived")]
    [SwaggerResponse(404, "The room does not exist, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPut("{id}/tags")]
    public async Task<FolderDto<T>> AddRoomTags(BatchTagsRequestDto<T> inDto)
    {
        var room = await customTagsService.AddRoomTagsAsync(inDto.Id, inDto.BatchTags.Names);
        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Removes tags from a room and returns the room with its other tags. The tags stay in the portal and on other
    /// rooms, even a tag that has no rooms left. To delete a tag from the portal, use `DELETE api/2.0/files/tags`.
    /// Requires the room manager role in the room, or a DocSpace admin. Other callers get 403. An archived room also
    /// returns 403. The call changes data, and you can repeat it with the same body. It skips names that the portal
    /// or the room does not have. So check the returned tags to see what changed. An empty list changes nothing. The
    /// call returns 400 for a null, blank, or too long name. If the room does not exist, the call returns 404.
    /// </remarks>
    /// <summary>Detach tags from a room</summary>
    /// <path>api/2.0/files/rooms/{id}/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its tag set after the change", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `names`, a name is null, blank or longer than 255 characters, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not edit this room, or the room is archived")]
    [SwaggerResponse(404, "The room does not exist, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpDelete("{id}/tags")]
    public async Task<FolderDto<T>> DeleteRoomTags(BatchTagsRequestDto<T> inDto)
    {
        foreach (var batchTagsName in inDto.BatchTags.Names)
        {
            ArgumentNullException.ThrowIfNull(batchTagsName, nameof(inDto.BatchTags.Names));
        }

        var room = await customTagsService.DeleteRoomTagsAsync(inDto.Id, inDto.BatchTags.Names);
        return await _folderDtoHelper.GetAsync(room);
    }


    /// <remarks>
    /// Sets an uploaded image as the room logo, and returns the room with the URLs of four logo sizes. First,
    /// upload the image with `POST api/2.0/files/logos`. Then pass the returned path in `tmpFile`. An empty `tmpFile`
    /// changes nothing. Use `x`, `y`, `width`, and `height` to crop the image. A position without a size returns 400,
    /// but a size without a position works. The logo hides the cover of the room. If you delete the logo, the room
    /// shows the cover again. Requires the room manager role in the room. Other callers get 403. An archived room
    /// also returns 403. The call changes data. The portal deletes the uploaded image after this call, so a repeated
    /// call returns 404. A path that another account uploaded returns 403. The call returns 400 for a crop outside
    /// the image or a value out of range. If the logo does not fit the portal storage quota, the call returns 402. If
    /// the room does not exist, the call returns 404.
    /// </remarks>
    /// <summary>Set the room logo</summary>
    /// <path>api/2.0/files/rooms/{id}/logo</path>
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "when a logo is given in `tmpFile`")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead when the room is an AI agent")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with the addresses of its new logo", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `tmpFile`, `x` or `y` is outside 0-1280, `width` or `height` is missing or outside 1-1280, the crop position lies outside the uploaded picture, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(402, "The logo does not fit into the portal storage quota")]
    [SwaggerResponse(403, "The caller may not edit this room, the room is archived, or `tmpFile` is not a picture the caller uploaded")]
    [SwaggerResponse(404, "No room with this ID exists, `tmpFile` names no uploaded picture or one already used, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPost("{id}/logo")]
    public async Task<FolderDto<T>> CreateRoomLogo(LogoRequest<T> inDto)
    {
        var room = await roomLogoManager.CreateAsync(inDto.Id, inDto.Logo.TmpFile, inDto.Logo.X, inDto.Logo.Y, inDto.Logo.Width, inDto.Logo.Height);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Changes the cover image and the background color of a room, and returns the room. You can send either field
    /// alone. An empty `cover` removes the cover, and an empty `color` restores the default. An empty body changes
    /// nothing. The room shows its cover only while it has no logo. A logo from `POST api/2.0/files/rooms/{id}/logo`
    /// hides the cover but keeps it. Requires the room manager role in the room. Other callers get 403. An archived
    /// room also returns 403. The call changes data, and you can repeat it with the same body. The cover stays after
    /// archiving and unarchiving. The call returns 400 for an unknown `cover` or an invalid `color`. If the room does
    /// not exist or was deleted, the call returns 404.
    /// </remarks>
    /// <summary>Change the room cover</summary>
    /// <path>api/2.0/files/rooms/{id}/cover</path>
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "when the color or the cover changes")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead when the room is an AI agent")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room as it is after the cover change", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read, `color` is not six hexadecimal digits, `cover` is not an identifier from the cover gallery, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not edit this room, or the room is archived")]
    [SwaggerResponse(404, "No room with this ID exists, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPost("{id}/cover")]
    public async Task<FolderDto<T>> ChangeRoomCover(CoverRequestDto<T> inDto)
    {
        var room = await roomLogoManager.ChangeCoverAsync(inDto.Id, inDto.Cover.Color, inDto.Cover.Cover);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Returns the built-in cover images for rooms. Each entry has the ID for `POST api/2.0/files/rooms/{id}/cover` and
    /// the image as vector markup. The list is the same for all portals, rooms, accounts, and languages. The IDs do
    /// not change, so you can store them. The images can change between product versions. Any signed-in user can
    /// call it. A guest who has nothing shared on the portal gets 403. The call is read-only. Only the IDs of this
    /// list are valid covers when you create or change a room.
    /// </remarks>
    /// <summary>Get room cover gallery</summary>
    /// <path>api/2.0/files/rooms/covers</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The built-in room covers with their identifiers and vector markup", typeof(IAsyncEnumerable<CoversResultDto>))]
    [SwaggerResponse(403, "The caller is a guest with whom nothing on the portal has been shared")]
    [HttpGet("covers")]
    public async IAsyncEnumerable<CoversResultDto> GetRoomCovers()
    {
        // The gallery itself is portal-wide reference data, so there is nothing room-specific to
        // check. Guests used to be refused outright, which also shut out a guest who had been invited
        // into a room and needs the gallery for the room-group dialog. A guest who belongs to no room
        // at all still has no use for it.
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            var subjects = await fileSecurity.GetUserSubjectsAsync(authContext.CurrentAccount.ID);

            // Any share at all, deliberately - not room membership specifically. The gallery is
            // reference data, so the bar is only "this guest has been let into something"; a guest
            // shared a single document passes, and there is nothing room-specific here for them to
            // learn. Both key kinds are asked: share records for rooms on the portal's own storage
            // are int-keyed, those for third-party-backed rooms string-keyed and in a separate table,
            // so a guest whose only membership is in a third-party room has no int-keyed record.
            var hasAnyShare = await daoFactory.GetSecurityDao<int>().GetSharesAsync(subjects).AnyAsync()
                           || await daoFactory.GetSecurityDao<string>().GetSharesAsync(subjects).AnyAsync();

            if (!hasAnyShare)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        foreach (var c in await RoomLogoManager.GetCoversAsync())
        {
            yield return new CoversResultDto { Id = c.Key, Data = c.Value };
        }
    }

    /// <remarks>
    /// Deletes the logo of a room and returns the room with empty logo URLs. The room then shows its cover and color
    /// again. To change the cover, use `POST api/2.0/files/rooms/{id}/cover`. The call changes nothing else in the
    /// room. Requires the room manager role in the room. Other callers get 403. An editor of the room also gets 403.
    /// An archived room also returns 403. The call changes data, and you can repeat it. A room without a logo
    /// returns 200. If the room does not exist or was deleted, the call returns 404. If a string `id` points to a
    /// storage account that is not connected, the call returns 400. To set a new logo, use
    /// `POST api/2.0/files/logos`, then `POST api/2.0/files/rooms/{id}/logo`.
    /// </remarks>
    /// <summary>Remove a room logo</summary>
    /// <path>api/2.0/files/rooms/{id}/logo</path>
    [RaisesWebhook(WebhookTrigger.RoomUpdated, When = "when the room had a logo")]
    [RaisesWebhook(WebhookTrigger.AgentUpdated, When = "instead when the room is an AI agent")]
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its logo removed", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "A third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The room has a logo and the caller may not edit it, or the room is archived")]
    [SwaggerResponse(404, "No room with this ID exists, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpDelete("{id}/logo")]
    public async Task<FolderDto<T>> DeleteRoomLogo(RoomIdRequestDto<T> inDto)
    {
        var room = await roomLogoManager.DeleteAsync(inDto.Id);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Pins a room to the top of the room list of the caller, and returns the room. Other members do not see the pin.
    /// Pinned rooms stay above other rooms with any sort or filter. Their order among themselves stays the same.
    /// Archiving removes the pin, so pin an unarchived room again. Requires read access to the room. Other callers
    /// get 403. A guest and a DocSpace admin without an invitation can also pin it. The call changes data, and you can
    /// repeat it. By default, an account can pin up to 10 rooms. AI agents and AI rooms have their own limits. Over
    /// the limit, the call returns 403. To free a place, use `PUT api/2.0/files/rooms/{id}/unpin`. An archived room
    /// or a folder that is not a room also returns 403. If the room does not exist, the call returns 404. If a
    /// string `id` points to a storage account that is not connected, the call returns 400.
    /// </remarks>
    /// <summary>Pin a room</summary>
    /// <path>api/2.0/files/rooms/{id}/pin</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its pinned flag set for the caller", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "A third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller cannot read this room, the room is archived or is not a room, or the caller already has the maximum number of pinned rooms (AI agents and AI rooms are counted separately)")]
    [SwaggerResponse(404, "No room with this ID exists, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPut("{id}/pin")]
    public async Task<FolderDto<T>> PinRoom(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.SetPinnedStatusAsync(inDto.Id, true);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Unpins a room for the caller and returns the room. The call changes only the order of the room list of the
    /// caller. The room, its members, and its contents stay the same. Unpinning frees a pin place of the account. AI
    /// rooms have their own places. So use this call when `PUT api/2.0/files/rooms/{id}/pin` returns 403 for the
    /// limit. Requires read access to the room. Other callers get 403. The call changes data, and you can repeat it.
    /// A room that is not pinned returns 200 and changes nothing. So the answer does not show that the room was
    /// pinned. An archived room or a folder that is not a room returns 403. If the room does not exist or was
    /// deleted, the call returns 404. If a string `id` points to a storage account that is not connected, the call
    /// returns 400.
    /// </remarks>
    /// <summary>Unpin a room</summary>
    /// <path>api/2.0/files/rooms/{id}/unpin</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its pinned flag cleared for the caller", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "A third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller cannot read this room, or the room is archived or is not a room")]
    [SwaggerResponse(404, "No room with this ID exists, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPut("{id}/unpin")]
    public async Task<FolderDto<T>> UnpinRoom(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.SetPinnedStatusAsync(inDto.Id, false);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Sends the room invitation email again to invited members. If `resendAll` is true, the call sends it to every
    /// pending invitation of the room and ignores `usersIds`. Otherwise, it sends it only to the accounts in
    /// `usersIds`. The call skips accounts that are not room members or that the caller cannot see. If `resendAll` is
    /// false and `usersIds` is empty, the call does nothing. So a 200 does not prove that an email was sent. Requires
    /// the room manager role in the room. Other callers get 403. An archived room also returns 403. The call changes no
    /// data. Do not repeat it. By default, a member who got this invitation in the last 10 minutes gets no new email.
    /// After that, each call sends the email again. The link in the earlier email stays valid. The response has no
    /// body. A body that cannot be read returns 400. If the room does not exist or is a room template, the call returns
    /// 404.
    /// </remarks>
    /// <summary>Resend the room invitations</summary>
    /// <path>api/2.0/files/rooms/{id}/resend</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The request is accepted; the pending members it covers are mailed again, except those who already got this invitation within the repeat period")]
    [SwaggerResponse(400, "The request body cannot be read, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller is not a manager of the room, or the room is archived")]
    [SwaggerResponse(404, "The room does not exist or is a room template, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPost("{id}/resend")]
    public async Task ResendEmailInvitations(UserInvitationRequestDto<T> inDto)
    {
        await _fileStorageService.ResendEmailInvitationsAsync(inDto.Id, inDto.UserInvitation.UsersIds, inDto.UserInvitation.ResendAll);
    }

    /// <remarks>
    /// Renumbers the items directly in a room from 1, without gaps or repeats, and returns the room. The items keep
    /// their order. The call moves, renames, copies, or deletes nothing. Files and folders share one sequence. The
    /// call does not change the numbers inside subfolders. The call is meant for rooms with `indexing` turned on,
    /// where lists follow this order. A room without indexing also accepts it. Requires the room manager role in the
    /// room. Other callers get 403. An archived room also returns 403. The call changes data, and you can repeat it.
    /// A second call changes nothing. If the room does not exist or was deleted, the call returns 404. If a string
    /// `id` points to a storage account that is not connected, the call returns 400.
    /// </remarks>
    /// <summary>Reorder room contents</summary>
    /// <path>api/2.0/files/rooms/{id}/reorder</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room whose contents were renumbered", typeof(FolderDto<int>))]
    [SwaggerResponse(400, "A third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller may not edit this room, or the room is archived")]
    [SwaggerResponse(404, "No room with this ID exists, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpPut("{id}/reorder")]
    public async Task<FolderDto<T>> ReorderRoom(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.ReOrderAsync(inDto.Id);
        await filesMessageService.SendAsync(MessageAction.FolderIndexReordered, room, room.Title);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Returns the files of a room that are new for the caller, grouped by the day of the last change. The newest
    /// day comes first, and in each day the newest file comes first. The list has only files, including files in
    /// subfolders at any depth. A new folder is not an entry. Changes of the caller are never new for the caller. A
    /// deleted file leaves the list. Requires read access to the room. Other callers get 403. The call is read-only
    /// and keeps the new-item marks. Opening the room with `GET api/2.0/files/rooms/{id}` clears them, so call this
    /// one first. An empty array means that nothing is new. A folder in Trash returns 403. If the room does not
    /// exist or was deleted, the call returns 404. If a string `id` points to a storage account that is not
    /// connected, the call returns 400. To read the new items of all rooms, use `GET api/2.0/files/rooms/news`.
    /// </remarks>
    /// <summary>Get new items in a room</summary>
    /// <path>api/2.0/files/rooms/{id}/news</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The new files of the room, grouped by day", typeof(List<NewItemsDto<FileEntryBaseDto>>))]
    [SwaggerResponse(400, "A third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The caller cannot read this room, or the id names a folder in the Trash")]
    [SwaggerResponse(404, "No room with this ID exists, or the id is neither a 32-bit number nor a third-party identifier of a known storage type")]
    [HttpGet("{id}/news")]
    public async Task<List<NewItemsDto<FileEntryBaseDto>>> GetNewRoomItems(RoomIdRequestDto<T> inDto)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(inDto.Id);

        var newItems = await _fileStorageService.GetNewRoomFilesAsync(folder);
        var result = new List<NewItemsDto<FileEntryBaseDto>>();

        foreach (var (date, entries) in newItems)
        {
            var apiDateTime = apiDateTimeHelper.Get(date);
            var items = new List<FileEntryBaseDto>();

            foreach (var en in entries)
            {
                items.Add(await GetFileEntryWrapperAsync(en, folder));
            }

            result.Add(new NewItemsDto<FileEntryBaseDto> { Date = apiDateTime, Items = items });
        }

        return result;
    }
}

public class VirtualRoomsCommonController(
    FileStorageService fileStorageService,
    FolderContentDtoHelper folderContentDtoHelper,
    GlobalFolderHelper globalFolderHelper,
    CustomTagsService customTagsService,
    RoomLogoManager roomLogoManager,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    AuthContext authContext,
    DocumentBuilderTaskManager<RoomIndexExportTask, int, RoomIndexExportTaskData> documentBuilderTaskManager,
    TenantManager tenantManager,
    IEventBus eventBus,
    UserManager userManager,
    IServiceProvider serviceProvider,
    ApiDateTimeHelper apiDateTimeHelper,
    RootNewItemsDtoHelper rootNewItemsDtoHelper,
    MetadataFilterHelper metadataFilterHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Returns one page of the rooms of a portal section. The `searchArea` parameter picks the section, by default the
    /// active rooms. The other parameters filter the rooms by type, title, tags, member, owner, storage, quota, privacy,
    /// and metadata. A room must match all filters. Any signed-in user can call it. If the caller cannot read the
    /// section, the call returns 403. A caller sees only the rooms they created or were invited to. A DocSpace admin
    /// sees all rooms. So an empty list means that the caller sees no matching room. The call is read-only, except
    /// that it stores a `sortBy` value as the default sort order of the account. Without `sortBy`, the call uses the
    /// stored order. The rooms come in `folders`, and `files` stays empty. The `current` field describes the section,
    /// and `total` counts all matching rooms before paging. To read one room, use `GET api/2.0/files/rooms/{id}`. The
    /// call returns 400 if `tags` is not a JSON array, `sortBy` is unknown, or the metadata filter is invalid.
    /// </remarks>
    /// <summary>Get rooms</summary>
    /// <path>api/2.0/files/rooms</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The rooms of the selected section with the paging counters", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, the `startIndex` is negative, or `tags` is not a JSON array of strings")]
    [SwaggerResponse(403, "The caller cannot read the selected section")]
    [HttpGet("rooms")]
    public async Task<FolderContentDto<int>> GetRoomsFolder(RoomContentRequestDto inDto)
    {
        var parentId = await GetRoomsSectionIdAsync(inDto.SearchArea);

        IEnumerable<string> tagNames = null;

        if (!string.IsNullOrEmpty(inDto.Tags))
        {
            try
            {
                tagNames = JsonSerializer.Deserialize<IEnumerable<string>>(inDto.Tags);
            }
            catch (JsonException)
            {
                // The parameter carries a JSON array as a string. A caller that sends a bare tag name
                // - which is what a generated client does when its own signature takes an array and
                // serialises it as a repeated query parameter - used to reach the serializer and end
                // the request as 500. A malformed query value is the caller's mistake.
                throw new ArgumentException(FilesCommonResource.ErrorMessage_BadRequest, nameof(inDto.Tags));
            }
        }

        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await GetRoomsAsync(parentId, inDto.Type, inDto.SearchArea, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.Text, metadataFilter,
            inDto.SubjectId, inDto.SubjectOwnerId, inDto.WithoutTags ?? false, tagNames, inDto.ExcludeSubject ?? false, inDto.Provider ?? ProviderFilter.None,
            inDto.QuotaFilter ?? QuotaFilter.All, inDto.StorageFilter ?? StorageFilter.None, inDto.PrivacyFilter ?? RoomPrivacyFilter.None, inDto.GroupId,
            inDto.WithAiFolder ?? false);
    }

    /// <remarks>
    /// Returns one page of the rooms of a portal section that match a metadata filter. The `searchArea` field picks
    /// the section, by default the active rooms, and `type` limits the room types. The `searchArea` field can also
    /// pick the private rooms section. The active rooms do not include private rooms. With only `metadataTemplateId`,
    /// the call keeps the rooms that have this template. A room must match every condition of `metadataFilters`. The
    /// call searches the `filterValue` text in room titles and metadata values. All given filters combine, and a body
    /// without them lists the whole section. Take the template and field IDs from
    /// `GET api/2.0/files/metadata/templates`. Any signed-in user can call it. If the caller cannot read the section,
    /// the call returns 403. A caller sees only the rooms they created or were invited to. A DocSpace admin sees all
    /// rooms. So an empty list means that the caller sees no matching room. The call is read-only, except that it
    /// stores a `sortBy` value as the default sort order of the account. Without `sortBy`, the call uses the stored
    /// order. You can repeat it with the same body. The rooms come in `folders`, and `files` stays empty. The `total`
    /// field counts all matching rooms before paging. To filter by tags, member, owner, storage, quota, or privacy,
    /// use `GET api/2.0/files/rooms`. The call returns 400 for an unknown template or field, a field outside the
    /// template, or more than 20 conditions. It also returns 400 for an unknown `sortBy`.
    /// </remarks>
    /// <summary>Search the rooms by metadata</summary>
    /// <path>api/2.0/files/rooms/search</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Returns the matching rooms of the section", typeof(FolderContentDto<int>))]
    [SwaggerResponse(400, "An unknown metadata template or field, a field outside the named template, more than 20 conditions, an unrecognised `sortBy` or a `count` out of range")]
    [SwaggerResponse(403, "You don't have enough permission to view the room content")]
    [HttpPost("rooms/search")]
    public async Task<FolderContentDto<int>> SearchRooms(RoomsMetadataSearchRequestDto inDto)
    {
        var parentId = await GetRoomsSectionIdAsync(inDto.SearchArea);

        var metadataFilter = await metadataFilterHelper.ParseAsync(inDto.MetadataTemplateId, inDto.MetadataFilters, inDto.IncludeHiddenMetadata ?? false);

        return await GetRoomsAsync(parentId, inDto.Type, inDto.SearchArea, inDto.SortBy, inDto.SortOrder, inDto.StartIndex, inDto.Count, inDto.FilterValue, metadataFilter);
    }

    private async Task<int> GetRoomsSectionIdAsync(SearchArea? searchArea)
    {
        return searchArea switch
        {
            SearchArea.Archive => await globalFolderHelper.GetFolderArchive(),
            SearchArea.Templates => await globalFolderHelper.GetFolderRoomTemplatesAsync(),
            SearchArea.FormTemplates => await globalFolderHelper.GetFolderRoomTemplatesAsync(),
            SearchArea.Forms => await globalFolderHelper.GetFolderFormsAsync(),
            SearchArea.PrivateRooms => await globalFolderHelper.GetFolderPrivateRoomsAsync(),
            SearchArea.AiHub => await globalFolderHelper.GetFolderAiHubAsync(),
            _ => await globalFolderHelper.GetFolderVirtualRooms()
        };
    }

    /// <summary>
    /// The rooms listing shared by the query string listing and the typed search: the search passes the metadata filter,
    /// the text, the types, the section and the paging, and leaves the other filters at their defaults.
    /// </summary>
    private async Task<FolderContentDto<int>> GetRoomsAsync(
        int parentId,
        IEnumerable<RoomType> types,
        SearchArea? searchArea,
        string sortBy,
        SortOrder sortOrder,
        int startIndex,
        int count,
        string filterValue,
        MetadataFilter metadataFilter,
        Guid? subjectId = null,
        Guid? subjectOwnerId = null,
        bool withoutTags = false,
        IEnumerable<string> tagNames = null,
        bool excludeSubject = false,
        ProviderFilter provider = ProviderFilter.None,
        QuotaFilter quotaFilter = QuotaFilter.All,
        StorageFilter storageFilter = StorageFilter.None,
        RoomPrivacyFilter privacyFilter = RoomPrivacyFilter.None,
        int? groupId = null,
        bool withAiFolder = false)
    {
        var filter = RoomTypeExtensions.MapToFilterType(types);

        // An unrecognised sortBy used to be dropped on the floor: the listing came back in the
        // default order and the caller had no way to tell its sort had been ignored. The accepted
        // values are the names of SortedByType - sorting by name is "AZ", not "title".
        OrderBy orderBy = null;
        if (!string.IsNullOrEmpty(sortBy))
        {
            if (!SortedByTypeExtensions.TryParse(sortBy, true, out var sortedBy))
            {
                throw new ArgumentException(FilesCommonResource.ErrorMessage_BadRequest, nameof(sortBy));
            }

            orderBy = new OrderBy(sortedBy, sortOrder == SortOrder.Ascending);
        }

        var content = await fileStorageService.GetFolderItemsAsync(
            parentId,
            startIndex,
            count,
            filter,
            false,
            subjectId,
            Guid.Empty,
            filterValue,
            [],
            true,
            false,
            orderBy,
            searchArea ?? SearchArea.Active,
            0,
            withoutTags,
            tagNames,
            excludeSubject,
            provider,
            subjectOwnerId,
            quotaFilter: quotaFilter,
            storageFilter: storageFilter,
            groupId: groupId,
            privacyFilter: privacyFilter,
            withAiFolder: withAiFolder,
            metadataFilter: metadataFilter);

        var dto = await folderContentDtoHelper.GetAsync(parentId, content, startIndex, filterValue, metadataFilter?.IncludeHiddenTemplates ?? false);

        return dto.NotFoundIfNull();
    }

    /// <remarks>
    /// Creates a custom room tag in the portal catalog and returns its name. The tag belongs to the portal, not to the
    /// caller. Requires a DocSpace admin or a room admin. Other callers get 403. The call changes data. If a tag with
    /// this name exists, the call returns that tag and creates nothing. So you can repeat it with the same body. Use
    /// the returned name in the `tags` filter of `GET api/2.0/files/rooms` and in the room tag calls. To attach the
    /// tag to a room, use `PUT api/2.0/files/rooms/{id}/tags`. That call also creates a missing tag. To read the
    /// catalog, use `GET api/2.0/files/tags`. The call returns 400 if the name is missing, empty, has only spaces, or
    /// is too long.
    /// </remarks>
    /// <summary>Create a room tag</summary>
    /// <path>api/2.0/files/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The name of the created tag, or of the tag that already carried this name", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read or has no `name`, or the name is empty, blank or longer than 255 characters")]
    [SwaggerResponse(403, "Only a room manager or a portal administrator can create tags")]
    [HttpPost("tags")]
    public async Task<string> CreateRoomTag(CreateTagRequestDto inDto)
    {
        var createdTag = await customTagsService.CreateTagAsync(inDto.Name);
        return createdTag.Name;
    }

    /// <remarks>
    /// Renames a custom room tag of the portal. Every room with this tag then shows the new name. Use the old name
    /// exactly as `GET api/2.0/files/tags` returns it. Requires a DocSpace admin. Other callers get 403. A room admin
    /// can create tags but also gets 403 here. The call changes data, and a repeated call returns 404. It returns the
    /// new name. After the rename, a `tags` filter of `GET api/2.0/files/rooms` with the old name finds no rooms. If
    /// no tag has the old name, the call returns 404. The call returns 400 if a name is empty, has only spaces, or is
    /// too long. It also returns 400 if any tag already has the new name, even this tag. So you cannot merge two tags.
    /// </remarks>
    /// <summary>Rename a room tag</summary>
    /// <path>api/2.0/files/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The new name of the renamed tag", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read or has no `oldName` or `newName`, a name is empty, blank or longer than 255 characters, or a tag with the new name already exists")]
    [SwaggerResponse(403, "Only a portal administrator can rename a tag")]
    [SwaggerResponse(404, "No tag with the old name exists in the catalog")]
    [HttpPut("tags")]
    public async Task<string> UpdateRoomTag(UpdateTagRequestDto inDto)
    {
        var updatedTag = await customTagsService.UpdateTagAsync(inDto.OldName, inDto.NewName);
        return updatedTag.Name;
    }

    /// <remarks>
    /// Returns the names of the custom room tags that the caller can see, as an array of strings. Any signed-in user
    /// can call it. A DocSpace admin gets the whole catalog, including tags that no room has. Any other caller gets
    /// only the tags of the active rooms that it can see, without repeats. So an empty list
    /// means that the caller sees no tagged room. The call is read-only. The list has no total, so a page shorter than
    /// `count` is the last one. Use these names in the `tags` filter of `GET api/2.0/files/rooms` and in the room tag
    /// calls. To create a tag, use `POST api/2.0/files/tags`. To check whether a tag is in use, use
    /// `GET api/2.0/files/tags/{tagName}/haslinks`. The call returns 400 if `count` or `startIndex` is out of range.
    /// </remarks>
    /// <summary>Get available room tags</summary>
    /// <path>api/2.0/files/tags</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The tag names available to the caller", typeof(IAsyncEnumerable<string>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, or the `startIndex` is negative")]
    [HttpGet("tags")]
    public IAsyncEnumerable<string> GetRoomTagsInfo(GetTagsInfoRequestDto inDto)
    {
        return customTagsService.GetTagsInfoAsync<int>(inDto.Text, TagType.Custom, inDto.StartIndex, inDto.Count);
    }

    /// <remarks>
    /// Returns whether any room still has the named tag. Run it before you delete the tag from the catalog. Requires a
    /// DocSpace admin. Other callers get 403. The call is read-only. It returns `true` if at least one active or
    /// archived room has the tag. Then `DELETE api/2.0/files/tags` would remove the tag from those rooms. After you
    /// remove the tag from its last room, the call returns `false`. If the last room with the tag is deleted, the
    /// portal deletes the tag too. Then the call returns 404. Send the name in the path and also as the `tagName` query
    /// parameter. The call reads only the query value. On MySQL, the name match ignores case. If no tag has this name,
    /// or the query has no `tagName`, the call returns 404.
    /// </remarks>
    /// <summary>Check room tag usage</summary>
    /// <path>api/2.0/files/tags/{tagName}/haslinks</path>
    /// <collection>item</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "True when at least one room still carries the tag", typeof(bool))]
    [SwaggerResponse(403, "Only a portal administrator can check tag usage")]
    [SwaggerResponse(404, "No tag with this name exists in the catalog, or the `tagName` query parameter is missing")]
    // HasTagLinksRequestDto binds `tagName` `[FromQuery]`, so the route placeholder of the same name
    // is unbound and the value has to be sent twice - which is what the generated SDKs already do.
    [SwaggerPathParameter("tagName", "The tag being checked. Send the same value as the `tagName` query parameter, which is the one the handler reads.")]
    [HttpGet("tags/{tagName}/haslinks")]
    public async Task<bool> HasTagLinks(HasTagLinksRequestDto requestDto)
    {
        var hasTagLinks = await customTagsService.HasTagLinks(requestDto.TagName);
        return hasTagLinks;
    }

    /// <remarks>
    /// Deletes custom room tags from the portal catalog and removes them from every room. The rooms and their content
    /// stay. Requires a DocSpace admin. Other callers get 403. A room admin can create tags but also gets 403 here. The
    /// call cannot be undone. Creating the name again with `POST api/2.0/files/tags` restores the tag but not its
    /// rooms. The call matches names, ignoring case on MySQL, and skips names that are not in the catalog. An empty
    /// list changes nothing. So a success does not prove that a tag was deleted, and you can repeat the call. To check
    /// a tag first, use `GET api/2.0/files/tags/{tagName}/haslinks`. The response has no body. To remove a tag from one
    /// room only, use `DELETE api/2.0/files/rooms/{id}/tags`. The call returns 400 if `names` is missing, or a name is
    /// empty, has only spaces, or is too long.
    /// </remarks>
    /// <summary>Delete the custom room tags</summary>
    /// <path>api/2.0/files/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The tags were removed from the catalog and from every room")]
    [SwaggerResponse(400, "The request body cannot be read or has no `names`, or a name is empty, blank or longer than 255 characters")]
    [SwaggerResponse(403, "Only a portal administrator can delete tags")]
    [HttpDelete("tags")]
    public async Task DeleteCustomTags(BatchTagsRequestDto inDto)
    {
        await customTagsService.DeleteTagsAsync<int>(inDto.Names);
    }

    /// <remarks>
    /// Saves an image in temporary storage and returns its path. This is the first step of setting a room logo, and
    /// no room changes. Then pass the path as `tmpFile` to `POST api/2.0/files/rooms/{id}/logo` with the crop area.
    /// Requires a DocSpace admin or a room admin. Other callers get 403. The call changes data in temporary storage
    /// only, and each call creates a new path. So you can repeat it. Send the image as multipart form data. The call
    /// uses the first file and ignores other parts. It converts the image to PNG and scales it down to fit 1280 by 1280
    /// pixels. The call returns 400 if the request is not a multipart form or has no file. It also returns 400 if the
    /// image is empty, over the size limit, or not a readable PNG or JPEG.
    /// </remarks>
    /// <summary>Upload a room logo image</summary>
    /// <path>api/2.0/files/logos</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The path of the stored temporary image", typeof(UploadResultDto))]
    [SwaggerResponse(400, "The request is not a multipart form or carries no file, or the first file is empty, larger than the portal limit for uploaded images, or not a readable PNG or JPEG image")]
    [SwaggerResponse(403, "Only a room manager or a portal administrator can upload a logo")]
    [HttpPost("logos")]
    public async Task<UploadResultDto> UploadRoomLogo(UploadRoomLogoRequestDto inDto)
    {
        var currentUserType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);

        if (currentUserType is not (EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        if (!Request.HasFormContentType)
        {
            throw new ArgumentException("The room logo must be sent as a multipart form");
        }

        if (inDto.FormCollection.Files.Count == 0)
        {
            throw new ArgumentException("No image file was sent");
        }

        var result = new UploadResultDto();

        try
        {
            result.Data = await roomLogoManager.SaveTempAsync(inDto.FormCollection.Files[0]);
            result.Success = true;
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or ImageWeightLimitException or ImageSizeLimitException)
        {
            // The image exceptions derive from plain Exception, so without this they would surface
            // as 500; every one of them means the caller sent something unusable.
            throw new ArgumentException(ex.Message, ex);
        }

        return result;
    }

    /// <remarks>
    /// Starts a background job that exports the index of a room to a spreadsheet, and returns the job. The room must
    /// have indexing turned on. Requires a DocSpace admin or the room role room manager in this room. Other callers
    /// get 403. It also returns 403 if the folder is not a room, the room is archived, or indexing is turned off. The
    /// call queues work. Each account has one export job. If an earlier job still runs, the call returns that job and
    /// starts nothing. So a repeated call during the export changes nothing. After the job ends, a new call replaces it
    /// with a new job, which saves another spreadsheet. Repeat `GET api/2.0/files/rooms/indexexport` until
    /// `isCompleted` is true. Then read `status` to tell success from failure or cancellation. The portal saves the
    /// spreadsheet in the My documents section of the caller, not in the room. The `resultFileId` and `resultFileUrl`
    /// fields of the job point to it. To cancel the job, use `DELETE api/2.0/files/rooms/indexexport`. If the room does
    /// not exist or is a room template, the call returns 404.
    /// </remarks>
    /// <summary>Start the room index export</summary>
    /// <path>api/2.0/files/rooms/{id}/indexexport</path>
    /// <exception cref="NotSupportedException"></exception>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The queued export job to poll", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(403, "The caller may not read the folder, the folder lies in the archive or is not a room with indexing turned on, or the caller is neither its room manager nor a portal administrator")]
    [SwaggerResponse(404, "The folder does not exist or lies in the room templates section")]
    [HttpPost("rooms/{id:int}/indexexport")]
    public async Task<DocumentBuilderTaskDto> StartRoomIndexExport(RoomIdRequestDto<int> inDto)
    {
        var room = await fileStorageService.GetFolderAsync(inDto.Id).NotFoundIfNull("Folder not found");

        if (room.RootId == await globalFolderHelper.FolderRoomTemplatesAsync)
        {
            throw new ItemNotFoundException();
        }

        // An archived room is read-only, and starting a background export against it used to be
        // accepted with 200 like any other room - the same guard every other write-side room
        // operation applies (see FileStorageService's ErrorMessage_UpdateArchivedRoom checks).
        if (room.RootFolderType == FolderType.Archive)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UpdateArchivedRoom);
        }

        var fileSecurity = serviceProvider.GetService<FileSecurity>();

        if (!await fileSecurity.CanIndexExportAsync(room))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        if (!room.SettingsIndexing)
        {
            throw new InvalidOperationException("Folder indexing is turned off");
        }

        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        var task = serviceProvider.GetService<RoomIndexExportTask>();

        var commonLinkUtility = serviceProvider.GetService<CommonLinkUtility>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null);

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var headers = MessageSettings.GetHttpHeaders(Request);
        var evt = new RoomIndexExportIntegrationEvent(userId, tenantId, inDto.Id, baseUri, headers: headers != null
            ? headers.ToDictionary(x => x.Key, x => x.Value.ToString())
            : []);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    /// <remarks>
    /// Returns the room index export job of the caller, started with `POST api/2.0/files/rooms/{id}/indexexport`.
    /// Each account has at most one job, whatever room it exports. Any signed-in user can call it. The call is
    /// read-only. This is the call to poll. Repeat it until `isCompleted` is true. The `isCompleted` field is also true
    /// for a failed or cancelled job. So read `status` for the result and `error` for the message. The `percentage`
    /// field changes in large steps and does not show the remaining time. After a successful export, `resultFileId`,
    /// `resultFileName`, and `resultFileUrl` point to the spreadsheet in the My documents section of the caller. If the
    /// account has no job or cancelled it, the response body is null. The job stays after completion until the next
    /// export replaces it.
    /// </remarks>
    /// <summary>Get the room index export</summary>
    /// <path>api/2.0/files/rooms/indexexport</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The state of the export, or null when the account has none", typeof(DocumentBuilderTaskDto))]
    [HttpGet("rooms/indexexport")]
    public async Task<DocumentBuilderTaskDto> GetRoomIndexExport()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        var task = await documentBuilderTaskManager.GetTask(tenantId, userId);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <remarks>
    /// Cancels the room index export job of the caller and deletes the job. Any signed-in user can call it. The call
    /// acts only on the caller's own job, so you cannot stop the job of another account. The call changes data. If the
    /// caller has no job, the call changes nothing, so you can repeat it. The cancellation is asynchronous. A job that
    /// is already saving the file may still finish. A spreadsheet saved before the cancellation stays in the My
    /// documents section of the caller. After this call, `GET api/2.0/files/rooms/indexexport` returns an empty body
    /// until a new export starts. The response has no body and does not say whether a job was running.
    /// </remarks>
    /// <summary>Terminate the room index export</summary>
    /// <path>api/2.0/files/rooms/indexexport</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The export of the calling account was cancelled, if there was one")]
    [HttpDelete("rooms/indexexport")]
    public async Task TerminateRoomIndexExport()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        var evt = new RoomIndexExportIntegrationEvent(userId, tenantId, 0, null, true);

        await eventBus.PublishAsync(evt);
    }

    /// <remarks>
    /// Returns the items marked as new for the caller in all active rooms. The list is grouped by the day of the
    /// change, then by room. Any signed-in user can call it. An item is new when another user creates or changes it in
    /// a room that the caller has opened. So the list never has the caller's own changes. The list has only files,
    /// including files in subfolders at any depth. A new subfolder is not an item. Days come newest first. Inside a
    /// day, rooms and files follow the same order by change time. The call is read-only and does not clear the marks.
    /// Opening the room with `GET api/2.0/files/rooms/{id}` clears them. An empty array means that the caller has
    /// nothing new. To get the new items of one room, use `GET api/2.0/files/rooms/{id}/news`.
    /// </remarks>
    /// <summary>Get new items in all rooms</summary>
    /// <path>api/2.0/files/rooms/news</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The new entries of every active room, grouped by day and by room", typeof(List<NewItemsDto<RoomNewItemsDto>>))]
    [HttpGet("rooms/news")]
    public async Task<List<NewItemsDto<RoomNewItemsDto>>> GetRoomsNewItems()
    {
        var rootId = await globalFolderHelper.FolderVirtualRoomsAsync;
        var newItems = await fileStorageService.GetNewRootFilesAsync(rootId);
        var result = new List<NewItemsDto<RoomNewItemsDto>>();

        foreach (var (key, value) in newItems)
        {
            var date = apiDateTimeHelper.Get(key);
            var items = new List<RoomNewItemsDto>();

            foreach (var (k, v) in value)
            {
                var item = await rootNewItemsDtoHelper.GetAsync(k, v, (room, roomItems) =>
                    new RoomNewItemsDto
                    {
                        Room = room,
                        Items = roomItems
                    });
                items.Add(item);
            }

            result.Add(new NewItemsDto<RoomNewItemsDto> { Date = date, Items = items });
        }

        return result;
    }
}
