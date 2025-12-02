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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The room security parameters.
/// </summary>
public class RoomGroupDto
{
    /// <summary>
    /// The group ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Group name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Group icon
    /// </summary>
    public LogoCover Icon { get; set; }

    /// <summary>
    /// The user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The list of rooms in the group. 
    /// </summary>
    public List<FileEntryBaseDto> Rooms { get; set; }
}

[Scope]
public class RoomGroupDtoHelper(FolderDtoHelper folderWrapperHelper, IDaoFactory daoFactory)
{
    public async Task<RoomGroupDto> GetAsync(RoomGroup group)
    {

        var result = new RoomGroupDto
        {
            Id = group.Id,
            Name = group.Name,
            UserId = group.UserID
        };

        var roomGroupDao = daoFactory.RoomGroupDao;
        var roomGroupRefs = await roomGroupDao.GetRoomsByGroupAsync(group.Id).ToListAsync();

        var fInt = new List<int>();
        var fString = new List<string>();

        foreach (var roomGroupRef in roomGroupRefs)
        {
            if (roomGroupRef.InternalRoomId != null)
            {
                fInt.Add((int)roomGroupRef.InternalRoomId);
            }
            else
            {
                fString.Add(roomGroupRef.ThirdpartyRoomId);
            }
        }

        var internalRooms = GetFoldersAsync(fInt).ToListAsync();
        var thirdPartyRooms = GetFoldersAsync(fString).ToListAsync();

        result.Rooms = [];
        foreach (var f in await Task.WhenAll(internalRooms.AsTask(), thirdPartyRooms.AsTask()))
        {
            result.Rooms.AddRange(f);
        }

        LogoCover cover = null;
        if (!string.IsNullOrEmpty(group.Icon) && (await RoomLogoManager.GetCoversAsync()).TryGetValue(group.Icon, out var fromDict))
        {
            cover = new LogoCover
            {
                Id = group.Icon,
                Data = fromDict
            };
        }
        result.Icon = cover;

        return result;

        async IAsyncEnumerable<FileEntryBaseDto> GetFoldersAsync<T>(IEnumerable<T> folders)
        {
            var folderDao = daoFactory.GetFolderDao<T>();

            await foreach (var folder in folderDao.GetFoldersAsync(folders))
            {
                yield return await folderWrapperHelper.GetAsync(folder);
            }
        }
    }
}