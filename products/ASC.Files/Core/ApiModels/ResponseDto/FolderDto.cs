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

public class FolderDto<T> : FileEntryDto<T>
{
    /// <summary>
    /// Parent folder ID
    /// </summary>
    [SwaggerSchemaCustom(Example = 10)]
    public T ParentId { get; set; }

    /// <summary>
    /// "Number of files
    /// </summary>
    [SwaggerSchemaCustom(Example = 5)]
    public int FilesCount { get; set; }

    /// <summary>
    /// Number of folders
    /// </summary>
    [SwaggerSchemaCustom(Example = 7)]
    public int FoldersCount { get; set; }

    /// <summary>
    /// Specifies if a folder is shareable or not
    /// </summary>
    public bool? IsShareable { get; set; }

    /// <summary>
    /// Specifies if a folder is favorite or not
    /// </summary>
    public bool? IsFavorite { get; set; }

    /// <summary>
    /// Number for a new folder
    /// </summary>
    public int New { get; set; }

    /// <summary>
    /// Specifies if a folder is muted or not
    /// </summary>
    public bool Mute { get; set; }

    /// <summary>
    /// List of tags
    /// </summary>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// Logo
    /// </summary>
    public Logo Logo { get; set; }

    /// <summary>
    /// Specifies if a folder is pinned or not
    /// </summary>
    public bool Pinned { get; set; }

    /// <summary>
    /// Room type
    /// </summary>
    public RoomType? RoomType { get; set; }

    /// <summary>
    /// Specifies if a folder is private or not
    /// </summary>
    public bool Private { get; set; }
    public bool Indexing { get; set; }
    public bool DenyDownload { get; set; }

    /// <summary>Room data lifetime settings</summary>
    public RoomDataLifetimeDto Lifetime { get; set; }

    public WatermarkDto Watermark { get; set; }

    /// <summary>
    /// Folder type
    /// </summary>
    public FolderType? Type { get; set; }

    /// <summary>
    /// InRoom
    /// </summary>
    public bool? InRoom { get; set; }

    /// <summary>
    /// Quota
    /// </summary>
    public long? QuotaLimit { get; set; }

    /// <summary>
    /// Specifies if the room has a custom quota or not
    /// </summary>
    public bool? IsCustomQuota { get; set; }

    /// <summary>
    /// Counter
    /// </summary>
    public long? UsedSpace { get; set; }
    
    public bool? External { get; set; }
    public bool? PasswordProtected { get; set; }
    public bool? Expired { get; set; }

    public override FileEntryType FileEntryType { get => FileEntryType.Folder; }
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
    BreadCrumbsManager breadCrumbsManager,
    TenantManager tenantManager,
    WatermarkDtoHelper watermarkHelper,
    IMapper mapper,
    ExternalShare externalShare)
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
                result.Tags = await tagDao.GetTagsAsync(TagType.Custom, [folder]).Select(t => t.Name).ToListAsync();
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

            if ((await tenantManager.GetCurrentTenantQuotaAsync()).Statistic)
            {
                var quotaRoomSettings = await settingsManager.LoadAsync<TenantRoomQuotaSettings>();
                result.UsedSpace = folder.Counter;

                if (quotaRoomSettings.EnableQuota && result.RootFolderType != FolderType.Archive && result.RootFolderType != FolderType.TRASH)
                {
                    result.IsCustomQuota = folder.SettingsQuota > -2;
                    result.QuotaLimit = folder.SettingsQuota > -2 ? folder.SettingsQuota : quotaRoomSettings.DefaultQuota;
                }
            }
            
            result.Watermark = watermarkHelper.Get(folder.SettingsWatermark);

            if (folder.ShareRecord is { IsLink: true })
            {
                result.External = true;
                result.PasswordProtected = !string.IsNullOrEmpty(folder.ShareRecord.Options?.Password) && 
                                           folder.Security.TryGetValue(FileSecurity.FilesSecurityActions.Read, out var canRead) && 
                                           !canRead;

                result.Expired = folder.ShareRecord.Options?.IsExpired;
                result.RequestToken = await externalShare.CreateShareKeyAsync(folder.ShareRecord.Subject);
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
    
    public async Task<FolderDto<T>> GetShortAsync<T>(Folder<T> folder)
    {
        var result = await GetFolderWrapperAsync(folder);
        result.ParentId = folder.ParentId;

        if (!DocSpaceHelper.IsRoom(folder.FolderType))
        {
            return result;
        }

        result.RoomType = DocSpaceHelper.MapToRoomType(folder.FolderType);
        result.Logo = await roomLogoManager.GetLogoAsync(folder);

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
        if (folder.FolderType != FolderType.VirtualRooms)
        {
            result.FilesCount = folder.FilesCount;
            result.FoldersCount = folder.FoldersCount;
        }

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
