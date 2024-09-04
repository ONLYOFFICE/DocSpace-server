﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Core.VirtualRooms;

[Scope]
public class RoomLogoValidator(IDaoFactory daoFactory, FileSecurity fileSecurity, SecurityContext securityContext) : IDataStoreValidator
{
    public async Task<bool> Validate(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path, nameof(path));

        if (!securityContext.IsAuthenticated)
        {
            return true;
        }

        var data = path.Split(RoomLogoManager.LogosPathSplitter);

        if (data.Length < 2)
        {
            return false;
        }

        var id = path.Substring(0, path.LastIndexOf(RoomLogoManager.LogosPathSplitter, StringComparison.Ordinal));
        
        if (int.TryParse(id, out var internalId))
        {
            return await CheckRoomAccess(internalId);
        }

        var provider = await daoFactory.ProviderDao.GetProviderInfoByEntryIdAsync(id);
        if (provider is { MutableEntityId: true })
        {
            id = provider.FolderId;
        }

        return await CheckRoomAccess(id);
    }
    
    private async Task<bool> CheckRoomAccess<T>(T id)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(id);

        return DocSpaceHelper.IsRoom(folder.FolderType) && await fileSecurity.CanReadAsync(folder);
    }
}