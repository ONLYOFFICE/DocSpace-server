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

namespace ASC.Migration.NextcloudWorkspace;

[Scope]
public class NextcloudWorkspaceMigration : AbstractMigration<NcMigrationInfo, NcMigratingUser, NcMigratingFiles, NcMigratingGroups>
{
    private string _takeout;
    private string _tmpFolder;
    private readonly SecurityContext _securityContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly UserManager _userManager;
    private readonly MigratorMeta _meta;
    public override MigratorMeta Meta => _meta;

    public NextcloudWorkspaceMigration(
        SecurityContext securityContext,
        MigrationLogger migrationLogger,
        IServiceProvider serviceProvider,
        UserManager userManager) : base(migrationLogger)
    {
        _securityContext = securityContext;
        _meta = new("Nextcloud", 5, false);
        _serviceProvider = serviceProvider;
        _userManager = userManager;
    }

    public override void Init(string path, CancellationToken cancellationToken, string operation)
    {
        _logger.Init();
        _cancellationToken = cancellationToken;


        _migrationInfo = new NcMigrationInfo();
        _migrationInfo.MigratorName = _meta.Name;
        _migrationInfo.Operation = operation;

        var files = Directory.GetFiles(path);
        if (files.Length == 0 || !files.Any(f => f.EndsWith(".zip")))
        {
            throw new Exception("Folder must not be empty and should contain only .zip files.");
        }
        foreach (var t in files)
        {
            if (t.EndsWith(".zip"))
            {
                _takeout = t;
                break;
            }
        }
        _migrationInfo.Files = new List<string> { Path.GetFileName(_takeout) };
        _tmpFolder = path;
    }

    public override async Task<MigrationApiInfo> ParseAsync(bool reportProgress = true)
    {
        if (reportProgress)
        {
            ReportProgress(5, MigrationResource.Unzipping);
        }
        try
        {
            try
            {
                using (var archive = ZipFile.OpenRead(_takeout))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(Path.Combine(_tmpFolder, entry.FullName));
                        }
                        else
                        {
                            var dir = Path.GetDirectoryName(Path.Combine(_tmpFolder, entry.FullName));
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                            entry.ExtractToFile(Path.Combine(_tmpFolder, entry.FullName));
                        }
                        if (_cancellationToken.IsCancellationRequested && reportProgress)
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Couldn't to unzip {_takeout}", ex);
            }
            if (reportProgress)
            {
                ReportProgress(30, MigrationResource.UnzippingFinished);
            }
            var bdFile = Directory.GetFiles(Directory.GetDirectories(_tmpFolder)[0], "*.bak")[0];
            if (bdFile == null)
            {
                throw new Exception();
            }
            if (reportProgress)
            {
                ReportProgress(40, MigrationResource.DumpParse);
            }
            var users = DbExtractUser(bdFile);
            var progress = 40;
            foreach (var u in users)
            {
                if (_cancellationToken.IsCancellationRequested && reportProgress)
                {
                    return null;
                }
                if (reportProgress)
                {
                    ReportProgress(progress, MigrationResource.DataProcessing);
                    progress += 50 / users.Count;
                }
                if (u.Data.DisplayName != null)
                {
                    try
                    {
                        var userName = u.Data.DisplayName.Split(' ');
                        u.Data.DisplayName = userName.Length > 1 ? $"{userName[0]} {userName[1]}".Trim() : userName[0].Trim();
                        var user = _serviceProvider.GetService<NcMigratingUser>();
                        user.Init(u, Directory.GetDirectories(_tmpFolder)[0], Log);
                        user.Parse();
                        if (user.Email.IsNullOrEmpty())
                        {
                            _migrationInfo.WithoutEmailUsers.Add(u.Uid, user);
                        }
                        else if (!(await _userManager.GetUserByEmailAsync(user.Email)).Equals(ASC.Core.Users.Constants.LostUser))
                        {
                            _migrationInfo.ExistUsers.Add(u.Uid, user);
                        }
                        else
                        {
                            _migrationInfo.Users.Add(u.Uid, user);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Couldn't parse user {u.Data.DisplayName}", ex);
                    }
                }
            }

            var groups = DbExtractGroup(bdFile);
            progress = 80;
            foreach (var item in groups)
            {
                ReportProgress(progress, MigrationResource.DataProcessing);
                progress += 10 / groups.Count;
                var group = _serviceProvider.GetService<NcMigratingGroups>();
                group.Init(item, Log);
                group.Parse();
                _migrationInfo.Groups.Add(group);
            }
        }
        catch (Exception ex)
        {
            _migrationInfo.FailedArchives.Add(Path.GetFileName(_takeout));
            Log($"Couldn't parse {Path.GetFileNameWithoutExtension(_takeout)} archive", ex);
        }
        if (reportProgress)
        {
            ReportProgress(100, MigrationResource.DataProcessingCompleted);
        }
        return _migrationInfo.ToApiInfo();
    }

