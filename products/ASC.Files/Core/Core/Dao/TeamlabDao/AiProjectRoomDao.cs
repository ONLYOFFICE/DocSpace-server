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

namespace ASC.Files.Core.Data;

[Scope]
public class AiProjectRoomDao(IDbContextFactory<FilesDbContext> dbContextFactory)
{
    public async Task<List<int>> GetRoomIdsAsync(int tenantId, int projectId)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.AiProjectRoomIdsAsync(tenantId, projectId).ToListAsync();
    }

    public async Task<Dictionary<int, FolderType>> GetLinkedRoomsAsync(int tenantId, int projectId, int roomsFolderId)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.AiProjectLinkedRoomsAsync(tenantId, projectId, roomsFolderId).ToDictionaryAsync(f => f.Id, f => f.FolderType);
    }

    public async Task<Dictionary<int, FolderType>> GetLinkableRoomsAsync(int tenantId, int roomsFolderId, IEnumerable<int> roomIds)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.AiProjectLinkableRoomsAsync(tenantId, roomsFolderId, roomIds).ToDictionaryAsync(f => f.Id, f => f.FolderType);
    }

    public async Task AddRoomsAsync(int tenantId, int projectId, IEnumerable<int> roomIds)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        filesDbContext.AiProjectRooms.AddRange(roomIds.Select(roomId => new DbFilesAiProjectRoom
        {
            TenantId = tenantId,
            ProjectId = projectId,
            RoomId = roomId
        }));

        await filesDbContext.SaveChangesAsync();
    }

    public async Task RemoveRoomsAsync(int tenantId, int projectId, IEnumerable<int> roomIds)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        await filesDbContext.DeleteAiProjectRoomsAsync(tenantId, projectId, roomIds);
    }
}
