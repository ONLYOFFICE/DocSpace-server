// (c) Copyright Ascensio System SIA 2009-2026
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
    /// <example>10</example>
    public T ParentId { get; set; }

    /// <summary>
    /// The number of files that the folder contains.
    /// </summary>
    /// <example>5</example>
    public int FilesCount { get; set; }

    /// <summary>
    /// The number of folders that the folder contains.
    /// </summary>
    /// <example>7</example>
    public int FoldersCount { get; set; }

    /// <summary>
    /// Specifies if the folder can be shared or not.
    /// </summary>
    /// <example>true</example>
    public bool? IsShareable { get; set; }

    /// <summary>
    /// The new element index in the folder.
    /// </summary>
    /// <example>0</example>
    public int New { get; set; }

    /// <summary>
    /// Specifies if the folder notifications are enabled or not.
    /// </summary>
    /// <example>false</example>
    public bool Mute { get; set; }

    /// <summary>
    /// The list of tags of the folder.
    /// </summary>
    /// <example>["tag1", "tag2"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The folder logo.
    /// </summary>
    /// <example>{"original": "", "large": "", "medium": "", "small": ""}</example>
    public Logo Logo { get; set; }

    /// <summary>
    /// Specifies if the folder is pinned or not.
    /// </summary>
    /// <example>false</example>
    public bool Pinned { get; set; }

    /// <summary>
    /// The room type of the folder.
    /// </summary>
    /// <example>0</example>
    public RoomType? RoomType { get; set; }

    /// <summary>
    /// Specifies if the folder is private or not.
    /// </summary>
    /// <example>false</example>
    public bool Private { get; set; }

    /// <summary>
    /// Specifies if the folder is indexed or not.
    /// </summary>
    /// <example>true</example>
    public bool Indexing { get; set; }

    /// <summary>
    /// Specifies if the folder can be downloaded or not.
    /// </summary>
    /// <example>false</example>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// The room data lifetime settings of the folder.
    /// </summary>
    /// <example>{"value": 12, "deletePermanently": false}</example>
    public RoomDataLifetimeDto Lifetime { get; set; }

    /// <summary>
    /// The watermark settings of the folder.
    /// </summary>
    /// <example>{"enabled": false}</example>
    public WatermarkDto Watermark { get; set; }

    /// <summary>
    /// The folder type.
    /// </summary>
    /// <example>0</example>
    public FolderType? Type { get; set; }

    /// <summary>
    /// Specifies if the folder is placed in the room or not.
    /// </summary>
    /// <example>false</example>
    public bool? InRoom { get; set; }

    /// <summary>
    /// The folder quota limit.
    /// </summary>
    /// <example>1073741824</example>
    public long? QuotaLimit { get; set; }

    /// <summary>
    /// Specifies if the folder room has a custom quota or not.
    /// </summary>
    /// <example>false</example>
    public bool? IsCustomQuota { get; set; }

    /// <summary>
    /// How much folder space is used (counter).
    /// </summary>
    /// <example>524288000</example>
    public long? UsedSpace { get; set; }

    /// <summary>
    /// Specifies if the folder is password protected or not.
    /// </summary>
    /// <example>false</example>
    public bool? PasswordProtected { get; set; }

    /// <summary>
    /// Specifies if an external link to the folder is expired or not.
    /// </summary>
    /// <example>false</example>
    [Obsolete("Use IsLinkExpired instead")]
    public bool? Expired { get; set; }

    /// <summary>
    /// The file entry type of the folder.
    /// </summary>
    /// <example>1</example>
    public override FileEntryType FileEntryType => FileEntryType.Folder;

    /// <summary>
    /// The AI chat settings for the folder room. Contains configuration for AI provider, model selection, and custom prompts.
    /// Only applicable to rooms with AI chat functionality enabled. Null if the room does not have chat settings configured.
    /// </summary>
    /// <remarks>
    /// This property configures AI-powered chat capabilities for a room. The settings include:
    /// - ProviderId: Identifier for the AI provider (e.g., OpenAI, Azure, internal gateway)
    /// - ModelId: Specific AI model to use (e.g., "gpt-4", "gpt-3.5-turbo")
    /// - Prompt: Custom system prompt to guide AI behavior for this room
    /// - Internal: Auto-calculated flag indicating if using the internal AI gateway
    /// </remarks>
    /// <example>
    /// {
    ///   "ProviderId": 1,
    ///   "ModelId": "gpt-4",
    ///   "Prompt": "You are a helpful assistant for project documentation.",
    ///   "Internal": false
    /// }
    /// </example>
    public ChatSettingsDto ChatSettings { get; set; }

    /// <summary>
    /// The room type of the root folder. Indicates the type of the parent room if the current folder is nested within a room hierarchy.
    /// This property helps identify the context in which a nested folder exists.
    /// </summary>
    /// <remarks>
    /// When a folder is located inside a room (e.g., a subfolder within a collaboration room), this property indicates
    /// the room type of the topmost room in the hierarchy. This is useful for applying room-specific logic or permissions
    /// to nested folders.
    ///
    /// Common room types include:
    /// - CustomRoom (2): Custom collaboration room
    /// - FillingFormsRoom (4): Forms filling room
    /// - EditingRoom (5): Document editing room
    /// - ReviewRoom (6): Document review room
    /// - ReadOnlyRoom (7): Read-only room
    /// - PublicRoom (8): Public access room
    ///
    /// Null if the folder is not nested within a room or is itself a top-level room.
    /// </remarks>
    /// <example>2</example>
    public RoomType? RootRoomType { get; set; }

    /// <summary>
    /// Specifies whether to save form data as XLSX file.
    /// </summary>
    public bool? SaveFormAsXLSX {  get; set; }

    /// <summary>
    /// Specifies whether to send form data to external database.
    /// </summary>
    public bool? SendFormToExternalDB { get; set; }
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
    FileSharing fileSharing,
    EntryStatusManager entryStatusManager,
    AiAccessibility accessibility,
    AiConfiguration aiConfiguration)
    : FileEntryDtoHelper(apiDateTimeHelper, employeeWrapperHelper, fileSharingHelper, fileSecurity, globalFolderHelper, filesSettingsHelper, fileDateTime, securityContext, userManager, daoFactory, externalShare, fileSharing, urlShortener)
{
    private readonly EmployeeDtoHelper _employeeWrapperHelper = employeeWrapperHelper;

    public async Task<FolderDto<T>> GetAsync<T>(
        Folder<T> folder, 
        List<FileShareRecord<string>> currentUserRecords = null, 
        string order = null, 
        IFolder contextFolder = null, 
        AiStatus aiStatus = null)
    {
        var result = await GetFolderWrapperAsync(folder);
        result.ParentId = folder.ParentId;
        
        if (folder.RootFolderType == FolderType.AiAgents && aiStatus == null)
        {
            aiStatus = await accessibility.GetStatusAsync();
        }

        if (folder.IsRoom)
        {
            if (folder.Tags == null)
            {
                var tagDao = _daoFactory.GetTagDao<T>();
                result.Tags = await tagDao.GetTagsAsync([TagType.Custom], [folder]).Select(t => t.Name).ToListAsync();
            }
            else
            {
                result.Tags = folder.Tags.OrderByDescending(t => t.Id).Select(t => t.Name);
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
                    FolderType.DefaultTemplates => IdConverter.Convert<T>(await _globalFolderHelper.FolderDefaultTemplatesAsync),
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

                result.UsedSpace = folder.Counter;

                TenantEntityQuotaSettings quotaSettings = folder.FolderType is FolderType.AiRoom
                ? await settingsManager.LoadAsync<TenantAiAgentQuotaSettings>()
                : await settingsManager.LoadAsync<TenantRoomQuotaSettings>();

                if (quotaSettings.EnableQuota && result.RootFolderType != FolderType.Archive && result.RootFolderType != FolderType.TRASH)
                {
                    result.IsCustomQuota = folder.SettingsQuota > -2;
                    result.QuotaLimit = folder.SettingsQuota > -2 ? folder.SettingsQuota : quotaSettings.DefaultQuota;
                }
            }

            result.Watermark = watermarkHelper.Get(folder.SettingsWatermark);
        }

        if (folder.ShareRecord is { IsLink: true })
        {
            result.External = Equals(folder.ShareRecord.EntryId, folder.Id);;
            result.PasswordProtected = !string.IsNullOrEmpty(folder.ShareRecord.Options?.Password) &&
                                       folder.Security.TryGetValue(FileSecurity.FilesSecurityActions.Read, out var canRead) &&
                                       !canRead;

#pragma warning disable CS0618 // Type or member is obsolete
            result.Expired = folder.ShareRecord.Options?.IsExpired;
            result.IsLinkExpired = folder.ShareRecord.Options?.IsExpired;
            result.RequestToken = await _externalShare.CreateShareKeyAsync(folder.ShareRecord.Subject);
            var expirationDate = folder.ShareRecord?.Options?.ExpirationDate;
            if (expirationDate != null && expirationDate != DateTime.MinValue)
            {
                result.ExpirationDate = _apiDateTimeHelper.Get(expirationDate);
            }

            var cachedFolder = _daoFactory.GetCacheFolderDao<T>();
            var parents = await cachedFolder.GetParentFoldersAsync(result.ParentId).ToListAsync();
            var parent = parents.LastOrDefault();
            if (!await _fileSecurity.CanReadAsync(parent))
            {
                result.ParentId = await _globalFolderHelper.GetFolderShareAsync<T>();
                result.RootFolderType = FolderType.SHARE;
            }
            
            var room = parents.FirstOrDefault(f => f.IsRoom);
            if (room != null)
            {
                result.OwnedBy = await _employeeWrapperHelper.GetAsync(room.CreateBy);
            }
        }

        if (folder.Order != 0)
        {
            if (string.IsNullOrEmpty(order) && contextFolder is not { IsRoom: true })
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
        result.AvailableShareRights = (await _fileSecurity.GetAccesses(folder)).ToDictionary(r => r.Key, r => r.Value.Select(v => v.ToStringFast()));
        
        if (folder.FolderType is FolderType.Knowledge or FolderType.ResultStorage)
        {
            result.Type = folder.FolderType;
        }
        
        if (folder.SettingsChatParameters != null)
        {
            if (folder.SettingsChatProviderId == AiGateway.ProviderId && !aiStatus.GatewayEnabled)
            {
                folder.SettingsChatProviderId = 0;
            }
            
            var modelId = folder.SettingsChatProviderId == 0 ? null : folder.SettingsChatParameters.ModelId;
            var model = modelId != null && folder.ChatProviderType.HasValue
                ? aiConfiguration.GetModel(folder.ChatProviderType.Value, modelId)
                : null;

            ChatMultimodalSettingsDto multimodal = null;
            if (model?.Multimodal?.Image != null)
            {
                multimodal = new ChatMultimodalSettingsDto
                {
                    Image = new ChatImageMultimodalSettingsDto
                    {
                        Formats = model.Multimodal.Image.Formats
                    }
                };
            }

            result.ChatSettings = new ChatSettingsDto
            {
                ProviderId = folder.SettingsChatProviderId,
                ModelId = modelId,
                ModelAlias = model?.Alias,
                Prompt = folder.SettingsChatParameters.Prompt,
                Multimodal = multimodal,
                Thinking = model?.Thinking ?? false
            };
        }

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
            else if (Equals(result.OriginRoomId, await _globalFolderHelper.FolderArchiveAsync))
            {
                result.OriginRoomTitle = result.OriginTitle;
            }            
            else if(result.RootFolderType == FolderType.USER)
            {
                result.OriginRoomTitle = FilesUCResource.SharedForMe;
            }
        }

        if (folder.RootFolderType == FolderType.USER && authContext.IsAuthenticated && !Equals(folder.RootCreateBy, authContext.CurrentAccount.ID))
        {
            switch (contextFolder)
            {
                case { FolderType: FolderType.Favorites }:
                case { FolderType: FolderType.Recent }:
                case { FolderType: FolderType.SHARE }:
                case { RootFolderType: FolderType.USER } when !Equals(contextFolder.RootCreateBy, authContext.CurrentAccount.ID):
                case null:
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

        if (folder.FolderType == FolderType.AiRoom)
        {
            result.FoldersCount -= 2;
        }
        
        if (aiStatus is { Enabled: false})
        {
            switch (folder.FolderType)
            {
                case FolderType.AiAgents:
                    result.Security[FileSecurity.FilesSecurityActions.Create] = false;
                    break;
                case FolderType.AiRoom:
                    result.Security[FileSecurity.FilesSecurityActions.EditRoom] = false;
                    result.Security[FileSecurity.FilesSecurityActions.ChangeOwner] = false;
                    result.Security[FileSecurity.FilesSecurityActions.EditAccess] = false;
                    break;
            }
        }

        return result;
    }

    public async Task<FolderDto<T>> GetShortAsync<T>(Folder<T> folder)
    {
        var result = await GetFolderWrapperAsync(folder);
        result.ParentId = folder.ParentId;

        if (!folder.IsRoom)
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

        if (folder.RootFolderType is FolderType.VirtualRooms or FolderType.RoomTemplates or FolderType.DefaultTemplates)
        {
            var isEnabledBadges = await badgesSettingsHelper.GetEnabledForCurrentUserAsync();

            if (!isEnabledBadges)
            {
                newBadges = 0;
            }
        }

        var result = await GetAsync<FolderDto<T>, T>(folder);
        if (folder.FolderType != FolderType.VirtualRooms && folder.FolderType != FolderType.RoomTemplates && folder.FolderType != FolderType.DefaultTemplates)
        {
            result.FilesCount = folder.FilesCount;
            result.FoldersCount = folder.FoldersCount;
        }
        if (folder.FolderType == FolderType.FillingFormsRoom)
        {
            result.SaveFormAsXLSX = folder.SettingsSaveFormAsXLSX;
            result.SendFormToExternalDB = folder.SettingsSendFormToExternalDB;
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