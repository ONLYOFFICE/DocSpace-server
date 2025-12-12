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

using ImageMagick;

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The file parameters.
/// </summary>
public class FileDto<T> : FileEntryDto<T>
{
    /// <summary>
    /// The folder ID where the file is located.
    /// </summary>
    public T FolderId { get; set; }

    /// <summary>
    /// The file version.
    /// </summary>
    [SwaggerSchemaCustom(Example = 3)]
    public int Version { get; set; }

    /// <summary>
    /// The version group of the file.
    /// </summary>
    [SwaggerSchemaCustom(Example = 1)]
    public int VersionGroup { get; set; }

    /// <summary>
    /// The content length of the file.
    /// </summary>
    [SwaggerSchemaCustom(Example = "12345")]
    public string ContentLength { get; set; }

    /// <summary>
    /// The pure content length of the file.
    /// </summary>
    public long? PureContentLength { get; set; }

    /// <summary>
    /// The current status of the file.
    /// </summary>
    public FileStatus FileStatus { get; set; }

    /// <summary>
    /// Specifies if the file is muted or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool Mute { get; set; }

    /// <summary>
    /// The URL link to view the file.
    /// </summary>
    [SwaggerSchemaCustom(Example = "https://www.onlyoffice.com/viewfile?fileid=2221")]
    [Url]
    public string ViewUrl { get; set; }

    /// <summary>
    /// The Web URL link to the file.
    /// </summary>
    [Url]
    public string WebUrl { get; set; }

    /// <summary>
    /// The file type.
    /// </summary>
    public FileType FileType { get; set; }

    /// <summary>
    /// The file extension.
    /// </summary>
    [SwaggerSchemaCustom(Example = ".txt")]
    public string FileExst { get; set; }

    /// <summary>
    /// The comment to the file.
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// Specifies if the file is encrypted or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool? Encrypted { get; set; }

    /// <summary>
    /// The thumbnail URL of the file.
    /// </summary>
    [Url]
    public string ThumbnailUrl { get; set; }

    /// <summary>
    /// The current thumbnail status of the file.
    /// </summary>
    public Thumbnail ThumbnailStatus { get; set; }

    /// <summary>
    /// Specifies if the file is locked or not.
    /// </summary>
    public bool? Locked { get; set; }

    /// <summary>
    /// The user ID of the person who locked the file.
    /// </summary>
    public string LockedBy { get; set; }

    /// <summary>
    /// Specifies if the file has a draft or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool? HasDraft { get; set; }

    /// <summary>
    /// The status of the form filling process.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public FormFillingStatus FormFillingStatus { get; set; } = FormFillingStatus.None;

    /// <summary>
    /// Specifies if the file is a form or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool? IsForm { get; set; }

    /// <summary>
    /// Specifies if the Custom Filter editing mode is enabled for a file or not.
    /// </summary>
    public bool? CustomFilterEnabled { get; set; }

    /// <summary>
    /// The name of the user who enabled a Custom Filter editing mode for a file.
    /// </summary>
    public string CustomFilterEnabledBy { get; set; }

    /// <summary>
    /// Specifies if the filling has started or not.
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool? StartFilling { get; set; }

    /// <summary>
    /// The InProcess folder ID of the file.
    /// </summary>
    public int? InProcessFolderId { get; set; }

    /// <summary>
    /// The InProcess folder title of the file.
    /// </summary>
    public string InProcessFolderTitle { get; set; }

    /// <summary>
    /// The file draft information with its location.
    /// </summary>
    public DraftLocation<T> DraftLocation { get; set; }

    /// <summary>
    /// The file accessibility.
    /// </summary>
    public IDictionary<Accessibility, bool> ViewAccessibility { get; set; }

    /// <summary>
    /// The time when the file was last opened.
    /// </summary>
    public ApiDateTime LastOpened { get; set; }

    /// <summary>
    /// The date when the file will be expired.
    /// </summary>
    public ApiDateTime Expired { get; set; }
    
    /// <summary>
    /// The file entry type.
    /// </summary>
    public override FileEntryType FileEntryType => FileEntryType.File;

    /// <summary>
    /// The vectorization status of the file.
    /// </summary>
    public VectorizationStatus? VectorizationStatus { get; set; }
    
    public Size Dimensions { get; set; }
}

