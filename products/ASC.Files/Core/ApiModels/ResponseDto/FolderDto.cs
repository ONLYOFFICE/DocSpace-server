// (c) Copyright Ascensio System SIA 2009-2025
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
/// The folder parameters.
/// </summary>
public class FolderDto<T> : FileEntryDto<T>
{
    /// <summary>
    /// The parent folder ID of the folder.
    /// </summary>
    [SwaggerSchemaCustom(Example = 10)]
    public T ParentId { get; set; }

    /// <summary>
    /// The number of files that the folder contains.
    /// </summary>
    [SwaggerSchemaCustom(Example = 5)]
    public int FilesCount { get; set; }

    /// <summary>
    /// The number of folders that the folder contains.
    /// </summary>
    [SwaggerSchemaCustom(Example = 7)]
    public int FoldersCount { get; set; }

    /// <summary>
    /// Specifies if the folder can be shared or not.
    /// </summary>
    public bool? IsShareable { get; set; }
    
    /// <summary>
    /// The new element index in the folder.
    /// </summary>
    public int New { get; set; }

    /// <summary>
    /// Specifies if the folder notifications are enabled or not.
    /// </summary>
    public bool Mute { get; set; }

    /// <summary>
    /// The list of tags of the folder.
    /// </summary>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The folder logo.
    /// </summary>
    public Logo Logo { get; set; }

    /// <summary>
    /// Specifies if the folder is pinned or not.
    /// </summary>
    public bool Pinned { get; set; }

    /// <summary>
    /// The room type of the folder.
    /// </summary>
    public RoomType? RoomType { get; set; }

    /// <summary>
    /// Specifies if the folder is private or not.
    /// </summary>
    public bool Private { get; set; }

    /// <summary>
    /// Specifies if the folder is indexed or not.
    /// </summary>
    public bool Indexing { get; set; }

    /// <summary>
    /// Specifies if the folder can be downloaded or not.
    /// </summary>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// The room data lifetime settings of the folder.
    /// </summary>
    public RoomDataLifetimeDto Lifetime { get; set; }

    /// <summary>
    /// The watermark settings of the folder.
    /// </summary>
    public WatermarkDto Watermark { get; set; }

    /// <summary>
    /// The folder type.
    /// </summary>
    public FolderType? Type { get; set; }

    /// <summary>
    /// Specifies if the folder is placed in the room or not.
    /// </summary>
    public bool? InRoom { get; set; }

    /// <summary>
    /// The folder quota limit.
    /// </summary>
    public long? QuotaLimit { get; set; }

    /// <summary>
    /// Specifies if the folder room has a custom quota or not.
    /// </summary>
    public bool? IsCustomQuota { get; set; }

    /// <summary>
    /// How much folder space is used (counter).
    /// </summary>
    public long? UsedSpace { get; set; }
    
    /// <summary>
    /// Specifies if the folder is password protected or not.
    /// </summary>
    public bool? PasswordProtected { get; set; }

    /// <summary>
    /// Specifies if an external link to the folder is expired or not.
    /// </summary>
    public bool? Expired { get; set; }

