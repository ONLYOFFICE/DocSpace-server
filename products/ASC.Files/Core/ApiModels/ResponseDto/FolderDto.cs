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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>The folder, with the fields that only a room carries filled in when the folder is a room.</summary>
public class FolderDto<T> : FileEntryDto<T>
{
    /// <summary>
    /// The folder this one is listed in. For a room it is the root of the section the room lives in, and for an entry
    /// opened through a sharing link whose real parent the caller may not read it is the root of the section with the
    /// entries shared with them.
    /// </summary>
    /// <example>10</example>
    public T ParentId { get; set; }

    /// <summary>
    /// How many files lie directly in the folder, without counting the subfolders. The roots of the `Rooms`, room
    /// templates and default templates sections always report 0, because the number is not collected for them.
    /// </summary>
    /// <example>5</example>
    public int FilesCount { get; set; }

    /// <summary>
    /// How many subfolders lie directly in the folder. For an AI room the two service subfolders it always holds are
    /// subtracted, so the number matches what a listing of it shows, and the roots of the `Rooms` and templates
    /// sections report 0.
    /// </summary>
    /// <example>7</example>
    public int FoldersCount { get; set; }

    /// <summary>
    /// Whether the caller may hand out access to the folder. It is filled in only for the folder a folder-contents
    /// answer is about, and is null in every other answer, so null says nothing about the sharing rights.
    /// </summary>
    /// <example>true</example>
    public bool? IsShareable { get; set; }

    /// <summary>
    /// How many entries inside the folder the caller has not opened yet, the number drawn as the badge on it. An
    /// account that turned the badges off in its own settings always reads 0 here, so 0 alone does not prove that
    /// everything has been seen.
    /// </summary>
    /// <example>3</example>
    public int New { get; set; }

    /// <summary>
    /// Whether the caller silenced the notifications of this room: true means no message about its activity reaches
    /// them. The choice belongs to the reading account rather than to the room, so two members of one room read
    /// different values.
    /// </summary>
    /// <example>false</example>
    public bool Mute { get; set; }

    /// <summary>
    /// The names of the tags attached to the room. Empty for a folder that is not a room, since only rooms carry
    /// tags, and the names are the ones from the portal tag catalogue.
    /// </summary>
    /// <example>["Marketing", "Q3"]</example>
    public IEnumerable<string> Tags { get; set; }

    /// <summary>
    /// The addresses of the room logo in four sizes, together with the colour and the built-in cover that are drawn
    /// when no logo was uploaded. A room without a logo answers with four empty addresses rather than with null, and
    /// the field is null for a folder that is not a room.
    /// </summary>
    /// <example>
    /// {"original": "", "large": "", "medium": "", "small": "", "color": "F2C4C4", "cover": {"id": "bookmark"}}
    /// </example>
    public Logo Logo { get; set; }

    /// <summary>
    /// Whether the caller pinned the room to the top of their own room list. Pinning is personal and is lost when the
    /// room is archived.
    /// </summary>
    /// <example>false</example>
    public bool Pinned { get; set; }

    /// <summary>
    /// The kind of the room, which decides the default access rules of its members. Null for a folder that is not a
    /// room.
    /// </summary>
    /// <example>2</example>
    public RoomType? RoomType { get; set; }

    /// <summary>
    /// Whether the room is a private one, which limits it to the accounts invited into it and needs encryption keys
    /// set up for each of them.
    /// </summary>
    /// <example>false</example>
    public bool Private { get; set; }

    /// <summary>
    /// Whether the contents of the room are kept in an explicit numbered order, the one reported as `order` on each
    /// entry, instead of being left to the sorting the reader asks for.
    /// </summary>
    /// <example>true</example>
    public bool Indexing { get; set; }

    /// <summary>
    /// Whether downloading and printing the contents of the room is forbidden, which leaves its members with viewing
    /// and editing in the editor.
    /// </summary>
    /// <example>false</example>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// The rule by which the files of the room are removed once they grow old. Null when the room has no such rule,
    /// which is also what is reported after the rule is switched off, because switching it off erases it.
    /// </summary>
    /// <example>{"enabled": true, "period": 1, "value": 12, "deletePermanently": false}</example>
    public RoomDataLifetimeDto Lifetime { get; set; }

