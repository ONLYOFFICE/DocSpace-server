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
    [SwaggerSchemaCustomInt("Folder ID", Example = 12334)]
    public T FolderId { get; set; }

    [SwaggerSchemaCustomInt("Version", Example = 3)]
    public int Version { get; set; }

    [SwaggerSchemaCustomInt("Version group", Example = 1)]
    public int VersionGroup { get; set; }

    [SwaggerSchemaCustom("Content length", Example = "12345")]
    public string ContentLength { get; set; }

    [SwaggerSchemaCustom("Pure content length")]
    public long? PureContentLength { get; set; }

    [SwaggerSchemaCustom("File status")]
    public FileStatus FileStatus { get; set; }

    [SwaggerSchemaCustom("Muted or not", Example = false)]
    public bool Mute { get; set; }

    [SwaggerSchemaCustom("URL to view a file", Example = "https://www.onlyoffice.com/viewfile?fileid=2221", Format = "uri")]
    public string ViewUrl { get; set; }

    [SwaggerSchemaCustom("Web URL", Format = "uri")]
    public string WebUrl { get; set; }

    [SwaggerSchemaCustom("File type")]
    public FileType FileType { get; set; }

    [SwaggerSchemaCustom("File extension", Example = ".txt")]
    public string FileExst { get; set; }

    [SwaggerSchemaCustom("Comment")]
    public string Comment { get; set; }

    [SwaggerSchemaCustom("Encrypted or not", Example = false)]
    public bool? Encrypted { get; set; }

    [SwaggerSchemaCustom("Thumbnail URL", Format = "uri")]
    public string ThumbnailUrl { get; set; }

    [SwaggerSchemaCustom("Thumbnail status")]
    public Thumbnail ThumbnailStatus { get; set; }

    [SwaggerSchemaCustom("Locked or not")]
    public bool? Locked { get; set; }

    [SwaggerSchemaCustom("User ID who locked a file")]
    public string LockedBy { get; set; }

    [SwaggerSchemaCustom("Denies file downloading or not", Example = false)]
    public bool DenyDownload { get; set; }

    [SwaggerSchemaCustom("Is there a draft or not", Example = false)]
    public bool? HasDraft { get; set; }

    [SwaggerSchemaCustom("Is there a form or not", Example = false)]
    public bool? IsForm { get; set; }

    [SwaggerSchemaCustom("Specifies if the filling has started or not", Example = false)]
    public bool? StartFilling { get; set; }

    [SwaggerSchemaCustom("InProcess folder ID")]
    public int? InProcessFolderId { get; set; }

    [SwaggerSchemaCustom("InProcess folder title")]
    public string InProcessFolderTitle { get; set; }

    [SwaggerSchemaCustom("Draft info")]
    public DraftLocation<T> DraftLocation { get; set; }

    [SwaggerSchemaCustom("Denies file sharing or not", Example = false)]
    public bool DenySharing { get; set; }

    [SwaggerSchemaCustom("File accessibility")]
    public IDictionary<Accessibility, bool> ViewAccessibility { get; set; }

    [SwaggerSchemaCustom("Available external rights")]
    public IDictionary<string, bool> AvailableExternalRights { get; set; }

    [SwaggerSchemaCustom("Request token")]
    public string RequestToken { get; set; }

    [SwaggerSchemaCustom(Description = "Last opened")]
    public ApiDateTime LastOpened { get; set; }
    
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
        FileSharing fileSharing,
        FileChecker fileChecker)
    : FileEntryDtoHelper(apiDateTimeHelper, employeeWrapperHelper, fileSharingHelper, fileSecurity, globalFolderHelper, filesSettingsHelper, fileDateTime) 
{
    private readonly ApiDateTimeHelper _apiDateTimeHelper = apiDateTimeHelper;

    public async Task<FileDto<T>> GetAsync<T>(File<T> file, int foldersCount = 0, string order = null)
    {
        var result = await GetFileWrapperAsync(file, foldersCount, order);

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
    private async Task<FileDto<T>> GetFileWrapperAsync<T>(File<T> file, int foldersCount, string order)
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
        result.DenyDownload = file.DenyDownload;
        result.DenySharing = file.DenySharing;
        result.Access = file.Access;
        result.LastOpened = _apiDateTimeHelper.Get(file.LastOpened);

        if (file.Order != 0)
        {
            file.Order += foldersCount;
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
    [SwaggerSchemaCustomInt("InProcess folder ID")]
    public T FolderId { get; set; }

    [SwaggerSchemaCustom("InProcess folder title")]
    public string FolderTitle { get; set; }

    [SwaggerSchemaCustomInt("Draft ID")]
    public T FileId { get; set; }

    [SwaggerSchemaCustom("Draft title")]
    public string FileTitle { get; set; }
}