    /// <summary>
    /// The file entry type of the folder.
    /// </summary>
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
    ExternalShare externalShare,
    FileSecurityCommon fileSecurityCommon,
    SecurityContext securityContext,
    UserManager userManager,
    IUrlShortener urlShortener,
    EntryStatusManager entryStatusManager)
    : FileEntryDtoHelper(apiDateTimeHelper, employeeWrapperHelper, fileSharingHelper, fileSecurity, globalFolderHelper, filesSettingsHelper, fileDateTime, securityContext, userManager, daoFactory, externalShare, urlShortener)
{
    public async Task<FolderDto<T>> GetAsync<T>(Folder<T> folder, List<FileShareRecord<string>> currentUserRecords = null, string order = null, IFolder contextFolder = null)
    {
        var result = await GetFolderWrapperAsync(folder);
        result.ParentId = folder.ParentId;

        if (DocSpaceHelper.IsRoom(folder.FolderType))
        {
            if (folder.Tags == null)
            {
                var tagDao = _daoFactory.GetTagDao<T>();
                result.Tags = await tagDao.GetTagsAsync([TagType.Custom], [folder]).Select(t => t.Name).ToListAsync();
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
                    FolderType.RoomTemplates => IdConverter.Convert<T>(await _globalFolderHelper.FolderRoomTemplatesAsync),
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

            if ((await tenantManager.GetCurrentTenantQuotaAsync()).Statistic &&
                    ((result.Security.TryGetValue(FileSecurity.FilesSecurityActions.Create, out var canCreate) && canCreate) ||
                     (result.RootFolderType is FolderType.Archive or FolderType.TRASH && result.Security.TryGetValue(FileSecurity.FilesSecurityActions.Delete, out var canDelete) && canDelete) ||
                     await fileSecurityCommon.IsDocSpaceAdministratorAsync(authContext.CurrentAccount.ID)))
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
        }
        
        if (folder.ShareRecord is { IsLink: true })
        {
            result.External = true;
            result.PasswordProtected = !string.IsNullOrEmpty(folder.ShareRecord.Options?.Password) && 
                                       folder.Security.TryGetValue(FileSecurity.FilesSecurityActions.Read, out var canRead) && 
                                       !canRead;

            result.Expired = folder.ShareRecord.Options?.IsExpired;
            result.RequestToken = await _externalShare.CreateShareKeyAsync(folder.ShareRecord.Subject);
            result.ExpirationDate = _apiDateTimeHelper.Get(folder.ShareRecord?.Options?.ExpirationDate);
            result.RootFolderType = FolderType.SHARE;
            var parent = await _daoFactory.GetCacheFolderDao<T>().GetFolderAsync(result.ParentId);
            if (!await _fileSecurity.CanReadAsync(parent))
            {
                result.ParentId = await _globalFolderHelper.GetFolderShareAsync<T>();
            }
        }
        
        if (folder.Order != 0)
        {
            if (string.IsNullOrEmpty(order) && (contextFolder == null || !DocSpaceHelper.IsRoom(contextFolder.FolderType)))
            {
                order = await breadCrumbsManager.GetBreadCrumbsOrderAsync(folder.ParentId);
            }
            
            result.Order = !string.IsNullOrEmpty(order) ? string.Join('.', order, folder.Order) : folder.Order.ToString();
        }

        if (DocSpaceHelper.IsFormsFillingSystemFolder(folder.FolderType))
        {
            result.Type = folder.FolderType;
        }

        result.Lifetime = folder.SettingsLifetime.MapToDto();
        result.AvailableShareRights =  (await _fileSecurity.GetAccesses(folder)).ToDictionary(r => r.Key, r => r.Value.Select(v => v.ToStringFast()));

        if (contextFolder is { FolderType: FolderType.Recent } or { FolderType: FolderType.Favorites })
        {
            var forbiddenActions = new List<FileSecurity.FilesSecurityActions>
            {
                FileSecurity.FilesSecurityActions.FillForms,
                FileSecurity.FilesSecurityActions.Edit,
                FileSecurity.FilesSecurityActions.SubmitToFormGallery,
                FileSecurity.FilesSecurityActions.CreateRoomFrom,
                FileSecurity.FilesSecurityActions.Duplicate,
                FileSecurity.FilesSecurityActions.Delete,
                FileSecurity.FilesSecurityActions.Lock,
                FileSecurity.FilesSecurityActions.CustomFilter,
                FileSecurity.FilesSecurityActions.Embed,
                FileSecurity.FilesSecurityActions.StartFilling,
                FileSecurity.FilesSecurityActions.StopFilling,
                FileSecurity.FilesSecurityActions.CopySharedLink,
                FileSecurity.FilesSecurityActions.CopyLink,
                FileSecurity.FilesSecurityActions.FillingStatus
            };

            foreach (var action in forbiddenActions)
            {
                result.Security[action] = false;   
            }

            result.CanShare = false;

            result.Order = "";

            var myId = await _globalFolderHelper.GetFolderMyAsync<T>();
            result.OriginTitle = Equals(result.OriginId, myId) ? FilesUCResource.MyFiles : result.OriginTitle;
            
            if (Equals(result.OriginRoomId, myId))
            {
                result.OriginRoomTitle = FilesUCResource.MyFiles;
            }
            else if(Equals(result.OriginRoomId,  await _globalFolderHelper.FolderArchiveAsync))
            {
                result.OriginRoomTitle = result.OriginTitle;
            }
        }
        
        if (folder.RootFolderType == FolderType.USER && authContext.IsAuthenticated && !Equals(folder.RootCreateBy, authContext.CurrentAccount.ID))
        {
            switch (contextFolder)
            {
                case { FolderType: FolderType.Recent }:
                case { FolderType: FolderType.SHARE }:
                case { RootFolderType: FolderType.USER } when !Equals(contextFolder.RootCreateBy, authContext.CurrentAccount.ID):
                    result.RootFolderType = FolderType.SHARE;
                    result.RootFolderId = await _globalFolderHelper.GetFolderShareAsync<T>();
                    var parent = await _daoFactory.GetCacheFolderDao<T>().GetFolderAsync(result.ParentId);
                    if (!await _fileSecurity.CanReadAsync(parent))
                    {
                        result.ParentId = await _globalFolderHelper.GetFolderShareAsync<T>();
                    }

                    break;
            }
        }
        
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

        if (folder.RootFolderType is FolderType.VirtualRooms or FolderType.RoomTemplates)
        {
            var isEnabledBadges = await badgesSettingsHelper.GetEnabledForCurrentUserAsync();

            if (!isEnabledBadges)
            {
                newBadges = 0;
            }
        }
        
        var result = await GetAsync<FolderDto<T>, T>(folder);
        if (folder.FolderType != FolderType.VirtualRooms && folder.FolderType != FolderType.RoomTemplates)
        {
            result.FilesCount = folder.FilesCount;
            result.FoldersCount = folder.FoldersCount;
        }
        
        await entryStatusManager.SetIsFavoriteFolderAsync(folder);
        
        result.IsShareable = folder.Shareable.NullIfDefault();
        result.IsFavorite = folder.IsFavorite;
        result.New = newBadges;
        result.Pinned = folder.Pinned;
        result.Private = folder.SettingsPrivate;
        result.Indexing = folder.SettingsIndexing;
        result.DenyDownload = folder.SettingsDenyDownload;

        return result;
    }
}
