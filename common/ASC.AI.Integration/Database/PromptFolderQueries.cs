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

namespace ASC.AI.Integration.Database;

public partial class AiIntegrationContext
{
    [PreCompileQuery]
    public Task<DbPromptFolder?> GetPromptFolderAsync(int tenantId, Guid createdBy, Guid id)
    {
        return PromptFolderQueriesContainer.GetPromptFolderAsync(this, tenantId, createdBy, id);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<DbPromptFolder> GetAllPromptFoldersAsync(int tenantId, Guid createdBy)
    {
        return PromptFolderQueriesContainer.GetAllPromptFoldersAsync(this, tenantId, createdBy);
    }

    [PreCompileQuery]
    public Task<int> UpdatePromptFolderNameAsync(int tenantId, Guid createdBy, Guid id, string name, DateTime updatedAt)
    {
        return PromptFolderQueriesContainer.UpdatePromptFolderNameAsync(this, tenantId, createdBy, id, name, updatedAt);
    }

    [PreCompileQuery]
    public Task<int> DeletePromptFolderAsync(int tenantId, Guid createdBy, Guid id)
    {
        return PromptFolderQueriesContainer.DeletePromptFolderAsync(this, tenantId, createdBy, id);
    }
}

static file class PromptFolderQueriesContainer
{
    public static readonly Func<AiIntegrationContext, int, Guid, Guid, Task<DbPromptFolder?>> GetPromptFolderAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy, Guid id) =>
                ctx.PromptFolders.FirstOrDefault(x =>
                    x.TenantId == tenantId && x.CreatedBy == createdBy && x.Id == id));

    public static readonly Func<AiIntegrationContext, int, Guid, IAsyncEnumerable<DbPromptFolder>> GetAllPromptFoldersAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy) =>
                ctx.PromptFolders
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy)
                    .OrderByDescending(x => x.CreatedAt)
                    .AsQueryable());

    public static readonly Func<AiIntegrationContext, int, Guid, Guid, string, DateTime, Task<int>> UpdatePromptFolderNameAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy, Guid id, string name, DateTime updatedAt) =>
                ctx.PromptFolders
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.Id == id)
                    .ExecuteUpdate(x => x
                        .SetProperty(y => y.Name, name)
                        .SetProperty(y => y.UpdatedAt, updatedAt)));

    public static readonly Func<AiIntegrationContext, int, Guid, Guid, Task<int>> DeletePromptFolderAsync =
        EF.CompileAsyncQuery(
            (AiIntegrationContext ctx, int tenantId, Guid createdBy, Guid id) =>
                ctx.PromptFolders
                    .Where(x => x.TenantId == tenantId && x.CreatedBy == createdBy && x.Id == id)
                    .ExecuteDelete());
}
