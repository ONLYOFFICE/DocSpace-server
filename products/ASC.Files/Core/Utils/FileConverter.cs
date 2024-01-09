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

using Microsoft.AspNetCore.Http.Extensions;

using Serializer = ProtoBuf.Serializer;

namespace ASC.Web.Files.Utils;

[Singleton]
public class FileConverterQueue(IDistributedCache distributedCache)
{
    private readonly object _locker = new();
    private const string Cache_key_prefix = "asc_file_converter_queue_";

    public void Add<T>(File<T> file,
                        string password,
                        int tenantId,
                        IAccount account,
                        bool deleteAfter,
                        string url,
                        string serverRootPath,
                        ExternalShareData externalShareData = null)
    {
        lock (_locker)
        {
            var cacheKey = GetCacheKey<T>();
            var task = PeekTask(file, cacheKey);

            if (task != null)
            {
                if (task.Progress != 100)
                {
                    return;
                }

                Dequeue(task, cacheKey);
            }

            var queueResult = new FileConverterOperationResult
            {
                Source = JsonSerializer.Serialize(new { id = file.Id, version = file.Version }),
                OperationType = FileOperationType.Convert,
                Error = string.Empty,
                Progress = 0,
                Result = string.Empty,
                Processed = "",
                Id = string.Empty,
                TenantId = tenantId,
                Account = account.ID,
                Delete = deleteAfter,
                StartDateTime = DateTime.UtcNow,
                Url = url,
                Password = password,
                ServerRootPath = serverRootPath,
                ExternalShareData = externalShareData != null ? JsonSerializer.Serialize(externalShareData) : null
            };

            Enqueue(queueResult, cacheKey);
        }
    }

    private void Enqueue(FileConverterOperationResult val, string cacheKey)
    {
        var fromCache = LoadFromCache(cacheKey).ToList();

        fromCache.Add(val);

        SaveToCache(fromCache, cacheKey);
    }

    private void Dequeue(FileConverterOperationResult val, string cacheKey)
    {
        var fromCache = LoadFromCache(cacheKey).ToList();

        fromCache.Remove(val);

        SaveToCache(fromCache, cacheKey);
    }

    private FileConverterOperationResult PeekTask<T>(File<T> file, string cacheKey)
    {
        var exist = LoadFromCache(cacheKey);

        return exist.LastOrDefault(x =>
        {
            var fileId = JsonDocument.Parse(x.Source).RootElement.GetProperty("id").Deserialize<T>();

            return String.Compare(file.Id.ToString(), fileId.ToString(), StringComparison.OrdinalIgnoreCase) == 0;
        });
    }

    internal bool IsConverting<T>(File<T> file, string cacheKey)
    {
        var result = PeekTask(file, cacheKey);

        return result != null && result.Progress != 100 && string.IsNullOrEmpty(result.Error);
    }


    public IEnumerable<FileConverterOperationResult> GetAllTask<T>()
    {
        var cacheKey = GetCacheKey<T>();
        var queueTasks = LoadFromCache(cacheKey);

        queueTasks = DeleteOrphanCacheItem(queueTasks, cacheKey);

        return queueTasks;
    }

    public void SetAllTask<T>(IEnumerable<FileConverterOperationResult> queueTasks)
    {
        var cacheKey = GetCacheKey<T>();
        SaveToCache(queueTasks, cacheKey);
    }


    public async Task<FileConverterOperationResult> GetStatusAsync<T>(KeyValuePair<File<T>, bool> pair, FileSecurity fileSecurity)
    {
        var cacheKey = GetCacheKey<T>();
        var file = pair.Key;
        var operation = PeekTask(file, cacheKey);

        if (operation != null && (pair.Value || await fileSecurity.CanReadAsync(file)))
        {
            if (operation.Progress == 100)
            {
                Dequeue(operation, cacheKey);
            }

            return operation;
        }

        return null;
    }


    public async Task<string> FileJsonSerializerAsync<T>(EntryStatusManager EntryManager, File<T> file, string folderTitle)
    {
        if (file == null)
        {
            return string.Empty;
        }

        await EntryManager.SetFileStatusAsync(file);

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyProperties = true,
            WriteIndented = false
        };

