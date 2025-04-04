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

public class FileDto<T> : FileEntryDto<T>
{
    /// <summary>
    /// Folder ID
    /// </summary>
    public T FolderId { get; set; }

    /// <summary>
    /// Version
    /// </summary>
    [SwaggerSchemaCustom(Example = 3)]
    public int Version { get; set; }

    /// <summary>
    /// Version group
    /// </summary>
    [SwaggerSchemaCustom(Example = 1)]
    public int VersionGroup { get; set; }

    /// <summary>
    /// Content length
    /// </summary>
    [SwaggerSchemaCustom(Example = "12345")]
    public string ContentLength { get; set; }

    /// <summary>
    /// Pure content length
    /// </summary>
    public long? PureContentLength { get; set; }

    /// <summary>
    /// File status
    /// </summary>
    public FileStatus FileStatus { get; set; }

    /// <summary>
    /// Muted or not
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool Mute { get; set; }

    /// <summary>
    /// URL to view a file
    /// </summary>
    [SwaggerSchemaCustom(Example = "https://www.onlyoffice.com/viewfile?fileid=2221")]
    [Url]
    public string ViewUrl { get; set; }

    /// <summary>
    /// Web URL
    /// </summary>
    [Url]
    public string WebUrl { get; set; }

    /// <summary>
    /// Short Web URL
    /// </summary>
    [Url]
    public string ShortWebUrl { get; set; }

    /// <summary>
    /// File type
    /// </summary>
    public FileType FileType { get; set; }

    /// <summary>
    /// File extension
    /// </summary>
    [SwaggerSchemaCustom(Example = ".txt")]
    public string FileExst { get; set; }

    /// <summary>
    /// Comment
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// Encrypted or not
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool? Encrypted { get; set; }

    /// <summary>
    /// Thumbnail URL
    /// </summary>
    [Url]
    public string ThumbnailUrl { get; set; }

    /// <summary>
    /// Thumbnail status
    /// </summary>
    public Thumbnail ThumbnailStatus { get; set; }

    /// <summary>
    /// Locked or not
    /// </summary>
    public bool? Locked { get; set; }

    /// <summary>
    /// User name who locked a file
    /// </summary>
    public string LockedBy { get; set; }

    /// <summary>
    /// Is there a draft or not
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool? HasDraft { get; set; }

    /// <summary>
    /// Status of the form filling process
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public FormFillingStatus FormFillingStatus { get; set; } = FormFillingStatus.None;

    /// <summary>
    /// Is there a form or not
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool? IsForm { get; set; }

    /// <summary>
    /// Is Custom Filter editing mode enabled for a file or not
    /// </summary>
    public bool? CustomFilterEnabled { get; set; }

    /// <summary>
    /// User name who enabled a Custom Filter editing mode for a file
    /// </summary>
    public string CustomFilterEnabledBy { get; set; }

    /// <summary>
    /// Specifies if the filling has started or not
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool? StartFilling { get; set; }

    /// <summary>
    /// InProcess folder ID
    /// </summary>
    public int? InProcessFolderId { get; set; }

    /// <summary>
    /// InProcess folder title
    /// </summary>
    public string InProcessFolderTitle { get; set; }

    /// <summary>
    /// Draft info
    /// </summary>
    public DraftLocation<T> DraftLocation { get; set; }

    /// <summary>
    /// File accessibility
    /// </summary>
    public IDictionary<Accessibility, bool> ViewAccessibility { get; set; }

    /// <summary>
    /// Available external rights
    /// </summary>
    public IDictionary<string, bool> AvailableExternalRights { get; set; }

    /// <summary>
    /// Last opened
    /// </summary>
    public ApiDateTime LastOpened { get; set; }
    public ApiDateTime Expired { get; set; }
    
    public override FileEntryType FileEntryType { get => FileEntryType.File; }
}

