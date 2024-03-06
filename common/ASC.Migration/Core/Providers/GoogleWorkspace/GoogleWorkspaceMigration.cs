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



using System.Linq;

using net.openstack.Providers.Rackspace.Objects.Databases;

namespace ASC.Migration.GoogleWorkspace;

[Scope]
public class GoogleWorkspaceMigration(
    MigrationLogger migrationLogger,
    SecurityContext securityContext,
    TempPath tempPath,
    IServiceProvider serviceProvider,
    UserManager userManager)
    : AbstractMigration<GwsMigrationInfo, GwsMigratingUser, GwsMigratingFiles, GWSMigratingGroups>(migrationLogger)
{
    private string[] _takeouts;
    private readonly MigratorMeta _meta = new("GoogleWorkspace", 5, true);
    private string _path;
    public override MigratorMeta Meta => _meta;

    public override async Task InitAsync(string path, CancellationToken cancellationToken, string operation)
    {
        await _logger.InitAsync();
        _cancellationToken = cancellationToken;
        
        _migrationInfo = new GwsMigrationInfo();
        _migrationInfo.MigratorName = _meta.Name;
        _migrationInfo.Operation = operation;

        var files = Directory.GetFiles(path);
        _path = path;
        if (files.Length == 0 || !files.Any(f => f.EndsWith(".zip")))
        {
            throw new Exception("Folder must not be empty and should contain .zip files.");
        }

        _takeouts = files.Where(item => item.EndsWith(".zip")).ToArray();
        _migrationInfo.Files = _takeouts.Select(Path.GetFileName).ToList();
        _migrationInfo.Path = path;
    }

    public override async Task<MigrationApiInfo> ParseAsync(bool reportProgress = true)
    {
        if (reportProgress)
        {
            ReportProgress(5, MigrationResource.StartOfDataProcessing);
        }

        var progressStep = 90 / _takeouts.Length;
        var i = 1;
        foreach (var takeout in _takeouts)
        {
            if (_cancellationToken.IsCancellationRequested && reportProgress)
            {
                return null;
            }

            if (reportProgress)
            {
                ReportProgress(GetProgress() + progressStep, MigrationResource.DataProcessing + $" {takeout} ({i++}/{_takeouts.Length})");
            }

            var tmpFolder = Path.Combine(tempPath.GetTempPath(), Path.GetFileNameWithoutExtension(takeout)); 
            var key = Path.GetFileName(takeout);
            try
            {
                using (var archive = ZipFile.OpenRead(takeout))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(Path.Combine(tmpFolder, entry.FullName));
                        }
                        else
                        {
                            var dir = Path.GetDirectoryName(Path.Combine(tmpFolder, entry.FullName));
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                            entry.ExtractToFile(Path.Combine(tmpFolder, entry.FullName));
                        }
                        if (_cancellationToken.IsCancellationRequested && reportProgress)
                        {
                            return null;
                        }
                    }
                }
                var rootFolder = Path.Combine(tmpFolder, "Takeout");

                if (!Directory.Exists(rootFolder))
                {
                    throw new Exception("Takeout zip does not contain root 'Takeout' folder.");
                }
                var directories = Directory.GetDirectories(rootFolder);
                if (directories.Length == 1 && directories[0].Split(Path.DirectorySeparatorChar).Last() == "Groups")
                {
                    var group = serviceProvider.GetService<GWSMigratingGroups>();
                    group.Init(rootFolder, Log);
                    group.Parse();
                    _migrationInfo.Groups.Add(group);
                }
                else
                {
                    var user = serviceProvider.GetService<GwsMigratingUser>();
                    user.Init(key, rootFolder, Log, securityContext.CurrentAccount);
                    user.Parse();
                    if (user.Email.IsNullOrEmpty())
                    {
                        _migrationInfo.WithoutEmailUsers.Add(key, user);
                    }
                    else if ((await userManager.GetUserByEmailAsync(user.Email)) != ASC.Core.Users.Constants.LostUser)
                    {
                        if (!_migrationInfo.ExistUsers.Any(u => u.Value.Email == user.Email) || _migrationInfo.Operation == "migration")
                        {
                            _migrationInfo.ExistUsers.Add(key, user);
                        }
                    }
                    else
                    {
                        if (!_migrationInfo.Users.Any(u => u.Value.Email == user.Email) || _migrationInfo.Operation == "migration") 
                        {
                            _migrationInfo.Users.Add(key, user);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _migrationInfo.FailedArchives.Add(key);
                Log($"Couldn't parse user from {key} archive", ex);
            }
            finally
            {
                if (Directory.Exists(tmpFolder))
                {
                    Directory.Delete(tmpFolder, true);
                }
            }
        }
        if (reportProgress)
        {
            ReportProgress(100, MigrationResource.DataProcessingCompleted);
        }

        return _migrationInfo.ToApiInfo();
    }

    public override async Task MigrateAsync(MigrationApiInfo migrationApiInfo)
    {
        ReportProgress(0, MigrationResource.PreparingForMigration);
        _importedUsers = new List<Guid>();
        _migrationInfo.Merge(migrationApiInfo);

        var usersForImport = _migrationInfo.Users
            .Where(u => u.Value.ShouldImport)
            .Select(u => u.Value).ToList();

        var failedUsers = new List<GwsMigratingUser>();
        var usersCount = usersForImport.Count;
        var progressStep = usersCount == 0 ? 25 : 25 / usersCount;
        // Add all users first
        var i = 1;
        foreach (var user in usersForImport)
        {
            ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.UserMigration, user.DisplayName, i++, usersCount));
            try
            {
                var elem = migrationApiInfo.Users.Find(element => element.Key == user.Key);
                user.DataСhange();
                user.UserType = elem.UserType;

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
                .Where(g => g.ShouldImport).ToList();
        var groupsCount = groupsForImport.Count;
        if (groupsCount != 0)
        {
            progressStep = groupsCount == 0 ? 25 : 25 / groupsCount;
            //Create all groups
            i = 1;
            foreach (var group in groupsForImport)
            {
                ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.GroupMigration, group.GroupName, i++, groupsCount));
                try
                {
                    await group.MigrateAsync();
                }
                catch (Exception ex)
                {
                    Log($"Couldn't migrate group {group.GroupName} ", ex);
                }
            }
        }

        i = 1;
        progressStep = usersCount == 0 ? 25 : 25 / usersCount;
        foreach (var user in usersForImport)
        {
            if (failedUsers.Contains(user))
            {
                ReportProgress(GetProgress() + progressStep, string.Format(MigrationResource.UserSkipped, user.DisplayName, i, usersCount));
                continue;
            }

            var smallStep = progressStep / 4;

            try
            {
                var currentUser = securityContext.CurrentAccount;
                await securityContext.AuthenticateMeAsync(user.Guid);
                user.MigratingFiles.Init(_path, user, Log, currentUser);
                user.MigratingFiles.SetUsersDict(usersForImport.Except(failedUsers));
                user.MigratingFiles.SetGroupsDict(groupsForImport);
                await user.MigratingFiles.MigrateAsync();
                await securityContext.AuthenticateMeAsync(currentUser.ID);
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

        foreach (var item in _takeouts)
        {
            File.Delete(item);
        }

        _migrationInfo.FailedUsers = failedUsers.Count;
        _migrationInfo.SuccessedUsers = usersForImport.Count - _migrationInfo.FailedUsers;
        ReportProgress(100, MigrationResource.MigrationCompleted);
    }
}
