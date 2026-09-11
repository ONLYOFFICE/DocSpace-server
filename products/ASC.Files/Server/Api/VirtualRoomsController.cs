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
    IDaoFactory daoFactory)
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
        daoFactory)
{
    private readonly AuthContext _authContext = authContext;

    /// <remarks>
    /// Creates a room in the portal Rooms section and returns it. `roomType` decides which sharing links, member
    /// roles and form features the room offers, and it cannot be changed afterwards, so a room of the wrong kind has
    /// to be recreated. The caller must be the portal owner, a portal administrator or a room administrator; a user
    /// or a guest is refused, and so is a public room while the portal forbids external sharing. `title` is required
    /// and must not be blank: characters a folder name cannot hold are replaced with underscores and the rest is
    /// truncated, so the stored title can differ from the one sent and two rooms can share it. `quota` is accepted
    /// only while the per-room quota feature is on and must stay within the portal quota, `cover` only for an id
    /// returned by `GET api/2.0/files/rooms/covers`, and `color` as six hexadecimal digits with no leading number
    /// sign. Tag names the portal does not know yet are added to the tag catalogue. `share` is not implemented and
    /// any non-empty value is rejected, so invite members afterwards with `PUT api/2.0/files/rooms/{id}/share`.
    /// Passing the portal room limit ends the call as a billing refusal and creates nothing.
    /// </remarks>
    /// <summary>Create a room</summary>
    /// <path>api/2.0/files/rooms</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The created room with its id, type, settings, logo and tags", typeof(FolderDto<int>))]
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
    /// Starts a background job that copies a room template into a new room of the Rooms section, and answers with the
    /// same progress record that `GET api/2.0/files/rooms/fromtemplate/status` returns. The caller must be able to
    /// read the template and to create rooms at all, so a user or a guest is refused, and the checks run before the
    /// job is queued. The room does not exist when the response arrives: poll the status operation until
    /// `isCompleted` is true, then take `roomId` from it, and treat a non-empty `error` as a failed job. Only one
    /// such job is kept per account, and a finished one is discarded when the next is started, so a second creation
    /// loses the record of the first. Anything not sent is inherited from the template, and `copyLogo` keeps the
    /// template logo and makes `logo` pointless. `quota` is accepted only while the per-room quota feature is on, and
    /// a template of a public room cannot be instantiated while the portal forbids external sharing. A template that
    /// does not exist or cannot be read is answered as missing.
    /// </remarks>
    /// <summary>Create a room from the template</summary>
    /// <path>api/2.0/files/rooms/fromtemplate</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The progress record of the room creation job", typeof(RoomFromTemplateStatusDto))]
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
    /// Returns the progress of the room-from-template job started by the calling account with
    /// `POST api/2.0/files/rooms/fromtemplate`. The record is private to the account that started the job: jobs of
    /// other members are never reported, and only one record is kept per account. The body is empty when the account
    /// has no such record, and it is also empty when the job queue cannot be read, so an empty answer is not proof
    /// that nothing was started. `progress` is a percentage, `isCompleted` marks the end of the job whether it
    /// succeeded or failed, `error` carries the failure message and is empty on success, and `roomId` is meaningful
    /// only once the room exists. The record survives the end of the job and is dropped when the next creation
    /// starts, so polling after completion keeps returning the same answer. Poll this operation until `isCompleted`
    /// is true and then read the room itself with `GET api/2.0/files/rooms/{id}`. The call changes nothing and is
    /// safe to repeat.
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
    /// Queues a background job that re-exports the collected data of every original form of a form filling room into
    /// the external database configured for the portal, and returns the job record. The room must be a form filling
    /// room and the caller must be able to edit it, otherwise the call is refused with 403; an unknown room is
    /// answered with 404. The export is not done when the response arrives: poll
    /// `GET api/2.0/files/rooms/{id}/externaldbsync` until `isCompleted` is true, then read `forms` for the per-form
    /// outcome, which stays empty while the job is running. Starting the job again while it is still running returns
    /// the same record instead of a second job, so a retry is safe; a finished job is replaced by the new one. One
    /// job is kept per room. A form whose data cannot be exported does not stop the others: it comes back in `forms`
    /// with `success` false and its own `error`. When the portal has no external database configured the call fails
    /// and nothing is queued.
    /// </remarks>
    /// <summary>Start external DB sync</summary>
    /// <path>api/2.0/files/rooms/{id}/externaldbsync</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The synchronization job record to poll", typeof(ExternalDbSyncTaskDto))]
    [SwaggerResponse(400, "The portal has no external database configured")]
    [SwaggerResponse(403, "The room is not a form filling room, or the caller cannot edit it")]
    [SwaggerResponse(404, "No room with this ID is visible to the caller")]
    [HttpPost("{id}/externalDbSync")]
    public async Task<ExternalDbSyncTaskDto> StartExternalDbSync(RoomIdRequestDto<int> inDto)
    {
        var task = await _fileStorageService.StartExternalDbSyncAsync(inDto.Id);
        return ExternalDbSyncTaskDto.Get(task);
    }

    /// <remarks>
    /// Returns the record of the external database export job of a form filling room, or an empty body when the room
    /// has no job at all. The room must be a form filling room and the caller must be able to edit it, otherwise the
    /// call is refused; an unknown room is answered with 404. This is the polling target of
    /// `POST api/2.0/files/rooms/{id}/externaldbsync`: repeat it until `isCompleted` is true, and then read `forms`,
    /// which lists one entry per original form with its own `success` and `error` and is empty while the job is still
    /// running. `percentage` advances as forms are processed, `status` distinguishes a job that is queued, running,
    /// finished or failed, and `error` carries the message of a job that stopped as a whole. The record belongs to
    /// the room rather than to the account that started the job, so any member who can edit the room sees the same
    /// answer. The call changes nothing and is safe to repeat.
    /// </remarks>
    /// <summary>Get external DB sync status</summary>
    /// <path>api/2.0/files/rooms/{id}/externaldbsync</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The synchronization job record, or an empty body when the room has no job", typeof(ExternalDbSyncTaskDto))]
    [SwaggerResponse(404, "No room with this ID is visible to the caller")]
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
    IDaoFactory daoFactory)
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
        daoFactory)
{
    /// <remarks>
    /// Turns a folder of a connected third-party storage account into a room of the `Rooms` section, so that the
    /// files of the room keep living in that storage instead of the portal. Connect the account first with
    /// `POST api/2.0/files/thirdparty` and take the path parameter from a folder listing of that account: it is the
    /// identifier of a folder in the storage, not of a room. One connected account can back one room only, so a
    /// second call over the same account is refused, and so is an account that was not connected for room storage.
    /// The caller needs the right to create rooms, which a portal user and a guest do not have; a public room is
    /// refused while the administrator restricts external access, and reaching the room limit of the tariff is
    /// refused too. With `createAsNewFolder` the room is a new subfolder named after `title`, otherwise the folder
    /// from the path becomes the room itself and `indexing`, `denyDownload`, `tags` and `logo` are then dropped. The
    /// answer is the new room, whose identifiers are strings; a public or a form-filling room already has its primary
    /// link, readable with `GET api/2.0/files/rooms/{id}/link`.
    /// </remarks>
    /// <summary>Create a third-party room</summary>
    /// <path>api/2.0/files/rooms/thirdparty/{id}</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room created out of the third-party folder, with string identifiers", typeof(FolderDto<string>))]
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
    IDaoFactory daoFactory)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    protected readonly FileStorageService _fileStorageService = fileStorageService;

    /// <remarks>
    /// Returns one room with its type, title, tags, logo, cover, colour, quota and virtual data room settings,
    /// together with the access level the caller has in it. Reading the room is not a side-effect-free call: it
    /// clears the caller new-item badges for that room, and `newForMe` comes back as 0, so read
    /// `GET api/2.0/files/rooms/{id}/news` first when the new items matter. The caller needs read access to the room;
    /// portal administrators can read a room they were never invited to, while a member without access is refused.
    /// The operation also answers an anonymous caller, but only in the context of a valid external share link of that
    /// room, and a plain anonymous request is rejected as unauthenticated. A room that never existed, was deleted, or
    /// lives in a section the caller cannot see is answered as missing. Archived rooms are returned as well and are
    /// recognised by their root section rather than by a separate flag. Use `GET api/2.0/files/rooms` to search and
    /// page through rooms instead of guessing ids.
    /// </remarks>
    /// <summary>Get room information</summary>
    /// <path>api/2.0/files/rooms/{id}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its settings and the access level of the caller", typeof(FolderDto<int>))]
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<FolderDto<T>> GetRoomInfo(RoomIdRequestDto<T> inDto)
    {
        var folder = (await _fileStorageService.GetRoomInfoAsync(inDto.Id)).NotFoundIfNull("Folder not found");

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <remarks>
    /// Applies a partial change to one room and returns the whole room as it is after it. Only the fields present in
    /// the body are touched, an empty body changes nothing, and a property the body does not define is rejected as an
    /// invalid request instead of being ignored. The caller must be a manager of this room: portal administrators do
    /// not get in without an invitation, and an archived room is refused. `title` is trimmed, sanitised the way a
    /// room title is sanitised at creation, and a blank value is treated as no change. `tags` replaces the whole tag
    /// set and an empty array clears it, an empty `color` restores the default and an empty `cover` removes the
    /// cover. A `quota` of -1 switches the room back to no custom limit, any other negative value restores the portal
    /// default, and a positive one is accepted only while the per-room quota feature is on. Turning `indexing` on
    /// renumbers the room contents. `chatSettings` belongs to an AI room and is rejected anywhere else. Use
    /// `POST api/2.0/files/rooms/{id}/logo` for logo cropping.
    /// </remarks>
    /// <summary>Update a room</summary>
    /// <path>api/2.0/files/rooms/{id}</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room as it is after the update", typeof(FolderDto<int>))]
    [HttpPut("{id}")]
    public async Task<FolderDto<T>> UpdateRoom(UpdateRoomRequestDto<T> inDto)
    {
        var room = await _fileStorageService.UpdateRoomAsync(inDto.Id, inDto.UpdateRoom);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Sets the same custom storage limit, in bytes, on every listed room and streams the updated rooms back in the
    /// order they were given. The per-room quota feature has to be on for the portal, and the value must stay within
    /// the portal own limit, otherwise the call is refused before anything is written. The caller must be a manager
    /// of each listed room, and an archived room or a room in the trash is refused. The list is not transactional:
    /// rooms processed before the offending one keep their new limit, so a failed call has to be checked room by
    /// room. Only numeric room ids are processed, which means ids of rooms stored in a connected third-party account
    /// are silently skipped. A room whose limit already equals the requested value is left untouched and still
    /// returned. To go back to the portal default use `PUT api/2.0/files/rooms/resetquota`, and to drop the custom
    /// limit entirely send a quota of -1 to `PUT api/2.0/files/rooms/{id}`.
    /// </remarks>
    /// <summary>Change the room quota limit</summary>
    /// <path>api/2.0/files/rooms/roomquota</path>
    /// <collection>list</collection>
    [Tags("Files / Quota")]
    [SwaggerResponse(200, "The rooms as they are after the new limit was applied", typeof(IAsyncEnumerable<FolderDto<int>>))]
    [HttpPut("roomquota")]
    public async IAsyncEnumerable<FolderDto<int>> UpdateRoomsQuota(UpdateRoomsQuotaRequestDto<T> inDto)
    {
        var (folderIntIds, _) = FileOperationsManager.GetIds(inDto.RoomIds);

        var folderTitles = new List<string>();

        foreach (var roomId in folderIntIds)
        {
            var room = await _fileStorageService.FolderQuotaChangeAsync(roomId, inDto.Quota);
            folderTitles.Add(room.Title);
            yield return await _folderDtoHelper.GetAsync(room);
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
    /// Returns every listed room to the default room quota of the portal and streams the updated rooms back in the
    /// order they were given. This is not the same as removing the limit: the room stops carrying its own value and
    /// starts following the portal default, which a portal administrator can change at any time. The per-room quota
    /// feature has to be on, the caller must be a manager of each listed room, and an archived room or a room in the
    /// trash is refused. The list is not transactional, so rooms processed before a failing one keep the default and
    /// the rest keep what they had. Only numeric room ids are processed, which means ids of rooms stored in a
    /// connected third-party account are silently skipped. Use `PUT api/2.0/files/rooms/roomquota` to set an explicit
    /// value, and a quota of -1 in `PUT api/2.0/files/rooms/{id}` to leave the room with no custom limit at all.
    /// </remarks>
    /// <summary>Reset the room quota limit</summary>
    /// <path>api/2.0/files/rooms/resetquota</path>
    /// <collection>list</collection>
    [Tags("Files / Quota")]
    [SwaggerResponse(200, "The rooms as they are after the default limit was restored", typeof(IAsyncEnumerable<FolderDto<int>>))]
    [HttpPut("resetquota")]
    public async IAsyncEnumerable<FolderDto<int>> ResetRoomQuota(UpdateRoomsRoomIdsRequestDto<T> inDto)
    {
        var (folderIntIds, _) = FileOperationsManager.GetIds(inDto.RoomIds);
        var folderTitles = new List<string>();
        var quotaRoomSettings = await settingsManager.LoadAsync<TenantRoomQuotaSettings>();

        foreach (var roomId in folderIntIds)
        {
            var room = await _fileStorageService.FolderQuotaChangeAsync(roomId, -2);
            folderTitles.Add(room.Title);

            yield return await _folderDtoHelper.GetAsync(room);
        }

        filesMessageService.Send(MessageAction.CustomQuotaPerRoomDefault, quotaRoomSettings.DefaultQuota.ToString(), folderTitles.ToArray());
    }


    /// <remarks>
    /// Queues a background job that deletes one room with everything inside it, and returns the operation record of
    /// that job. Deleting a room is destructive and has no trash step: the room and its files are gone once the job
    /// finishes, unlike a file or a folder, which is moved to the trash first. The right to delete is checked before
    /// the job is queued, so a caller who may not delete the room is refused straight away and an unknown room is
    /// answered as missing; the same checks run again when the job starts, which is why the `error` of the finished
    /// operation still has to be read. Poll `GET api/2.0/files/fileops` until `finished` is true, or read the
    /// returned record again by its `id`. The record is kept until it is read once, so one poll after completion
    /// still sees it. `deleteAfter` in the body is required by the contract but has no effect on the job. An archived
    /// room is deleted the same way, and a second delete of the same id reports that the room is missing.
    /// </remarks>
    /// <summary>Remove a room</summary>
    /// <path>api/2.0/files/rooms/{id}</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The queued delete operation to poll", typeof(FileOperationDto))]
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
    /// Queues a background job that moves one room from the Rooms section to the Archive section, and returns the
    /// operation record of that job. An archived room stays readable to its members and becomes read only: files
    /// cannot be created, renamed or edited in it, and its settings, tags, logo and links can no longer be changed,
    /// which is why many other room operations answer an archived room with a refusal. The caller must be a manager
    /// of the room; administrators of the portal cannot archive a room they were not invited to, and a room template
    /// cannot be archived at all and is answered as missing. The room is not archived when the response arrives: poll
    /// `GET api/2.0/files/fileops` until `finished` is true. Archiving an already archived room is harmless.
    /// `deleteAfter` decides only how long the finished record survives, not what happens to the room. Use
    /// `PUT api/2.0/files/rooms/{id}/unarchive` to bring the room back.
    /// </remarks>
    /// <summary>Archive a room</summary>
    /// <path>api/2.0/files/rooms/{id}/archive</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The queued archive operation to poll", typeof(FileOperationDto))]
    [HttpPut("{id}/archive")]
    public async Task<FileOperationDto> ArchiveRoom(ArchiveRoomRequestDto<T> inDto)
    {
        var room = await _fileStorageService.GetFolderAsync(inDto.Id);
        if (room.RootId is int root && root == await globalFolderHelper.FolderRoomTemplatesAsync)
        {
            throw new ItemNotFoundException();
        }

        var destFolder = JsonSerializer.SerializeToElement(await globalFolderHelper.FolderArchiveAsync);
        var movableRoom = JsonSerializer.SerializeToElement(inDto.Id);

        var taskId = await fileMoveCopyOperationsManager.Publish([movableRoom], [], destFolder, false, FileConflictResolveType.Skip, !inDto.ArchiveRoom.DeleteAfter, false);
        var tasks = await fileMoveCopyOperationsManager.GetOperationResults(id: taskId);

        return await fileOperationDtoHelper.GetAsync(tasks.FirstOrDefault());
    }

    /// <remarks>
    /// Queues a background job that moves one room from the Archive section back to the Rooms section, and returns
    /// the operation record of that job. The room becomes writable again with the membership, tags, logo and links it
    /// had before, while the pinned state of its members is not restored and has to be set again with
    /// `PUT api/2.0/files/rooms/{id}/pin`. The caller must be a manager of the room; a member who was only invited to
    /// it is refused, a room template is answered as missing, and a room that was never archived simply stays where
    /// it is. The room is not moved when the response arrives: poll `GET api/2.0/files/fileops` until `finished` is
    /// true, and expect a room that is still archived until then. `deleteAfter` decides only how long the finished
    /// record survives. Calling the operation twice in a row does not corrupt the room, and a deleted or unknown room
    /// id is reported as missing.
    /// </remarks>
    /// <summary>Unarchive a room</summary>
    /// <path>api/2.0/files/rooms/{id}/unarchive</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The queued unarchive operation to poll", typeof(FileOperationDto))]
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
    /// Adds, changes and removes room members in one batch, and returns the resulting access list of the named
    /// subjects. Each entry names either an account or a group of the portal, or the email address of somebody who
    /// has no account yet, together with the access level to grant; an access of 0 removes the subject from the room.
    /// An entry without an access level is ignored, the same subject listed twice keeps the last level, and an empty
    /// list is accepted and changes nothing. The caller must be a manager of the room, so an invitation sent by a
    /// user or a guest is refused, and an account that is a portal user or a guest cannot be made a room manager.
    /// Inviting by email also needs the portal to allow guest invitations. A subject the caller is not allowed to see
    /// is dropped without an error, which is why the answer has to be compared with the request. Removing a member
    /// who still holds a form role is refused through `error` unless `force` is set. `notify` sends the invitation
    /// email with the optional `message`.
    /// </remarks>
    /// <summary>Set the room access rights</summary>
    /// <path>api/2.0/files/rooms/{id}/share</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The access entries of the named subjects, plus a warning or an error when something was not applied", typeof(RoomSecurityDto))]
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
    /// Returns one page of the access list of a room: the owner first, then the managers, the groups, the ordinary
    /// members, the guests and finally the invitations nobody has accepted yet, with the total in the response
    /// headers. `filterType` selects what is listed and defaults to accounts and groups, which leaves the sharing
    /// links of the room out; those are read with `GET api/2.0/files/rooms/{id}/links`. `filterValue` matches the
    /// displayed name of the subject, and an invitation that is still pending is listed under the email address it
    /// was sent to. Paging is done with `count` and `startIndex`, and the order is stable between calls. Any member
    /// who can read the room sees the accounts and the groups, so the list is not limited to the managers, and portal
    /// administrators can read the list of a room they were never invited to; somebody who is not in the room at all
    /// is refused. Asking for the link entries instead needs the right to see the links of the room, and a member
    /// without it gets an empty page rather than an error.
    /// </remarks>
    /// <summary>Get the room access rights</summary>
    /// <path>api/2.0/files/rooms/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "One page of the room access entries, ordered by role and then by name", typeof(IAsyncEnumerable<FileShareDto>))]
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
    /// Creates, updates or deletes one sharing link of a room and returns it. `linkType` chooses the kind: an
    /// invitation link makes whoever opens it a member with the given access level, while an external link opens the
    /// room without an account. Omitting `linkId` creates a link, passing the id of an existing one updates it, and
    /// an unknown id is created with that id; the kind of an existing link cannot be changed afterwards. An access
    /// level of 0 deletes the link, and deleting the primary external link of a public or form filling room
    /// immediately replaces it with a fresh one, so such a room is never left without one. A room keeps at most one
    /// invitation link, and a second one is refused; form filling rooms take no invitation links, and collaboration,
    /// form filling and virtual data rooms take no external links. An expiration date in the past is dropped silently
    /// for an external link and rejected for an invitation link. `password`, `denyDownload` and `internal` apply to
    /// external links only.
    /// </remarks>
    /// <summary>Set the room external or invitation link</summary>
    /// <path>api/2.0/files/rooms/{id}/links</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The link as it is after the change, or an empty body when nothing was created", typeof(FileShareDto))]
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
    /// Returns the sharing links of a room, with the invitation and the external links mixed together unless `type`
    /// narrows it to one kind. Each entry carries the link address, its title, access level, expiration, the flag
    /// that marks the primary external link of the room and, for invitation links, how many times it may still be
    /// used. Public and form filling rooms come with an external link created for them, so an empty answer there
    /// means that the link was revoked rather than that the room is private; rooms of the other kinds start with no
    /// links at all and only gain one when somebody creates it, which for a collaboration room and a virtual data
    /// room can be an invitation link alone. The caller needs access to the room and the right to see its links: a
    /// member invited without that right gets an empty list rather than an error, while somebody who is not in the
    /// room at all is refused. Paging parameters are not honoured here: the first hundred links are returned and the
    /// reported count is the number of entries actually sent.
    /// </remarks>
    /// <summary>Get the room links</summary>
    /// <path>api/2.0/files/rooms/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The sharing links of the room", typeof(IAsyncEnumerable<FileShareDto>))]
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
    /// Returns the primary external link of a room, which is the one address meant to be handed out to people outside
    /// the portal. A public room and a form filling room get such a link when they are created, and asking for it
    /// again returns the same link rather than a new one, so the answer is stable. In a room that has no primary link
    /// yet this call creates one instead of reporting nothing, which needs the right to manage the links of the room:
    /// a member invited with a lower level is refused with 403, and so is anybody who is not in the room at all. A
    /// link that was explicitly revoked stays revoked and is reported as missing rather than recreated, and an
    /// unknown room is answered with 404 as well. An archived public room still reports its link. The answer is the
    /// same entry that `GET api/2.0/files/rooms/{id}/links` returns with the primary flag set, including the request
    /// token that has to travel with the address.
    /// </remarks>
    /// <summary>Get the room primary external link</summary>
    /// <path>api/2.0/files/rooms/{id}/link</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The primary external link of the room", typeof(FileShareDto))]
    [SwaggerResponse(403, "The caller may not see the links of this room")]
    [SwaggerResponse(404, "No room with this ID is visible to the caller, or its primary link was revoked")]
    [HttpGet("{id}/link")]
    public async Task<FileShareDto> GetRoomsPrimaryExternalLink(RoomIdRequestDto<T> inDto)
    {
        var linkAce = await _fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.Folder);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Attaches the named tags to a room and returns the room with its whole tag set. Tags are portal-wide labels
    /// shared by every room, and a name that the catalogue does not hold yet is created there by this call, so
    /// attaching is also the short way of adding a tag to the portal. Names already attached to the room are kept as
    /// they are, and repeating the call changes nothing, which makes it safe to retry. An empty list is accepted and
    /// does nothing, while a blank or overlong name is rejected as an invalid request. The caller must be a manager
    /// of the room or an administrator of the portal, and a room in the Archive section is refused with 403. A tag
    /// has no identifier of its own and is addressed by name, so `GET api/2.0/files/tags` is what shows which names
    /// already exist. Use `DELETE api/2.0/files/rooms/{id}/tags` to detach them again, which leaves the tags
    /// themselves in the catalogue.
    /// </remarks>
    /// <summary>Attach tags to a room</summary>
    /// <path>api/2.0/files/rooms/{id}/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its tag set after the change", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "The caller may not edit this room, or the room is archived")]
    [HttpPut("{id}/tags")]
    public async Task<FolderDto<T>> AddRoomTags(BatchTagsRequestDto<T> inDto)
    {
        var room = await customTagsService.AddRoomTagsAsync(inDto.Id, inDto.BatchTags.Names);
        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Detaches the named tags from a room and returns the room with its remaining tag set. Only the link between the
    /// room and the tag is removed: the tag stays in the portal catalogue and keeps working for every other room, and
    /// `DELETE api/2.0/files/tags` is what removes it from the portal itself. Names that are not in the catalogue, or
    /// not attached to this room, are skipped without an error, so a successful answer does not prove that anything
    /// was detached; compare the returned tag set instead. An empty list is accepted and does nothing, while a null
    /// entry in the list is rejected as an invalid request. The caller must be a manager of the room or an
    /// administrator of the portal, and a room in the Archive section is refused with 403. A tag that loses its last
    /// room stays in the catalogue, and only deleting that room takes the tag with it.
    /// </remarks>
    /// <summary>Detach tags from a room</summary>
    /// <path>api/2.0/files/rooms/{id}/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its tag set after the change", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "The caller may not edit this room, or the room is archived")]
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
    /// Turns an image already uploaded to the portal into the logo of a room and returns the room with the addresses
    /// of the four logo sizes. This is the second half of a two-step flow: upload the picture with
    /// `POST api/2.0/files/logos` first and pass the path it returns as `tmpFile`, because the image itself is never
    /// sent here. The temporary file belongs to the account that uploaded it and is consumed by this call, so it
    /// cannot be reused for a second room and a path somebody else uploaded is refused. `x`, `y`, `width` and
    /// `height` crop the picture; sending a position without a size is rejected as an invalid request, while a size
    /// without a position is accepted. An empty `tmpFile` leaves the room as it is. A logo replaces the cover in the
    /// interface without erasing it, and removing the logo brings the cover back. The caller must be a manager of the
    /// room, an archived room is refused, and an unknown room is answered with 404.
    /// </remarks>
    /// <summary>Set the room logo</summary>
    /// <path>api/2.0/files/rooms/{id}/logo</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with the addresses of its new logo", typeof(FolderDto<int>))]
    [SwaggerResponse(404, "No room with this ID is visible to the caller")]
    [HttpPost("{id}/logo")]
    public async Task<FolderDto<T>> CreateRoomLogo(LogoRequest<T> inDto)
    {
        var room = await roomLogoManager.CreateAsync(inDto.Id, inDto.Logo.TmpFile, inDto.Logo.X, inDto.Logo.Y, inDto.Logo.Width, inDto.Logo.Height);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Sets the cover picture and the background colour a room is shown with, and returns the whole room afterwards.
    /// `cover` accepts only an identifier listed by `GET api/2.0/files/rooms/covers`, and `color` only six
    /// hexadecimal digits with no leading number sign, so anything else is rejected as an invalid request. Either
    /// field may be sent on its own, an empty `cover` clears the picture, an empty `color` restores the default one,
    /// and an empty body leaves the room untouched. The cover is what the room shows while it has no uploaded logo:
    /// setting a logo with `POST api/2.0/files/rooms/{id}/logo` hides the cover without erasing it, and deleting that
    /// logo brings it back. The caller must be a manager of the room, an archived room is refused with 403, and an
    /// unknown or deleted room is answered with 404. Repeating the same request is harmless, and the cover survives
    /// archiving and unarchiving.
    /// </remarks>
    /// <summary>Change the room cover</summary>
    /// <path>api/2.0/files/rooms/{id}/cover</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room as it is after the cover change", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "The caller may not edit this room, or the room is archived")]
    [SwaggerResponse(404, "No room with this ID is visible to the caller")]
    [HttpPost("{id}/cover")]
    public async Task<FolderDto<T>> ChangeRoomCover(CoverRequestDto<T> inDto)
    {
        var room = await roomLogoManager.ChangeCoverAsync(inDto.Id, inDto.Cover.Color, inDto.Cover.Cover);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Returns the gallery of cover pictures a room can be given: every entry pairs the identifier to send to
    /// `POST api/2.0/files/rooms/{id}/cover` with the drawing itself as inline vector markup ready to be rendered.
    /// The gallery is built into the product rather than stored per portal, so it is the same for every account and
    /// every room, does not depend on what rooms exist, and its identifiers do not change with the language of the
    /// request. The identifiers are unique and stable, which makes them safe to keep in a client, while the drawings
    /// behind them may change between product versions. Any account of the portal may read the gallery, but a guest
    /// is refused. The list is the only source of valid cover identifiers: a value that is not in it is rejected
    /// wherever a cover is set, including room creation and room update. The call changes nothing and is safe to
    /// repeat.
    /// </remarks>
    /// <summary>Get room cover gallery</summary>
    /// <path>api/2.0/files/rooms/covers</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The built-in room covers with their identifiers and vector markup", typeof(IAsyncEnumerable<CoversResultDto>))]
    [HttpGet("covers")]
    public async IAsyncEnumerable<CoversResultDto> GetRoomCovers()
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        foreach (var c in await RoomLogoManager.GetCoversAsync())
        {
            yield return new CoversResultDto { Id = c.Key, Data = c.Value };
        }
    }

    /// <remarks>
    /// Removes the uploaded logo of a room and returns the room with empty logo addresses. What the room falls back
    /// to is its cover and colour, which the logo only hid: if a cover was set before the logo, it is shown again,
    /// and `POST api/2.0/files/rooms/{id}/cover` is what changes it. Nothing else about the room is touched, so
    /// membership, tags, links and settings are preserved. A room that has no logo is accepted and answered with 200,
    /// and repeating the call is therefore harmless. The caller must be a manager of the room; a member invited even
    /// with editing rights is refused, and so is a room in the Archive section. A room that does not exist or was
    /// deleted is answered as missing. After the logo is removed a new one can be set again through
    /// `POST api/2.0/files/logos` followed by `POST api/2.0/files/rooms/{id}/logo`.
    /// </remarks>
    /// <summary>Remove a room logo</summary>
    /// <path>api/2.0/files/rooms/{id}/logo</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its logo removed", typeof(FolderDto<int>))]
    [HttpDelete("{id}/logo")]
    public async Task<FolderDto<T>> DeleteRoomLogo(RoomIdRequestDto<T> inDto)
    {
        var room = await roomLogoManager.DeleteAsync(inDto.Id);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Pins a room to the top of the room list of the calling account and returns the room with the pinned flag set.
    /// Pinning is personal: it changes the order only for the caller, is invisible to the other members of the room,
    /// and does not survive a trip through the Archive section, so an unarchived room has to be pinned again. Pinned
    /// rooms stay above the unpinned ones whatever sorting or filter the listing uses, and their own order between
    /// each other is stable. An account may keep only a limited number of pinned rooms at a time, ten on a portal
    /// with the default configuration, and AI rooms are counted separately against their own allowance; a request
    /// over the limit is refused until something is unpinned with `PUT api/2.0/files/rooms/{id}/unpin`. Pinning a
    /// room that is already pinned changes nothing and is safe to repeat. Anybody who can read the room may pin it,
    /// including guests and portal administrators who were never invited, while somebody who is not in the room is
    /// refused, an archived room is rejected and an unknown room is answered as missing.
    /// </remarks>
    /// <summary>Pin a room</summary>
    /// <path>api/2.0/files/rooms/{id}/pin</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its pinned flag set for the caller", typeof(FolderDto<int>))]
    [HttpPut("{id}/pin")]
    public async Task<FolderDto<T>> PinRoom(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.SetPinnedStatusAsync(inDto.Id, true);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Removes a room from the pinned group of the calling account and returns the room with the pinned flag cleared.
    /// Only the personal ordering of the caller changes: the room itself, its members, their roles and its contents
    /// are left exactly as they were, and the room stays in the list, simply among the unpinned ones. Unpinning frees
    /// one of the pin slots of the account, which AI rooms count separately, so it is the way out of a refused
    /// `PUT api/2.0/files/rooms/{id}/pin`. Unpinning a room that was never pinned is accepted and changes nothing, so
    /// the call can be repeated safely and its answer does not prove that anything was pinned before. Anybody who can
    /// read the room may unpin it, while somebody who is not in the room at all is refused and an unknown or deleted
    /// room is answered as missing. An archived room cannot be unpinned.
    /// </remarks>
    /// <summary>Unpin a room</summary>
    /// <path>api/2.0/files/rooms/{id}/unpin</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room with its pinned flag cleared for the caller", typeof(FolderDto<int>))]
    [HttpPut("{id}/unpin")]
    public async Task<FolderDto<T>> UnpinRoom(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.SetPinnedStatusAsync(inDto.Id, false);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Sends the room invitation email again to members who were invited but have not joined yet. `resendAll` covers
    /// every pending invitation of the room and makes `usersIds` irrelevant, while an explicit list without that flag
    /// is limited to the named accounts. An account that has already accepted the invitation, is not a member of the
    /// room, or is invisible to the caller is skipped without an error, and a request that names nobody and does not
    /// set the flag does nothing, so a successful answer never proves that a message went out. Nothing about the room
    /// or its membership changes, and the operation can be repeated. The caller must be a manager of the room, an
    /// archived room is refused, a room template is answered as missing, and a malformed account id is rejected as an
    /// invalid request. The call is rate limited, so a client that loops over members should send one batch instead.
    /// The response carries no body.
    /// </remarks>
    /// <summary>Resend the room invitations</summary>
    /// <path>api/2.0/files/rooms/{id}/resend</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The invitations that were still pending have been sent again")]
    [HttpPost("{id}/resend")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task ResendEmailInvitations(UserInvitationRequestDto<T> inDto)
    {
        await _fileStorageService.ResendEmailInvitationsAsync(inDto.Id, inDto.UserInvitation.UsersIds, inDto.UserInvitation.ResendAll);
    }

    /// <remarks>
    /// Renumbers the manual order of the items lying directly in a room so that they run from one upwards with no
    /// gaps and no duplicates, and returns the room. The order of the items relative to each other is preserved: only
    /// the numbers are compacted, and nothing is moved, renamed, duplicated or deleted. Files and folders share one
    /// sequence. Nested folders keep their own numbering and are not touched, so each level is compacted on its own.
    /// The operation is meant for a room with indexing turned on, where the manual order is what listings follow; a
    /// room without indexing accepts it and simply has nothing that depends on the result. Running it twice changes
    /// nothing the second time, and an already dense sequence is left as it is, which makes the call safe to retry.
    /// The caller must be a manager of the room; a member invited with any other level is refused, an archived room
    /// is rejected, and an unknown or deleted room is answered as missing.
    /// </remarks>
    /// <summary>Reorder room contents</summary>
    /// <path>api/2.0/files/rooms/{id}/reorder</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The room whose contents were renumbered", typeof(FolderDto<int>))]
    [HttpPut("{id}/reorder")]
    public async Task<FolderDto<T>> ReorderRoom(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.ReOrderAsync(inDto.Id);
        await filesMessageService.SendAsync(MessageAction.FolderIndexReordered, room, room.Title);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <remarks>
    /// Returns what is new for the calling account in one room, grouped by the day the entry was last changed, with
    /// the newest day first and the entries inside a day ordered from the most recent. Only files are reported: a
    /// folder somebody else created is not an entry of its own, while a file created inside it is, however deep it
    /// lies. What the caller changed is never new for the caller, and a file that was deleted afterwards disappears
    /// from the answer. Reading this list leaves the badges alone, which is what makes it the operation to call
    /// before `GET api/2.0/files/rooms/{id}`, since opening the room clears them. An empty array therefore means that
    /// there is nothing new, not that the badges were already read. The caller needs access to the room; somebody who
    /// is not a member is refused, and an unknown or deleted room is answered as missing. Use
    /// `GET api/2.0/files/rooms/news` for the same report across every room at once.
    /// </remarks>
    /// <summary>Get new items in a room</summary>
    /// <path>api/2.0/files/rooms/{id}/news</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The new files of the room, grouped by day", typeof(List<NewItemsDto<FileEntryBaseDto>>))]
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
    RootNewItemsDtoHelper rootNewItemsDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Lists the rooms of one section of the portal: the active rooms by default, or the archive, the form-filling
    /// section or the room templates, chosen with `searchArea`. The rooms arrive in `folders` while `files` stays
    /// empty, `current` describes the section itself, and `total` counts every room that matched the filters before
    /// paging. A caller sees only the rooms they created or were invited to, while a portal administrator sees all of
    /// them, so an empty answer means nothing is visible to this account rather than nothing exists. The remaining
    /// parameters narrow the same set, by room type, title, tags, member, owner, storage, quota and privacy, and they
    /// combine with each other. Sorting is not free of side effects: a `sortBy` value is also stored as this
    /// account's default order for later listings, and omitting it reuses the stored order. Page the result with
    /// `count` and `startIndex`. Read a single room with `GET api/2.0/files/rooms/{id}`, and create one with
    /// `POST api/2.0/files/rooms`.
    /// </remarks>
    /// <summary>Get rooms</summary>
    /// <path>api/2.0/files/rooms</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The rooms of the selected section with the paging counters", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "The caller cannot read the selected section")]
    [HttpGet("rooms")]
    public async Task<FolderContentDto<int>> GetRoomsFolder(RoomContentRequestDto inDto)
    {
        var parentId = inDto.SearchArea switch
        {
            SearchArea.Archive => await globalFolderHelper.GetFolderArchive(),
            SearchArea.Templates => await globalFolderHelper.GetFolderRoomTemplatesAsync(),
            SearchArea.FormTemplates => await globalFolderHelper.GetFolderRoomTemplatesAsync(),
            SearchArea.Forms => await globalFolderHelper.GetFolderFormsAsync(),
            _ => await globalFolderHelper.GetFolderVirtualRooms()
        };

        var filter = RoomTypeExtensions.MapToFilterType(inDto.Type);

        var tagNames = !string.IsNullOrEmpty(inDto.Tags)
            ? JsonSerializer.Deserialize<IEnumerable<string>>(inDto.Tags)
            : null;

        // An unrecognised sortBy used to be dropped on the floor: the listing came back in the
        // default order and the caller had no way to tell its sort had been ignored. The accepted
        // values are the names of SortedByType - sorting by name is "AZ", not "title".
        OrderBy orderBy = null;
        if (!string.IsNullOrEmpty(inDto.SortBy))
        {
            if (!SortedByTypeExtensions.TryParse(inDto.SortBy, true, out var sortBy))
            {
                throw new ArgumentException(FilesCommonResource.ErrorMessage_BadRequest, nameof(inDto.SortBy));
            }

            orderBy = new OrderBy(sortBy, inDto.SortOrder == SortOrder.Ascending);
        }

        var startIndex = inDto.StartIndex;
        var count = inDto.Count;
        var filterValue = inDto.Text;

        var content = await fileStorageService.GetFolderItemsAsync(
            parentId,
            startIndex,
            count,
            filter,
            false,
            inDto.SubjectId,
            Guid.Empty,
            filterValue,
            [],
            true,
            false,
            orderBy,
            inDto.SearchArea ?? SearchArea.Active,
            0,
            inDto.WithoutTags ?? false,
            tagNames,
            inDto.ExcludeSubject ?? false,
            inDto.Provider ?? ProviderFilter.None,
            inDto.SubjectOwnerId,
            quotaFilter: inDto.QuotaFilter ?? QuotaFilter.All,
            storageFilter: inDto.StorageFilter ?? StorageFilter.None,
            groupId: inDto.GroupId ?? null,
            privacyFilter: inDto.PrivacyFilter ?? RoomPrivacyFilter.None);

        var dto = await folderContentDtoHelper.GetAsync(parentId, content, startIndex);

        return dto.NotFoundIfNull();
    }

    /// <remarks>
    /// Adds a custom tag to the portal-wide catalog of room tags and answers with the stored name. Tags are shared by
    /// the whole portal instead of belonging to the caller: once the tag exists, every room manager can attach it to
    /// their own rooms with `PUT api/2.0/files/rooms/{id}/tags`, and that call also creates a tag it does not find.
    /// Creating a name that is already in the catalog returns the existing tag unchanged rather than a duplicate or
    /// an error, so repeating the call after a timeout is safe. A blank name, or one longer than the published limit,
    /// is rejected as an invalid request. Only a room manager or a portal administrator may create a tag, and a user
    /// or a guest is refused. The answer is the name as stored, and that name is the value to send in the `tags`
    /// filter of `GET api/2.0/files/rooms` and in the room tag calls. The catalog itself is read with
    /// `GET api/2.0/files/tags`.
    /// </remarks>
    /// <summary>Create a room tag</summary>
    /// <path>api/2.0/files/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The name of the created tag, or of the tag that already carried this name", typeof(string))]
    [SwaggerResponse(403, "Only a room manager or a portal administrator can create tags")]
    [HttpPost("tags")]
    public async Task<string> CreateRoomTag(CreateTagRequestDto inDto)
    {
        var createdTag = await customTagsService.CreateTagAsync(inDto.Name);
        return createdTag.Name;
    }

    /// <remarks>
    /// Renames a custom room tag in the portal catalog. The rename follows the tag everywhere it is used: every room
    /// that carries it keeps it and shows the new name, so nothing has to be re-attached afterwards. Only a portal
    /// administrator may rename a tag, and a room manager who is allowed to create tags is still refused here. The
    /// old name is matched exactly as it is stored rather than searched for, and a name that is not in the catalog is
    /// answered as missing. A new name that another tag already occupies is rejected as an invalid request, because
    /// tag names are unique across the portal; both names must be non-blank and within the published length limit.
    /// The answer is the new name. Stored queries are not updated for the caller: a `tags` filter of
    /// `GET api/2.0/files/rooms` that still names the old value stops matching anything. The catalog is read with
    /// `GET api/2.0/files/tags`.
    /// </remarks>
    /// <summary>Rename a room tag</summary>
    /// <path>api/2.0/files/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The new name of the renamed tag", typeof(string))]
    [SwaggerResponse(403, "Only a portal administrator can rename a tag")]
    [HttpPut("tags")]
    public async Task<string> UpdateRoomTag(UpdateTagRequestDto inDto)
    {
        var updatedTag = await customTagsService.UpdateTagAsync(inDto.OldName, inDto.NewName);
        return updatedTag.Name;
    }

    /// <remarks>
    /// Returns the custom room tags available to the caller as a flat array of names, not of objects. What the array
    /// holds depends on the account: a portal administrator gets the whole catalog, including tags that no room uses
    /// yet, while every other account gets only the tags attached to rooms it can see, with duplicates removed. An
    /// empty answer therefore means that this caller sees no tagged room, not that the portal has no tags.
    /// `filterValue` keeps the names that contain the given text, ignoring case, while `count` and `startIndex` page
    /// the result; no total is returned, so a page shorter than `count` is the signal that the list is exhausted. The
    /// names are exactly the values accepted by the `tags` filter of `GET api/2.0/files/rooms` and by the room tag
    /// calls, which makes this the call to fill a tag picker with. Add a tag with `POST api/2.0/files/tags` and check
    /// whether one is still in use with `GET api/2.0/files/tags/{tagName}/haslinks`.
    /// </remarks>
    /// <summary>Get available room tags</summary>
    /// <path>api/2.0/files/tags</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The tag names available to the caller", typeof(IAsyncEnumerable<string>))]
    [HttpGet("tags")]
    public IAsyncEnumerable<string> GetRoomTagsInfo(GetTagsInfoRequestDto inDto)
    {
        return customTagsService.GetTagsInfoAsync<int>(inDto.Text, TagType.Custom, inDto.StartIndex, inDto.Count);
    }

    /// <remarks>
    /// Reports whether any room still carries the named tag, which is the check to run before the tag is deleted from
    /// the catalog. Only a portal administrator may call it, and every other account is refused. The name is matched
    /// exactly against the catalog, and a name that is not in it is answered with 404. That also tells the two ways a
    /// tag stops being used apart: taking the tag off the last room that carried it leaves the tag in the catalog and
    /// turns the answer to false, while deleting that last room removes the tag itself, after which the call answers
    /// 404. A true answer means at least one room, active or archived, still references the tag, so deleting it with
    /// `DELETE api/2.0/files/tags` would strip it from those rooms. The handler reads the tag name from the query
    /// string, so the value has to be sent twice: in the path segment and as the `tagName` query parameter.
    /// </remarks>
    /// <summary>Check room tag usage</summary>
    /// <path>api/2.0/files/tags/{tagName}/haslinks</path>
    /// <collection>item</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "True when at least one room still carries the tag", typeof(bool))]
    [SwaggerResponse(404, "No tag with this name exists in the catalog")]
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
    /// Deletes custom room tags from the portal catalog by name and detaches them from every room that carries them;
    /// the rooms themselves and their content are untouched, and only the tag disappears from their tag lists. Only a
    /// portal administrator may call it, and a room manager who is allowed to create tags is refused. The names are
    /// matched exactly as they are stored: names that are not in the catalog are skipped in silence and an empty list
    /// is accepted as a no-op, so a successful answer does not prove that anything was deleted; check a name with
    /// `GET api/2.0/files/tags/{tagName}/haslinks` first when that matters. The call cannot be undone: creating the
    /// name again with `POST api/2.0/files/tags` brings back the tag but not its links, which have to be attached to
    /// each room once more. The answer carries no body. To take a tag off one room and leave it in the catalog for
    /// the others, use `DELETE api/2.0/files/rooms/{id}/tags` instead.
    /// </remarks>
    /// <summary>Delete the custom room tags</summary>
    /// <path>api/2.0/files/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The tags were removed from the catalog and from every room")]
    [SwaggerResponse(403, "Only a portal administrator can delete tags")]
    [HttpDelete("tags")]
    public async Task DeleteCustomTags(BatchTagsRequestDto inDto)
    {
        await customTagsService.DeleteTagsAsync<int>(inDto.Names);
    }

    /// <remarks>
    /// Stores an image in temporary storage and answers with the path to it, which is the first half of setting a
    /// room logo. No room changes here: pass the returned path as `tmpFile` to `POST api/2.0/files/rooms/{id}/logo`,
    /// together with the crop rectangle, to make the image the logo of a room. The image travels as multipart form
    /// data, and the first file part of the request is the one that is used while any other part is ignored. It is
    /// re-encoded to PNG and scaled down to fit 1280 by 1280 pixels, so a larger picture is accepted and shrunk,
    /// while a part that is not a readable image, or one over the portal limit for uploaded images, is refused with
    /// 400. Only a room manager or a portal administrator may upload, and everyone else gets 403. Every call produces
    /// a new path, and an image that is never used stays in temporary storage until it is cleaned up, so uploading
    /// twice is harmless.
    /// </remarks>
    /// <summary>Upload a room logo image</summary>
    /// <path>api/2.0/files/logos</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The path of the stored temporary image", typeof(UploadResultDto))]
    [SwaggerResponse(400, "The request carries no image, or the image cannot be used as a logo")]
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
    /// Queues a background job that builds the index of a virtual data room as a spreadsheet, and answers with the
    /// job record to poll. The room has to be a virtual data room with indexing switched on, and the caller has to be
    /// its manager or a portal administrator; any other kind of room, a room template, and a member invited with a
    /// lower access level are refused, while an unknown room is answered as missing. There is one job per account:
    /// starting an export while an earlier one is still running answers with that earlier record instead of queuing a
    /// second job, and a finished record is replaced by the new one. Poll `GET api/2.0/files/rooms/indexexport` until
    /// `isCompleted` is true, then read `status` to tell a completed job from a failed or cancelled one, and take
    /// `resultFileId` and `resultFileUrl` from the same record. The report is saved as a spreadsheet in the My
    /// documents section of the caller, not in the room. Cancel a running job with
    /// `DELETE api/2.0/files/rooms/indexexport`.
    /// </remarks>
    /// <summary>Start the room index export</summary>
    /// <path>api/2.0/files/rooms/{id}/indexexport</path>
    /// <exception cref="NotSupportedException"></exception>
    [Tags("Rooms")]
    [SwaggerResponse(200, "The queued export job to poll", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(501, "Folder indexing is turned off")]
    [HttpPost("rooms/{id:int}/indexexport")]
    public async Task<DocumentBuilderTaskDto> StartRoomIndexExport(RoomIdRequestDto<int> inDto)
    {
        var room = await fileStorageService.GetFolderAsync(inDto.Id).NotFoundIfNull("Folder not found");

        if (room.RootId == await globalFolderHelper.FolderRoomTemplatesAsync)
        {
            throw new ItemNotFoundException();
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
    /// Returns the state of the index export of the calling account, the job started by
    /// `POST api/2.0/files/rooms/{id}/indexexport`. The record is not addressed by room: there is at most one per
    /// account, and the answer describes the latest export whichever room it was started for. When the account has
    /// never started one, or its record was cancelled, the body is null rather than an error, so null is the normal
    /// way of saying that there is nothing to report. While the job runs, `percentage` moves in coarse steps instead
    /// of smoothly, which makes it a rough hint rather than a measure of the remaining time; `isCompleted` is the
    /// field to wait on, and it is also set for a job that failed or was cancelled, so read `status` to tell the
    /// outcomes apart and `error` for the message. After a successful build, `resultFileId`, `resultFileName` and
    /// `resultFileUrl` point to the spreadsheet saved in the My documents section of the caller. The record survives
    /// completion and is replaced only by the next export.
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
    /// Cancels the room index export of the calling account and drops its job record. No room is named because there
    /// is at most one export per account, so the call always acts on the caller's own job and never on somebody
    /// else's: an account with nothing running gets a successful answer that changes nothing, which makes the call
    /// safe to repeat and makes it useless as a way of stopping an export somebody else started. Afterwards
    /// `GET api/2.0/files/rooms/indexexport` answers with an empty body until a new export is started with
    /// `POST api/2.0/files/rooms/{id}/indexexport`. The cancellation is asynchronous: the background job stops at its
    /// next checkpoint, so one that is already saving the file may still finish, and a report that was written before
    /// the cancellation stays in the My documents section of the caller and has to be deleted as an ordinary file.
    /// The answer carries no body and says nothing about whether an export was running.
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
    /// Collects everything that is marked as new for the caller across the active rooms into one answer, grouped
    /// first by the day an entry changed and then by the room it belongs to. An entry becomes new when somebody else
    /// creates or changes it in a room the caller has already opened, so the caller's own work never shows up here,
    /// and neither does anything from a room they have never visited. Only files are listed: a new subfolder is not
    /// an item, although files created inside it are, at any depth. The days come newest first, and inside a day the
    /// rooms and their files follow the same order by change time. The archive is out of scope, only rooms of the
    /// active section are covered. Reading the list clears nothing: the marks stay until the room itself is opened
    /// with `GET api/2.0/files/rooms/{id}`. An empty array means that this account has nothing new. For one room, use
    /// `GET api/2.0/files/rooms/{id}/news`.
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
