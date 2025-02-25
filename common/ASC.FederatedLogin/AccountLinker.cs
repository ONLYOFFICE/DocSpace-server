// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.FederatedLogin;

[Singleton]
public class AccountLinkerStorage
{
    private readonly ICache _cache;
    private readonly ICacheNotify<LinkerCacheItem> _notify;

    public AccountLinkerStorage(ICacheNotify<LinkerCacheItem> notify, ICache cache)
    {
        _cache = cache;
        _notify = notify;
        notify.Subscribe(c => cache.Remove(c.Obj), CacheNotifyAction.Remove);
    }

    public async Task RemoveFromCacheAsync(string obj)
    {
        await _notify.PublishAsync(new LinkerCacheItem { Obj = obj }, CacheNotifyAction.Remove);
    }

    public async Task<List<LoginProfile>> GetFromCacheAsync(string obj, Func<string, Task<List<LoginProfile>>> fromDb)
    {
        var profiles = _cache.Get<List<LoginProfile>>(obj);
        if (profiles == null)
        {
            profiles = await fromDb(obj);
            _cache.Insert(obj, profiles, DateTime.UtcNow + TimeSpan.FromMinutes(10));
        }

        return profiles;
    }
}

[Scope]
public class AccountLinker(
    AccountLinkerStorage accountLinkerStorage,
    IDbContextFactory<AccountLinkContext> accountLinkContextManager,
    TenantManager tenantManager)
{
    public async Task<IEnumerable<string>> GetLinkedObjectsByHashIdAsync(string hashId)
    {
        await using var accountLinkContext = await accountLinkContextManager.CreateDbContextAsync();
        return await Queries.LinkedObjectsByHashIdAsync(accountLinkContext, hashId).ToListAsync();
    }

    public async Task<IEnumerable<LoginProfile>> GetLinkedProfilesAsync(string obj, string provider)
    {
        return (await GetLinkedProfilesAsync(obj)).Where(profile => profile.Provider.Equals(provider));
    }

    public async Task<IDictionary<string, LoginProfile>> GetLinkedProfilesAsync(IEnumerable<string> objects, string provider)
    {
        return (await GetLinkedProfilesAsync(objects)).Where(o => o.Value.Provider.Equals(provider)).ToDictionary(k => k.Key, v => v.Value);
    }

    public async Task<List<LoginProfile>> GetLinkedProfilesAsync(string obj)
    {
        return await accountLinkerStorage.GetFromCacheAsync(obj, GetLinkedProfilesFromDBAsync);
    }

    public async Task<IEnumerable<LoginProfile>> GetLinkedProfilesAsync()
    {
        var tenant = tenantManager.GetCurrentTenantId();
        var cacheKey = CacheKey(tenant);
        
        var profiles = await accountLinkerStorage.GetFromCacheAsync(cacheKey, async _ =>
        {
            await using var accountLinkContext = await accountLinkContextManager.CreateDbContextAsync();
            return await Queries.AccountLinksByTenantAsync(accountLinkContext, tenant)
                .Select(x => new LoginProfile(x.Profile, x.Id))
                .ToListAsync();
        });
        
        return profiles;
    }

    public async Task AddLinkAsync(Guid obj, LoginProfile profile)
    {
        var accountLink = new AccountLinks
        {
            Id = obj.ToString(),
            UId = profile.HashId,
            Provider = profile.Provider,
            Profile = profile.ToString(),
            Linked = DateTime.UtcNow
        };

        await using var accountLinkContext = await accountLinkContextManager.CreateDbContextAsync();
        var tenant = tenantManager.GetCurrentTenantId();

        if (await Queries.ExistAccountAsync(accountLinkContext, tenant, profile.HashId))
        {
            throw new Exception("ErrorAccountAlreadyUse");
        }
        await accountLinkContext.AddOrUpdateAsync(a => a.AccountLinks, accountLink);
        await accountLinkContext.SaveChangesAsync();

        await accountLinkerStorage.RemoveFromCacheAsync(obj.ToString());
        await accountLinkerStorage.RemoveFromCacheAsync(CacheKey(tenant));
    }

    public async Task AddLinkAsync(Guid obj, string id, string provider)
    {
        await AddLinkAsync(obj, new LoginProfile { Id = id, Provider = provider });
    }

    public async Task RemoveProviderAsync(string obj, string provider = null, string hashId = null)
    {
        await using var accountLinkContext = await accountLinkContextManager.CreateDbContextAsync();
        var tenant = tenantManager.GetCurrentTenantId();

        var accountLink = await Queries.AccountLinkAsync(accountLinkContext, obj, provider, hashId);

        accountLinkContext.AccountLinks.Remove(accountLink);
        await accountLinkContext.SaveChangesAsync();

        await accountLinkerStorage.RemoveFromCacheAsync(obj);
        await accountLinkerStorage.RemoveFromCacheAsync(CacheKey(tenant));
    }

    private async Task<List<LoginProfile>> GetLinkedProfilesFromDBAsync(string obj)
    {
        await using var accountLinkContext = await accountLinkContextManager.CreateDbContextAsync();
        //Retrieve by unique id
        return (await Queries.LinkedProfilesFromDbAsync(accountLinkContext, obj).ToListAsync())
                .ConvertAll(r => new LoginProfile(r));
    }

    private async Task<IDictionary<string, LoginProfile>> GetLinkedProfilesAsync(IEnumerable<string> objects)
    {
        await using var accountLinkContext = await accountLinkContextManager.CreateDbContextAsync();

        return await accountLinkContext.AccountLinks.Where(r => objects.Contains(r.Id))
            .Select(r => new { r.Id, r.Profile })
            .ToDictionaryAsync(k => k.Id, v =>  new LoginProfile(v.Profile));
    }
    
    private static string CacheKey(int tenantId) => $"tenant_profiles_{tenantId}";
}

