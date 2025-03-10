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

using Constants = ASC.Core.Users.Constants;
using SecurityAction = ASC.Common.Security.Authorizing.Action;

namespace ASC.Web.Core;

[Singleton]
public class WebItemSecurityCache
{
    private readonly ICache _cache;
    private readonly ICacheNotify<WebItemSecurityNotifier> _cacheNotify;

    public WebItemSecurityCache(ICacheNotify<WebItemSecurityNotifier> cacheNotify, ICache cache)
    {
        _cache = cache;
        _cacheNotify = cacheNotify;
        _cacheNotify.Subscribe(r =>
        {
            ClearCache(r.Tenant);
        }, CacheNotifyAction.Any);
    }

    public void ClearCache(int tenantId)
    {
        _cache.Remove(GetCacheKey(tenantId));
    }

    private string GetCacheKey(int tenantId)
    {
        return $"{tenantId}:webitemsecurity";
    }

    public async Task PublishAsync(int tenantId)
    {
        await _cacheNotify.PublishAsync(new WebItemSecurityNotifier { Tenant = tenantId }, CacheNotifyAction.Any);
    }

    public Dictionary<string, bool> Get(int tenantId)
    {
        return _cache.Get<Dictionary<string, bool>>(GetCacheKey(tenantId));
    }

    public Dictionary<string, bool> GetOrInsert(int tenantId)
    {

        var dic = Get(tenantId);
        if (dic == null)
        {
            dic = new Dictionary<string, bool>();
            _cache.Insert(GetCacheKey(tenantId), dic, DateTime.UtcNow.Add(TimeSpan.FromMinutes(1)));
        }

        return dic;
    }
}

