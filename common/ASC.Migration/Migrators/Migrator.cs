// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

using ASC.Files.Core.Security;

namespace ASC.Migration.Core.Migrators;

public abstract class Migrator(
    SecurityContext securityContext,
    UserManager userManager,
    UserPhotoManager userPhotoManager,
    TenantQuotaFeatureStatHelper tenantQuotaFeatureStatHelper,
    QuotaSocketManager quotaSocketManager,
    FileStorageService fileStorageService,
    GlobalFolderHelper globalFolderHelper,
    IServiceProvider serviceProvider,
    IDaoFactory daoFactory,
    MigrationLogger migrationLogger,
    AuthContext authContext,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    UserManagerWrapper userManagerWrapper,
    UserSocketManager socketManager)
    : IAsyncDisposable
{
    protected SecurityContext SecurityContext { get; } = securityContext;
    protected UserSocketManager SocketManager { get; } = socketManager;
    protected UserManager UserManager { get; } = userManager;
    protected UserPhotoManager UserPhotoManager { get; } = userPhotoManager;
    private TenantQuotaFeatureStatHelper TenantQuotaFeatureStatHelper { get; } = tenantQuotaFeatureStatHelper;
    private QuotaSocketManager QuotaSocketManager { get; } = quotaSocketManager;
    private FileStorageService FileStorageService { get; } = fileStorageService;
    private GlobalFolderHelper GlobalFolderHelper { get; } = globalFolderHelper;
    private IServiceProvider ServiceProvider { get; } = serviceProvider;
    private IDaoFactory DaoFactory { get; } = daoFactory;
    protected MigrationLogger MigrationLogger { get; } = migrationLogger;
    private AuthContext AuthContext { get; } = authContext;
    protected DisplayUserSettingsHelper DisplayUserSettingsHelper { get; } = displayUserSettingsHelper;
    private UserManagerWrapper UserManagerWrapper { get; } = userManagerWrapper;

    public MigrationInfo MigrationInfo { get; protected init; }
    private IAccount _currentUser;
    private Dictionary<string, MigrationUser> _usersForImport;
    private List<string> _importedUsers;
    private List<string> _failedUsers;
    private const string FolderKey = "folder";
    private const string FileKey = "file";

    protected double _lastProgressUpdate;
    protected string _lastStatusUpdate;

    protected string TmpFolder { get; set; }

    public Func<double, string, Task> OnProgressUpdateAsync { get; set; }

    public abstract Task InitAsync(string path, OperationType operation, CancellationToken cancellationToken);
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

    /// <summary>
    /// Reports unpacking once per whole percent rather than per entry: every report publishes the operation state
    /// and writes a line to the migration log, and a backup may hold hundreds of thousands of files.
    /// </summary>
    protected async Task ReportUnzippingAsync(double progress, string status = null)
    {
        if (Math.Floor(progress) > Math.Floor(_lastProgressUpdate))
        {
            await ReportProgressAsync(progress, status ?? MigrationResource.Unzipping);
        }
    }

    public void Log(string msg, Exception exception = null)
    {
        MigrationLogger.Log(msg, exception);
    }

    protected static string GetSafeExtractPath(string root, string entryName)
    {
        if (!TryGetSafeExtractPath(root, entryName, out var fullPath))
        {
            throw new InvalidDataException($"Archive entry is outside the extraction directory: {entryName}");
        }

        return fullPath;
    }

    protected static bool TryGetSafeExtractPath(string root, string entryName, out string fullPath)
    {
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        fullPath = Path.GetFullPath(Path.Combine(normalizedRoot, entryName));

        return fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || fullPath.Equals(normalizedRoot, StringComparison.Ordinal);
    }

    /// <summary>
    /// The most bytes the uploaded archives may unpack to, set by the operation from the tenant quota and the
    /// <c>migration:maxExtractedSize</c> ceiling. The quota check before the parse sees only the compressed upload.
    /// </summary>
    public long MaxExtractedSize { get; set; } = long.MaxValue;

    /// <summary>
    /// The most entries the uploaded archives may hold, set by the operation from <c>migration:maxExtractedEntries</c>.
    /// The byte limit does not see directories, empty files or the entry headers, which compress to a few bytes
    /// each, so an archive of millions of empty entries would pass it and exhaust inodes, CPU and memory.
    /// </summary>
    public long MaxExtractedEntries { get; set; } = long.MaxValue;

    /// <summary>
    /// The most pixels a profile photo from a backup may have: 4096 x 4096, far beyond what a profile photo needs and
    /// still well within what decoding it costs. A larger one is left out of the import with a line in the log.
    /// </summary>
    private const ulong MaxPhotoPixels = 4096UL * 4096;

    private long _extractedSize;
    private long _extractedEntries;

    private void CountExtractedSize(long length)
    {
        _extractedSize += length;
        if (_extractedSize > MaxExtractedSize)
        {
            throw new LargeBackupException(MigrationResource.LargeBackup);
        }
    }

    private void CountExtractedEntries(long count)
    {
        _extractedEntries += count;
        if (_extractedEntries > MaxExtractedEntries)
        {
            throw new LargeBackupException(MigrationResource.LargeBackup);
        }
    }

    /// <summary>
    /// Unpacks a zip archive into <paramref name="root"/>, counting its entries and the bytes written against the limits.
    /// A path met twice in the archive keeps the later entry, as unzip does; a file that was there before the extraction
    /// is never replaced. <paramref name="onEntry"/> is called before each entry with its 1-based number and the entry count.
    /// </summary>
    /// <returns><see langword="false"/> when <paramref name="cancellationToken"/> stopped the extraction.</returns>
    protected async Task<bool> ExtractZipAsync(ZipArchive archive, string root, Func<int, int, Task> onEntry, CancellationToken cancellationToken)
    {
        // the central directory lists every entry up front, so an archive of too many is refused before anything is written
        var count = archive.Entries.Count;
        CountExtractedEntries(count);

        var written = new HashSet<string>();
        var number = 0;
        foreach (var entry in archive.Entries)
        {
            await onEntry(++number, count);

            var destPath = GetSafeExtractPath(root, entry.FullName);
            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                await using var entryStream = await entry.OpenAsync(cancellationToken);
                await ExtractEntryAsync(entryStream, destPath, !written.Add(destPath), cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Unpacks a tar stream into <paramref name="root"/>, counting its entries and the bytes written against the limits.
    /// A path met twice in the archive keeps the later entry, as tar does. A hard link to a file unpacked earlier is
    /// unpacked as a copy of it; symbolic links, other hard links, sparse files and devices are skipped with a line in
    /// the log, as their target may lie outside the extraction folder. With <paramref name="replaceExisting"/> a file that was there before
    /// the extraction is replaced rather than refused. <paramref name="onEntry"/> is called before each entry.
    /// A truncated tar fails with <see cref="EndOfStreamException"/>: GZipStream does not notice a cut-off stream, but
    /// TarReader requires the end-of-archive block, so even a cut on an entry boundary is reported.
    /// </summary>
    /// <returns><see langword="false"/> when <paramref name="cancellationToken"/> stopped the extraction.</returns>
    protected async Task<bool> ExtractTarAsync(Stream tarStream, string root, bool replaceExisting, Func<Task> onEntry, CancellationToken cancellationToken)
    {
        await using var reader = new TarReader(tarStream, leaveOpen: true);
        var written = new HashSet<string>();
        while (await reader.GetNextEntryAsync(cancellationToken: cancellationToken) is { } entry)
        {
            // a tar has no entry count up front, so the entries are counted as they come
            CountExtractedEntries(1);
            await onEntry();

            switch (entry.EntryType)
            {
                case TarEntryType.Directory:
                    Directory.CreateDirectory(GetSafeExtractPath(root, entry.Name));
                    break;
                case TarEntryType.RegularFile or TarEntryType.V7RegularFile or TarEntryType.ContiguousFile:
                    {
                        var destPath = GetSafeExtractPath(root, entry.Name);
                        Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                        var repeated = !written.Add(destPath);
                        await ExtractEntryAsync(entry.DataStream ?? Stream.Null, destPath, replaceExisting || repeated, cancellationToken);
                        break;
                    }
                // tar stores a file with several names once, and every other name as a hard link to the first one: the
                // link is unpacked as a copy of that file, but only of one this extraction wrote, so it never reaches
                // outside the folder or a file that was there before
                case TarEntryType.HardLink
                    when TryGetSafeExtractPath(root, entry.LinkName, out var targetPath) && written.Contains(targetPath):
                    {
                        var destPath = GetSafeExtractPath(root, entry.Name);
                        if (destPath == targetPath)
                        {
                            break;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                        var repeated = !written.Add(destPath);
                        await using var target = File.OpenRead(targetPath);
                        await ExtractEntryAsync(target, destPath, replaceExisting || repeated, cancellationToken);
                        break;
                    }
                // the archive's own metadata, not a file
                case TarEntryType.GlobalExtendedAttributes:
                    break;
                // symbolic links (their target may lie outside the folder), other hard links, sparse files, devices:
                // logged, so that a file missing from the import can be traced
                default:
                    Log(string.Format(MigrationResource.ArchiveEntrySkipped, entry.Name, entry.EntryType));
                    break;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Writes an archive entry to a new file, counting the bytes actually written rather than the size the archive declares.
    /// With <paramref name="overwrite"/> the file may already exist and is replaced; its bytes are counted again.
    /// </summary>
    private async Task ExtractEntryAsync(Stream source, string destPath, bool overwrite, CancellationToken cancellationToken)
    {
        await using var target = new FileStream(destPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);
        var buffer = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            CountExtractedSize(read);
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
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
        _importedUsers = [];
        _failedUsers = [];

        MigrationInfo.Merge(migrationInfo);

        _usersForImport = MigrationInfo.Users.Where(u => u.Value.ShouldImport).ToDictionary();

        await MigrateUsersAsync();

        await MigrateGroupAsync();

        var progressStep = _usersForImport.Count == 0 ? 30 : 30 / _usersForImport.Count;
        var i = 1;
        foreach (var kv in _usersForImport.Where(u => !_failedUsers.Contains(u.Value.Info.Email)))
        {
            try
            {
                await ReportProgressAsync(_lastProgressUpdate + progressStep, string.Format(MigrationResource.MigratingUserFiles, kv.Value.Info.DisplayUserName(DisplayUserSettingsHelper), i++, _usersForImport.Count));
                await MigrateStorageAsync(kv.Value.Storage, kv.Value);
            }
            catch (Exception e)
            {
                Log(MigrationResource.CanNotImportUserFiles, e);
                MigrationInfo.Errors.Add($"{kv.Key} - {MigrationResource.CanNotImportUserFiles}");
            }
        }

        if (MigrationInfo.CommonStorage != null)
        {
            try
            {
                await ReportProgressAsync(85, string.Format(MigrationResource.MigrationCommonFiles));
                await MigrateStorageAsync(MigrationInfo.CommonStorage);
            }
            catch (Exception e)
            {
                Log(MigrationResource.CanNotImportCommonFiles, e);
                MigrationInfo.Errors.Add(MigrationResource.CanNotImportCommonFiles);
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
                Log(MigrationResource.CanNotImportProjectFiles, e);
                MigrationInfo.Errors.Add(MigrationResource.CanNotImportProjectFiles);
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
        var progressStep = _usersForImport.Count == 0 ? 30 : 30 / _usersForImport.Count;
        foreach (var (key, user) in MigrationInfo.Users)
        {
            try
            {
                if (user.ShouldImport)
                {
                    await ReportProgressAsync(_lastProgressUpdate + progressStep, string.Format(MigrationResource.UserMigration, user.Info.DisplayUserName(DisplayUserSettingsHelper), i++, MigrationInfo.Users.Count));
                }
                var saved = await UserManager.GetUserByEmailAsync(user.Info.Email);

                if (user.ShouldImport && (saved.Equals(Constants.LostUser) || saved.Removed))
                {
                    DataChange(user);
                    user.Info.UserName = await UserManagerWrapper.MakeUniqueNameAsync(user.Info);
                    user.Info.ActivationStatus = EmployeeActivationStatus.Pending;
                    saved = await UserManager.SaveUserInfo(user.Info, user.UserType);
                    await SocketManager.AddUserAsync(saved);
                    var groupId = user.UserType switch
                    {
                        EmployeeType.User => Constants.GroupUser.ID,
                        EmployeeType.DocSpaceAdmin => Constants.GroupAdmin.ID,
                        EmployeeType.RoomAdmin => Constants.GroupRoomAdmin.ID,
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
                        // every migrator builds this path from the backup's contents, so it is checked once more where it is opened
                        var photoPath = GetSafeExtractPath(TmpFolder, user.PathToPhoto);

                        // the photo manager decodes the whole image, and a photo of a few kilobytes can declare gigapixels:
                        // its size is read from the header first, which decodes no pixels
                        var photoInfo = new MagickImageInfo(photoPath);
                        if ((ulong)photoInfo.Width * photoInfo.Height > MaxPhotoPixels)
                        {
                            Log(string.Format(MigrationResource.PhotoTooLarge, user.Info.DisplayUserName(DisplayUserSettingsHelper), photoInfo.Width, photoInfo.Height));
                        }
                        else
                        {
                            await UserPhotoManager.SaveOrUpdatePhoto(user.Info.Id, await File.ReadAllBytesAsync(photoPath));
                        }
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
            catch (Exception e)
            {
                Log(MigrationResource.CanNotImportUser, e);
                MigrationInfo.Errors.Add($"{key} - {MigrationResource.CanNotImportUser}");
                _failedUsers.Add(user.Info.Email);
                MigrationInfo.Users.Remove(key);
            }
        }
    }

    private static void DataChange(MigrationUser user)
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
                    var user = _usersForImport.TryGetValue(userGuid, out var value) ? value.Info : Constants.LostUser;
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
        if (!storage.ShouldImport || storage.Files.Count == 0 && storage.Folders.Count == 0)
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
        Folder<int> newFolder;
        if (storage.Type != FolderType.BUNCH)
        {
            newFolder = storage.Type == FolderType.USER
            ? await FileStorageService.CreateFolderAsync(await GlobalFolderHelper.FolderMyAsync, $"ASC migration files {DateTime.Now:dd.MM.yyyy}")
                    : await FileStorageService.CreateRoomAsync($"ASC migration common files {DateTime.Now:dd.MM.yyyy}", RoomType.PublicRoom, false, false, 0, null, false, null, null, null, null, null);
            Log(MigrationResource.CreateRootFolder);
        }
        else
        {
            newFolder = ServiceProvider.GetService<Folder<int>>();
            newFolder.Id = -1;
        }

        matchingFilesIds.Add($"{FolderKey}-{storage.RootKey}", newFolder);
        var orderedFolders = storage.Folders.OrderBy(f => f.Level);
        foreach (var folder in orderedFolders)
        {
            var parentEntry = matchingFilesIds[$"{FolderKey}-{folder.ParentId}"];
            if (parentEntry.Id != 0)
            {
                if (storage.Type == FolderType.BUNCH && parentEntry.Id == -1)
                {
                    newFolder = await FileStorageService.CreateRoomAsync(folder.Title, folder.Private ? RoomType.EditingRoom : RoomType.CustomRoom, false, false, 0, null, false, null, null, null, null, null);

                    if (MigrationInfo.Users.TryGetValue(folder.Owner, out var owner))
                    {
                        if (owner.UserType is EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin && newFolder.CreateBy != owner.Info.Id)
                        {
                            await FileStorageService.ChangeOwnerAsync([newFolder.Id], [], owner.Info.Id).ToListAsync();
                        }
                    }

                    Log(string.Format(MigrationResource.CreateShareRoom, newFolder.Title));
                }
                else
                {
                    newFolder = await FileStorageService.CreateFolderAsync(parentEntry.Id, folder.Title);

                    Log(string.Format(MigrationResource.CreateFolder, newFolder.Title));
                }
            }
            else
            {
                newFolder = ServiceProvider.GetService<Folder<int>>();
                newFolder.Title = folder.Title;
            }
            matchingFilesIds.Add($"{FolderKey}-{folder.Id}", newFolder);
        }

        var fileDao = DaoFactory.GetFileDao<int>();

        foreach (var file in storage.Files)
        {
            try
            {
                // every migrator builds this path from the backup's contents, so it is checked once more where it is opened
                await using var fs = new FileStream(GetSafeExtractPath(TmpFolder, file.Path), FileMode.Open, FileAccess.Read);

                var newFile = ServiceProvider.GetService<File<int>>();
                newFile.ParentId = matchingFilesIds[$"{FolderKey}-{file.Folder}"].Id;
                newFile.Comment = FilesCommonResource.CommentCreate;
                newFile.Title = Path.GetFileName(file.Title);
                newFile.ContentLength = fs.Length;
                newFile.Version = file.Version;
                newFile.VersionGroup = file.VersionGroup;
                newFile.CreateOn = file.Created;
                newFile.ModifiedOn = file.Modified;
                if (matchingFilesIds.ContainsKey($"{FileKey}-{file.Id}"))
                {
                    newFile.Id = matchingFilesIds[$"{FileKey}-{file.Id}"].Id;
                }
                if (newFile.ParentId != 0)
                {
                    newFile = await fileDao.SaveFileAsync(newFile, fs);
                    Log(string.Format(MigrationResource.CreateFile, file.Title));
                }
                if (!matchingFilesIds.ContainsKey($"{FileKey}-{file.Id}") && newFile.Id != 0)
                {
                    matchingFilesIds.Add($"{FileKey}-{file.Id}", newFile);
                }
            }
            catch (Exception ex)
            {
                Log(string.Format(MigrationResource.CanNotCreateFile, Path.GetFileName(file.Title)), ex);
                MigrationInfo.Errors.Add(string.Format(MigrationResource.CanNotCreateFile, Path.GetFileName(file.Title)));
            }
        }

        if (storage.Type == FolderType.COMMON || (!storage.ShouldImportSharedFiles && !storage.ShouldImportSharedFolders))
        {
            return;
        }

        var fileSecurity = ServiceProvider.GetService<FileSecurity>();
        var orderedSecurity = storage.Securities.OrderBy(s => OrderSecurity(storage, s));
        foreach (var security in orderedSecurity)
        {
            var key = string.Empty;
            FileEntry<int> entry = null;

            try
            {
                var entryIsFile = security.EntryType == 2;

                if (entryIsFile && !storage.ShouldImportSharedFiles)
                {
                    continue;
                }

                if (!entryIsFile && !storage.ShouldImportSharedFolders)
                {
                    continue;
                }

                key = $"{(entryIsFile ? FileKey : FolderKey)}-{security.EntryId}";

                if (!matchingFilesIds.TryGetValue(key, out entry) || entry.Id == 0)
                {
                    continue;
                }

                MigrationUser migrationUser = null;
                MigrationGroup migrationGroup = null;

                if (!MigrationInfo.Users.TryGetValue(security.Subject, out migrationUser) &&
                    !MigrationInfo.Groups.TryGetValue(security.Subject, out migrationGroup))
                {
                    if (security.Subject.Equals(Constants.GroupEveryone.ID.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        migrationGroup = new MigrationGroup
                        {
                            Info = new GroupInfo
                            {
                                ID = Constants.GroupEveryone.ID,
                                Name = Constants.GroupEveryone.Name
                            }
                        };
                    }
                    else if (security.Subject.Equals(Constants.GroupAdmin.ID.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        migrationGroup = new MigrationGroup
                        {
                            Info = new GroupInfo
                            {
                                ID = Constants.GroupAdmin.ID,
                                Name = Constants.GroupAdmin.Name
                            }
                        };
                    }
                    else
                    {
                        continue;
                    }
                }

                if (migrationUser != null && entry.CreateBy == migrationUser.Info.Id)
                {
                    continue;
                }

                var ace = new AceWrapper
                {
                    Access = (Files.Core.Security.FileShare)security.Security,
                    Id = migrationUser != null ? migrationUser.Info.Id : migrationGroup.Info.ID
                };

                var aceCollection = new AceCollection<int>
                {
                    Files = entryIsFile ? [entry.Id] : [],
                    Folders = entryIsFile ? [] : [entry.Id],
                    Aces = [ace],
                    Message = null
                };

                try
                {
                    await FileStorageService.SetAceObjectAsync(aceCollection, false);
                }
                catch (InvalidOperationException)
                {
                    if (ace.Access is Files.Core.Security.FileShare.None or Files.Core.Security.FileShare.Restrict)
                    {
                        throw;
                    }

                    var accesses = entryIsFile
                        ? await fileSecurity.GetAccesses((File<int>)entry)
                        : await fileSecurity.GetAccesses((Folder<int>)entry);

                    var type = migrationUser != null ? SubjectType.User : SubjectType.Group;

                    if (accesses.TryGetValue(type, out var availableShareRights) &&
                        !availableShareRights.Contains(ace.Access))
                    {
                        ace.Access = availableShareRights
                            .LastOrDefault(s => s is not (Files.Core.Security.FileShare.None or Files.Core.Security.FileShare.Restrict));

                        await FileStorageService.SetAceObjectAsync(aceCollection, false);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(string.Format(MigrationResource.CanNotShare, key, security.Subject), ex);
                MigrationInfo.Errors.Add(string.Format(MigrationResource.CanNotShare, $"{entry?.Title} ({key})", security.Subject));
            }
        }
    }

    private static int OrderSecurity(MigrationStorage storage, MigrationSecurity security)
    {
        if (security.EntryType != 1)
        {
            return 0;
        }
        var folder = storage.Folders.FirstOrDefault(f => f.Id == security.EntryId);
        return folder?.Level ?? 0;
    }

    public async Task SaveLogAsync()
    {
        if (MigrationLogger != null)
        {
            await MigrationLogger.SaveLogAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (MigrationLogger != null)
        {
            await MigrationLogger.DisposeAsync();
        }
    }
}
