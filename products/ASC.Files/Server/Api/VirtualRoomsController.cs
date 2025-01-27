// (c) Copyright Ascensio System SIA 2009-2024
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
    FileOperationsManager fileOperationsManager,
    FileStorageService fileStorageService,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileShareDtoHelper fileShareDtoHelper,
    IMapper mapper,
    SocketManager socketManager,
    ApiContext apiContext,
    FilesMessageService filesMessageService,
    SettingsManager settingsManager,
    ApiDateTimeHelper apiDateTimeHelper)
    : VirtualRoomsController<int>(globalFolderHelper,
        fileOperationDtoHelper,
        customTagsService,
        roomLogoManager,
        fileOperationsManager,
        fileStorageService,
        folderDtoHelper,
        fileDtoHelper,
        fileShareDtoHelper,
        mapper,
        socketManager,
        apiContext,
        filesMessageService,
        settingsManager,
        apiDateTimeHelper)
    {
    /// <summary>
    /// Creates a room in the "Rooms" section.
    /// </summary>
    /// <short>Create a room</short>
    /// <path>api/2.0/files/rooms</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [HttpPost("")]
    public async Task<FolderDto<int>> CreateRoomAsync(CreateRoomRequestDto inDto)
    {
        var lifetime = _mapper.Map<RoomDataLifetimeDto, RoomDataLifetime>(inDto.Lifetime);
        if (lifetime != null)
        { 
            lifetime.StartDate = DateTime.UtcNow;
        }

        var room = await _fileStorageService.CreateRoomAsync(inDto.Title, inDto.RoomType, inDto.Private, inDto.Indexing, inDto.Share, inDto.Quota, lifetime, inDto.DenyDownload, inDto.Watermark, inDto.Color, inDto.Cover, inDto.Tags, inDto.Logo);

        return await _folderDtoHelper.GetAsync(room);
    }
}

