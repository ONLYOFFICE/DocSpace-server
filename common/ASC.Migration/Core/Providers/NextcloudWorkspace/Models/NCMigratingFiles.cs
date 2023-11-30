// (c) Copyright Ascensio System SIA 2010-2022
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

using File = System.IO.File;

namespace ASC.Migration.NextcloudWorkspace.Models.Parse;

[Transient]
public class NCMigratingFiles : MigratingFiles
{
    public override int FoldersCount => _foldersCount;
    public override int FilesCount => _filesCount;
    public override long BytesTotal => _bytesTotal;

    private readonly GlobalFolderHelper _globalFolderHelper;
    private readonly IDaoFactory _daoFactory;
    private readonly FileStorageService _fileStorageService;
    private readonly IServiceProvider _serviceProvider;
    private readonly SecurityContext _securityContext;
    private readonly UserManager _userManager;

    private NCMigratingUser _user;
    private string _rootFolder;
    private List<NCFileCache> _files;
    private List<NCFileCache> _folders;
    private int _foldersCount;
    private int _filesCount;
    private long _bytesTotal;
    private NCStorages _storages;
    private Dictionary<string, NCMigratingUser> _users;
    private string _folderCreation;

    public NCMigratingFiles(GlobalFolderHelper globalFolderHelper,
        IDaoFactory daoFactory,
        FileStorageService fileStorageService,
        IServiceProvider serviceProvider,
        SecurityContext securityContext,
        UserManager usermanager)
    {
        _globalFolderHelper = globalFolderHelper;
        _daoFactory = daoFactory;
        _fileStorageService = fileStorageService;
        _serviceProvider = serviceProvider;
        _securityContext = securityContext;
        _userManager = usermanager;
    }

    public void Init(string rootFolder, NCMigratingUser user, NCStorages storages, Action<string, Exception> log)
    {
        _rootFolder = rootFolder;
        _user = user;
        _storages = storages;
        Log = log;
    }