    private List<NCGroup> DbExtractGroup(string dbFile)
    {
        var groups = new List<NCGroup>();

        var sqlFile = File.ReadAllText(dbFile);

        var groupList = GetDumpChunk("oc_groups", sqlFile);
        if (groupList == null)
        {
            return groups;
        }

        groups.AddRange(groupList.Select(group => new NCGroup
        {
            GroupGid = group.Split(',').First().Trim('\''),
            UsersUid = []
        }));

        var usersInGroups = GetDumpChunk("oc_group_user", sqlFile);
        foreach (var user in usersInGroups)
        {
            var userGroupGid = user.Split(',').First().Trim('\'');
            var userUid = user.Split(',').Last().Trim('\'');
            groups.Find(ggid => userGroupGid == ggid.GroupGid).UsersUid.Add(userUid);
        }

        return groups;
    }

    private List<NCUser> DbExtractUser(string dbFile)
    {
        var userDataList = new Dictionary<string, NCUser>();

        var sqlFile = File.ReadAllText(dbFile);

        var accounts = GetDumpChunk("oc_accounts", sqlFile);
        if (accounts == null)
        {
            return userDataList.Values.ToList();
        }

        foreach (var account in accounts)
        {
            var userId = account.Split(',').First().Trim('\'');

            userDataList.Add(userId, new NCUser
            {
                Uid = userId,
                Data = new NCUserData(),
                Storages = new NCStorages()
            });
        }

        var accountsData = GetDumpChunk("oc_accounts_data", sqlFile);
        if (accountsData != null)
        {
            foreach (var accountData in accountsData)
            {
                var values = accountData.Split(',')
                    .Select(s => s.Trim('\'')).ToArray();
                userDataList.TryGetValue(values[1], out var user);
                if (user == null)
                {
                    continue;
                }

                switch (values[2])
                {
                    case "displayname":
                        user.Data.DisplayName = values[3];
                        break;
                    case "address":
                        user.Data.Address = values[3];
                        break;
                    case "email":
                        user.Data.Email = values[3];
                        break;
                    case "phone":
                        user.Data.Phone = values[3];
                        break;
                    case "twitter":
                        user.Data.Twitter = values[3];
                        break;
                }
            }
        }
        else
        {
            throw new Exception();
        }

        var storages = GetDumpChunk("oc_storages", sqlFile);
        if (storages != null)
        {
            foreach (var storage in storages)
            {
                var values = storage.Split(',')
                           .Select(s => s.Trim('\'')).ToArray();
                var uid = values[1].Split(':').Last();
                userDataList.TryGetValue(uid, out var user);
                if (user == null)
                {
                    continue;
                }

                user.Storages.NumericId = int.Parse(values[0]);
                user.Storages.Id = values[1];
                user.Storages.FileCache = new List<NCFileCache>();
            }
        }

        var storagesDict = userDataList.Values
            .Select(u => u.Storages)
            .ToDictionary(s => s.NumericId, s => s);
        var fileCaches = GetDumpChunk("oc_filecache", sqlFile);
        if (fileCaches != null)
        {
            foreach (var cache in fileCaches)
            {
                var values = cache.Split(',')
                           .Select(s => s.Trim('\'')).ToArray();
                var storageId = int.Parse(values[1]);
                storagesDict.TryGetValue(storageId, out var storage);
                if (storage == null)
                {
                    continue;
                }

                storage.FileCache.Add(new NCFileCache()
                {
                    FileId = int.Parse(values[0]),
                    Path = values[2],
                    Share = new List<NCShare>()
                });
            }
        }

        return userDataList.Values.ToList();
    }

