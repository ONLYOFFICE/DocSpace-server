// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Files.Core.Core.Thirdparty;

internal abstract class AbstractProviderInfo<TFile, TFolder, TItem, TProvider>(DisposableWrapper wrapper,
        ProviderInfoHelper providerInfoHelper)
    : IProviderInfo<TFile, TFolder, TItem>
    where TFile : class, TItem
    where TFolder : class, TItem
    where TItem : class
    where TProvider : Consumer, IOAuthProvider, new()
{
    public abstract Selector Selector { get; }
    public abstract ProviderFilter ProviderFilter { get; }
    public virtual bool MutableEntityId => false;

    internal readonly ProviderInfoHelper ProviderInfoHelper = providerInfoHelper;

    public DateTime CreateOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    public string CustomerTitle { get; set; }
    public string FolderId { get; set; }
    public FolderType FolderType { get; set; }
    public bool HasLogo { get; set; }
    public int ProviderId { get; set; }
    public Guid Owner { get; set; }
    public bool Private { get; set; }
    public string ProviderKey { get; set; }
    public string RootFolderId => $"{Selector.Id}-" + ProviderId;
    public FolderType RootFolderType { get; set; }
    public AuthData AuthData { get; set; }
    public string Color { get; set; }
    private bool StorageOpened => wrapper.TryGetStorage(ProviderId, out var storage) && storage.IsOpened;

    public Task<IThirdPartyStorage<TFile, TFolder, TItem>> StorageAsync
    {
        get
        {
            if (!wrapper.TryGetStorage<IThirdPartyStorage<TFile, TFolder, TItem>>(ProviderId, out var storage) || !storage.IsOpened)
            {
                return wrapper.CreateStorageAsync<IThirdPartyStorage<TFile, TFolder, TItem>, TProvider>(AuthData, ProviderId);
            }

            return Task.FromResult(storage);
        }
    }

    public async Task<bool> CheckAccessAsync()
    {
        var storage = await StorageAsync;

        return await storage.CheckAccessAsync();
    }

    public void Dispose()
    {
        if (StorageOpened)
        {
            StorageAsync.Result.Close();
        }
    }

    public Task InvalidateStorageAsync()
    {
        wrapper?.Dispose();

        return CacheResetAsync();
    }

    public void UpdateTitle(string newtitle)
    {
        CustomerTitle = newtitle;
    }

    public Task CacheResetAsync(string id = null, bool? isFile = null)
    {
        return ProviderInfoHelper.CacheResetAsync(Selector.Id, ProviderId, id, isFile);
    }

    public async Task<TFile> GetFileAsync(string fileId)
    {
        var storage = await StorageAsync;

        return await ProviderInfoHelper.GetFileAsync(storage, ProviderId, fileId, Selector.Id);
    }

    public async Task<TFolder> CreateFolderAsync(string title, string folderId, Func<TFolder, string> idSelector)
    {
        var storage = await StorageAsync;

        return await ProviderInfoHelper.CreateFolderAsync(storage, ProviderId, title, folderId, Selector.Id, idSelector);
    }

    public async Task<TFolder> GetFolderAsync(string folderId)
    {
        var storage = await StorageAsync;

        return await ProviderInfoHelper.GetFolderAsync(storage, ProviderId, folderId, Selector.Id);
    }

    public async Task<List<TItem>> GetItemsAsync(string folderId, Func<TItem, string> idSelector, Func<TItem, bool> isFile)
    {
        var storage = await StorageAsync;

        return await ProviderInfoHelper.GetItemsAsync(storage, ProviderId, folderId, Selector.Id, idSelector, isFile);
    }
}

[Singleton]
public class ProviderInfoHelper(IFusionCacheProvider cacheProvider)
{
    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(1);

    internal async Task CacheResetAsync(string selector, int thirdId, string id = null, bool? isFile = null)
    {
        if (id == null)
        {
            await _cache.RemoveByTagAsync(CacheExtention.GetProviderTag(selector, thirdId));
        }
        else
        {
            if (isFile.HasValue)
            {
                if (isFile.Value)
                {
                    await _cache.RemoveByTagAsync(CacheExtention.GetProviderFileTag(selector, thirdId, id));
                }
                else
                {
                    await _cache.RemoveByTagAsync(CacheExtention.GetProviderFolderTag(selector, thirdId, id));
                }
            }
            else
            {
                await _cache.RemoveByTagAsync(CacheExtention.GetProviderFolderItemsTag(selector, thirdId, id));
            }
        }
    }

    internal async ValueTask<TFile> GetFileAsync<TFile>(IThirdPartyFileStorage<TFile> storage, int id, string fileId, string selector) where TFile : class
    {
        var key = $"{selector}f-" + id + "-" + fileId;
        var file = await _cache.GetOrSetAsync<TFile>(key, async (ctx, token) =>
        {
            var file = await storage.GetFileAsync(fileId);

            return ctx.Modified(file);
        }, _cacheExpiration,
        [CacheExtention.GetProviderTag(selector, id), CacheExtention.GetProviderFileTag(selector, id, fileId)]);

        return file;
    }