    /// <summary>
    /// The watermark stamped over the documents of the room while they are viewed and printed. Null when the room has
    /// no watermark, and for every folder that is not a room.
    /// </summary>
    /// <example>{"additions": 1, "text": "Confidential", "rotate": -45, "imageScale": 100}</example>
    public WatermarkDto Watermark { get; set; }

    /// <summary>
    /// The part the folder plays inside its room: one of the service folders of the form-filling flow, or the
    /// knowledge and result storages of an AI room. It stays null for an ordinary folder and for the room itself, so
    /// it does not describe folders in general.
    /// </summary>
    /// <example>27</example>
    public FolderType? Type { get; set; }

    /// <summary>
    /// Whether the caller holds the room through an invitation of their own: true for the account that created it and
    /// for a member invited personally, false when the access comes from a group they belong to, and null for a
    /// folder that is not a room.
    /// </summary>
    /// <example>false</example>
    public bool? InRoom { get; set; }

    /// <summary>
    /// How much space the files of the room may take, in bytes. It is the limit set on this room, or the portal
    /// default for rooms when none was set. Null when the tariff of the portal does not count room statistics, when
    /// room quotas are switched off, when the room lies in the archive or the trash, or when the caller may only read
    /// it.
    /// </summary>
    /// <example>1073741824</example>
    public long? QuotaLimit { get; set; }

    /// <summary>
    /// Whether `quotaLimit` is a limit set on this room (true) or the portal default for rooms (false). Null exactly
    /// when `quotaLimit` is null.
    /// </summary>
    /// <example>false</example>
    public bool? IsCustomQuota { get; set; }

    /// <summary>
    /// How much the files of the room take, in bytes, as of the last time the counter was recomputed. The counter is
    /// refreshed when a file operation finishes, so a read right after an upload or a deletion can still report the
    /// previous figure. Null for a folder that is not a room.
    /// </summary>
    /// <example>524288000</example>
    public long? UsedSpace { get; set; }

    /// <summary>
    /// Whether the sharing link the folder was opened through asks for a password that has not been entered yet.
    /// While it is true the contents stay unreadable; send the password to `POST api/2.0/files/share/{key}/password`
    /// first. Null when the folder was not reached through a link.
    /// </summary>
    /// <example>false</example>
    public bool? PasswordProtected { get; set; }

    /// <summary>
    /// Deprecated, read `isLinkExpired` instead: whether the sharing link the folder was opened through has run out
    /// of its lifetime.
    /// </summary>
    /// <example>false</example>
    [Obsolete("Use IsLinkExpired instead")]
    public bool? Expired { get; set; }

    /// <summary>
    /// Always reports a folder, which is what tells folders from files apart in a listing that mixes both.
    /// </summary>
    /// <example>1</example>
    public override FileEntryType FileEntryType => FileEntryType.Folder;

    /// <summary>
    /// The chat configuration of an AI room. Only the system prompt is reported here, whatever else the room stores,
    /// and the field is null for every folder that is not an AI room.
    /// </summary>
    /// <example>{"prompt": "You are a helpful assistant for project documentation."}</example>
    public ChatSettingsDto ChatSettings { get; set; }

    /// <summary>
    /// The kind of the room the folder lies in. It is filled in only for the folder a folder-contents answer is
    /// about, and only when that room is an AI room, so it is null in every other answer and for every other room
    /// kind.
    /// </summary>
    /// <example>9</example>
    public RoomType? RootRoomType { get; set; }

    /// <summary>
    /// Whether the answers collected in this form-filling room are also gathered into a spreadsheet next to the
    /// completed copies. Filled in for form-filling rooms only.
    /// </summary>
    /// <example>false</example>
    public bool? SaveFormAsXLSX {  get; set; }

    /// <summary>
    /// Whether the answers collected in this form-filling room are also pushed into the external database configured
    /// for the portal. Filled in for form-filling rooms only.
    /// </summary>
    /// <example>false</example>
    public bool? SendFormToExternalDB { get; set; }