    private IEnumerable<string> GetDumpChunk(string tableName, string dump)
    {
        var regex = new Regex($"INSERT INTO `{tableName}` VALUES (.*);");
        var match = regex.Match(dump);
        if (!match.Success)
        {
            return null;
        }

        var entryRegex = new Regex(@"(\(.*?\))[,;]");
        var accountDataMatches = entryRegex.Matches(match.Groups[1].Value + ";");
        return accountDataMatches.Cast<Match>()
            .Select(m => m.Groups[1].Value.Trim(new[] { '(', ')' }));
    }

    public override async Task MigrateAsync(MigrationApiInfo migrationApiInfo)
    {
        ReportProgress(0, MigrationResource.PreparingForMigration);
        _importedUsers = new List<Guid>();
        _migrationInfo.Merge(migrationApiInfo);

        var usersForImport = _migrationInfo.Users
            .Where(u => u.Value.ShouldImport)
            .Select(u => u.Value).ToList();

        var failedUsers = new List<NcMigratingUser>();
        var usersCount = usersForImport.Count;
        var progressStep = usersCount == 0 ? 25 : 25 / usersCount;
        var i = 1;
        foreach (var user in usersForImport)
        {
            ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.UserMigration, user.DisplayName, i++, usersCount));
            try
            {
                var u = migrationApiInfo.Users.Find(element => element.Key == user.Key);
                user.DataСhange(u);
                user.UserType = u.UserType;

                await user.MigrateAsync();
                _importedUsers.Add(user.Guid);
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
            //Create all groups
            i = 1;
            foreach (var group in groupsForImport)
            {
                ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.GroupMigration, group.GroupName, i++, groupsCount));
                try
                {
                    group.UsersGuidList = _migrationInfo.Users
                    .Where(user => group.UserGuidList.Exists(u => user.Key == u))
                    .Select(u => u)
                    .ToDictionary(k => k.Key, v => v.Value.Guid);
                    await group.MigrateAsync();
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
            if (failedUsers.Contains(user))
            {
                ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.UserSkipped, user.DisplayName, i, usersCount));
                continue;
            }

            var smallStep = progressStep / 3;

            try
            {
                var currentUser = _securityContext.CurrentAccount;
                await _securityContext.AuthenticateMeAsync(user.Guid);
                user.MigratingFiles.SetUsersDict(usersForImport.Except(failedUsers));
                user.MigratingFiles.SetGroupsDict(groupsForImport);
                await user.MigratingFiles.MigrateAsync();
                await _securityContext.AuthenticateMeAsync(currentUser.ID);
            }
            catch (Exception ex)
            {
                Log($"Couldn't migrate user {user.DisplayName} ({user.Email}) files", ex);
            }
            finally
            {
                ReportProgress(GetProgress() + smallStep, string.Format(MigrationResource.MigratingUserFiles, user.DisplayName, i, usersCount));
            }
            i++;
        }

        if (Directory.Exists(_tmpFolder))
        {
            Directory.Delete(_tmpFolder, true);
        }

        _migrationInfo.FailedUsers = failedUsers.Count;
        _migrationInfo.SuccessedUsers = usersForImport.Count - _migrationInfo.FailedUsers;
        ReportProgress(100, MigrationResource.MigrationCompleted);
    }
}
