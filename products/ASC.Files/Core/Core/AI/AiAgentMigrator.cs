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

public readonly record struct AiAgentMigrationResult(int Migrated, int Failed);

[Scope]
public class AiAgentMigrator(
    IDaoFactory daoFactory,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    TenantManager tenantManager,
    AuthContext authContext,
    UserManager userManager,
    IDistributedLockProvider distributedLockProvider,
    RoomLogoManager roomLogoManager,
    SocketManager socketManager,
    IEventBus eventBus,
    ILogger<AiAgentMigrator> logger)
{
    public async Task<AiAgentMigrationResult> MigrateAgentsAsync(IReadOnlyList<int> agentIds, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        var migrated = 0;
        var failed = 0;

        try
        {
            foreach (var agentId in agentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await using (await distributedLockProvider.TryAcquireLockAsync(LockKeyHelper.GetAiAgentMigrationKey(tenantId, agentId), cancellationToken: cancellationToken))
                    {
                        if (await MigrateAgentAsync(tenantId, agentId))
                        {
                            migrated++;
                        }
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    failed++;
                    logger.ErrorAgentFailed(e, tenantId, agentId);
                }
            }
        }
        finally
        {
            if (agentIds.Count > 0)
            {
                logger.InfoPortalProcessed(tenantId, agentIds.Count, migrated, failed);
            }
        }

        return new AiAgentMigrationResult(migrated, failed);
    }

    public async Task<List<int>> FindAgentsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantManager.GetCurrentTenantId();
        var folderDao = daoFactory.GetFolderDao<int>();

        int[] rootIds =
        [
            await folderDao.GetFolderIDAiAgentsAsync(false),
            await folderDao.GetFolderIDVirtualRooms(false),
            await folderDao.GetFolderIDAiHubAsync(false)
        ];

        var parentIds = rootIds.Where(id => id != 0).ToList();
        if (parentIds.Count == 0)
        {
            return [];
        }

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await filesDbContext.AiAgentRoomIdsAsync(tenantId, parentIds).ToListAsync(cancellationToken);
    }

    private async Task<bool> MigrateAgentAsync(int tenantId, int agentId)
    {
        var folderDao = daoFactory.GetFolderDao<int>();

        var agent = await folderDao.GetFolderAsync(agentId);
        if (agent is not { FolderType: FolderType.AiRoom })
        {
            return false;
        }

        await MoveOutputsAsync(folderDao, tenantId, agent);
        await DeleteKnowledgeSubfoldersAsync(folderDao, tenantId, agent.Id);

        var aiHubId = await folderDao.GetFolderIDAiHubAsync(true);
        if (agent.ParentId != aiHubId)
        {
            await folderDao.MoveFolderAsync(agentId, aiHubId, null);
        }

        var conversion = await folderDao.ConvertAiAgentAsync(agentId);
        if (conversion == null)
        {
            return false;
        }

        await CompleteAsync(folderDao, tenantId, agent, conversion);

        return true;
    }

    private async Task MoveOutputsAsync(IFolderDao<int> folderDao, int tenantId, Folder<int> agent)
    {
        List<DbFolder> children;

        await using (var filesDbContext = await dbContextFactory.CreateDbContextAsync())
        {
            children = await filesDbContext.AiAgentChildFoldersAsync(tenantId, agent.Id).ToListAsync();
        }

        int? myDocumentsId = null;

        foreach (var child in children.Where(c => c.FolderType != FolderType.Knowledge))
        {
            if (await folderDao.IsEmptyAsync(child.Id))
            {
                await folderDao.DeleteFolderAsync(child.Id);

                continue;
            }

            // The type goes first: a folder of a system type cannot be moved. The title is stored by the move
            // itself, so a run that stops between the two statements leaves the title of the folder as it was
            // and the next run composes the same one.
            if (child.FolderType != FolderType.DEFAULT)
            {
                await folderDao.ChangeFolderTypeAsync(await folderDao.GetFolderAsync(child.Id), FolderType.DEFAULT);
            }

            myDocumentsId ??= await folderDao.GetFolderIDUserAsync(true, await GetOutputsOwnerIdAsync(agent));

            await folderDao.MoveFolderAsync(child.Id, myDocumentsId.Value, AiAgentMigrationTitle.Compose(agent.Title, child.Title), null);

            await RunSideEffectAsync(tenantId, agent.Id, "the socket event of the moved Chat outputs folder", async () =>
            {
                await socketManager.CreateFolderAsync(await folderDao.GetFolderAsync(child.Id));
            });
        }
    }

    private async Task DeleteKnowledgeSubfoldersAsync(IFolderDao<int> folderDao, int tenantId, int agentId)
    {
        List<int> subfolderIds;
        List<int> fileIds;

        await using (var filesDbContext = await dbContextFactory.CreateDbContextAsync())
        {
            var knowledge = await filesDbContext.AiAgentKnowledgeFolderAsync(tenantId, agentId);
            if (knowledge == null)
            {
                return;
            }

            subfolderIds = await filesDbContext.AiAgentChildFoldersAsync(tenantId, knowledge.Id).Select(f => f.Id).ToListAsync();
            fileIds = await filesDbContext.AiAgentNestedFileIdsAsync(tenantId, knowledge.Id).ToListAsync();
        }

        if (subfolderIds.Count == 0)
        {
            return;
        }

        if (fileIds.Count > 0)
        {
            var fileDao = daoFactory.GetFileDao<int>();
            var files = await fileDao.GetFilesAsync(fileIds).ToListAsync();

            await fileDao.DeleteFilesAsync(files.Select(f => KeyValuePair.Create(f.Id, f.GetFileQuotaOwner())));
        }

        foreach (var subfolderId in subfolderIds)
        {
            await folderDao.DeleteFolderAsync(subfolderId);
        }

        logger.InfoKnowledgeSubfoldersDeleted(tenantId, agentId, subfolderIds.Count, fileIds.Count);
    }

    private async Task<Guid> GetOutputsOwnerIdAsync(Folder<int> agent)
    {
        var owner = await userManager.GetUsersAsync(agent.CreateBy);

        return userManager.UserExists(owner) && !userManager.IsSystemUser(owner.Id) && owner.Status != EmployeeStatus.Terminated
            ? owner.Id
            : tenantManager.GetCurrentTenant().OwnerId;
    }

    private async Task CompleteAsync(IFolderDao<int> folderDao, int tenantId, Folder<int> agent, AiAgentConversion conversion)
    {
        var agentId = agent.Id;
        var projectId = conversion.ProjectId;
        var currentAccount = authContext.CurrentAccount.ID;

        if (agent.SettingsHasLogo)
        {
            await RunSideEffectAsync(tenantId, agentId, "the copy of the logo", async () =>
            {
                var project = await folderDao.GetFolderAsync(projectId);

                if (await roomLogoManager.CopyAsync(agent, project))
                {
                    project.SettingsHasLogo = true;
                    await folderDao.SaveFolderAsync(project);
                }
            });
        }

        await RunSideEffectAsync(tenantId, agentId, "the index event of the project", () =>
            eventBus.PublishAsync(new FolderIndexIntegrationEvent(currentAccount, tenantId) { FolderId = projectId, Action = FolderIndexAction.Index }));

        await RunSideEffectAsync(tenantId, agentId, "the socket event of the project", async () =>
        {
            await socketManager.CreateFolderAsync(await folderDao.GetFolderAsync(projectId));
        });

        if (!conversion.HasKnowledge)
        {
            await DeleteRoomWithoutKnowledgeAsync(folderDao, tenantId, agentId, agent.SettingsHasLogo);

            return;
        }

        await RunSideEffectAsync(tenantId, agentId, "the index event of the room", () =>
            eventBus.PublishAsync(new FolderIndexIntegrationEvent(currentAccount, tenantId) { FolderId = agentId, Action = FolderIndexAction.Index }));

        await RunSideEffectAsync(tenantId, agentId, "the index events of the knowledge files", async () =>
        {
            foreach (var fileId in conversion.KnowledgeFileIds)
            {
                await eventBus.PublishAsync(new FileIndexIntegrationEvent(currentAccount, tenantId) { FileId = fileId, Action = FileIndexAction.UpdateFolders });
            }
        });

        await RunSideEffectAsync(tenantId, agentId, "the socket event of the room", async () =>
        {
            await socketManager.UpdateFolderAsync(await folderDao.GetFolderAsync(agentId));
        });
    }

    private async Task DeleteRoomWithoutKnowledgeAsync(IFolderDao<int> folderDao, int tenantId, int roomId, bool hasLogo)
    {
        Folder<int> room = null;
        IEnumerable<Guid> users = null;
        IEnumerable<Guid> sharedUsers = null;

        // the recipients are read before the deletion: afterwards nobody can read the room
        await RunSideEffectAsync(tenantId, roomId, "the recipients of the socket event of the deleted room", async () =>
        {
            room = await folderDao.GetFolderAsync(roomId);
            (users, sharedUsers) = await socketManager.GetDeleteRecipientsAsync(room);
        });

        var deleted = false;

        await RunSideEffectAsync(tenantId, roomId, "the deletion of the room without knowledge files", async () =>
        {
            await folderDao.DeleteFolderAsync(roomId);
            deleted = true;
        });

        if (deleted && hasLogo)
        {
            await RunSideEffectAsync(tenantId, roomId, "the deletion of the logo files of the deleted room", () =>
                roomLogoManager.DeleteLogoFilesAsync(roomId));
        }

        if (deleted && room != null && users != null)
        {
            await RunSideEffectAsync(tenantId, roomId, "the socket event of the deleted room", () =>
                socketManager.DeleteFolder(room, users, sharedUsers: sharedUsers));
        }
    }

    private async Task RunSideEffectAsync(int tenantId, int agentId, string effect, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception e)
        {
            logger.WarningEffectFailed(e, tenantId, agentId, effect);
        }
    }
}
