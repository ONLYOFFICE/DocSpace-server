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
public class AiProjectThreadsService(
    TenantManager tenantManager,
    AuthContext authContext,
    ThreadsStorage storage,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    IServiceProvider serviceProvider,
    IDistributedLockProvider distributedLockProvider)
{
    private const string ArtifactsSuffix = "artifacts";
    private const string AttachmentsSuffix = "attachments";

    public async Task<AiThread> GetForReadAsync(int projectId, Guid threadId)
    {
        var thread = await storage.ReadByIdAsync(tenantManager.GetCurrentTenantId(), threadId);
        if (thread is null || thread.EntryId != projectId || thread.CreatedBy != authContext.CurrentAccount.ID)
        {
            throw new ItemNotFoundException();
        }

        return thread;
    }

    public async Task<File<int>> SaveArtifactAsync(AiThread thread, string title, long contentLength, Stream content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(contentLength);

        var project = await GetForWriteAsync(thread);
        var folderId = await GetOrCreateAsync(thread, project.Id, ThreadFolderKind.Artifacts);

        var maxUploadSize = await daoFactory.GetFolderDao<int>().GetMaxUploadSizeAsync(folderId);
        if (contentLength > maxUploadSize)
        {
            throw FileSizeComment.GetFileSizeException(maxUploadSize);
        }

        var fileDao = daoFactory.GetFileDao<int>();

        title = Global.ReplaceInvalidCharsAndTruncate(title);
        if (await fileDao.GetFileAsync(folderId, title) != null)
        {
            title = await fileDao.GetAvailableTitleAsync(title, folderId);
        }

        var file = serviceProvider.GetRequiredService<File<int>>();
        file.ParentId = folderId;
        file.Title = title;
        file.ContentLength = contentLength;
        file.CreateBy = thread.CreatedBy;

        return await fileDao.SaveThreadFileAsync(file, content, thread.Id);
    }

    public async Task<int> GetOrCreateAttachmentsFolderAsync(AiThread thread)
    {
        var project = await GetForWriteAsync(thread);

        return await GetOrCreateAsync(thread, project.Id, ThreadFolderKind.Attachments);
    }

    public async Task DeleteAsync(AiThread thread)
    {
        var tenantId = tenantManager.GetCurrentTenantId();

        if (await GetProjectAsync(thread) != null)
        {
            await storage.MarkDeletedAsync(tenantId, thread.Id);

            return;
        }

        await storage.DeleteAsync(tenantId, thread.Id);
    }

    public async Task DeleteMarkedAsync(DeletedThread thread)
    {
        await DeleteFolderAsync(thread.ArtifactsFolderId);
        await DeleteFolderAsync(thread.AttachmentsFolderId);

        await storage.DeleteMarkedAsync(thread.TenantId, thread.Id);
    }

    private async Task<Folder<int>> GetForWriteAsync(AiThread thread)
    {
        if (!thread.EntryId.HasValue)
        {
            throw new ArgumentException(FilesCommonResource.ErrorMessage_BadRequest, nameof(thread));
        }

        var project = await daoFactory.GetFolderDao<int>().GetFolderAsync(thread.EntryId.Value)
                      ?? throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);

        if (project.FolderType != FolderType.AiProjectRoom)
        {
            throw new ArgumentException(FilesCommonResource.ErrorMessage_BadRequest, nameof(thread));
        }

        if (!await fileSecurity.CanUseAiAsync(project))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        return project;
    }

    private async Task<Folder<int>> GetProjectAsync(AiThread thread)
    {
        if (!thread.EntryId.HasValue)
        {
            return null;
        }

        var folder = await daoFactory.GetFolderDao<int>().GetFolderAsync(thread.EntryId.Value);

        return folder is { FolderType: FolderType.AiProjectRoom } ? folder : null;
    }

    private async Task DeleteFolderAsync(int? folderId)
    {
        if (!folderId.HasValue)
        {
            return;
        }

        var folderDao = daoFactory.GetFolderDao<int>();
        if (await folderDao.GetFolderAsync(folderId.Value) == null)
        {
            return;
        }

        var fileDao = daoFactory.GetFileDao<int>();
        var fileIds = await fileDao.GetFilesAsync(folderId.Value).ToListAsync();
        var files = await fileDao.GetFilesAsync(fileIds).ToListAsync();

        var deleted = (await fileDao.DeleteFilesAsync(files.Select(f => KeyValuePair.Create(f.Id, f.GetFileQuotaOwner())))).ToHashSet();

        await folderDao.ChangeTreeFolderSizeAsync(folderId.Value, -files.Where(f => deleted.Contains(f.Id)).Sum(f => f.ContentLength));
        await folderDao.DeleteFolderAsync(folderId.Value);
    }

    private async Task<int> GetOrCreateAsync(AiThread thread, int projectId, ThreadFolderKind kind)
    {
        var folderId = GetFolderId(thread);
        if (folderId.HasValue)
        {
            return folderId.Value;
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"ai_thread_folders_{tenantId}_{thread.Id}"))
        {
            var current = await storage.ReadByIdAsync(tenantId, thread.Id) ?? throw new ItemNotFoundException();

            folderId = GetFolderId(current);
            if (folderId.HasValue)
            {
                return folderId.Value;
            }

            var folder = serviceProvider.GetRequiredService<Folder<int>>();
            folder.Title = $"{current.Id}-{(kind == ThreadFolderKind.Artifacts ? ArtifactsSuffix : AttachmentsSuffix)}";
            folder.ParentId = projectId;
            folder.CreateBy = current.CreatedBy;

            var createdId = await daoFactory.GetFolderDao<int>().SaveThreadFolderAsync(folder, current.Id, kind);
            if (createdId != 0)
            {
                return createdId;
            }

            current = await storage.ReadByIdAsync(tenantId, thread.Id) ?? throw new ItemNotFoundException();

            return GetFolderId(current) ?? throw new ItemNotFoundException();
        }

        int? GetFolderId(AiThread x)
        {
            return kind == ThreadFolderKind.Artifacts ? x.ArtifactsFolderId : x.AttachmentsFolderId;
        }
    }
}
