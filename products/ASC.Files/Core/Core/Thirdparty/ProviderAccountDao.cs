// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Files.Thirdparty;

[EnumExtensions]
public enum ProviderTypes
{
    Box,
    BoxNet,
    DropboxV2,
    Google,
    GoogleDrive,
    OneDrive,
    SharePoint,
    WebDav,
    kDrive,
    Yandex
}

[Scope]
internal class ProviderAccountDao(IServiceProvider serviceProvider,
        TenantUtil tenantUtil,
        TenantManager tenantManager,
        InstanceCrypto instanceCrypto,
        SecurityContext securityContext,
        ConsumerFactory consumerFactory,
        IDbContextFactory<FilesDbContext> dbContextFactory,
        OAuth20TokenHelper oAuth20TokenHelper,
        ILoggerProvider options)
    : IProviderDao
{
    private int TenantID
    {
        get
        {
            return tenantManager.GetCurrentTenant().Id;
        }
    }

    private readonly ILogger _logger = options.CreateLogger("ASC.Files");

    public virtual Task<IProviderInfo> GetProviderInfoAsync(int linkId)
    {
        var providersInfo = GetProvidersInfoInternalAsync(linkId);

        return providersInfo.SingleAsync().AsTask();
    }

    public async Task<IProviderInfo> GetProviderInfoByEntryIdAsync(string entryId)
    {
        try
        {
            var id = Selectors.Pattern.Match(entryId).Groups["id"].Value;
            return await GetProviderInfoAsync(int.Parse(id));
        }
        catch
        {
            return null;
        }
    }

    public virtual IAsyncEnumerable<IProviderInfo> GetProvidersInfoAsync()
    {
        return GetProvidersInfoInternalAsync();
    }

    public virtual IAsyncEnumerable<IProviderInfo> GetProvidersInfoAsync(FolderType folderType, string searchText = null)
    {
        return GetProvidersInfoInternalAsync(folderType: folderType, searchText: searchText);
    }

    public virtual IAsyncEnumerable<IProviderInfo> GetProvidersInfoAsync(Guid userId)
    {
        try
        {
            var filesDbContext = dbContextFactory.CreateDbContext();
            var thirdPartyAccounts = Queries.ThirdPartyAccountsAsync(filesDbContext, TenantID, userId);

            return thirdPartyAccounts.Select(ToProviderInfo);
        }
        catch (Exception e)
        {
            _logger.ErrorGetProvidersInfoInternalUser(userId, e);

            return new List<IProviderInfo>().ToAsyncEnumerable();
        }
    }

    private IAsyncEnumerable<IProviderInfo> GetProvidersInfoInternalAsync(int linkId = -1, FolderType folderType = FolderType.DEFAULT, string searchText = null)
    {
        try
        {
            var filesDbContext = dbContextFactory.CreateDbContext();
            return Queries.ThirdPartyAccountsByFilterAsync(filesDbContext, TenantID, linkId, folderType, securityContext.CurrentAccount.ID, GetSearchText(searchText))
                .Select(ToProviderInfo);
        }
        catch (Exception e)
        {
            _logger.ErrorGetProvidersInfoInternal(linkId, folderType, securityContext.CurrentAccount.ID, e);
            return new List<IProviderInfo>().ToAsyncEnumerable();
        }
    }

    public virtual async Task<int> SaveProviderInfoAsync(string providerKey, string customerTitle, AuthData authData, FolderType folderType)
    {
        ProviderTypes prKey;
        try
        {
            prKey = (ProviderTypes)Enum.Parse(typeof(ProviderTypes), providerKey, true);
        }
        catch (Exception)
        {
            throw new ArgumentException("Unrecognize ProviderType");
        }

        authData = GetEncodedAccessToken(authData, prKey);

        if (!await CheckProviderInfoAsync(ToProviderInfo(0, prKey, customerTitle, authData, securityContext.CurrentAccount.ID, folderType, tenantUtil.DateTimeToUtc(tenantUtil.DateTimeNow()))))
        {
            throw new UnauthorizedAccessException(string.Format(FilesCommonResource.ErrorMessage_SecurityException_Auth, providerKey));
        }

        var now = tenantUtil.DateTimeToUtc(tenantUtil.DateTimeNow());

        var dbFilesThirdpartyAccount = new DbFilesThirdpartyAccount
        {
            Id = 0,
            TenantId = TenantID,
            Provider = providerKey,
            Title = Global.ReplaceInvalidCharsAndTruncate(customerTitle),
            UserName = authData.Login ?? "",
            Password = EncryptPassword(authData.Password),
            FolderType = folderType,
            CreateOn = now,
            ModifiedOn = now,
            UserId = securityContext.CurrentAccount.ID,
            Token = EncryptPassword(authData.RawToken ?? ""),
            Url = authData.Url ?? ""
        };

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var res = await filesDbContext.AddOrUpdateAsync(r => r.ThirdpartyAccount, dbFilesThirdpartyAccount);
        await filesDbContext.SaveChangesAsync();

        return res.Id;
    }

    public async Task<bool> CheckProviderInfoAsync(IProviderInfo providerInfo)
    {
        return providerInfo != null && await providerInfo.CheckAccessAsync();
    }
    
    public async Task<IProviderInfo> UpdateRoomProviderInfoAsync(ProviderData data)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var forUpdate = await Queries.ThirdPartyAccountAsync(filesDbContext, TenantID, data.Id);

        if (forUpdate == null)
        {
            return null;
        }

        if (data.AuthData != null && !data.AuthData.IsEmpty())
        {
            ProviderTypesExtensions.TryParse(forUpdate.Provider, true, out var key);
            var updatedAuthData = GetEncodedAccessToken(data.AuthData, key);
            updatedAuthData.Url = forUpdate.Url;
            
            if (!await CheckProviderInfoAsync(ToProviderInfo(0, key, forUpdate.Title, updatedAuthData, securityContext.CurrentAccount.ID, forUpdate.FolderType, 
                    tenantUtil.DateTimeToUtc(tenantUtil.DateTimeNow()))))
            {
                throw new UnauthorizedAccessException(string.Format(FilesCommonResource.ErrorMessage_SecurityException_Auth, key));
            }
            
            forUpdate.UserName = updatedAuthData.Login ?? string.Empty;
            forUpdate.Password = EncryptPassword(updatedAuthData.Password);
            forUpdate.Token = EncryptPassword(updatedAuthData.RawToken);
        }

        if (!string.IsNullOrEmpty(data.Title))
        {
            forUpdate.Title = data.Title;
        }

        if (!string.IsNullOrEmpty(data.FolderId))
        {
            forUpdate.FolderId = data.FolderId;
        }

        if (data.FolderType.HasValue)
        {
            forUpdate.RoomType = data.FolderType.Value;
        }

        if (data.RootFolderType.HasValue)
        {
            forUpdate.FolderType = data.RootFolderType.Value;
        }

        if (data.Private.HasValue)
        {
            forUpdate.Private = data.Private.Value;
        }

        if (data.HasLogo.HasValue)
        {
            forUpdate.HasLogo = data.HasLogo.Value;
        }

        if (!string.IsNullOrEmpty(data.Color))
        {
            forUpdate.Color = data.Color;
        }

        forUpdate.ModifiedOn = DateTime.UtcNow;
        
        filesDbContext.Update(forUpdate);
        await filesDbContext.SaveChangesAsync();
        
        return ToProviderInfo(forUpdate);
    }

    public virtual async Task<int> UpdateProviderInfoAsync(int linkId, AuthData authData)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var tenantId = TenantID;
        var login = authData.Login ?? "";
        var password = EncryptPassword(authData.Password);
        var token = EncryptPassword(authData.RawToken ?? "");
        var url = authData.Url ?? "";

        var forUpdateCount = await Queries.UpdateThirdPartyAccountsAsync(filesDbContext, tenantId, linkId, login, password, token, url);

        return forUpdateCount == 1 ? linkId : default;
    }

    public virtual async Task<int> UpdateProviderInfoAsync(int linkId, string customerTitle, AuthData newAuthData, FolderType folderType, Guid? userId = null)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        var authData = new AuthData();
        if (newAuthData != null && !newAuthData.IsEmpty())
        {
            DbFilesThirdpartyAccount input;
            try
            {
                input = await Queries.ThirdPartyAccountByLinkIdAsync(filesDbContext, TenantID, linkId);
            }
            catch (Exception e)
            {
                _logger.ErrorUpdateProviderInfo(linkId, securityContext.CurrentAccount.ID, e);
                throw;
            }

            if (!ProviderTypesExtensions.TryParse(input.Provider, true, out var key))
            {
                throw new ArgumentException("Unrecognize ProviderType");
            }

            authData = new AuthData(
                !string.IsNullOrEmpty(newAuthData.Url) ? newAuthData.Url : input.Url,
                input.UserName,
                !string.IsNullOrEmpty(newAuthData.Password) ? newAuthData.Password : DecryptPassword(input.Password, linkId),
                newAuthData.RawToken);

            if (!string.IsNullOrEmpty(newAuthData.RawToken))
            {
                authData = GetEncodedAccessToken(authData, key);
            }

            if (!await CheckProviderInfoAsync(ToProviderInfo(0, key, customerTitle, authData, securityContext.CurrentAccount.ID, folderType, tenantUtil.DateTimeToUtc(tenantUtil.DateTimeNow()))))
            {
                throw new UnauthorizedAccessException(string.Format(FilesCommonResource.ErrorMessage_SecurityException_Auth, key));
            }
        }

        var toUpdate = Queries.ThirdPartyAccountsByLinkIdAsync(filesDbContext, TenantID, linkId);
        var toUpdateCount = 0;

        await foreach (var t in toUpdate)
        {
            if (!string.IsNullOrEmpty(customerTitle))
            {
                t.Title = customerTitle;
            }

            if (folderType != FolderType.DEFAULT)
            {
                t.FolderType = folderType;
            }

            if (userId.HasValue)
            {
                t.UserId = userId.Value;
            }

            if (!authData.IsEmpty())
            {
                t.UserName = authData.Login ?? "";
                t.Password = EncryptPassword(authData.Password);
                t.Token = EncryptPassword(authData.RawToken ?? "");
                t.Url = authData.Url ?? "";
            }
            
            t.ModifiedOn = DateTime.UtcNow;

            toUpdateCount++;
        }

        await filesDbContext.SaveChangesAsync();

        return toUpdateCount == 1 ? linkId : default;
    }

    public virtual async Task<int> UpdateBackupProviderInfoAsync(string providerKey, string customerTitle, AuthData newAuthData)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        DbFilesThirdpartyAccount thirdParty;
        try
        {
            thirdParty = await Queries.ThirdPartyBackupAccountAsync(filesDbContext, TenantID);
        }
        catch (Exception e)
        {
            _logger.ErrorUpdateBackupProviderInfo(securityContext.CurrentAccount.ID, e);
            throw;
        }

        if (!ProviderTypesExtensions.TryParse(providerKey, true, out var key))
        {
            throw new ArgumentException("Unrecognize ProviderType");
        }

        if (newAuthData != null && !newAuthData.IsEmpty())
        {
            if (!string.IsNullOrEmpty(newAuthData.RawToken))
            {
                newAuthData = GetEncodedAccessToken(newAuthData, key);
            }

            if (!await CheckProviderInfoAsync(ToProviderInfo(0, key, customerTitle, newAuthData, securityContext.CurrentAccount.ID, FolderType.ThirdpartyBackup, tenantUtil.DateTimeToUtc(tenantUtil.DateTimeNow()))).ConfigureAwait(false))
            {
                throw new UnauthorizedAccessException(string.Format(FilesCommonResource.ErrorMessage_SecurityException_Auth, key));
            }
        }

        if (!string.IsNullOrEmpty(customerTitle))
        {
            thirdParty.Title = customerTitle;
        }

        thirdParty.UserId = securityContext.CurrentAccount.ID;
        thirdParty.Provider = providerKey;

        if (newAuthData != null && !newAuthData.IsEmpty())
        {
            thirdParty.UserName = newAuthData.Login ?? "";
            thirdParty.Password = EncryptPassword(newAuthData.Password);
            thirdParty.Token = EncryptPassword(newAuthData.RawToken ?? "");
            thirdParty.Url = newAuthData.Url ?? "";
        }
        
        thirdParty.ModifiedOn = DateTime.UtcNow;
        
        filesDbContext.Update(thirdParty);
        await filesDbContext.SaveChangesAsync();

        return thirdParty.Id;
    }

    public virtual async Task RemoveProviderInfoAsync(int linkId)
    {
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = filesDbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            await using var tr = await dbContext.Database.BeginTransactionAsync();

            var folderId = (await GetProviderInfoAsync(linkId)).RootFolderId;
            var entryIDs = await Queries.HashIdsAsync(dbContext, TenantID, folderId).ToListAsync();

            await Queries.DeleteDbFilesSecuritiesAsync(dbContext, TenantID, entryIDs);
            await Queries.DeleteDbFilesTagLinksAsync(dbContext, TenantID, entryIDs);
            await Queries.DeleteThirdPartyAccountsByLinkIdAsync(dbContext, TenantID, linkId);

            await tr.CommitAsync();
        });
    }

    private IProviderInfo ToProviderInfo(int id, ProviderTypes providerKey, string customerTitle, AuthData authData, Guid owner, FolderType type, DateTime createOn)
    {
        var dbFilesThirdPartyAccount = new DbFilesThirdpartyAccount
        {
            Id = id,
            Title = customerTitle,
            Token = EncryptPassword(authData.RawToken),
            Url = authData.Url,
            UserName = authData.Login,
            Password = EncryptPassword(authData.Password),
            UserId = owner,
            FolderType = type,
            CreateOn = createOn,
            Provider = providerKey.ToString()
        };

        return ToProviderInfo(dbFilesThirdPartyAccount);
    }

    public IProviderInfo ToProviderInfo(DbFilesThirdpartyAccount input)
    {
        if (!ProviderTypesExtensions.TryParse(input.Provider, true, out var key))
        {
            return null;
        }

        var id = input.Id;
        var providerTitle = input.Title ?? string.Empty;
        var token = DecryptToken(input.Token, id);
        var owner = input.UserId;
        var rootFolderType = input.FolderType;
        var folderType = input.RoomType;
        var privateRoom = input.Private;
        var folderId = input.FolderId;
        var createOn = tenantUtil.DateTimeFromUtc(input.CreateOn);
        var modifiedOn = tenantUtil.DateTimeFromUtc(input.ModifiedOn);
        var authData = new AuthData(input.Url, input.UserName, DecryptPassword(input.Password, id), token, input.Provider);
        var hasLogo = input.HasLogo;
        var color = input.Color;

        if (key == ProviderTypes.kDrive)
        {
            authData.Url = "https://connect.drive.infomaniak.com";
        }

        if (key == ProviderTypes.Box)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token can't be null");
            }

            var box = serviceProvider.GetService<BoxProviderInfo>();
            box.ProviderId = id;
            box.CustomerTitle = providerTitle;
            box.Owner = owner == Guid.Empty ? securityContext.CurrentAccount.ID : owner;
            box.ProviderKey = input.Provider;
            box.RootFolderType = rootFolderType;
            box.CreateOn = createOn;
            box.ModifiedOn = modifiedOn;
            box.AuthData = authData;
            box.FolderType = folderType;
            box.FolderId = folderId;
            box.Private = privateRoom;
            box.HasLogo = hasLogo;
            box.Color = color;

            return box;
        }

        if (key == ProviderTypes.DropboxV2)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token can't be null");
            }

            var drop = serviceProvider.GetService<DropboxProviderInfo>();
            drop.ProviderId = id;
            drop.CustomerTitle = providerTitle;
            drop.Owner = owner == Guid.Empty ? securityContext.CurrentAccount.ID : owner;
            drop.ProviderKey = input.Provider;
            drop.RootFolderType = rootFolderType;
            drop.CreateOn = createOn;
            drop.ModifiedOn = modifiedOn;
            drop.AuthData = authData;
            drop.FolderType = folderType;
            drop.FolderId = folderId;
            drop.Private = privateRoom;
            drop.HasLogo = hasLogo;
            drop.Color = color;

            return drop;
        }

        if (key == ProviderTypes.SharePoint)
        {
            if (!string.IsNullOrEmpty(authData.Login) && string.IsNullOrEmpty(authData.Password))
            {
                throw new ArgumentNullException("password", "Password can't be null");
            }

            var sh = serviceProvider.GetService<SharePointProviderInfo>();
            sh.ProviderId = id;
            sh.CustomerTitle = providerTitle;
            sh.Owner = owner == Guid.Empty ? securityContext.CurrentAccount.ID : owner;
            sh.ProviderKey = input.Provider;
            sh.RootFolderType = rootFolderType;
            sh.CreateOn = createOn;
            sh.ModifiedOn = modifiedOn;
            sh.InitClientContext(authData);
            sh.FolderType = folderType;
            sh.FolderId = folderId;
            sh.Private = privateRoom;
            sh.HasLogo = hasLogo;
            sh.Color = color;

            return sh;
        }

        if (key == ProviderTypes.GoogleDrive)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token can't be null");
            }

            var gd = serviceProvider.GetService<GoogleDriveProviderInfo>();
            gd.ProviderId = id;
            gd.CustomerTitle = providerTitle;
            gd.Owner = owner == Guid.Empty ? securityContext.CurrentAccount.ID : owner;
            gd.ProviderKey = input.Provider;
            gd.RootFolderType = rootFolderType;
            gd.CreateOn = createOn;
            gd.ModifiedOn = modifiedOn;
            gd.AuthData = authData;
            gd.FolderType = folderType;
            gd.FolderId = folderId;
            gd.Private = privateRoom;
            gd.HasLogo = hasLogo;
            gd.Color = color;

            return gd;
        }

        if (key == ProviderTypes.OneDrive)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token can't be null");
            }

            var od = serviceProvider.GetService<OneDriveProviderInfo>();
            od.ProviderId = id;
            od.CustomerTitle = providerTitle;
            od.Owner = owner == Guid.Empty ? securityContext.CurrentAccount.ID : owner;
            od.ProviderKey = input.Provider;
            od.RootFolderType = rootFolderType;
            od.CreateOn = createOn;
            od.ModifiedOn = modifiedOn;
            od.AuthData = authData;
            od.FolderType = folderType;
            od.FolderId = folderId;
            od.Private = privateRoom;
            od.HasLogo = hasLogo;
            od.Color = color;

            return od;
        }

        if (string.IsNullOrEmpty(input.Provider))
        {
            throw new ArgumentNullException("providerKey");
        }

        if (string.IsNullOrEmpty(authData.RawToken) && string.IsNullOrEmpty(authData.Password))
        {
            throw new ArgumentNullException("token", "Both token and password can't be null");
        }

        if (!string.IsNullOrEmpty(authData.Login) && string.IsNullOrEmpty(authData.Password) && string.IsNullOrEmpty(authData.RawToken))
        {
            throw new ArgumentNullException("password", "Password can't be null");
        }

        var webDavProviderInfo = serviceProvider.GetService<WebDavProviderInfo>();
        webDavProviderInfo.ProviderId = id;
        webDavProviderInfo.CustomerTitle = providerTitle;
        webDavProviderInfo.Owner = owner == Guid.Empty ? securityContext.CurrentAccount.ID : owner;
        webDavProviderInfo.ProviderKey = input.Provider;
        webDavProviderInfo.RootFolderType = rootFolderType;
        webDavProviderInfo.CreateOn = createOn;
        webDavProviderInfo.ModifiedOn = modifiedOn;
        webDavProviderInfo.AuthData = authData;
        webDavProviderInfo.FolderType = folderType;
        webDavProviderInfo.FolderId = folderId;
        webDavProviderInfo.Private = privateRoom;
        webDavProviderInfo.HasLogo = hasLogo;
        webDavProviderInfo.Color = color;

        return webDavProviderInfo;
    }

    private AuthData GetEncodedAccessToken(AuthData authData, ProviderTypes provider)
    {
        string code;
        OAuth20Token token;

        switch (provider)
        {
            case ProviderTypes.GoogleDrive:
                code = authData.RawToken;
                token = oAuth20TokenHelper.GetAccessToken<GoogleLoginProvider>(consumerFactory, code);

                if (token == null)
                {
                    throw new UnauthorizedAccessException(string.Format(FilesCommonResource.ErrorMessage_SecurityException_Auth, provider));
                }

                return new AuthData(token: token.ToJson());

            case ProviderTypes.Box:
                code = authData.RawToken;
                token = oAuth20TokenHelper.GetAccessToken<BoxLoginProvider>(consumerFactory, code);

                if (token == null)
                {
                    throw new UnauthorizedAccessException(string.Format(FilesCommonResource.ErrorMessage_SecurityException_Auth, provider));
                }

                return new AuthData(token: token.ToJson());

            case ProviderTypes.DropboxV2:
                code = authData.RawToken;
                token = oAuth20TokenHelper.GetAccessToken<DropboxLoginProvider>(consumerFactory, code);

                if (token == null)
                {
                    throw new UnauthorizedAccessException(string.Format(FilesCommonResource.ErrorMessage_SecurityException_Auth, provider));
                }

                return new AuthData(token: token.ToJson());

            case ProviderTypes.OneDrive:
                code = authData.RawToken;
                token = oAuth20TokenHelper.GetAccessToken<OneDriveLoginProvider>(consumerFactory, code);

                if (token == null)
                {
                    throw new UnauthorizedAccessException(string.Format(FilesCommonResource.ErrorMessage_SecurityException_Auth, provider));
                }

                return new AuthData(token: token.ToJson());
            case ProviderTypes.SharePoint:
            case ProviderTypes.WebDav:
                break;

            default:
                authData.Url = null;
                break;
        }

        return authData;
    }

    private string EncryptPassword(string password)
    {
        return string.IsNullOrEmpty(password) ? string.Empty : instanceCrypto.Encrypt(password);
    }

    private string DecryptPassword(string password, int id)
    {
        try
        {
            return string.IsNullOrEmpty(password) ? string.Empty : instanceCrypto.Decrypt(password);
        }
        catch (Exception e)
        {
            _logger.ErrorDecryptPassword(id, securityContext.CurrentAccount.ID, e);
            return null;
        }
    }

    private string DecryptToken(string token, int id)
    {
        try
        {
            return DecryptPassword(token, id);
        }
        catch
        {
            //old token in base64 without encrypt
            return token ?? "";
        }
    }
}

