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


using ASCShare = ASC.Files.Core.Security.FileShare;
using File = System.IO.File;

namespace ASC.Migration.GoogleWorkspace.Models;

[Transient]
public class GwsMigratingFiles : MigratingFiles
{
    public override int FoldersCount => _foldersCount;

    public override int FilesCount => _filesCount;

    public override long BytesTotal => _bytesTotal;

    private string _newParentFolder;
    private string _newSharedParentFolder;

    private List<string> _files;
    private List<string> _folders;
    private readonly GlobalFolderHelper _globalFolderHelper;
    private readonly IDaoFactory _daoFactory;
    private readonly FileSecurity _fileSecurity;
    private readonly FileStorageService _fileStorageService;
    private readonly TempPath _tempPath;
    private readonly IServiceProvider _serviceProvider;
    private string _rootFolder;
    private int _foldersCount;
    private int _filesCount;
    private long _bytesTotal;
    private GwsMigratingUser _user;
    private Dictionary<string, GwsMigratingUser> _users;
    private string _folderCreation;
    private readonly SecurityContext _securityContext;
    private readonly UserManager _userManager;

    public GwsMigratingFiles(
        GlobalFolderHelper globalFolderHelper,
        IDaoFactory daoFactory,
        FileSecurity fileSecurity,
        FileStorageService fileStorageService,
        TempPath tempPath,
        IServiceProvider serviceProvider,
        SecurityContext securityContext,
        UserManager userManager)
    {
        _globalFolderHelper = globalFolderHelper;
        _daoFactory = daoFactory;
        _fileSecurity = fileSecurity;
        _fileStorageService = fileStorageService;
        _tempPath = tempPath;
        _serviceProvider = serviceProvider;
        _securityContext = securityContext;
        _userManager = userManager;
    }

    public void Init(string rootFolder, GwsMigratingUser user, Action<string, Exception> log)
    {
        _rootFolder = rootFolder;
        _user = user;
        Log = log;
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
        _folderCreation = _folderCreation != null ? _folderCreation : DateTime.Now.ToString("dd.MM.yyyy");
        _newParentFolder = MigrationResource.GoogleModuleNameDocuments + " " + _folderCreation;
        _newSharedParentFolder = $"Google files shared from {_user.Email} {_folderCreation}";

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
                _folders.Add(_newParentFolder + Path.DirectorySeparatorChar.ToString() + entry.Substring(drivePath.Length + 1));
            }
            else
            {
                _filesCount++;
                var fi = new FileInfo(entry);
                _bytesTotal += fi.Length;
                _files.Add(_newParentFolder + Path.DirectorySeparatorChar.ToString() + entry.Substring(drivePath.Length + 1));
            }
        }
    }

    public void SetUsersDict(IEnumerable<GwsMigratingUser> users)
    {
        _users = users.ToDictionary(user => user.Email, user => user);
    }

    public override async Task MigrateAsync()
    {
        var tmpFolder = Path.Combine(_tempPath.GetTempPath(), Path.GetFileNameWithoutExtension(_user.Key));
        try
        {
            ZipFile.ExtractToDirectory(Path.Combine(_rootFolder, _user.Key), tmpFolder);
            var drivePath = Path.Combine(tmpFolder, "Takeout", "Drive");
            if (ShouldImport) {
                // Create all folders first
                var foldersDict = new Dictionary<string, Folder<int>>();
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

                            var parentId = i == 0 ? await _globalFolderHelper.FolderMyAsync : foldersDict[string.Join(Path.DirectorySeparatorChar.ToString(), split.Take(i))].Id;
                            try
                            {
                                var createdFolder = await _fileStorageService.CreateNewFolderAsync(parentId, split[i]);
                                path = path.Contains(_newParentFolder + Path.DirectorySeparatorChar.ToString()) ? path.Replace(_newParentFolder + Path.DirectorySeparatorChar.ToString(), "") : path;
                                foldersDict.Add(path, createdFolder);
                            }
                            catch (Exception ex)
                            {
                                Log($"Couldn't create folder {path}", ex);
                            }
                        }
                    }
                }
                //create default folder
                if ((_folders == null || _folders.Count == 0) && (_files != null && _files.Count != 0))
                {
                    var parentId = await _globalFolderHelper.FolderMyAsync;
                    var createdFolder = await _fileStorageService.CreateNewFolderAsync(parentId, _newParentFolder);
                    foldersDict.Add(_newParentFolder, createdFolder);
                }

                if (_files != null && _files.Count != 0)
                {
                    foreach (var file in _files)
                    {
                        var maskFile = file.Replace(MigrationResource.GoogleModuleNameDocuments + " " + _folderCreation + Path.DirectorySeparatorChar.ToString(), "");
                        var maskParentPath = Path.GetDirectoryName(maskFile);
                        var realPath = Path.Combine(drivePath, maskFile);

                        try
                        {
                            var parentFolder = string.IsNullOrWhiteSpace(maskParentPath) ? foldersDict[_newParentFolder] : foldersDict[maskParentPath];
                            await AddFileAsync(realPath, parentFolder.Id, Path.GetFileName(file));
                        }
                        catch (Exception ex)
                        {
                            Log($"Couldn't create file {maskParentPath}/{Path.GetFileName(file)}", ex);
                        }
                    }
                }
            }

            var sharedFolders = new Dictionary<string, int>();
            if (ShouldImportSharedFiles && _files != null && _files.Count != 0)
            {
                foreach (var file in _files)
                {
                    var maskFile = file.Replace(MigrationResource.GoogleModuleNameDocuments + " " + _folderCreation + Path.DirectorySeparatorChar.ToString(), "");
                    var maskParentPath = Path.GetDirectoryName(maskFile);
                    var realPath = Path.Combine(drivePath, maskFile);
                    if (TryReadInfoFile(realPath, out var info))
                    {
                        foreach (var shareInfo in info.Permissions)
                        {
                            if (string.IsNullOrEmpty(shareInfo.EmailAddress))
                            {
                                continue;
                            }

                            _users.TryGetValue(shareInfo.EmailAddress, out var userToShare);
                            if (userToShare == null)
                            {
                                continue;
                            }

                            try
                            {
                                var user = await _userManager.GetUserByEmailAsync(shareInfo.EmailAddress);
                                await _securityContext.AuthenticateMeAsync(user.Id);
                                if (!sharedFolders.ContainsKey(shareInfo.EmailAddress))
                                {
                                    var parentId = await _globalFolderHelper.FolderMyAsync;
                                    var createdFolder = await _fileStorageService.CreateNewFolderAsync(parentId, _newSharedParentFolder);
                                    sharedFolders.Add(shareInfo.EmailAddress, createdFolder.Id);
                                }
                                await AddFileAsync(realPath, sharedFolders[shareInfo.EmailAddress], Path.GetFileName(file));
                            }
                            catch (Exception ex)
                            {
                                Log($"Couldn't share file {maskParentPath}/{Path.GetFileName(file)} to {shareInfo.EmailAddress}", ex);
                            }
                        }
                    }
                }
                await _securityContext.AuthenticateMeAsync(_user.Guid);
            }
        }
        catch
        {
            throw;
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
        using var fs = new FileStream(realPath, FileMode.Open);
        var fileDao = _daoFactory.GetFileDao<int>();

        var newFile = _serviceProvider.GetService<File<int>>();
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
            info = JsonConvert.DeserializeObject<GwsDriveFileInfo>(File.ReadAllText(infoFilePath));
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
                return ASCShare.ReadWrite;
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
}
