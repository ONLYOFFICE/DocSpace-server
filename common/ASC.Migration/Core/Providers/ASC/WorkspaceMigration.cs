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

using Constants = ASC.Core.Users.Constants;

namespace ASC.Migration.Core.Core.Providers;

[Scope]
public class WorkspaceMigration(
    MigrationLogger migrationLogger,
    IServiceProvider serviceProvider,
    UserManager userManager,
    WorkspaceMigratingFiles migratingCommonFiles,
    WorkspaceMigratingFiles migratingProjectFiles,
    SecurityContext securityContext)
    : AbstractMigration<WorkspaceMigrationInfo, WorkspaceMigratingUser, WorkspaceMigratingFiles,
        WorkspaceMigrationGroups>(migrationLogger)
{
    private string _backup;
    private string _tmpFolder;
    private readonly MigratorMeta _meta = new("Workspace", 5, false);
    private IDataReadOperator _dataReader;
    private Dictionary<string, string> _mappedGuids;
    public override MigratorMeta Meta => _meta;

    public override async Task InitAsync(string path, CancellationToken cancellationToken, string operation)
    {
        await _logger.InitAsync();
        _cancellationToken = cancellationToken;
        

        _migrationInfo = new WorkspaceMigrationInfo();
        _migrationInfo.MigratorName = _meta.Name;
        _migrationInfo.Operation = operation;
        _tmpFolder = path;
        _mappedGuids = new();

        var files = Directory.GetFiles(path);
        if (files.Length == 0 || !files.Any(f => f.EndsWith(".gz") || f.EndsWith(".tar")))
        {
            throw new Exception("Folder must not be empty and should contain only .gz or .tar files.");
        }

        _backup = files.First(f => f.EndsWith(".gz") || f.EndsWith(".tar"));
        _migrationInfo.Files = new List<string> { Path.GetFileName(_backup) };
    }
    public override async Task<MigrationApiInfo> ParseAsync(bool reportProgress = true)
    {
        if (reportProgress)
        {
            ReportProgress(5, MigrationResource.StartOfDataProcessing);
        }
        try
        {
            var currentUser = securityContext.CurrentAccount;
            _dataReader = DataOperatorFactory.GetReadOperator(_backup, reportProgress ? _cancellationToken : CancellationToken.None, false);
            if (_cancellationToken.IsCancellationRequested && reportProgress)
            {
                return null;
            }
            await using var stream = _dataReader.GetEntry("databases/core/core_user");
            var data = new DataTable();
            data.ReadXml(stream);
            var progressStep = 50 / data.Rows.Count;
            foreach (var row in data.Rows.Cast<DataRow>())
            {
                if (_cancellationToken.IsCancellationRequested && reportProgress)
                {
                    return null;
                }

                if (row["removed"].ToString() == "1" || row["removed"].ToString() == "true")
                {
                    continue;
                } 
                var u = new WorkspaceUser()
                {
                    Id = row["id"].ToString(),
                    Info = new UserInfo()
                    {
                        UserName = row["email"].ToString().Split('@').First(),
                        FirstName = row["firstname"].ToString(),
                        LastName = row["lastname"].ToString(),
                        ActivationStatus = EmployeeActivationStatus.Activated,
                        Email = row["email"].ToString(),
                    }
                };
                if (reportProgress)
                {
                    ReportProgress(GetProgress() + progressStep, MigrationResource.DataProcessing);
                }

                var user = serviceProvider.GetService<WorkspaceMigratingUser>();
                user.Init(u.Id, u, _tmpFolder, _dataReader, Log, _mappedGuids, currentUser);
                user.Parse();
                if (!(await userManager.GetUserByEmailAsync(u.Info.Email)).Equals(Constants.LostUser))
                {
                    _migrationInfo.ExistUsers.Add(u.Id, user);
                }
                else
                {
                    _migrationInfo.Users.Add(u.Id, user);
                }
            }

            var groups = DbExtractGroup();
            var progress = 60;
            foreach (var item in groups)
            {
                if (_cancellationToken.IsCancellationRequested && reportProgress)
                {
                    return null;
                }
                progress += 20 / groups.Count;
                if (reportProgress)
                {
                    ReportProgress(progress, MigrationResource.DataProcessing);
                }
                
                var group = serviceProvider.GetService<WorkspaceMigrationGroups>();
                group.Init(item, Log);
                group.Parse();
                _migrationInfo.Groups.Add(group);
            }

            if (reportProgress)
            {
                ReportProgress(80, MigrationResource.DataProcessing);
            }

            var storage = new WorkspaceStorage
            {
                Files = new List<WorkspaceFile>(),
                Folders = new List<WorkspaceFolder>()
            };
            migratingCommonFiles.Init(string.Empty, null, _dataReader, storage, Log, _mappedGuids, FolderType.COMMON, currentUser);
            migratingCommonFiles.Parse();

            try
            {
                if (reportProgress)
                {
                    ReportProgress(90, MigrationResource.DataProcessing);
                }
                storage = new WorkspaceStorage
                {
                    Files = new List<WorkspaceFile>(), Folders = new List<WorkspaceFolder>()
                };
                migratingProjectFiles.Init(string.Empty, null, _dataReader, storage, Log, _mappedGuids,
                    FolderType.BUNCH, currentUser);
                migratingProjectFiles.Parse();
            }
            catch
            {
                migratingProjectFiles = null;
            }
        }
        catch (Exception ex)
        {
            _migrationInfo.FailedArchives.Add(Path.GetFileName(_backup));
            Log($"Couldn't parse {Path.GetFileNameWithoutExtension(_backup)} archive", ex);
            throw new Exception($"Couldn't parse {Path.GetFileNameWithoutExtension(_backup)} archive");
        }
        if (reportProgress)
        {
            ReportProgress(100, MigrationResource.DataProcessingCompleted);
        }
        return _migrationInfo.ToApiInfo();
    }

    private List<WorkspaceGroup> DbExtractGroup()
    {
        try
        {
            using var streamGroup = _dataReader.GetEntry("databases/core/core_group");
            var dataGroup = new DataTable();
            dataGroup.ReadXml(streamGroup);

            var groups = (from row in dataGroup.Rows.Cast<DataRow>()
                where int.Parse(row["removed"].ToString()) == 0
                select new WorkspaceGroup
                {
                    Id = Guid.Parse(row["id"].ToString()),
                    Name = row["name"].ToString(),
                    CategoryId = Guid.Parse(row["categoryid"].ToString()),
                    Sid = row["sid"].ToString(),
                    UsersUId = new HashSet<string>(),
                    ManagersUId = new HashSet<string>()
                }).ToList();

            using var streamUserGroup = _dataReader.GetEntry("databases/core/core_usergroup");
            var dataUserGroup = new DataTable();
            dataUserGroup.ReadXml(streamUserGroup);

            foreach (var row in dataUserGroup.Rows.Cast<DataRow>())
            {
                if (int.Parse(row["removed"].ToString()) == 0)
                {
                    var groupId = Guid.Parse(row["groupid"].ToString());
                    var g = groups.FirstOrDefault(g => g.Id == groupId);
                    g?.UsersUId.Add(row["userid"].ToString());
                    if (string.Equals(row["ref_type"].ToString(), "1"))
                    {
                        g?.ManagersUId.Add(row["userid"].ToString());
                    }
                }
            }

            return groups;
        }
        catch
        {
            return new();
        }
    }

    public override async Task MigrateAsync(MigrationApiInfo migrationInfo)
    {
        ReportProgress(0, MigrationResource.PreparingForMigration);
        var currentUser = securityContext.CurrentAccount;
        _importedUsers = new List<Guid>();
        _migrationInfo.Merge(migrationInfo);
        migratingCommonFiles.ShouldImport = migrationInfo.ImportCommonFiles;
        migratingProjectFiles.ShouldImport = migrationInfo.ImportProjectFiles;

        var usersForImport = _migrationInfo.Users.ToList();

        var failedUsers = new List<WorkspaceMigratingUser>();
        var usersCount = usersForImport.Count;
        var progressStep = usersCount == 0 ? 25 : 25 / usersCount;
        var i = 1;

        foreach (var kv in usersForImport)
        {
            var key = kv.Key;
            var user = kv.Value;
            ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.UserMigration, user.DisplayName, i++, usersCount));
            try
            {
                await user.MigrateAsync();
                if (user.Guid != Constants.LostUser.Id && !user.Removed) 
                {
                    _mappedGuids.Add(key, user.Guid.ToString());
                }
                if (user.ShouldImport)
                {
                    _importedUsers.Add(user.Guid);
                }
            }
            catch (Exception ex)
            {
                failedUsers.Add(user);
                Log($"Couldn't migrate user {user.DisplayName} ({user.Email})", ex);
            }
        }

        var groupsForImport = _migrationInfo.Groups
            .Where(g => g.ShouldImport)
            .Select(g => g).ToList();
        
        var groupsCount = groupsForImport.Count;
        if (groupsCount != 0)
        {
            progressStep = groupsCount == 0 ? 25 : 25 / groupsCount;
            
            i = 1;
            foreach (var group in groupsForImport)
            {
                ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.GroupMigration, group.GroupName, i++, groupsCount));
                try
                {
                    var key = group.Guid.ToString();
                    group.UsersGuidList = _migrationInfo.Users
                        .Where(user => user.Value.Guid != Constants.LostUser.Id && !user.Value.Removed)
                        .Where(user => group.UserGuidList.Exists(u => user.Key == u))
                        .Select(u => u)
                        .ToDictionary(k => k.Key, v => v.Value.Guid);
                    if (group.UsersGuidList.Count != 0)
                    {
                        await group.MigrateAsync();
                        _mappedGuids.Add(key, group.Guid.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Log($"Couldn't migrate group {group.GroupName} ", ex);
                }
            }
        }

        i = 1;
        progressStep = usersCount == 0 ? 25 : 25 / usersCount;
        foreach (var kv in usersForImport)
        {
            var user = kv.Value;
            ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.MigratingUserFiles, user.DisplayName, i++, usersCount));
            try
            {
                await user.MigratingFiles.MigrateAsync();
            }
            catch (Exception ex)
            {
                failedUsers.Add(user);
                Log($"Couldn't migrate user {user.DisplayName} ({user.Email}) files", ex);
            }
        }

        await securityContext.AuthenticateMeAsync(currentUser);
        ReportProgress(70, string.Format(MigrationResource.MigrationCommonFiles));
        await migratingCommonFiles.MigrateAsync();
        if (migratingProjectFiles != null)
        {
            ReportProgress(80, string.Format(MigrationResource.MigrationProjectFiles));
            await migratingProjectFiles.MigrateAsync();
        }

        if (Directory.Exists(_tmpFolder))
        {
            Directory.Delete(_tmpFolder, true);
        }

        _migrationInfo.FailedUsers = failedUsers.Count;
        _migrationInfo.SuccessedUsers = usersForImport.Where(u => u.Value.ShouldImport).Count() - _migrationInfo.FailedUsers;
        ReportProgress(100, MigrationResource.MigrationCompleted);
    }
}
