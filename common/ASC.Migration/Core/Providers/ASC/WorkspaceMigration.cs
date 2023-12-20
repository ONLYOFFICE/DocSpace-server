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
public class WorkspaceMigration : AbstractMigration<WorkspaceMigrationInfo, WorkspaceMigratingUser, WorkspaceMigratingFiles, WorkspaceMigrationGroups>
{
    private string _backup;
    private string _tmpFolder;
    private readonly IServiceProvider _serviceProvider;
    private readonly MigratorMeta _meta;
    private readonly UserManager _userManager;
    private IDataReadOperator _dataReader;
    private Dictionary<string, string> _mappedGuids;
    private WorkspaceMigratingFiles _migratingCommonFiles;
    private WorkspaceMigratingFiles _migratingProjectFiles;
    public override MigratorMeta Meta => _meta;

    public WorkspaceMigration(MigrationLogger migrationLogger,
        IServiceProvider serviceProvider,
        UserManager userManager,
        WorkspaceMigratingFiles migratingCommonFiles,
        WorkspaceMigratingFiles migratingProjectFiles) : base(migrationLogger)
    {
        _meta = new("Workspace", 5, false);
        _serviceProvider = serviceProvider;
        _userManager = userManager;
        _migratingCommonFiles = migratingCommonFiles;
        _migratingProjectFiles = migratingProjectFiles;
    }

    public override void Init(string path, CancellationToken cancellationToken)
    {
        _logger.Init();
        _cancellationToken = cancellationToken;
        var files = Directory.GetFiles(path);
        if (files.Length == 0 || !files.Any(f => f.EndsWith(".gz") || f.EndsWith(".tar")))
        {
            throw new Exception("Folder must not be empty and should contain only .gz or .tar files.");
        }

        _backup = files.First(f => f.EndsWith(".gz") || f.EndsWith(".tar"));

        _migrationInfo = new WorkspaceMigrationInfo();
        _migrationInfo.MigratorName = _meta.Name;
        _tmpFolder = path;
        _mappedGuids = new();
    }
    public override async Task<MigrationApiInfo> Parse(bool reportProgress = true)
    {
        try
        {
            _dataReader = DataOperatorFactory.GetReadOperator(_backup, false);
            if (reportProgress)
            {
                ReportProgress(5, MigrationResource.StartOfDataProcessing);
            }

            await using var stream = _dataReader.GetEntry("databases/core/core_user");
            var data = new DataTable();
            data.ReadXml(stream);
            var progressStep = 70 / data.Rows.Count;
            var i = 1;
            foreach (var row in data.Rows.Cast<DataRow>())
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    if (reportProgress)
                    {
                        ReportProgress(100, MigrationResource.MigrationCanceled);
                    }
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
                        ActivationStatus = EmployeeActivationStatus.Pending,
                        Email = row["email"].ToString(),
                    }
                };
                if (reportProgress)
                {
                    ReportProgress(GetProgress() + progressStep, MigrationResource.DataProcessing + $" {u.Id} ({i++}/{data.Rows.Count})");
                }

