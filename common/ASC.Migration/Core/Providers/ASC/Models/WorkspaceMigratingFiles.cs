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

using FileShare = ASC.Files.Core.Security.FileShare;

namespace ASC.Migration.Core.Core.Providers.Models;

[Transient]
public class WorkspaceMigratingFiles : MigratingFiles
{
    public override int FoldersCount => _storage.Folders.Count;
    public override int FilesCount => _storage.Files.Count;
    public override long BytesTotal => _bytesTotal;

    private string _key;
    private long _bytesTotal;
    private string _myFolder;
    private IDataReadOperator _dataReader;
    private WorkspaceStorage _storage;
    private List<WorkspaceSecurity> _securities;
    private Dictionary<string, string> _mappedGuids;
    private readonly FileStorageService _fileStorageService;
    private readonly GlobalFolderHelper _globalFolderHelper;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDaoFactory _daoFactory;
    private readonly SecurityContext _securityContext;
    private WorkspaceMigratingUser _user;

    public WorkspaceMigratingFiles(FileStorageService fileStorageService,
        GlobalFolderHelper globalFolderHelper,
        IServiceProvider serviceProvider,
        IDaoFactory daoFactory,
        SecurityContext securityContext)
    {
        _fileStorageService = fileStorageService;
        _globalFolderHelper = globalFolderHelper;
        _serviceProvider = serviceProvider;
        _daoFactory = daoFactory;
        _securityContext = securityContext;
    }

    public void Init(string key, WorkspaceMigratingUser user, IDataReadOperator dataReader, WorkspaceStorage storage, Action<string, Exception> log, Dictionary<string, string> mappedGuids)
    {
        _key = key;
        _user = user;
        _dataReader = dataReader;
        Log = log;
        _storage = storage;
        _securities = new();
        _mappedGuids = mappedGuids;
    }

    public override void Parse()
    {
        using var streamFolders = _dataReader.GetEntry("databases/files/files_folder");
        var dataFolders = new DataTable();
        dataFolders.ReadXml(streamFolders);
        foreach (var row in dataFolders.Rows.Cast<DataRow>())
        {
            if (row["create_by"].ToString().Equals(_key) 
                && (FolderType)int.Parse(row["folder_type"].ToString()) == FolderType.USER)
            {
                _myFolder = row["id"].ToString();
                break;
            }
        }

        using var streamTree = _dataReader.GetEntry("databases/files/files_folder_tree");
        var dataTree = new DataTable();
        dataTree.ReadXml(streamTree);
        var folderTree = new Dictionary<string, int>();
        foreach (var row in dataTree.Rows.Cast<DataRow>())
        {
            if (row["parent_id"].ToString().Equals(_myFolder))
            {
                folderTree.Add(row["folder_id"].ToString(), int.Parse(row["level"].ToString()));
            }
        }

        foreach (var row in dataFolders.Rows.Cast<DataRow>())
        {
            if (row["parent_id"].ToString().Equals(_myFolder))
            {
                var id = row["id"].ToString();
                var folder = new WorkspaceFolder()
                {
                    Id = int.Parse(id),
                    ParentId = int.Parse(row["parent_id"].ToString()),
                    Title = row["title"].ToString(),
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
        DbExtractFilesSecurity();
    }
    
    private void DbExtractFilesSecurity()
    {
        using var streamGroup = _dataReader.GetEntry("databases/files/files_security");
        var data = new DataTable();
        data.ReadXml(streamGroup);

        foreach (var row in data.Rows.Cast<DataRow>())
        {
            if (row["owner"].ToString() == _key)
            {
                var security = new WorkspaceSecurity()
                {
                    Owner = row["owner"].ToString(),
                    Subject = row["subject"].ToString(),
                    SubjectType = int.Parse(row["subject_type"].ToString()),
                    EntryId = int.Parse(row["entry_id"].ToString()),
                    EntryType = int.Parse(row["entry_type"].ToString()),
                    Security = int.Parse(row["security"].ToString())
                };
                _securities.Add(security);
            }
        }
    }

    public override async Task MigrateAsync()
    {
        if (!ShouldImport)
        {
            return;
        }

        await _securityContext.AuthenticateMeAsync(_user.Guid);

        var newFolder = await _fileStorageService.CreateNewFolderAsync(await _globalFolderHelper.FolderMyAsync, $"ASC migration files {DateTime.Now:dd.MM.yyyy}");

        var matchingIds = new Dictionary<int, int> { { int.Parse(_myFolder), newFolder.Id } };

        var orderedFolders = _storage.Folders.OrderBy(f => f.Level);
        foreach (var folder in orderedFolders)
        {
            newFolder = await _fileStorageService.CreateNewFolderAsync(matchingIds[folder.ParentId], folder.Title);
            matchingIds.Add(folder.Id, newFolder.Id);
        }

        var fileDao = _daoFactory.GetFileDao<int>();
        foreach (var file in _storage.Files)
        {
            try
            {
                var path =
                    $"files/folder_{(Convert.ToInt32(file.Id) / 1000 + 1) * 1000}/file_{file.Id}/v{file.Version}/content{FileUtility.GetFileExtension(file.Title)}";
                await using var fs = _dataReader.GetEntry(path);

                var newFile = _serviceProvider.GetService<File<int>>();
                newFile.ParentId = matchingIds[file.Folder];
                newFile.Comment = FilesCommonResource.CommentCreate;
                newFile.Title = Path.GetFileName(file.Title);
                newFile.ContentLength = fs.Length;
                newFile.Version = file.Version;
                newFile.VersionGroup = file.VersionGroup;
                newFile = await fileDao.SaveFileAsync(newFile, fs);
                matchingIds.Add(file.Id, newFile.Id);
            }
            catch(Exception ex)
            {
                Log($"Couldn't create file {file.Title}", ex);
            }
        }

        foreach (var security in _securities)
        {
            var list = new List<AceWrapper>
            {
                new AceWrapper
                {
                    Access = (FileShare)security.Security,
                    Id = Guid.Parse(_mappedGuids[security.Subject]),
                    SubjectGroup = security.EntryType == 2
                }
            };

            var entryIsFile = _storage.Files.Exists(el => el.Id == security.EntryId);
            var aceCollection = new AceCollection<int>
            {
                Files = entryIsFile ? new List<int> { matchingIds[security.EntryId] } : [],
                Folders = entryIsFile ? [] : new List<int> { matchingIds[security.EntryId] },
                Aces = list,
                Message = null
            };
            
            await _fileStorageService.SetAceObjectAsync(aceCollection, false);
        }
    }
}
