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

namespace ASC.Data.Storage.DiscStorage;

[Scope]
public class DiscDataStore(
    TempStream tempStream,
    TenantManager tenantManager,
    PathUtils pathUtils,
    EmailValidationKeyProvider emailValidationKeyProvider,
    IHttpContextAccessor httpContextAccessor,
    ILoggerProvider options,
    ILogger<DiscDataStore> logger,
    EncryptionSettingsHelper encryptionSettingsHelper,
    EncryptionFactory encryptionFactory,
    IHttpClientFactory clientFactory,
    TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
    QuotaSocketManager quotaSocketManager,
    SettingsManager settingsManager,
    IQuotaService quotaService,
    UserManager userManager,
    CustomQuota customQuota)
    : BaseStorage(tempStream, tenantManager, pathUtils, emailValidationKeyProvider, httpContextAccessor, options, logger, clientFactory, tenantQuotaFeatureStatHelper, quotaSocketManager, settingsManager, quotaService, userManager, customQuota)
{
    public override bool IsSupportInternalUri => false;
    public override bool IsSupportedPreSignedUri => false;
    public override bool IsSupportChunking => true;

    private readonly Dictionary<string, MappedPath> _mappedPaths = new();
    private ICrypt _crypt;

    public override async Task<IDataStore> ConfigureAsync(string tenant, Handler handlerConfig, Module moduleConfig, IDictionary<string, string> props, IDataStoreValidator validator)
    {
        Tenant = tenant;
        //Fill map path
        Modulename = moduleConfig.Name;
        DataList = new DataList(moduleConfig);

        foreach (var domain in moduleConfig.Domain)
        {
            _mappedPaths.Add(domain.Name, new MappedPath(_pathUtils, tenant, moduleConfig.AppendTenantId, domain.Path, handlerConfig.GetProperties()));
        }

        //Add default
        _mappedPaths.Add(string.Empty, new MappedPath(_pathUtils, tenant, moduleConfig.AppendTenantId, PathUtils.Normalize(moduleConfig.Path), handlerConfig.GetProperties()));

        //Make expires
        DomainsExpires = moduleConfig.Domain.Where(x => x.Expires != TimeSpan.Zero).ToDictionary(x => x.Name, y => y.Expires);
        DomainsExpires.Add(string.Empty, moduleConfig.Expires);

        DomainsContentAsAttachment = moduleConfig.Domain.Where(x => x.ContentAsAttachment.HasValue).ToDictionary(x => x.Name, y => y.ContentAsAttachment.Value);
        DomainsContentAsAttachment.Add(string.Empty, moduleConfig.ContentAsAttachment ?? false);

        var settings = moduleConfig.DisabledEncryption ? new EncryptionSettings() : await encryptionSettingsHelper.LoadAsync();
        _crypt = encryptionFactory.GetCrypt(moduleConfig.Name, settings);
        DataStoreValidator = validator;
        return this;
    }

    public string GetPhysicalPath(string domain, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var pathMap = GetPath(domain);

        return (pathMap.PhysicalPath + EnsureLeadingSlash(path)).Replace('\\', '/');
    }

    public override Task<Stream> GetReadStreamAsync(string domain, string path)
    {
        return GetReadStreamAsync(domain, path, true);
    }

    private async Task<Stream> GetReadStreamAsync(string domain, string path, bool withDescription)
    {
        ArgumentNullException.ThrowIfNull(path);

        var target = GetTarget(domain, path);

        if (File.Exists(target))
        {
            return withDescription ? await _crypt.GetReadStreamAsync(target) : File.OpenRead(target);
        }

        throw new FileNotFoundException("File not found", Path.GetFullPath(target));
    }

    public override async Task<Stream> GetReadStreamAsync(string domain, string path, long offset)
    {
        ArgumentNullException.ThrowIfNull(path);

        var target = GetTarget(domain, path);

        if (File.Exists(target))
        {
            var stream = await _crypt.GetReadStreamAsync(target);
            if (0 < offset && stream.CanSeek)
            {
                stream.Seek(offset, SeekOrigin.Begin);
            }
            else if (0 < offset)
            {
                throw new InvalidOperationException("Seek stream is not impossible");
            }

            return stream;
        }

        throw new FileNotFoundException("File not found", Path.GetFullPath(target));
    }

    public override Task<Stream> GetReadStreamAsync(string domain, string path, long offset, long length)
    {
        return GetReadStreamAsync(domain, path, offset);
    }
    
    public override Task<Uri> SaveAsync(string domain, string path, Guid ownerId, Stream stream, string contentType, string contentDisposition)
    {
        return SaveAsync(domain, path, stream, ownerId);
    }
    public override Task<Uri> SaveAsync(string domain, string path, Stream stream, string contentType, string contentDisposition)
    {
        return SaveAsync(domain, path, stream);
    }

    public override Task<Uri> SaveAsync(string domain, string path, Stream stream, string contentEncoding, int cacheDays)
    {
        return SaveAsync(domain, path, stream);
    }
    private bool EnableQuotaCheck(string domain)
    {
        return (QuotaController != null) && !domain.EndsWith("_temp");
    }

    public override async Task<Uri> SaveAsync(string domain, string path, Stream stream)
    {
        return await SaveAsync(domain, path, stream, Guid.Empty);
    }
    public override async Task<Uri> SaveAsync(string domain, string path, Stream stream, Guid ownerId)
    {
        Logger.DebugSavePath(path);

        var (buffered, isNew) = await _tempStream.TryGetBufferedAsync(stream);
        try
        {
            if (EnableQuotaCheck(domain))
            {
                await QuotaController.QuotaUsedCheckAsync(buffered.Length, ownerId);
            }

            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(stream);

            //Try seek to start
            if (buffered.CanSeek)
            {
                buffered.Seek(0, SeekOrigin.Begin);
            }

            //Lookup domain
            var target = GetTarget(domain, path);
            CreateDirectory(target);
            //Copy stream

            //optimaze disk file copy
            long fslen;
            if (buffered is FileStream fileStream)
            {
                File.Copy(fileStream.Name, target, true);
                fslen = fileStream.Length;
            }
            else
            {
                await using var fs = File.Open(target, FileMode.Create);
                await buffered.CopyToAsync(fs);
                fslen = fs.Length;
            }

            await QuotaUsedAddAsync(domain, fslen, ownerId);

            await _crypt.EncryptFileAsync(target);

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

    public override Task<Uri> SaveAsync(string domain, string path, Stream stream, ACL acl)
    {
        return SaveAsync(domain, path, stream);
    }

    #region chunking
    public override Task<string> InitiateChunkedUploadAsync(string domain, string path)
    {
        var target = GetTarget(domain, path);
        CreateDirectory(target);
        return Task.FromResult(target);
    }

    public override async Task<string> UploadChunkAsync(string domain, string path, string uploadId, Stream stream, long defaultChunkSize, int chunkNumber, long chunkLength)
    {
        var target = GetTarget(domain, path + "chunks");

        if (!Directory.Exists(target))
        {
            Directory.CreateDirectory(target);
        }

        await using (var fs = new FileStream(Path.Combine(target, chunkNumber.ToString()), FileMode.Create))
        {
            await stream.CopyToAsync(fs);
        }

        return $"{chunkNumber}_{uploadId}";
    }

    public override async Task<Uri> FinalizeChunkedUploadAsync(string domain, string path, string uploadId, Dictionary<int, string> eTags)
    {
        var target = GetTarget(domain, path);

        var targetChunks = target + "chunks";
        if (!Directory.Exists(targetChunks))
        {
            throw new FileNotFoundException("file not found " + target);
        }

        var sortETags = eTags.OrderBy(e => e.Key);

        await using (var fs = new FileStream(target, FileMode.Create))
        {
            foreach (var eTag in sortETags)
            {
                await using var eTagFs = new FileStream(Path.Combine(targetChunks, eTag.Key.ToString()), FileMode.Open);
                await eTagFs.CopyToAsync(fs);
            }
        }
        Directory.Delete(Path.Combine(targetChunks), true);

        if (QuotaController != null)
        {
            var size = await _crypt.GetFileSizeAsync(target);
            await QuotaUsedAddAsync(domain, size);
        }

        await _crypt.EncryptFileAsync(target);

        return await GetUriAsync(domain, path);
    }

    public override Task AbortChunkedUploadAsync(string domain, string path, string uploadId)
    {
        var target = GetTarget(domain, path + "chunks");
        if (Directory.Exists(target))
        {
            Directory.Delete(target);
        }
        return Task.CompletedTask;
    }

    #endregion

    public override async Task DeleteAsync(string domain, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var target = GetTarget(domain, path);

        if (File.Exists(target))
        {
            var size = await _crypt.GetFileSizeAsync(target);
            File.Delete(target);

            await QuotaUsedDeleteAsync(domain, size);
        }
        else
        {
            throw new FileNotFoundException("file not found", target);
        }
    }

    public override async Task DeleteFilesAsync(string domain, List<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        foreach (var path in paths)
        {
            var target = GetTarget(domain, path);

            if (!File.Exists(target))
            {
                continue;
            }

            var size = await _crypt.GetFileSizeAsync(target);
            File.Delete(target);

            await QuotaUsedDeleteAsync(domain, size);
        }
    }

    public override async Task DeleteFilesAsync(string domain, string folderPath, string pattern, bool recursive)
    {
        await DeleteFilesAsync(domain, folderPath, pattern, recursive, Guid.Empty);
    }
    public override async Task DeleteFilesAsync(string domain, string folderPath, string pattern, bool recursive, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        //Return dirs
        var targetDir = GetTarget(domain, folderPath);
        if (Directory.Exists(targetDir))
        {
            var entries = Directory.GetFiles(targetDir, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            foreach (var entry in entries)
            {
                var size = await _crypt.GetFileSizeAsync(entry);
                File.Delete(entry);
                await QuotaUsedDeleteAsync(domain, size, ownerId);
            }
        }
        else
        {
            throw new DirectoryNotFoundException($"Directory '{targetDir}' not found");
        }
    }

    public override async Task DeleteFilesAsync(string domain, string folderPath, DateTime fromDate, DateTime toDate)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        //Return dirs
        var targetDir = GetTarget(domain, folderPath);
        if (Directory.Exists(targetDir))
        {
            var entries = Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories);
            foreach (var entry in entries)
            {
                var fileInfo = new FileInfo(entry);
                if (fileInfo.LastWriteTime >= fromDate && fileInfo.LastWriteTime <= toDate)
                {
                    var size = await _crypt.GetFileSizeAsync(entry);
                    File.Delete(entry);
                    await QuotaUsedDeleteAsync(domain, size);
                }
            }
        }
        else
        {
            throw new DirectoryNotFoundException($"Directory '{targetDir}' not found");
        }
    }

    public override Task MoveDirectoryAsync(string srcDomain, string srcDir, string newDomain, string newDir)
    {
        var target = GetTarget(srcDomain, srcDir);
        var newtarget = GetTarget(newDomain, newDir);
        var newtargetSub = newtarget.Remove(newtarget.LastIndexOf(Path.DirectorySeparatorChar));

        if (!Directory.Exists(newtargetSub))
        {
            Directory.CreateDirectory(newtargetSub);
        }

        Directory.Move(target, newtarget);

        return Task.CompletedTask;
    }

    public override async Task<Uri> MoveAsync(string srcDomain, string srcPath, string newDomain, string newPath, bool quotaCheckFileSize = true)
    {
       return await MoveAsync(srcDomain, srcPath, newDomain, newPath, Guid.Empty, quotaCheckFileSize);
    }

    public override async Task<Uri> MoveAsync(string srcDomain, string srcPath, string newDomain, string newPath, Guid ownerId, bool quotaCheckFileSize = true)
    {
        ArgumentNullException.ThrowIfNull(srcPath);
        ArgumentNullException.ThrowIfNull(newPath);

        var target = GetTarget(srcDomain, srcPath);
        var newtarget = GetTarget(newDomain, newPath);

        if (File.Exists(target))
        {
            if (!Directory.Exists(Path.GetDirectoryName(newtarget)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newtarget));
            }

            var flength = await _crypt.GetFileSizeAsync(target);

            //Delete file if exists
            if (File.Exists(newtarget))
            {
                File.Delete(newtarget);
            }

            File.Move(target, newtarget);

            await QuotaUsedDeleteAsync(srcDomain, flength);
            await QuotaUsedAddAsync(newDomain, flength, ownerId, quotaCheckFileSize);
        }
        else
        {
            throw new FileNotFoundException("File not found", Path.GetFullPath(target));
        }
        return await GetUriAsync(newDomain, newPath);
    }

    public override Task<bool> IsDirectoryAsync(string domain, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        //Return dirs
        var targetDir = GetTarget(domain, path);
        if (!string.IsNullOrEmpty(targetDir) && !targetDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            targetDir += Path.DirectorySeparatorChar;
        }
        return Task.FromResult(!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir));
    }

    public override async Task DeleteDirectoryAsync(string domain, string path)
    {
        await DeleteDirectoryAsync(domain, path, Guid.Empty);
    }

    public override async Task DeleteDirectoryAsync(string domain, string path, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(path);

        //Return dirs
        var targetDir = GetTarget(domain, path);

        if (string.IsNullOrEmpty(targetDir))
        {
            throw new Exception("targetDir is null");
        }

        if (!targetDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            targetDir += Path.DirectorySeparatorChar;
        }

        if (!Directory.Exists(targetDir))
        {
            return;
        }

        var entries = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories);
        var size = await entries.Where(r =>
        {
            if (QuotaController == null || string.IsNullOrEmpty(QuotaController.ExcludePattern))
            {
                return true;
            }
            return !Path.GetFileName(r).StartsWith(QuotaController.ExcludePattern);
        }
        ).ToAsyncEnumerable().SelectAwait(async r => await _crypt.GetFileSizeAsync(r)).SumAsync();

        var subDirs = Directory.GetDirectories(targetDir, "*", SearchOption.AllDirectories).ToList();
        subDirs.Reverse();
        subDirs.ForEach(subdir => Directory.Delete(subdir, true));

        Directory.Delete(targetDir, true);

        await QuotaUsedDeleteAsync(domain, size, ownerId);
    }

    public override async Task<long> GetFileSizeAsync(string domain, string path)
    {
        var target = GetTarget(domain, path);

        if (File.Exists(target))
        {
            return await _crypt.GetFileSizeAsync(target);
        }

        throw new FileNotFoundException("file not found " + target);
    }

    public override async Task<long> GetDirectorySizeAsync(string domain, string path)
    {
        var target = GetTarget(domain, path);

        if (Directory.Exists(target))
        {
            return await Directory.GetFiles(target, "*.*", SearchOption.AllDirectories)
                .ToAsyncEnumerable()
                .SelectAwait(async entry => await _crypt.GetFileSizeAsync(entry))
                .SumAsync();
        }

        throw new FileNotFoundException("directory not found " + target);
    }

    public override async Task<(Uri, string)> SaveTempAsync(string domain, Stream stream)
    {
        var assignedPath = Guid.NewGuid().ToString();
        return (await SaveAsync(domain, assignedPath, stream), assignedPath);
    }

    public override async Task<string> SavePrivateAsync(string domain, string path, Stream stream, DateTime expires)
    {
        var result = await SaveAsync(domain, path, stream);
        return result.ToString();
    }

    public override async Task DeleteExpiredAsync(string domain, string folderPath, TimeSpan oldThreshold)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        //Return dirs
        var targetDir = GetTarget(domain, folderPath);
        if (!Directory.Exists(targetDir))
        {
            return;
        }

        var entries = Directory.GetFiles(targetDir, "*.*", SearchOption.TopDirectoryOnly);
        foreach (var entry in entries)
        {
            var finfo = new FileInfo(entry);
            if ((DateTime.UtcNow - finfo.CreationTimeUtc) > oldThreshold)
            {
                var size = await _crypt.GetFileSizeAsync(entry);
                File.Delete(entry);

                await QuotaUsedDeleteAsync(domain, size);
            }
        }
    }

    public override string GetUploadForm(string domain, string directoryPath, string redirectTo, long maxUploadSize, string contentType, string contentDisposition, string submitLabel)
    {
        throw new NotSupportedException("This operation supported only on s3 storage");
    }

    public override string GetUploadUrl()
    {
        throw new NotSupportedException("This operation supported only on s3 storage");
    }

    public override string GetPostParams(string domain, string directoryPath, long maxUploadSize, string contentType, string contentDisposition)
    {
        throw new NotSupportedException("This operation supported only on s3 storage");
    }

    public override string GetRootDirectory(string domain)
    {
        var targetDir = GetTarget(domain , "");
        var dir = GetTarget("", "");
        if (!string.IsNullOrEmpty(targetDir) && !targetDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            return targetDir[dir.Length..].Trim('\\').Trim('/');
        }
        return string.Empty;
    }

    public override IAsyncEnumerable<string> ListDirectoriesRelativeAsync(string domain, string path, bool recursive)
    {
        ArgumentNullException.ThrowIfNull(path);

        //Return dirs
        var targetDir = GetTarget(domain, path);
        if (!string.IsNullOrEmpty(targetDir) && !targetDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            targetDir += Path.DirectorySeparatorChar;
        }

        if (Directory.Exists(targetDir))
        {
            var entries = Directory.EnumerateDirectories(targetDir, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Select(e => e[targetDir.Length..]);
            return entries.ToAsyncEnumerable();
        }
        return AsyncEnumerable.Empty<string>();
    }

    public override IAsyncEnumerable<string> ListFilesRelativeAsync(string domain, string path, string pattern, bool recursive)
    {
        ArgumentNullException.ThrowIfNull(path);

        //Return dirs
        var targetDir = GetTarget(domain, path);
        if (!string.IsNullOrEmpty(targetDir) && !targetDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            targetDir += Path.DirectorySeparatorChar;
        }

        if (Directory.Exists(targetDir))
        {
            var entries = Directory.EnumerateFiles(targetDir, pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                .Select(e=> e[targetDir.Length..]);
            return entries.ToAsyncEnumerable();
        }
        return AsyncEnumerable.Empty<string>();
    }

    public override Task<bool> IsFileAsync(string domain, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        //Return dirs
        var target = GetTarget(domain, path);
        var result = File.Exists(target);
        return Task.FromResult(result);
    }

    public override async Task<long> ResetQuotaAsync(string domain)
    {
        if (QuotaController != null)
        {
            var size = await GetUsedQuotaAsync(domain);
            await QuotaController.QuotaUsedSetAsync(Modulename, domain, DataList.GetData(domain), size);
        }

        return 0;
    }

    public override async Task<long> GetUsedQuotaAsync(string domain)
    {
        var target = GetTarget(domain, string.Empty);
        long size = 0;

        if (Directory.Exists(target))
        {
            var entries = Directory.GetFiles(target, "*.*", SearchOption.AllDirectories);
            size = await entries.ToAsyncEnumerable().SelectAwait(async entry => await _crypt.GetFileSizeAsync(entry)).SumAsync();
        }
        return size;
    }

    public override async Task<Uri> CopyAsync(string srcDomain, string srcpath, string newDomain, string newPath)
    {
        ArgumentNullException.ThrowIfNull(srcpath);
        ArgumentNullException.ThrowIfNull(newPath);

        var target = GetTarget(srcDomain, srcpath);
        var newtarget = GetTarget(newDomain, newPath);

        if (File.Exists(target))
        {
            if (!Directory.Exists(Path.GetDirectoryName(newtarget)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newtarget));
            }

            File.Copy(target, newtarget, true);

            var flength = await _crypt.GetFileSizeAsync(target);
            await QuotaUsedAddAsync(newDomain, flength);
        }
        else
        {
            throw new FileNotFoundException("File not found", Path.GetFullPath(target));
        }
        return await GetUriAsync(newDomain, newPath);
    }

    public override async Task CopyDirectoryAsync(string srcDomain, string srcdir, string newDomain, string newDir)
    {
        var target = GetTarget(srcDomain, srcdir);
        var newtarget = GetTarget(newDomain, newDir);

        var diSource = new DirectoryInfo(target);
        var diTarget = new DirectoryInfo(newtarget);

        await CopyAllAsync(diSource, diTarget, newDomain);
    }


    public Stream GetWriteStream(string domain, string path, FileMode fileMode = FileMode.Create)
    {
        ArgumentNullException.ThrowIfNull(path);

        var target = GetTarget(domain, path);
        CreateDirectory(target);

        return File.Open(target, fileMode);
    }

    public async Task DecryptAsync(string domain, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var target = GetTarget(domain, path);

        if (File.Exists(target))
        {
            await _crypt.DecryptFileAsync(target);
        }
        else
        {
            throw new FileNotFoundException("file not found", target);
        }
    }
    protected override Task<Uri> SaveWithAutoAttachmentAsync(string domain, string path, Guid ownerId, Stream stream, string attachmentFileName)
    {
        return SaveAsync(domain, path, stream, ownerId);
    }
    protected override Task<Uri> SaveWithAutoAttachmentAsync(string domain, string path, Stream stream, string attachmentFileName)
    {
        return SaveAsync(domain, path, stream);
    }

    private async Task CopyAllAsync(DirectoryInfo source, DirectoryInfo target, string newdomain)
    {
        // Check if the target directory exists, if not, create it.
        if (!Directory.Exists(target.FullName))
        {
            Directory.CreateDirectory(target.FullName);
        }

        // Copy each file into it's new directory.
        foreach (var fi in source.GetFiles())
        {
            var fp = CrossPlatform.PathCombine(target.ToString(), fi.Name);
            fi.CopyTo(fp, true);
            var size = await _crypt.GetFileSizeAsync(fp);
            await QuotaUsedAddAsync(newdomain, size);
        }

        // Copy each subdirectory using recursion.
        foreach (var diSourceSubDir in source.GetDirectories())
        {
            var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            await CopyAllAsync(diSourceSubDir, nextTargetSubDir, newdomain);
        }
    }

    private MappedPath GetPath(string domain)
    {
        if (domain != null && _mappedPaths.TryGetValue(domain, out var value))
        {
            return value;
        }

        return _mappedPaths[string.Empty].AppendDomain(domain);
    }

    private static void CreateDirectory(string target)
    {
        var targetDirectory = Path.GetDirectoryName(target);
        if (!Directory.Exists(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }
    }

    private string GetTarget(string domain, string path)
    {
        var pathMap = GetPath(domain);
        //Build Dir
        var target = CrossPlatform.PathCombine(pathMap.PhysicalPath, PathUtils.Normalize(path));
        ValidatePath(target);

        return target;
    }

    private static void ValidatePath(string target)
    {
        if (Path.GetDirectoryName(target).IndexOfAny(Path.GetInvalidPathChars()) != -1 ||
            Path.GetFileName(target).IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
        {
            //Throw
            throw new ArgumentException("bad path");
        }
    }

    public async Task EncryptAsync(string domain, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var target = GetTarget(domain, path);

        if (File.Exists(target))
        {
            await _crypt.EncryptFileAsync(target);
        }
        else
        {
            throw new FileNotFoundException("file not found", target);
        }
    }

    public override Task<string> GetFileEtagAsync(string domain, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var target = GetTarget(domain, path);
        var lastModificationDate = File.Exists(target) ? File.GetLastWriteTimeUtc(target) : throw new FileNotFoundException("File not found" + target);
        var etag = '"' + lastModificationDate.Ticks.ToString("X8", CultureInfo.InvariantCulture) + '"';

        return Task.FromResult(etag);
    }
}