[Scope]
public class WebItemSecurity(UserManager userManager,
        AuthContext authContext,
        PermissionContext permissionContext,
        AuthManager authentication,
        WebItemManager webItemManager,
        TenantManager tenantManager,
        AuthorizationManager authorizationManager,
        WebItemSecurityCache webItemSecurityCache,
        SettingsManager settingsManager,
        CountPaidUserChecker countPaidUserChecker, 
        IDistributedLockProvider distributedLockProvider)
    {
    
    private static readonly SecurityAction _read = new(new Guid("77777777-32ae-425f-99b5-83176061d1ae"), "ReadWebItem", false);

    //
    public async Task<bool> IsAvailableForMeAsync(Guid id)
    {
        return await IsAvailableForUserAsync(id, authContext.CurrentAccount.ID);
    }

    public async Task<bool> IsAvailableForUserAsync(Guid itemId, Guid @for)
    {
        var tenant = tenantManager.GetCurrentTenant();

        var id = itemId.ToString();
        bool result;
        var dic = webItemSecurityCache.GetOrInsert(tenant.Id);
        if (dic != null)
        {
            lock (dic)
            {
                if (dic.TryGetValue(id + @for, out var value))
                {
                    return value;
                }
            }
        }

        // can read or administrator
        var securityObj = WebItemSecurityObject.Create(id, webItemManager);

        var webItem = webItemManager[securityObj.WebItemId];
        if (webItem != null)
        {
            if ((
                webItem.ID == WebItemManager.PeopleProductID ||
                webItem.ID == WebItemManager.BirthdaysProductID) &&
                await userManager.IsGuestAsync(@for))
            {
                // hack: people and birthday products not visible for collaborators
                result = false;
            }
            else if (webItem is IModule)
            {
                result =
                    await permissionContext.PermissionResolver.CheckAsync(await authentication.GetAccountByIDAsync(tenant.Id, @for), securityObj, null, _read) &&
                    await IsAvailableForUserAsync(webItemManager.GetParentItemId(webItem.ID), @for);
            }
            else
            {
                var hasUsers = (await authorizationManager.GetAcesAsync(Guid.Empty, _read.ID, securityObj)).Any(a => a.Subject != Constants.GroupEveryone.ID);
                result = await permissionContext.PermissionResolver.CheckAsync(await authentication.GetAccountByIDAsync(tenant.Id, @for), securityObj, null, _read) ||
                         (hasUsers && await IsProductAdministratorAsync(securityObj.WebItemId, @for));
            }
        }
        else
        {
            result = false;
        }

        dic = webItemSecurityCache.Get(tenant.Id);
        if (dic != null)
        {
            lock (dic)
            {
                dic[id + @for] = result;
            }
        }
        return result;
    }

    public async Task SetSecurityAsync(string id, bool enabled, params Guid[] subjects)
    {
        if ((await settingsManager.LoadAsync<TenantAccessSettings>()).Anyone)
        {
            throw new SecurityException("Security settings are disabled for an open portal");
        }

        var securityObj = WebItemSecurityObject.Create(id, webItemManager);

        // remove old aces
        await authorizationManager.RemoveAllAcesAsync(securityObj);
        var allowToAll = new AzRecord(Constants.GroupEveryone.ID, _read.ID, AceType.Allow, securityObj.FullId);
        await authorizationManager.RemoveAceAsync(allowToAll);

        // set new aces
        if (subjects == null || subjects.Length == 0 || subjects.Contains(Constants.GroupEveryone.ID))
        {
            if (!enabled && subjects is { Length: 0 })
            {
                // users from list with no users equals allow to all users
                enabled = true;
            }
            subjects = [Constants.GroupEveryone.ID];
        }
        foreach (var s in subjects)
        {
            var a = new AzRecord(s, _read.ID, enabled ? AceType.Allow : AceType.Deny, securityObj.FullId);
            await authorizationManager.AddAceAsync(a);
        }

        await webItemSecurityCache.PublishAsync(tenantManager.GetCurrentTenantId());
    }

    public async Task<WebItemSecurityInfo> GetSecurityInfoAsync(string id)
    {
        var info = (await GetSecurityAsync(id)).ToList();
        var module = webItemManager.GetParentItemId(new Guid(id)) != Guid.Empty;
        
        return new WebItemSecurityInfo
        {
            WebItemId = id,

            Enabled = info.Count == 0 || (!module && info.Exists(i => i.Item2)) || (module && info.TrueForAll(i => i.Item2)),

            Users = await info
                           .ToAsyncEnumerable()
                           .SelectAwait(async i => await userManager.GetUsersAsync(i.Item1))
                           .Where(u => u.Id != Constants.LostUser.Id).ToListAsync(),

            Groups = await info
                           .ToAsyncEnumerable()
                           .SelectAwait(async i => await userManager.GetGroupInfoAsync(i.Item1))
                           .Where(g => g.ID != Constants.LostGroupInfo.ID && g.CategoryID != Constants.SysGroupCategoryId).ToListAsync()
        };
    }


    private async Task<IEnumerable<Tuple<Guid, bool>>> GetSecurityAsync(string id)
    {
        var securityObj = WebItemSecurityObject.Create(id, webItemManager);
        var result = (await authorizationManager
            .GetAcesWithInheritsAsync(Guid.Empty, _read.ID, securityObj, null))
            .GroupBy(a => a.Subject)
            .Select(a => Tuple.Create(a.Key, a.First().AceType == AceType.Allow))
            .ToList();
        if (result.Count == 0)
        {
            result.Add(Tuple.Create(Constants.GroupEveryone.ID, false));
        }
        return result;
    }

    public async Task SetProductAdministrator(Guid productId, Guid userid, bool administrator)
    {
        if (productId == Guid.Empty)
        {
            productId = Constants.GroupAdmin.ID;
        }

        if (administrator)
        {
            var tenantId = tenantManager.GetCurrentTenantId();

            await using (await distributedLockProvider.TryAcquireFairLockAsync(LockKeyHelper.GetPaidUsersCountCheckKey(tenantId)))
            {
                var type = await userManager.GetUserTypeAsync(userid);
                switch (type)
                {
                    case EmployeeType.Guest:
                        await countPaidUserChecker.CheckAppend();
                        await userManager.RemoveUserFromGroupAsync(userid, Constants.GroupGuest.ID);
                        break;
                    case EmployeeType.User:
                        await countPaidUserChecker.CheckAppend();
                        await userManager.RemoveUserFromGroupAsync(userid, Constants.GroupUser.ID);
                        break;
                }

                if (productId == WebItemManager.PeopleProductID)
                {
                    foreach (var ace in GetPeopleModuleActions(userid))
                    {
                        await authorizationManager.AddAceAsync(ace);
                    }
                }

                await userManager.AddUserIntoGroupAsync(userid, productId);
            }
        }
        else
        {
            if (productId == Constants.GroupAdmin.ID)
            {
                var groups = new List<Guid>();
                groups.AddRange(webItemManager.GetItemsAll().OfType<IProduct>().Select(p => p.ID));

                foreach (var id in groups)
                {
                    await userManager.RemoveUserFromGroupAsync(userid, id);
                }
            }

            if (productId == Constants.GroupAdmin.ID || productId == WebItemManager.PeopleProductID)
            {
                foreach (var ace in GetPeopleModuleActions(userid))
                {
                    await authorizationManager.RemoveAceAsync(ace);
                }
            }

            await userManager.RemoveUserFromGroupAsync(userid, productId);
        }

        await webItemSecurityCache.PublishAsync(tenantManager.GetCurrentTenantId());
    }

    public async Task<bool> IsProductAdministratorAsync(Guid productId, Guid userid)
    {
        return await userManager.IsUserInGroupAsync(userid, Constants.GroupAdmin.ID) ||
               await userManager.IsUserInGroupAsync(userid, productId);
    }

    public async Task<IEnumerable<UserInfo>> GetProductAdministratorsAsync(Guid productId)
    {
        var groups = new List<Guid>();
        if (productId == Guid.Empty)
        {
            groups.Add(Constants.GroupAdmin.ID);
            groups.AddRange(webItemManager.GetItemsAll().OfType<IProduct>().Select(p => p.ID));
        }
        else
        {
            groups.Add(productId);
        }

        var users = new List<UserInfo>();
        foreach (var id in groups)
        {
            users.AddRange(await userManager.GetUsersByGroupAsync(id));
        }
        return users;
    }

    private static IEnumerable<AzRecord> GetPeopleModuleActions(Guid userid)
    {
        return new List<Guid>
                {
                    Constants.Action_AddRemoveUser.ID,
                    Constants.Action_EditUser.ID,
                    Constants.Action_EditGroups.ID
                }.Select(action => new AzRecord(userid, action, AceType.Allow));
    }

    private sealed class WebItemSecurityObject : ISecurityObject
    {
        public Guid WebItemId { get; }
        private readonly WebItemManager _webItemManager;

        public Type ObjectType
        {
            get { return GetType(); }
        }

        public object SecurityId
        {
            get { return WebItemId.ToString("N"); }
        }

        public string FullId => AzObjectIdHelper.GetFullObjectId(this);

        public bool InheritSupported
        {
            get { return true; }
        }

        public bool ObjectRolesSupported
        {
            get { return false; }
        }

        public static WebItemSecurityObject Create(string id, WebItemManager webItemManager)
        {
            ArgumentException.ThrowIfNullOrEmpty(id);

            var itemId = Guid.Empty;
            if (32 <= id.Length)
            {
                itemId = new Guid(id);
            }
            else
            {
                var w = webItemManager
                    .GetItemsAll()
                    .Find(i => id.Equals(i.GetSysName(), StringComparison.InvariantCultureIgnoreCase));
                if (w != null)
                {
                    itemId = w.ID;
                }
            }
            return new WebItemSecurityObject(itemId, webItemManager);
        }


        private WebItemSecurityObject(Guid itemId, WebItemManager webItemManager)
        {
            WebItemId = itemId;
            _webItemManager = webItemManager;
        }

        public ISecurityObjectId InheritFrom(ISecurityObjectId objectId)
        {
            if (objectId is WebItemSecurityObject s)
            {
                return Create(_webItemManager.GetParentItemId(s.WebItemId).ToString("N"), _webItemManager) is WebItemSecurityObject parent && parent.WebItemId != s.WebItemId && parent.WebItemId != Guid.Empty ? parent : null;
            }
            return null;
        }

        public IEnumerable<IRole> GetObjectRoles(ISubject account, ISecurityObjectId objectId, SecurityCallContext callContext)
        {
            throw new NotImplementedException();
        }
    }
}
