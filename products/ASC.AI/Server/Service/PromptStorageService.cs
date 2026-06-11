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

namespace ASC.AI.Service;

[Scope]
public class PromptStorageService(
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    PromptStorage storage,
    PromptFolderStorage folderStorage,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity)
{
    private static readonly EmployeeType[] _allowedTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin];

    public async Task<Prompt> CreateAsync(string name, string text, Guid? folderId)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var tenantId = tenantManager.GetCurrentTenantId();

        if (folderId.HasValue)
        {
            await AssertFolderExistsAsync(tenantId, folderId.Value);
        }

        return await storage.CreateAsync(tenantId, CurrentUserId, name, text, folderId);
    }

    public async Task<IEnumerable<Prompt>> CreateManyAsync(IReadOnlyList<PromptCreateData> prompts)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        return await storage.CreateManyAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, prompts);
    }

    public async Task<Prompt> ReadByIdAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var prompt = await storage.ReadByIdAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, id);

        return prompt ?? throw new ItemNotFoundException();
    }

    public async Task<IEnumerable<Prompt>> ReadAllAsync()
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        return await storage.ReadAllAsync(tenantManager.GetCurrentTenantId(), CurrentUserId);
    }

    public async Task<IEnumerable<Prompt>> ReadByFolderIdAsync(Guid? folderId)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        return await storage.ReadByFolderIdAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, folderId);
    }

    public async Task UpdateAsync(Guid id, string? name, string? text, bool changeFolder, Guid? folderId)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        if (name is not null && string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(@"Prompt name cannot be empty or whitespace", nameof(name));
        }

        if (text is not null && string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(@"Prompt text cannot be empty or whitespace", nameof(text));
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var existing = await storage.ReadByIdAsync(tenantId, CurrentUserId, id) ?? throw new ItemNotFoundException();

        var newName = name ?? existing.Name;
        var newText = text ?? existing.Text;
        var newFolderId = changeFolder ? folderId : existing.FolderId;

        if (changeFolder && newFolderId.HasValue)
        {
            await AssertFolderExistsAsync(tenantId, newFolderId.Value);
        }

        var updated = await storage.UpdateAsync(tenantId, CurrentUserId, id, newName, newText, newFolderId);
        if (!updated)
        {
            throw new ItemNotFoundException();
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var deleted = await storage.DeleteAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, id);
        if (!deleted)
        {
            throw new ItemNotFoundException();
        }
    }

    private async Task AssertFolderExistsAsync(int tenantId, Guid folderId)
    {
        _ = await folderStorage.ReadByIdAsync(tenantId, CurrentUserId, folderId)
            ?? throw new ItemNotFoundException($"Prompt folder with id '{folderId}' was not found");
    }
}
