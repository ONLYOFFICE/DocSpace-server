﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Migration.Core.Migrators;

public abstract class Migrator : IAsyncDisposable
{
    protected SecurityContext SecurityContext { get; }
    protected UserManager UserManager { get; }
    protected TenantQuotaFeatureStatHelper TenantQuotaFeatureStatHelper { get; }
    protected QuotaSocketManager QuotaSocketManager { get; }
    protected FileStorageService FileStorageService { get; }
    protected GlobalFolderHelper GlobalFolderHelper { get; }
    protected IServiceProvider ServiceProvider { get; }
    protected IDaoFactory DaoFactory { get; }
    protected EntryManager EntryManager { get; }
    protected MigrationLogger MigrationLogger { get; }
    protected AuthContext AuthContext { get; }
    protected DisplayUserSettingsHelper DisplayUserSettingsHelper { get; }
    protected UserManagerWrapper UserManagerWrapper { get; }

    public MigrationInfo MigrationInfo { get; set; }
    protected IAccount _currentUser;
    private Dictionary<string, MigrationUser> _usersForImport;
    protected List<string> _importedUsers;
    protected List<string> _failedUsers;
    protected readonly string _folderKey = "folder";
    protected readonly string _fileKey = "file";

    protected double _lastProgressUpdate;
    protected string _lastStatusUpdate;

    protected string TmpFolder { get; set; }

    public Func<double, string, Task> OnProgressUpdateAsync { get; set; }

    public Migrator(SecurityContext securityContext,
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
        UserManagerWrapper userManagerWrapper)
    {
        SecurityContext = securityContext;
        UserManager = userManager;
        TenantQuotaFeatureStatHelper = tenantQuotaFeatureStatHelper;
        QuotaSocketManager = quotaSocketManager;
        FileStorageService = fileStorageService;
        GlobalFolderHelper = globalFolderHelper;
        ServiceProvider = serviceProvider;
        DaoFactory = daoFactory;
        EntryManager = entryManager;
        MigrationLogger = migrationLogger;
        AuthContext = authContext;
        DisplayUserSettingsHelper = displayUserSettingsHelper;
        UserManagerWrapper = userManagerWrapper;
    }
    
    public abstract Task InitAsync(string path, CancellationToken cancellationToken, OperationType operation);
    public abstract Task<MigrationApiInfo> ParseAsync(bool reportProgress = true);

    protected async Task ReportProgressAsync(double value, string status)
    {
        _lastProgressUpdate = value;
        _lastStatusUpdate = status;
        if (OnProgressUpdateAsync != null)
        {
            await OnProgressUpdateAsync(value, status);
        }
        MigrationLogger.Log($"{value:0.00} progress: {status}");
    }

    public void Log(string msg, Exception exception = null)
    {
        MigrationLogger.Log(msg, exception);
    }

    public string GetLogName()
    {
        return MigrationLogger.GetLogName();
    }

    public List<string> GetGuidImportedUsers()
    {
        return _importedUsers;
    }

    public async Task MigrateAsync(MigrationApiInfo migrationInfo)
    {
        await ReportProgressAsync(0, MigrationResource.PreparingForMigration);
        _currentUser = AuthContext.CurrentAccount;
        _importedUsers = new();
        _failedUsers = new();

        var currentUser = SecurityContext.CurrentAccount;
        MigrationInfo.Merge(migrationInfo);

        _usersForImport = MigrationInfo.Users.Where(u => u.Value.ShouldImport).ToDictionary();

        await MigrateUsersAsync();

        await MigrateGroupAsync();

        var progressStep = _usersForImport.Count == 0 ? 30 : 30 / _usersForImport.Count;
        var i = 1;
        foreach (var kv in _usersForImport.Where(u=> !_failedUsers.Contains(u.Value.Info.Email)))
        {
            try
            {
                await ReportProgressAsync(_lastProgressUpdate + progressStep, string.Format(MigrationResource.MigratingUserFiles, kv.Value.Info.DisplayUserName(DisplayUserSettingsHelper), i++, _usersForImport.Count));
                await MigrateStorageAsync(kv.Value.Storage, kv.Value);
            }
            catch(Exception e)
            {
                Log(MigrationResource.CanNotImportUserFiles, e);
                MigrationInfo.Errors.Add($"{kv.Key} - {MigrationResource.CanNotImportUserFiles}"); 
            }
        }

        if(MigrationInfo.CommonStorage != null)
        {
            try
            {
                await ReportProgressAsync(85, string.Format(MigrationResource.MigrationCommonFiles));
                await MigrateStorageAsync(MigrationInfo.CommonStorage);
            }
            catch(Exception e)
            {
                Log(MigrationResource.СanNotImportCommonFiles, e);
                MigrationInfo.Errors.Add(MigrationResource.СanNotImportCommonFiles);
            }
        }

        if (MigrationInfo.ProjectStorage != null)
        {
            try
            {
                await ReportProgressAsync(90, string.Format(MigrationResource.MigrationProjectFiles));
                await MigrateStorageAsync(MigrationInfo.ProjectStorage);
            }
            catch (Exception e)
            {
                Log(MigrationResource.СanNotImportProjectFiles, e);
                MigrationInfo.Errors.Add(MigrationResource.СanNotImportProjectFiles);
            }
        }

        if (Directory.Exists(TmpFolder))
        {
            Directory.Delete(TmpFolder, true);
        }

        MigrationInfo.FailedUsers = _failedUsers.Count;
        MigrationInfo.SuccessedUsers = _usersForImport.Count - MigrationInfo.FailedUsers;
        await ReportProgressAsync(100, MigrationResource.MigrationCompleted);
    }

