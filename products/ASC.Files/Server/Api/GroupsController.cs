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

public class GroupsController(FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileStorageService fileStorageService,
    RoomGroupDtoHelper roomGroupDtoHelper,
    AuthContext authContext)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{

    /// <summary>
    /// Creates a new room group with the specified name, icon, and list of rooms.
    /// </summary>
    /// <short>
    /// Add a new room group
    /// </short>
    /// <path>api/2.0/files/group</path>
    [HttpPost("group")]
    public async Task<RoomGroupDto> AddGroup(RoomGroupRequestDto inDto)
    {
        var (roomIntIds, roomStringIds) = FileOperationsManager.GetIds(inDto.Rooms);

        if (roomIntIds.Count == 0 && roomStringIds.Count == 0)
        {
            throw new InvalidOperationException("At least one room must be provided.");
        }

        var group = await fileStorageService.SaveRoomGroupAsync(new RoomGroup
        {
            Name = inDto.Name,
            UserID = authContext.CurrentAccount.ID,
            Icon = inDto.Icon
        });

        var addIntTasks = roomIntIds.Select(id => fileStorageService.AddRoomToGroupAsync(id, group.Id));
        var addStringTasks = roomStringIds.Select(id => fileStorageService.AddRoomToGroupAsync(id, group.Id));

        await Task.WhenAll(addIntTasks.Concat(addStringTasks));

        return await roomGroupDtoHelper.GetAsync(group);
    }

    [HttpGet("group/{id:int}")]
    public async Task<RoomGroupDto> GetGroupInfo(GroupIdRequestDto inDto)
    {
        var group = await GetGroupInfoAsync(inDto.Id).NotFoundIfNull("Group not found");

        return await roomGroupDtoHelper.GetAsync(group);
    }

    [HttpPut("group/{id:int}")]
    public async Task<RoomGroupDto> UpdateGroup(UpdateGroupRequestDto inDto)
    {
        var group = await GetGroupInfoAsync(inDto.Id);

        group.Name = inDto.Update.GroupName ?? group.Name;
        await fileStorageService.SaveRoomGroupAsync(group);

        if (inDto.Update.RoomsToAdd != null)
        {
            var (roomIntIds, roomStringIds) = FileOperationsManager.GetIds(inDto.Update.RoomsToAdd);
            await TransferRoomsToGroupAsync(roomIntIds, roomStringIds, group);
        }

        if (inDto.Update.RoomsToRemove != null)
        {
            var (roomIntIds, roomStringIds) = FileOperationsManager.GetIds(inDto.Update.RoomsToRemove);
            await RemoveRoomsFromGroupAsync(roomIntIds, roomStringIds, group);
        }

        // messageService.Send(MessageAction.GroupUpdated, MessageTarget.Create(inDto.Id), group.Name);
        //await socketManager.UpdateGroupAsync(dto);

        return await roomGroupDtoHelper.GetAsync(group);
    }

    [HttpPost("group/{id:int}/icon")]
    public async Task<RoomGroupDto> ChangeRoomIcon(IconRequestDto inDto)
    {
        var group = await fileStorageService.ChangeGroupIconAsync(inDto.Id, inDto.Update.Icon);

        return await roomGroupDtoHelper.GetAsync(group);
    }

    private async Task TransferRoomsToGroupAsync(List<int> roomIntIds, List<string> roomStringIds, RoomGroup group)
    {
        var addIntTasks = roomIntIds.Select(id => fileStorageService.AddRoomToGroupAsync(id, group.Id));
        var addStringTasks = roomStringIds.Select(id => fileStorageService.AddRoomToGroupAsync(id, group.Id));

        await Task.WhenAll(addIntTasks.Concat(addStringTasks));
    }
    private async Task RemoveRoomsFromGroupAsync(List<int> roomIntIds, List<string> roomStringIds, RoomGroup group)
    {
        var addIntTasks = roomIntIds.Select(id => fileStorageService.RemoveRoomFromGroupAsync(id, group.Id));
        var addStringTasks = roomStringIds.Select(id => fileStorageService.RemoveRoomFromGroupAsync(id, group.Id));

        await Task.WhenAll(addIntTasks.Concat(addStringTasks));
    }
    private async Task<RoomGroup> GetGroupInfoAsync(int id)
    {
        var group = await fileStorageService.GetGroupInfoAsync(id);
        if (group == null)
        {
            throw new ItemNotFoundException(Resource.ErrorGroupNotFound);
        }

        return group;
    }
}