public class VirtualRoomsThirdPartyController(
    GlobalFolderHelper globalFolderHelper,
    FileOperationDtoHelper fileOperationDtoHelper,
    CustomTagsService customTagsService,
    RoomLogoManager roomLogoManager,
    FileOperationsManager fileOperationsManager,
    FileStorageService fileStorageService,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileShareDtoHelper fileShareDtoHelper,
    IMapper mapper,
    SocketManager socketManager,
    ApiContext apiContext,
    FilesMessageService filesMessageService,
    SettingsManager settingsManager,
    ApiDateTimeHelper apiDateTimeHelper)
    : VirtualRoomsController<string>(globalFolderHelper,
        fileOperationDtoHelper,
        customTagsService,
        roomLogoManager,
        fileOperationsManager,
        fileStorageService,
        folderDtoHelper,
        fileDtoHelper,
        fileShareDtoHelper,
        mapper,
        socketManager,
        apiContext,
        filesMessageService,
        settingsManager,
        apiDateTimeHelper)
    {
    /// <summary>
    /// Creates a room in the "Rooms" section stored in a third-party storage.
    /// </summary>
    /// <short>Create a third-party room</short>
    /// <path>api/2.0/files/rooms/thirdparty/{id}</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<string>))]
    [HttpPost("thirdparty/{id}")]
    public async Task<FolderDto<string>> CreateRoomThirdPartyAsync(CreateThirdPartyRoomRequestDto inDto)
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
    FileOperationsManager fileOperationsManager,
    FileStorageService fileStorageService,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileShareDtoHelper fileShareDtoHelper,
    IMapper mapper,
    SocketManager socketManager,
    ApiContext apiContext,
    FilesMessageService filesMessageService,
    SettingsManager settingsManager,
    ApiDateTimeHelper apiDateTimeHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
    {
    protected readonly FileStorageService _fileStorageService = fileStorageService;
    protected readonly FilesMessageService _filesMessageService = filesMessageService;
    protected readonly IMapper _mapper = mapper;

    /// <summary>
    /// Returns the room information.
    /// </summary>
    /// <short>Get room information</short>
    /// <path>api/2.0/files/rooms/{id}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<FolderDto<T>> GetRoomInfoAsync(RoomIdRequestDto<T> inDto)
    {
        var folder = await _fileStorageService.GetFolderAsync(inDto.Id).NotFoundIfNull("Folder not found");

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <summary>
    /// Renames a room with the ID specified in  the request.
    /// </summary>
    /// <short>Rename a room</short>
    /// <path>api/2.0/files/rooms/{id}</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Updated room information", typeof(FolderDto<int>))]
    [HttpPut("{id}")]
    public async Task<FolderDto<T>> UpdateRoomAsync(UpdateRoomRequestDto<T> inDto)
    {
        var room = await _fileStorageService.UpdateRoomAsync(inDto.Id, inDto.UpdateRoom);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Changes a quota limit for the rooms with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Change a room quota limit
    /// </short>
    /// <path>api/2.0/files/rooms/roomquota</path>
    /// <collection>list</collection>
    [Tags("Files / Quota")]
    [SwaggerResponse(200, "List of rooms with the detailed information", typeof(FolderDto<int>))]
    [HttpPut("roomquota")]
    public async IAsyncEnumerable<FolderDto<int>> UpdateRoomsQuotaAsync(UpdateRoomsQuotaRequestDto<T> inDto)
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
            _filesMessageService.Send(MessageAction.CustomQuotaPerRoomChanged, inDto.Quota.ToString(), folderTitles.ToArray());
        }
        else
        {
            _filesMessageService.Send(MessageAction.CustomQuotaPerRoomDisabled, string.Join(", ", folderTitles.ToArray()));
        }


    }

    /// <summary>
    /// Resets a quota limit for the rooms with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Reset a room quota limit
    /// </short>
    /// <path>api/2.0/files/rooms/resetquota</path>
    /// <collection>list</collection>
    [Tags("Files / Quota")]
    [SwaggerResponse(200, "List of rooms with the detailed information", typeof(FolderDto<int>))]
    [HttpPut("resetquota")]
    public async IAsyncEnumerable<FolderDto<int>> ResetRoomQuotaAsync(UpdateRoomsRoomIdsRequestDto<T> inDto)
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

        _filesMessageService.Send(MessageAction.CustomQuotaPerRoomDefault, quotaRoomSettings.DefaultQuota.ToString(), folderTitles.ToArray());
    }


    /// <summary>
    /// Removes a room with the ID specified in the request.
    /// </summary>
    /// <short>Remove a room</short>
    /// <path>api/2.0/files/rooms/{id}</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "File operation", typeof(FileOperationDto))]
    [HttpDelete("{id}")]
    public async Task<FileOperationDto> DeleteRoomAsync(DeleteRoomRequestDto<T> inDto)
    {
        await fileOperationsManager.PublishDelete(new List<T> { inDto.Id }, new List<T>(), false, !inDto.DeleteRoom.DeleteAfter, true);
        
        return await fileOperationDtoHelper.GetAsync((await fileOperationsManager.GetOperationResults()).FirstOrDefault());
    }

    /// <summary>
    /// Moves a room with the ID specified in the request to the "Archive" section.
    /// </summary>
    /// <short>Archive a room</short>
    /// <path>api/2.0/files/rooms/{id}/archive</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "File operation", typeof(FileOperationDto))]
    [HttpPut("{id}/archive")]
    public async Task<FileOperationDto> ArchiveRoomAsync(ArchiveRoomRequestDto<T> inDto)
    {
        var destFolder = JsonSerializer.SerializeToElement(await globalFolderHelper.FolderArchiveAsync);
        var movableRoom = JsonSerializer.SerializeToElement(inDto.Id);
        
        await fileOperationsManager.PublishMoveOrCopyAsync([movableRoom], [], destFolder, false, FileConflictResolveType.Skip, !inDto.ArchiveRoom.DeleteAfter);
        
        return await fileOperationDtoHelper.GetAsync((await fileOperationsManager.GetOperationResults()).FirstOrDefault());
    }

    /// <summary>
    /// Moves a room with the ID specified in the request from the "Archive" section to the "Rooms" section.
    /// </summary>
    /// <short>Unarchive a room</short>
    /// <path>api/2.0/files/rooms/{id}/unarchive</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "File operation", typeof(FileOperationDto))]
    [HttpPut("{id}/unarchive")]
    public async Task<FileOperationDto> UnarchiveRoomAsync(ArchiveRoomRequestDto<T> inDto)
    {
        var destFolder = JsonSerializer.SerializeToElement(await globalFolderHelper.FolderVirtualRoomsAsync);
        var movableRoom = JsonSerializer.SerializeToElement(inDto.Id);
        
        await fileOperationsManager.PublishMoveOrCopyAsync([movableRoom], [], destFolder, false, FileConflictResolveType.Skip, !inDto.ArchiveRoom.DeleteAfter);
        return await fileOperationDtoHelper.GetAsync((await fileOperationsManager.GetOperationResults()).FirstOrDefault());
    }

    /// <summary>
    /// Sets the access rights to a room with the ID specified in the request.
    /// </summary>
    /// <short>Set room access rights</short>
    /// <path>api/2.0/files/rooms/{id}/share</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room security information", typeof(RoomSecurityDto))]
    [HttpPut("{id}/share")]
    [EnableRateLimiting(RateLimiterPolicy.EmailInvitationApi)]
    public async Task<RoomSecurityDto> SetRoomSecurityAsync(RoomInvitationRequestDto<T> inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto);

        var result = new RoomSecurityDto();

        if (inDto.RoomInvitation.Invitations == null || !inDto.RoomInvitation.Invitations.Any())
        {
            return result;
        }

        var wrappers = _mapper.Map<IEnumerable<RoomInvitation>, List<AceWrapper>>(inDto.RoomInvitation.Invitations);

        var aceCollection = new AceCollection<T>
        {
            Files = Array.Empty<T>(),
            Folders = [inDto.Id],
            Aces = wrappers,
            Message = inDto.RoomInvitation.Message
        };

        result.Warning = await _fileStorageService.SetAceObjectAsync(aceCollection, inDto.RoomInvitation.Notify, inDto.RoomInvitation.Culture);
        result.Members = await _fileStorageService.GetRoomSharedInfoAsync(inDto.Id, inDto.RoomInvitation.Invitations.Select(s => s.Id))
            .SelectAwait(async a => await fileShareDtoHelper.Get(a))
            .ToListAsync();

        return result;
    }

    /// <summary>
    /// Returns the access rights of a room with the ID specified in the request.
    /// </summary>
    /// <short>Get room access rights</short>
    /// <path>api/2.0/files/rooms/{id}/share</path>
    /// <collection>list</collection>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Security information of room files", typeof(FileShareDto))]
    [HttpGet("{id}/share")]
    public async IAsyncEnumerable<FileShareDto> GetRoomSecurityInfoAsync(RoomSecurityInfoRequestDto<T> inDto)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);
        var text = apiContext.FilterValue;

        var totalCountTask = await _fileStorageService.GetPureSharesCountAsync(inDto.Id, FileEntryType.Folder, inDto.FilterType, text);
        apiContext.SetCount(Math.Min(totalCountTask - offset, count)).SetTotalCount(totalCountTask);

        await foreach (var ace in _fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.Folder, inDto.FilterType, text, offset, count))
        {
            yield return await fileShareDtoHelper.Get(ace);
        }
    }

    /// <summary>
    /// Sets an external or invitation link with the ID specified in the request.
    /// </summary>
    /// <short>Set an external or invitation link</short>
    /// <path>api/2.0/files/rooms/{id}/links</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room security information", typeof(FileShareDto))]
    [HttpPut("{id}/links")]
    public async Task<FileShareDto> SetLinkAsync(RoomLinkRequestDto<T> inDto)
    {
        var linkAce = inDto.RoomLink.LinkType switch
        {
            LinkType.Invitation => await _fileStorageService.SetInvitationLinkAsync(inDto.Id, inDto.RoomLink.LinkId, inDto.RoomLink.Title, inDto.RoomLink.Access),
            LinkType.External => await _fileStorageService.SetExternalLinkAsync(inDto.Id, FileEntryType.Folder, inDto.RoomLink.LinkId, inDto.RoomLink.Title,
                inDto.RoomLink.Access, inDto.RoomLink.ExpirationDate, inDto.RoomLink.Password?.Trim(), inDto.RoomLink.DenyDownload),
            _ => throw new InvalidOperationException()
        };

        return linkAce is not null ? await fileShareDtoHelper.Get(linkAce) : null;
    }

    /// <summary>
    /// Returns the links of a room with the ID specified in the request.
    /// </summary>
    /// <short>Get room links</short>
    /// <path>api/2.0/files/rooms/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room security information", typeof(FileShareDto))]
    [HttpGet("{id}/links")]
    public async IAsyncEnumerable<FileShareDto> GetRoomLinksAsync(GetRoomLinksRequestDto<T> inDto)
    {
        var filterType = inDto.Type.HasValue ? inDto.Type.Value switch
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
    /// Returns the primary external link of a room with the ID specified in the request.
    /// </summary>
    /// <short>Get primary external link</short>
    /// <path>api/2.0/files/rooms/{id}/link</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room security information", typeof(FileShareDto))]
    [SwaggerResponse(404, "Not Found")]
    [HttpGet("{id}/link")]
    public async Task<FileShareDto> GetRoomsPrimaryExternalLinkAsync(RoomIdRequestDto<T> inDto)
    {
        var linkAce = await _fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.Folder);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <summary>
    /// Adds the tags to a room with the ID specified in the request.
    /// </summary>
    /// <short>Add room tags</short>
    /// <path>api/2.0/files/rooms/{id}/tags</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "You don't have permission to edit the room")]
    [HttpPut("{id}/tags")]
    public async Task<FolderDto<T>> AddTagsAsync(BatchTagsRequestDto<T> inDto)
    {
        var room = await customTagsService.AddRoomTagsAsync(inDto.Id, inDto.BatchTags.Names);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Removes the tags from a room with the ID specified in the request.
    /// </summary>
    /// <short>Remove room tags</short>
    /// <path>api/2.0/files/rooms/{id}/tags</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "You don't have permission to edit the room")]
    [HttpDelete("{id}/tags")]
    public async Task<FolderDto<T>> DeleteTagsAsync(BatchTagsRequestDto<T> inDto)
    {
        var room = await customTagsService.DeleteRoomTagsAsync(inDto.Id, inDto.BatchTags.Names);

        return await _folderDtoHelper.GetAsync(room);
    }

    
    /// <summary>
    /// Creates a logo for a room with the ID specified in the request.
    /// </summary>
    /// <short>Create a room logo</short>
    /// <path>api/2.0/files/rooms/{id}/logo</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [SwaggerResponse(404, "The required room was not found")]
    [HttpPost("{id}/logo")]
    public async Task<FolderDto<T>> CreateRoomLogoAsync(LogoRequest<T> inDto)
    {
        var room = await roomLogoManager.CreateAsync(inDto.Id, inDto.Logo.TmpFile, inDto.Logo.X, inDto.Logo.Y, inDto.Logo.Width, inDto.Logo.Height);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Changes room cover
    /// </summary>
    /// <path>api/2.0/files/rooms/{id}/cover</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room cover", typeof(FolderDto<int>))]
    [SwaggerResponse(403, "You don't have permission to change cover")]
    [SwaggerResponse(404, "The required room was not found")]
    [HttpPost("{id}/cover")]
    public async Task<FolderDto<T>> ChangeRoomCoverAsync(CoverRequestDto<T> inDto)
    {
        var room = await roomLogoManager.ChangeCoverAsync(inDto.Id, inDto.Cover.Color, inDto.Cover.Cover);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Gets covers
    /// </summary>
    /// <path>api/2.0/files/rooms/covers</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Gets room cover", typeof(IAsyncEnumerable<CoversResultDto>))]
    [HttpGet("covers")]
    public async IAsyncEnumerable<CoversResultDto> GetCovers()
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
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [HttpDelete("{id}/logo")]
    public async Task<FolderDto<T>> DeleteRoomLogoAsync(RoomIdRequestDto<T> inDto)
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
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [HttpPut("{id}/pin")]
    public async Task<FolderDto<T>> PinRoomAsync(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.SetPinnedStatusAsync(inDto.Id, true);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Unpins a room with the ID specified in the request from the top of the list.
    /// </summary>
    /// <short>Unpin a room</short>
    /// <path>api/2.0/files/rooms/{id}/unpin</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [HttpPut("{id}/unpin")]
    public async Task<FolderDto<T>> UnpinRoomAsync(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.SetPinnedStatusAsync(inDto.Id, false);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Resends the email invitations to a room with the ID specified in the request to the selected users.
    /// </summary>
    /// <short>Resend room invitations</short>\
    /// <path>api/2.0/files/rooms/{id}/resend</path>
    [Tags("Files / Rooms")]
    [HttpPost("{id}/resend")]
    [EnableRateLimiting(RateLimiterPolicy.SensitiveApi)]
    public async Task ResendEmailInvitationsAsync(UserInvitationRequestDto<T> inDto)
    {
        await _fileStorageService.ResendEmailInvitationsAsync(inDto.Id, inDto.UserInvitation.UsersIds, inDto.UserInvitation.ResendAll);
    }

    /// <summary>
    /// Reorders to a room with ID specified in the request
    /// </summary>
    /// <path>api/2.0/files/rooms/{id}/reorder</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Room information", typeof(FolderDto<int>))]
    [HttpPut("{id}/reorder")]
    public async Task<FolderDto<T>> ReorderAsync(RoomIdRequestDto<T> inDto)
    {
        var room = await _fileStorageService.ReOrderAsync(inDto.Id);
        await _filesMessageService.SendAsync(MessageAction.FolderIndexReordered, room, room.Title);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Returns a list of all the new items from a room with the ID specified in the request.
    /// </summary>
    /// <short>Get new room items</short>
    /// <path>api/2.0/files/rooms/{id}/news</path>
    /// <collection>list</collection>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "List of file entry information", typeof(IAsyncEnumerable<NewItemsDto<FileEntryDto>>))]
    [HttpGet("{id}/news")]
    public async Task<List<NewItemsDto<FileEntryDto>>> GetNewItemsFromRoomAsync(RoomIdRequestDto<T> inDto)
    {
        var newItems = await _fileStorageService.GetNewRoomFilesAsync(inDto.Id);
        var result = new List<NewItemsDto<FileEntryDto>>();
        
        foreach (var (date, entries) in newItems)
        {
            var apiDateTime = apiDateTimeHelper.Get(date);
            var items = new List<FileEntryDto>();

            foreach (var en in entries)
            {
                items.Add(await GetFileEntryWrapperAsync(en));
            }
            
            result.Add(new NewItemsDto<FileEntryDto> { Date = apiDateTime, Items = items });
        }

        return result;
    }
}

public class VirtualRoomsCommonController(FileStorageService fileStorageService,
        FolderContentDtoHelper folderContentDtoHelper,
        GlobalFolderHelper globalFolderHelper,
        ApiContext apiContext,
        CustomTagsService customTagsService,
        RoomLogoManager roomLogoManager,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        AuthContext authContext,
        DocumentBuilderTaskManager documentBuilderTaskManager,
        TenantManager tenantManager,
        IEventBus eventBus,
        UserManager userManager,
        IServiceProvider serviceProvider,
        ApiDateTimeHelper apiDateTimeHelper,
        RoomNewItemsDtoHelper roomNewItemsDtoHelper,
        IHttpContextAccessor httpContextAccessor)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Returns the contents of the "Rooms" section by the parameters specified in the request.
    /// </summary>
    /// <short>Get rooms</short>
    /// <path>api/2.0/files/rooms</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Returns the contents of the \"Rooms\" section", typeof(FolderContentDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to view the room content")]
    [HttpGet("rooms")]
    public async Task<FolderContentDto<int>> GetRoomsFolderAsync(RoomContentRequestDto inDto)
    {
        var parentId = inDto.SearchArea != SearchArea.Archive 
            ? await globalFolderHelper.GetFolderVirtualRooms()
            : await globalFolderHelper.GetFolderArchive();

        var filter = RoomTypeExtensions.MapToFilterType(inDto.Type);

        var tagNames = !string.IsNullOrEmpty(inDto.Tags) 
            ? JsonSerializer.Deserialize<IEnumerable<string>>(inDto.Tags) 
            : null;

        OrderBy orderBy = null;
        if (SortedByTypeExtensions.TryParse(apiContext.SortBy, true, out var sortBy))
        {
            orderBy = new OrderBy(sortBy, !apiContext.SortDescending);
        }

        var startIndex = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);
        var filterValue = apiContext.FilterValue;

        var content = await fileStorageService.GetFolderItemsAsync(
            parentId,
            startIndex,
            count,
            filter,
            false,
            inDto.SubjectId,
            filterValue,
            [],
            inDto.SearchInContent ?? false,
            inDto.WithSubfolders ?? false,
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
    /// Creates a custom tag with the parameters specified in the request.
    /// </summary>
    /// <short>Create a tag</short>
    /// <path>api/2.0/files/tags</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "New tag name", typeof(object))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpPost("tags")]
    public async Task<string> CreateTagAsync(CreateTagRequestDto inDto)
    {
        var createdTag = await customTagsService.CreateTagAsync(inDto.Name);
        return createdTag.Name;
    }

    /// <summary>
    /// Returns a list of custom tags.
    /// </summary>
    /// <short>Get tags</short>
    /// <path>api/2.0/files/tags</path>
    /// <collection>list</collection>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "List of tag names", typeof(object))]
    [HttpGet("tags")]
    public async IAsyncEnumerable<object> GetTagsInfoAsync()
    {
        var from = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);

        await foreach (var tag in customTagsService.GetTagsInfoAsync<int>(apiContext.FilterValue, TagType.Custom, from, count))
        {
            yield return tag;
        }
    }

    /// <summary>
    /// Deletes a bunch of custom tags specified in the request.
    /// </summary>
    /// <short>Delete tags</short>
    /// <path>api/2.0/files/tags</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [HttpDelete("tags")]
    public async Task DeleteCustomTagsAsync(BatchTagsRequestDto inDto)
    {
        await customTagsService.DeleteTagsAsync<int>(inDto.Names);
    }

    /// <summary>
    /// Uploads a temporary image to create a room logo.
    /// </summary>
    /// <short>Upload an image for room logo</short>
    /// <path>api/2.0/files/logos</path>
    [Tags("Files / Rooms")]
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
    /// Starts room index export
    /// </summary>
    /// <path>api/2.0/files/rooms/{id:int}/indexexport</path>
    /// <exception cref="NotSupportedException"></exception>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "Ok", typeof(DocumentBuilderTaskDto))]
    [SwaggerResponse(501, "Folder indexing is turned off")]
    [HttpPost("rooms/{id:int}/indexexport")]
    public async Task<DocumentBuilderTaskDto> StartRoomIndexExportAsync(RoomIdRequestDto<int> inDto)
    {
        var room = await fileStorageService.GetFolderAsync(inDto.Id).NotFoundIfNull("Folder not found");

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
        
        var headers = MessageSettings.GetHttpHeaders(httpContextAccessor?.HttpContext?.Request);
        var evt = new RoomIndexExportIntegrationEvent(userId, tenantId, inDto.Id, baseUri, headers: headers != null 
            ? headers.ToDictionary(x => x.Key, x => x.Value.ToString())
            : []);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    /// <summary>
    /// Gets room index export
    /// </summary>
    /// <path>api/2.0/files/rooms/indexexport</path>
    [Tags("Files / Rooms")]
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
    /// Terminates room index export
    /// </summary>
    /// <path>api/2.0/files/rooms/indexexport</path>
    [Tags("Files / Rooms")]
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
    /// Gets room new items
    /// </summary>
    /// <path>api/2.0/files/rooms/news</path>
    [Tags("Files / Rooms")]
    [SwaggerResponse(200, "List of new items", typeof(IAsyncEnumerable<NewItemsDto<RoomNewItemsDto>>))]
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