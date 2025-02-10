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

namespace ASC.Web.Files.Services.WCFService.FileOperations;

[ProtoContract]
public record FileDownloadOperationData<T> : FileOperationData<T>
{
    public FileDownloadOperationData()
    {
        
    }
    
    public FileDownloadOperationData(IEnumerable<T> folders,
        IEnumerable<FilesDownloadOperationItem<T>> filesDownload,
        int tenantId,
        IDictionary<string, string> headers,
        ExternalSessionSnapshot sessionSnapshot,
        string baseUri =  null,
        bool holdResult = true) : base(folders, filesDownload.Select(f => f.Id).ToList(), tenantId, headers, sessionSnapshot, holdResult)
    {
        FilesDownload = filesDownload;
        BaseUri = baseUri;
    }

    [ProtoMember(7)]
    public IEnumerable<FilesDownloadOperationItem<T>> FilesDownload { get; init; }
    
    [ProtoMember(8)]
    public string BaseUri { get; init; }
}

public record FilesDownloadOperationItem<T>(T Id, string Ext, string Password);

[Transient]
public class FileDownloadOperation(IServiceProvider serviceProvider) : ComposeFileOperation<FileDownloadOperationData<string>, FileDownloadOperationData<int>>(serviceProvider)
{    
    protected override FileOperationType FileOperationType { get => FileOperationType.Download; }
    
    public override async Task RunJob(DistributedTask distributedTask, CancellationToken cancellationToken)
    {
        DaoOperation = new FileDownloadOperation<int>(_serviceProvider, Data);
        ThirdPartyOperation = new FileDownloadOperation<string>(_serviceProvider, ThirdPartyData);

        await base.RunJob(distributedTask, cancellationToken);

        await using var scope = await ThirdPartyOperation.CreateScopeAsync();
        var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
        var instanceCrypto = scope.ServiceProvider.GetRequiredService<InstanceCrypto>();
        var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
        var externalShare = scope.ServiceProvider.GetRequiredService<ExternalShare>();
        var globalStore = scope.ServiceProvider.GetService<GlobalStore>();
        var filesLinkUtility = scope.ServiceProvider.GetService<FilesLinkUtility>();
        var tempStream = scope.ServiceProvider.GetService<TempStream>();
        var stream = tempStream.Create();

        var baseUri = Data?.BaseUri ?? ThirdPartyData?.BaseUri;
        if (!string.IsNullOrEmpty(baseUri))
        {
            var commonLinkUtility = scope.ServiceProvider.GetRequiredService<CommonLinkUtility>();
            commonLinkUtility.ServerUri = baseUri;
        }

        var thirdPartyOperation = ThirdPartyOperation as FileDownloadOperation<string>;
        var daoOperation = DaoOperation as FileDownloadOperation<int>;
        
        var thirdPartyFileOnly = thirdPartyOperation.Files.Count == 1 && thirdPartyOperation.Folders.Count == 0;
        var daoFileOnly = daoOperation.Files.Count == 1 && daoOperation.Folders.Count == 0;
        var compress = !((thirdPartyFileOnly || daoFileOnly) && (thirdPartyFileOnly != daoFileOnly));

        string archiveExtension;
        
        if (compress)
        {           
            using (var zip = scope.ServiceProvider.GetService<CompressToArchive>())
            {
                archiveExtension = await zip.GetArchiveExtension();
            }
            
            await thirdPartyOperation.CompressToZipAsync(stream, scope);
            await daoOperation.CompressToZipAsync(stream, scope);
        }
        else
        {
            if (thirdPartyFileOnly)
            {
                archiveExtension = await thirdPartyOperation.GetFileAsync(stream, scope);
            }
            else
            {
                archiveExtension = await daoOperation.GetFileAsync(stream, scope);
            }
        }

        if (stream != null)
        {
            stream.Position = 0;
            string fileName;

            var thirdPartyFolderOnly = thirdPartyOperation.Folders.Count == 1 && thirdPartyOperation.Files.Count == 0;
            var daoFolderOnly = daoOperation.Folders.Count == 1 && daoOperation.Files.Count == 0;
            if ((thirdPartyFolderOnly || daoFolderOnly) && (thirdPartyFolderOnly != daoFolderOnly))
            {
                fileName = $@"{(thirdPartyFolderOnly ?
                    (await daoFactory.GetFolderDao<string>().GetFolderAsync(thirdPartyOperation.Folders[0])).Title :
                    (await daoFactory.GetFolderDao<int>().GetFolderAsync(daoOperation.Folders[0])).Title)}{archiveExtension}";
            }
            else if (!compress)
            {
                fileName = archiveExtension;
            }
            else
            {
                fileName = $@"{(tenantManager.GetCurrentTenant()).Alias.ToLower()}-{FileConstant.DownloadTitle}-{DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}{archiveExtension}";
            }