[Scope]
public class FileDtoHelper(
    ApiDateTimeHelper apiDateTimeHelper,
        EmployeeDtoHelper employeeWrapperHelper,
        AuthContext authContext,
        IDaoFactory daoFactory,
        FileSecurity fileSecurity,
        GlobalFolderHelper globalFolderHelper,
        CommonLinkUtility commonLinkUtility,
        FilesLinkUtility filesLinkUtility,
        FileUtility fileUtility,
        FileSharingHelper fileSharingHelper,
        BadgesSettingsHelper badgesSettingsHelper,
        FilesSettingsHelper filesSettingsHelper,
        FileDateTime fileDateTime,
        ExternalShare externalShare,
        BreadCrumbsManager breadCrumbsManager,
        FileChecker fileChecker,
        SecurityContext securityContext,
        UserManager userManager,
        IUrlShortener urlShortener)
    : FileEntryDtoHelper(apiDateTimeHelper, employeeWrapperHelper, fileSharingHelper, fileSecurity, globalFolderHelper, filesSettingsHelper, fileDateTime, securityContext, userManager, daoFactory) 
{
    private readonly ApiDateTimeHelper _apiDateTimeHelper = apiDateTimeHelper;

    public async Task<FileDto<T>> GetAsync<T>(File<T> file, string order = null, TimeSpan? expiration = null, IFolder contextFolder = null)
    {
        var result = await GetFileWrapperAsync(file, order, expiration, contextFolder);

        result.FolderId = file.ParentId;
        
        if (file.RootFolderType == FolderType.USER && authContext.IsAuthenticated && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
        {
            result.RootFolderType = FolderType.Recent;
            result.FolderId = await _globalFolderHelper.GetFolderRecentAsync<T>();
        }
        
        result.ViewAccessibility = await fileUtility.GetAccessibility(file);
        result.AvailableExternalRights = _fileSecurity.GetFileAccesses(file, SubjectType.ExternalLink);
        
        return result;
    }

    private async Task<FileDto<T>> GetFileWrapperAsync<T>(File<T> file, string order, TimeSpan? expiration, IFolder contextFolder = null)
    {
        var result = await GetAsync<FileDto<T>, T>(file);
        var isEnabledBadges = await badgesSettingsHelper.GetEnabledForCurrentUserAsync();

        var extension = FileUtility.GetFileExtension(file.Title);
        var fileType = FileUtility.GetFileTypeByExtention(extension);

        var fileDao = _daoFactory.GetFileDao<T>();

        if (file.IsForm)
        {
            var folderDao = _daoFactory.GetCacheFolderDao<T>();

            Task<T> linkedIdTask;
            Task<EntryProperties<T>> propertiesTask;
            
            if (file.FormInfo != null)
            {
                linkedIdTask = Task.FromResult(file.FormInfo.LinkedId);
                propertiesTask = Task.FromResult(file.FormInfo.Properties);
            }
            else
            {
                linkedIdTask = _daoFactory.GetLinkDao<T>().GetLinkedAsync(file.Id);
                propertiesTask = fileDao.GetProperties(file.Id);
            }
            
            var currentFolderTask = folderDao.GetFolderAsync(file.ParentId);
            await Task.WhenAll(linkedIdTask, propertiesTask, currentFolderTask);

            var linkedId = linkedIdTask.Result;
            var properties = propertiesTask.Result;
            var currentFolder = currentFolderTask.Result;

            Folder<T> currentRoom;
            if (!DocSpaceHelper.IsRoom(currentFolder.FolderType) && file.RootFolderType is FolderType.VirtualRooms or FolderType.Archive or FolderType.RoomTemplates)
            {
                currentRoom = await DocSpaceHelper.GetParentRoom(file, folderDao) ?? currentFolder;
            }
            else
            {
                currentRoom = currentFolder;
            }

            if (currentRoom is { FolderType: FolderType.FillingFormsRoom } && properties != null && properties.FormFilling.StartFilling)
            {
                    result.Security[FileSecurity.FilesSecurityActions.Lock] = false;
            }

            if (currentRoom.Security == null)
            {
                _ = await _fileSecurity.SetSecurity(new[] { currentRoom }.ToAsyncEnumerable()).ToListAsync();
            }

            if (!file.IsForm && (FilterType)file.Category == FilterType.None)
            {
                result.IsForm = await fileChecker.CheckExtendedPDF(file);
            }
            else
            {
                result.IsForm = file.IsForm;
            }

            if (DocSpaceHelper.IsFormsFillingSystemFolder(currentFolder.FolderType) || currentFolder.FolderType == FolderType.FillingFormsRoom)
            {
                result.Security[FileSecurity.FilesSecurityActions.Edit] = false;
            }

            result.HasDraft = result.IsForm == true ? !Equals(linkedId, default(T)) : null;

            if (currentRoom is { FolderType: FolderType.VirtualDataRoom })
            {
                var (currentStep, roles) = await fileDao.GetUserFormRoles(file.Id, authContext.CurrentAccount.ID);
                var roleList = await roles.ToListAsync();

                if (currentStep == -1 && result.Security[FileSecurity.FilesSecurityActions.Edit])
                {
                    result.FormFillingStatus = FormFillingStatus.Draft;
                }

                if (currentStep != -1)
                {
                    if (!DateTime.MinValue.Equals(properties.FormFilling.FillingStopedDate))
                    {
                        result.FormFillingStatus = FormFillingStatus.Stoped;
                    }
                    else if (currentStep == 0)
                    {
                        result.FormFillingStatus = FormFillingStatus.Complete;
                    }
                    else
                    {
                        var unsubmittedRole = roleList.FirstOrDefault(r => !r.Submitted);
                        switch (unsubmittedRole)
                        {
                            case not null:
                                result.FormFillingStatus = currentStep == unsubmittedRole.Sequence
                                    ? FormFillingStatus.YouTurn
                                    : FormFillingStatus.InProgress;
                                break;
                            default:
                                if (roleList.Count > 0)
                                {
                                    result.FormFillingStatus = FormFillingStatus.InProgress;
                                }
                                break;
                        }
                    }
                }
            }

            var formFilling = properties?.FormFilling;
            if (formFilling != null)
            {
                result.StartFilling = formFilling.StartFilling;
                if (!Equals(linkedId, default(T)))
                {
                    var draftLocation = new DraftLocation<T> { FolderId = formFilling.ToFolderId, FolderTitle = formFilling.Title, FileId = linkedId };
                    var draft = await fileDao.GetFileAsync(linkedId);
                    if (draft != null)
                    {
                        draftLocation.FileTitle = draft.Title;
                    }

                    result.DraftLocation = draftLocation;
                }
            }
        }

        result.FileExst = extension;
        result.FileType = fileType;
        result.Version = file.Version;
        result.VersionGroup = file.VersionGroup;
        result.ContentLength = file.ContentLengthString;
        result.FileStatus = await file.GetFileStatus();
        result.Mute = !isEnabledBadges;
        result.PureContentLength = file.ContentLength.NullIfDefault();
        result.Comment = file.Comment;
        result.Encrypted = file.Encrypted.NullIfDefault();
        result.Locked = file.Locked.NullIfDefault();
        result.LockedBy = file.LockedBy;
        result.Access = file.Access;
        result.LastOpened = _apiDateTimeHelper.Get(file.LastOpened);
        result.CustomFilterEnabled = file.CustomFilterEnabled.NullIfDefault();
        result.CustomFilterEnabledBy = file.CustomFilterEnabledBy;

        if (!file.ProviderEntry && file.RootFolderType == FolderType.VirtualRooms && !expiration.HasValue)
        {
            var folderDao = _daoFactory.GetCacheFolderDao<T>();
            var room = await DocSpaceHelper.GetParentRoom(file, folderDao);
            if (room?.SettingsLifetime != null)
            {
                expiration = DateTime.UtcNow - room.SettingsLifetime.GetExpirationUtc();
            }
        }

        if (expiration.HasValue && expiration.Value != TimeSpan.MaxValue)
        {
            var update = result.Updated;

            if (result.Version > 1)
            {
                var firstVersion = await fileDao.GetFileAsync(result.Id, 1);
                update = _apiDateTimeHelper.Get(firstVersion.ModifiedOn);
            }

            result.Expired = new ApiDateTime(update.UtcTime + expiration.Value, update.TimeZoneOffset);
        }

        if (file.Order != 0)
        {
            if (string.IsNullOrEmpty(order) && (contextFolder == null || !DocSpaceHelper.IsRoom(contextFolder.FolderType)))
            {
                order = await breadCrumbsManager.GetBreadCrumbsOrderAsync(file.ParentId);
            }
            
            result.Order = !string.IsNullOrEmpty(order) ? string.Join('.', order, file.Order) : file.Order.ToString();
        }

        try
        {
            var externalMediaAccess = file.ShareRecord is { SubjectType: SubjectType.PrimaryExternalLink or SubjectType.ExternalLink };
            
            if (externalMediaAccess)
            {
                result.RequestToken = await externalShare.CreateShareKeyAsync(file.ShareRecord.Subject);
            }
            
            result.ViewUrl = externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(file.DownloadUrl), result.RequestToken);

            result.WebUrl = externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, file.Title, file.Id, file.Version, externalMediaAccess)), result.RequestToken);
            result.ShortWebUrl = await urlShortener.GetShortenLinkAsync(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebEditorUrl(file.Id)));
            result.ThumbnailStatus = file.ThumbnailStatus;

            var cacheKey = Math.Abs(result.Updated.GetHashCode());

            if (file.ThumbnailStatus == Thumbnail.Created)
            {
                result.ThumbnailUrl = externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileThumbnailUrl(file.Id, file.Version)) + $"&hash={cacheKey}", result.RequestToken);
            }
        }
        catch (Exception)
        {
            //Don't catch anything here because of httpcontext
        }

        return result;
    }
}

public class DraftLocation<T>
{
    /// <summary>
    /// InProcess folder ID
    /// </summary> 
    public T FolderId { get; set; }

    /// <summary>
    /// InProcess folder title
    /// </summary>
    public string FolderTitle { get; set; }

    /// <summary>
    /// Draft ID
    /// </summary>
    public T FileId { get; set; }

    /// <summary>
    /// Draft title
    /// </summary>
    public string FileTitle { get; set; }
}