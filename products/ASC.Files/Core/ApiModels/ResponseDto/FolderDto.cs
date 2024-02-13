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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// </summary>
public class FolderDto<T> : FileEntryDto<T>
{
    /// <summary>Parent folder ID</summary>
    /// <type>System.Int32, System</type>
    public T ParentId { get; set; }

    /// <summary>Number of files</summary>
    /// <type>System.Int32, System</type>
    public int FilesCount { get; set; }

    /// <summary>Number of folders</summary>
    /// <type>System.Int32, System</type>
    public int FoldersCount { get; set; }

    /// <summary>Specifies if a folder is shareable or not</summary>
    /// <type>System.Nullable{System.Boolean}, System</type>
    public bool? IsShareable { get; set; }

    /// <summary>Specifies if a folder is favorite or not</summary>
    /// <type>System.Nullable{System.Boolean}, System</type>
    public bool? IsFavorite { get; set; }

    /// <summary>Number for a new folder</summary>
    /// <type>System.Int32, System</type>
    public int New { get; set; }

    /// <summary>Specifies if a folder is muted or not</summary>
    /// <type>System.Boolean, System</type>
    public bool Mute { get; set; }

    /// <summary>List of tags</summary>
    /// <type>System.Collections.Generic.IEnumerable{System.String}, System.Collections.Generic</type>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>Logo</summary>
    /// <type>ASC.Files.Core.VirtualRooms.Logo, ASC.Files.Core</type>
    public Logo Logo { get; set; }

    /// <summary>Specifies if a folder is pinned or not</summary>
    /// <type>System.Boolean, System</type>
    public bool Pinned { get; set; }

    /// <summary>Room type</summary>
    /// <type>System.Nullable{ASC.Files.Core.ApiModels.RequestDto.RoomType}, System</type>
    public RoomType? RoomType { get; set; }

    /// <summary>Specifies if a folder is private or not</summary>
    /// <type>System.Boolean, System</type>
    public bool Private { get; set; }

    /// <summary>Folder type</summary>
    /// <type>ASC.Files.Core.FolderType, ASC.Files.Core</type>
    public FolderType? Type { get; set; }

    public bool? InRoom { get; set; }

    /// <summary>Quota</summary>
    /// <type>System.Int32, System</type>
    public long? QuotaLimit { get; set; }

    /// <summary>Specifies if the room has a custom quota or not.</summary>
    /// <type>System.Boolean, System</type>
    public bool? IsCustomQuota { get; set; }

    /// <summary>Counter</summary>
    /// <type>System.Int32, System</type>
    public long? UsedSpace { get; set; }
    public static FolderDto<int> GetSample()
    {
        return new FolderDto<int>
        {
            Access = FileShare.ReadWrite,
            //Updated = ApiDateTime.GetSample(),
            //Created = ApiDateTime.GetSample(),
            //CreatedBy = EmployeeWraper.GetSample(),
            Id = 10,
            RootFolderType = FolderType.BUNCH,
            Shared = false,
            Title = "Some titile",
            //UpdatedBy = EmployeeWraper.GetSample(),
            FilesCount = 5,
            FoldersCount = 7,
            ParentId = 10,
            IsShareable = null,
            IsFavorite = null
        };
    }
}