            var store = await globalStore.GetStoreAsync();
            string path;
            string sessionKey = null;

            var isAuthenticated = _principal?.Identity is IAccount { IsAuthenticated: true };

            if (isAuthenticated)
            {
                path = $@"{((IAccount)_principal.Identity).ID}\{fileName}";
            }
            else
            {
                var sessionId = await externalShare.GetSessionIdAsync();
                var linkId = await externalShare.GetLinkIdAsync();

                if (sessionId == Guid.Empty || linkId == Guid.Empty)
                {
                    throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
                }

                path = $@"{linkId}\{sessionId}\{fileName}";
                sessionKey = await externalShare.CreateDownloadSessionKeyAsync();
            }

            if (await store.IsFileAsync(FileConstant.StorageDomainTmp, path))
            {
                await store.DeleteAsync(FileConstant.StorageDomainTmp, path);
            }

            await store.SaveAsync(
                FileConstant.StorageDomainTmp,
                path,
                stream,
                MimeMapping.GetMimeMapping(path),
                "attachment; filename=\"" + Uri.EscapeDataString(fileName) + "\"");

            this[Res] = $"{filesLinkUtility.FileHandlerPath}?{FilesLinkUtility.Action}=bulk&filename={Uri.EscapeDataString(await instanceCrypto.EncryptAsync(fileName))}";

            if (!isAuthenticated)
            {
                this[Res] += $"&session={HttpUtility.UrlEncode(sessionKey)}";
            }
        }

        this[Finish] = true;
        await PublishChanges();
    }

    protected override async Task PublishChanges(DistributedTask task)
    {
        var thirdpartyTask = ThirdPartyOperation;
        var daoTask = DaoOperation;

        var error1 = thirdpartyTask[Err];
        var error2 = daoTask[Err];

        if (!string.IsNullOrEmpty(error1))
        {
            this[Err] = error1;
        }
        else if (!string.IsNullOrEmpty(error2))
        {
            this[Err] = error2;
        }

        this[Process] = thirdpartyTask[Process] + daoTask[Process];

        var progressSteps = ThirdPartyOperation.Total + DaoOperation.Total + 1;

        var progress = (int)(this[Process] / (double)progressSteps * 100);

        this[Progress] = progress < 100 ? progress : 100;
        await PublishChanges();
    }
}

class FileDownloadOperation<T> : FileOperation<FileDownloadOperationData<T>, T>
{
    private readonly Dictionary<T, (string, string)> _files;
    private readonly IDictionary<string, StringValues> _headers;
    private ItemNameValueCollection<T> _entriesPathId;

