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
public class VirtualRoomsInternalController(GlobalFolderHelper globalFolderHelper,
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
        SettingsManager settingsManager)
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
    settingsManager)
{
    /// <summary>
    /// Creates a room in the "Rooms" section.
    /// </summary>
    /// <short>Create a room</short>
    /// <category>Rooms</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateRoomRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating a room</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Room information</returns>
    /// <path>api/2.0/files/rooms</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("")]
    public async Task<FolderDto<int>> CreateRoomAsync(CreateRoomRequestDto inDto)
    {
        var room = await _fileStorageService.CreateRoomAsync(inDto.Title, inDto.RoomType, inDto.Private, inDto.Indexing, inDto.Share, inDto.Quota, inDto.Color, inDto.Cover);

        return await _folderDtoHelper.GetAsync(room);
    }
}

public class VirtualRoomsThirdPartyController(GlobalFolderHelper globalFolderHelper,
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
        SettingsManager settingsManager)
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
    settingsManager)
{
    /// <summary>
    /// Creates a room in the "Rooms" section stored in a third-party storage.
    /// </summary>
    /// <short>Create a third-party room</short>
    /// <category>Rooms</category>
    /// <param type="System.String, System" method="url" name="id">ID of the folder in the third-party storage in which the contents of the room will be stored</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateRoomRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating a room</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Room information</returns>
    /// <path>api/2.0/files/rooms/thirdparty/{id}</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("thirdparty/{id}")]
    public async Task<FolderDto<string>> CreateRoomAsync(string id, CreateThirdPartyRoomRequestDto inDto)
    {
        var room = await _fileStorageService.CreateThirdPartyRoomAsync(inDto.Title, inDto.RoomType, id, inDto.Private, inDto.Indexing, inDto.CreateAsNewFolder, inDto.Color, inDto.Cover);

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
    SettingsManager settingsManager)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    protected readonly FileStorageService _fileStorageService = fileStorageService;
    protected readonly FilesMessageService _filesMessageService = filesMessageService;

    /// <summary>
    /// Returns the room information.
    /// </summary>
    /// <short>Get room information</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Room information</returns>
    /// <path>api/2.0/files/rooms/{id}</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<FolderDto<T>> GetRoomInfoAsync(T id)
    {
        var folder = await _fileStorageService.GetFolderAsync(id).NotFoundIfNull("Folder not found");

        return await _folderDtoHelper.GetAsync(folder);
    }

    /// <summary>
    /// Renames a room with the ID specified in  the request.
    /// </summary>
    /// <short>Rename a room</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.UpdateRoomRequestDto, ASC.Files.Core" name="inDto">Request parameters for updating a room</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Updated room information</returns>
    /// <path>api/2.0/files/rooms/{id}</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id}")]
    public async Task<FolderDto<T>> UpdateRoomAsync(T id, UpdateRoomRequestDto inDto)
    {
        var room = await _fileStorageService.UpdateRoomAsync(id, inDto);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Changes a quota limit for the rooms with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Change a room quota limit
    /// </short>
    /// <category>Quota</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.UpdateRoomsQuotaRequestDto, ASC.Files.Core" name="inDto">Request parameters for updating room</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">List of rooms with the detailed information</returns>
    /// <path>api/2.0/files/rooms/roomquota</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
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
            await _filesMessageService.SendAsync(MessageAction.CustomQuotaPerRoomChanged, inDto.Quota.ToString(), folderTitles.ToArray());
    }
        else
        {
            await _filesMessageService.SendAsync(MessageAction.CustomQuotaPerRoomDisabled, string.Join(", ", folderTitles.ToArray()));
        }


    }

    /// <summary>
    /// Resets a quota limit for the rooms with the IDs specified in the request.
    /// </summary>
    /// <short>
    /// Reset a room quota limit
    /// </short>
    /// <category>Quota</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.UpdateRoomsQuotaRequestDto, ASC.Files.Core" name="inDto">Request parameters for updating room</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">List of rooms with the detailed information</returns>
    /// <path>api/2.0/files/rooms/resetquota</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [HttpPut("resetquota")]
    public async IAsyncEnumerable<FolderDto<int>> ResetRoomQuotaAsync(UpdateRoomsQuotaRequestDto<T> inDto)
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

        await _filesMessageService.SendAsync(MessageAction.CustomQuotaPerRoomDefault, quotaRoomSettings.DefaultQuota.ToString(), folderTitles.ToArray());
    }
    

    /// <summary>
    /// Removes a room with the ID specified in the request.
    /// </summary>
    /// <short>Remove a room</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DeleteRoomRequestDto, ASC.Files.Core" name="inDto">Request parameters for deleting a room</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">File operation</returns>
    /// <path>api/2.0/files/rooms/{id}</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("{id}")]
    public async Task<FileOperationDto> DeleteRoomAsync(T id, DeleteRoomRequestDto inDto)
    {
        await fileOperationsManager.PublishDelete(new List<T> { id }, new List<T>(), false, !inDto.DeleteAfter, true);
        
        return await fileOperationDtoHelper.GetAsync((await fileOperationsManager.GetOperationResults()).FirstOrDefault());
    }

    /// <summary>
    /// Moves a room with the ID specified in the request to the "Archive" section.
    /// </summary>
    /// <short>Archive a room</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.ArchiveRoomRequestDto, ASC.Files.Core" name="inDto">Request parameters for archiving a room</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">File operation</returns>
    /// <path>api/2.0/files/rooms/{id}/archive</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id}/archive")]
    public async Task<FileOperationDto> ArchiveRoomAsync(T id, ArchiveRoomRequestDto inDto)
    {
        var destFolder = JsonSerializer.SerializeToElement(await globalFolderHelper.FolderArchiveAsync);
        var movableRoom = JsonSerializer.SerializeToElement(id);
        
        await fileOperationsManager.PublishMoveOrCopyAsync([movableRoom], [], destFolder, false, FileConflictResolveType.Skip, !inDto.DeleteAfter);
        
        return await fileOperationDtoHelper.GetAsync((await fileOperationsManager.GetOperationResults()).FirstOrDefault());
    }

    /// <summary>
    /// Moves a room with the ID specified in the request from the "Archive" section to the "Rooms" section.
    /// </summary>
    /// <short>Unarchive a room</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.ArchiveRoomRequestDto, ASC.Files.Core" name="inDto">Request parameters for unarchiving a room</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">File operation</returns>
    /// <path>api/2.0/files/rooms/{id}/unarchive</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id}/unarchive")]
    public async Task<FileOperationDto> UnarchiveRoomAsync(T id, ArchiveRoomRequestDto inDto)
    {
        var destFolder = JsonSerializer.SerializeToElement(await globalFolderHelper.FolderVirtualRoomsAsync);
        var movableRoom = JsonSerializer.SerializeToElement(id);
        
        await fileOperationsManager.PublishMoveOrCopyAsync([movableRoom], [], destFolder, false, FileConflictResolveType.Skip, !inDto.DeleteAfter);
        return await fileOperationDtoHelper.GetAsync((await fileOperationsManager.GetOperationResults()).FirstOrDefault());
    }

    /// <summary>
    /// Sets the access rights to a room with the ID specified in the request.
    /// </summary>
    /// <short>Set room access rights</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.RoomInvitationRequestDto, ASC.Files.Core" name="inDto">Request parameters for inviting users to a room</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.RoomSecurityDto, ASC.Files.Core">Room security information</returns>
    /// <path>api/2.0/files/rooms/{id}/share</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id}/share")]
    [EnableRateLimiting(RateLimiterPolicy.EmailInvitationApi)]
    public async Task<RoomSecurityDto> SetRoomSecurityAsync(T id, RoomInvitationRequestDto inDto)
    {
        ArgumentNullException.ThrowIfNull(inDto);

        var result = new RoomSecurityDto();

        if (inDto.Invitations == null || !inDto.Invitations.Any())
        {
            return result;
        }

        var wrappers = mapper.Map<IEnumerable<RoomInvitation>, List<AceWrapper>>(inDto.Invitations);

        var aceCollection = new AceCollection<T>
        {
            Files = Array.Empty<T>(),
            Folders = new[] { id },
            Aces = wrappers,
            Message = inDto.Message
        };

        result.Warning = await _fileStorageService.SetAceObjectAsync(aceCollection, inDto.Notify, inDto.Culture);
        result.Members = await _fileStorageService.GetRoomSharedInfoAsync(id, inDto.Invitations.Select(s => s.Id))
            .SelectAwait(async a => await fileShareDtoHelper.Get(a))
            .ToListAsync();

        return result;
    }

    /// <summary>
    /// Returns the access rights of a room with the ID specified in the request.
    /// </summary>
    /// <short>Get room access rights</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.Security.ShareFilterType, ASC.Files.Core" name="filterType">Share type filter</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">Security information of room files</returns>
    /// <path>api/2.0/files/rooms/{id}/share</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("{id}/share")]
    public async IAsyncEnumerable<FileShareDto> GetRoomSecurityInfoAsync(T id, ShareFilterType filterType = ShareFilterType.UserOrGroup)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);
        var text = apiContext.FilterValue;

        var totalCountTask = await _fileStorageService.GetPureSharesCountAsync(id, FileEntryType.Folder, filterType, text);
        apiContext.SetCount(Math.Min(totalCountTask - offset, count)).SetTotalCount(totalCountTask);

        await foreach (var ace in _fileStorageService.GetPureSharesAsync(id, FileEntryType.Folder, filterType, text, offset, count))
        {
            yield return await fileShareDtoHelper.Get(ace);
        }
    }

    /// <summary>
    /// Sets an external or invitation link with the ID specified in the request.
    /// </summary>
    /// <short>Set an external or invitation link</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.LinkRequestDto, ASC.Files.Core" name="inDto">Link request parameters</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">Room security information</returns>
    /// <path>api/2.0/files/rooms/{id}/links</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id}/links")]
    public async Task<FileShareDto> SetLinkAsync(T id, RoomLinkRequestDto inDto)
    {
        var linkAce = inDto.LinkType switch
        {
            LinkType.Invitation => await _fileStorageService.SetInvitationLinkAsync(id, inDto.LinkId, inDto.Title, inDto.Access),
            LinkType.External => await _fileStorageService.SetExternalLinkAsync(id, FileEntryType.Folder, inDto.LinkId, inDto.Title, inDto.Access , inDto.ExpirationDate, 
                inDto.Password?.Trim(), inDto.DenyDownload),
            _ => throw new InvalidOperationException()
        };

        return linkAce is not null ? await fileShareDtoHelper.Get(linkAce) : null;
    }

    /// <summary>
    /// Returns the links of a room with the ID specified in the request.
    /// </summary>
    /// <short>Get room links</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="System.Nullable{ASC.Files.Core.ApiModels.ResponseDto.LinkType}, System" name="type">Link type</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">Room security information</returns>
    /// <path>api/2.0/files/rooms/{id}/links</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [HttpGet("{id}/links")]
    public async IAsyncEnumerable<FileShareDto> GetLinksAsync(T id, LinkType? type)
    {
        var filterType = type.HasValue ? type.Value switch
        {
            LinkType.Invitation => ShareFilterType.InvitationLink,
            LinkType.External => ShareFilterType.ExternalLink,
            _ => ShareFilterType.Link
        }
            : ShareFilterType.Link;

        var counter = 0;

        await foreach (var ace in _fileStorageService.GetPureSharesAsync(id, FileEntryType.Folder, filterType, null, 0, 100))
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
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">Room security information</returns>
    /// <path>api/2.0/files/rooms/{id}/link</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("{id}/link")]
    public async Task<FileShareDto> GetPrimaryExternalLinkAsync(T id)
    {
        var linkAce = await _fileStorageService.GetPrimaryExternalLinkAsync(id, FileEntryType.Folder);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <summary>
    /// Adds the tags to a room with the ID specified in the request.
    /// </summary>
    /// <short>Add room tags</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BatchTagsRequestDto, ASC.Files.Core" name="inDto">Request parameters for adding tags</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Room information</returns>
    /// <path>api/2.0/files/rooms/{id}/tags</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id}/tags")]
    public async Task<FolderDto<T>> AddTagsAsync(T id, BatchTagsRequestDto inDto)
    {
        var room = await customTagsService.AddRoomTagsAsync(id, inDto.Names);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Removes the tags from a room with the ID specified in the request.
    /// </summary>
    /// <short>Remove room tags</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BatchTagsRequestDto, ASC.Files.Core" name="inDto">Request parameters for removing tags</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Room information</returns>
    /// <path>api/2.0/files/rooms/{id}/tags</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("{id}/tags")]
    public async Task<FolderDto<T>> DeleteTagsAsync(T id, BatchTagsRequestDto inDto)
    {
        var room = await customTagsService.DeleteRoomTagsAsync(id, inDto.Names);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Creates a logo for a room with the ID specified in the request.
    /// </summary>
    /// <short>Create a room logo</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.LogoRequestDto, ASC.Files.Core" name="inDto">Logo request parameters</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Room information</returns>
    /// <path>api/2.0/files/rooms/{id}/logo</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("{id}/logo")]
    public async Task<FolderDto<T>> CreateRoomLogoAsync(T id, LogoRequestDto inDto)
    {
        var room = await roomLogoManager.CreateAsync(id, inDto.TmpFile, inDto.X, inDto.Y, inDto.Width, inDto.Height);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }
    
    [HttpPost("{id}/cover")]
    public async Task<FolderDto<T>> ChangeRoomCoverAsync(T id, CoverRequestDto inDto)
    {
        var room = await roomLogoManager.ChangeCoverAsync(id, inDto.Color, inDto.Cover);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }
    
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
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Room information</returns>
    /// <path>api/2.0/files/rooms/{id}/logo</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("{id}/logo")]
    public async Task<FolderDto<T>> DeleteRoomLogoAsync(T id)
    {
        var room = await roomLogoManager.DeleteAsync(id);

        await socketManager.UpdateFolderAsync(room);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Pins a room with the ID specified in the request to the top of the list.
    /// </summary>
    /// <short>Pin a room</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Room information</returns>
    /// <path>api/2.0/files/rooms/{id}/pin</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id}/pin")]
    public async Task<FolderDto<T>> PinRoomAsync(T id)
    {
        var room = await _fileStorageService.SetPinnedStatusAsync(id, true);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Unpins a room with the ID specified in the request from the top of the list.
    /// </summary>
    /// <short>Unpin a room</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderDto, ASC.Files.Core">Room information</returns>
    /// <path>api/2.0/files/rooms/{id}/unpin</path>
    /// <httpMethod>PUT</httpMethod>
    [HttpPut("{id}/unpin")]
    public async Task<FolderDto<T>> UnpinRoomAsync(T id)
    {
        var room = await _fileStorageService.SetPinnedStatusAsync(id, false);

        return await _folderDtoHelper.GetAsync(room);
    }

    /// <summary>
    /// Resends the email invitations to a room with the ID specified in the request to the selected users.
    /// </summary>
    /// <short>Resend room invitations</short>
    /// <category>Rooms</category>
    /// <param type="System.Int32, System" method="url" name="id">Room ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.UserInvintationRequestDto, ASC.Files.Core" name="inDto">User invitation request parameters</param>
    /// <returns></returns>
    /// <path>api/2.0/files/rooms/{id}/resend</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("{id}/resend")]
    public async Task ResendEmailInvitationsAsync(T id, UserInvitationRequestDto inDto)
    {
        await _fileStorageService.ResendEmailInvitationsAsync(id, inDto.UsersIds, inDto.ResendAll);
    }

    [HttpPut("{id}/settings")]
    public async Task<FolderDto<T>> UpdateSettingsAsync(T id, SettingsRoomRequestDto inDto)
    {
        var room = await _fileStorageService.SetRoomSettingsAsync(id, inDto.Indexing);

        return await _folderDtoHelper.GetAsync(room);
    }
    
    [HttpPut("{id}/reorder")]
    public async Task<FolderDto<T>> ReorderAsync(T id)
    {
        var room = await _fileStorageService.ReOrder(id);

        return await _folderDtoHelper.GetAsync(room);
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
        IServiceProvider serviceProvider)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Returns the contents of the "Rooms" section by the parameters specified in the request.
    /// </summary>
    /// <short>Get rooms</short>
    /// <category>Rooms</category>
    /// <param type="System.Nullable{ASC.Files.Core.ApiModels.RequestDto.RoomType}, System" name="type">Filter by room type</param>
    /// <param type="System.String, System" name="subjectId">Filter by user ID</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="searchInContent">Specifies whether to search within the section contents or not</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withSubfolders">Specifies whether to return sections with or without subfolders</param>
    /// <param type="System.Nullable{ASC.Files.Core.VirtualRooms.SearchArea}, System" name="searchArea">Room search area (Active, Archive, Any)</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="withoutTags">Specifies whether to search by tags or not</param>
    /// <param type="System.String, System" name="tags">Tags in the serialized format</param>
    /// <param type="System.Nullable{System.Boolean}, System" name="excludeSubject">Specifies whether to exclude a subject or not</param>
    /// <param type="System.Nullable{ASC.Files.Core.ProviderFilter}, System" name="provider">Filter by provider name (None, Box, DropBox, GoogleDrive, kDrive, OneDrive, WebDav)</param>
    /// <param type="System.Nullable{ASC.Files.Core.Core.SubjectFilter}, System" name="subjectFilter">Filter by subject (Owner - 1, Member - 1)</param>
    /// <param type="System.Nullable{ASC.Core.QuotaFilter}, System" name="quotaFilter">Filter by quota (Default - 1, Custom - 2)</param>
    /// <param type="System.Nullable{ASC.Core.StorageFilter}, ASC.Files.Core" name="storageFilter">Filter by storage (Internal - 1, ThirdParty - 2)</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FolderContentDto, ASC.Files.Core">Rooms contents</returns>
    /// <path>api/2.0/files/rooms</path>
    /// <httpMethod>GET</httpMethod>
    [HttpGet("rooms")]
    public async Task<FolderContentDto<int>> GetRoomsFolderAsync(
        RoomType? type,
        string subjectId,
        bool? searchInContent,
        bool? withSubfolders,
        SearchArea? searchArea,
        bool? withoutTags,
        string tags,
        bool? excludeSubject,
        ProviderFilter? provider,
        SubjectFilter? subjectFilter,
        QuotaFilter? quotaFilter,
        StorageFilter? storageFilter)
    {
        var parentId = searchArea != SearchArea.Archive 
            ? await globalFolderHelper.GetFolderVirtualRooms()
            : await globalFolderHelper.GetFolderArchive();

        var filter = type switch
        {
            RoomType.FillingFormsRoom => FilterType.FillingFormsRooms,
            RoomType.EditingRoom => FilterType.EditingRooms,
            RoomType.CustomRoom => FilterType.CustomRooms,
            RoomType.PublicRoom => FilterType.PublicRooms,
            _ => FilterType.None
        };

        var tagNames = !string.IsNullOrEmpty(tags) 
            ? JsonSerializer.Deserialize<IEnumerable<string>>(tags) 
            : null;

        OrderBy orderBy = null;
        if (SortedByTypeExtensions.TryParse(apiContext.SortBy, true, out var sortBy))
        {
            orderBy = new OrderBy(sortBy, !apiContext.SortDescending);
        }

        var startIndex = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);
        var filterValue = apiContext.FilterValue;

        var content = await fileStorageService.GetFolderItemsAsync(parentId, startIndex, count, filter, false, subjectId, filterValue,
            [], searchInContent ?? false, withSubfolders ?? false, orderBy, searchArea ?? SearchArea.Active, default, withoutTags ?? false, tagNames, excludeSubject ?? false, 
            provider ?? ProviderFilter.None, subjectFilter ?? SubjectFilter.Owner, quotaFilter: quotaFilter ?? QuotaFilter.All, storageFilter: storageFilter ?? StorageFilter.None);

        var dto = await folderContentDtoHelper.GetAsync(parentId, content, startIndex);

        return dto.NotFoundIfNull();
    }

    /// <summary>
    /// Creates a custom tag with the parameters specified in the request.
    /// </summary>
    /// <short>Create a tag</short>
    /// <category>Rooms</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateTagRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating a tag</param>
    /// <returns type="System.Object, System">New tag name</returns>
    /// <path>api/2.0/files/tags</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("tags")]
    public async Task<object> CreateTagAsync(CreateTagRequestDto inDto)
    {
        return await customTagsService.CreateTagAsync(inDto.Name);
    }

    /// <summary>
    /// Returns a list of custom tags.
    /// </summary>
    /// <short>Get tags</short>
    /// <category>Rooms</category>
    /// <returns type="System.Object, System">List of tag names</returns>
    /// <path>api/2.0/files/tags</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
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
    /// <category>Rooms</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BatchTagsRequestDto, ASC.Files.Core" name="inDto">Batch tags request parameters</param>
    /// <returns></returns>
    /// <path>api/2.0/files/tags</path>
    /// <httpMethod>DELETE</httpMethod>
    [HttpDelete("tags")]
    public async Task DeleteTagsAsync(BatchTagsRequestDto inDto)
    {
        await customTagsService.DeleteTagsAsync<int>(inDto.Names);
    }

    /// <summary>
    /// Uploads a temporary image to create a room logo.
    /// </summary>
    /// <short>Upload an image for room logo</short>
    /// <category>Rooms</category>
    /// <param type="Microsoft.AspNetCore.Http.IFormCollection, Microsoft.AspNetCore.Http" name="formCollection">Image data</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.UploadResultDto, ASC.Files.Core">Upload result</returns>
    /// <path>api/2.0/files/logos</path>
    /// <httpMethod>POST</httpMethod>
    [HttpPost("logos")]
    public async Task<UploadResultDto> UploadRoomLogo(IFormCollection formCollection)
    {
        var currentUserType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);

        if (currentUserType is not (EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin))
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }
        
        var result = new UploadResultDto();

        try
        {
            if (formCollection.Files.Count != 0)
            {
                var roomLogo = formCollection.Files[0];
                
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

    [HttpPost("rooms/{id:int}/indexexport")]
    public async Task<DocumentBuilderTaskDto> StartRoomIndexExportAsync(int id)
    {
        var room = await fileStorageService.GetFolderAsync(id).NotFoundIfNull("Folder not found");

        if (!room.SettingsIndexing)
        {
            throw new NotSupportedException("Folder indexing is turned off");
        }

        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var userId = authContext.CurrentAccount.ID;

        var task = serviceProvider.GetService<DocumentBuilderTask<int>>();

        var commonLinkUtility = serviceProvider.GetService<CommonLinkUtility>();

        var baseUri = commonLinkUtility.ServerRootPath;

        task.Init(baseUri, tenantId, userId, null, null, null);

        var taskProgress = await documentBuilderTaskManager.StartTask(task, false);

        var evt = new RoomIndexExportIntegrationEvent(userId, tenantId, id, baseUri);

        await eventBus.PublishAsync(evt);

        return DocumentBuilderTaskDto.Get(taskProgress);
    }

    [HttpGet("rooms/indexexport")]
    public async Task<DocumentBuilderTaskDto> GetRoomIndexExport()
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var userId = authContext.CurrentAccount.ID;

        var task = await documentBuilderTaskManager.GetTask(tenantId, userId);

        return DocumentBuilderTaskDto.Get(task);
    }

    [HttpDelete("rooms/indexexport")]
    public async Task TerminateRoomIndexExport()
    {
        var tenantId = await tenantManager.GetCurrentTenantIdAsync();
        var userId = authContext.CurrentAccount.ID;

        var evt = new RoomIndexExportIntegrationEvent(userId, tenantId, 0, null, true);

        await eventBus.PublishAsync(evt);
    }
}