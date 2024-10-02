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
    /// User ID who locked a file
    /// </summary>
    public string LockedBy { get; set; }

    /// <summary>
    /// Is there a draft or not
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool? HasDraft { get; set; }

    /// <summary>
    /// Is there a form or not
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool? IsForm { get; set; }

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
    /// Request token
    /// </summary>
    public string RequestToken { get; set; }

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
        FileSharing fileSharing,
        FileChecker fileChecker)
    : FileEntryDtoHelper(apiDateTimeHelper, employeeWrapperHelper, fileSharingHelper, fileSecurity, globalFolderHelper, filesSettingsHelper, fileDateTime) 
{
    private readonly ApiDateTimeHelper _apiDateTimeHelper = apiDateTimeHelper;

    public async Task<FileDto<T>> GetAsync<T>(File<T> file, string order = null, TimeSpan? expiration = null)
    {
        var result = await GetFileWrapperAsync(file, order, expiration);

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

    private Dictionary<string, AceWrapper> shareCache = new();
    private async Task<FileDto<T>> GetFileWrapperAsync<T>(File<T> file, string order, TimeSpan? expiration)
    {
        var result = await GetAsync<FileDto<T>, T>(file);
        var isEnabledBadges = await badgesSettingsHelper.GetEnabledForCurrentUserAsync();

        var extension = FileUtility.GetFileExtension(file.Title);
        var fileType = FileUtility.GetFileTypeByExtention(extension);

        if (fileType == FileType.Pdf)
        {
            var linkDao = daoFactory.GetLinkDao<T>();
            var folderDao = daoFactory.GetCacheFolderDao<T>();
            var fileDao = daoFactory.GetFileDao<T>();

            var linkedIdTask = linkDao.GetLinkedAsync(file.Id);
            var propertiesTask = fileDao.GetProperties(file.Id);
            var currentFolderTask = folderDao.GetFolderAsync(file.ParentId);
            await Task.WhenAll(linkedIdTask, propertiesTask, currentFolderTask);

            var linkedId = linkedIdTask.Result;
            var properties = propertiesTask.Result;
            var currentFolder = currentFolderTask.Result;

            Folder<T> currentRoom;
            if (!DocSpaceHelper.IsRoom(currentFolder.FolderType))
            {
                var (roomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(currentFolder);
                if (int.TryParse(roomId?.ToString(), out var curRoomId) && curRoomId != -1)
                {
                    currentRoom = await folderDao.GetFolderAsync(roomId);
                }
                else
                {
                    currentRoom = currentFolder;
                }
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

            AceWrapper ace = null;
            var currentRoomId = currentRoom.Id?.ToString();
            if (currentRoomId != null && !shareCache.TryGetValue(currentRoomId, out ace))
            {
                ace = await fileSharing.GetPureSharesAsync(currentRoom, [authContext.CurrentAccount.ID]).FirstOrDefaultAsync();
                shareCache.TryAdd(currentRoomId, ace);
            }

            if (!file.IsForm && (FilterType)file.Category == FilterType.None)
            {
                result.IsForm = await fileChecker.CheckExtendedPDF(file);
            }
            else
            {
                result.IsForm = file.IsForm;
            }

            if (ace is { Access: FileShare.FillForms } || result.IsForm == false || currentFolder.FolderType == FolderType.FormFillingFolderInProgress)
            {
                result.Security[FileSecurity.FilesSecurityActions.EditForm] = false;
            }

            result.HasDraft = result.IsForm == true ? !Equals(linkedId, default(T)) : null;

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

        if (file.RootFolderType == FolderType.VirtualRooms && !expiration.HasValue)
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var (roomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(file);
            var room = await folderDao.GetFolderAsync(roomId).NotFoundIfNull();
            if (room.SettingsLifetime != null)
            {
                expiration = DateTime.UtcNow - room.SettingsLifetime.GetExpirationUtc();
            }
        }

        if (expiration.HasValue && expiration.Value != TimeSpan.MaxValue)
        {
            result.Expired = new ApiDateTime(result.Updated.UtcTime + expiration.Value, result.Updated.TimeZoneOffset);
        }

        if (file.Order != 0)
        {
            if (string.IsNullOrEmpty(order))
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