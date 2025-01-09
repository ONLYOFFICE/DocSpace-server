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

using ASC.Web.Core;

using Constants = ASC.Core.Users.Constants;

namespace ASC.Migration.Core.Migrators.Provider;

[Transient(typeof(Migrator))]
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
        DisplayUserSettingsHelper displayUserSettingsHelper,
        UserManagerWrapper userManagerWrapper) : base(securityContext, userManager, tenantQuotaFeatureStatHelper, quotaSocketManager, fileStorageService, globalFolderHelper, serviceProvider, daoFactory, entryManager, migrationLogger, authContext, displayUserSettingsHelper, userManagerWrapper)
    {
        MigrationInfo = new MigrationInfo { Name = "Workspace" };
    }

    public override async Task InitAsync(string path, OperationType operation, CancellationToken cancellationToken)
    {
        MigrationLogger.Init();
        _cancellationToken = cancellationToken;

        MigrationInfo.Operation = operation;
        TmpFolder = path;

        var files = Directory.GetFiles(path);
        if (files.Length == 0 || !files.Any(f => f.EndsWith(".gz") || f.EndsWith(".tar")))
        {
            MigrationInfo.FailedArchives = files.ToList();
            throw new Exception("Archive must be .gz or .tar");
        }

        _backup = files.First(f => f.EndsWith(".gz") || f.EndsWith(".tar"));
        MigrationInfo.Files = [Path.GetFileName(_backup)];
        await ReportProgressAsync(1, "start");
    }

    public override async Task<MigrationApiInfo> ParseAsync(bool reportProgress = true)
    {
        if (reportProgress)
        {
            await ReportProgressAsync(5, MigrationResource.Unzipping);
        }
        try
        {
            _dataReader = DataOperatorFactory.GetReadOperator(_backup, reportProgress ? _cancellationToken : CancellationToken.None, false);

            if (_cancellationToken.IsCancellationRequested && reportProgress)
            {
                return null;
            }

            var folders =  _dataReader.GetDirectories("");
            if (folders.Any(f=> Path.GetFileNameWithoutExtension(f).Contains("databases")))
            {
                await InnerParseAsync(reportProgress);
            }
            else
            {
                foreach (var folder in folders)
                {
                    _dataReader.SetFolder(folder);
                    await InnerParseAsync(reportProgress, folders.Count());
                }
            }

        }
        catch(Exception e)
        {
            MigrationInfo.FailedArchives.Add(Path.GetFileName(_backup));
            var error = string.Format(MigrationResource.CanNotParseArchive, Path.GetFileNameWithoutExtension(_backup));
            await ReportProgressAsync(_lastProgressUpdate, error);
            throw new Exception(error, e);
        }
        if (reportProgress)
        {
            await ReportProgressAsync(100, MigrationResource.DataProcessingCompleted);
        }
        return MigrationInfo.ToApiInfo();
    }

    private async Task InnerParseAsync(bool reportProgress, int count = 1)
    {
        if (reportProgress)
        {
            await ReportProgressAsync(_lastProgressUpdate + (double)25 / count, MigrationResource.UnzippingFinished);
        }
        await ParseUsersAsync(reportProgress, count);

        if (reportProgress)
        {
            await ReportProgressAsync(_lastProgressUpdate + (double)10 / count, MigrationResource.DataProcessing);
        }
        ParseGroup();

        if (reportProgress)
        {
            await ReportProgressAsync(_lastProgressUpdate + (double)10 / count, MigrationResource.DataProcessing);
        }

        if (MigrationInfo.CommonStorage == null)
        {
            MigrationInfo.CommonStorage = new MigrationStorage
            {
                Type = FolderType.COMMON
            };
            ParseStorage(MigrationInfo.CommonStorage);
        }
        else
        {
            var commonStorage = new MigrationStorage
            {
                Type = FolderType.COMMON
            };
            ParseAndUnionStorage(commonStorage, MigrationInfo.CommonStorage);
        }

        if (reportProgress)
        {
            await ReportProgressAsync(_lastProgressUpdate + (double)10 / count, MigrationResource.DataProcessing);
        }
        if (MigrationInfo.ProjectStorage == null) 
        {
            MigrationInfo.ProjectStorage = new MigrationStorage
            {
                Type = FolderType.BUNCH
            };
            ParseStorage(MigrationInfo.ProjectStorage);
        }
        else
        {
            var projectStorage = new MigrationStorage
            {
                Type = FolderType.BUNCH
            };
            ParseAndUnionStorage(projectStorage, MigrationInfo.ProjectStorage);
        }
    }

    private void ParseAndUnionStorage(MigrationStorage newStorage, MigrationStorage destinationStorage, string key = "")
    {
        ArgumentNullException.ThrowIfNull(destinationStorage);

        ParseStorage(newStorage, key);

        newStorage.Folders = newStorage.Folders.Select(f =>
        {
            if (f.ParentId.ToString() == newStorage.RootKey)
            {
                f.ParentId = int.Parse(destinationStorage.RootKey);
            }
            return f;
        }).ToList();

        newStorage.Files = newStorage.Files.Select(f =>
        {
            if (f.Folder.ToString() == newStorage.RootKey)
            {
                f.Folder = int.Parse(destinationStorage.RootKey);
            }
            return f;
        }).ToList();

        destinationStorage.BytesTotal += newStorage.BytesTotal;
        destinationStorage.Securities = destinationStorage.Securities.Union(newStorage.Securities).ToList();
        destinationStorage.Files = destinationStorage.Files.Union(newStorage.Files).ToList();
        destinationStorage.Folders = destinationStorage.Folders.Union(newStorage.Folders).ToList();
    }

    private async Task ParseUsersAsync(bool reportProgress, int count)
    {
        await using var stream = _dataReader.GetEntry("databases/core/core_user");
        var data = new DataTable();
        data.ReadXml(stream);
        var progressStep = (double)30 / count / data.Rows.Count;
        foreach (var row in data.Rows.Cast<DataRow>())
        {
            if (reportProgress)
            {
                await ReportProgressAsync(_lastProgressUpdate + progressStep, MigrationResource.DataProcessing);
            }
            if (row["removed"].ToString() == "1" || row["removed"].ToString() == "True")
            {
                continue;
            }
            var key = row["id"].ToString();
            var u = new MigrationUser(DisplayUserSettingsHelper)
            {
                Info = new UserInfo
                {
                    UserName = row["email"].ToString().Split('@').First(),
                    FirstName = row["firstname"].ToString(),
                    LastName = row["lastname"].ToString(),
                    Email = row["email"].ToString()
                }
            };

            var drivePath = (Directory.Exists(Path.Combine(_dataReader.GetFolder(), "userPhotos")) 
                                ? Path.Combine(_dataReader.GetFolder(), "userPhotos") 
                                : null) ?? 
                            (Directory.GetFiles(_dataReader.GetFolder()).Any(f=> Path.GetFileName(f).StartsWith("userPhotos")) ? _dataReader.GetFolder() : null);

            if (drivePath == null)
            {
                u.HasPhoto = false;
            }
            else
            {
                u.PathToPhoto = Directory.GetFiles(drivePath).FirstOrDefault(p => Path.GetFileName(p).Contains(key + "_orig_"));
                u.HasPhoto = u.PathToPhoto != null;
            }

            if(!u.HasPhoto)
            {
                await using var streamPhotos = _dataReader.GetEntry("databases/core/core_userphoto");
                var dataPhotots = new DataTable();
                dataPhotots.ReadXml(streamPhotos);
                foreach (var rowPhoto in dataPhotots.Rows.Cast<DataRow>())
                {
                    if (rowPhoto["userId"].ToString() == key)
                    {
                        var bytes = rowPhoto["photo"] as byte[];
                        var img = SixLabors.ImageSharp.Image.Load(bytes);
                        var format = img.Metadata.DecodedImageFormat;

                        u.PathToPhoto = Path.Combine(_dataReader.GetFolder(), $"{key}.{CommonPhotoManager.GetImgFormatName(format)}");
                        u.HasPhoto = true;

                        await using var fs = new FileStream(u.PathToPhoto, FileMode.Create);
                        await fs.WriteAsync(bytes, _cancellationToken);
                    }
                }
            }

            u.Storage = new MigrationStorage { Type = FolderType.USER };

            var ascUser = await UserManager.GetUserByEmailAsync(u.Info.Email);
            if (ascUser.Status == EmployeeStatus.Terminated)
            {
                continue;
            }
            if (!ascUser.Equals(Constants.LostUser))
            {
                var user = MigrationInfo.ExistUsers.SingleOrDefault(eu => eu.Value.Info.Email == u.Info.Email);
                if (user.Value == null)
                {
                    ParseStorage(u.Storage, key);
                    MigrationInfo.ExistUsers.Add(key, u);
                }
                else
                {
                    ParseAndUnionStorage(u.Storage, user.Value.Storage, key);
                }
            }
            else
            {
                var user = MigrationInfo.Users.SingleOrDefault(eu => eu.Value.Info.Email == u.Info.Email);
                if (user.Value == null)
                {
                    ParseStorage(u.Storage, key);
                    MigrationInfo.Users.Add(key, u);
                }
                else
                {
                    ParseAndUnionStorage(u.Storage, user.Value.Storage, key);
                }
            }
        }
    }

    private void ParseStorage(MigrationStorage storage, string createBy = "")
    {
        //docker unzip filesfolder_... instend of files/folder... 
        var folderFiles = _dataReader.GetDirectories("").Select(Path.GetFileName).FirstOrDefault(d => d.StartsWith("files"));
        folderFiles = folderFiles.Equals("files") ? "files/folder" : folderFiles.Split('_')[0];

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

        var projectTitle = new Dictionary<string, ValueTuple<string, bool, string>>();
        if (storage.Type == FolderType.BUNCH)
        {
            var projectProjects = new Dictionary<string, ValueTuple<string, bool, string>>();
            using var streamProject = _dataReader.GetEntry("databases/projects/projects_projects");
            var dataProject = new DataTable();
            dataProject.ReadXml(streamProject);
            foreach (var row in dataProject.Rows.Cast<DataRow>())
            {
                projectProjects.Add(row["id"].ToString(), (row["title"].ToString(), row["private"].ToString() == "1", row["responsible_id"].ToString()));
            }
            storage.RootKey = "0";
            folderTree.Add("0", -1);


            using var streamBunch = _dataReader.GetEntry("databases/files/files_bunch_objects");

            dataProject = new DataTable();
            dataProject.ReadXml(streamBunch);
            foreach (var row in dataProject.Rows.Cast<DataRow>())
            {
                if (row["right_node"].ToString().StartsWith("projects/project/"))
                {
                    try
                    {
                        var split = row["right_node"].ToString().Split('/');
                        projectTitle.Add(row["left_node"].ToString(), projectProjects[split.Last()]);
                    }
                    catch
                    {

                    }
                }
            }
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
                var id = row["id"].ToString();
                var priv = false;
                var owner = "";
                if (storage.Type == FolderType.BUNCH)
                {
                    if (projectTitle.ContainsKey(id))
                    {
                        var split = row["title"].ToString().Split('_');
                        title = projectTitle[id].Item1;
                        priv = projectTitle[id].Item2;
                        owner = projectTitle[id].Item3;
                    }
                    else
                    {
                        if (folderTree[id] == 0)
                        {
                            continue;
                        }
                    }
                }
                var folder = new MigrationFolder
                {
                    Id = int.Parse(id),
                    ParentId = int.Parse(row["parent_id"].ToString()),
                    Title = title,
                    Level = folderTree[id],
                    Private = priv,
                    Owner = owner
                };
                storage.Folders.Add(folder);
            }
        }
        
        if (storage.Type == FolderType.BUNCH) 
        {
            var remove = new List<string>();
            foreach (var entry in folderTree)
            {
                var id = int.Parse(entry.Key);
                if (storage.Folders.All(f => f.Id != id))
                {
                    remove.Add(entry.Key);
                }
            }
            var removeFolder = new List<MigrationFolder>();
            foreach(var entry in storage.Folders)
            {
                if(entry.ParentId != 0 && storage.Folders.All(f => f.Id != entry.ParentId))
                {
                    remove.Add(entry.Id.ToString());
                    removeFolder.Add(entry);
                }
            }
            foreach (var r in remove)
            {
                folderTree.Remove(r);
            }
            foreach (var rf in removeFolder)
            {
                storage.Folders.Remove(rf);
            }
        }

        using var streamFiles = _dataReader.GetEntry("databases/files/files_file");
        var datafiles = new DataTable();
        datafiles.ReadXml(streamFiles);
        foreach (var row in datafiles.Rows.Cast<DataRow>())
        {
            if (folderTree.ContainsKey(row["folder_id"].ToString()))
            {
                var file = new MigrationFile
                {
                    Id = int.Parse(row["id"].ToString()),
                    Folder = int.Parse(row["folder_id"].ToString()),
                    Title = row["title"].ToString(),
                    Version = int.Parse(row["version"].ToString()),
                    VersionGroup = int.Parse(row["version_group"].ToString()),
                    Comment = row["comment"].ToString(),
                    Created = DateTime.Parse(row["create_on"].ToString()),
                    Modified = DateTime.Parse(row["modified_on"].ToString())
                };
                file.Path = Path.Combine(_dataReader.GetFolder(),$"{folderFiles}_{(Convert.ToInt32(file.Id) / 1000 + 1) * 1000}/file_{file.Id}/v{file.Version}/content{FileUtility.GetFileExtension(file.Title)}");
                storage.Files.Add(file);
                storage.BytesTotal += int.Parse(row["content_length"].ToString());
            }
        }

        switch (storage.Type)
        {
            case FolderType.USER:
                DbExtractFilesSecurity(storage, createBy);
                break;
            case FolderType.BUNCH:
                ExtractProjectSecurity(storage);
                break;
        }
    }

    private void ExtractProjectSecurity(MigrationStorage storage)
    {
        using var streamBunch = _dataReader.GetEntry("databases/files/files_bunch_objects");

        var dataProject = new DataTable();
        dataProject.ReadXml(streamBunch);
        var mapper = new Dictionary<string, string>();
        foreach (var row in dataProject.Rows.Cast<DataRow>())
        {
            if (row["right_node"].ToString().StartsWith("projects/project/") && storage.Folders.Any(f=> f.Id == int.Parse(row["left_node"].ToString())))
            {
                var split = row["right_node"].ToString().Split('/');
                mapper.Add(split.Last(), row["left_node"].ToString());
            }
        }

        using var streamFiles = _dataReader.GetEntry("databases/projects/projects_project_participant");
        var datafiles = new DataTable();
        datafiles.ReadXml(streamFiles);
        foreach (var row in datafiles.Rows.Cast<DataRow>())
        {
            if (row["removed"].ToString() == "0" 
                && mapper.ContainsKey(row["project_id"].ToString()) 
                && storage.Folders.FirstOrDefault(f=> f.Id == int.Parse(mapper[row["project_id"].ToString()])).Private)
            {
                var security = new MigrationSecurity
                {
                    Subject = row["participant_id"].ToString(),
                    EntryId = int.Parse(mapper[row["project_id"].ToString()]),
                    EntryType = 1,
                    Security = (int)Files.Core.Security.FileShare.ContentCreator
                };
                storage.Securities.Add(security);
            }
        }
    }
    
    private void DbExtractFilesSecurity(MigrationStorage storage, string createBy)
    {
        using var streamGroup = _dataReader.GetEntry("databases/files/files_security");
        var data = new DataTable();
        data.ReadXml(streamGroup);

        foreach (var row in data.Rows.Cast<DataRow>())
        {
            var result = int.TryParse(row["entry_id"].ToString(), out var id);
            if (!result)
            {
                continue;
            }
            if (row["owner"].ToString() == createBy && (storage.Files.Select(f => f.Id).ToList().Contains(id) || storage.Folders.Select(f=> f.Id).ToList().Contains(id)))
            {
                var security = new MigrationSecurity
                {
                    Subject = row["subject"].ToString(),
                    EntryId = id,
                    EntryType = int.Parse(row["entry_type"].ToString()),
                    Security = int.Parse(row["security"].ToString()) switch
                    {
                        1 => (int)Files.Core.Security.FileShare.Editing,
                        2 => (int)Files.Core.Security.FileShare.Read,
                        6 => (int)Files.Core.Security.FileShare.Comment,
                        _ => (int)Files.Core.Security.FileShare.None
                    }
                };
                storage.Securities.Add(security);
            }
        }
    }

    private void ParseGroup()
    {
        using var streamGroup = _dataReader.GetEntry("databases/core/core_group");
        var dataGroup = new DataTable();
        dataGroup.ReadXml(streamGroup);

        foreach(var row in dataGroup.Rows.Cast<DataRow>())
        {
            if(int.TryParse(row["removed"].ToString(), out var result))
            {
                if (result == 1)
                {
                    continue;
                }
            }
            else
            {
                if (row["removed"].ToString() == "True")
                {
                    continue;
                }
            }
            var group = new MigrationGroup
            {
                Info = new GroupInfo
                {
                    Name = row["name"].ToString()
                },
                UserKeys = []
            };
            MigrationInfo.Groups.Add(row["id"].ToString(), group);
        }

        using var streamUserGroup = _dataReader.GetEntry("databases/core/core_usergroup");
        var dataUserGroup = new DataTable();
        dataUserGroup.ReadXml(streamUserGroup);

        foreach (var row in dataUserGroup.Rows.Cast<DataRow>())
        {
            if (row["removed"].ToString() == "1" || row["removed"].ToString() == "True")
            {
                continue;
            }
            var groupId = row["groupid"].ToString();
            if (MigrationInfo.Groups.TryGetValue(groupId, out var g))
            {
                g.UserKeys.Add(row["userid"].ToString());
                if (string.Equals(row["ref_type"].ToString(), "1"))
                {
                    g.ManagerKey = row["userid"].ToString();
                }
            }
        }
    }
}