    /// <summary>
    /// The form the completed copies in this folder were filled from, taken from the copy submitted last. Null while
    /// the folder holds no completed copy, and for every folder that does not collect them.
    /// </summary>
    /// <example>42</example>
    public int? OriginalFormId { get; set; }
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
    SecurityContext securityContext,
    UserManager userManager,
    IUrlShortener urlShortener,
    FileSharing fileSharing,
    EntryStatusManager entryStatusManager)
    : FileEntryDtoHelper(apiDateTimeHelper, employeeWrapperHelper, fileSharingHelper, fileSecurity, globalFolderHelper, filesSettingsHelper, fileDateTime, securityContext, userManager, daoFactory, externalShare, fileSharing, urlShortener)
{
    private readonly EmployeeDtoHelper _employeeWrapperHelper = employeeWrapperHelper;

    public async Task<FolderDto<T>> GetAsync<T>(
        Folder<T> folder,
        List<FileShareRecord<string>> currentUserRecords = null,
        string order = null,
        IFolder contextFolder = null)
    {
        var result = await GetFolderWrapperAsync(folder);
        result.ParentId = folder.ParentId;

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

            result.UsedSpace = folder.Counter;

            if ((await tenantManager.GetCurrentTenantQuotaAsync()).Statistic &&
                    ((result.Security.TryGetValue(FileSecurity.FilesSecurityActions.EditRoom, out var canEdit) && canEdit) ||
                     (result.RootFolderType is FolderType.Archive or FolderType.TRASH && result.Security.TryGetValue(FileSecurity.FilesSecurityActions.Delete, out var canDelete) && canDelete) ||
                     (result.Security.TryGetValue(FileSecurity.FilesSecurityActions.Create, out var canCreate) && canCreate)))
            {
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
            result.External = Equals(folder.ShareRecord.EntryId, folder.Id);
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

        if (folder.IsAgent && folder.ChatSettings == null)
        {
            folder.ChatSettings = await _daoFactory.GetFolderDao<T>().GetChatSettingsAsync(folder.Id);
        }

        if (folder.ChatSettings != null)
        {
            result.ChatSettings = new ChatSettingsDto
            {
                Prompt = folder.ChatSettings.Prompt
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
            result.OriginTitle = Equals(result.OriginId, myId) ? FilesUCResource.Files : result.OriginTitle;

            if (Equals(result.OriginRoomId, myId))
            {
                result.OriginRoomTitle = FilesUCResource.Files;
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

        if (folder.FolderType == FolderType.FormFillingFolderDone && folder.Id is int doneFolderId)
        {
            var fileDao = _daoFactory.GetFileDao<int>();
            var completedForm = await fileDao
                .GetFilesAsync(doneFolderId, new OrderBy(SortedByType.DateAndTime, false), FilterType.PdfForm, false, Guid.Empty, null, null, false, count: 1)
                .FirstOrDefaultAsync();

            var canUpdateXlsx = false;
            if (completedForm != null)
            {
                var completedFormProperties = await fileDao.GetProperties(completedForm.Id);
                var originalFormId = completedFormProperties?.FormFilling?.OriginalFormId ?? 0;
                if (originalFormId != 0)
                {
                    result.OriginalFormId = originalFormId;
                    var originalForm = await fileDao.GetFileAsync(originalFormId);
                    canUpdateXlsx = originalForm != null && await _fileSecurity.CanUpdateXlsxAsync(originalForm);
                }
            }

            result.Security[FileSecurity.FilesSecurityActions.UpdateXlsx] = canUpdateXlsx;
            result.Security[FileSecurity.FilesSecurityActions.AnalyzeResponses] = canUpdateXlsx;
        }
        else
        {
            result.Security[FileSecurity.FilesSecurityActions.UpdateXlsx] = false;
            result.Security[FileSecurity.FilesSecurityActions.AnalyzeResponses] = false;
        }

        if (folder.FolderType.IsPublicSystemFolder())
        {
            result.CreatedBy = EmployeeDto.Default;
            result.UpdatedBy = EmployeeDto.Default;
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
