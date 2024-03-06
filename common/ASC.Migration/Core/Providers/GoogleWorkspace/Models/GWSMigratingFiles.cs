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


using System.Text.Json;

using ASC.Common.Security.Authentication;
using ASC.Web.Files.Utils;

using ASCShare = ASC.Files.Core.Security.FileShare;
using File = System.IO.File;

namespace ASC.Migration.GoogleWorkspace.Models;

[Transient]
public class GwsMigratingFiles(
    GlobalFolderHelper globalFolderHelper,
    IDaoFactory daoFactory,
    FileStorageService fileStorageService,
    TempPath tempPath,
    IServiceProvider serviceProvider,
    SecurityContext securityContext,
    EntryManager entryManager,
    UserManager userManager)
    : MigratingFiles
{
    public override int FoldersCount => _foldersCount;

    public override int FilesCount => _filesCount;

    public override long BytesTotal => _bytesTotal;

    private string _newParentFolder;

    private List<string> _files;
    private List<string> _folders;
    private string _rootFolder;
    private int _foldersCount;
    private int _filesCount;
    private long _bytesTotal;
    private GwsMigratingUser _user;
    private Dictionary<string, GwsMigratingUser> _users;
    private Dictionary<string, GWSMigratingGroups> _groups;
    private string _folderCreation;
    private IAccount _currentUser;
    private readonly Dictionary<string, AceWrapper> _aces = new Dictionary<string, AceWrapper>();

    public void Init(string rootFolder, GwsMigratingUser user, Action<string, Exception> log, IAccount currentUser)
    {
        _rootFolder = rootFolder;
        _user = user;
        Log = log;
        _currentUser = currentUser;
    }

    public override void Parse()
    {
        var drivePath = Path.Combine(_rootFolder, "Drive");
        if (!Directory.Exists(drivePath))
        {
            return;
        }

        var entries = Directory.GetFileSystemEntries(drivePath, "*", SearchOption.AllDirectories);

        var filteredEntries = new List<string>();
        _files = new List<string>();
        _folders = new List<string>();
        _folderCreation = _folderCreation ?? DateTime.Now.ToString("dd.MM.yyyy");
        _newParentFolder = MigrationResource.GoogleModuleNameDocuments + " " + _folderCreation;

        foreach (var entry in entries)
        {
            if (ShouldIgnoreFile(entry, entries))
            {
                continue;
            }

            filteredEntries.Add(entry);
        }

        foreach (var entry in filteredEntries)
        {
            var attr = File.GetAttributes(entry);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                _foldersCount++;
                _folders.Add(_newParentFolder + Path.DirectorySeparatorChar+ entry.Substring(drivePath.Length + 1));
            }
            else
            {
                _filesCount++;
                var fi = new FileInfo(entry);
                _bytesTotal += fi.Length;
                _files.Add(_newParentFolder + Path.DirectorySeparatorChar + entry.Substring(drivePath.Length + 1));
            }
        }
    }

    public void SetUsersDict(IEnumerable<GwsMigratingUser> users)
    {
        _users = users.ToDictionary(user => user.Email, user => user);
    }
    public void SetGroupsDict(IEnumerable<GWSMigratingGroups> groups)
    {
        _groups = groups.ToDictionary(group => group.GroupName, group => group);
    }

    public override async Task MigrateAsync()
    {
        if (!ShouldImport)
        {
            return;
        }
        var tmpFolder = Path.Combine(tempPath.GetTempPath(), Path.GetFileNameWithoutExtension(_user.Key));
        try
        {
            ZipFile.ExtractToDirectory(Path.Combine(_rootFolder, _user.Key), tmpFolder);
            var drivePath = Path.Combine(tmpFolder, "Takeout", "Drive");
            // Create all folders first
            var foldersDict = new Dictionary<string, Folder<int>>();

            //create default folder
            if ((_folders == null || _folders.Count == 0) && (_files != null && _files.Count != 0))
            {
                var parentId = await globalFolderHelper.FolderMyAsync;
                var createdFolder = await fileStorageService.CreateFolderAsync(parentId, _newParentFolder);
                foldersDict.Add(_newParentFolder, createdFolder);
            }

            if (_folders != null && _folders.Count != 0)
            {
                foreach (var folder in _folders)
                {
                    var split = folder.Split(Path.DirectorySeparatorChar); // recursivly create all the folders
                    for (var i = 0; i < split.Length; i++)
                    {
                        var path = string.Join(Path.DirectorySeparatorChar.ToString(), split.Take(i + 1));
                        if (foldersDict.ContainsKey(path))
                        {
                            continue; // skip folder if it was already created as a part of another path
                        }

                        var parentId = i == 0 ? await globalFolderHelper.FolderMyAsync : foldersDict[string.Join(Path.DirectorySeparatorChar.ToString(), split.Take(i))].Id;
                        try
                        {
                            var realPath = Path.Combine(drivePath, path.Replace(MigrationResource.GoogleModuleNameDocuments + " " + _folderCreation + Path.DirectorySeparatorChar, ""));
                            if (ShouldImportSharedFolders && TryReadInfoFile(realPath, out var info))
                            {
                                var list = new List<AceWrapper>();
                                foreach (var shareInfo in info.Permissions)
                                {
                                    if (shareInfo.Type is "user" or "group")
                                    {
                                        var shareType = GetPortalShare(shareInfo);
                                        _users.TryGetValue(shareInfo.EmailAddress, out var userToShare);
                                        _groups.TryGetValue(shareInfo.Name, out var groupToShare);
                                        if ((userToShare == null && groupToShare == null) || shareType == null)
                                        {
                                            continue;
                                        }

                                        var entryGuid = userToShare?.Guid ?? groupToShare.Guid;

                                        list.Add(new AceWrapper
                                        {
                                            Access = shareType.Value,
                                            Id = entryGuid,
                                            SubjectGroup = false
                                        });
                                    }
                                }

                                path = path.Contains(_newParentFolder + Path.DirectorySeparatorChar.ToString()) ? path.Replace(_newParentFolder + Path.DirectorySeparatorChar.ToString(), "") : path;
                                if (list.Count == 0)
                                {
                                    var createdFolder = await fileStorageService.CreateFolderAsync(parentId, split[i]);
                                    foldersDict.Add(path, createdFolder);
                                }
                                else
                                {
                                    if (_user.UserType == EmployeeType.Collaborator)
                                    {
                                        await securityContext.AuthenticateMeAsync(_currentUser);
                                    }

                                    var room = await fileStorageService.CreateRoomAsync(split[i],
                                        RoomType.EditingRoom, false, false, new List<FileShareParams>(), 0);
                                    foldersDict.Add(path, room);

                                    if (_user.UserType == EmployeeType.Collaborator)
                                    {
                                        list.Add(new AceWrapper
                                        {
                                            Access = ASCShare.Collaborator,
                                            Id = _user.Guid,
                                            SubjectGroup = false
                                        });
                                    }

                                    var aceCollection = new AceCollection<int>
                                    {
                                        Files = new List<int>(),
                                        Folders = new List<int> { room.Id },
                                        Aces = list,
                                        Message = null
                                    };

                                    try
                                    {
                                        await fileStorageService.SetAceObjectAsync(aceCollection, false);
                                        await securityContext.AuthenticateMeAsync(_user.Guid);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log($"Couldn't change folder permissions for {room.Id}", ex);
                                    }
                                }
                            }
                            else
                            {
                                var createdFolder = await fileStorageService.CreateFolderAsync(parentId, split[i]);
                                path = path.Contains(_newParentFolder + Path.DirectorySeparatorChar.ToString()) ? path.Replace(_newParentFolder + Path.DirectorySeparatorChar.ToString(), "") : path;
                                foldersDict.Add(path, createdFolder);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Couldn't create folder {path}", ex);
                        }
                    }
                }
            }

            var filesDict = new Dictionary<string, File<int>>();
            if (_files != null && _files.Count != 0)
            {
                foreach (var file in _files)
                {
                    var maskFile = file.Replace(MigrationResource.GoogleModuleNameDocuments + " " + _folderCreation + Path.DirectorySeparatorChar, "");
                    var maskParentPath = Path.GetDirectoryName(maskFile);
                    var realPath = Path.Combine(drivePath, maskFile);

                    try
                    {
                        var parentFolder = string.IsNullOrWhiteSpace(maskParentPath) ? foldersDict[_newParentFolder] : foldersDict[maskParentPath];
                        var newFile = await AddFileAsync(realPath, parentFolder.Id, Path.GetFileName(file));
                        filesDict.Add(realPath, newFile);
                    }
                    catch (Exception ex)
                    {
                        Log($"Couldn't create file {maskParentPath}/{Path.GetFileName(file)}", ex);
                    }
                }
            }

            if (!ShouldImportSharedFiles)
            {
                return;
            }
            var entries = filesDict
            .ToDictionary(kv => kv.Key, kv => (FileEntry<int>)kv.Value)
            .OrderBy(kv => kv.Key.Count(c => Path.DirectorySeparatorChar.Equals(c)));

            foreach (var kv in entries)
            {
                if (!TryReadInfoFile(kv.Key, out var info))
                {
                    continue;
                }

                if (kv.Value is File<int>)
                {
                    foreach (var shareInfo in info.Permissions)
                    {
                        if (shareInfo.Type is "user")
                        {
                            var shareType = GetPortalShare(shareInfo);
                            _users.TryGetValue(shareInfo.EmailAddress, out var userToShare);
                            if (userToShare == null || shareType == null)
                            {
                                continue;
                            }
                            var ace = await GetAceAsync(kv, shareType.Value);
                            if (ace != null)
                            {
                                await securityContext.AuthenticateMeAsync(userToShare.Guid);
                                await entryManager.MarkAsRecentByLink(kv.Value as File<int>, ace.Id);
                            }
                        }
                        else if(shareInfo.Type is "group")
                        {
                            _groups.TryGetValue(shareInfo.Name, out var groupToShare);
                            var shareType = GetPortalShare(shareInfo);
                            if (groupToShare == null || shareType == null)
                            {
                                continue;
                            }
                            var ace = await GetAceAsync(kv, shareType.Value);
                            if (ace != null)
                            {
                                var users = userManager.GetUsers(false, EmployeeStatus.Active,
                                new List<List<Guid>> { new List<Guid> { groupToShare.Guid } },
                                new List<Guid>(), new List<Tuple<List<List<Guid>>, List<Guid>>>(), null, null, null, "", false, "firstname",
                                    true, 100000, 0).Where(u => u.Id != _user.Guid);
                                await foreach (var u in users)
                                {
                                    await securityContext.AuthenticateMeAsync(u.Id);
                                    await entryManager.MarkAsRecentByLink(kv.Value as File<int>, ace.Id);
                                }
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            if (Directory.Exists(tmpFolder))
            {
                Directory.Delete(tmpFolder, true);
            }
        }
    }

    private async Task<File<int>> AddFileAsync(string realPath, int folderId, string fileTitle)
    {
        await using var fs = new FileStream(realPath, FileMode.Open);
        var fileDao = daoFactory.GetFileDao<int>();

        var newFile = serviceProvider.GetService<File<int>>();
        newFile.ParentId = folderId;
        newFile.Comment = FilesCommonResource.CommentCreate;
        newFile.Title = fileTitle;
        newFile.ContentLength = fs.Length;
        return await fileDao.SaveFileAsync(newFile, fs);
    }

    private static readonly Regex _versionRegex = new Regex(@"(\([\d]+\))");
    private string FindInfoFile(string entry)
    {
        var infoFilePath = entry + InfoFile;
        if (File.Exists(infoFilePath))
        {
            return infoFilePath; // file.docx-info.json
        }

        var ext = Path.GetExtension(entry);
        infoFilePath = entry.Substring(0, entry.Length - ext.Length) + InfoFile;
        if (File.Exists(infoFilePath))
        {
            return infoFilePath; // file-info.json
        }

        var versionMatch = _versionRegex.Match(entry);
        if (!versionMatch.Success)
        {
            return null;
        }

        var version = versionMatch.Groups[1].Value;
        infoFilePath = entry.Replace(version, "") + InfoFile.Replace(".", version + ".");
        if (File.Exists(infoFilePath))
        {
            return infoFilePath; // file.docx-info(1).json
        }

        infoFilePath = entry.Substring(0, entry.Length - ext.Length).Replace(version, "") + InfoFile.Replace(".", version + ".");
        if (File.Exists(infoFilePath))
        {
            return infoFilePath; // file-info(1).json
        }

        return null;
    }

    private bool TryReadInfoFile(string entry, out GwsDriveFileInfo info)
    {
        info = null;
        var infoFilePath = FindInfoFile(entry);

        if (infoFilePath == null)
        {
            return false;
        }

        try
        {
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };
            info = JsonSerializer.Deserialize<GwsDriveFileInfo>(File.ReadAllText(infoFilePath), options);
            return true;
        }
        catch (Exception ex)
        {
            Log($"Couldn't read info file for {entry}", ex);
        }

        return false;
    }

    private static readonly Regex _workspacesRegex = new Regex(@"Workspaces(\(\d+\))?.json");
    private static readonly Regex _pinnedRegex = new Regex(@".*-at-.*-pinned\..*");
    private const string CommentsFile = "-comments.html";
    private const string InfoFile = "-info.json";
    private static readonly Regex _commentsVersionFile = new Regex(@"-comments(\([\d]+\))\.html");
    private static readonly Regex _infoVersionFile = new Regex(@"-info(\([\d]+\))\.json");
    private bool ShouldIgnoreFile(string entry, string[] entries)
    {
        if (_workspacesRegex.IsMatch(Path.GetFileName(entry)))
        {
            return true; // ignore workspaces.json
        }

        if (_pinnedRegex.IsMatch(Path.GetFileName(entry)))
        {
            return true; // ignore pinned files
        }

        if (entry.EndsWith(CommentsFile) || entry.EndsWith(InfoFile)) // check if this really a meta for existing file
        {
            // folder - folder
            // folder-info.json - valid meta

            // file.docx - file
            // file.docx-info.json - valid meta
            // file-info.json - valid meta

            var baseName = entry.Substring(0, entry.Length - (entry.EndsWith(CommentsFile) ? CommentsFile.Length : InfoFile.Length));
            if (entries.Contains(baseName))
            {
                return true;
            }

            if (entries
                .Where(e => e.StartsWith(baseName + "."))
                .Select(e => e.Substring(0, e.Length - Path.GetExtension(e).Length))
                .Contains(baseName))
            {
                return true;
            }
        }

        // file(1).docx - file
        // file.docx-info(1).json - valid meta
        // file-info(1).json - valid meta
        var commentsVersionMatch = _commentsVersionFile.Match(entry);
        if (commentsVersionMatch.Success)
        {
            var baseName = entry.Substring(0, entry.Length - commentsVersionMatch.Groups[0].Value.Length);
            baseName = baseName.Insert(baseName.LastIndexOf("."), commentsVersionMatch.Groups[1].Value);

            if (entries.Contains(baseName))
            {
                return true;
            }

            if (entries
                .Where(e => e.StartsWith(baseName + "."))
                .Select(e => e.Substring(0, e.Length - Path.GetExtension(e).Length))
                .Contains(baseName))
            {
                return true;
            }
        }

        var infoVersionMatch = _infoVersionFile.Match(entry);
        if (infoVersionMatch.Success)
        {
            var baseName = entry.Substring(0, entry.Length - infoVersionMatch.Groups[0].Length);
            baseName = baseName.Insert(baseName.LastIndexOf("."), infoVersionMatch.Groups[1].Value);

            if (entries.Contains(baseName))
            {
                return true;
            }

            if (entries
                .Where(e => e.StartsWith(baseName + "."))
                .Select(e => e.Substring(0, e.Length - Path.GetExtension(e).Length))
                .Contains(baseName))
            {
                return true;
            }
        }

        return false;
    }

    private ASCShare? GetPortalShare(GwsDriveFilePermission fileInfo)
    {
        switch (fileInfo.Role)
        {
            case "writer":
                return ASCShare.Editing;
            case "reader":
                if (fileInfo.AdditionalRoles == null)
                {
                    return ASCShare.Read;
                }

                if (fileInfo.AdditionalRoles.Contains("commenter"))
                {
                    return ASCShare.Comment;
                }
                else
                {
                    return ASCShare.Read;
                }

            default:
                return null;
        };
    }


    private async Task<AceWrapper> GetAceAsync(KeyValuePair<string, FileEntry<int>> kv, ASCShare shareType)
    {
        if (!_aces.ContainsKey($"{shareType}{kv.Value.Id}"))
        {
            try
            {
                await securityContext.AuthenticateMeAsync(_user.Guid);
                var ace = await fileStorageService.SetExternalLinkAsync(kv.Value.Id, FileEntryType.File, Guid.Empty, null, shareType, requiredAuth: true,
                    primary: false);
                _aces.Add($"{shareType}{kv.Value.Id}", ace);
                return ace;
            }
            catch
            {
                _aces.Add($"{shareType}{kv.Value.Id}", null);
                return null;
            }
        }
        else
        {
            return _aces[$"{shareType}{kv.Value.Id}"];
        }
    }
}
