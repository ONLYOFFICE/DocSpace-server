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

using ImageMagick;

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// A stored file as the calling account sees it: where it lives, which revision this is, how it can be opened and
/// what the portal is currently doing with it.
/// </summary>
public class FileDto<T> : FileEntryDto<T>
{
    /// <summary>
    /// The folder the file is stored in. When the file was reached through a share and the caller cannot open its
    /// real parent, the identifier of the Shared with me section is reported instead, so this is where the file is
    /// visible rather than where it physically sits.
    /// </summary>
    /// <example>10</example>
    public T FolderId { get; set; }

    /// <summary>
    /// The revision this entry describes. It starts at 1 and moves to the next number each time new content is stored
    /// over the file, except for an editing session opened against the file itself, which replaces the content and
    /// keeps the number. `GET api/2.0/files/file/{fileId}/history` lists them all.
    /// </summary>
    /// <example>3</example>
    public int Version { get; set; }

    /// <summary>
    /// Groups revisions that belong together, which is how a history can fold a long editing session into one entry:
    /// versions saved inside one session share this number, and an upload over the file starts a new group.
    /// </summary>
    /// <example>1</example>
    public int VersionGroup { get; set; }

    /// <summary>
    /// The size already formatted for display, with a unit and the separators of the caller's language. Read
    /// `pureContentLength` for a number to calculate with.
    /// </summary>
    /// <example>1.29 MB</example>
    public string ContentLength { get; set; }

    /// <summary>
    /// The size of the stored content in bytes, and null for an empty file.
    /// </summary>
    /// <example>1352001</example>
    public long? PureContentLength { get; set; }

    /// <summary>
    /// What the portal is currently doing with the file and how the caller stands towards it - open in the editor,
    /// unread, being converted, and so on. The value is a bit mask that combines those states, so a file can report a
    /// number that matches none of the published members on its own.
    /// </summary>
    /// <example>2</example>
    public FileStatus FileStatus { get; set; }

    /// <summary>
    /// The accounts that have the file open in the editor at this moment, as account identifier to display name, and
    /// empty when nobody has. The all-zero identifier stands for people who came in through an external link without
    /// signing in, and its name carries their number in brackets when there is more than one.
    /// </summary>
    /// <example>{"9a1e28c4-51f2-4f6b-b0a3-0c21e7f2a7d1": "John Doe"}</example>
    public Dictionary<Guid, string> EditingBy { get; set; }

    /// <summary>
    /// Not a property of the file at all: it repeats, inverted, the calling account's own switch for new-item badges,
    /// so it is the same in every entry of one answer. True means that account has badges turned off.
    /// </summary>
    /// <example>false</example>
    public bool Mute { get; set; }

    /// <summary>
    /// The address that returns the bytes of the file - a download, in spite of the name; `webUrl` is the address a
    /// person opens. When the file was reached through an external link the address carries the key of that link, so
    /// it keeps working without signing in.
    /// </summary>
    /// <example>https://example.com/filehandler.ashx?action=download&amp;fileid=2221</example>
    [Url]
    public string ViewUrl { get; set; }

    /// <summary>
    /// The page that opens the file in a browser: the editor for a format the portal edits, the media viewer for
    /// pictures, audio and video, and the download address for a format it cannot show at all.
    /// </summary>
    /// <example>https://example.com/doceditor?fileid=2221</example>
    [Url]
    public string WebUrl { get; set; }

    /// <summary>
    /// The broad kind of content, worked out from the extension, which is what a client uses to pick an icon or a
    /// viewer without parsing `fileExst` itself.
    /// </summary>
    /// <example>7</example>
    public FileType FileType { get; set; }

    /// <summary>
    /// The extension of the stored file, leading dot included and always lower case. For a format the portal keeps in
    /// a converted shape this is the extension it is served under, not the one it was uploaded with.
    /// </summary>
    /// <example>.docx</example>
    public string FileExst { get; set; }

    /// <summary>
    /// The note kept with this revision. The portal writes it itself for revisions it creates, an upload over an
    /// existing file among them, and an editor stores the note a person typed when saving a version.
    /// </summary>
    /// <example>Uploaded file</example>
    public string Comment { get; set; }

    /// <summary>
    /// True for a file in a private room, whose content the server never sees and which therefore cannot be converted
    /// or taken over by an upload. Null, rather than false, for an ordinary file.
    /// </summary>
    /// <example>false</example>
    public bool? Encrypted { get; set; }

    /// <summary>
    /// The address of the generated preview image. It is filled in only while `thumbnailStatus` says the preview has
    /// been created, and it carries a suffix that changes with the file, so an image cached for an earlier revision
    /// is not reused.
    /// </summary>
    /// <example>https://example.com/filehandler.ashx?action=thumb&amp;fileid=2221</example>
    [Url]
    public string ThumbnailUrl { get; set; }

