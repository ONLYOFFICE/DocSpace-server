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

using ASC.Common.Threading.DistributedLock.Abstractions;

namespace ASC.AI.Integration.Preferences;

[Scope]
public class PreferencesStorage(
    IDbContextFactory<AiIntegrationContext> dbContextFactory,
    IDistributedLockProvider distributedLockProvider)
{
    public async Task<Preferences?> ReadAsync(int tenantId, Guid userId, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var entity = entryId.HasValue
            ? await context.GetPreferencesByEntryAsync(tenantId, userId, entryId.Value)
            : await context.GetPreferencesAsync(tenantId, userId);

        if (entity == null)
        {
            return null;
        }

        return new Preferences
        {
            DeepMode = entity.DeepMode
        };
    }

    public async Task UpsertAsync(int tenantId, Guid userId, Preferences preferences, int? entryId = null)
    {
        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetLockKey(tenantId, userId, entryId)))
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var strategy = dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var context = await dbContextFactory.CreateDbContextAsync();

                var existing = entryId.HasValue
                    ? await context.GetPreferencesByEntryAsync(tenantId, userId, entryId.Value)
                    : await context.GetPreferencesAsync(tenantId, userId);

                if (existing != null)
                {
                    var entity = new DbPreference
                    {
                        Id = existing.Id,
                        TenantId = tenantId,
                        CreatedBy = userId,
                        EntryId = entryId,
                        DeepMode = preferences.DeepMode
                    };

                    context.Preferences.Attach(entity);
                    context.Entry(entity).Property(x => x.DeepMode).IsModified = true;
                }
                else
                {
                    context.Preferences.Add(new DbPreference
                    {
                        Id = Guid.CreateVersion7(),
                        TenantId = tenantId,
                        CreatedBy = userId,
                        EntryId = entryId,
                        DeepMode = preferences.DeepMode
                    });
                }

                await context.SaveChangesAsync();
            });
        }
    }

    public async Task DeleteAsync(int tenantId, Guid userId, int? entryId = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        if (entryId.HasValue)
        {
            await context.DeletePreferencesByEntryAsync(tenantId, userId, entryId.Value);
        }
        else
        {
            await context.DeletePreferencesAsync(tenantId, userId);
        }
    }

    private static string GetLockKey(int tenantId, Guid userId, int? entryId)
    {
        return entryId.HasValue
            ? $"ai_integration_preferences_{tenantId}_{userId}_{entryId.Value}"
            : $"ai_integration_preferences_{tenantId}_{userId}";
    }
}
