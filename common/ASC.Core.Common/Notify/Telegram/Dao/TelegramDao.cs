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

namespace ASC.Core.Common.Notify.Telegram;

[Scope]
public class TelegramDao(IDbContextFactory<TelegramDbContext> dbContextFactory)
{
    public async Task RegisterUserAsync(Guid userId, int tenantId, long telegramId, string telegramUsername)
    {
        var user = new TelegramUser
        {
            PortalUserId = userId,
            TenantId = tenantId,
            TelegramUserId = telegramId,
            TelegramUsername = telegramUsername
        };

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        _ = await dbContext.AddOrUpdateAsync(q => q.Users, user);
        _ = await dbContext.SaveChangesAsync();
    }

    public async Task<TelegramUser> GetUserAsync(Guid userId, int tenantId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Users.FindAsync(tenantId, userId);
    }

    public async Task<List<TelegramUser>> GetUsersAsync(long telegramId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await Queries.TelegramUsersAsync(dbContext, telegramId).ToListAsync();
    }

    public async Task DeleteAsync(Guid userId, int tenantId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        _ = await Queries.DeleteTelegramUsersAsync(dbContext, tenantId, userId);
    }

    public async Task DeleteAsync(long telegramId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        _ = await Queries.DeleteTelegramUsersByTelegramIdAsync(dbContext, telegramId);
    }
}

static file class Queries
{
    public static readonly Func<TelegramDbContext, long, IAsyncEnumerable<TelegramUser>> TelegramUsersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TelegramDbContext ctx, long telegramId) =>
                ctx.Users

                    .Where(r => r.TelegramUserId == telegramId));

    public static readonly Func<TelegramDbContext, int, Guid, Task<int>> DeleteTelegramUsersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TelegramDbContext ctx, int tenantId, Guid userId) =>
                ctx.Users
                    .Where(r => r.PortalUserId == userId)
                    .Where(r => r.TenantId == tenantId)
                    .ExecuteDelete());

    public static readonly Func<TelegramDbContext, long, Task<int>> DeleteTelegramUsersByTelegramIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (TelegramDbContext ctx, long telegramId) =>
                ctx.Users
                    .Where(r => r.TelegramUserId == telegramId)
                    .ExecuteDelete());
}