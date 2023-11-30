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

    public void Init(string key, WorkspaceMigratingUser user, IDataReadOperator dataReader, WorkspaceStorage storage, Action<string, Exception> log)
    {
        _key = key;
        _user = user;
        _dataReader = dataReader;
        Log = log;
        _storage = storage;
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
                continue;
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
    }

    public override async Task MigrateAsync()
    {
        if (!ShouldImport)
        {
            return;
        }

        await _securityContext.AuthenticateMeAsync(_user.Guid);

        var newFolder = await _fileStorageService.CreateNewFolderAsync(await _globalFolderHelper.FolderMyAsync, $"ASC migration files {DateTime.Now.ToString("dd.MM.yyyy")}");

        var compareIds = new Dictionary<int, int>();
        
        compareIds.Add(int.Parse(_myFolder), newFolder.Id);

        if (_storage.Folders != null)
        {
            _storage.Folders.OrderBy(f => f.Level);
            foreach (var folder in _storage.Folders)
            {
                newFolder = await _fileStorageService.CreateNewFolderAsync(compareIds[folder.ParentId], folder.Title);
                compareIds.Add(folder.Id, newFolder.Id);
            }
        }

        if (_storage.Files != null)
        {
            var fileDao = _daoFactory.GetFileDao<int>();
            foreach (var file in _storage.Files)
            {
                try
                {
                    var path = string.Format("files/folder_{0}/file_{1}/v{2}/content{3}", (Convert.ToInt32(file.Id) / 1000 + 1) * 1000, file.Id, file.Version, FileUtility.GetFileExtension(file.Title));
                    using var fs = _dataReader.GetEntry(path);

                    var newFile = _serviceProvider.GetService<File<int>>();
                    newFile.ParentId = compareIds[file.Folder];
                    newFile.Comment = FilesCommonResource.CommentCreate;
                    newFile.Title = Path.GetFileName(file.Title);
                    newFile.ContentLength = fs.Length;
                    newFile.Version = file.Version;
                    newFile.VersionGroup = file.VersionGroup;
                    newFile = await fileDao.SaveFileAsync(newFile, fs);
                }
                catch(Exception ex)
                {
                    Log($"Couldn't create file {file.Title}", ex);
                }
            }
        }
    }
}