    public override void Parse()
    {
        var drivePath = Directory.Exists(Path.Combine(_rootFolder, "data", _user.Key, "files")) ?
            Path.Combine(_rootFolder, "data", _user.Key, "files") : null;
        if (drivePath == null)
        {
            return;
        }

        _files = new List<NCFileCache>();
        _folders = new List<NCFileCache>();
        _folderCreation = _folderCreation != null ? _folderCreation : DateTime.Now.ToString("dd.MM.yyyy");
        foreach (var entry in _storages.FileCache)
        {
            var paths = entry.Path.Split('/');
            if (paths[0] != "files")
            {
                continue;
            }

            paths[0] = "NextCloud’s Files " + _folderCreation;
            entry.Path = string.Join("/", paths);

            if (paths.Length >= 1)
            {
                var tmpPath = drivePath;
                for (var i = 1; i < paths.Length; i++)
                {
                    tmpPath = Path.Combine(tmpPath, paths[i]);
                }
                if (Directory.Exists(tmpPath) || File.Exists(tmpPath))
                {
                    var attr = File.GetAttributes(tmpPath);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        _foldersCount++;
                        _folders.Add(entry);
                    }
                    else
                    {
                        _filesCount++;
                        var fi = new FileInfo(tmpPath);
                        _bytesTotal += fi.Length;
                        _files.Add(entry);
                    }
                }
            }
        }
    }

    public override async Task MigrateAsync()
    {
        if (!ShouldImport)
        {
            return;
        }

        var drivePath = Directory.Exists(Path.Combine(_rootFolder, "data", _user.Key, "files")) ?
            Path.Combine(_rootFolder, "data", _user.Key) : null;
        if (drivePath == null)
        {
            return;
        }

        var foldersDict = new Dictionary<string, Folder<int>>();
        if (_folders != null)
        {
            foreach (var folder in _folders)
            {
                var split = folder.Path.Split('/');
                for (var i = 0; i < split.Length; i++)
                {
                    var path = string.Join(Path.DirectorySeparatorChar.ToString(), split.Take(i + 1));
                    if (foldersDict.ContainsKey(path))
                    {
                        continue;
                    }

                    var parentId = i == 0 ? await _globalFolderHelper.FolderMyAsync : foldersDict[string.Join(Path.DirectorySeparatorChar.ToString(), split.Take(i))].Id;
                    try
                    {
                        var newFolder = await _fileStorageService.CreateNewFolderAsync(parentId, split[i]);
                        foldersDict.Add(path, newFolder);
                    }
                    catch (Exception ex)
                    {
                        Log($"Couldn't create folder {path}", ex);
                    }
                }
            }
        }

        if (_files != null)
        {
            foreach (var file in _files)
            {
                var maskPaths = file.Path.Split('/');
                if (maskPaths[0] == "NextCloud’s Files " + DateTime.Now.ToString("dd.MM.yyyy"))
                {
                    maskPaths[0] = "files";
                }
                var maskPath = string.Join(Path.DirectorySeparatorChar.ToString(), maskPaths);
                var parentPath = Path.GetDirectoryName(file.Path);
                try
                {
                    var realPath = Path.Combine(drivePath, maskPath);
                    var fileDao = _daoFactory.GetFileDao<int>();
                    var folderDao = _daoFactory.GetFolderDao<int>();

                    var parentFolder = string.IsNullOrWhiteSpace(parentPath) ? await folderDao.GetFolderAsync(await _globalFolderHelper.FolderMyAsync) : foldersDict[parentPath];
                    await AddFileAsync(realPath, parentFolder.Id, Path.GetFileName(file.Path));
                }
                catch (Exception ex)
                {
                    Log($"Couldn't create file {parentPath}/{Path.GetFileName(file.Path)}", ex);
                }
            }
        }

        var sharedFolders = new Dictionary<string, int>();

        if (ShouldImportSharedFiles && _files != null && _files.Count != 0)
        {
            foreach (var file in _files)
            {
                var maskPaths = file.Path.Split('/');
                if (maskPaths[0] == "NextCloud’s Files " + DateTime.Now.ToString("dd.MM.yyyy"))
                {
                    maskPaths[0] = "files";
                }
                var maskPath = string.Join(Path.DirectorySeparatorChar.ToString(), maskPaths);
                var parentPath = Path.GetDirectoryName(file.Path);
                var realPath = Path.Combine(drivePath, maskPath); 

                foreach (var shareInfo in file.Share)
                {
                    if (shareInfo.ShareWith == null)
                    {
                        continue;
                    }

                    _users.TryGetValue(shareInfo.ShareWith, out var userToShare);
                    if (userToShare == null)
                    {
                        continue;
                    }

                    try
                    {
                        var user = await _userManager.GetUserByEmailAsync(shareInfo.ShareWith);
                        await _securityContext.AuthenticateMeAsync(user.Id);
                        if (!sharedFolders.ContainsKey(shareInfo.ShareWith))
                        {
                            var parentId = await _globalFolderHelper.FolderMyAsync;
                            var createdFolder = await _fileStorageService.CreateNewFolderAsync(parentId, $"NextCloud’s files shared from {_user.Email} {_folderCreation}");
                            sharedFolders.Add(shareInfo.ShareWith, createdFolder.Id);
                        }
                        await AddFileAsync(realPath, sharedFolders[shareInfo.ShareWith], Path.GetFileName(file.Path));
                    }
                    catch (Exception ex)
                    {
                        Log($"Couldn't share file {parentPath}/{Path.GetFileName(file.Path)} to {shareInfo.ShareWith}", ex);
                    }
                }
                await _securityContext.AuthenticateMeAsync(_user.Guid);
            }
        }

        if (ShouldImportSharedFiles && _folders != null && _folders.Count != 0)
        {
            foreach (var folder in _folders)
            {
                var split = folder.Path.Split('/');
                if (split[0] == "NextCloud’s Files " + DateTime.Now.ToString("dd.MM.yyyy"))
                {
                    split[0] = "files";
                }
                var maskPath = string.Join(Path.DirectorySeparatorChar.ToString(), split);
                var realPath = Path.Combine(drivePath, maskPath);

                foreach (var shareInfo in folder.Share)
                {
                    if (shareInfo.ShareWith == null)
                    {
                        continue;
                    }

                    _users.TryGetValue(shareInfo.ShareWith, out var userToShare);
                    if (userToShare == null)
                    {
                        continue;
                    }
                    try
                    {
                        var user = await _userManager.GetUserByEmailAsync(shareInfo.ShareWith);
                        await _securityContext.AuthenticateMeAsync(user.Id);
                        if (!sharedFolders.ContainsKey(shareInfo.ShareWith))
                        {
                            var parentId = await _globalFolderHelper.FolderMyAsync;
                            var createdFolder = await _fileStorageService.CreateNewFolderAsync(parentId, $"NextCloud’s files shared from {_user.Email} {_folderCreation}");
                            sharedFolders.Add(shareInfo.ShareWith, createdFolder.Id);
                        }
                        await AddFolderAsync(sharedFolders[shareInfo.ShareWith], realPath, split.Last());
                    }
                    catch (Exception ex)
                    {
                        Log($"Couldn't share folder {Path.GetFileName(folder.Path)} to {shareInfo.ShareWith}", ex);
                    }
                }
            }
            await _securityContext.AuthenticateMeAsync(_user.Guid);
        }
    }

    private async Task AddFolderAsync(int parentId, string path, string title)
    {
        var newFolder = await _fileStorageService.CreateNewFolderAsync(parentId, title);
        foreach (var file in Directory.GetFiles(path))
        {
            await AddFileAsync(file, newFolder.Id, Path.GetFileName(file));
        }

        foreach (var innerFolder in Directory.GetDirectories(path))
        {
            await AddFolderAsync(newFolder.Id, innerFolder, innerFolder.Split('/').Last());
        }
    }

    private async Task<File<int>> AddFileAsync(string realPath, int folderId, string fileTitle)
    {
        using var fs = new FileStream(realPath, FileMode.Open);
        var fileDao = _daoFactory.GetFileDao<int>();

        var newFile = _serviceProvider.GetService<File<int>>();
        newFile.ParentId = folderId;
        newFile.Comment = FilesCommonResource.CommentCreate;
        newFile.Title = fileTitle;
        newFile.ContentLength = fs.Length;
        return await fileDao.SaveFileAsync(newFile, fs);
    }

    public void SetUsersDict(IEnumerable<NCMigratingUser> users)
    {
        _users = users.ToDictionary(user => user.Key, user => user);
    }
}