        return JsonSerializer.Serialize(
            new FileJsonSerializerData<T>
            {
                Id = file.Id,
                Title = file.Title,
                Version = file.Version,
                FolderID = file.ParentId,
                FolderTitle = folderTitle ?? "",
                FileJson = JsonSerializer.Serialize(file, options)
            }, options);
    }

    private bool IsOrphanCacheItem(FileConverterOperationResult x)
    {
        return !string.IsNullOrEmpty(x.Processed)
                           && (x.Progress == 100 && DateTime.UtcNow - x.StopDateTime > TimeSpan.FromMinutes(1) ||
                               DateTime.UtcNow - x.StopDateTime > TimeSpan.FromMinutes(10));
    }

    private IEnumerable<FileConverterOperationResult> DeleteOrphanCacheItem(IEnumerable<FileConverterOperationResult> queueTasks, string cacheKey)
    {
        var listTasks = queueTasks.ToList();

        if (listTasks.RemoveAll(IsOrphanCacheItem) > 0)
        {
            SaveToCache(listTasks, cacheKey);
        }

        return listTasks;
    }

    private void SaveToCache(IEnumerable<FileConverterOperationResult> queueTasks, string cacheKey)
    {
        if (!queueTasks.Any())
        {
            distributedCache.Remove(cacheKey);

            return;
        }

        using var ms = new MemoryStream();

        Serializer.Serialize(ms, queueTasks);

        distributedCache.Set(cacheKey, ms.ToArray(), new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(15)
        });
    }

    internal static string GetCacheKey<T>()
    {
        return $"{Cache_key_prefix}_{typeof(T).Name}".ToLowerInvariant();
    }

    private IEnumerable<FileConverterOperationResult> LoadFromCache(string cacheKey)
    {
        var serializedObject = distributedCache.Get(cacheKey);

        if (serializedObject == null)
        {
            return new List<FileConverterOperationResult>();
        }

        using var ms = new MemoryStream(serializedObject);

        return Serializer.Deserialize<List<FileConverterOperationResult>>(ms);
    }
}

public class FileJsonSerializerData<T>
{
    public T Id { get; set; }
    public string Title { get; set; }
    public int Version { get; set; }
    public T FolderID { get; set; }
    public string FolderTitle { get; set; }
    public string FileJson { get; set; }
}

