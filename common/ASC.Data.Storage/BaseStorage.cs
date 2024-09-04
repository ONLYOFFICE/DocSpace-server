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

namespace ASC.Data.Storage;

public abstract class BaseStorage(TempStream tempStream,
        TenantManager tenantManager,
        PathUtils pathUtils,
        EmailValidationKeyProvider emailValidationKeyProvider,
        IHttpContextAccessor httpContextAccessor,
        ILoggerProvider options,
        ILogger logger,
        IHttpClientFactory clientFactory,
        TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
        QuotaSocketManager quotaSocketManager,
        SettingsManager settingsManager,
        IQuotaService quotaService,
        UserManager userManager,
        CustomQuota customQuota)
    : IDataStore
{
    public IQuotaController QuotaController { get; set; }
    public IDataStoreValidator DataStoreValidator { get; set; }
    public virtual bool IsSupportInternalUri => true;
    public virtual bool IsSupportCdnUri => false;
    public virtual bool IsSupportedPreSignedUri => true;
    public virtual bool IsSupportChunking => false;
    public virtual bool ContentAsAttachment => false;
    internal string Modulename { get; set; }
    internal bool Cache { get; set; }
    internal DataList DataList { get; set; }
    internal string Tenant { get; set; }
    internal Dictionary<string, TimeSpan> DomainsExpires { get; set; } = new();
    protected ILogger Logger { get; set; } = logger;

    protected readonly TempStream _tempStream = tempStream;
    protected readonly PathUtils _pathUtils = pathUtils;
    protected readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    protected readonly ILoggerProvider _options = options;
    protected readonly IHttpClientFactory _clientFactory = clientFactory;

    public TimeSpan GetExpire(string domain)
    {
        return DomainsExpires.TryGetValue(domain, out var expire) ? expire : DomainsExpires[string.Empty];
    }

    public async Task<Uri> GetUriAsync(string path)
    {
        return await GetUriAsync(string.Empty, path);
    }

    public async Task<Uri> GetUriAsync(string domain, string path)
    {
        return await GetPreSignedUriAsync(domain, path, TimeSpan.MaxValue, null);
    }

    public async Task<Uri> GetPreSignedUriAsync(string domain, string path, TimeSpan expire, IEnumerable<string> headers)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (string.IsNullOrEmpty(Tenant) && IsSupportInternalUri)
        {
            return await GetInternalUriAsync(domain, path, expire, headers);
        }

        var headerAttr = string.Empty;
        if (headers != null)
        {
            headerAttr = string.Join("&", headers.Select(HttpUtility.UrlEncode));
        }

        if (expire == TimeSpan.Zero || expire == TimeSpan.MinValue || expire == TimeSpan.MaxValue)
        {
            expire = GetExpire(domain);
        }

        var query = string.Empty;
        if (expire != TimeSpan.Zero && expire != TimeSpan.MinValue && expire != TimeSpan.MaxValue)
        {
            var expireString = expire.TotalMinutes.ToString(CultureInfo.InvariantCulture);

            int currentTenantId;
            var currentTenant = await tenantManager.GetCurrentTenantAsync(false);
            if (currentTenant != null)
            {
                currentTenantId = currentTenant.Id;
            }
            else if (!TenantPath.TryGetTenant(Tenant, out currentTenantId))
            {
                currentTenantId = 0;
            }

            var auth = emailValidationKeyProvider.GetEmailKey(currentTenantId, path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar) + "." + headerAttr + "." + expireString);
            query = $"{(path.IndexOf('?') >= 0 ? "&" : "?")}{Constants.QueryExpire}={expireString}&{Constants.QueryAuth}={auth}";
        }

        if (!string.IsNullOrEmpty(headerAttr))
        {
            query += $"{(query.IndexOf('?') >= 0 ? "&" : "?")}{Constants.QueryHeader}={HttpUtility.UrlEncode(headerAttr)}";
        }

        var tenant = Tenant.Trim('/');
        var vpath = _pathUtils.ResolveVirtualPath(Modulename, domain);
        vpath = _pathUtils.ResolveVirtualPath(vpath, false);
        vpath = string.Format(vpath, tenant);
        var virtualPath = new Uri(vpath + "/", UriKind.RelativeOrAbsolute);

        var uri = virtualPath.IsAbsoluteUri ?
                      new MonoUri(virtualPath, virtualPath.LocalPath.TrimEnd('/') + EnsureLeadingSlash(path.Replace('\\', '/')) + query) :
                      new MonoUri(virtualPath.ToString().TrimEnd('/') + EnsureLeadingSlash(path.Replace('\\', '/')) + query, UriKind.Relative);

        return uri;
    }

    public virtual Task<Uri> GetInternalUriAsync(string domain, string path, TimeSpan expire, IEnumerable<string> headers)
    {
        return Task.FromResult<Uri>(null);
    }

    public virtual Task<Uri> GetCdnPreSignedUriAsync(string domain, string path, TimeSpan expire, IEnumerable<string> headers)
    {
        return Task.FromResult<Uri>(null);
    }

    public abstract Task<Stream> GetReadStreamAsync(string domain, string path);

    public abstract Task<Stream> GetReadStreamAsync(string domain, string path, long offset);

    public abstract Task<Stream> GetReadStreamAsync(string domain, string path, long offset, long length);
    
    public abstract Task<Uri> SaveAsync(string domain, string path, Stream stream);
    public abstract Task<Uri> SaveAsync(string domain, string path, Stream stream, Guid ownerId);
    public abstract Task<Uri> SaveAsync(string domain, string path, Stream stream, ACL acl);

    public async Task<Uri> SaveAsync(string domain, string path, Stream stream, string attachmentFileName)
    {
        return await SaveAsync(domain, path, Guid.Empty, stream, attachmentFileName);
    }
    public async Task<Uri> SaveAsync(string domain, string path, Guid ownerId, Stream stream, string attachmentFileName)
    {
        if (!string.IsNullOrEmpty(attachmentFileName))
        {
            return await SaveWithAutoAttachmentAsync(domain, path, ownerId, stream, attachmentFileName);
        }
        return await SaveAsync(domain, path, stream, ownerId);
    }

    protected abstract Task<Uri> SaveWithAutoAttachmentAsync(string domain, string path, Guid ownerId, Stream stream, string attachmentFileName);
    public abstract Task<Uri> SaveAsync(string domain, string path, Stream stream, string contentType,
                            string contentDisposition);

    public abstract Task<Uri> SaveAsync(string domain, string path,Guid ownerId, Stream stream, string contentType,
                            string contentDisposition);
    public abstract Task<Uri> SaveAsync(string domain, string path, Stream stream, string contentEncoding, int cacheDays);

    public async Task<Uri> SaveAsync(string path, Stream stream, string attachmentFileName)
    {
        return await SaveAsync(string.Empty, path, stream, attachmentFileName);
    }

    public async Task<Uri> SaveAsync(string path, Stream stream)
    {
        return await SaveAsync(string.Empty, path, stream);
    }
    
    protected abstract Task<Uri> SaveWithAutoAttachmentAsync(string domain, string path, Stream stream, string attachmentFileName);

    #region chunking

    public virtual Task<string> InitiateChunkedUploadAsync(string domain, string path)
    {
        throw new NotImplementedException();
    }

    public virtual Task<string> UploadChunkAsync(string domain, string path, string uploadId, Stream stream, long defaultChunkSize, int chunkNumber, long chunkLength)
    {
        throw new NotImplementedException();
    }

    public virtual Task<Uri> FinalizeChunkedUploadAsync(string domain, string path, string uploadId, Dictionary<int, string> eTags)
    {
        throw new NotImplementedException();
    }

    public virtual Task AbortChunkedUploadAsync(string domain, string path, string uploadId)
    {
        throw new NotImplementedException();
    }

    public virtual IDataWriteOperator CreateDataWriteOperator(
            CommonChunkedUploadSession chunkedUploadSession,
            CommonChunkedUploadSessionHolder sessionHolder, bool isConsumerStorage = false)
    {
        return new ChunkZipWriteOperator(_tempStream, chunkedUploadSession, sessionHolder);
    }

    public virtual string GetBackupExtension(bool isConsumerStorage = false)
    {
        return "tar.gz";
    }

    #endregion

    public abstract Task DeleteAsync(string domain, string path);
    public abstract Task DeleteFilesAsync(string domain, string folderPath, string pattern, bool recursive, Guid ownerId);
    public abstract Task DeleteFilesAsync(string domain, string folderPath, string pattern, bool recursive);
    public abstract Task DeleteFilesAsync(string domain, List<string> paths);
    public abstract Task DeleteFilesAsync(string domain, string folderPath, DateTime fromDate, DateTime toDate);
    public abstract Task MoveDirectoryAsync(string srcDomain, string srcDir, string newDomain, string newDir);
    public abstract Task<Uri> MoveAsync(string srcDomain, string srcPath, string newDomain, string newPath, bool quotaCheckFileSize = true);
    public abstract Task<Uri> MoveAsync(string srcdomain, string srcpath, string newdomain, string newpath, Guid ownerId, bool quotaCheckFileSize = true);
    public abstract Task<(Uri, string)> SaveTempAsync(string domain, Stream stream);
    public virtual string GetRootDirectory(string domain)
    {
        return domain;
    }
    public abstract IAsyncEnumerable<string> ListDirectoriesRelativeAsync(string domain, string path, bool recursive);
    public abstract IAsyncEnumerable<string> ListFilesRelativeAsync(string domain, string path, string pattern, bool recursive);

    public abstract Task<bool> IsFileAsync(string domain, string path);
    public abstract Task<bool> IsDirectoryAsync(string domain, string path);
    public abstract Task DeleteDirectoryAsync(string domain, string path);
    public abstract Task DeleteDirectoryAsync(string domain, string path, Guid ownerId);
    public abstract Task<long> GetFileSizeAsync(string domain, string path);
    public abstract Task<long> GetDirectorySizeAsync(string domain, string path);
    public abstract Task<long> ResetQuotaAsync(string domain);
    public abstract Task<long> GetUsedQuotaAsync(string domain);
    public abstract Task<Uri> CopyAsync(string srcDomain, string path, string newDomain, string newPath);
    public abstract Task CopyDirectoryAsync(string srcDomain, string dir, string newDomain, string newDir);

    public async Task<Stream> GetReadStreamAsync(string path)
    {
        return await GetReadStreamAsync(string.Empty, path);
    }

    public async Task DeleteAsync(string path)
    {
        await DeleteAsync(string.Empty, path);
    }

    public async Task DeleteFilesAsync(string folderPath, string pattern, bool recursive)
    {
        await DeleteFilesAsync(string.Empty, folderPath, pattern, recursive);
    }

    public async Task<Uri> MoveAsync(string srcPath, string newDomain, string newPath)
    {
        return await MoveAsync(string.Empty, srcPath, newDomain, newPath);
    }

    public async Task<(Uri, string)> SaveTempAsync(Stream stream)
    {
        return await SaveTempAsync(string.Empty, stream);
    }

    public IAsyncEnumerable<string> ListDirectoriesRelativeAsync(string path, bool recursive)
    {
        return ListDirectoriesRelativeAsync(string.Empty, path, recursive);
    }

    public IAsyncEnumerable<Uri> ListFilesAsync(string path, string pattern, bool recursive)
    {
        return ListFilesAsync(string.Empty, path, pattern, recursive);
    }

    public async IAsyncEnumerable<Uri> ListFilesAsync(string domain, string path, string pattern, bool recursive)
    {
        var filePaths = ListFilesRelativeAsync(domain, path, pattern, recursive);

        await foreach (var paths in filePaths)
        {
            yield return await GetUriAsync(domain, CrossPlatform.PathCombine(PathUtils.Normalize(path), paths));
        }
    }

    public async Task<bool> IsFileAsync(string path)
    {
        return await IsFileAsync(string.Empty, path);
    }

    public async Task<bool> IsDirectoryAsync(string path)
    {
        return await IsDirectoryAsync(string.Empty, path);
    }

    public async Task DeleteDirectoryAsync(string path)
    {
        await DeleteDirectoryAsync(string.Empty, path);
    }
    public async Task DeleteDirectoryAsync(Guid ownerId, string path)
    {
        await DeleteDirectoryAsync(string.Empty, path, ownerId);
    }

    public async Task<long> GetFileSizeAsync(string path)
    {
        return await GetFileSizeAsync(string.Empty, path);
    }

    public async Task<long> GetDirectorySizeAsync(string path)
    {
        return await GetDirectorySizeAsync(string.Empty, path);
    }

    public async Task<Uri> CopyAsync(string path, string newDomain, string newPath)
    {
        return await CopyAsync(string.Empty, path, newDomain, newPath);
    }

    public async Task CopyDirectoryAsync(string dir, string newDomain, string newDir)
    {
        await CopyDirectoryAsync(string.Empty, dir, newDomain, newDir);
    }

    public virtual IDataStore Configure(string tenant, Handler handlerConfig, Module moduleConfig, IDictionary<string, string> props, IDataStoreValidator validator)
    {
        return this;
    }

    public IDataStore SetQuotaController(IQuotaController controller)
    {
        QuotaController = controller;

        return this;
    }

    public abstract Task<string> SavePrivateAsync(string domain, string path, Stream stream, DateTime expires);
    public abstract Task DeleteExpiredAsync(string domain, string path, TimeSpan oldThreshold);

    public abstract string GetUploadForm(string domain, string directoryPath, string redirectTo, long maxUploadSize,
                                         string contentType, string contentDisposition, string submitLabel);

    public abstract string GetUploadUrl();

    public abstract string GetPostParams(string domain, string directoryPath, long maxUploadSize, string contentType,
                                         string contentDisposition);

    internal async Task QuotaUsedAddAsync(string domain, long size, bool quotaCheckFileSize = true)
    {
        await QuotaUsedAddAsync(domain, size, Guid.Empty, quotaCheckFileSize);
    }
    internal async Task QuotaUsedAddAsync(string domain, long size, Guid ownerId, bool quotaCheckFileSize = true)
    {
        if (QuotaController != null)
        {
            ownerId = ownerId == Guid.Empty && Modulename != "files" ? Core.Configuration.Constants.CoreSystem.ID : ownerId;

            await QuotaController.QuotaUsedAddAsync(Modulename, domain, DataList.GetData(domain), size, ownerId, quotaCheckFileSize);
            var(name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<MaxTotalSizeFeature, long>();
            _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
            await NotifyChangeUserQuota(ownerId);
        }
    }

    internal async Task QuotaUsedDeleteAsync(string domain, long size)
    {
       await QuotaUsedDeleteAsync(domain, size, Guid.Empty);
    }
    internal async Task QuotaUsedDeleteAsync(string domain, long size, Guid ownerId)
    {
        if (QuotaController != null)
        {
            await QuotaController.QuotaUsedDeleteAsync(Modulename, domain, DataList.GetData(domain), size, ownerId);
            var (name, value) = await tenantQuotaFeatureStatHelper.GetStatAsync<MaxTotalSizeFeature, long>();
            _ = quotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
            await NotifyChangeUserQuota(ownerId);
        }
    }

    private async Task NotifyChangeUserQuota(Guid ownerId)
    {
        var quotaUserSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
        if (ownerId != Guid.Empty && ownerId != Core.Configuration.Constants.CoreSystem.ID)
        {
            var currentTenant = await tenantManager.GetCurrentTenantAsync(false);
            var user = await userManager.GetUsersAsync(ownerId);
            var userQuotaData = await settingsManager.LoadAsync<UserQuotaSettings>(user);
            var userQuotaLimit = userQuotaData.UserQuota == userQuotaData.GetDefault().UserQuota ? quotaUserSettings.DefaultQuota : userQuotaData.UserQuota;
            var userUsedSpace = Math.Max(0, (await quotaService.FindUserQuotaRowsAsync(currentTenant.Id, user.Id)).Where(r => !string.IsNullOrEmpty(r.Tag) && !string.Equals(r.Tag, Guid.Empty.ToString())).Sum(r => r.Counter));

            _ = quotaSocketManager.ChangeCustomQuotaUsedValueAsync(currentTenant.Id, customQuota.GetFeature<UserCustomQuotaFeature>().Name, quotaUserSettings.EnableQuota, userUsedSpace, userQuotaLimit, [user.Id]);
        }
    }

    internal static string EnsureLeadingSlash(string str)
    {
        return "/" + str.TrimStart('/');
    }

    public abstract Task<string> GetFileEtagAsync(string domain, string path);

    public async Task<string> GetUrlWithHashAsync(string domain, string path)
    {
        var uri = (await GetUriAsync(domain, path)).ToString();

        var hash = (await GetFileEtagAsync(domain, path)).Trim('"');

        return QueryHelpers.AddQueryString(uri, Constants.QueryHash, hash);
    }

    private sealed class MonoUri : Uri
    {
        public MonoUri(Uri baseUri, string relativeUri)
            : base(baseUri, relativeUri) { }

        public MonoUri(string uriString, UriKind uriKind)
            : base(uriString, uriKind) { }

        public override string ToString()
        {
            var s = base.ToString();
            if (WorkContext.IsMono && s.StartsWith(UriSchemeFile + SchemeDelimiter))
            {
                return s[7..];
            }

            return s;
        }
    }
}
