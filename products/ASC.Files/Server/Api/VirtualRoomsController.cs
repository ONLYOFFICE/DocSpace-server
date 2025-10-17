// (c) Copyright Ascensio System SIA 2009-2025
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
        daoFactory)
{
    /// <summary>
    /// Creates a room in the "Rooms" section.
    /// </summary>
    /// <short>Create a room</short>
    /// <path>api/2.0/files/rooms</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [HttpPost("")]
    public async Task<FolderDto<int>> CreateRoom(CreateRoomRequestDto inDto)
    {
        var lifetime = inDto.Lifetime.Map();
        if (lifetime != null)
        {
            lifetime.StartDate = DateTime.UtcNow;
        }

        var room = await _fileStorageService.CreateRoomAsync(inDto.Title, inDto.RoomType, inDto.Private, inDto.Indexing, inDto.Share, inDto.Quota, lifetime, inDto.DenyDownload, inDto.Watermark, inDto.Color, inDto.Cover, inDto.Tags, inDto.Logo);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Creates a room in the "Rooms" section based on the template.
    /// </summary>
    /// <short>Create a room from the template</short>
    /// <path>api/2.0/files/rooms/fromTemplate</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Status", typeof(RoomFromTemplateStatusDto))]
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

        var taskId = await roomTemplatesWorker.StartCreateRoomAsync(tenantManager.GetCurrentTenantId(), authContext.CurrentAccount.ID,
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

        await eventBus.PublishAsync(new CreateRoomFromTemplateIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId())
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

    /// <summary>
    /// Returns the progress of creating a room from the template.
    /// </summary>
    /// <short>Get the room creation progress</short>
    /// <path>api/2.0/files/rooms/fromTemplate/status</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Status", typeof(RoomFromTemplateStatusDto))]
    [HttpGet("fromTemplate/status")]
    public async Task<RoomFromTemplateStatusDto> GetRoomCreatingStatus()
    {
        try
        {
            var status = await roomTemplatesWorker.GetStatusRoomCreatingAsync(tenantManager.GetCurrentTenantId());
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
        daoFactory)
{
    /// <summary>
    /// Creates a room in the "Rooms" section stored in a third-party storage.
    /// </summary>
    /// <short>Create a third-party room</short>
    /// <path>api/2.0/files/rooms/thirdparty/{id}</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<string>))]
    [HttpPost("thirdparty/{id}")]
    public async Task<FolderDto<string>> CreateRoomThirdParty(CreateThirdPartyRoomRequestDto inDto)
    {
        var room = await _fileStorageService.CreateThirdPartyRoomAsync(inDto.Room.Title, inDto.Room.RoomType, inDto.Id, inDto.Room.Private, inDto.Room.Indexing, inDto.Room.CreateAsNewFolder, inDto.Room.DenyDownload, inDto.Room.Color, inDto.Room.Cover, inDto.Room.Tags, inDto.Room.Logo);

        return await _folderDtoHelper.GetAsync(room);
    }
}

[DefaultRoute("rooms")]
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
    IDaoFactory daoFactory)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    protected readonly FileStorageService _fileStorageService = fileStorageService;

    /// <summary>
    /// Returns the room information.
    /// </summary>
    /// <short>Get room information</short>
    /// <path>api/2.0/files/rooms/{id}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<FolderDto<T>> GetRoomInfo(RoomIdRequestDto<T> inDto)
    {
        var folder = await _fileStorageService.GetFolderAsync(inDto.Id).NotFoundIfNull("Folder not found");

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <summary>
    /// Updates a room with the ID specified in the request.
    /// </summary>
    /// <short>Update a room</short>
    /// <path>api/2.0/files/rooms/{id}</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Updated room information", typeof(FolderDto<int>))]
    [HttpPut("{id}")]
    public async Task<FolderDto<T>> UpdateRoom(UpdateRoomRequestDto<T> inDto)
    {
        var room = await _fileStorageService.UpdateRoomAsync(inDto.Id, inDto.UpdateRoom);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Changes the quota limit for the rooms with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Change the room quota limit
    /// </short>
    /// <path>api/2.0/files/rooms/roomquota</path>
    /// <collection>list</collection>
    [Tags("Files / Quota")]
    [SwaggerResponse(200, "List of rooms with the detailed information", typeof(IAsyncEnumerable<FolderDto<int>>))]
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

    /// <summary>
    /// Resets the quota limit for the rooms with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Reset the room quota limit
    /// </short>
    /// <path>api/2.0/files/rooms/resetquota</path>
    /// <collection>list</collection>
    [Tags("Files / Quota")]
    [SwaggerResponse(200, "List of rooms with the detailed information", typeof(IAsyncEnumerable<FolderDto<int>>))]
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


    /// <summary>
    /// Removes a room with the ID specified in the request.
    /// </summary>
    /// <short>Remove a room</short>
    /// <path>api/2.0/files/rooms/{id}</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "File operation", typeof(FileOperationDto))]
    [HttpDelete("{id}")]
    public async Task<FileOperationDto> DeleteRoom(DeleteRoomRequestDto<T> inDto)
    {
        await fileDeleteOperationsManager.Publish([inDto.Id], [], false, !inDto.DeleteRoom.DeleteAfter, true);

        return await fileOperationDtoHelper.GetAsync((await fileDeleteOperationsManager.GetOperationResults()).FirstOrDefault());
    }

    /// <summary>
    /// Moves a room with the ID specified in the request to the "Archive" section.
    /// </summary>
    /// <short>Archive a room</short>
    /// <path>api/2.0/files/rooms/{id}/archive</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "File operation", typeof(FileOperationDto))]
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

    /// <summary>
    /// Moves a room with the ID specified in the request from the "Archive" section to the "Rooms" section.
    /// </summary>
    /// <short>Unarchive a room</short>
    /// <path>api/2.0/files/rooms/{id}/unarchive</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "File operation", typeof(FileOperationDto))]
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

    /// <summary>
    /// Sets the access rights to the room with the ID specified in the request.
    /// </summary>
    /// <short>Set the room access rights</short>
    /// <path>api/2.0/files/rooms/{id}/share</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room security information", typeof(RoomSecurityDto))]
    [HttpPut("{id}/share")]
    [EnableRateLimiting(RateLimiterPolicy.EmailInvitationApi)]
    public async Task<RoomSecurityDto> SetRoomSecurity(RoomInvitationRequestDto<T> inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto);

        var result = new RoomSecurityDto();

        if (inDto.RoomInvitation.Invitations == null || !inDto.RoomInvitation.Invitations.Any())
        {
            return result;
        }

        var guestsInvited =
            inDto.RoomInvitation.Invitations.Any(i => !string.IsNullOrEmpty(i.Email) && i.Access != FileShare.None) ||
            await inDto.RoomInvitation.Invitations
                .Where(r => r.Id != Guid.Empty && r.Access != FileShare.None)
                .ToAsyncEnumerable()
                .AnyAwaitAsync(async i => await userManager.IsGuestAsync(i.Id));
        
        var usersInvited =
            inDto.RoomInvitation.Invitations.Any(i => !string.IsNullOrEmpty(i.Email) && i.Access != FileShare.None) ||
            await inDto.RoomInvitation.Invitations
                .Where(r => r.Id != Guid.Empty && r.Access != FileShare.None)
                .ToAsyncEnumerable()
                .AnyAwaitAsync(async i => await userManager.IsUserAsync(i.Id));
        
        if (guestsInvited)
        {
            var invitationSettings = await settingsManager.LoadAsync<TenantUserInvitationSettings>();
            if (!invitationSettings.AllowInvitingGuests)
            {
                throw new SecurityException(Resource.ErrorAccessDenied);
            }
        }

        var room = await _fileStorageService.GetFolderAsync(inDto.Id).NotFoundIfNull("Folder not found");

        if (room.RootId is int root && 
            root == await globalFolderHelper.FolderRoomTemplatesAsync && 
            (inDto.RoomInvitation.Invitations.Any(i => i.Access != FileShare.None && i.Access != FileShare.Read) || guestsInvited || usersInvited))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_RoleNotAvailable);
        }

        foreach (var invitation in inDto.RoomInvitation.Invitations)
        {
            if (invitation.Access == FileShare.None && !inDto.RoomInvitation.Force)
            {
                if (await _fileStorageService.ShouldPreventUserDeletion(room, invitation.Id))
                {
                    result.Error = RoomSecurityError.FormRoleBlockingDeletion;
                    return result;
                }
            }
        }

        var wrappers = inDto.RoomInvitation.Invitations.Map();

        var aceCollection = new AceCollection<T> { Files = [], Folders = [inDto.Id], Aces = wrappers, Message = inDto.RoomInvitation.Message };

        result.Warning = (await _fileStorageService.SetAceObjectAsync(aceCollection, inDto.RoomInvitation.Notify, inDto.RoomInvitation.Culture)).Select(r=> r.Warning).FirstOrDefault();
        result.Members = await _fileStorageService.GetRoomSharedInfoAsync(inDto.Id, inDto.RoomInvitation.Invitations.Select(s => s.Id))
            .SelectAwait(async a => await fileShareDtoHelper.Get(a))
            .ToListAsync();

        return result;
    }

    /// <summary>
    /// Returns the access rights of a room with the ID specified in the request.
    /// </summary>
    /// <short>Get the room access rights</short>
    /// <path>api/2.0/files/rooms/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Security information of room files", typeof(IAsyncEnumerable<FileShareDto>))]
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

    /// <summary>
    /// Sets the room external or invitation link with the ID specified in the request.
    /// </summary>
    /// <short>Set the room external or invitation link</short>
    /// <path>api/2.0/files/rooms/{id}/links</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room security information", typeof(FileShareDto))]
    [HttpPut("{id}/links")]
    public async Task<FileShareDto> SetRoomLink(RoomLinkRequestDto<T> inDto)
    {
        var linkAce = inDto.RoomLink.LinkType switch
        {
            LinkType.Invitation => await _fileStorageService.SetInvitationLinkAsync(inDto.Id, inDto.RoomLink.LinkId, inDto.RoomLink.Title, inDto.RoomLink.Access),
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

        if (inDto.RoomLink.LinkId != Guid.Empty && linkAce.Id != inDto.RoomLink.LinkId  && result.SharedLink != null)
        {
            result.SharedLink.RequestToken = null;
        }

        return result;
    }

    /// <summary>
    /// Returns the links of the room with the ID specified in the request.
    /// </summary>
    /// <short>Get the room links</short>
    /// <path>api/2.0/files/rooms/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room security information", typeof(IAsyncEnumerable<FileShareDto>))]
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

    /// <summary>
    /// Returns the primary external link of the room with the ID specified in the request.
    /// </summary>
    /// <short>Get the room primary external link</short>
    /// <path>api/2.0/files/rooms/{id}/link</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room security information", typeof(FileShareDto))]
    [SwaggerResponse(404, "Not Found")]
    [HttpGet("{id}/link")]
    public async Task<FileShareDto> GetRoomsPrimaryExternalLink(RoomIdRequestDto<T> inDto)
    {
        var linkAce = await _fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.Folder);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <summary>
    /// Adds the tags to a room with the ID specified in the request.
    /// </summary>
    /// <short>Add the room tags</short>
    /// <path>api/2.0/files/rooms/{id}/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "You don't have permission to edit the room")]
    [HttpPut("{id}/tags")]
    public async Task<FolderDto<T>> AddRoomTags(BatchTagsRequestDto<T> inDto)
    {
        var room = await customTagsService.AddRoomTagsAsync(inDto.Id, inDto.BatchTags.Names);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Removes the tags from a room with the ID specified in the request.
    /// </summary>
    /// <short>Remove the room tags</short>
    /// <path>api/2.0/files/rooms/{id}/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "You don't have permission to edit the room")]
    [HttpDelete("{id}/tags")]
    public async Task<FolderDto<T>> DeleteRoomTags(BatchTagsRequestDto<T> inDto)
    {
        var room = await customTagsService.DeleteRoomTagsAsync(inDto.Id, inDto.BatchTags.Names);

        return await _folderDtoHelper.GetAsync(room);
    }


    /// <summary>
    /// Creates a logo for a room with the ID specified in the request.
    /// </summary>
    /// <short>Create a room logo</short>
    /// <path>api/2.0/files/rooms/{id}/logo</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [SwaggerResponse(404, "The required room was not found")]
    [HttpPost("{id}/logo")]
    public async Task<FolderDto<T>> CreateRoomLogo(LogoRequest<T> inDto)
    {
        var room = await roomLogoManager.CreateAsync(inDto.Id, inDto.Logo.TmpFile, inDto.Logo.X, inDto.Logo.Y, inDto.Logo.Width, inDto.Logo.Height);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Changes a cover of a room with the ID specified in the request.
    /// </summary>
    /// <short>Change the room cover</short>
    /// <path>api/2.0/files/rooms/{id}/cover</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room cover", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "You don't have permission to change cover")]
    [SwaggerResponse(404, "The required room was not found")]
    [HttpPost("{id}/cover")]
    public async Task<FolderDto<T>> ChangeRoomCover(CoverRequestDto<T> inDto)
    {
        var room = await roomLogoManager.ChangeCoverAsync(inDto.Id, inDto.Cover.Color, inDto.Cover.Cover);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Returns a list of all covers.
    /// </summary>
    /// <short>Get covers</short>
    /// <path>api/2.0/files/rooms/covers</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Gets room cover", typeof(IAsyncEnumerable<CoversResultDto>))]
    [HttpGet("covers")]
    public async IAsyncEnumerable<CoversResultDto> GetRoomCovers()
    {
        foreach (var c in await RoomLogoManager.GetCoversAsync())
        {
            yield return new CoversResultDto { Id = c.Key, Data = c.Value };
        }
    }

    /// <summary>
    /// Removes a logo from a room with the ID specified in the request.
    /// </summary>
    /// <short>Remove a room logo</short>
    /// <path>api/2.0/files/rooms/{id}/logo</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [HttpDelete("{id}/logo")]
    public async Task<FolderDto<T>> DeleteRoomLogo(RoomIdRequestDto<T> inDto)
    {
        var room = await roomLogoManager.DeleteAsync(inDto.Id);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Pins a room with the ID specified in the request to the top of the list.
    /// </summary>
    /// <short>Pin a room</short>
    /// <path>api/2.0/files/rooms/{id}/pin</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [HttpPut("{id}/pin")]
    public async Task<FolderDto<T>> PinRoom(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.SetPinnedStatusAsync(inDto.Id, true);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Unpins a room with the ID specified in the request from the top of the list.
    /// </summary>
    /// <short>Unpin a room</short>
    /// <path>api/2.0/files/rooms/{id}/unpin</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [HttpPut("{id}/unpin")]
    public async Task<FolderDto<T>> UnpinRoom(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.SetPinnedStatusAsync(inDto.Id, false);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Resends the email invitations to a room with the ID specified in the request to the selected users.
    /// </summary>
    /// <short>Resend the room invitations</short>
    /// <path>api/2.0/files/rooms/{id}/resend</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Ok")]
    [HttpPost("{id}/resend")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task ResendEmailInvitations(UserInvitationRequestDto<T> inDto)
    {
        await _fileStorageService.ResendEmailInvitationsAsync(inDto.Id, inDto.UserInvitation.UsersIds, inDto.UserInvitation.ResendAll);
    }

    /// <summary>
    /// Reorders the room with ID specified in the request.
    /// </summary>
    /// <short>Reorder the room</short>
    /// <path>api/2.0/files/rooms/{id}/reorder</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [HttpPut("{id}/reorder")]
    public async Task<FolderDto<T>> ReorderRoom(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.ReOrderAsync(inDto.Id);
        await filesMessageService.SendAsync(MessageAction.FolderIndexReordered, room, room.Title);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Returns a list of all the new items from a room with the ID specified in the request.
    /// </summary>
    /// <short>Get the new room items</short>
    /// <path>api/2.0/files/rooms/{id}/news</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "List of file entry information", typeof(List<NewItemsDto<FileEntryBaseDto>>))]
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
    RoomNewItemsDtoHelper roomNewItemsDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Returns the contents of the "Rooms" section by the parameters specified in the request.
    /// </summary>
    /// <short>Get rooms</short>
    /// <path>api/2.0/files/rooms</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Returns the contents of the \"Rooms\" section", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the room content")]
    [HttpGet("rooms")]
    public async Task<FolderContentDto<int>> GetRoomsFolder(RoomContentRequestDto inDto)
    {
        var parentId = inDto.SearchArea switch
        {
            SearchArea.Archive => await globalFolderHelper.GetFolderArchive(),
            SearchArea.Templates => await globalFolderHelper.GetFolderRoomTemplatesAsync(),
            _ => await globalFolderHelper.GetFolderVirtualRooms()
        };

        var filter = RoomTypeExtensions.MapToFilterType(inDto.Type);

        var tagNames = !string.IsNullOrEmpty(inDto.Tags)
            ? JsonSerializer.Deserialize<IEnumerable<string>>(inDto.Tags)
            : null;

        OrderBy orderBy = null;
        if (SortedByTypeExtensions.TryParse(inDto.SortBy, true, out var sortBy))
        {
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
            inDto.SubjectFilter ?? SubjectFilter.Owner,
            quotaFilter: inDto.QuotaFilter ?? QuotaFilter.All,
            storageFilter: inDto.StorageFilter ?? StorageFilter.None);

        var dto = await folderContentDtoHelper.GetAsync(parentId, content, startIndex);

        return dto.NotFoundIfNull();
    }

    /// <summary>
    /// Creates a custom room tag with the parameters specified in the request.
    /// </summary>
    /// <short>Create a room tag</short>
    /// <path>api/2.0/files/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "New tag name", typeof(object))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpPost("tags")]
    public async Task<string> CreateRoomTag(CreateTagRequestDto inDto)
    {
        var createdTag = await customTagsService.CreateTagAsync(inDto.Name);
        return createdTag.Name;
    }

    /// <summary>
    /// Returns a list of custom room tags.
    /// </summary>
    /// <short>Get the room tags</short>
    /// <path>api/2.0/files/tags</path>
    /// <collection>list</collection>
    [Tags("Rooms")]
    [SwaggerResponse(200, "List of tag names", typeof(IAsyncEnumerable<object>))]
    [HttpGet("tags")]
    public IAsyncEnumerable<object> GetRoomTagsInfo(GetTagsInfoRequestDto inDto)
    {
        return customTagsService.GetTagsInfoAsync<int>(inDto.Text, TagType.Custom, inDto.StartIndex, inDto.Count);
    }

    /// <summary>
    /// Deletes a bunch of custom room tags specified in the request.
    /// </summary>
    /// <short>Delete the custom room tags</short>
    /// <path>api/2.0/files/tags</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpDelete("tags")]
    public async Task DeleteCustomTags(BatchTagsRequestDto inDto)
    {
        await customTagsService.DeleteTagsAsync<int>(inDto.Names);
    }

    /// <summary>
    /// Uploads a temporary image to create a room logo.
    /// </summary>
    /// <short>Upload a room logo image</short>
    /// <path>api/2.0/files/logos</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Upload result", typeof(UploadResultDto))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPost("logos")]
    public async Task<UploadResultDto> UploadRoomLogo(UploadRoomLogoRequestDto inDto)
    {
        var currentUserType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);

        if (currentUserType is not (EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        var result = new UploadResultDto();

        try
        {
            if (inDto.FormCollection.Files.Count != 0)
            {
                var roomLogo = inDto.FormCollection.Files[0];

                result.Data = await roomLogoManager.SaveTempAsync(roomLogo);
                result.Success = true;
            }
            else
            {
                result.Success = false;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// Starts the index export of a room with the ID specified in the request.
    /// </summary>
    /// <short>Start the room index export</short>
    /// <path>api/2.0/files/rooms/{id}/indexexport</path>
    /// <exception cref="NotSupportedException"></exception>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Ok", typeof(DocumentBuilderTaskDto))]
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

    /// <summary>
    /// Returns the room index export.
    /// </summary>
    /// <short>Get the room index export</short>
    /// <path>api/2.0/files/rooms/indexexport</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Ok", typeof(DocumentBuilderTaskDto))]
    [HttpGet("rooms/indexexport")]
    public async Task<DocumentBuilderTaskDto> GetRoomIndexExport()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        var task = await documentBuilderTaskManager.GetTask(tenantId, userId);

        return DocumentBuilderTaskDto.Get(task);
    }

    /// <summary>
    /// Terminates the room index export.
    /// </summary>
    /// <short>Terminate the room index export</short>
    /// <path>api/2.0/files/rooms/indexexport</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "Ok")]
    [HttpDelete("rooms/indexexport")]
    public async Task TerminateRoomIndexExport()
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var userId = authContext.CurrentAccount.ID;

        var evt = new RoomIndexExportIntegrationEvent(userId, tenantId, 0, null, true);

        await eventBus.PublishAsync(evt);
    }

    /// <summary>
    /// Returns the room new items.
    /// </summary>
    /// <short>Get the room new items</short>
    /// <path>api/2.0/files/rooms/news</path>
    [Tags("Rooms")]
    [SwaggerResponse(200, "List of new items", typeof(List<NewItemsDto<RoomNewItemsDto>>))]
    [HttpGet("rooms/news")]
    public async Task<List<NewItemsDto<RoomNewItemsDto>>> GetRoomsNewItems()
    {
        var newItems = await fileStorageService.GetNewRoomFilesAsync();
        var result = new List<NewItemsDto<RoomNewItemsDto>>();

        foreach (var (key, value) in newItems)
        {
            var date = apiDateTimeHelper.Get(key);
            var items = new List<RoomNewItemsDto>();

            foreach (var (k, v) in value)
            {
                var item = await roomNewItemsDtoHelper.GetAsync(k, v);
                items.Add(item);
            }

            result.Add(new NewItemsDto<RoomNewItemsDto> { Date = date, Items = items });
        }

        return result;
    }
}