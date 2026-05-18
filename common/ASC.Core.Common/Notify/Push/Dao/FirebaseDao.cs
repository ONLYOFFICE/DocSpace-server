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

namespace ASC.Core.Common.Notify.Push;

[Scope]
public class FirebaseDao(IDbContextFactory<FirebaseDbContext> dbContextFactory)
{
    public async Task<FireBaseUser> RegisterUserDeviceAsync(Guid userId, int tenantId, string fbDeviceToken, bool isSubscribed, string application)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var user = await Queries.FireBaseUserAsync(dbContext, tenantId, userId, application, fbDeviceToken);


        if (user == null)
        {
            var newUser = new FireBaseUser
            {
                UserId = userId,
                TenantId = tenantId,
                FirebaseDeviceToken = fbDeviceToken,
                IsSubscribed = isSubscribed,
                Application = application
            };
            await dbContext.AddAsync(newUser);
            await dbContext.SaveChangesAsync();

            return newUser;
        }

        return user;
    }

    public virtual async Task<List<FireBaseUser>> GetSubscribedUserDeviceTokensAsync(Guid userId, int tenantId, string application)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        return await Queries.FireBaseSubscribedUsersAsync(dbContext, tenantId, userId, application).ToListAsync();
    }

    public async Task<FireBaseUser> UpdateUserAsync(Guid userId, int tenantId, string fbDeviceToken, bool isSubscribed, string application)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var user = await Queries.FireBaseUserAsync(dbContext, tenantId, userId, application, fbDeviceToken);

        if (user != null)
        {
            user.IsSubscribed = isSubscribed;
            dbContext.Update(user);
            await dbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task DeleteInvalidTokenAsync(Guid userId, int tenantId, string fbDeviceToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var user = await Queries.DeleteFireBaseUserTokenAsync(dbContext, tenantId, userId, fbDeviceToken);
    }

}

[Scope]
public class CacheFirebaseDao(IDbContextFactory<FirebaseDbContext> dbContextFactory) : FirebaseDao(dbContextFactory)
{
    private readonly ConcurrentDictionary<(int, Guid, string), List<FireBaseUser>> _cache = new();
    public override async Task<List<FireBaseUser>> GetSubscribedUserDeviceTokensAsync(Guid userId, int tenantId, string application)
    {

        if (!_cache.TryGetValue((tenantId, userId, application), out var result))
        {
            result = await base.GetSubscribedUserDeviceTokensAsync(userId, tenantId, application);
            _cache.TryAdd((tenantId, userId, application), result);
        }
        return result;
    }
}

static file class Queries
{
    public static readonly Func<FirebaseDbContext, int, Guid, string, string, Task<FireBaseUser>> FireBaseUserAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FirebaseDbContext ctx, int tenantId, Guid userId, string application, string fbDeviceToken) =>
                ctx.Users.FirstOrDefault(r => r.UserId == userId && r.TenantId == tenantId && r.Application == application && r.FirebaseDeviceToken == fbDeviceToken));

    public static readonly Func<FirebaseDbContext, int, Guid, string, IAsyncEnumerable<FireBaseUser>>
        FireBaseSubscribedUsersAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FirebaseDbContext ctx, int tenantId, Guid userId, string application) =>
                ctx.Users
                    .Where(r => r.UserId == userId)
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.IsSubscribed == true)
                    .Where(r => r.Application == application)
                    .GroupBy(r => r.FirebaseDeviceToken)
                    .Select(g => g.First()));

    public static readonly Func<FirebaseDbContext, int, Guid, string, Task<int>> DeleteFireBaseUserTokenAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (FirebaseDbContext ctx, int tenantId, Guid userId, string fbDeviceToken) =>
                ctx.Users
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.UserId == userId)
                    .Where(r => r.FirebaseDeviceToken == fbDeviceToken)
                    .ExecuteDelete());
}