    /// <summary>
    /// How far the preview image has got. Only the created state means `thumbnailUrl` holds an address; the others
    /// mean there is none, either because it is still being produced or because this format has no preview.
    /// </summary>
    /// <example>1</example>
    public Thumbnail ThumbnailStatus { get; set; }

    /// <summary>
    /// True while the file is held under a lock that stops anyone but its holder from editing it, and null rather
    /// than false when there is no lock. `lockedBy` names the holder unless the caller is the holder.
    /// </summary>
    /// <example>false</example>
    public bool? Locked { get; set; }

    /// <summary>
    /// The display name of the account holding the lock, and null when the caller holds it - so `locked` true
    /// together with no name here means the lock is the caller's own.
    /// </summary>
    /// <example>John Doe</example>
    public string LockedBy { get; set; }

    /// <summary>
    /// For a fillable PDF form, whether the caller already has a filling draft of it, in which case `draftLocation`
    /// says where that draft lives. Null for anything that is not a form.
    /// </summary>
    /// <example>false</example>
    public bool? HasDraft { get; set; }

    /// <summary>
    /// How far the filling of this form has got for the calling account, and whose turn it is now. It is worked out
    /// only inside a virtual data room, where filling runs in steps; everywhere else it stays at the none value.
    /// </summary>
    /// <example>3</example>
    public FormFillingStatus FormFillingStatus { get; set; } = FormFillingStatus.None;

    /// <summary>
    /// Whether the PDF is a fillable form rather than a plain document. When the stored classification does not say,
    /// the portal opens the file to find out, so the answer is reliable for a PDF and null for anything else.
    /// </summary>
    /// <example>true</example>
    public bool? IsForm { get; set; }

    /// <summary>
    /// True while a spreadsheet is in the mode where each person sorts and filters their own view without changing
    /// what the others see, and null rather than false when it is not.
    /// </summary>
    /// <example>false</example>
    public bool? CustomFilterEnabled { get; set; }

    /// <summary>
    /// The display name of the account that turned that mode on, and null when the caller turned it on themselves.
    /// </summary>
    /// <example>John Doe</example>
    public string CustomFilterEnabledBy { get; set; }

    /// <summary>
    /// For a form in a room for filling, whether it has been released for filling; until then it is still being
    /// prepared and only the people running the room work with it. Null for a file this does not apply to.
    /// </summary>
    /// <example>true</example>
    public bool? StartFilling { get; set; }

    /// <summary>
    /// True during the short window in which a released form is still being written out by the editor. Neither
    /// filling nor editing is accepted while it lasts, so a client should wait and read the file again.
    /// </summary>
    /// <example>false</example>
    public bool? IsFillingPreparing { get; set; }

    /// <summary>
    /// Left empty by the portal: the folder holding the caller's draft is reported in `draftLocation` instead.
    /// </summary>
    /// <example>10</example>
    public int? InProcessFolderId { get; set; }

    /// <summary>
    /// Left empty by the portal, like the identifier beside it; the draft's folder is named in `draftLocation`.
    /// </summary>
    /// <example>In Process</example>
    public string InProcessFolderTitle { get; set; }

    /// <summary>
    /// The folder that collects the completed copies of this form. It is filled in only for the original form of a
    /// room for filling, and only for a caller allowed to work with that form; null everywhere else.
    /// </summary>
    /// <example>55</example>
    public int? ResultsFolderId { get; set; }

    /// <summary>
    /// Where the caller's own filling draft of this form is kept. Null when there is no draft yet, which is the same
    /// thing `hasDraft` reports.
    /// </summary>
    /// <example>{"folderId": 10, "fileId": 123, "fileTitle": "John Doe - Application.pdf"}</example>
    public DraftLocation<T> DraftLocation { get; set; }

    /// <summary>
    /// Which ways of opening this format the portal supports at all - its own editor, the picture viewer, the media
    /// player and so on. It answers whether the format can be shown, not whether this account may do it; rights are
    /// reported in `security`.
    /// </summary>
    /// <example>{"WebView": true, "ImageView": false, "MediaView": false}</example>
    public IDictionary<Accessibility, bool> ViewAccessibility { get; set; }

    /// <summary>
    /// The moment the caller last opened the file. It is kept per account and is what orders the Recent section, so
    /// it is null for a file this account has never opened. Written with the offset of the portal's time zone.
    /// </summary>
    /// <example>2026-09-11T13:45:00+03:00</example>
    public ApiDateTime LastOpened { get; set; }

