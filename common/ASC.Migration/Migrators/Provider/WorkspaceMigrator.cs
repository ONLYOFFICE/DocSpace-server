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

using ASC.Migration.Core.Migrators.Model;
using ASC.Web.Core.Users;
using ASC.Web.Files.Utils;

using Constants = ASC.Core.Users.Constants;

namespace ASC.Migration.Core.Migrators.Provider;

[Transient]
public class WorkspaceMigrator : Migrator
{
    private CancellationToken _cancellationToken;
    private string _backup;
    private IDataReadOperator _dataReader;
    
    public WorkspaceMigrator(SecurityContext securityContext,
        UserManager userManager,
        TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
        QuotaSocketManager quotaSocketManager, 
        FileStorageService fileStorageService,
        GlobalFolderHelper globalFolderHelper, 
        IServiceProvider serviceProvider,
        IDaoFactory daoFactory, 
        EntryManager entryManager, 
        MigrationLogger migrationLogger,
        AuthContext authContext, 
        DisplayUserSettingsHelper displayUserSettingsHelper) : base(securityContext, userManager, tenantQuotaFeatureStatHelper, quotaSocketManager, fileStorageService, globalFolderHelper, serviceProvider, daoFactory, entryManager, migrationLogger, authContext, displayUserSettingsHelper)
    {
        MigrationInfo = new MigrationInfo() { Name = "Workspace" };
    }

    public override async Task InitAsync(string path, CancellationToken cancellationToken, OperationType operation)
    {
        await MigrationLogger.InitAsync();
        _cancellationToken = cancellationToken;

        MigrationInfo.Operation = operation;
        TmpFolder = path;

        var files = Directory.GetFiles(path);
        if (files.Length == 0 || !files.Any(f => f.EndsWith(".gz") || f.EndsWith(".tar")))
        {
            throw new Exception("Folder must not be empty and should contain only .gz or .tar files.");
        }

        _backup = files.First(f => f.EndsWith(".gz") || f.EndsWith(".tar"));
        MigrationInfo.Files = new List<string> { Path.GetFileName(_backup) };
    }

    public override async Task<MigrationApiInfo> ParseAsync(bool reportProgress = true)
    {
        if (reportProgress)
        {
            ReportProgress(5, MigrationResource.StartOfDataProcessing);
        }
        try
        {
            var currentUser = SecurityContext.CurrentAccount;
            _dataReader = DataOperatorFactory.GetReadOperator(_backup, reportProgress ? _cancellationToken : CancellationToken.None, false);
            if (_cancellationToken.IsCancellationRequested && reportProgress)
            {
                return null;
            }

            if (reportProgress)
            {
                ReportProgress(60, MigrationResource.DataProcessing);
            }
            await ParseUsersAsync();

            if (reportProgress)
            {
                ReportProgress(80, MigrationResource.StartOfDataProcessing);
            }
            ParseGroup();

            if (reportProgress)
            {
                ReportProgress(85, MigrationResource.StartOfDataProcessing);
            }
            MigrationInfo.CommonStorage = new()
            {
                Type = FolderType.COMMON
            };
            ParseStorage(MigrationInfo.CommonStorage);

            if (reportProgress)
            {
                ReportProgress(90, MigrationResource.StartOfDataProcessing);
            }
            MigrationInfo.ProjectStorage = new()
            {
                Type = FolderType.BUNCH
            };
            ParseStorage(MigrationInfo.ProjectStorage);
        }
        catch
        {
            MigrationInfo.FailedArchives.Add(Path.GetFileName(_backup));
            ReportProgress(_lastProgressUpdate, $"Couldn't parse {Path.GetFileNameWithoutExtension(_backup)} archive");
            throw new Exception($"Couldn't parse {Path.GetFileNameWithoutExtension(_backup)} archive");
        }
        if (reportProgress)
        {
            ReportProgress(100, MigrationResource.DataProcessingCompleted);
        }
        return MigrationInfo.ToApiInfo();
    }

    public async Task ParseUsersAsync()
    {
        await using var stream = _dataReader.GetEntry("databases/core/core_user");
        var data = new DataTable();
        data.ReadXml(stream);
        var progressStep = 50 / data.Rows.Count;
        foreach (var row in data.Rows.Cast<DataRow>())
        {
            if (row["removed"].ToString() == "1" || row["removed"].ToString() == "true")
            {
                continue;
            }
            var key = row["id"].ToString();
            var u = new MigrationUser(DisplayUserSettingsHelper)
            {
                Info = new UserInfo()
                {
                    UserName = row["email"].ToString().Split('@').First(),
                    FirstName = row["firstname"].ToString(),
                    LastName = row["lastname"].ToString(),
                    ActivationStatus = EmployeeActivationStatus.Activated,
                    Email = row["email"].ToString(),
                }
            };

            var drivePath = Directory.Exists(Path.Combine(TmpFolder, _dataReader.GetFolder(), "storage", "userPhotos")) ?
            Path.Combine(TmpFolder, _dataReader.GetFolder(), "storage", "userPhotos") : null;
            if (drivePath == null)
            {
                u.HasPhoto = false;
            }
            else
            {
                u.PathToPhoto = Directory.GetFiles(drivePath).FirstOrDefault(p => Path.GetFileName(p).StartsWith(key + "_orig_"));
                u.HasPhoto = u.PathToPhoto != null;
            }

            u.Storage = new MigrationStorage();
            u.Storage.Type = FolderType.USER;

            ParseStorage(u.Storage, key);

            if (!(await UserManager.GetUserByEmailAsync(u.Info.Email)).Equals(Constants.LostUser))
            {
                MigrationInfo.ExistUsers.Add(key, u);
            }
            else
            {
                MigrationInfo.Users.Add(key, u);
            }
        }
    }

