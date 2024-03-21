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

using ASC.Common.Security.Authentication;
using ASC.Web.Files.Utils;

using Constants = ASC.Core.Users.Constants;
using FileShare = ASC.Files.Core.Security.FileShare;

namespace ASC.Migration.Core.Core.Providers.Models;

[Transient]
public class WorkspaceMigratingFiles(
    FileStorageService fileStorageService,
    GlobalFolderHelper globalFolderHelper,
    IServiceProvider serviceProvider,
    IDaoFactory daoFactory,
    SecurityContext securityContext,
    EntryManager entryManager,
    UserManager userManager)
    : MigratingFiles
{
    public override int FoldersCount => _storage.Folders.Count;
    public override int FilesCount => _storage.Files.Count;
    public override long BytesTotal => _bytesTotal;

    private string _key;
    private long _bytesTotal;
    private string _folder;
    private IDataReadOperator _dataReader;
    private WorkspaceStorage _storage;
    private List<WorkspaceSecurity> _securities;
    private Dictionary<string, string> _mappedGuids;
    private WorkspaceMigratingUser _user;
    private FolderType _type;
    private IAccount _currentUser;
    private readonly string _folderKey = "folder";
    private readonly string _fileKey = "file";

    public void Init(string key,
        WorkspaceMigratingUser user,
        IDataReadOperator dataReader,
        WorkspaceStorage storage,
        Action<string, Exception> log,
        Dictionary<string, string> mappedGuids,
        FolderType type,
        IAccount currentUser)
    {
        _key = key;
        _user = user;
        _dataReader = dataReader;
        Log = log;
        _storage = storage;
        _securities = new();
        _mappedGuids = mappedGuids;
        _type = type;
        _currentUser = currentUser;
    }

    public override void Parse()
    {
        var rootFolders = new List<string>();
        using var streamFolders = _dataReader.GetEntry("databases/files/files_folder");
        var dataFolders = new DataTable();
        dataFolders.ReadXml(streamFolders);
        foreach (var row in dataFolders.Rows.Cast<DataRow>())
        {
            if ((FolderType)int.Parse(row["folder_type"].ToString()) == _type && (
                    (_type == FolderType.USER && row["create_by"].ToString().Equals(_key)) ||
                    _type == FolderType.COMMON ||
                    (_type == FolderType.BUNCH && row["title"].ToString().StartsWith("projects_project"))))
            {
                rootFolders.Add(row["id"].ToString());
            }
        }

        using var streamTree = _dataReader.GetEntry("databases/files/files_folder_tree");
        var dataTree = new DataTable();
        dataTree.ReadXml(streamTree);
        var folderTree = new Dictionary<string, int>();
        foreach (var row in dataTree.Rows.Cast<DataRow>())
        {
            if (rootFolders.Contains(row["parent_id"].ToString()))
            {
                folderTree.Add(row["folder_id"].ToString(), int.Parse(row["level"].ToString()));
            }
        }

        var projectTitle = new Dictionary<string, string>();
        if (_type == FolderType.BUNCH)
        {
            using var streamProject = _dataReader.GetEntry("databases/projects/projects_projects");
            var dataProject = new DataTable();
            dataProject.ReadXml(streamProject);
            foreach (var row in dataProject.Rows.Cast<DataRow>())
            {
                projectTitle.Add(row["id"].ToString(), row["title"].ToString());
            }
            _folder = "0";
            folderTree.Add("0", -1);
        }
        else
        {
            _folder = rootFolders.Count == 0 ? "0" : rootFolders[0];
        }

        foreach (var row in dataFolders.Rows.Cast<DataRow>())
        {
            if (folderTree.ContainsKey(row["id"].ToString()) && row["id"].ToString() != _folder)
            {
                var title = row["title"].ToString();
                if (_type == FolderType.BUNCH)
                {
                    if (row["parent_id"].ToString() == "0" && row["title"].ToString().StartsWith("projects_project"))
                    {
                        var split = row["title"].ToString().Split('_');
                        title = projectTitle[split.Last()];
                    }
                }
                var id = row["id"].ToString();
                var folder = new WorkspaceFolder
                {
                    Id = int.Parse(id),
                    ParentId = int.Parse(row["parent_id"].ToString()),
                    Title = title,
                    Level = folderTree[id]
                };
                _storage.Folders.Add(folder);
            }
        }

        using var streamFiles = _dataReader.GetEntry("databases/files/files_file");
        var datafiles = new DataTable();
        datafiles.ReadXml(streamFiles);
        foreach (var row in datafiles.Rows.Cast<DataRow>())
        {
            if (folderTree.ContainsKey(row["folder_id"].ToString()))
            {
                var files = new WorkspaceFile()
                {
                    Id = int.Parse(row["id"].ToString()),
                    Folder = int.Parse(row["folder_id"].ToString()),
                    Title = row["title"].ToString(),
                    Version = int.Parse(row["version"].ToString()),
                    VersionGroup = int.Parse(row["version_group"].ToString())
                };
                _storage.Files.Add(files);
                _bytesTotal += int.Parse(row["content_length"].ToString());
            }
        }

        if (_type == FolderType.USER)
        {
            DbExtractFilesSecurity();
        }
    }
    
    private void DbExtractFilesSecurity()
    {
        using var streamGroup = _dataReader.GetEntry("databases/files/files_security");
        var data = new DataTable();
        data.ReadXml(streamGroup);

        foreach (var row in data.Rows.Cast<DataRow>())
        {
            var id = int.Parse(row["entry_id"].ToString());
            if (row["owner"].ToString() == _key && (_storage.Files.Select(f => f.Id).ToList().Contains(id) || _storage.Folders.Select(f=> f.Id).ToList().Contains(id)))
            {
                var security = new WorkspaceSecurity()
                {
                    Owner = row["owner"].ToString(),
                    Subject = row["subject"].ToString(),
                    SubjectType = int.Parse(row["subject_type"].ToString()),
                    EntryId = id,
                    EntryType = int.Parse(row["entry_type"].ToString()),
                    Security = int.Parse(row["security"].ToString())
                };
                _securities.Add(security);
            }
        }
    }

    public override async Task MigrateAsync()
    {
        if (!ShouldImport || _storage.Files.Count == 0)
        {
            return;
        }

        if (_user != null)
        {
            await securityContext.AuthenticateMeAsync(_user.Guid);
        }

        var newFolder = _type == FolderType.USER
            ? await fileStorageService.CreateFolderAsync(await globalFolderHelper.FolderMyAsync,
                $"ASC migration files {DateTime.Now:dd.MM.yyyy}")
            : await fileStorageService.CreateRoomAsync($"ASC migration {(_type == FolderType.BUNCH ? "project" : "common")} files {DateTime.Now:dd.MM.yyyy}",
                RoomType.PublicRoom, false, false, new List<FileShareParams>(), 0);
        Log($"create root folder", null);
        
        var matchingIds = new Dictionary<string, FileEntry<int>> { { $"{_folderKey}-{_folder}", newFolder } };

        var orderedFolders = _storage.Folders.OrderBy(f => f.Level);
        foreach (var folder in orderedFolders)
        {
            if (!ShouldImportSharedFolders || !_securities.Any(s => s.EntryId == folder.Id && s.EntryType == 1) 
                || matchingIds[$"{_folderKey}-{folder.ParentId}"].Id != 0) 
            {
                newFolder = await fileStorageService.CreateFolderAsync(matchingIds[$"{_folderKey}-{folder.ParentId}"].Id, folder.Title);
                Log($"create folder {newFolder.Title}", null);
            }
            else
            {
                newFolder = serviceProvider.GetService<Folder<int>>();
                newFolder.Title = folder.Title;
            }
            matchingIds.Add($"{_folderKey}-{folder.Id}", newFolder);
        }

        var fileDao = daoFactory.GetFileDao<int>();

        //docker unzip filesfolder_... instend of files/folder... 
        var folderFiles = _dataReader.GetDirectories("").Select(d => Path.GetFileName(d)).FirstOrDefault(d => d.StartsWith("files"));
        if (folderFiles.Equals("files"))
        {
            folderFiles = "files/folder";
        }
        else
        {
            folderFiles = folderFiles.Split('_')[0];
        }

        foreach (var file in _storage.Files)
        {
            try
            {
                var path =
                    $"{folderFiles}_{(Convert.ToInt32(file.Id) / 1000 + 1) * 1000}/file_{file.Id}/v{file.Version}/content{FileUtility.GetFileExtension(file.Title)}";
                await using var fs = _dataReader.GetEntry(path);

                var newFile = serviceProvider.GetService<File<int>>();
                newFile.ParentId = matchingIds[$"{_folderKey}-{file.Folder}"].Id;
                newFile.Comment = FilesCommonResource.CommentCreate;
                newFile.Title = Path.GetFileName(file.Title);
                newFile.ContentLength = fs.Length;
                newFile.Version = file.Version;
                newFile.VersionGroup = file.VersionGroup;
                if (!ShouldImportSharedFolders || !_securities.Any(s => s.EntryId == file.Folder && s.EntryType == 1) || newFile.ParentId != 0)
                {
                    newFile = await fileDao.SaveFileAsync(newFile, fs);
                }
                if (!matchingIds.ContainsKey($"{_fileKey}-{file.Id}"))
                {
                    matchingIds.Add($"{_fileKey}-{file.Id}", newFile);
                    Log($"create file {file.Title}", null);
                }
            }
            catch(Exception ex)
            {
                Log($"Couldn't create file {file.Title}", ex);
            }
        }

        if (_type != FolderType.USER || !ShouldImportSharedFiles && !ShouldImportSharedFolders)
        {
            return;
        }

        var matchingRoomIds = new Dictionary<int, FileEntry<int>>();
        var aces = new Dictionary<string, AceWrapper>();
        foreach (var security in _securities)
        {
            try
            {
                var entryIsFile = security.EntryType == 2;
                if (entryIsFile && ShouldImportSharedFiles)
                {
                    var key = $"{_fileKey}-{security.EntryId}";
                    await securityContext.AuthenticateMeAsync(_user.Guid);
                    AceWrapper ace = null;
                    if (!aces.ContainsKey($"{security.Security}{matchingIds[key].Id}")) 
                    {
                        try
                        {
                            ace = await fileStorageService.SetExternalLinkAsync(matchingIds[key].Id, FileEntryType.File, Guid.Empty, null, (FileShare)security.Security, requiredAuth: true,
                                primary: false);
                            aces.Add($"{security.Security}{matchingIds[key].Id}", ace);
                        }
                        catch
                        {
                            ace = null;
                            aces.Add($"{security.Security}{matchingIds[key].Id}", null);
                        }
                    }
                    else
                    {
                        ace = aces[$"{security.Security}{matchingIds[key].Id}"];
                    }
                    if (ace != null) {
                        var user = await userManager.GetUsersAsync(Guid.Parse(_mappedGuids[security.Subject]));
                        if (user.Id == Constants.LostUser.Id)
                        {
                            var users = userManager.GetUsers(false, EmployeeStatus.Active,
                                new List<List<Guid>> { new List<Guid> { Guid.Parse(_mappedGuids[security.Subject]) } },
                                new List<Guid>(), new List<Tuple<List<List<Guid>>, List<Guid>>>(), null, null, null, "", false, "firstname",
                                true, 100000, 0).Where(u => u.Id != _user.Guid);
                            await foreach (var u in users)
                            {
                                await securityContext.AuthenticateMeAsync(u.Id);
                                await entryManager.MarkAsRecentByLink(matchingIds[key] as File<int>, ace.Id);
                            }
                        }
                        else
                        {
                            await securityContext.AuthenticateMeAsync(user.Id);
                            await entryManager.MarkAsRecentByLink(matchingIds[key] as File<int>, ace.Id);
                        }
                    }
                }
                else if(ShouldImportSharedFolders)
                {
                    var key = $"{_folderKey}-{security.EntryId}";
                    if (!matchingRoomIds.ContainsKey(security.EntryId)) 
                    {
                        if (_user.UserType == EmployeeType.Collaborator) 
                        {
                            await securityContext.AuthenticateMeAsync(_currentUser);
                        }
                        else
                        {
                            await securityContext.AuthenticateMeAsync(_user.Guid);
                        }
                        var room = await fileStorageService.CreateRoomAsync($"{matchingIds[key].Title}",
                            RoomType.EditingRoom, false, false, new List<FileShareParams>(), 0);

                        orderedFolders = _storage.Folders.Where(f => f.ParentId == security.EntryId).OrderBy(f => f.Level);
                        matchingRoomIds.Add(security.EntryId, room);
                        Log($"create share room {room.Title}", null);

                        if (_user.UserType == EmployeeType.Collaborator)
                        {
                            var aceList = new List<AceWrapper>
                            {
                                new AceWrapper
                                {
                                    Access = FileShare.Collaborator,
                                    Id = _user.Guid
                                }
                            };

                            var collection = new AceCollection<int>
                            {
                                Files = Array.Empty<int>(),
                                Folders = new List<int> { matchingRoomIds[security.EntryId].Id },
                                Aces = aceList,
                                Message = null
                            };

                            await fileStorageService.SetAceObjectAsync(collection, false);
                        }

                        foreach (var folder in orderedFolders)
                        {
                            newFolder = await fileStorageService.CreateFolderAsync(matchingRoomIds[folder.ParentId].Id, folder.Title);
                            matchingRoomIds.Add(folder.Id, newFolder);
                            Log($"create folder {newFolder.Title}", null);
                        }
                        foreach (var file in _storage.Files.Where(f => matchingRoomIds.ContainsKey(f.Folder)))
                        {
                            var path = $"{folderFiles}_{(Convert.ToInt32(file.Id) / 1000 + 1) * 1000}/file_{file.Id}/v{file.Version}/content{FileUtility.GetFileExtension(file.Title)}";
                            await using var fs = _dataReader.GetEntry(path);

                            var newFile = serviceProvider.GetService<File<int>>();
                            newFile.ParentId = matchingRoomIds[security.EntryId].Id;
                            newFile.Comment = FilesCommonResource.CommentCreate;
                            newFile.Title = Path.GetFileName(file.Title);
                            newFile.ContentLength = fs.Length;
                            newFile.Version = file.Version;
                            newFile.VersionGroup = file.VersionGroup;
                            newFile = await fileDao.SaveFileAsync(newFile, fs);
                            Log($"create file {newFile.Title}", null);
                        }
                    }
                    if (_user.UserType == EmployeeType.Collaborator && _currentUser.ID == Guid.Parse(_mappedGuids[security.Subject]))
                    {
                        continue;
                    }

                    var list = new List<AceWrapper>
                    {
                        new AceWrapper
                        {
                            Access = (FileShare)security.Security,
                            Id = Guid.Parse(_mappedGuids[security.Subject])
                        }
                    };

                    var aceCollection = new AceCollection<int>
                    {
                        Files = Array.Empty<int>(),
                        Folders = new List<int> { matchingRoomIds[security.EntryId].Id },
                        Aces = list,
                        Message = null
                    };

                    await fileStorageService.SetAceObjectAsync(aceCollection, false);
                }
            }
            catch(Exception ex)
            {
                Log($"Couldn't share entry {security.EntryId} to {security.Subject}", ex);
            }
        }
    }
}