[Scope]
public class FolderDtoHelper(ApiDateTimeHelper apiDateTimeHelper,
        EmployeeDtoHelper employeeWrapperHelper,
        AuthContext authContext,
        IDaoFactory daoFactory,
        FileSecurity fileSecurity,
        GlobalFolderHelper globalFolderHelper,
        FileSharingHelper fileSharingHelper,
        RoomLogoManager roomLogoManager,
        BadgesSettingsHelper badgesSettingsHelper,
        RoomsNotificationSettingsHelper roomsNotificationSettingsHelper,
        FilesSettingsHelper filesSettingsHelper,
        FileDateTime fileDateTime, 
        FileSecurityCommon fileSecurityCommon,
        SettingsManager settingsManager,
        CoreBaseSettings coreBaseSettings,
        TenantManager tenantManager)
    : FileEntryDtoHelper(apiDateTimeHelper, employeeWrapperHelper, fileSharingHelper, fileSecurity, globalFolderHelper, filesSettingsHelper, fileDateTime)
    {

    public async Task<FolderDto<T>> GetAsync<T>(Folder<T> folder, List<Tuple<FileEntry<T>, bool>> folders = null, List<FileShareRecord> currentUserRecords = null, string order = null)
    {
        var result = await GetFolderWrapperAsync(folder);

        result.ParentId = folder.ParentId;

        if (DocSpaceHelper.IsRoom(folder.FolderType))
        {
            if (folder.Tags == null)
            {
                var tagDao = daoFactory.GetTagDao<T>();
                result.Tags = await tagDao.GetTagsAsync(TagType.Custom, new[] { folder }).Select(t => t.Name).ToListAsync();
            }
            else
            {
                result.Tags = folder.Tags.Select(t => t.Name);
            }

            result.Logo = await roomLogoManager.GetLogoAsync(folder);
            result.RoomType = DocSpaceHelper.GetRoomType(folder.FolderType);

            if (folder.ProviderEntry && folder.RootFolderType is FolderType.VirtualRooms)
            {
                result.ParentId = IdConverter.Convert<T>(await _globalFolderHelper.GetFolderVirtualRooms());
            }

            result.Mute = roomsNotificationSettingsHelper.CheckMuteForRoom(result.Id.ToString());

            if (folder.CreateBy == authContext.CurrentAccount.ID ||
                !await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID))
            {
                result.InRoom = true;
            }
            else
            {
                currentUserRecords ??= await _fileSecurity.GetUserRecordsAsync<T>().ToListAsync();

                result.InRoom = currentUserRecords.Exists(c => c.EntryId.Equals(folder.Id));
            }

            if ((coreBaseSettings.Standalone || (await tenantManager.GetCurrentTenantQuotaAsync()).Statistic) && 
                    ((result.Security.TryGetValue(FileSecurity.FilesSecurityActions.Create, out var canCreate) && canCreate) || 
                     ((result.RootFolderType == FolderType.Archive || result.RootFolderType == FolderType.TRASH) && (result.Security.TryGetValue(FileSecurity.FilesSecurityActions.Delete, out var canDelete) && canDelete))))
            {
                var quotaRoomSettings = await settingsManager.LoadAsync<TenantRoomQuotaSettings>();
                result.UsedSpace = folder.Counter;

                if (quotaRoomSettings.EnableQuota && result.RootFolderType != FolderType.Archive && result.RootFolderType != FolderType.TRASH)
                {
                    result.IsCustomQuota = folder.SettingsQuota > -2;
                    result.QuotaLimit = folder.SettingsQuota > -2 ? folder.SettingsQuota : quotaRoomSettings.DefaultQuota;
                }
            }
        }
        if (folder.RootFolderType == FolderType.USER && !Equals(folder.RootCreateBy, authContext.CurrentAccount.ID))
        {
            result.RootFolderType = FolderType.SHARE;

            var folderDao = daoFactory.GetFolderDao<T>();

            if (folders != null)
            {
                var folderWithRight = folders.Find(f => f.Item1.Id.Equals(folder.ParentId));
                if (folderWithRight is not { Item2: true })
                {
                    result.ParentId = await _globalFolderHelper.GetFolderShareAsync<T>();
                }
            }
            else
            {
                FileEntry<T> parentFolder = await folderDao.GetFolderAsync(folder.ParentId);
                var canRead = await _fileSecurity.CanReadAsync(parentFolder);
                if (!canRead)
                {
                    result.ParentId = await _globalFolderHelper.GetFolderShareAsync<T>();
                }
            }
        }

        if (folder.Order != 0)
        {
            result.Order = !string.IsNullOrEmpty(order) ? string.Join('.', order, folder.Order) : folder.Order.ToString();
        }

        if (folder.FolderType == FolderType.InProcessFormFolder || 
            folder.FolderType == FolderType.ReadyFormFolder || 
            folder.FolderType == FolderType.FormFillingFolderDone || 
            folder.FolderType == FolderType.FormFillingFolderInProgress)
        {
            result.Type = folder.FolderType;
        }

        return result;
    }

    private async Task<FolderDto<T>> GetFolderWrapperAsync<T>(Folder<T> folder)
    {
        var newBadges = folder.NewForMe;

        if (folder.RootFolderType == FolderType.VirtualRooms)
        {
            var isEnabledBadges = await badgesSettingsHelper.GetEnabledForCurrentUserAsync();

            if (!isEnabledBadges)
            {
                newBadges = 0;
            }
        }

        var result = await GetAsync<FolderDto<T>, T>(folder);
        result.FilesCount = folder.FilesCount;
        result.FoldersCount = folder.FoldersCount;
        result.IsShareable = folder.Shareable.NullIfDefault();
        result.IsFavorite = folder.IsFavorite.NullIfDefault();
        result.New = newBadges;
        result.Pinned = folder.Pinned;
        result.Private = folder.SettingsPrivate;

        return result;
    }
}
