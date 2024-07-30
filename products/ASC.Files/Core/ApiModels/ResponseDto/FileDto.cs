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
    [SwaggerSchemaCustom(Example = "12334", Description = "Folder ID")]
    public T FolderId { get; set; }

    [SwaggerSchemaCustom(Example = "3", Description = "Version", Format = "int32")]
    public int Version { get; set; }

    [SwaggerSchemaCustom(Example = "1", Description = "Version group", Format = "int32")]
    public int VersionGroup { get; set; }

    [SwaggerSchemaCustom(Example = "1234", Description = "Content length")]
    public string ContentLength { get; set; }

    [SwaggerSchemaCustom(Example = "1234", Description = "Pure content length", Format = "int64", Nullable = true)]
    public long? PureContentLength { get; set; }

    [SwaggerSchemaCustom(Example = "None", Description = "File status")]
    public FileStatus FileStatus { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Muted or not")]
    public bool Mute { get; set; }

    [SwaggerSchemaCustom(Example = "https://www.onlyoffice.com/viewfile?fileid=2221", Description = "URL to view a file", Format = "uri")]
    public string ViewUrl { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Web URL", Format = "uri")]
    public string WebUrl { get; set; }

    [SwaggerSchemaCustom(Example = "Document", Description = "File type")]
    public FileType FileType { get; set; }

    [SwaggerSchemaCustom(Example = ".txt", Description = "File extension")]
    public string FileExst { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Comment")]
    public string Comment { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Encrypted or not", Nullable = true)]
    public bool? Encrypted { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Thumbnail URL", Format = "uri")]
    public string ThumbnailUrl { get; set; }

    [SwaggerSchemaCustom(Example = "Created", Description = "Thumbnail status")]
    public Thumbnail ThumbnailStatus { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Locked or not", Nullable = true)]
    public bool? Locked { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "User ID who locked a file")]
    public string LockedBy { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Denies file downloading or not")]
    public bool DenyDownload { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Is there a draft or not", Nullable = true)]
    public bool? HasDraft { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Is there a form or not", Nullable = true)]
    public bool? IsForm { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Specifies if the filling has started or not", Nullable = true)]
    public bool? StartFilling { get; set; }

    [SwaggerSchemaCustom(Example = "1234", Description = "InProcess folder ID", Format = "int32", Nullable = true)]
    public int? InProcessFolderId { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "InProcess folder title")]
    public string InProcessFolderTitle { get; set; }

    [SwaggerSchemaCustom(Description = "Draft info")]
    public DraftLocation<T> DraftLocation { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Denies file sharing or not")]
    public bool DenySharing { get; set; }

    [SwaggerSchemaCustom(Description = "File accessibility")]
    public IDictionary<Accessibility, bool> ViewAccessibility { get; set; }

    [SwaggerSchemaCustom(Description = "Available external rights")]
    public IDictionary<string, bool> AvailableExternalRights { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Request token")]
    public string RequestToken { get; set; }

    [SwaggerSchemaCustom(Example = "2008-04-10T06-30-00.000Z", Description = "Last opened")]
    public ApiDateTime LastOpened { get; set; }


    public static FileDto<int> GetSample()
    {
        return new FileDto<int>
        {
            Access = FileShare.ReadWrite,
            //Updated = ApiDateTime.GetSample(),
            //Created = ApiDateTime.GetSample(),
            //CreatedBy = EmployeeWraper.GetSample(),
            Id = 10,
            RootFolderType = FolderType.BUNCH,
            Shared = false,
            Title = "Some titile.txt",
            FileExst = ".txt",
            FileType = FileType.Document,
            //UpdatedBy = EmployeeWraper.GetSample(),
            ContentLength = 12345.ToString(CultureInfo.InvariantCulture),
            FileStatus = FileStatus.IsNew,
            FolderId = 12334,
            Version = 3,
            VersionGroup = 1,
            ViewUrl = "https://www.onlyoffice.com/viewfile?fileid=2221"
        };
    }
}

[Scope]
public class FileDtoHelper(ApiDateTimeHelper apiDateTimeHelper,
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

    private async Task<FileDto<T>> GetFileWrapperAsync<T>(File<T> file, int foldersCount, string order)
    {
        var result = await GetAsync<FileDto<T>, T>(file);
        var isEnabledBadges = await badgesSettingsHelper.GetEnabledForCurrentUserAsync();

        var extension = FileUtility.GetFileExtension(file.Title);
        var fileType = FileUtility.GetFileTypeByExtention(extension);

        if (fileType == FileType.Pdf)
        {
            var linkDao = daoFactory.GetLinkDao<T>();
            var folderDao = daoFactory.GetFolderDao<T>();

            var linkedIdTask = linkDao.GetLinkedAsync(file.Id);
            var propertiesTask = daoFactory.GetFileDao<T>().GetProperties(file.Id);
            var currentFolderTask = folderDao.GetFolderAsync((T)Convert.ChangeType(file.ParentId, typeof(T)));
            await Task.WhenAll(linkedIdTask, propertiesTask, currentFolderTask);

            var linkedId = await linkedIdTask;
            var properties = await propertiesTask;
            var currentFolder = await currentFolderTask;

            Folder<T> currentRoom = null;
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
            if (currentRoom != null && properties != null)
            {
                if (currentRoom.FolderType == FolderType.FillingFormsRoom && properties.FormFilling.StartFilling)
                    result.Security[FileSecurity.FilesSecurityActions.Lock] = false;
            }

            var ace = await fileSharing.GetPureSharesAsync(currentRoom, new List<Guid> { authContext.CurrentAccount.ID }).FirstOrDefaultAsync();

            if (!file.IsForm && (FilterType)file.Category == FilterType.None)
            {
                result.IsForm = await fileChecker.CheckExtendedPDF(file);
            }
            else
            {
                result.IsForm = file.IsForm;
            }

            if (ace is { Access: FileShare.FillForms } || 
                result.IsForm == null || result.IsForm == false || 
                currentFolder.FolderType == FolderType.FormFillingFolderInProgress)
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
                    var draftLocation = new DraftLocation<T>()
                    {
                        FolderId = formFilling.ToFolderId,
                        FolderTitle = formFilling.Title,
                        FileId = (T)Convert.ChangeType(linkedId, typeof(T))
                    };

                    var fileDao = daoFactory.GetFileDao<T>();
                    var draft = await fileDao.GetFileAsync((T)Convert.ChangeType(linkedId, typeof(T)));
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

            result.WebUrl = externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, file.Title, file.Id, file.Version, externalMediaAccess)),
                result.RequestToken);

            result.ThumbnailStatus = file.ThumbnailStatus;

            var cacheKey = Math.Abs(result.Updated.GetHashCode());

            if (file.ThumbnailStatus == Thumbnail.Created)
            {
                result.ThumbnailUrl =
                    externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileThumbnailUrl(file.Id, file.Version)) +
                                                              $"&hash={cacheKey}", result.RequestToken);
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
    [SwaggerSchemaCustom(Example = "1234", Description = "InProcess folder ID", Format = "int32")]
    public T FolderId { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "InProcess folder title")]
    public string FolderTitle { get; set; }

    [SwaggerSchemaCustom(Example = "1234", Description = "Draft ID", Format = "int32")]
    public T FileId { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Draft title")]
    public string FileTitle { get; set; }
}