[Scope(Additional = typeof(FileConverterExtension))]
public class FileConverter(FileUtility fileUtility,
    FilesLinkUtility filesLinkUtility,
    IDaoFactory daoFactory,
    SetupInfo setupInfo,
    PathProvider pathProvider,
    FileSecurity fileSecurity,
    FileMarker fileMarker,
    TenantManager tenantManager,
    AuthContext authContext,
    EntryManager entryManager,
    FilesSettingsHelper filesSettingsHelper,
    GlobalFolderHelper globalFolderHelper,
    FilesMessageService filesMessageService,
    FileShareLink fileShareLink,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    FileTrackerHelper fileTracker,
    BaseCommonLinkUtility baseCommonLinkUtility,
    EntryStatusManager entryStatusManager,
    IServiceProvider serviceProvider,
    IHttpClientFactory clientFactory,
    SocketManager socketManager,
    FileConverterQueue fileConverterQueue,
    ExternalShare externalShare)
{
    private readonly IHttpContextAccessor _httpContextAccesor;

    public FileConverter(
        FileUtility fileUtility,
        FilesLinkUtility filesLinkUtility,
        IDaoFactory daoFactory,
        SetupInfo setupInfo,
        PathProvider pathProvider,
        FileSecurity fileSecurity,
        FileMarker fileMarker,
        TenantManager tenantManager,
        AuthContext authContext,
        EntryManager entryManager,
        FilesSettingsHelper filesSettingsHelper,
        GlobalFolderHelper globalFolderHelper,
        FilesMessageService filesMessageService,
        FileShareLink fileShareLink,
        DocumentServiceHelper documentServiceHelper,
        DocumentServiceConnector documentServiceConnector,
        FileTrackerHelper fileTracker,
        BaseCommonLinkUtility baseCommonLinkUtility,
        EntryStatusManager entryStatusManager,
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccesor,
        IHttpClientFactory clientFactory,
        SocketManager socketManager,
        FileConverterQueue fileConverterQueue, ExternalShare externalShare)
        : this(fileUtility, filesLinkUtility, daoFactory, setupInfo, pathProvider, fileSecurity,
              fileMarker, tenantManager, authContext, entryManager, filesSettingsHelper,
              globalFolderHelper, filesMessageService, fileShareLink, documentServiceHelper, documentServiceConnector, fileTracker,
              baseCommonLinkUtility, entryStatusManager, serviceProvider, clientFactory, socketManager, fileConverterQueue, externalShare)
    {
        _httpContextAccesor = httpContextAccesor;
    }

    public bool EnableAsUploaded => fileUtility.ExtsMustConvert.Count > 0 && !string.IsNullOrEmpty(filesLinkUtility.DocServiceConverterUrl);

    public bool MustConvert<T>(File<T> file)
    {
        if (file == null)
        {
            return false;
        }

        var ext = FileUtility.GetFileExtension(file.Title);

        return fileUtility.ExtsMustConvert.Contains(ext);
    }

    public async Task<bool> EnableConvertAsync<T>(File<T> file, string toExtension)
    {
        if (file == null || string.IsNullOrEmpty(toExtension))
        {
            return false;
        }

        if (file.Encrypted)
        {
            return false;
        }

        var fileExtension = file.ConvertedExtension;
        if (fileExtension.Trim('.').Equals(toExtension.Trim('.'), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fileExtension = FileUtility.GetFileExtension(file.Title);
        if (fileUtility.InternalExtension.ContainsValue(toExtension))
        {
            return true;
        }

        return (await fileUtility.GetExtsConvertibleAsync()).ContainsKey(fileExtension) && (await fileUtility.GetExtsConvertibleAsync())[fileExtension].Contains(toExtension);
    }

    public Task<Stream> ExecAsync<T>(File<T> file)
    {
        return ExecAsync(file, fileUtility.GetInternalExtension(file.Title));
    }

    public async Task<Stream> ExecAsync<T>(File<T> file, string toExtension, string password = null)
    {
        if (!await EnableConvertAsync(file, toExtension))
        {
            var fileDao = daoFactory.GetFileDao<T>();

            return await fileDao.GetFileStreamAsync(file);
        }

        if (file.ContentLength > setupInfo.AvailableFileSize)
        {
            throw new Exception(string.Format(FilesCommonResource.ErrorMassage_FileSizeConvert, FileSizeComment.FilesSizeToString(setupInfo.AvailableFileSize)));
        }

        var fileUri = await pathProvider.GetFileStreamUrlAsync(file);
        var docKey = await documentServiceHelper.GetDocKeyAsync(file);
        fileUri = await documentServiceConnector.ReplaceCommunityAdressAsync(fileUri);

        var folderDao = daoFactory.GetFolderDao<T>();
        var (watermarkSettings, room) = await DocSpaceHelper.GetWatermarkSettings(file, folderDao);
        var options = documentServiceHelper.GetOptions(watermarkSettings, room);

        var uriTuple = await documentServiceConnector.GetConvertedUriAsync(fileUri, file.ConvertedExtension, toExtension, docKey, password, CultureInfo.CurrentUICulture.Name, null, null, options, false);
        var convertUri = uriTuple.ConvertedDocumentUri;
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(convertUri)
        };

        var httpClient = clientFactory.CreateClient();
        var response = await httpClient.SendAsync(request);

        return new ResponseStream(response);
    }

    public async Task<FileOperationResult> ExecSynchronouslyAsync<T>(File<T> file, string doc)
    {
        var fileDao = daoFactory.GetFileDao<T>();

        if (!await fileSecurity.CanReadAsync(file))
        {
            (var readLink, file, _) = await fileShareLink.CheckAsync(doc, true, fileDao);
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file), FilesCommonResource.ErrorMassage_FileNotFound);
            }
            if (!readLink)
            {
                throw new SecurityException(FilesCommonResource.ErrorMassage_SecurityException_ReadFile);
            }
        }

        var fileUri = await pathProvider.GetFileStreamUrlAsync(file);
        var fileExtension = file.ConvertedExtension;
        var toExtension = fileUtility.GetInternalExtension(file.Title);
        var docKey = await documentServiceHelper.GetDocKeyAsync(file);

        fileUri = await documentServiceConnector.ReplaceCommunityAdressAsync(fileUri);

        var uriTuple = await documentServiceConnector.GetConvertedUriAsync(fileUri, fileExtension, toExtension, docKey, null, CultureInfo.CurrentUICulture.Name, null, null, null, false);
        var convertUri = uriTuple.ConvertedDocumentUri;
        var convertType = uriTuple.convertedFileType;

        var operationResult = new FileConverterOperationResult
        {
            Source = JsonSerializer.Serialize(new { id = file.Id, version = file.Version }),
            OperationType = FileOperationType.Convert,
            Error = string.Empty,
            Progress = 0,
            Result = string.Empty,
            Processed = "",
            Id = string.Empty,
            TenantId = await tenantManager.GetCurrentTenantIdAsync(),
            Account = authContext.CurrentAccount.ID,
            Delete = false,
            StartDateTime = DateTime.UtcNow,
            Url = _httpContextAccesor?.HttpContext?.Request.GetDisplayUrl(),
            Password = null,
            ServerRootPath = baseCommonLinkUtility.ServerRootPath,
            ExternalShareData = await externalShare.GetLinkIdAsync() != Guid.Empty ? JsonSerializer.Serialize(externalShare.GetCurrentShareDataAsync()) : null
        };

        var operationResultError = string.Empty;

        var newFile = await SaveConvertedFileAsync(file, convertUri, convertType);
        if (newFile != null)
        {
            await socketManager.CreateFileAsync(file);
            var folderDao = daoFactory.GetFolderDao<T>();
            var folder = await folderDao.GetFolderAsync(newFile.ParentId);
            var folderTitle = await fileSecurity.CanReadAsync(folder) ? folder.Title : null;
            operationResult.Result = await fileConverterQueue.FileJsonSerializerAsync(entryStatusManager, newFile, folderTitle);
        }

        operationResult.Progress = 100;
        operationResult.StopDateTime = DateTime.UtcNow;
        operationResult.Processed = "1";

        if (!string.IsNullOrEmpty(operationResultError))
        {
            operationResult.Error = operationResultError;
        }

        return operationResult;
    }

    public async Task ExecAsynchronouslyAsync<T>(File<T> file, bool deleteAfter, string password = null)
    {
        if (!MustConvert(file))
        {
            throw new ArgumentException(FilesCommonResource.ErrorMassage_NotSupportedFormat);
        }
        if (!string.IsNullOrEmpty(file.ConvertedType) || fileUtility.InternalExtension.ContainsValue(FileUtility.GetFileExtension(file.Title)))
        {
            return;
        }

        await fileMarker.RemoveMarkAsNewAsync(file);

        fileConverterQueue.Add(file, password, (await tenantManager.GetCurrentTenantAsync()).Id, authContext.CurrentAccount, deleteAfter, _httpContextAccesor?.HttpContext?.Request.GetDisplayUrl(),
            baseCommonLinkUtility.ServerRootPath, await externalShare.GetLinkIdAsync() != Guid.Empty ? await externalShare.GetCurrentShareDataAsync() : null);
    }

    public bool IsConverting<T>(File<T> file)
    {
        if (!MustConvert(file) || !string.IsNullOrEmpty(file.ConvertedType))
        {
            return false;
        }

        return fileConverterQueue.IsConverting(file, FileConverterQueue.GetCacheKey<T>());
    }

    public async IAsyncEnumerable<FileOperationResult> GetStatusAsync<T>(IEnumerable<KeyValuePair<File<T>, bool>> filesPair)
    {
        foreach (var pair in filesPair)
        {
            var r = await fileConverterQueue.GetStatusAsync(pair, fileSecurity);

            if (r != null)
            {
                yield return r;
            }
        }
    }

    public async Task<File<T>> SaveConvertedFileAsync<T>(File<T> file, string convertedFileUrl, string convertedFileType)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();
        File<T> newFile = null;
        var markAsTemplate = false;
        var isNewFile = false;
        var newFileTitle = FileUtility.ReplaceFileExtension(file.Title, convertedFileType);

        if (!filesSettingsHelper.StoreOriginalFiles && await fileSecurity.CanEditAsync(file))
        {
            newFile = (File<T>)file.Clone();
            newFile.Version++;
            markAsTemplate = FileUtility.ExtsTemplate.Contains(FileUtility.GetFileExtension(file.Title), StringComparer.CurrentCultureIgnoreCase)
                          && fileUtility.ExtsWebTemplate.Contains(FileUtility.GetFileExtension(newFileTitle), StringComparer.CurrentCultureIgnoreCase);
        }
        else
        {
            var folderId = await globalFolderHelper.GetFolderMyAsync<T>();

            var parent = await folderDao.GetFolderAsync(file.ParentId);
            if (parent != null
                && await fileSecurity.CanCreateAsync(parent))
            {
                folderId = parent.Id;
            }

            if (Equals(folderId, 0))
            {
                throw new SecurityException(FilesCommonResource.ErrorMassage_FolderNotFound);
            }

            if (filesSettingsHelper.UpdateIfExist && (parent != null && !folderId.Equals(parent.Id) || !file.ProviderEntry))
            {
                newFile = await fileDao.GetFileAsync(folderId, newFileTitle);
                if (newFile != null && await fileSecurity.CanEditAsync(newFile) && !await entryManager.FileLockedForMeAsync(newFile.Id) && !fileTracker.IsEditing(newFile.Id))
                {
                    newFile.Version++;
                    newFile.VersionGroup++;
                }
                else
                {
                    newFile = null;
                }
            }

            if (newFile == null)
            {
                newFile = serviceProvider.GetService<File<T>>();
                newFile.ParentId = folderId;
                isNewFile = true;
            }
        }

        newFile.Title = newFileTitle;
        newFile.ConvertedType = null;
        newFile.Comment = string.Format(FilesCommonResource.CommentConvert, file.Title);
        newFile.ThumbnailStatus = Thumbnail.Waiting;

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(convertedFileUrl)
        };

        var httpClient = clientFactory.CreateClient();

        try
        {
            using var response = await httpClient.SendAsync(request);
            await using var convertedFileStream = new ResponseStream(response);
            newFile.ContentLength = convertedFileStream.Length;
            newFile = await fileDao.SaveFileAsync(newFile, convertedFileStream);

            if (!isNewFile)
            {
                await socketManager.UpdateFileAsync(newFile);
            }
            else
            {
                await socketManager.CreateFileAsync(newFile);
            }
        }
        catch (HttpRequestException e)
        {
            var errorString = $"HttpRequestException: {e.StatusCode}";

            if (e.StatusCode != HttpStatusCode.NotFound)
            {
                    errorString += $" Error {e.Message}";
                }

            throw new Exception(errorString);
        }

        await filesMessageService.SendAsync(MessageAction.FileConverted, newFile, MessageInitiator.DocsService, newFile.Title);

        var linkDao = daoFactory.GetLinkDao();
        await linkDao.DeleteAllLinkAsync(file.Id.ToString());

        await fileMarker.MarkAsNewAsync(newFile);

        var tagDao = daoFactory.GetTagDao<T>();
        var tags = await tagDao.GetTagsAsync(file.Id, FileEntryType.File, TagType.System).ToListAsync();
        if (tags.Count > 0)
        {
            tags.ForEach(r => r.EntryId = newFile.Id);
            await tagDao.SaveTagsAsync(tags);
        }

        if (markAsTemplate)
        {
            await tagDao.SaveTagsAsync(Tag.Template(authContext.CurrentAccount.ID, newFile));
        }

        return newFile;
    }
}

public static class FileConverterExtension
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<FileConverterQueue>();
    }
}