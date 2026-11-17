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
public class AiThreadFoldersService(
    TenantManager tenantManager,
    ThreadsStorage storage,
    IDaoFactory daoFactory,
    IServiceProvider serviceProvider,
    IDistributedLockProvider distributedLockProvider)
{
    private const string ArtifactsSuffix = "artifacts";
    private const string AttachmentsSuffix = "attachments";

    public Task<int> GetOrCreateArtifactsFolderAsync(AiThread thread, int projectId)
    {
        return GetOrCreateAsync(thread, projectId, ThreadFolderKind.Artifacts);
    }

    public Task<int> GetOrCreateAttachmentsFolderAsync(AiThread thread, int projectId)
    {
        return GetOrCreateAsync(thread, projectId, ThreadFolderKind.Attachments);
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
