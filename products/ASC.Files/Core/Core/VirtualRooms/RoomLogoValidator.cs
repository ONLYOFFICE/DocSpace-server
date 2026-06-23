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

namespace ASC.Files.Core.VirtualRooms;

[Scope]
public class RoomLogoValidator(IDaoFactory daoFactory, FileSecurity fileSecurity, SecurityContext securityContext) : IDataStoreValidator
{
    public async Task<bool> Validate(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

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

        return folder.IsRoom && await fileSecurity.CanReadAsync(folder);
    }
}