    public void ParseStorage(MigrationStorage storage, string createBy = "")
    {
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

        var rootFolders = new List<string>();
        using var streamFolders = _dataReader.GetEntry("databases/files/files_folder");
        var dataFolders = new DataTable();
        dataFolders.ReadXml(streamFolders);
        foreach (var row in dataFolders.Rows.Cast<DataRow>())
        {
            if ((FolderType)int.Parse(row["folder_type"].ToString()) == storage.Type && (
                    (storage.Type == FolderType.USER && row["create_by"].ToString().Equals(createBy)) ||
                    storage.Type == FolderType.COMMON ||
                    (storage.Type == FolderType.BUNCH && row["title"].ToString().StartsWith("projects_project"))))
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
        if (storage.Type == FolderType.BUNCH)
        {
            using var streamProject = _dataReader.GetEntry("databases/projects/projects_projects");
            var dataProject = new DataTable();
            dataProject.ReadXml(streamProject);
            foreach (var row in dataProject.Rows.Cast<DataRow>())
            {
                projectTitle.Add(row["id"].ToString(), row["title"].ToString());
            }
            storage.RootKey = "0";
            folderTree.Add("0", -1);
        }
        else
        {
            storage.RootKey = rootFolders.Count == 0 ? "0" : rootFolders[0];
        }

        foreach (var row in dataFolders.Rows.Cast<DataRow>())
        {
            if (folderTree.ContainsKey(row["id"].ToString()) && row["id"].ToString() != storage.RootKey)
            {
                var title = row["title"].ToString();
                if (storage.Type == FolderType.BUNCH)
                {
                    if (row["parent_id"].ToString() == "0" && row["title"].ToString().StartsWith("projects_project"))
                    {
                        var split = row["title"].ToString().Split('_');
                        title = projectTitle[split.Last()];
                    }
                }
                var id = row["id"].ToString();
                var folder = new MigrationFolder
                {
                    Id = int.Parse(id),
                    ParentId = int.Parse(row["parent_id"].ToString()),
                    Title = title,
                    Level = folderTree[id]
                };
                storage.Folders.Add(folder);
            }
        }

        using var streamFiles = _dataReader.GetEntry("databases/files/files_file");
        var datafiles = new DataTable();
        datafiles.ReadXml(streamFiles);
        foreach (var row in datafiles.Rows.Cast<DataRow>())
        {
            if (folderTree.ContainsKey(row["folder_id"].ToString()))
            {
                var file = new MigrationFile()
                {
                    Id = int.Parse(row["id"].ToString()),
                    Folder = int.Parse(row["folder_id"].ToString()),
                    Title = row["title"].ToString(),
                    Version = int.Parse(row["version"].ToString()),
                    VersionGroup = int.Parse(row["version_group"].ToString())
                };
                file.Path = Path.Combine(_dataReader.GetFolder(),$"{folderFiles}_{(Convert.ToInt32(file.Id) / 1000 + 1) * 1000}/file_{file.Id}/v{file.Version}/content{FileUtility.GetFileExtension(file.Title)}");
                storage.Files.Add(file);
                storage.BytesTotal += int.Parse(row["content_length"].ToString());
            }
        }

        if (storage.Type == FolderType.USER)
        {
            DbExtractFilesSecurity(storage, createBy);
        }
    }
    
    private void DbExtractFilesSecurity(MigrationStorage storage, string createBy)
    {
        using var streamGroup = _dataReader.GetEntry("databases/files/files_security");
        var data = new DataTable();
        data.ReadXml(streamGroup);

        foreach (var row in data.Rows.Cast<DataRow>())
        {
            var id = int.Parse(row["entry_id"].ToString());
            if (row["owner"].ToString() == createBy && (storage.Files.Select(f => f.Id).ToList().Contains(id) || storage.Folders.Select(f=> f.Id).ToList().Contains(id)))
            {
                var security = new MigrationSecurity()
                {
                    Owner = row["owner"].ToString(),
                    Subject = row["subject"].ToString(),
                    SubjectType = int.Parse(row["subject_type"].ToString()),
                    EntryId = id,
                    EntryType = int.Parse(row["entry_type"].ToString()),
                    Security = int.Parse(row["security"].ToString())
                };
                storage.Securities.Add(security);
            }
        }
    }

    public void ParseGroup()
    {
        using var streamGroup = _dataReader.GetEntry("databases/core/core_group");
        var dataGroup = new DataTable();
        dataGroup.ReadXml(streamGroup);

        foreach(var row in dataGroup.Rows.Cast<DataRow>())
        {
            if(int.Parse(row["removed"].ToString()) == 1)
            {
                continue;
            }
            var group = new MigrationGroup
            {
                Info = new()
                {
                    Name = row["name"].ToString(),
                    CategoryID = Guid.Parse(row["categoryid"].ToString()),
                    Sid = row["sid"].ToString()
                },
                UserKeys = new HashSet<string>()
            };
            MigrationInfo.Groups.Add(row["id"].ToString(), group);
        }

        using var streamUserGroup = _dataReader.GetEntry("databases/core/core_usergroup");
        var dataUserGroup = new DataTable();
        dataUserGroup.ReadXml(streamUserGroup);

        foreach (var row in dataUserGroup.Rows.Cast<DataRow>())
        {
            if (int.Parse(row["removed"].ToString()) == 0)
            {
                var groupId = row["groupid"].ToString();
                if (MigrationInfo.Groups.ContainsKey(groupId))
                {
                    var g = MigrationInfo.Groups[groupId];
                    g.UserKeys.Add(row["userid"].ToString());
                    if (string.Equals(row["ref_type"].ToString(), "1"))
                    {
                        g.ManagerKey = row["userid"].ToString();
                    }
                }
            }
        }
    }
}