static file class Queries
{
    public static readonly Func<AccountLinkContext, string, IAsyncEnumerable<string>> LinkedObjectsByHashIdAsync =
        EF.CompileAsyncQuery(
            (AccountLinkContext ctx, string hashId) =>
                ctx.AccountLinks
                    .Where(r => r.UId == hashId)
                    .Where(r => r.Provider != string.Empty)
                    .Select(r => r.Id));

    public static readonly Func<AccountLinkContext, string, string, string, Task<AccountLinks>> AccountLinkAsync =
        EF.CompileAsyncQuery(
            (AccountLinkContext ctx, string id, string provider, string hashId) =>
                ctx.AccountLinks
                    .Where(r => r.Id == id)
                    .Where(r => string.IsNullOrEmpty(provider) || r.Provider == provider)
                    .FirstOrDefault(r => string.IsNullOrEmpty(hashId) || r.UId == hashId));

    public static readonly Func<AccountLinkContext, string, IAsyncEnumerable<string>> LinkedProfilesFromDbAsync =
        EF.CompileAsyncQuery(
            (AccountLinkContext ctx, string id) =>
                ctx.AccountLinks
                    .Where(r => r.Id == id)
                    .Select(r => r.Profile));

    public static readonly Func<AccountLinkContext, int, string, Task<bool>> ExistAccountAsync =
        EF.CompileAsyncQuery(
            (AccountLinkContext ctx, int tenant, string hashId) =>
                ctx.AccountLinks.Join(ctx.Users, a => a.Id, u => u.Id.ToString(),
                        (accountLink, user) => new { accountLink, user })
                        .Any(q => q.accountLink.UId == hashId && q.user.TenantId == tenant));
    
    public static readonly Func<AccountLinkContext, int, IAsyncEnumerable<AccountLinks>> AccountLinksByTenantAsync =
        EF.CompileAsyncQuery(
            (AccountLinkContext ctx, int tenant) =>
                ctx.AccountLinks.Join(ctx.Users, a => a.Id, u => u.Id.ToString(), 
                        (accountLink, user) => new { accountLink, user })
                        .Where(x => x.user.TenantId == tenant)
                        .Select(x => x.accountLink));
}