public static class ProviderAccountDaoExtension
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<BoxProviderInfo>();
        services.TryAdd<DropboxProviderInfo>();
        services.TryAdd<SharePointProviderInfo>();
        services.TryAdd<GoogleDriveProviderInfo>();
        services.TryAdd<OneDriveProviderInfo>();
        services.TryAdd<WebDavProviderInfo>();
    }
}

public class ProviderData
{
    public int Id { get; init; }
    public string Title { get; init; }
    public string FolderId { get; init; }
    public FolderType? FolderType { get; init; }
    public FolderType? RootFolderType { get; init; }
    public bool? Private { get; init; }
    public bool? HasLogo { get; init; }
    public string Color { get; init; }
    public AuthData AuthData { get; init; }
}

static file class Queries
{
    public static readonly Func<FilesDbContext, int, Guid, IAsyncEnumerable<DbFilesThirdpartyAccount>>
        ThirdPartyAccountsAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, Guid userId) =>
                ctx.ThirdpartyAccount
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.UserId == userId));

    public static readonly
        Func<FilesDbContext, int, int, FolderType, Guid, string, IAsyncEnumerable<DbFilesThirdpartyAccount>>
        ThirdPartyAccountsByFilterAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId, FolderType folderType, Guid userId, string searchText) =>
                ctx.ThirdpartyAccount
                    
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => !(folderType == FolderType.USER || folderType == FolderType.DEFAULT && linkId == -1) ||
                                r.UserId == userId || r.FolderType == FolderType.ThirdpartyBackup)
                    .Where(r => linkId == -1 || r.Id == linkId)
                    .Where(r => folderType == FolderType.DEFAULT &&
                        !(r.FolderType == FolderType.ThirdpartyBackup && linkId == -1) || r.FolderType == folderType)
                    .Where(r => searchText == "" || r.Title.ToLower().Contains(searchText)));

    public static readonly Func<FilesDbContext, int, int, Task<DbFilesThirdpartyAccount>> ThirdPartyAccountAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId) =>
                ctx.ThirdpartyAccount
                    .FirstOrDefault(r =>  r.Id == linkId && r.TenantId == tenantId));

    public static readonly Func<FilesDbContext, int, int, string, string, string, string, Task<int>>
        UpdateThirdPartyAccountsAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId, string login, string password, string token, string url) =>
                ctx.ThirdpartyAccount
                    .Where(r => r.Id == linkId)
                    .Where(r => r.TenantId == tenantId)
                    .ExecuteUpdate(f => f
                        .SetProperty(p => p.UserName, login)
                        .SetProperty(p => p.Password, password)
                        .SetProperty(p => p.Token, token)
                        .SetProperty(p => p.Url, url)
                        .SetProperty(p => p.ModifiedOn, DateTime.UtcNow)));

    public static readonly Func<FilesDbContext, int, int, Task<DbFilesThirdpartyAccount>>
        ThirdPartyAccountByLinkIdAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId) =>
                ctx.ThirdpartyAccount
                    .Single(r => r.TenantId == tenantId &&r.Id == linkId));

    public static readonly Func<FilesDbContext, int, int, IAsyncEnumerable<DbFilesThirdpartyAccount>>
        ThirdPartyAccountsByLinkIdAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId) =>
                ctx.ThirdpartyAccount
                    .AsTracking()
                    .Where(r => r.Id == linkId)
                    .Where(r => r.TenantId == tenantId));

    public static readonly Func<FilesDbContext, int, int, Task<int>>
        DeleteThirdPartyAccountsByLinkIdAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, int linkId) =>
                ctx.ThirdpartyAccount
                    .AsTracking()
                    .Where(r => r.Id == linkId)
                    .Where(r => r.TenantId == tenantId)
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, Task<DbFilesThirdpartyAccount>> ThirdPartyBackupAccountAsync =
        EF.CompileAsyncQuery((FilesDbContext ctx, int tenantId) =>
                ctx.ThirdpartyAccount.Single(r => r.TenantId == tenantId && r.FolderType == FolderType.ThirdpartyBackup));

    public static readonly Func<FilesDbContext, int, string, IAsyncEnumerable<string>> HashIdsAsync =
        EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, string folderId) =>
                ctx.ThirdpartyIdMapping
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Id.StartsWith(folderId))
                    .Select(r => r.HashId));

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, Task<int>>
        DeleteDbFilesSecuritiesAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> entryIDs) =>
                ctx.Security
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => entryIDs.Any(a => a == r.EntryId))
                    .ExecuteDelete());

    public static readonly Func<FilesDbContext, int, IEnumerable<string>, Task<int>>
        DeleteDbFilesTagLinksAsync = EF.CompileAsyncQuery(
            (FilesDbContext ctx, int tenantId, IEnumerable<string> entryIDs) =>
                ctx.TagLink
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => entryIDs.Any(e => e == r.EntryId))
                    .ExecuteDelete());
}