    private async Task MigrateUsersAsync()
    {
        var i = 1;
        var progressStep = !_usersForImport.Any() ? 30 : 30 / _usersForImport.Count;
        foreach (var kv in MigrationInfo.Users)
        {
            var key = kv.Key;
            var user = kv.Value;
            try
            {
                if (user.ShouldImport)
                {
                    await ReportProgressAsync(_lastProgressUpdate + progressStep, string.Format(MigrationResource.UserMigration, user.Info.DisplayUserName(DisplayUserSettingsHelper), i++, MigrationInfo.Users.Count));
                }
                var saved = await UserManager.GetUserByEmailAsync(user.Info.Email);

                if (user.ShouldImport && (saved.Equals(Constants.LostUser) || saved.Removed))
                {
                    DataСhange(user);
                    user.Info.UserName = await UserManagerWrapper.MakeUniqueNameAsync(user.Info);
                    user.Info.ActivationStatus = EmployeeActivationStatus.Pending;
                    saved = await UserManager.SaveUserInfo(user.Info, user.UserType);
                    var groupId = user.UserType switch
                    {
                        EmployeeType.Collaborator => Constants.GroupCollaborator.ID,
                        EmployeeType.DocSpaceAdmin => Constants.GroupAdmin.ID,
                        EmployeeType.RoomAdmin => Constants.GroupManager.ID,
                        _ => Guid.Empty
                    };

                    if (groupId != Guid.Empty)
                    {
                        await UserManager.AddUserIntoGroupAsync(saved.Id, groupId, true);
                    }
                    else if (user.UserType == EmployeeType.RoomAdmin)
                    {
                        var (name, value) = await TenantQuotaFeatureStatHelper.GetStatAsync<CountPaidUserFeature, int>();
                        _ = QuotaSocketManager.ChangeQuotaUsedValueAsync(name, value);
                    }

                    if (user.HasPhoto)
                    {
                        using var ms = new MemoryStream();
                        await using (var fs = File.OpenRead(user.PathToPhoto))
                        {
                            await fs.CopyToAsync(ms);
                        }
                        await UserManager.SaveUserPhotoAsync(user.Info.Id, ms.ToArray());
                    }
                }
                if (saved.Equals(Constants.LostUser))
                {
                    MigrationInfo.Users.Remove(key);
                }
                else
                {
                    user.Info = saved;
                }

                if (user.ShouldImport)
                {
                    _importedUsers.Add(user.Info.Email);
                }
            }
            catch(Exception e)
            {
                Log(MigrationResource.CanNotImportUser, e);
                MigrationInfo.Errors.Add($"{key} - {MigrationResource.CanNotImportUser}");
                _failedUsers.Add(user.Info.Email);
                MigrationInfo.Users.Remove(key);
            }
        }
    }

    private void DataСhange(MigrationUser user)
    {
        user.Info.UserName ??= user.Info.Email.Split('@').First();
        user.Info.LastName ??= user.Info.FirstName;
    }