    public FileDownloadOperation(IServiceProvider serviceProvider, FileDownloadOperationData<T> fileDownloadOperationData)
        : base(serviceProvider, fileDownloadOperationData)
    {
        _files = fileDownloadOperationData.FilesDownload?.ToDictionary(r => r.Id, r => (r.Ext, r.Password)) ?? new Dictionary<T, (string, string)>();
        _headers = fileDownloadOperationData.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value));
        this[OpType] = (int)FileOperationType.Download;
    }

    protected override async Task DoJob(AsyncServiceScope serviceScope)
    {
        if (Files.Count == 0 && Folders.Count == 0)
        {
            return;
        }

        (_entriesPathId, var filesForSend, var folderForSend) = await GetEntriesPathIdAsync(serviceScope);

        if (_entriesPathId == null || _entriesPathId.Count == 0)
        {
            if (Files.Count > 0)
            {
                throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
            }

            throw new DirectoryNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        Total = _entriesPathId.Count + 1;

        ReplaceLongPath(_entriesPathId);

        Total = _entriesPathId.Count;

        await PublishChanges();

        var filesMessageService = serviceScope.ServiceProvider.GetRequiredService<FilesMessageService>();
        foreach (var file in filesForSend)
        {
            var key = file.Id;
            if (_files.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value.Item1))
            {
                await filesMessageService.SendAsync(MessageAction.FileDownloadedAs, file, _headers, file.Title, value.Item1);
            }
            else
            {
                await filesMessageService.SendAsync(MessageAction.FileDownloaded, file, _headers, file.Title);
            }
        }

        foreach (var folder in folderForSend)
        {
            await filesMessageService.SendAsync(MessageAction.FolderDownloaded, folder, _headers, folder.Title);
        }
    }

    private async Task<ItemNameValueCollection<T>> ExecPathFromFileAsync(IServiceScope scope, FileEntry<T> file, string path)
    {
        var fileMarker = scope.ServiceProvider.GetService<FileMarker>();
        await fileMarker.RemoveMarkAsNewAsync(file);

        var folderDao = scope.ServiceProvider.GetService<IDaoFactory>().GetFolderDao<T>();
        var fileUtility = scope.ServiceProvider.GetService<FileUtility>();
        var title = file.Title;

        var fileExt = FileUtility.GetFileExtension(title);
        var extsConvertible = await fileUtility.GetExtsConvertibleAsync();
        var convertible = extsConvertible.TryGetValue(fileExt, out _);

        if (convertible && await DocSpaceHelper.IsWatermarkEnabled(file, folderDao))
        {
            _files[file.Id] = (FileUtility.WatermarkedDocumentExt, _files.TryGetValue(file.Id, out var value) ? value.Item2 : default);
        }

        if (_files.TryGetValue(file.Id, out var convertToExt) && !string.IsNullOrEmpty(convertToExt.Item1))
        {
            title = FileUtility.ReplaceFileExtension(title, convertToExt.Item1);
        }

        var entriesPathId = new ItemNameValueCollection<T>();
        entriesPathId.Add(path + title, file.Id);

        return entriesPathId;
    }

    private async Task<(ItemNameValueCollection<T>, IEnumerable<FileEntry<T>>, IEnumerable<FileEntry<T>>)> GetEntriesPathIdAsync(AsyncServiceScope scope)
    {
        var fileMarker = scope.ServiceProvider.GetService<FileMarker>();
        var entriesPathId = new ItemNameValueCollection<T>();
        IEnumerable<FileEntry<T>> filesForSend = new List<File<T>>();
        IEnumerable<FileEntry<T>> folderForSend = new List<Folder<T>>();

        if (0 < Files.Count)
        {
            filesForSend = await FilesSecurity.FilterDownloadAsync(FileDao.GetFilesAsync(Files)).ToListAsync();

            foreach (var file in filesForSend)
            {
                entriesPathId.Add(await ExecPathFromFileAsync(scope, file, string.Empty));
            }
        }
        if (0 < Folders.Count)
        {
            folderForSend = await FilesSecurity.FilterDownloadAsync(FolderDao.GetFoldersAsync(Folders)).ToListAsync();

            foreach (var folder in folderForSend)
            {
                await fileMarker.RemoveMarkAsNewAsync(folder);
            }

            var filesInFolder = await GetFilesInFoldersAsync(scope, folderForSend.Select(x => x.Id), string.Empty);
            entriesPathId.Add(filesInFolder);
        }

        if (Folders.Count == 1 && Files.Count == 0)
        {
            var entriesPathIdWithoutRoot = new ItemNameValueCollection<T>();

            foreach (var path in entriesPathId.AllKeys)
            {
                entriesPathIdWithoutRoot.Add(path.Remove(0, path.IndexOf('/') + 1), entriesPathId[path]);
            }

            return (entriesPathIdWithoutRoot, filesForSend, folderForSend);
        }

        return (entriesPathId, filesForSend, folderForSend);
    }
    private async Task<ItemNameValueCollection<T>> GetFilesInFoldersAsync(IServiceScope scope, IEnumerable<T> folderIds, string path)
    {
        var fileMarker = scope.ServiceProvider.GetService<FileMarker>();

        CancellationToken.ThrowIfCancellationRequested();

        var entriesPathId = new ItemNameValueCollection<T>();

        foreach (var folderId in folderIds)
        {
            CancellationToken.ThrowIfCancellationRequested();

            var folder = await FolderDao.GetFolderAsync(folderId);
            if (folder == null || !await FilesSecurity.CanDownloadAsync(folder))
            {
                continue;
            }


            var folderPath = path + folder.Title + "/";
            entriesPathId.Add(folderPath, default(T));

            var files = FilesSecurity.FilterDownloadAsync(FileDao.GetFilesAsync(folder.Id, null, FilterType.None, false, Guid.Empty, string.Empty, null, true));

            await foreach (var file in files)
            {
                entriesPathId.Add(await ExecPathFromFileAsync(scope, file, folderPath));
            }

            await fileMarker.RemoveMarkAsNewAsync(folder);

            var nestedFolders = await FilesSecurity.FilterDownloadAsync(FolderDao.GetFoldersAsync(folder.Id)).ToListAsync();

            var filesInFolder = await GetFilesInFoldersAsync(scope, nestedFolders.Select(f => f.Id), folderPath);
            entriesPathId.Add(filesInFolder);
        }

        return entriesPathId;
    }

    internal async Task CompressToZipAsync(Stream stream, IServiceScope scope)
    {
        if (_entriesPathId == null)
        {
            return;
        }

        var fileConverter = scope.ServiceProvider.GetService<FileConverter>();
        var fileDao = scope.ServiceProvider.GetService<IFileDao<T>>();

        using var compressTo = scope.ServiceProvider.GetService<CompressToArchive>();
        await compressTo.SetStream(stream);
        string error = null;

        foreach (var path in _entriesPathId.AllKeys)
        {
            if (string.IsNullOrEmpty(path))
            {
                await ProgressStep();
                continue;
            }

            var counter = 0;
            foreach (var entryId in _entriesPathId[path])
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                }

                var newTitle = path;

                File<T> file = null;
                var convertToExt = string.Empty;
                var password = string.Empty;

                if (!Equals(entryId, default(T)))
                {
                    await fileDao.InvalidateCacheAsync(entryId);
                    file = await fileDao.GetFileAsync(entryId);

                    if (file == null)
                    {
                        this[Err] = FilesCommonResource.ErrorMessage_FileNotFound;
                        continue;
                    }

                    if (_files.TryGetValue(file.Id, out var convertData) && !string.IsNullOrEmpty(convertData.Item1))
                    {
                        (convertToExt, password) = convertData;
                        var sourceFileName = Path.GetFileName(path);
                        var targetFileName = FileUtility.ReplaceFileExtension(sourceFileName, convertToExt);
                        newTitle = path.Replace(sourceFileName, targetFileName);
                    }
                }

                if (0 < counter)
                {
                    var suffix = " (" + counter + ")";

                    if (!Equals(entryId, default(T)))
                    {
                        newTitle = newTitle.IndexOf('.') > 0 ? newTitle.Insert(newTitle.LastIndexOf('.'), suffix) : newTitle + suffix;
                    }
                    else
                    {
                        break;
                    }
                }

                if (!Equals(entryId, default(T)))
                {
                    await compressTo.CreateEntry(newTitle, file.ModifiedOn);
                    try
                    {
                        await using var readStream = await fileConverter.EnableConvertAsync(file, convertToExt, true) ? 
                            await fileConverter.ExecAsync(file, convertToExt, password) : 
                            await fileDao.GetFileStreamAsync(file);

                        var t = Task.Run(async () => await compressTo.PutStream(readStream));

                        while (!t.IsCompleted)
                        {
                            await PublishChanges();
                            await Task.Delay(100);
                        }

                        await compressTo.CloseEntry();
                    }
                    catch (Exception ex) when(ex.InnerException is DocumentServiceException { Code: DocumentServiceException.ErrorCode.ConvertPassword })
                    {
                        error += $"{entryId}_password:";

                        Logger.ErrorWithException(ex);
                    }
                    catch (Exception ex)
                    {
                        error += ex.Message;

                        Logger.ErrorWithException(ex);
                    }
                }
                else
                {
                    await compressTo.CreateEntry(newTitle);
                    await compressTo.PutNextEntry();
                    await compressTo.CloseEntry();
                }

                counter++;

                if (!Equals(entryId, default(T)))
                {
                    ProcessedFile(entryId);
                }
                else
                {
                    ProcessedFolder(default);
                }
            }

            await ProgressStep();
        }

        if (!string.IsNullOrEmpty(error))
        {
            this[Err] = error;
            await PublishChanges();
        }
        
    }
    
    internal async Task<string> GetFileAsync(Stream stream, IServiceScope scope)
    {
        if (_entriesPathId == null)
        {
            return null;
        }

        var fileConverter = scope.ServiceProvider.GetService<FileConverter>();
        var fileDao = scope.ServiceProvider.GetService<IFileDao<T>>();

        var path = _entriesPathId.AllKeys.FirstOrDefault();
        if (string.IsNullOrEmpty(path))
        {
            await ProgressStep();
            return null;
        }

        var entryId = _entriesPathId[path].FirstOrDefault();
        if (CancellationToken.IsCancellationRequested)
        {
            CancellationToken.ThrowIfCancellationRequested();
        }

        var newTitle = path;

        File<T> file = null;
        var convertToExt = string.Empty;
        var password = string.Empty;

        if (!Equals(entryId, default(T)))
        {
            await fileDao.InvalidateCacheAsync(entryId);
            file = await fileDao.GetFileAsync(entryId);

            if (file == null)
            {
                this[Err] = FilesCommonResource.ErrorMessage_FileNotFound;
                return null;
            }

            if (_files.TryGetValue(file.Id, out var convertData) && !string.IsNullOrEmpty(convertData.Item1))
            {
                (convertToExt, password) = convertData;
                var sourceFileName = Path.GetFileName(path);
                var targetFileName = FileUtility.ReplaceFileExtension(sourceFileName, convertToExt);
                newTitle = path.Replace(sourceFileName, targetFileName);
            }
        }

        if (!Equals(entryId, default(T)))
        {
            try
            {
                await using var readStream = await fileConverter.EnableConvertAsync(file, convertToExt, true) ?
                    await fileConverter.ExecAsync(file, convertToExt, password) :
                    await fileDao.GetFileStreamAsync(file);
                
                await readStream.CopyToAsync(stream);
            }
            catch (Exception ex)
            {
                this[Err] = ex.Message;

                Logger.ErrorWithException(ex);
            }
        }


        if (!Equals(entryId, default(T)))
        {
            ProcessedFile(entryId);
        }
        else
        {
            ProcessedFolder(default);
        }
        

        await ProgressStep();
        

        return newTitle;
    }

    private void ReplaceLongPath(ItemNameValueCollection<T> entriesPathId)
    {
        foreach (var path in new List<string>(entriesPathId.AllKeys))
        {
            CancellationToken.ThrowIfCancellationRequested();

            if (200 >= path.Length || path.Contains('/'))
            {
                continue;
            }

            var ids = entriesPathId[path];
            entriesPathId.Remove(path);

            var newTitle = "LONG_FOLDER_NAME" + path[path.LastIndexOf('/')..];
            entriesPathId.Add(newTitle, ids);
        }
    }
}

internal class ItemNameValueCollection<T>
{
    private readonly Dictionary<string, List<T>> _dic = new();


    public IEnumerable<string> AllKeys => _dic.Keys;

    public IEnumerable<T> this[string name] => _dic[name].ToArray();

    public int Count => _dic.Count;

    public void Add(string name, T value)
    {
        if (!_dic.ContainsKey(name))
        {
            _dic.Add(name, []);
        }

        _dic[name].Add(value);
    }

    public void Add(ItemNameValueCollection<T> collection)
    {
        foreach (var key in collection.AllKeys)
        {
            foreach (var value in collection[key])
            {
                Add(key, value);
            }
        }
    }

    public void Add(string name, IEnumerable<T> values)
    {
        if (!_dic.ContainsKey(name))
        {
            _dic.Add(name, []);
        }

        _dic[name].AddRange(values);
    }

    public void Remove(string name)
    {
        _dic.Remove(name);
    }
}
