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


using net.openstack.Providers.Rackspace.Objects.Databases;

using ASCShare = ASC.Files.Core.Security.FileShare;

namespace ASC.Migration.Core.Migrators.Provider.NC;

[Transient]
public class NextcloudWorkspaceMigrator : Migrator
{
    private CancellationToken _cancellationToken;
    private string _takeout;

    private readonly Regex _emailRegex = new Regex(@"(\S*@\S*\.\S*)");
    private readonly Regex _phoneRegex = new Regex(@"(\+?\d+)");

    public NextcloudWorkspaceMigrator(SecurityContext securityContext,
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
        MigrationInfo = new MigrationInfo() { Name = "Nextcloud" };
    }

    public override async Task InitAsync(string path, CancellationToken cancellationToken, OperationType operation)
    {
        await MigrationLogger.InitAsync();
        _cancellationToken = cancellationToken;

        MigrationInfo.Operation = operation;

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
        MigrationInfo.Files = new List<string> { Path.GetFileName(_takeout) };
        TmpFolder = path;
    }

    public override async Task<MigrationApiInfo> ParseAsync(bool reportProgress = true)
    {
        if (reportProgress)
        {
            await ReportProgressAsync(5, MigrationResource.Unzipping);
        }
        try
        {
            double progress = 5;
            try
            {
                using (var archive = ZipFile.OpenRead(_takeout))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (reportProgress)
                        {
                            progress += 45d / archive.Entries.Count;
                            await ReportProgressAsync(progress, MigrationResource.Unzipping);
                        }
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(Path.Combine(TmpFolder, entry.FullName));
                        }
                        else
                        {
                            var dir = Path.GetDirectoryName(Path.Combine(TmpFolder, entry.FullName));
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                            entry.ExtractToFile(Path.Combine(TmpFolder, entry.FullName));
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
                Log(string.Format(MigrationResource.CanNotToUnzip, _takeout), ex);
            }

            if (reportProgress)
            {
                await ReportProgressAsync(50, MigrationResource.UnzippingFinished);
            }

            var dbFile = Directory.GetFiles(Directory.GetDirectories(TmpFolder)[0], "*.bak")[0];
            if (dbFile == null)
            {
                throw new Exception();
            }
            if (reportProgress)
            {
                await ReportProgressAsync(60, MigrationResource.DumpParse);
            }
            var users = DbExtractUser(dbFile);
            progress = 60;
            foreach (var user in users)
            {
                if (_cancellationToken.IsCancellationRequested && reportProgress)
                {
                    return null;
                }
                if (reportProgress)
                {
                    await ReportProgressAsync(progress, MigrationResource.DataProcessing);
                    progress += 20 / users.Count;
                }
                if (!string.IsNullOrEmpty(user.Value.Info.FirstName))
                {
                    try
                    {
                        var rootFolder = Directory.GetDirectories(TmpFolder)[0];
                        var drivePath = Directory.Exists(Path.Combine(rootFolder, "data", user.Key, "cache")) ?
                                Path.Combine(rootFolder, "data", user.Key, "cache") : null;
                        if (drivePath == null)
                        {
                            user.Value.HasPhoto = false;
                        }
                        else
                        {
                            user.Value.PathToPhoto = File.Exists(Path.Combine(drivePath, "avatar_upload")) ? Directory.GetFiles(drivePath, "avatar_upload")[0] : null;
                            user.Value.HasPhoto = user.Value.PathToPhoto != null ? true : false;
                        }

                        if (!user.Value.HasPhoto)
                        {
                            var appdataDir = Directory.GetDirectories(Path.Combine(rootFolder, "data")).FirstOrDefault(dir => dir.Split(Path.DirectorySeparatorChar).Last().StartsWith("appdata_"));
                            if (appdataDir != null)
                            {
                                var pathToAvatarDir = Path.Combine(appdataDir, "avatar", user.Key);
                                user.Value.PathToPhoto = File.Exists(Path.Combine(pathToAvatarDir, "generated")) ? null : Path.Combine(pathToAvatarDir, "avatar.jpg");
                                user.Value.HasPhoto = user.Value.PathToPhoto != null ? true : false;
                            }
                        }
                        ParseStorage(user.Key, user.Value, dbFile);
                        if (string.IsNullOrEmpty(user.Value.Info.Email))
                        {
                            MigrationInfo.WithoutEmailUsers.Add(user.Key, user.Value);
                        }
                        else if (!(await UserManager.GetUserByEmailAsync(user.Value.Info.Email)).Equals(ASC.Core.Users.Constants.LostUser))
                        {
                            MigrationInfo.ExistUsers.Add(user.Key, user.Value);
                        }
                        else
                        {
                            MigrationInfo.Users.Add(user.Key, user.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(string.Format(MigrationResource.CanNotParseUser, user.Value.Info.DisplayUserName(DisplayUserSettingsHelper)), ex);
                    }
                }
            }
            if (reportProgress)
            {
                await ReportProgressAsync(90, MigrationResource.DataProcessing);
            }
            DbExtractGroup(dbFile);
        }
        catch
        {
            MigrationInfo.FailedArchives.Add(Path.GetFileName(_takeout));
            var error = string.Format(MigrationResource.CanNotParseArchive, Path.GetFileNameWithoutExtension(_takeout));
            await ReportProgressAsync(100, error);
            throw new Exception(error);
        }
        if (reportProgress)
        {
            await ReportProgressAsync(100, MigrationResource.DataProcessingCompleted);
        }
        return MigrationInfo.ToApiInfo();
    }

    private void DbExtractGroup(string dbFile)
    {
        var sqlFile = File.ReadAllText(dbFile);

        var groupList = GetDumpChunk("oc_groups", sqlFile);
        if (groupList == null)
        {
            return;
        }

        foreach (var g in groupList)
        {
            var group = new MigrationGroup() { Info = new(), UserKeys = new HashSet<string>() };
            group.Info.Name = g.Split(',').First().Trim('\'');
            MigrationInfo.Groups.Add(group.Info.Name, group);
        }

        var usersInGroups = GetDumpChunk("oc_group_user", sqlFile);
        foreach (var user in usersInGroups)
        {
            var userGroupGid = user.Split(',').First().Trim('\'');
            var userUid = user.Split(',').Last().Trim('\'');
            MigrationInfo.Groups[userGroupGid].UserKeys.Add(userUid);
        }
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

    private Dictionary<string, MigrationUser> DbExtractUser(string dbFile)
    {
        var usersData = new Dictionary<string, MigrationUser>();

        var sqlFile = File.ReadAllText(dbFile);

        var accounts = GetDumpChunk("oc_accounts", sqlFile);
        if (accounts == null)
        {
            return usersData;
        }

        foreach (var account in accounts)
        {
            var userId = account.Split(',').First().Trim('\'');

            usersData.Add(userId, new MigrationUser(DisplayUserSettingsHelper)
            {
                Info = new UserInfo(),
                Storage = new MigrationStorage()
            });
        }

        var accountsData = GetDumpChunk("oc_accounts_data", sqlFile);
        if (accountsData != null)
        {
            foreach (var accountData in accountsData)
            {
                var values = accountData.Split(',')
                    .Select(s => s.Trim('\'')).ToArray();
                usersData.TryGetValue(values[1], out var user);
                if (user == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(values[3]))
                {
                    switch (values[2])
                    {
                        case "displayname":
                            if (!string.IsNullOrEmpty(values[3]))
                            {
                                var userName = values[3].Split(' ');
                                values[3] = userName.Length > 1 ? $"{userName[0]} {userName[1]}".Trim() : userName[0].Trim();
                                userName = values[3].Split(' ');
                                user.Info.FirstName = userName[0];
                                if (userName.Length > 1)
                                {
                                    user.Info.LastName = userName[1];
                                }
                            }
                            break;
                        case "address":
                            user.Info.Location = values[3];
                            break;
                        case "email":
                            if (values[3] != "NULL")
                            {
                                user.Info.Email = values[3];
                            }
                            if (!string.IsNullOrEmpty(user.Info.Email))
                            {
                                var email = _emailRegex.Match(user.Info.Email);
                                if (email.Success)
                                {
                                    user.Info.Email = email.Groups[1].Value;
                                }
                            }
                            break;
                        case "phone":
                            var phone = _phoneRegex.Match(values[3]);
                            if (phone.Success)
                            {
                                user.Info.ContactsList.Add(phone.Groups[1].Value);
                            }
                            break;
                        case "twitter":
                            user.Info.ContactsList.Add(values[3]);
                            break;
                    }
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
                usersData.TryGetValue(uid, out var user);
                if (user == null)
                {
                    continue;
                }

                user.Storage.RootKey = values[0];
            }
        }
        return usersData;
    }

    private void ParseStorage(string key, MigrationUser user, string dbFile)
    {
        var rootFolder = Directory.GetDirectories(TmpFolder)[0];

        var drivePath = Directory.Exists(Path.Combine(rootFolder, "data", key, "files")) ?
            Path.Combine(rootFolder, "data", key, "files") : null;
        if (drivePath == null)
        {
            return;
        }

        var sqlFile = File.ReadAllText(dbFile);

        var filesAndFolders = new List<NCFileCache>();
        var fileCaches = GetDumpChunk("oc_filecache", sqlFile);
        if (fileCaches != null)
        {
            foreach (var cache in fileCaches)
            {
                var values = cache.Split(',')
                           .Select(s => s.Trim('\'')).ToArray();
                if (user.Storage.RootKey != values[1])
                {
                    continue;
                }

                filesAndFolders.Add(new NCFileCache()
                {
                    FileId = int.Parse(values[0]),
                    Path = values[2],
                    Share = new List<NCShare>()
                });
            }
        }

        var shares = GetDumpChunk("oc_share", sqlFile);
        if (shares != null)
        {
            foreach (var share in shares)
            {
                var values = share.Split(',')
                           .Select(s => s.Trim('\'')).ToArray();
                var fileId = int.Parse(values[10]);
                var file = filesAndFolders.FirstOrDefault(ff => ff.FileId == fileId);
                if (file == null)
                {
                    continue;
                }

                file.Share.Add(new NCShare()
                {
                    Id = int.Parse(values[0]),
                    ShareWith = values[2],
                    Premissions = int.Parse(values[12])
                });
            }
        }

        var j = 1;
        foreach (var entry in filesAndFolders)
        {
            var paths = entry.Path.Split('/');
            if (paths[0] != "files")
            {
                continue;
            }

            entry.Path = string.Join("/", paths);

            if (paths.Length >= 1)
            {
                var tmpPath = drivePath;
                for (var i = 1; i < paths.Length; i++)
                {
                    tmpPath = Path.Combine(tmpPath, paths[i]);
                }
                if (Directory.Exists(tmpPath) || File.Exists(tmpPath))
                {
                    var attr = File.GetAttributes(tmpPath);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        var split = entry.Path.Split('/');
                        var folder = new MigrationFolder()
                        {
                            Id = entry.FileId,
                            Level = j++,
                            ParentId = split.Length > 1 ? filesAndFolders.FirstOrDefault(ff => ff.Path == string.Join('/',split[0..(split.Length - 1)])).FileId : int.Parse(user.Storage.RootKey),
                            Title = split.Last()
                        };
                        user.Storage.Folders.Add(folder);
                        AddShare(user, entry, false);
                    }
                    else
                    {
                        var fi = new FileInfo(tmpPath);
                        user.Storage.BytesTotal += fi.Length;
                        var split = entry.Path.Split('/');
                        var file = new MigrationFile()
                        {
                            Id = entry.FileId,
                            Path = tmpPath,
                            Title = split.Last(),
                            Folder = split.Length > 1 ? filesAndFolders.FirstOrDefault(ff => ff.Path == string.Join('/', split[0..(split.Length - 1)])).FileId : int.Parse(user.Storage.RootKey)
                        };
                        user.Storage.Files.Add(file);
                        AddShare(user, entry, true);
                    }
                }
            }
        }
    }

    private void AddShare(MigrationUser user, NCFileCache entry, bool isFile)
    {
        if (entry.Share.Count == 0)
        {
            return;
        }

        foreach (var shareInfo in entry.Share)
        {
            if (shareInfo.ShareWith == null)
            {
                continue;
            }

            var shareType = GetPortalShare(shareInfo.Premissions, isFile);

            var security = new MigrationSecurity()
            {
                Subject = shareInfo.ShareWith,
                EntryId = entry.FileId,
                EntryType = isFile ? 2 : 1,
                Security = (int)shareType
            };

            user.Storage.Securities.Add(security);
        }
    }

    private ASCShare GetPortalShare(int role, bool entryType)
    {
        if (entryType)
        {
            if (role == 1 || role == 17)
            {
                return ASCShare.Read;
            }

            return ASCShare.ReadWrite;//permission = 19 => denySharing = true, permission = 3 => denySharing = false; ASCShare.ReadWrite
        }
        else
        {
            if (Array.Exists(new int[] { 1, 17, 9, 25, 5, 21, 13, 29, 3, 19, 11, 27 }, el => el == role))
            {
                return ASCShare.Read;
            }

            return ASCShare.ReadWrite;//permission = 19||23 => denySharing = true, permission = 7||15 => denySharing = false; ASCShare.ReadWrite
        }
    }
}