                var user = _serviceProvider.GetService<WorkspaceMigratingUser>();
                user.Init(u.Id, u, _tmpFolder, _dataReader, Log, _mappedGuids);
                user.Parse();
                if (!(await _userManager.GetUserByEmailAsync(u.Info.Email)).Equals(Constants.LostUser))
                {
                    _migrationInfo.ExistUsers.Add(u.Id, user);
                }
                else
                {
                    _migrationInfo.Users.Add(u.Id, user);
                }
            }

            var groups = DbExtractGroup();
            var progress = 80;
            foreach (var item in groups)
            {
                ReportProgress(progress, MigrationResource.DataProcessing);
                progress += 10 / groups.Count;
                var group = _serviceProvider.GetService<WorkspaceMigrationGroups>();
                group.Init(item, Log);
                group.Parse();
                _migrationInfo.Groups.Add(group);
            }

            var storage = new WorkspaceStorage
            {
                Files = new List<WorkspaceFile>(),
                Folders = new List<WorkspaceFolder>()
            };
            _migratingCommonFiles.Init(string.Empty, null, _dataReader, storage, Log, _mappedGuids, FolderType.COMMON);
            _migratingCommonFiles.Parse();

            try
            {
                storage = new WorkspaceStorage
                {
                    Files = new List<WorkspaceFile>(), Folders = new List<WorkspaceFolder>()
                };
                _migratingProjectFiles.Init(string.Empty, null, _dataReader, storage, Log, _mappedGuids,
                    FolderType.BUNCH);
                _migratingProjectFiles.Parse();
            }
            catch
            {
                _migratingProjectFiles = null;
            }
        }
        catch (Exception ex)
        {
            _migrationInfo.FailedArchives.Add(Path.GetFileName(_backup));
            Log($"Couldn't parse {Path.GetFileNameWithoutExtension(_backup)} archive", ex);
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

    public override async Task Migrate(MigrationApiInfo migrationInfo)
    {
        ReportProgress(0, MigrationResource.PreparingForMigration);
        _importedUsers = new List<Guid>();
        _migrationInfo.Merge(migrationInfo);
        _migratingCommonFiles.ShouldImport = migrationInfo.ImportCommonFiles;
        _migratingProjectFiles.ShouldImport = migrationInfo.ImportProjectFiles;

        var usersForImport = _migrationInfo.Users
            .Where(u => u.Value.ShouldImport)
            .Select(u => u.Value).ToList();

        var failedUsers = new List<WorkspaceMigratingUser>();
        var usersCount = usersForImport.Count;
        var progressStep = usersCount == 0 ? 25 : 25 / usersCount;
        var i = 1;

        foreach (var user in usersForImport)
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                ReportProgress(100, MigrationResource.MigrationCanceled);
                return;
            }
            ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.UserMigration, user.DisplayName, i++, usersCount));
            try
            {
                var u = migrationInfo.Users.Find(element => element.Key == user.Key);
                user.UserType = u.UserType;

                await user.MigrateAsync();
                _importedUsers.Add(user.Guid);
                _mappedGuids.Add(u.Key, user.Guid.ToString());
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
            progressStep = 25 / groupsCount;
            
            i = 1;
            foreach (var group in groupsForImport)
            {
                if (_cancellationToken.IsCancellationRequested) { ReportProgress(100, MigrationResource.MigrationCanceled); return; }
                ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.GroupMigration, group.GroupName, i++, groupsCount));
                try
                {
                    var key = group.Guid.ToString();
                    group.UsersGuidList = _migrationInfo.Users
                        .Where(user => group.UserGuidList.Exists(u => user.Key == u))
                        .Select(u => u)
                        .ToDictionary(k => k.Key, v => v.Value.Guid);
                    await group.MigrateAsync();
                    _mappedGuids.Add(key, group.Guid.ToString());
                }
                catch (Exception ex)
                {
                    Log($"Couldn't migrate group {group.GroupName} ", ex);
                }
            }
        }

        i = 1;
        foreach (var user in usersForImport)
        {
            ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.UserMigration, user.DisplayName, i++, usersCount));
            try
            {
                if (user.UserType != EmployeeType.User)
                {
                    await user.MigratingFiles.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                failedUsers.Add(user);
                Log($"Couldn't migrate user {user.DisplayName} ({user.Email}) files", ex);
            }
        }

        await _migratingCommonFiles.MigrateAsync();
        if (_migratingProjectFiles != null)
        {
            await _migratingProjectFiles.MigrateAsync();
        }

        _migrationInfo.FailedUsers = failedUsers.Count;
        _migrationInfo.SuccessedUsers = usersCount - _migrationInfo.FailedUsers;
        ReportProgress(100, MigrationResource.MigrationCompleted);
    }

    public override void Dispose()
    {
        base.Dispose();
        _dataReader?.Dispose();
    }
}
