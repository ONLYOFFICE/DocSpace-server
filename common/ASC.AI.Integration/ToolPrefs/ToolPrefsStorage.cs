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

namespace ASC.AI.Integration.ToolPrefs;

[Scope]
public class ToolPrefsStorage(IDbContextFactory<AiIntegrationContext> dbContextFactory)
{
    public async Task<Dictionary<string, List<string>>> ReadDisabledAsync(int tenantId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        return await context.GetAllToolPrefsAsync(tenantId)
            .ToDictionaryAsync(x => x.ServerType, x => x.Tools);
    }

    public async Task UpsertDisabledAsync(int tenantId, IReadOnlyDictionary<string, List<string>> disabled)
    {
        if (disabled.Count == 0)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            var existingKeys = await context.GetExistingToolPrefsServerTypesAsync(tenantId, disabled.Keys)
                .ToHashSetAsync();

            var now = DateTime.UtcNow;
            foreach (var (serverType, tools) in disabled)
            {
                if (existingKeys.Contains(serverType))
                {
                    var entity = new DbToolPrefs
                    {
                        TenantId = tenantId,
                        ServerType = serverType,
                        Tools = tools,
                        CreatedAt = default
                    };
                    context.ToolPrefs.Attach(entity);
                    context.Entry(entity).Property(x => x.Tools).IsModified = true;
                }
                else
                {
                    context.ToolPrefs.Add(new DbToolPrefs
                    {
                        TenantId = tenantId,
                        ServerType = serverType,
                        Tools = tools,
                        CreatedAt = now
                    });
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    public async Task DeleteDisabledAsync(int tenantId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        await context.DeleteAllToolPrefsAsync(tenantId);
    }
}
