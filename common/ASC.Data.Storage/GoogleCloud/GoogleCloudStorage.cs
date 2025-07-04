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

using Object = Google.Apis.Storage.v1.Data.Object;

namespace ASC.Data.Storage.GoogleCloud;

[Scope]
public class GoogleCloudStorage(TempStream tempStream,
        TenantManager tenantManager,
        PathUtils pathUtils,
        EmailValidationKeyProvider emailValidationKeyProvider,
        IHttpContextAccessor httpContextAccessor,
        ILoggerProvider factory,
        ILogger<GoogleCloudStorage> options,
        IHttpClientFactory clientFactory,
        TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
        QuotaSocketManager quotaSocketManager,
        SettingsManager settingsManager,
        IQuotaService quotaService,
        UserManager userManager,
        CustomQuota customQuota)
    : BaseStorage(tempStream, tenantManager, pathUtils, emailValidationKeyProvider, httpContextAccessor, factory, options, clientFactory, tenantQuotaFeatureStatHelper, quotaSocketManager, settingsManager, quotaService, userManager, customQuota)
{
    public override bool IsSupportChunking => true;

    private string _subDir = string.Empty;
    private Dictionary<string, PredefinedObjectAcl> _domainsAcl;
    private PredefinedObjectAcl _moduleAcl;
    private string _bucket = string.Empty;
    private string _json = string.Empty;
    private Uri _bucketRoot;
    private Uri _bucketSSlRoot;
    private bool _lowerCasing = true;

    public override Task<IDataStore> ConfigureAsync(string tenant, Handler handlerConfig, Module moduleConfig, IDictionary<string, string> props, IDataStoreValidator dataStoreValidator)
    {
        Tenant = tenant;

        if (moduleConfig != null)
        {
            Modulename = moduleConfig.Name;
            DataList = new DataList(moduleConfig);

            DomainsExpires = moduleConfig.Domain.Where(x => x.Expires != TimeSpan.Zero).ToDictionary(x => x.Name, y => y.Expires);
            DomainsExpires.Add(string.Empty, moduleConfig.Expires);

            DomainsContentAsAttachment = moduleConfig.Domain.Where(x => x.ContentAsAttachment.HasValue).ToDictionary(x => x.Name, y => y.ContentAsAttachment.Value);
            DomainsContentAsAttachment.Add(string.Empty, moduleConfig.ContentAsAttachment ?? false);

            _domainsAcl = moduleConfig.Domain.ToDictionary(x => x.Name, y => GetGoogleCloudAcl(y.Acl));
            _moduleAcl = GetGoogleCloudAcl(moduleConfig.Acl);
        }
        else
        {
            Modulename = string.Empty;
            DataList = null;

            DomainsExpires = new Dictionary<string, TimeSpan> { { string.Empty, TimeSpan.Zero } };
            DomainsContentAsAttachment = new Dictionary<string, bool> { { string.Empty, false } };

            _domainsAcl = new Dictionary<string, PredefinedObjectAcl>();
            _moduleAcl = PredefinedObjectAcl.PublicRead;
        }

        _bucket = props["bucket"];

        _bucketRoot = props.ContainsKey("cname") && Uri.IsWellFormedUriString(props["cname"], UriKind.Absolute)
                          ? new Uri(props["cname"], UriKind.Absolute)
                              : new Uri("https://storage.googleapis.com/" + _bucket + "/", UriKind.Absolute);

        _bucketSSlRoot = props.ContainsKey("cnamessl") &&
                         Uri.IsWellFormedUriString(props["cnamessl"], UriKind.Absolute)
                             ? new Uri(props["cnamessl"], UriKind.Absolute)
                                 : new Uri("https://storage.googleapis.com/" + _bucket + "/", UriKind.Absolute);

        if (props.TryGetValue("lower", out var value))
        {
            bool.TryParse(value, out _lowerCasing);
        }

        _json = props["json"];

        props.TryGetValue("subdir", out _subDir);

        DataStoreValidator = dataStoreValidator;
        
        return Task.FromResult<IDataStore>(this);
    }

    public static long DateToUnixTimestamp(DateTime date)
    {
        var ts = date - new DateTime(1970, 1, 1, 0, 0, 0);

        return (long)ts.TotalSeconds;
    }

    public override Task<Uri> GetInternalUriAsync(string domain, string path, TimeSpan expire, IEnumerable<string> headers)
    {
        if (expire == TimeSpan.Zero || expire == TimeSpan.MinValue || expire == TimeSpan.MaxValue)
        {
            expire = GetExpire(domain);
        }
        if (expire == TimeSpan.Zero || expire == TimeSpan.MinValue || expire == TimeSpan.MaxValue)
        {
            return Task.FromResult(GetUriShared(domain, path));
        }

        return InternalGetInternalUriAsync(domain, path, expire);
    }

    private async Task<Uri> InternalGetInternalUriAsync(string domain, string path, TimeSpan expire)
    {
        using var storage = await GetStorageAsync();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(_json ?? ""));
        var preSignedURL = await (await FromCredentialStreamAsync(stream)).SignAsync(_bucket, MakePath(domain, path), expire, HttpMethod.Get);

        return MakeUri(preSignedURL);
    }

    public Uri GetUriShared(string domain, string path)
    {
        return new Uri(SecureHelper.IsSecure(_httpContextAccessor.HttpContext, _options) ? _bucketSSlRoot : _bucketRoot, MakePath(domain, path));
    }
    public override Task<Stream> GetReadStreamAsync(string domain, string path)
    {
        return GetReadStreamAsync(domain, path, 0);
    }

    public override async Task<Stream> GetReadStreamAsync(string domain, string path, long offset)
    {
        return await GetReadStreamAsync(domain, path, offset, long.MaxValue);
    }
    
    public override async Task<Stream> GetReadStreamAsync(string domain, string path, long offset, long length)
    {
        var tempStream = _tempStream.Create();

        var storage = await GetStorageAsync();
        DownloadObjectOptions options = null;
        
        if (length > 0 && (offset > 0 || offset == 0 && length != long.MaxValue))
        {
            options = new DownloadObjectOptions
            {
                Range = new RangeHeaderValue(offset, offset + length - 1)
            };
        }
        
        await storage.DownloadObjectAsync(_bucket, MakePath(domain, path), tempStream, options);

        if (offset > 0)
        {
            tempStream.Seek(offset, SeekOrigin.Begin);
        }

        tempStream.Position = 0;

        return tempStream;
    }

    public override Task<Uri> SaveAsync(string domain, string path, Stream stream)
    {
        return SaveAsync(domain, path, stream, string.Empty, string.Empty);
    }

    public override Task<Uri> SaveAsync(string domain, string path, Stream stream, Guid ownerId)
    {
        return SaveAsync(domain, path, ownerId, stream, string.Empty, string.Empty);
    }

    public override Task<Uri> SaveAsync(string domain, string path, Stream stream, ACL acl)
    {
        return SaveAsync(domain, path, stream, null, null, acl);
    }

    public override Task<Uri> SaveAsync(string domain, string path, Stream stream, string contentType, string contentDisposition)
    {
        return SaveAsync(domain, path, Guid.Empty, stream, contentType, contentDisposition);
    }
    public override Task<Uri> SaveAsync(string domain, string path, Guid ownerId, Stream stream, string contentType, string contentDisposition)
    {
        return SaveAsync(domain, path, ownerId, stream, contentType, contentDisposition, ACL.Auto);
    }

    public override Task<Uri> SaveAsync(string domain, string path, Stream stream, string contentEncoding, int cacheDays)
    {
        return SaveAsync(domain, path, stream, string.Empty, string.Empty, ACL.Auto, contentEncoding, cacheDays);
    }

    private bool EnableQuotaCheck(string domain)
    {
        return (QuotaController != null) && !domain.EndsWith("_temp");
    }

    public async Task<Uri> SaveAsync(string domain, string path, Stream stream, string contentType,
                 string contentDisposition, ACL acl, string contentEncoding = null, int cacheDays = 5)
    {
        return await SaveAsync(domain, path, Guid.Empty, stream, contentType,
                  contentDisposition, acl, contentEncoding, cacheDays);
    }
    public async Task<Uri> SaveAsync(string domain, string path, Guid ownerId, Stream stream, string contentType,
                  string contentDisposition, ACL acl, string contentEncoding = null, int cacheDays = 5)
    {

        var (buffered, isNew) = await _tempStream.TryGetBufferedAsync(stream);
        try
        {
            if (EnableQuotaCheck(domain))
            {
                await QuotaController.QuotaUsedCheckAsync(buffered.Length, ownerId);
            }

            var mime = string.IsNullOrEmpty(contentType)
                        ? MimeMapping.GetMimeMapping(Path.GetFileName(path))
                        : contentType;

            using var storage = await GetStorageAsync();

            var uploadObjectOptions = new UploadObjectOptions
            {
                PredefinedAcl = acl == ACL.Auto ? GetDomainACL(domain) : GetGoogleCloudAcl(acl)
            };

            buffered.Position = 0;

            var uploaded = await storage.UploadObjectAsync(_bucket, MakePath(domain, path), mime, buffered, uploadObjectOptions);

            uploaded.ContentEncoding = contentEncoding;
            uploaded.CacheControl = $"public, maxage={(int)TimeSpan.FromDays(cacheDays).TotalSeconds}";

            uploaded.Metadata ??= new Dictionary<string, string>();

            uploaded.Metadata["Expires"] = DateTime.UtcNow.Add(TimeSpan.FromDays(cacheDays)).ToString("R", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(contentDisposition))
            {
                uploaded.ContentDisposition = contentDisposition;
            }
            else if (mime == "application/octet-stream")
            {
                uploaded.ContentDisposition = "attachment";
            }

            await storage.UpdateObjectAsync(uploaded);

            //           InvalidateCloudFront(MakePath(domain, path));

            await QuotaUsedAddAsync(domain, buffered.Length);

            return await GetUriAsync(domain, path);
        }
        finally
        {
            if (isNew)
            {
                await buffered.DisposeAsync();
            }
        }
    }

    public override async Task DeleteAsync(string domain, string path)
    {
        using var storage = await GetStorageAsync();

        var key = MakePath(domain, path);
        var size = await GetFileSizeAsync(domain, path);

        await storage.DeleteObjectAsync(_bucket, key);

        await QuotaUsedDeleteAsync(domain, size);
    }

    public override async Task DeleteFilesAsync(string domain, string folderPath, string pattern, bool recursive)
    {
        await DeleteFilesAsync(domain, folderPath, pattern, recursive, Guid.Empty);
    }
    public override async Task DeleteFilesAsync(string domain, string folderPath, string pattern, bool recursive, Guid ownerId)
    {
        using var storage = await GetStorageAsync();

        IAsyncEnumerable<Object> objToDel;

        if (recursive)
        {
            objToDel = storage
                       .ListObjectsAsync(_bucket, MakePath(domain, folderPath))
                       .Where(x => Wildcard.IsMatch(pattern, Path.GetFileName(x.Name)));
        }
        else
        {
            objToDel = AsyncEnumerable.Empty<Object>();
        }

        await foreach (var obj in objToDel)
        {
            await storage.DeleteObjectAsync(_bucket, obj.Name);
            await QuotaUsedDeleteAsync(domain, Convert.ToInt64(obj.Size), ownerId);
        }
    }

    public override async Task DeleteFilesAsync(string domain, List<string> paths)
    {
        if (paths.Count == 0)
        {
            return;
        }

        var keysToDel = new List<string>();

        long quotaUsed = 0;

        foreach (var path in paths)
        {
            try
            {

                var key = MakePath(domain, path);

                if (QuotaController != null)
                {
                    quotaUsed += await GetFileSizeAsync(domain, path);
                }

                keysToDel.Add(key);
            }
            catch (FileNotFoundException)
            {

            }
        }

        if (keysToDel.Count == 0)
        {
            return;
        }

        using var storage = await GetStorageAsync();

        foreach (var e in keysToDel)
        {
            await storage.DeleteObjectAsync(_bucket, e);
        }

        if (quotaUsed > 0)
        {
            await QuotaUsedDeleteAsync(domain, quotaUsed);
        }
    }

    public override async Task DeleteFilesAsync(string domain, string folderPath, DateTime fromDate, DateTime toDate)
    {
        using var storage = await GetStorageAsync();

        var objToDel = GetObjectsAsync(domain, folderPath, true)
                      .Where(x => x.UpdatedDateTimeOffset >= fromDate && x.UpdatedDateTimeOffset <= toDate);

        await foreach (var obj in objToDel)
        {
            await storage.DeleteObjectAsync(_bucket, obj.Name);
            await QuotaUsedDeleteAsync(domain, Convert.ToInt64(obj.Size));
        }
    }

    public override async Task MoveDirectoryAsync(string srcDomain, string srcDir, string newDomain, string newDir)
    {
        using var storage = await GetStorageAsync();
        var srckey = MakePath(srcDomain, srcDir);
        var dstkey = MakePath(newDomain, newDir);

        var objects = storage.ListObjects(_bucket, srckey);

        foreach (var obj in objects)
        {
            await storage.CopyObjectAsync(_bucket, srckey, _bucket, dstkey, new CopyObjectOptions
            {
                DestinationPredefinedAcl = GetDomainACL(newDomain)
            });

            await storage.DeleteObjectAsync(_bucket, srckey);

        }
    }

    public override async Task<Uri> MoveAsync(string srcDomain, string srcPath, string newDomain, string newPath, bool quotaCheckFileSize = true)
    {
        return await MoveAsync(srcDomain, srcPath, newDomain, newPath, Guid.Empty, quotaCheckFileSize);
    }

    public override async Task<Uri> MoveAsync(string srcDomain, string srcPath, string newDomain, string newPath, Guid ownerId, bool quotaCheckFileSize = true)
    {
        using var storage = await GetStorageAsync();

        var srcKey = MakePath(srcDomain, srcPath);
        var dstKey = MakePath(newDomain, newPath);
        var size = await GetFileSizeAsync(srcDomain, srcPath);

        await storage.CopyObjectAsync(_bucket, srcKey, _bucket, dstKey, new CopyObjectOptions
        {
            DestinationPredefinedAcl = GetDomainACL(newDomain)
        });

        await DeleteAsync(srcDomain, srcPath);

        await QuotaUsedDeleteAsync(srcDomain, size);
        await QuotaUsedAddAsync(newDomain, size, ownerId, quotaCheckFileSize);

        return await GetUriAsync(newDomain, newPath);
    }

    public override async Task<(Uri, string)> SaveTempAsync(string domain, Stream stream)
    {
        var assignedPath = Guid.NewGuid().ToString();

        return (await SaveAsync(domain, assignedPath, stream), assignedPath);
    }

    public override IAsyncEnumerable<string> ListDirectoriesRelativeAsync(string domain, string path, bool recursive)
    {
        return GetObjectsAsync(domain, path, recursive)
               .Select(x => x.Name[MakePath(domain, path + "/").Length..]);
    }
    

    private IAsyncEnumerable<Object> GetObjectsAsync(string domain, string path, bool recursive)
    {
        using var storage = GetStorage();

        var items = storage.ListObjectsAsync(_bucket, MakePath(domain, path));

        if (recursive)
        {
            return items;
        }

        return items.Where(x => x.Name.IndexOf('/', MakePath(domain, path + "/").Length) == -1);
    }

    public override IAsyncEnumerable<string> ListFilesRelativeAsync(string domain, string path, string pattern, bool recursive)
    {
        return GetObjectsAsync(domain, path, recursive).Where(x => Wildcard.IsMatch(pattern, Path.GetFileName(x.Name)))
               .Select(x => x.Name[MakePath(domain, path + "/").Length..].TrimStart('/'));
    }

    public override async Task<bool> IsFileAsync(string domain, string path)
    {
        var storage = await GetStorageAsync();

        var objects = await storage.ListObjectsAsync(_bucket, MakePath(domain, path)).ReadPageAsync(1);

        return objects.Any();
    }

    public override Task<bool> IsDirectoryAsync(string domain, string path)
    {
        return IsFileAsync(domain, path);
    }

    public override async Task DeleteDirectoryAsync(string domain, string path)
    {
        await DeleteDirectoryAsync(domain, path, Guid.Empty);
    }
    public override async Task DeleteDirectoryAsync(string domain, string path, Guid ownerId)
    {
        using var storage = await GetStorageAsync();

        var objToDel = storage.ListObjectsAsync(_bucket, MakePath(domain, path));

        await foreach (var obj in objToDel)
        {
            await storage.DeleteObjectAsync(_bucket, obj.Name);

            if (QuotaController != null)
            {
                if (string.IsNullOrEmpty(QuotaController.ExcludePattern) ||
                    !Path.GetFileName(obj.Name).StartsWith(QuotaController.ExcludePattern))
                {
                    await QuotaUsedDeleteAsync(domain, Convert.ToInt64(obj.Size), ownerId);
                }
            }
        }
    }

    public override async Task<long> GetFileSizeAsync(string domain, string path)
    {
        using var storage = await GetStorageAsync();

        var obj = await storage.GetObjectAsync(_bucket, MakePath(domain, path));

        return obj.Size.HasValue ? Convert.ToInt64(obj.Size.Value) : 0;
    }

    public override async Task<long> GetDirectorySizeAsync(string domain, string path)
    {
        using var storage = await GetStorageAsync();

        var objToDel = storage
                          .ListObjectsAsync(_bucket, MakePath(domain, path));

        long result = 0;

        await foreach (var obj in objToDel)
        {
            if (obj.Size.HasValue)
            {
                result += Convert.ToInt64(obj.Size.Value);
            }
        }

        return result;
    }

    public override async Task<long> ResetQuotaAsync(string domain)
    {
        using var storage = await GetStorageAsync();

        var objects = storage
                          .ListObjectsAsync(_bucket, MakePath(domain, string.Empty));

        if (QuotaController != null)
        {
            long size = 0;

            await foreach (var obj in objects)
            {
                if (obj.Size.HasValue)
                {
                    size += Convert.ToInt64(obj.Size.Value);
                }
            }

            await QuotaController.QuotaUsedSetAsync(Modulename, domain, DataList.GetData(domain), size);

            return size;
        }

        return 0;
    }


    public override async Task<long> GetUsedQuotaAsync(string domain)
    {
        using var storage = await GetStorageAsync();

        var objects = storage
                          .ListObjectsAsync(_bucket, MakePath(domain, string.Empty));

        long result = 0;

        await foreach (var obj in objects)
        {
            if (obj.Size.HasValue)
            {
                result += Convert.ToInt64(obj.Size.Value);
            }
        }

        return result;
    }

    public override async Task<Uri> CopyAsync(string srcDomain, string srcpath, string newDomain, string newPath)
    {
        using var storage = await GetStorageAsync();

        var size = await GetFileSizeAsync(srcDomain, srcpath);

        var options = new CopyObjectOptions
        {
            DestinationPredefinedAcl = GetDomainACL(newDomain)
        };

        await storage.CopyObjectAsync(_bucket, MakePath(srcDomain, srcpath), _bucket, MakePath(newDomain, newPath), options);

        await QuotaUsedAddAsync(newDomain, size);

        return await GetUriAsync(newDomain, newPath);
    }

    public override async Task CopyDirectoryAsync(string srcDomain, string srcdir, string newDomain, string newDir)
    {
        var srckey = MakePath(srcDomain, srcdir);
        var dstkey = MakePath(newDomain, newDir);
        //List files from src

        using var storage = await GetStorageAsync();

        var objects = storage.ListObjectsAsync(_bucket, srckey);

        await foreach (var obj in objects)
        {
            await storage.CopyObjectAsync(_bucket, srckey, _bucket, dstkey, new CopyObjectOptions
            {
                DestinationPredefinedAcl = GetDomainACL(newDomain)
            });

            await QuotaUsedAddAsync(newDomain, Convert.ToInt64(obj.Size));
        }
    }

    public override async Task<string> SavePrivateAsync(string domain, string path, Stream stream, DateTime expires)
    {
        using var storage = await GetStorageAsync();

        var (buffered, isNew) = await _tempStream.TryGetBufferedAsync(stream);

        try
        {
            var uploadObjectOptions = new UploadObjectOptions
            {
                PredefinedAcl = PredefinedObjectAcl.BucketOwnerFullControl
            };

            buffered.Position = 0;

            var uploaded = await storage.UploadObjectAsync(_bucket, MakePath(domain, path), "application/octet-stream", buffered, uploadObjectOptions);

            uploaded.CacheControl = $"public, maxage={(int)TimeSpan.FromDays(5).TotalSeconds}";
            uploaded.ContentDisposition = "attachment";
            uploaded.Metadata ??= new Dictionary<string, string>();
            uploaded.Metadata["Expires"] = DateTime.UtcNow.Add(TimeSpan.FromDays(5)).ToString("R", CultureInfo.InvariantCulture);
            uploaded.Metadata.Add("private-expire", expires.ToFileTimeUtc().ToString(CultureInfo.InvariantCulture));

            await storage.UpdateObjectAsync(uploaded);
        }
        finally
        {
            if (isNew)
            {
                await buffered.DisposeAsync();
            }
        }

        using var mStream = new MemoryStream(Encoding.UTF8.GetBytes(_json ?? ""));
        var signDuration = expires.Date == DateTime.MinValue ? expires.TimeOfDay : expires.Subtract(DateTime.UtcNow);
        var preSignedURL = await (await FromCredentialStreamAsync(mStream))
            .SignAsync(RequestTemplate.FromBucket(_bucket).WithObjectName(MakePath(domain, path)), Options.FromDuration(signDuration));

        //TODO: CNAME!
        return preSignedURL;
    }

    public override async Task DeleteExpiredAsync(string domain, string path, TimeSpan oldThreshold)
    {
        using var storage = await GetStorageAsync();

        var objects = storage.ListObjectsAsync(_bucket, MakePath(domain, path));

        await foreach (var obj in objects)
        {
            var objInfo = await storage.GetObjectAsync(_bucket, MakePath(domain, path));

            var privateExpireKey = objInfo.Metadata["private-expire"];

            if (string.IsNullOrEmpty(privateExpireKey))
            {
                continue;
            }

            if (!long.TryParse(privateExpireKey, out var fileTime))
            {
                continue;
            }

            if (DateTime.UtcNow <= DateTime.FromFileTimeUtc(fileTime))
            {
                continue;
            }

            await storage.DeleteObjectAsync(_bucket, MakePath(domain, path));

        }
    }

    #region chunking

    public override async Task<string> InitiateChunkedUploadAsync(string domain, string path)
    {
        using var storage = await GetStorageAsync();

        var tempUploader = storage.CreateObjectUploader(_bucket, MakePath(domain, path), null, new MemoryStream());

        var sessionUri = await tempUploader.InitiateSessionAsync();

        return sessionUri.ToString();
    }



    public override async Task<string> UploadChunkAsync(string domain,
                                      string path,
                                      string uploadUri,
                                      Stream stream,
                                      long defaultChunkSize,
                                      int chunkNumber,
                                      long chunkLength)
    {

        var bytesRangeStart = Convert.ToString((chunkNumber - 1) * defaultChunkSize);
        var bytesRangeEnd = Convert.ToString((chunkNumber - 1) * defaultChunkSize + chunkLength - 1);

        var totalBytes = "*";

        if (chunkLength != defaultChunkSize)
        {
            totalBytes = Convert.ToString((chunkNumber - 1) * defaultChunkSize + chunkLength);
        }

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(uploadUri),
            Method = HttpMethod.Put
        };
        request.Content = new StreamContent(stream);
        request.Content.Headers.ContentRange = new ContentRangeHeaderValue(Convert.ToInt64(bytesRangeStart),
                                                               Convert.ToInt64(bytesRangeEnd),
                                                               Convert.ToInt64(totalBytes));

        const int MAX_RETRIES = 100;

        for (var i = 0; i < MAX_RETRIES; i++)
        {
            var millisecondsTimeout = Math.Min(Convert.ToInt32(Math.Pow(2, i)) + RandomNumberGenerator.GetInt32(1000), 32 * 1000);

            try
            {
                var httpClient = _clientFactory.CreateClient();
                using var response = await httpClient.SendAsync(request);

                break;
            }
            catch (HttpRequestException ex)
            {
                var status = (int)ex.StatusCode;

                if (status is 408 or 500 or 502 or 503 or 504)
                {
                    Thread.Sleep(millisecondsTimeout);
                    continue;
                }

                if (status != 308)
                {
                    throw;
                }

                break;
            }
            catch
            {
                await AbortChunkedUploadAsync(domain, path, uploadUri);
                throw;
            }
        }

        return string.Empty;
    }

    public override async Task<Uri> FinalizeChunkedUploadAsync(string domain, string path, string uploadUri, Dictionary<int, string> eTags)
    {
        if (QuotaController != null)
        {
            var size = await GetFileSizeAsync(domain, path);
            await QuotaUsedAddAsync(domain, size);
        }

        return await GetUriAsync(domain, path);
    }

    public override Task AbortChunkedUploadAsync(string domain, string path, string uploadUri)
    {
        return Task.CompletedTask;
    }

    #endregion

    public override string GetUploadForm(string domain, string directoryPath, string redirectTo, long maxUploadSize, string contentType, string contentDisposition, string submitLabel)
    {
        throw new NotImplementedException();
    }

    public override string GetUploadUrl()
    {
        throw new NotImplementedException();
    }

    public override string GetPostParams(string domain, string directoryPath, long maxUploadSize, string contentType, string contentDisposition)
    {
        throw new NotImplementedException();
    }

    protected override Task<Uri> SaveWithAutoAttachmentAsync(string domain, string path, Stream stream, string attachmentFileName)
    {
        return SaveWithAutoAttachmentAsync(domain, path, Guid.Empty, stream, attachmentFileName);
    }
    protected override Task<Uri> SaveWithAutoAttachmentAsync(string domain, string path, Guid ownerId, Stream stream, string attachmentFileName)
    {
        var contentDisposition = $"attachment; filename={HttpUtility.UrlPathEncode(attachmentFileName)};";
        if (attachmentFileName.Any(c => c >= 0 && c <= 127))
        {
            contentDisposition = $"attachment; filename*=utf-8''{HttpUtility.UrlPathEncode(attachmentFileName)};";
        }
        return SaveAsync(domain, path, ownerId, stream, null, contentDisposition);
    }

    private StorageClient GetStorage()
    {
        var credential = GoogleCredential.FromJson(_json);

        return StorageClient.Create(credential);
    }

    private async Task<StorageClient> GetStorageAsync()
    {
        var credential = GoogleCredential.FromJson(_json);

        return await StorageClient.CreateAsync(credential);
    }

    private string MakePath(string domain, string path)
    {
        string result;

        path = path.TrimStart('\\', '/').TrimEnd('/').Replace('\\', '/');

        if (!string.IsNullOrEmpty(_subDir))
        {
            if (_subDir.Length == 1 && (_subDir[0] == '/' || _subDir[0] == '\\'))
            {
                result = path;
            }
            else
            {
                result = $"{_subDir}/{path}"; // Ignory all, if _subDir is not null
            }
        }
        else//Key combined from module+domain+filename
        {
            result = $"{Tenant}/{Modulename}/{domain}/{path}";
        }

        result = result.Replace("//", "/").TrimStart('/');
        if (_lowerCasing)
        {
            result = result.ToLowerInvariant();
        }

        return result;
    }

    private Uri MakeUri(string preSignedURL)
    {
        var uri = new Uri(preSignedURL);
        var signedPart = uri.PathAndQuery.TrimStart('/');

        return new Uri(uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? _bucketSSlRoot : _bucketRoot, signedPart);
    }

    // private void InvalidateCloudFront(params string[] paths)
    // {
    //     throw new NotImplementedException();
    // }

    private PredefinedObjectAcl GetGoogleCloudAcl(ACL _)
    {
        return PredefinedObjectAcl.PublicRead;
        //return acl switch
        //{
        //    ACL.Read => PredefinedObjectAcl.PublicRead,
        //    _ => PredefinedObjectAcl.PublicRead,
        //};
    }

    private PredefinedObjectAcl GetDomainACL(string domain)
    {
        if (GetExpire(domain) != TimeSpan.Zero)
        {
            return PredefinedObjectAcl.Private;
        }

        return _domainsAcl.GetValueOrDefault(domain, _moduleAcl);
    }

    public override async Task<string> GetFileEtagAsync(string domain, string path)
    {
        var storage = await GetStorageAsync();
        var objectName = MakePath(domain, path);

        var obj = await storage.GetObjectAsync(_bucket, objectName);

        var lastModificationDate = obj == null ? throw new FileNotFoundException("File not found" + objectName) : obj.UpdatedDateTimeOffset ?? obj.TimeCreatedDateTimeOffset ?? DateTime.MinValue;

        var etag = '"' + lastModificationDate.Ticks.ToString("X8", CultureInfo.InvariantCulture) + '"';

        return etag;
    }
}