    internal async Task<TFolder> CreateFolderAsync<TFolder>(IThirdPartyFolderStorage<TFolder> storage, int id, string title, string folderId, string selector,
        Func<TFolder, string> idSelector) where TFolder : class
    {
        var folder = await storage.CreateFolderAsync(title, folderId);
        if (folder == null)
        {
            return null;
        }

        var key = $"{selector}d-" + id + "-" + idSelector(folder);
        await _cache.SetAsync(key, folder, _cacheExpiration,
            [CacheExtention.GetProviderTag(selector, id), CacheExtention.GetProviderFolderTag(selector, id, idSelector(folder))]);

        return folder;
    }

    internal async Task<TFolder> GetFolderAsync<TFolder>(IThirdPartyFolderStorage<TFolder> storage, int id, string folderId, string selector) where TFolder : class
    {
        var key = $"{selector}d-" + id + "-" + folderId;

        var folder = await _cache.GetOrSetAsync<TFolder>(key, async (ctx, token) =>
        {
            var folder = await storage.GetFolderAsync(folderId);

            return ctx.Modified(folder);
        }, _cacheExpiration,
        [CacheExtention.GetProviderTag(selector, id), CacheExtention.GetProviderFolderTag(selector, id, folderId)]);

        return folder;
    }

    internal async Task<List<TItem>> GetItemsAsync<TItem>(IThirdPartyItemStorage<TItem> storage, int id, string folderId, string selector,
        Func<TItem, string> idSelector, Func<TItem, bool> isFile, bool? folder = null) where TItem : class
    {
        var key = $"{selector}-" + id + "-" + folderId;

        if (folder.HasValue)
        {
            key += folder.Value ? "-d" : "-f";
        }

        var items = await _cache.GetOrSetAsync<List<TItem>>(key, async (ctx, token) =>
        {
            List<TItem> items = null;
            if (folder != null && storage is IGoogleDriveItemStorage<TItem> googleStorage)
            {
                items = await googleStorage.GetItemsAsync(folderId, folder);
            }
            else
            {
                items = await storage.GetItemsAsync(folderId);
            }

            var tags = new List<string>
            {
                CacheExtention.GetProviderTag(selector, id),
                CacheExtention.GetProviderFolderTag(selector, id, folderId),
                CacheExtention.GetProviderFolderItemsTag(selector, id, folderId),
            };

            foreach (var item in items)
            {
                if (isFile(item))
                {
                    tags.Add(CacheExtention.GetProviderFileTag(selector, id, idSelector(item)));
                }
                else
                {
                    tags.Add(CacheExtention.GetProviderFolderTag(selector, id, idSelector(item)));
                }
            }

            ctx.Tags = tags.ToArray();
            return ctx.Modified(items);
        });

        return items;
    }
}

[Transient]
public class DisposableWrapper(IServiceProvider serviceProvider, OAuth20TokenHelper oAuth20TokenHelper)
    : IDisposable
{
    private readonly ConcurrentDictionary<int, IThirdPartyStorage> _storages = new();

    public void Dispose()
    {
        foreach (var (key, storage) in _storages)
        {
            storage.Close();
            _storages.Remove(key, out _);
        }
    }

    internal Task<T> CreateStorageAsync<T, T1>(AuthData authData, int id)
        where T : IThirdPartyStorage
        where T1 : Consumer, IOAuthProvider, new()
    {
        if (TryGetStorage<T>(id, out var storage) && storage.IsOpened)
        {
            return Task.FromResult(storage);
        }

        return InternalCreateStorageAsync<T, T1>(authData, id);
    }

    internal bool TryGetStorage<T>(int providerId, out T storage)
    {
        var result = _storages.TryGetValue(providerId, out var s);
        storage = (T)s;
        return result;
    }

    internal bool TryGetStorage(int providerId, out IThirdPartyStorage storage)
    {
        return _storages.TryGetValue(providerId, out storage);
    }

    private async ValueTask<OAuth20Token> CheckTokenAsync<T>(OAuth20Token token, int id) where T : Consumer, IOAuthProvider, new()
    {
        if (token == null)
        {
            throw new UnauthorizedAccessException("Cannot create third party session with given token");
        }

        if (!token.IsExpired)
        {
            return token;
        }

        token = oAuth20TokenHelper.RefreshToken<T>(token);

        var dbDao = serviceProvider.GetService<ProviderAccountDao>();
        await dbDao.UpdateProviderInfoAsync(id, new AuthData(token: token.ToJson()));

        return token;
    }

    private async Task<T> InternalCreateStorageAsync<T, T1>(AuthData authData, int id)
        where T : IThirdPartyStorage
        where T1 : Consumer, IOAuthProvider, new()
    {
        var storage = serviceProvider.GetService<T>();

        if (storage.AuthScheme == AuthScheme.OAuth)
        {
            authData.Token = await CheckTokenAsync<T1>(authData.Token, id);
        }

        storage.Open(authData);

        _storages.TryAdd(id, storage);

        return storage;
    }
}