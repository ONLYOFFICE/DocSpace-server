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


namespace ASC.Files.Core.Core.AI;

[Scope]
public class AiProjectRoomService(
    IDaoFactory daoFactory,
    AiProjectRoomDao aiProjectRoomDao,
    FileSecurity fileSecurity,
    TenantManager tenantManager,
    GlobalFolderHelper globalFolderHelper,
    IDistributedLockProvider distributedLockProvider,
    FilesMessageService filesMessageService,
    SocketManager socketManager)
{
    public const int MaxRoomsPerType = 100;

    public async Task<List<Folder<int>>> GetRoomsAsync(int projectId)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        await GetProjectAsync(projectId);

        var linked = await aiProjectRoomDao.GetLinkedRoomsAsync(tenantId, projectId, await globalFolderHelper.GetFolderAiHubAsync());
        if (linked.Count == 0)
        {
            return [];
        }

        return await daoFactory.GetFolderDao<int>().GetFoldersAsync(linked.Keys).ToListAsync();
    }

    public async Task LinkRoomsAsync(int projectId, IReadOnlySet<int> roomIds)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var project = await GetProjectForEditAsync(projectId);
        var roomsFolderId = await globalFolderHelper.GetFolderAiHubAsync();

        var linkable = await aiProjectRoomDao.GetLinkableRoomsAsync(tenantId, roomsFolderId, roomIds);
        ThrowIfNotLinkable([.. roomIds.Where(id => !linkable.ContainsKey(id))]);

        var rooms = await daoFactory.GetFolderDao<int>().GetFoldersAsync(linkable.Keys).ToListAsync();
        var readableIds = await fileSecurity.FilterReadAsync(rooms.ToAsyncEnumerable()).Select(r => r.Id).ToHashSetAsync();
        ThrowIfNotLinkable([.. roomIds.Where(id => !readableIds.Contains(id))]);

        List<Folder<int>> added;

        await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetAiProjectRoomsKey(tenantId, projectId)))
        {
            var linked = await aiProjectRoomDao.GetLinkedRoomsAsync(tenantId, projectId, roomsFolderId);

            added = [.. rooms.Where(r => !linked.ContainsKey(r.Id))];

            CheckLimit(linked, added, FolderType.InstructionsRoom, FilesCommonResource.ErrorMessage_AiProjectInstructionsRoomsLimit);
            CheckLimit(linked, added, FolderType.KnowledgeRoom, FilesCommonResource.ErrorMessage_AiProjectKnowledgeRoomsLimit);

            if (added.Count > 0)
            {
                await aiProjectRoomDao.AddRoomsAsync(tenantId, projectId, added.Select(r => r.Id));
            }
        }

        if (added.Count == 0)
        {
            return;
        }

        await filesMessageService.SendAsync(MessageAction.RoomsLinkedToAiProject, project, project.Title, string.Join(", ", added.Select(r => r.Title)));

        await socketManager.UpdateFolderAsync(project);
    }

    public async Task UnlinkRoomsAsync(int projectId, IReadOnlySet<int> roomIds)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var project = await GetProjectForEditAsync(projectId);

        List<int> removed;

        await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetAiProjectRoomsKey(tenantId, projectId)))
        {
            var linkedIds = await aiProjectRoomDao.GetRoomIdsAsync(tenantId, projectId);

            removed = [.. linkedIds.Where(roomIds.Contains)];
            if (removed.Count > 0)
            {
                await aiProjectRoomDao.RemoveRoomsAsync(tenantId, projectId, removed);
            }
        }

        if (removed.Count == 0)
        {
            return;
        }

        var titles = await daoFactory.GetFolderDao<int>().GetFoldersAsync(removed).Select(r => r.Title).ToListAsync();
        await filesMessageService.SendAsync(MessageAction.RoomsUnlinkedFromAiProject, project, project.Title, string.Join(", ", titles));

        await socketManager.UpdateFolderAsync(project);
    }

    private async Task<Folder<int>> GetProjectAsync(int projectId)
    {
        var project = await daoFactory.GetFolderDao<int>().GetFolderAsync(projectId);
        if (project is not { FolderType: FolderType.AiProjectRoom })
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanReadAsync(project))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_ReadFolder);
        }

        return project;
    }

    private async Task<Folder<int>> GetProjectForEditAsync(int projectId)
    {
        var project = await GetProjectAsync(projectId);
        if (!await fileSecurity.CanEditRoomAsync(project))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditRoom);
        }

        return project;
    }

    private static void ThrowIfNotLinkable(List<int> roomIds)
    {
        if (roomIds.Count > 0)
        {
            throw new ArgumentException(string.Format(FilesCommonResource.ErrorMessage_AiProjectRoomsNotLinkable, string.Join(", ", roomIds)));
        }
    }

    private static void CheckLimit(Dictionary<int, FolderType> linked, List<Folder<int>> added, FolderType roomType, string message)
    {
        if (linked.Values.Count(t => t == roomType) + added.Count(r => r.FolderType == roomType) > MaxRoomsPerType)
        {
            throw new ArgumentException(string.Format(message, MaxRoomsPerType));
        }
    }
}