    private async Task MigrateGroupAsync()
    {
        var i = 1;
        var progressStep = MigrationInfo.Groups.Count == 0 ? 20 : 20 / MigrationInfo.Groups.Count;
        foreach (var kv in MigrationInfo.Groups)
        {
            var key = kv.Key;
            var group = kv.Value;

            await ReportProgressAsync(_lastProgressUpdate + progressStep, string.Format(MigrationResource.GroupMigration, group.Info.Name, i++, MigrationInfo.Groups.Count));
            
            if (!group.ShouldImport)
            {
                return;
            }
            var existingGroups = (await UserManager.GetGroupsAsync()).ToList();
            var oldGroup = existingGroups.Find(g => g.Name == group.Info.Name);
            if (oldGroup != null)
            {
                group.Info = oldGroup;
            }
            else
            {
                group.Info = await UserManager.SaveGroupInfoAsync(group.Info);
            }

            foreach (var userGuid in group.UserKeys)
            {
                try
                {
                    var user = _usersForImport.ContainsKey(userGuid) ? _usersForImport[userGuid].Info : Constants.LostUser;
                    if (user.Equals(Constants.LostUser))
                    {
                        continue;
                    }
                    if (!await UserManager.IsUserInGroupAsync(user.Id, group.Info.ID))
                    {
                        await UserManager.AddUserIntoGroupAsync(user.Id, group.Info.ID);
                        if (group.ManagerKey == userGuid)
                        {
                            await UserManager.SetDepartmentManagerAsync(group.Info.ID, user.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log(string.Format(MigrationResource.CanNotAddUserInGroup, userGuid, group.Info.Name), ex);
                    MigrationInfo.Errors.Add(string.Format(MigrationResource.CanNotAddUserInGroup, userGuid, group.Info.Name));
                }
            }
        }
    }

    private async Task MigrateStorageAsync(MigrationStorage storage, MigrationUser user = null)
    {
        if (!storage.ShouldImport || storage.Files.Count == 0)
        {
            return;
        }

        if (user != null)
        {
            await SecurityContext.AuthenticateMeAsync(user.Info.Id);
        }
        else
        {
            await SecurityContext.AuthenticateMeAsync(_currentUser);
        }

        var matchingFilesIds = new Dictionary<string, FileEntry<int>>();
        Folder<int> newFolder = null;
        if (storage.Type != FolderType.BUNCH) 
        {
            newFolder = storage.Type == FolderType.USER
                ? await FileStorageService.CreateFolderAsync(await GlobalFolderHelper.FolderMyAsync, $"ASC migration files {DateTime.Now:dd.MM.yyyy}")
                    : await FileStorageService.CreateRoomAsync($"ASC migration common files {DateTime.Now:dd.MM.yyyy}", RoomType.PublicRoom, false, false, new List<FileShareParams>(), 0);
            Log(MigrationResource.СreateRootFolder);

            matchingFilesIds.Add($"{_folderKey}-{storage.RootKey}", newFolder);
        }
        else
        {
            newFolder = ServiceProvider.GetService<Folder<int>>();
            newFolder.Id = -1;
            matchingFilesIds.Add($"{_folderKey}-{storage.RootKey}", newFolder);
        }
        var orderedFolders = storage.Folders.OrderBy(f => f.Level);
        foreach (var folder in orderedFolders)
        {
            if (!storage.ShouldImportSharedFolders ||
                !storage.Securities.Any(s => s.EntryId == folder.Id && s.EntryType == 1) && matchingFilesIds[$"{_folderKey}-{folder.ParentId}"].Id != 0)
            {
                if (storage.Type == FolderType.BUNCH && !folder.Private)
                {
                    newFolder = await FileStorageService.CreateRoomAsync(folder.Title, RoomType.PublicRoom, false, false, new List<FileShareParams>(), 0);
                    Log(string.Format(MigrationResource.CreateFolder, newFolder.Title));
                }
                else
                {
                    newFolder = await FileStorageService.CreateFolderAsync(matchingFilesIds[$"{_folderKey}-{folder.ParentId}"].Id, folder.Title);
                    Log(string.Format(MigrationResource.CreateFolder, newFolder.Title));
                }
            }
            else
            {
                newFolder = ServiceProvider.GetService<Folder<int>>();
                newFolder.Title = folder.Title;
            }
            matchingFilesIds.Add($"{_folderKey}-{folder.Id}", newFolder);
        }

        var fileDao = DaoFactory.GetFileDao<int>();

        foreach (var file in storage.Files)
        {
            try
            {
                await using var fs = new FileStream(file.Path, FileMode.Open);

                var newFile = ServiceProvider.GetService<File<int>>();
                newFile.ParentId = matchingFilesIds[$"{_folderKey}-{file.Folder}"].Id;
                newFile.Comment = FilesCommonResource.CommentCreate;
                newFile.Title = Path.GetFileName(file.Title);
                newFile.ContentLength = fs.Length;
                newFile.Version = file.Version;
                newFile.VersionGroup = file.VersionGroup;
                newFile.Comment = file.Comment;
                newFile.CreateOn = file.Created;
                newFile.ModifiedOn = file.Modified;
                if (matchingFilesIds.ContainsKey($"{_fileKey}-{file.Id}"))
                {
                    newFile.Id = matchingFilesIds[$"{_fileKey}-{file.Id}"].Id;
                }
                if (!storage.ShouldImportSharedFolders || !storage.Securities.Any(s => s.EntryId == file.Folder && s.EntryType == 1) && newFile.ParentId != 0)
                {
                    newFile = await fileDao.SaveFileAsync(newFile, fs);
                    Log(string.Format(MigrationResource.CreateFile, file.Title));
                }
                if (!matchingFilesIds.ContainsKey($"{_fileKey}-{file.Id}") && newFile.Id != 0)
                {
                    matchingFilesIds.Add($"{_fileKey}-{file.Id}", newFile);
                }
            }
            catch (Exception ex)
            {
                Log(string.Format(MigrationResource.CanNotCreateFile, file.Title), ex);
                MigrationInfo.Errors.Add(string.Format(MigrationResource.CanNotCreateFile, file.Title));
            }
        }

        if (storage.Type == FolderType.COMMON || !storage.ShouldImportSharedFiles && !storage.ShouldImportSharedFolders)
        {
            return;
        }

        var aces = new Dictionary<string, AceWrapper>();
        var matchingRoomIds = new Dictionary<int, FileEntry<int>>();
        var innerFolders = new List<int>();
        var orderedSecurity = storage.Securities.OrderBy(s => OrderSecurity(storage,s));
        foreach (var security in orderedSecurity)
        {
            try
            {
                if (!MigrationInfo.Users.ContainsKey(security.Subject) && !MigrationInfo.Groups.ContainsKey(security.Subject))
                {
                    continue;
                }
                var access = (Files.Core.Security.FileShare)security.Security;

                var entryIsFile = security.EntryType == 2;
                if (entryIsFile && storage.ShouldImportSharedFiles)
                {
                    var key = $"{_fileKey}-{security.EntryId}";
                    if(!matchingFilesIds.ContainsKey(key))
                    {
                        continue;
                    }
                    await SecurityContext.AuthenticateMeAsync(user.Info.Id);
                    AceWrapper ace = null;
                    if (!aces.ContainsKey($"{security.Security}{matchingFilesIds[key].Id}"))
                    {
                        try
                        {
                            ace = await FileStorageService.SetExternalLinkAsync(matchingFilesIds[key].Id, FileEntryType.File, Guid.Empty, null, access, requiredAuth: true,
                                primary: false);
                            aces.Add($"{security.Security}{matchingFilesIds[key].Id}", ace);
                        }
                        catch
                        {
                            ace = null;
                            aces.Add($"{security.Security}{matchingFilesIds[key].Id}", null);
                        }
                    }
                    else
                    {
                        ace = aces[$"{security.Security}{matchingFilesIds[key].Id}"];
                    }
                    if (ace != null)
                    {
                        if (MigrationInfo.Users.ContainsKey(security.Subject))
                        {
                            var userForShare = await UserManager.GetUsersAsync(MigrationInfo.Users[security.Subject].Info.Id);
                            await SecurityContext.AuthenticateMeAsync(userForShare.Id);
                            await EntryManager.MarkAsRecentByLink(matchingFilesIds[key] as File<int>, ace.Id);
                        }
                        else
                        {
                            var users = UserManager.GetUsers(false, EmployeeStatus.Active,
                                [[MigrationInfo.Groups[security.Subject].Info.ID]],
                                [], [], null, null, null, "", false, "firstname",
                                true, 100000, 0).Where(u => u.Id != user.Info.Id);
                            await foreach (var u in users)
                            {
                                await SecurityContext.AuthenticateMeAsync(u.Id);
                                await EntryManager.MarkAsRecentByLink(matchingFilesIds[key] as File<int>, ace.Id);
                            }
                        }
                    }
                }
                else if (storage.ShouldImportSharedFolders)
                {
                    var localMatchingRoomIds = new Dictionary<int, FileEntry<int>>();
                    var key = $"{_folderKey}-{security.EntryId}";

                    if (innerFolders.Contains(security.EntryId))
                    {
                        continue;
                    }
                    if (!matchingRoomIds.ContainsKey(security.EntryId))
                    {
                        if (storage.Type == FolderType.BUNCH)
                        {
                            var owner = storage.Folders.FirstOrDefault(f => f.Id == security.EntryId).Owner;
                            user = MigrationInfo.Users[owner];
                        }

                        if (user.UserType == EmployeeType.Collaborator)
                        {
                            await SecurityContext.AuthenticateMeAsync(_currentUser);
                        }
                        else
                        {
                            await SecurityContext.AuthenticateMeAsync(user.Info.Id);
                        }
                        var room = await FileStorageService.CreateRoomAsync($"{matchingFilesIds[key].Title}",
                            RoomType.EditingRoom, false, false, new List<FileShareParams>(), 0);

                        orderedFolders = storage.Folders.Where(f => f.ParentId == security.EntryId).OrderBy(f => f.Level);
                        matchingRoomIds.Add(security.EntryId, room);
                        localMatchingRoomIds.Add(security.EntryId, room);
                        Log(string.Format(MigrationResource.CreateShareRoom, room.Title));

                        if (user.UserType == EmployeeType.Collaborator)
                        {
                            var aceList = new List<AceWrapper>
                            {
                                new AceWrapper
                                {
                                    Access = Files.Core.Security.FileShare.PowerUser,
                                    Id = user.Info.Id
                                }
                            };

                            var collection = new AceCollection<int>
                            {
                                Files = Array.Empty<int>(),
                                Folders = new List<int> { matchingRoomIds[security.EntryId].Id },
                                Aces = aceList,
                                Message = null
                            };

                            await FileStorageService.SetAceObjectAsync(collection, false);
                        }

                        foreach (var folder in orderedFolders)
                        {
                            newFolder = await FileStorageService.CreateFolderAsync(matchingRoomIds[folder.ParentId].Id, folder.Title);
                            matchingRoomIds.Add(folder.Id, newFolder);
                            innerFolders.Add(folder.Id);
                            Log(string.Format(MigrationResource.CreateFolder, newFolder.Title));
                        }
                        foreach (var file in storage.Files.Where(f => localMatchingRoomIds.ContainsKey(f.Folder)))
                        {
                            try
                            {
                                await using var fs = new FileStream(file.Path, FileMode.Open);

                                var newFile = ServiceProvider.GetService<File<int>>();
                                newFile.ParentId = localMatchingRoomIds[security.EntryId].Id;
                                newFile.Comment = FilesCommonResource.CommentCreate;
                                newFile.Title = Path.GetFileName(file.Title);
                                newFile.ContentLength = fs.Length;
                                newFile.Version = file.Version;
                                newFile.VersionGroup = file.VersionGroup;
                                newFile.CreateOn = file.Created;
                                newFile.ModifiedOn = file.Modified;
                                if (matchingFilesIds.ContainsKey($"{_fileKey}-{file.Id}"))
                                {
                                    newFile.Id = matchingFilesIds[$"{_fileKey}-{file.Id}"].Id;
                                }
                                newFile = await fileDao.SaveFileAsync(newFile, fs);
                                Log(string.Format(MigrationResource.CreateFile, file.Title));
                                if (!matchingFilesIds.ContainsKey($"{_fileKey}-{file.Id}"))
                                {
                                    matchingFilesIds.Add($"{_fileKey}-{file.Id}", newFile);
                                }
                            }
                            catch(Exception ex)
                            {
                                Log(string.Format(MigrationResource.CanNotCreateFile, file.Title), ex);
                                MigrationInfo.Errors.Add(string.Format(MigrationResource.CanNotCreateFile, file.Title));
                            }
                        }
                    }
                    if (_usersForImport.ContainsKey(security.Subject) && _currentUser.ID == _usersForImport[security.Subject].Info.Id)
                    {
                        continue;
                    }

                    var list = new List<AceWrapper>
                    {
                        new AceWrapper
                        {
                            Access = access,
                            Id = MigrationInfo.Users.ContainsKey(security.Subject) 
                                ? MigrationInfo.Users[security.Subject].Info.Id 
                                : MigrationInfo.Groups[security.Subject].Info.ID
                        }
                    };

                    var aceCollection = new AceCollection<int>
                    {
                        Files = Array.Empty<int>(),
                        Folders = new List<int> { matchingRoomIds[security.EntryId].Id },
                        Aces = list,
                        Message = null
                    };

                    await FileStorageService.SetAceObjectAsync(aceCollection, false);
                }
            }
            catch (Exception ex)
            {
                Log(string.Format(MigrationResource.CanNotShare, security.EntryId, security.Subject), ex);
                MigrationInfo.Errors.Add(string.Format(MigrationResource.CanNotShare, security.EntryId, security.Subject));
            }
        }
    }

    private int OrderSecurity(MigrationStorage storage, MigrationSecurity security)
    {
        if(security.EntryType != 1)
        {
            return 0;
        }
        var folder = storage.Folders.FirstOrDefault(f => f.Id == security.EntryId);
        return folder == null ? 0 : folder.Level;
    }

    public async ValueTask DisposeAsync()
    {
        if (MigrationLogger != null)
        {
            await MigrationLogger.DisposeAsync();
        }
    }
}
