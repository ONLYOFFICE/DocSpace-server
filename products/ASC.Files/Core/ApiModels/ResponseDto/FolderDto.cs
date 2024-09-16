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
    public bool Indexing { get; set; }
    public bool DenyDownload { get; set; }

    /// <summary>Room data lifetime settings</summary>
    /// <type>ASC.Files.Core.ApiModels.RoomDataLifetimeDto, ASC.Files.Core</type>
    public RoomDataLifetimeDto Lifetime { get; set; }

    /// <summary>Folder type</summary>
    /// <type>System.Nullable{ASC.Files.Core.FolderType}, System</type>
    public FolderType? Type { get; set; }

    public bool? InRoom { get; set; }

    /// <summary>Quota</summary>
    /// <type>System.Nullable{System.Int64}, System</type>
    public long? QuotaLimit { get; set; }

    /// <summary>Specifies if the room has a custom quota or not</summary>
    /// <type>System.Nullable{System.Boolean}, System</type>
    public bool? IsCustomQuota { get; set; }

    /// <summary>Counter</summary>
    /// <type>System.Nullable{System.Int64}, System</type>
    public long? UsedSpace { get; set; }

    public override FileEntryType FileEntryType { get => FileEntryType.Folder; }

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
public class FolderDtoHelper(
    ApiDateTimeHelper apiDateTimeHelper,
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
    SettingsManager settingsManager,
    CoreBaseSettings coreBaseSettings,
    BreadCrumbsManager breadCrumbsManager,
    TenantManager tenantManager,
    IMapper mapper)
    : FileEntryDtoHelper(apiDateTimeHelper, employeeWrapperHelper, fileSharingHelper, fileSecurity, globalFolderHelper, filesSettingsHelper, fileDateTime)
    {

    public async Task<FolderDto<T>> GetAsync<T>(Folder<T> folder, List<FileShareRecord<string>> currentUserRecords = null, string order = null)
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
            result.RoomType = DocSpaceHelper.MapToRoomType(folder.FolderType);

            if (folder.ProviderEntry)
            {
                result.ParentId = folder.RootFolderType switch
                {
                    FolderType.VirtualRooms => IdConverter.Convert<T>(await _globalFolderHelper.FolderVirtualRoomsAsync),
                    FolderType.Archive => IdConverter.Convert<T>(await _globalFolderHelper.FolderArchiveAsync),
                    _ => result.ParentId
                };
            }

            result.Mute = await roomsNotificationSettingsHelper.CheckMuteForRoomAsync(result.Id.ToString());

            if (folder.CreateBy == authContext.CurrentAccount.ID)
            {
                result.InRoom = true;
            }
            else if (folder.ShareRecord is { SubjectType: SubjectType.Group })
            {
                result.InRoom = false;
            }
            else
            {
                currentUserRecords ??= await _fileSecurity.GetUserRecordsAsync().ToListAsync();

                result.InRoom = currentUserRecords.Exists(c => c.EntryId.Equals(folder.Id.ToString()) && c.SubjectType == SubjectType.User) && 
                                !currentUserRecords.Exists(c => c.EntryId.Equals(folder.Id.ToString()) && c.SubjectType == SubjectType.Group);
            }

            if ((coreBaseSettings.Standalone || (await tenantManager.GetCurrentTenantQuotaAsync()).Statistic) && 
                    ((result.Security.TryGetValue(FileSecurity.FilesSecurityActions.Create, out var canCreate) && canCreate) || 
                     (result.RootFolderType is FolderType.Archive or FolderType.TRASH && (result.Security.TryGetValue(FileSecurity.FilesSecurityActions.Delete, out var canDelete) && canDelete))))
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

        if (folder.Order != 0)
        {
            if (string.IsNullOrEmpty(order))
            {
                order = await breadCrumbsManager.GetBreadCrumbsOrderAsync(folder.ParentId);
            }
            
            result.Order = !string.IsNullOrEmpty(order) ? string.Join('.', order, folder.Order) : folder.Order.ToString();
        }

        if (DocSpaceHelper.IsFormsFillingSystemFolder(folder.FolderType))
        {
            result.Type = folder.FolderType;
        }

        result.Lifetime = mapper.Map<RoomDataLifetime, RoomDataLifetimeDto>(folder.SettingsLifetime);

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
        result.Indexing = folder.SettingsIndexing;
        result.DenyDownload = folder.SettingsDenyDownload;

        return result;
    }
}
