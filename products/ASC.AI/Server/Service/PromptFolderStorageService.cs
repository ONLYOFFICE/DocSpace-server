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
public class PromptFolderStorageService(
    UserManager userManager,
    AuthContext authContext,
    TenantManager tenantManager,
    PromptFolderStorage storage,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    AiGateway gateway) : IntegrationServiceBase(userManager, authContext, daoFactory, fileSecurity, gateway)
{
    private static readonly EmployeeType[] _allowedTypes = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin];

    public async Task<PromptFolder> CreateAsync(string name)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        return await storage.CreateAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, name);
    }

    public async Task<IReadOnlyList<PromptFolder>> CreateManyAsync(IReadOnlyList<string> names)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        return await storage.CreateManyAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, names);
    }

    public async Task<PromptFolder> ReadByIdAsync(Guid id)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var folder = await storage.ReadByIdAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, id);

        return folder ?? throw new ItemNotFoundException();
    }

    public async Task<List<PromptFolder>> ReadAllAsync()
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        return await storage.ReadAllAsync(tenantManager.GetCurrentTenantId(), CurrentUserId);
    }

    public async Task RenameAsync(Guid id, string name)
    {
        await AssertUserHasAccessAsync(_allowedTypes);

        var updated = await storage.UpdateNameAsync(tenantManager.GetCurrentTenantId(), CurrentUserId, id, name);
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
}
