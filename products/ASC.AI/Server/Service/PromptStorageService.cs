// (c) Copyright Ascensio System SIA 2009-2026
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

using ASC.AI.Integration.Prompts;
using ASC.Core.Users;

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