    /// <summary>
    /// The moment the file falls under the lifetime rule of the room holding it and is removed. It is counted from
    /// the first revision rather than the latest one, so editing a file does not postpone it, and it is null when the
    /// room sets no lifetime. Written with the offset of the portal's time zone.
    /// </summary>
    /// <example>2026-12-31T23:59:59+03:00</example>
    public ApiDateTime Expired { get; set; }

    /// <summary>
    /// Always the file value, which is what tells files from folders in a listing that mixes both.
    /// </summary>
    /// <example>2</example>
    public override FileEntryType FileEntryType => FileEntryType.File;

    /// <summary>
    /// How far the indexing of the file's content for AI search has got. It is null for a file that has never been
    /// queued for indexing, which is every file while the feature is off for the portal.
    /// </summary>
    /// <example>1</example>
    public VectorizationStatus? VectorizationStatus { get; set; }

    /// <summary>
    /// The table collecting the submitted values of this form in the external database configured for its room. The
    /// field is left out of the answer entirely when the form has no such table.
    /// </summary>
    /// <example>form_123_v1</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ExternalDbTableName { get; set; }

    /// <summary>
    /// The pixel size of the picture, measured by reading the stored file rather than taken from any stored metadata.
    /// Null for anything that is not a picture the portal can show, and also when the file could not be read.
    /// </summary>
    /// <example>{"width": 1920, "height": 1080}</example>
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
    FileHelper fileHelper,
    FilesSettingsHelper filesSettingsHelper,
    FileDateTime fileDateTime,
    ExternalShare externalShare,
    BreadCrumbsManager breadCrumbsManager,
    FileChecker fileChecker,
    SecurityContext securityContext,
    UserManager userManager,
    IUrlShortener urlShortener,
    FileSharing fileSharing,
    AiAccessibility aiAccessibility,
    FileTrackerHelper fileTracker)
    : FileEntryDtoHelper(apiDateTimeHelper, employeeWrapperHelper, fileSharingHelper, fileSecurity, globalFolderHelper, filesSettingsHelper, fileDateTime, securityContext, userManager, daoFactory, externalShare, fileSharing, urlShortener)
{
    private readonly EmployeeDtoHelper _employeeWrapperHelper = employeeWrapperHelper;

    public async Task<FileDto<T>> GetAsync<T>(File<T> file, string order = null, TimeSpan? expiration = null, IFolder contextFolder = null, AiStatus aiStatus = null)
    {
        var result = await GetFileWrapperAsync(file, order, expiration, contextFolder);

        result.ViewAccessibility = await fileUtility.GetAccessibility(file);
        result.AvailableShareRights =  (await _fileSecurity.GetAccesses(file)).ToDictionary(r => r.Key, r => r.Value.Select(v => v.ToStringFast()));
        result.VectorizationStatus = file.VectorizationStatus;
        aiStatus ??= await aiAccessibility.GetStatusAsync();

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
            result.OriginTitle = Equals(result.OriginId, myId) ? FilesUCResource.Files : result.OriginTitle;

            if (Equals(result.OriginRoomId, myId))
            {
                result.OriginRoomTitle = FilesUCResource.Files;
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
                // ignored
            }
        }

        if (aiStatus is { Enabled: false})
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
        var fileStateTask = fileHelper.GetFileState(file);

        var extension = FileUtility.GetFileExtension(file.Title);
        var fileType = FileUtility.GetFileTypeByExtention(extension);

        await Task.WhenAll(getFileTask, badgesTask, fileStateTask);

        var result = await getFileTask;
        var isEnabledBadges = await badgesTask;
        var fileState = await fileStateTask;

        file.SetFileState(fileState);

        result.FolderId = file.ParentId;
        result.FileExst = extension;
        result.FileType = fileType;
        result.Version = file.Version;
        result.VersionGroup = file.VersionGroup;
        result.ContentLength = file.ContentLengthString;
        result.FileStatus = file.FileStatus;
        result.EditingBy = file.EditingBy;
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

            var linkedId = await linkedIdTask;
            var properties = await propertiesTask;
            var currentFolder = await currentFolderTask;

            Folder<T> currentRoom;
            if (!currentFolder.IsRoom && file.RootFolderType is FolderType.VirtualRooms or FolderType.Archive or FolderType.RoomTemplates or FolderType.DefaultTemplates)
            {
                currentRoom = await DocSpaceHelper.GetParentRoom(file, folderDao) ?? currentFolder;
            }
            else
            {
                currentRoom = currentFolder;
            }

            if (currentRoom is { FolderType: FolderType.FillingFormsRoom }
                && properties is { FormFilling: not null }
                && currentFolder.FolderType is not (FolderType.FormFillingFolderInProgress or FolderType.FormFillingFolderDone))
            {
                if (properties.FormFilling.StartFilling)
                {
                    var isPreparing = await fileTracker.IsEditingAsync(file.Id);
                    result.IsFillingPreparing = isPreparing;

                    result.Security[FileSecurity.FilesSecurityActions.StartFilling] = false;
                    result.Security[FileSecurity.FilesSecurityActions.Lock] = false;

                    if (isPreparing)
                    {
                        result.Security[FileSecurity.FilesSecurityActions.FillForms] = false;
                        result.Security[FileSecurity.FilesSecurityActions.Edit] = false;
                        result.Security[FileSecurity.FilesSecurityActions.StopFilling] = false;
                    }
                }
                else
                {
                    result.Security[FileSecurity.FilesSecurityActions.FillForms] = false;
                    result.Security[FileSecurity.FilesSecurityActions.StopFilling] = false;
                }
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

            if (DocSpaceHelper.IsFormsFillingSystemFolder(currentFolder.FolderType))
            {
                result.Security[FileSecurity.FilesSecurityActions.Edit] = false;
            }

            result.HasDraft = result.IsForm == true ? !Equals(linkedId, default(T)) : null;

            var formFilling = properties?.FormFilling;
            if (formFilling != null)
            {
                result.StartFilling = formFilling.StartFilling;
                result.ExternalDbTableName = formFilling.ExternalDbTableName;
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

            var isOriginalForm = currentRoom is { FolderType: FolderType.FillingFormsRoom }
                && formFilling is { StartFilling: true }
                && Equals(file.Id, formFilling.OriginalFormId);

            result.Security[FileSecurity.FilesSecurityActions.UpdateXlsx] = isOriginalForm
                && (result.Security[FileSecurity.FilesSecurityActions.Edit] || file.Access == FileShare.ContentCreator);

            if (isOriginalForm && formFilling.ResultsFolderId is int resultsFolderId)
            {
                result.ResultsFolderId = resultsFolderId;
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
        else if (extension is ".xlsx" or ".csv" && file.RootFolderType == FolderType.VirtualRooms)
        {
            var xlsxProperties = await fileDao.GetProperties(file.Id);
            var xlsxFormFilling = xlsxProperties?.FormFilling;

            var canUpdateXlsx = false;
            if (xlsxFormFilling != null && !Equals(xlsxFormFilling.OriginalFormId, default(T)))
            {
                var xlsxFolder = await folderDao.GetFolderAsync(file.ParentId);
                if (xlsxFolder.FolderType == FolderType.FormFillingFolderDone)
                {
                    var originalForm = await fileDao.GetFileAsync(xlsxFormFilling.OriginalFormId);
                    canUpdateXlsx = originalForm != null && (await _fileSecurity.CanEditAsync(originalForm) || file.Access == FileShare.ContentCreator);
                }
            }

            result.Security[FileSecurity.FilesSecurityActions.UpdateXlsx] = canUpdateXlsx;
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
                var parent = parents.LastOrDefault();
                if (!await _fileSecurity.CanReadAsync(parent))
                {
                    result.FolderId = await _globalFolderHelper.GetFolderShareAsync<T>();
                    result.RootFolderType = FolderType.SHARE;
                }

                var room = parents.FirstOrDefault(f => f.IsRoom);
                if (room != null)
                {
                    result.OwnedBy = authContext.IsAuthenticated ? await _employeeWrapperHelper.GetAsync(room.CreateBy) : null;
                }
            }

            result.ViewUrl = _externalShare.GetUrlWithShare(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileDownloadUrl(file.Id)), result.RequestToken);
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
/// Where the caller's own filling draft of a form is kept.
/// </summary>
public class DraftLocation<T>
{
    /// <summary>
    /// The folder holding the draft: the sub-folder that the room for filling keeps for drafts of this particular
    /// form.
    /// </summary>
    /// <example>10</example>
    public T FolderId { get; set; }

    /// <summary>
    /// The title of that folder, which the portal takes from the form itself when the form is released for filling.
    /// </summary>
    /// <example>Application</example>
    public string FolderTitle { get; set; }

    /// <summary>
    /// The draft itself - the copy the caller fills in, not the original form, and the identifier to pass to the file
    /// operations while filling.
    /// </summary>
    /// <example>123</example>
    public T FileId { get; set; }

    /// <summary>
    /// The title of the draft, which the portal builds from the name of the person filling it and the name of the
    /// form. Null when the draft the record points at no longer exists.
    /// </summary>
    /// <example>John Doe - Application.pdf</example>
    public string FileTitle { get; set; }
}
