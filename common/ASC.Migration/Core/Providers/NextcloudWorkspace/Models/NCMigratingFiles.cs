﻿// (c) Copyright Ascensio System SIA 2009-2024
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
using FileShare = ASC.Files.Core.Security.FileShare;

namespace ASC.Migration.NextcloudWorkspace.Models.Parse;

[Transient]
public class NcMigratingFiles : MigratingFiles
{
    public override int FoldersCount => _foldersCount;
    public override int FilesCount => _filesCount;
    public override long BytesTotal => _bytesTotal;

    private readonly GlobalFolderHelper _globalFolderHelper;
    private readonly IDaoFactory _daoFactory;
    private readonly FileStorageService _fileStorageService;
    private readonly IServiceProvider _serviceProvider;

    private NcMigratingUser _user;
    private string _rootFolder;
    private List<NCFileCache> _files;
    private List<NCFileCache> _folders;
    private int _foldersCount;
    private int _filesCount;
    private long _bytesTotal;
    private NCStorages _storages;
    private Dictionary<string, NcMigratingUser> _users;
    private Dictionary<string, NcMigratingGroups> _groups;
    private string _folderCreation;
    private readonly Dictionary<object, int> _matchingFileId = new();

    public NcMigratingFiles(GlobalFolderHelper globalFolderHelper,
        IDaoFactory daoFactory,
        FileStorageService fileStorageService,
        IServiceProvider serviceProvider)
    {
        _globalFolderHelper = globalFolderHelper;
        _daoFactory = daoFactory;
        _fileStorageService = fileStorageService;
        _serviceProvider = serviceProvider;
    }

    public void Init(string rootFolder, NcMigratingUser user, NCStorages storages, Action<string, Exception> log)
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
        _folderCreation = _folderCreation ?? DateTime.Now.ToString("dd.MM.yyyy");
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
                        var newFolder = await _fileStorageService.CreateFolderAsync(parentId, split[i]);
                        foldersDict.Add(path, newFolder);
                        _matchingFileId.Add(newFolder.Id, folder.FileId);
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
                    var folderDao = _daoFactory.GetFolderDao<int>();

                    var parentFolder = string.IsNullOrWhiteSpace(parentPath) ? await folderDao.GetFolderAsync(await _globalFolderHelper.FolderMyAsync) : foldersDict[parentPath];
                    var newFile = await AddFileAsync(realPath, parentFolder.Id, Path.GetFileName(file.Path));
                    _matchingFileId.Add(newFile.Id, file.FileId);
                }
                catch (Exception ex)
                {
                    Log($"Couldn't create file {parentPath}/{Path.GetFileName(file.Path)}", ex);
                }
            }
        }
        
        if (!ShouldImportSharedFiles && !ShouldImportSharedFolders)
        {
            return;
        }
        
        foreach (var item in _matchingFileId)
        {
            var list = new List<AceWrapper>();
            var entryIsFile = _files.Exists(el => el.FileId == item.Value);
            if (entryIsFile && !ShouldImportSharedFiles || !entryIsFile && !ShouldImportSharedFolders)
            {
                continue;
            }
            var entry = entryIsFile ? _files.Find(el => el.FileId == item.Value) : _folders.Find(el => el.FileId == item.Value);
            if (entry.Share.Count == 0)
            {
                continue;
            }

            foreach (var shareInfo in entry.Share)
            {
                if (shareInfo.ShareWith == null)
                {
                    continue;
                }

                var shareType = GetPortalShare(shareInfo.Premissions, entryIsFile);
                _users.TryGetValue(shareInfo.ShareWith, out var userToShare);
                _groups.TryGetValue(shareInfo.ShareWith, out var groupToShare);

                if (userToShare != null || groupToShare != null)
                {
                    var entryGuid = userToShare?.Guid ?? groupToShare.Guid;
                    list.Add(new AceWrapper
                    {
                        Access = shareType.Value,
                        Id = entryGuid,
                        SubjectGroup = false
                    });
                }
            }
            if (list.Count == 0)
            {
                continue;
            }

            var aceCollection = new AceCollection<int>
            {
                Files = entryIsFile ? new List<int> { (int)item.Key } : [],
                Folders = entryIsFile ? [] : new List<int> { (int)item.Key },
                Aces = list,
                Message = null
            };

            try
            {
                await _fileStorageService.SetAceObjectAsync(aceCollection, false);
            }
            catch (Exception ex)
            {
                Log($"Couldn't change file permissions for {item.Key}", ex);
            }
        }
    }


    private async Task<File<int>> AddFileAsync(string realPath, int folderId, string fileTitle)
    {
        await using var fs = new FileStream(realPath, FileMode.Open);
        var fileDao = _daoFactory.GetFileDao<int>();

        var newFile = _serviceProvider.GetService<File<int>>();
        newFile.ParentId = folderId;
        newFile.Comment = FilesCommonResource.CommentCreate;
        newFile.Title = fileTitle;
        newFile.ContentLength = fs.Length;
        return await fileDao.SaveFileAsync(newFile, fs);
    }

    public void SetUsersDict(IEnumerable<NcMigratingUser> users)
    {
        _users = users.ToDictionary(user => user.Key, user => user);
    }
    public void SetGroupsDict(IEnumerable<NcMigratingGroups> groups)
    {
        _groups = groups.ToDictionary(group => group.GroupName, group => group);
    }
    
    private FileShare? GetPortalShare(int role, bool entryType)
    {
        if (entryType)
        {
            if (role == 1 || role == 17)
            {
                return FileShare.Read;
            }

            return FileShare.ReadWrite;//permission = 19 => denySharing = true, permission = 3 => denySharing = false; FileShare.ReadWrite
        }
        else
        {
            if (Array.Exists(new int[] { 1, 17, 9, 25, 5, 21, 13, 29, 3, 19, 11, 27 }, el => el == role))
            {
                return FileShare.Read;
            }

            return FileShare.ReadWrite;//permission = 19||23 => denySharing = true, permission = 7||15 => denySharing = false; FileShare.ReadWrite
        }
    }
}
