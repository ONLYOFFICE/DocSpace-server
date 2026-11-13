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

using Thread = ASC.AI.Integration.Threads.Thread;

namespace ASC.AI.Service;

[Scope]
public class ThreadFoldersService(
    TenantManager tenantManager,
    ThreadsStorage storage,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    IServiceProvider serviceProvider,
    IDistributedLockProvider distributedLockProvider)
{
    private const string ArtifactsSuffix = "artifacts";
    private const string AttachmentsSuffix = "attachments";

    public async Task<Folder<int>> GetProjectAsync(Thread thread)
    {
        if (!thread.EntryId.HasValue)
        {
            throw new ArgumentException("The thread does not belong to an AI project");
        }

        var project = await daoFactory.GetFolderDao<int>().GetFolderAsync(thread.EntryId.Value)
                      ?? throw new ItemNotFoundException();

        if (project.FolderType != FolderType.AiProjectRoom)
        {
            throw new ArgumentException("The thread does not belong to an AI project");
        }

        if (!await fileSecurity.CanUseAiAsync(project))
        {
            throw new SecurityException();
        }

        return project;
    }

    public Task<int> GetOrCreateArtifactsFolderAsync(Thread thread)
    {
        return GetOrCreateAsync(thread, ArtifactsSuffix, x => x.ArtifactsFolderId, storage.SetArtifactsFolderAsync);
    }

    public Task<int> GetOrCreateAttachmentsFolderAsync(Thread thread)
    {
        return GetOrCreateAsync(thread, AttachmentsSuffix, x => x.AttachmentsFolderId, storage.SetAttachmentsFolderAsync);
    }

    private async Task<int> GetOrCreateAsync(
        Thread thread,
        string suffix,
        Func<Thread, int?> getFolderId,
        Func<int, Guid, int, Task<int>> setFolderIdAsync)
    {
        var project = await GetProjectAsync(thread);

        var folderId = getFolderId(thread);
        if (folderId.HasValue)
        {
            return folderId.Value;
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"ai_thread_folders_{tenantId}_{thread.Id}"))
        {
            var current = await storage.ReadByIdAsync(tenantId, thread.Id) ?? throw new ItemNotFoundException();

            folderId = getFolderId(current);
            if (folderId.HasValue)
            {
                return folderId.Value;
            }

            var folderDao = daoFactory.GetFolderDao<int>();

            var folder = serviceProvider.GetRequiredService<Folder<int>>();
            folder.Title = $"{current.Id}-{suffix}";
            folder.ParentId = project.Id;
            folder.CreateBy = current.CreatedBy;

            var createdId = await folderDao.SaveFolderAsync(folder);
            var attached = false;

            try
            {
                attached = await setFolderIdAsync(tenantId, current.Id, createdId) > 0;
            }
            finally
            {
                if (!attached)
                {
                    await folderDao.DeleteFolderAsync(createdId);
                }
            }

            if (attached)
            {
                return createdId;
            }

            current = await storage.ReadByIdAsync(tenantId, thread.Id) ?? throw new ItemNotFoundException();

            return getFolderId(current) ?? throw new ItemNotFoundException();
        }
    }
}