[Scope]
public class FileDtoHelper(
    IHttpContextAccessor httpContextAccessor,
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
    IUrlShortener urlShortener,
    AiAccessibility aiAccessibility)
    : FileEntryDtoHelper(apiDateTimeHelper, employeeWrapperHelper, fileSharingHelper, fileSecurity, globalFolderHelper, filesSettingsHelper, fileDateTime, securityContext, userManager, daoFactory, externalShare, urlShortener) 
{
    private readonly EmployeeDtoHelper _employeeWrapperHelper = employeeWrapperHelper;

    public async Task<FileDto<T>> GetAsync<T>(File<T> file, string order = null, TimeSpan? expiration = null, IFolder contextFolder = null, bool? aiReady = null)
    {
        Task<bool> aiReadyTask = null;
        if (aiReady == null)
        {
            aiReadyTask = aiAccessibility.IsAiEnabledAsync();
        }
        
        var result = await GetFileWrapperAsync(file, order, expiration, contextFolder);
        
        result.ViewAccessibility = await fileUtility.GetAccessibility(file);
        result.AvailableShareRights =  (await _fileSecurity.GetAccesses(file)).ToDictionary(r => r.Key, r => r.Value.Select(v => v.ToStringFast()));
        result.VectorizationStatus = file.VectorizationStatus;
        
        if (contextFolder == null)
        {
            var referer = httpContextAccessor.HttpContext?.Request.Headers.Referer.FirstOrDefault();
            if (referer != null)
            {
                var uri = new Uri(referer);
                var query = HttpUtility.ParseQueryString(uri.Query);
                var folderId = query["folder"];
                if (!string.IsNullOrEmpty(folderId))
                {
                    var shareId = await _globalFolderHelper.GetFolderShareAsync<string>();
                    if (folderId == "@share")
                    {
                        folderId = shareId;
                    }

                    if (int.TryParse(folderId, out var fId))
                    {
                        var internalFolderDao = _daoFactory.GetCacheFolderDao<int>();
                        var folder = await internalFolderDao.GetFolderAsync(fId);

                        if (folder.RootFolderType == FolderType.USER && authContext.IsAuthenticated && !Equals(folder.RootCreateBy, authContext.CurrentAccount.ID))
                        {
                            folder = await internalFolderDao.GetFolderAsync(fId);
                        }

                        contextFolder = folder;
                    }
                }
            }
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
                FileSecurity.FilesSecurityActions.FillingStatus,
                FileSecurity.FilesSecurityActions.Vectorization,
                FileSecurity.FilesSecurityActions.Rename
            };

            foreach (var action in forbiddenActions)
            {
                result.Security[action] = false;   
            }

            result.Locked = false;
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
        
        var currentUserId = authContext.CurrentAccount.ID;
        if (file.RootFolderType == FolderType.USER && authContext.IsAuthenticated && !Equals(file.RootCreateBy, currentUserId))
        {                   
            switch (contextFolder)
            {
                case { FolderType: FolderType.Favorites }:
                case { FolderType: FolderType.Recent }:
                case { FolderType: FolderType.SHARE }:
                case { RootFolderType: FolderType.USER } when !Equals(contextFolder.RootCreateBy, currentUserId):
                case null:
                    var folderShareAsync = await _globalFolderHelper.GetFolderShareAsync<T>();
                    result.RootFolderType = FolderType.SHARE;
                    result.RootFolderId = folderShareAsync;
                    var parent = await _daoFactory.GetCacheFolderDao<T>().GetFolderAsync(result.FolderId);

                    if (!parent.SecurityByUsers.TryGetValue(currentUserId, out _))
                    {
                        parent.SecurityByUsers.Add(currentUserId, new Dictionary<FileSecurity.FilesSecurityActions, bool>());
                    }

                    if (!parent.SecurityByUsers[currentUserId].TryGetValue(FileSecurity.FilesSecurityActions.Read, out var canReadParent))
                    {
                        canReadParent = await _fileSecurity.CanReadAsync(parent);
                        parent.SecurityByUsers[currentUserId][FileSecurity.FilesSecurityActions.Read] = canReadParent;
                    }
                    
                    if (!canReadParent)
                    {
                        result.FolderId = folderShareAsync;
                    }

                    if (contextFolder is {FolderType: FolderType.Recent}  or { FolderType: FolderType.Favorites })
                    {
                        result.OriginRoomTitle = FilesUCResource.SharedForMe;
                    }
                    
                    break;
            }
        }
        
        if (fileUtility.CanImageView(file.PureTitle))
        {
            try
            {
                await using var stream = await _daoFactory.GetFileDao<T>().GetFileStreamAsync(file);
                using var image = new MagickImage();
                image.Ping(stream);
                result.Dimensions = new Size
                {
                    Height = image.Height,
                    Width = image.Width
                };
            }
            catch (Exception)
            {
            }

        }
        
        if (aiReadyTask != null)
        {
            aiReady = await aiReadyTask;
        }
        
        if (aiReady is false)
        {
            if (result.Security.ContainsKey(FileSecurity.FilesSecurityActions.AskAi))
            {
                result.Security[FileSecurity.FilesSecurityActions.AskAi] = false;
            }
        }

        return result;
    }

    private async Task<FileDto<T>> GetFileWrapperAsync<T>(File<T> file, string order, TimeSpan? expiration, IFolder contextFolder = null)
    {
        var fileDao = _daoFactory.GetFileDao<T>();
        var folderDao = _daoFactory.GetCacheFolderDao<T>();

        var getFileTask = GetAsync<FileDto<T>, T>(file);
        var badgesTask = badgesSettingsHelper.GetEnabledForCurrentUserAsync();
        var fileStatusTask = file.GetFileStatus();

        var extension = FileUtility.GetFileExtension(file.Title);
        var fileType = FileUtility.GetFileTypeByExtention(extension);

        await Task.WhenAll(getFileTask, badgesTask, fileStatusTask);

        var result = getFileTask.Result;
        var isEnabledBadges = badgesTask.Result;
        result.FolderId = file.ParentId;
        result.FileExst = extension;
        result.FileType = fileType;
        result.Version = file.Version;
        result.VersionGroup = file.VersionGroup;
        result.ContentLength = file.ContentLengthString;
        result.FileStatus = fileStatusTask.Result;
        result.Mute = !isEnabledBadges;
        result.PureContentLength = file.ContentLength.NullIfDefault();
        result.Comment = file.Comment;
        result.Encrypted = file.Encrypted.NullIfDefault();
        result.IsFavorite = file.IsFavorite.NullIfDefault();
        result.Locked = file.Locked.NullIfDefault();
        result.LockedBy = file.LockedBy;
        result.Access = file.Access;
        result.LastOpened = _apiDateTimeHelper.Get(file.LastOpened);
        result.CustomFilterEnabled = file.CustomFilterEnabled.NullIfDefault();
        result.CustomFilterEnabledBy = file.CustomFilterEnabledBy;

        if (fileType == FileType.Pdf)
        {
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
            if (!currentFolder.IsRoom && file.RootFolderType is FolderType.VirtualRooms or FolderType.Archive or FolderType.RoomTemplates)
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

            result.IsForm = file.IsForm;
            if (fileType == FileType.Pdf && !file.IsForm && (FilterType)file.Category == FilterType.None)
            {
                result.IsForm = await fileChecker.IsFormPDFFile(file);
            }

            if (DocSpaceHelper.IsFormsFillingSystemFolder(currentFolder.FolderType) || currentFolder.FolderType == FolderType.FillingFormsRoom)
            {
                result.Security[FileSecurity.FilesSecurityActions.Edit] = false;
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

            if (currentRoom is { FolderType: FolderType.VirtualDataRoom })
            {
                var (currentStep, roleList) = await fileDao.GetUserFormRoles(file.Id, authContext.CurrentAccount.ID);
                if (currentStep == -1 && result.Security[FileSecurity.FilesSecurityActions.Edit] && properties is { CopyToFillOut: true })
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
                                if (roleList.Count > 0 || properties.FormFilling.StartedByUserId.Equals(authContext.CurrentAccount.ID))
                                {
                                    result.FormFillingStatus = FormFillingStatus.InProgress;
                                }
                                break;
                        }
                    }
                    try
                    {
                        result.ShortWebUrl = await _urlShortener.GetShortenLinkAsync(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebEditorUrl(file.Id)));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        if (!file.ProviderEntry && file.RootFolderType == FolderType.VirtualRooms && !expiration.HasValue)
        {
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
            if (string.IsNullOrEmpty(order) && contextFolder is not { IsRoom: true })
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
                result.IsLinkExpired = file.ShareRecord.Options?.IsExpired;
                result.RequestToken = await _externalShare.CreateShareKeyAsync(file.ShareRecord.Subject);
                result.External = Equals(file.ShareRecord.EntryId, file.Id);
                
                var expirationDate = file.ShareRecord?.Options?.ExpirationDate;
                if (expirationDate != null && expirationDate != DateTime.MinValue)
                {
                    result.ExpirationDate = _apiDateTimeHelper.Get(expirationDate);
                }
                
                var parents = await folderDao.GetParentFoldersAsync(result.FolderId).ToListAsync();
                var parent = parents.Count >= 2 ? parents[^2] : null;
                if (!await _fileSecurity.CanReadAsync(parent))
                {
                    result.FolderId = await _globalFolderHelper.GetFolderShareAsync<T>();
                    result.RootFolderType = FolderType.SHARE;
                }
            
                var room = parents.FirstOrDefault(f => f.IsRoom);
                if (room != null)
                {
                    result.OwnedBy = await _employeeWrapperHelper.GetAsync(room.CreateBy);
                }
            }
            
            result.ViewUrl = _externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(file.DownloadUrl), result.RequestToken);
            result.WebUrl = _externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, file.Title, file.Id, file.Version, externalMediaAccess)), result.RequestToken);
            result.ThumbnailStatus = file.ThumbnailStatus;
            
            var cacheKey = Math.Abs(result.Updated.GetHashCode());

            if (file.ThumbnailStatus == Thumbnail.Created)
            {
                result.ThumbnailUrl = _externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileThumbnailUrl(file.Id, file.Version)) + $"&hash={cacheKey}", result.RequestToken);
            }
        }
        catch (Exception)
        {
            //Don't catch anything here because of httpcontext
        }

        return result;
    }
}

/// <summary>
/// The file draft parameters.
/// </summary>
public class DraftLocation<T>
{
    /// <summary>
    /// The InProcess folder ID of the draft.
    /// </summary> 
    public T FolderId { get; set; }

    /// <summary>
    /// The InProcess folder title of the draft.
    /// </summary>
    public string FolderTitle { get; set; }

    /// <summary>
    /// The draft ID.
    /// </summary>
    public T FileId { get; set; }

    /// <summary>
    /// The draft title.
    /// </summary>
    public string FileTitle { get; set; }
}