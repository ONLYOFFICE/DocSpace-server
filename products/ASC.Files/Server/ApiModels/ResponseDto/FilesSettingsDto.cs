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

using ASC.ElasticSearch.Core;
using ASC.Files.Core.Vectorization.Settings;

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// Everything a client needs to work with documents in this portal: the format tables, the address templates, the
/// upload limits, the portal-wide switches and the preferences of the calling account.
/// </summary>
public class FilesSettingsDto
{
    /// <summary>
    /// Images the portal can show in its own viewer. Anything outside the list has to be downloaded to be seen.
    /// </summary>
    /// <example>[".bmp", ".gif", ".jpeg", ".jpg", ".png", ".svg"]</example>
    public List<string> ExtsImagePreviewed { get; set; }

    /// <summary>
    /// Audio and video the portal can play in its own player.
    /// </summary>
    /// <example>[".mp4", ".webm", ".mp3", ".ogg"]</example>
    public List<string> ExtsMediaPreviewed { get; set; }

    /// <summary>
    /// Documents the editor can open read-only. A format that is here but not in the edited list can be viewed and
    /// not changed.
    /// </summary>
    /// <example>[".docx", ".xlsx", ".pptx", ".pdf"]</example>
    public List<string> ExtsWebPreviewed { get; set; }

    /// <summary>
    /// Documents the editor can open for editing. Uploading a format outside this list and outside the convertible
    /// list leaves a file that can only be downloaded.
    /// </summary>
    /// <example>[".docx", ".xlsx", ".pptx"]</example>
    public List<string> ExtsWebEdited { get; set; }

    /// <summary>
    /// Documents that can be edited inside a private room, where the content is encrypted on the client.
    /// </summary>
    /// <example>[".docx", ".xlsx", ".pptx"]</example>
    public List<string> ExtsWebEncrypt { get; set; }

    /// <summary>
    /// Documents that support the reviewing mode, so that granting review access to them is meaningful.
    /// </summary>
    /// <example>[".docx"]</example>
    public List<string> ExtsWebReviewed { get; set; }

    /// <summary>
    /// Spreadsheets that support the custom filter mode, where a filter applied by one editor does not disturb the
    /// others.
    /// </summary>
    /// <example>[".xlsx"]</example>
    public List<string> ExtsWebCustomFilterEditing { get; set; }

    /// <summary>
    /// Documents that can only be filled in or commented on rather than edited freely, whatever access the caller
    /// holds.
    /// </summary>
    /// <example>[".pdf"]</example>
    public List<string> ExtsWebRestrictedEditing { get; set; }

    /// <summary>
    /// Documents that support comments, so that granting comment access to them is meaningful.
    /// </summary>
    /// <example>[".docx"]</example>
    public List<string> ExtsWebCommented { get; set; }

    /// <summary>
    /// Documents the portal treats as templates to create new files from.
    /// </summary>
    /// <example>[".docx", ".xlsx", ".pptx"]</example>
    public List<string> ExtsWebTemplate { get; set; }

    /// <summary>
    /// Formats that cannot be edited as they are and are converted on upload or on first opening. Which target each
    /// one has is in the convertible table below.
    /// </summary>
    /// <example>[".doc", ".xls", ".ppt"]</example>
    public List<string> ExtsMustConvert { get; set; }

    /// <summary>
    /// The conversion map of the portal: for each source extension, the extensions it can be converted into. Use it
    /// to fill the target format of a conversion request instead of guessing one.
    /// </summary>
    /// <example>{".doc": [".docx", ".pdf"], ".xls": [".xlsx", ".pdf"]}</example>
    public IDictionary<string, List<string>> ExtsConvertible { get; set; }

    /// <summary>
    /// Formats the portal offers to create and upload as documents. It is not an upload filter: files of other
    /// formats are stored as they are.
    /// </summary>
    /// <example>[".docx", ".xlsx", ".pdf"]</example>
    public List<string> ExtsUploadable { get; set; }

    /// <summary>
    /// Formats recognised as archives, which is what decides the archive icon and the offer to unpack.
    /// </summary>
    /// <example>[".zip", ".rar", ".7z"]</example>
    public ImmutableList<string> ExtsArchive { get; set; }

    /// <summary>
    /// Formats classified as video. The classification lists drive icons and the media filters of the listing
    /// operations, and are wider than what the built-in player can show.
    /// </summary>
    /// <example>[".mp4", ".webm", ".avi"]</example>
    public ImmutableList<string> ExtsVideo { get; set; }

    /// <summary>
    /// Formats classified as audio.
    /// </summary>
    /// <example>[".mp3", ".ogg", ".wav"]</example>
    public ImmutableList<string> ExtsAudio { get; set; }

    /// <summary>
    /// Formats classified as images.
    /// </summary>
    /// <example>[".png", ".jpg", ".gif"]</example>
    public ImmutableList<string> ExtsImage { get; set; }

    /// <summary>
    /// Formats classified as spreadsheets.
    /// </summary>
    /// <example>[".xlsx", ".xls", ".ods"]</example>
    public ImmutableList<string> ExtsSpreadsheet { get; set; }

    /// <summary>
    /// Formats classified as presentations.
    /// </summary>
    /// <example>[".pptx", ".ppt", ".odp"]</example>
    public ImmutableList<string> ExtsPresentation { get; set; }

    /// <summary>
    /// Formats classified as text documents.
    /// </summary>
    /// <example>[".docx", ".doc", ".odt"]</example>
    public ImmutableList<string> ExtsDocument { get; set; }

    /// <summary>
    /// Formats classified as diagrams.
    /// </summary>
    /// <example>[".vsdx"]</example>
    public ImmutableList<string> ExtsDiagram { get; set; }

    /// <summary>
    /// The extension the portal creates for each kind of document, keyed by that kind. This is what a new empty
    /// document gets when no extension is asked for.
    /// </summary>
    /// <example>{"Document": ".docx", "Spreadsheet": ".xlsx", "Presentation": ".pptx"}</example>
    public Dictionary<FileType, string> InternalFormats { get; set; }

    /// <summary>
    /// The extension of a fillable form template in this portal. It is configurable, so read it rather than assuming
    /// the product default.
    /// </summary>
    /// <example>.pdf</example>
    public string MasterFormExtension { get; set; }

    /// <summary>
    /// The name of the query parameter that pins a document address to one version. Append it to the addresses below
    /// instead of composing a version address by hand.
    /// </summary>
    /// <example>version</example>
    public string ParamVersion { get; set; }

    /// <summary>
    /// The name of the query parameter that asks a download address for a converted copy in another format.
    /// </summary>
    /// <example>outputtype</example>
    public string ParamOutType { get; set; }

    /// <summary>
    /// The template of the address a file is downloaded from: substitute the file identifier for the `{0}`
    /// placeholder. Add the version and output-type parameters named above for a particular version or format.
    /// </summary>
    /// <example>https://example.com/filehandler.ashx?action=download&amp;fileid={0}</example>
    [Url]
    public string FileDownloadUrlString { get; set; }

    /// <summary>
    /// The template of the address that opens a file in the viewer inside the portal, with `{0}` for the file
    /// identifier. It is a portal-relative address, meant to be opened in a browser rather than called as an API.
    /// </summary>
    /// <example>/products/files/doceditor?fileid={0}&amp;action=view</example>
    public string FileWebViewerUrlString { get; set; }

    /// <summary>
    /// The same viewer address as an absolute one, for a message or a page outside the portal.
    /// </summary>
    /// <example>https://example.com/products/files/doceditor?fileid={0}&amp;action=view</example>
    [Url]
    public string FileWebViewerExternalUrlString { get; set; }

    /// <summary>
    /// The template of the address that opens a file for editing inside the portal, with `{0}` for the file
    /// identifier. Whether the session really becomes editable still depends on the access the caller holds.
    /// </summary>
    /// <example>/products/files/doceditor?fileid={0}&amp;action=edit</example>
    public string FileWebEditorUrlString { get; set; }

    /// <summary>
    /// The same editing address as an absolute one, for use outside the portal.
    /// </summary>
    /// <example>https://example.com/products/files/doceditor?fileid={0}&amp;action=edit</example>
    [Url]
    public string FileWebEditorExternalUrlString { get; set; }

    /// <summary>
    /// The template of the address that sends the browser on to whichever viewer or editor suits the file, with `{0}`
    /// for the file identifier. Use it when the kind of the file is not known in advance.
    /// </summary>
    /// <example>https://example.com/products/files/{0}</example>
    [Url]
    public string FileRedirectPreviewUrlString { get; set; }

    /// <summary>
    /// The template of the address a file thumbnail is fetched from, with `{0}` for the file identifier. A thumbnail
    /// is built in the background, so the address can answer with nothing for a while after the file appears.
    /// </summary>
    /// <example>https://example.com/filehandler.ashx?action=thumb&amp;fileid={0}</example>
    [Url]
    public string FileThumbnailUrlString { get; set; }

    /// <summary>
    /// Whether the caller asked to be prompted before a deletion. Written by `PUT api/2.0/files/changedeleteconfrim`.
    /// </summary>
    /// <example>true</example>
    public bool ConfirmDelete { get; set; }

    /// <summary>
    /// Whether this portal allows third-party storages to be connected at all. It is set portal-wide by an
    /// administrator, so a member sees it as read-only.
    /// </summary>
    /// <example>true</example>
    public bool EnableThirdParty { get; set; }

    /// <summary>
    /// Whether links that open an entry without a portal account may be created in this portal. Set portal-wide by an
    /// administrator.
    /// </summary>
    /// <example>true</example>
    public bool ExternalShare { get; set; }

    /// <summary>
    /// Whether the share-to-network buttons are offered next to an external link. It is reported as false whenever
    /// external sharing itself is off.
    /// </summary>
    /// <example>true</example>
    public bool ExternalShareSocialMedia { get; set; }

    /// <summary>
    /// Whether the caller's uploads keep the original file when the portal converts them. With false the conversion
    /// replaces the uploaded file with a new version of it.
    /// </summary>
    /// <example>true</example>
    public bool StoreOriginalFiles { get; set; }

    /// <summary>
    /// Whether the caller asked for new documents to be created with the default name instead of being prompted for
    /// one.
    /// </summary>
    /// <example>false</example>
    public bool KeepNewFileName { get; set; }

    /// <summary>
    /// Whether the caller asked to see extensions in file titles. Stored titles always carry the extension whatever
    /// this says.
    /// </summary>
    /// <example>true</example>
    public bool DisplayFileExtension { get; set; }

    /// <summary>
    /// Whether the caller is told about the result of a conversion. There is no operation in this document that
    /// writes it.
    /// </summary>
    /// <example>true</example>
    public bool ConvertNotify { get; set; }

    /// <summary>
    /// Whether the prompt shown before a running operation is abandoned is hidden for the caller.
    /// </summary>
    /// <example>false</example>
    public bool HideConfirmCancelOperation { get; set; }

    /// <summary>
    /// Whether the prompt that offers to keep a copy in the original format on conversion is hidden for the caller.
    /// Once true it cannot be turned back through the API.
    /// </summary>
    /// <example>false</example>
    public bool HideConfirmConvertSave { get; set; }

    /// <summary>
    /// Whether the prompt that offers to open the conversion result is hidden for the caller. Once true it cannot be
    /// turned back through the API.
    /// </summary>
    /// <example>false</example>
    public bool HideConfirmConvertOpen { get; set; }

    /// <summary>
    /// Whether the warning shown before the lifetime settings of a room are changed is hidden for the caller.
    /// </summary>
    /// <example>false</example>
    public bool HideConfirmRoomLifetime { get; set; }

    /// <summary>
    /// The ordering the listing operations fall back to when a request names none. It follows the last order the
    /// caller asked a listing for, so it changes on its own as the account is used.
    /// </summary>
    /// <example>{"sortedBy": "DateAndTime", "isAsc": false}</example>
    public OrderBy DefaultOrder { get; set; }

    /// <summary>
    /// Whether the editor writes a document back to storage while the session is still open. It is on for every
    /// portal and cannot be switched off.
    /// </summary>
    /// <example>true</example>
    public bool Forcesave { get; set; }

    /// <summary>
    /// Whether those intermediate saves are kept as separate versions. They are not, in any portal: they update the
    /// current version instead.
    /// </summary>
    /// <example>false</example>
    public bool StoreForcesave { get; set; }

    /// <summary>
    /// Whether the "Recent" section is offered to the caller among the section roots.
    /// </summary>
    /// <example>true</example>
    public bool RecentSection { get; set; }

    /// <summary>
    /// Whether the "Favorites" section is offered to the caller among the section roots.
    /// </summary>
    /// <example>true</example>
    public bool FavoritesSection { get; set; }

    /// <summary>
    /// Whether the "Templates" section is offered to the caller among the section roots.
    /// </summary>
    /// <example>true</example>
    public bool TemplatesSection { get; set; }

    /// <summary>
    /// The archive format the caller's multi-item downloads are packed into: true for `.tar.gz`, false for `.zip`.
    /// </summary>
    /// <example>true</example>
    public bool DownloadTarGz { get; set; }

    /// <summary>
    /// The trash auto-clearing setting of the caller, the same pair `GET api/2.0/files/settings/autocleanup` returns.
    /// </summary>
    /// <example>{"isAutoCleanUp": true, "gap": 3}</example>
    public AutoCleanUpData AutomaticallyCleanUp { get; set; }

    /// <summary>
    /// Whether documents in this portal can be searched by what is inside them and not only by title. It depends on
    /// the full-text search service being configured and having indexed the portal.
    /// </summary>
    /// <example>true</example>
    public bool CanSearchByContent { get; set; }

    /// <summary>
    /// The access rights the sharing dialog offers the caller by default. The portal normalises the set it stores, so
    /// this can be shorter than what was last sent.
    /// </summary>
    /// <example>[1, 2]</example>
    public List<FileShare> DefaultSharingAccessRights { get; set; }

    /// <summary>
    /// How many upload requests the portal accepts from one account at a time. Sending more than this in parallel
    /// gets the extra ones refused rather than queued.
    /// </summary>
    /// <example>10</example>
    public int MaxUploadThreadCount { get; set; }

    /// <summary>
    /// The size in bytes of one chunk of a chunked upload. Split a large file exactly along this size: a chunk that
    /// does not match is refused by the upload session.
    /// </summary>
    /// <example>10485760</example>
    public long ChunkUploadSize { get; set; }

    /// <summary>
    /// Whether the caller asked for documents to open in the current browser tab.
    /// </summary>
    /// <example>false</example>
    public bool OpenEditorInSameTab { get; set; }

    /// <summary>
    /// Whether the caller asked to see rooms arranged by the groups they belong to.
    /// </summary>
    /// <example>true</example>
    public bool OrganizeRoomsGrouping { get; set; }

    /// <summary>
    /// The kind of external link this portal offers first: true for a link only its own accounts can open, false for
    /// one anyone holding it can open.
    /// </summary>
    /// <example>false</example>
    public bool DefaultShareLinkInternal { get; set; }

    /// <summary>
    /// Whether the external sharing restriction covers personal documents. It matters only while external sharing is
    /// off.
    /// </summary>
    /// <example>true</example>
    public bool ExternalShareApplyToDocuments { get; set; }

    /// <summary>
    /// Whether the external sharing restriction covers rooms, including making a new one public. It matters only
    /// while external sharing is off.
    /// </summary>
    /// <example>true</example>
    public bool ExternalShareApplyToRooms { get; set; }

    /// <summary>
    /// Whether links created before the restriction stop opening as well, rather than only new ones being refused.
    /// </summary>
    /// <example>true</example>
    public bool BlockExistingLinksOnRestrict { get; set; }

    /// <summary>
    /// Formats whose content can be indexed for the AI features of the portal. A file outside the list is left out of
    /// that index.
    /// </summary>
    /// <example>[".docx", ".pdf", ".txt"]</example>
    public List<string> ExtsFilesVectorized { get; set; }

    /// <summary>
    /// The largest file size in bytes that is indexed for the AI features. A larger file is skipped even when its
    /// format is listed above.
    /// </summary>
    /// <example>5242880</example>
    public long MaxVectorizationFileSize { get; set; }
}


[Scope]
public class FilesSettingsDtoConverter(
    FileUtility fileUtility,
    FilesLinkUtility filesLinkUtility,
    FilesSettingsHelper filesSettingsHelper,
    SetupInfo setupInfo,
    VectorizationGlobalSettings vectorizationGlobalSettings,
    SearchSettingsHelper searchSettingsHelper)
{
    public async Task<FilesSettingsDto> Get()
    {
        return new FilesSettingsDto
        {
            ExtsImagePreviewed = fileUtility.ExtsImagePreviewed,
            ExtsMediaPreviewed = fileUtility.ExtsMediaPreviewed,
            ExtsWebPreviewed = fileUtility.ExtsWebPreviewed,
            ExtsWebEdited = fileUtility.ExtsWebEdited,
            ExtsWebEncrypt = fileUtility.ExtsWebEncrypt,
            ExtsWebReviewed = fileUtility.ExtsWebReviewed,
            ExtsWebCustomFilterEditing = fileUtility.ExtsWebCustomFilterEditing,
            ExtsWebRestrictedEditing = fileUtility.ExtsWebRestrictedEditing,
            ExtsWebCommented = fileUtility.ExtsWebCommented,
            ExtsWebTemplate = fileUtility.ExtsWebTemplate,
            ExtsMustConvert = fileUtility.ExtsMustConvert,
            ExtsConvertible = await fileUtility.GetExtsConvertibleAsync(),
            ExtsUploadable = fileUtility.ExtsUploadable,
            ExtsArchive = FileUtility.ExtsArchive,
            ExtsVideo = FileUtility.ExtsVideo,
            ExtsAudio = FileUtility.ExtsAudio,
            ExtsImage = FileUtility.ExtsImage,
            ExtsSpreadsheet = FileUtility.ExtsSpreadsheet,
            ExtsPresentation = FileUtility.ExtsPresentation,
            ExtsDocument = FileUtility.ExtsDocument,
            ExtsDiagram = FileUtility.ExtsDiagram,
            InternalFormats = fileUtility.InternalExtension,
            MasterFormExtension = fileUtility.MasterFormExtension,
            ParamVersion = FilesLinkUtility.Version,
            ParamOutType = FilesLinkUtility.OutType,
            FileDownloadUrlString = filesLinkUtility.FileDownloadUrlString,
            FileWebViewerUrlString = filesLinkUtility.FileWebViewerUrlString,
            FileWebViewerExternalUrlString = filesLinkUtility.FileWebViewerExternalUrlString,
            FileWebEditorUrlString = filesLinkUtility.FileWebEditorUrlString,
            FileWebEditorExternalUrlString = filesLinkUtility.FileWebEditorExternalUrlString,
            FileRedirectPreviewUrlString = filesLinkUtility.FileRedirectPreviewUrlString,
            FileThumbnailUrlString = filesLinkUtility.FileThumbnailUrlString,
            ConfirmDelete = await filesSettingsHelper.GetConfirmDelete(),
            EnableThirdParty = await filesSettingsHelper.GetEnableThirdParty(),
            ExternalShare = await filesSettingsHelper.GetExternalShare(),
            ExternalShareSocialMedia = await filesSettingsHelper.GetExternalShareSocialMedia(),
            StoreOriginalFiles = await filesSettingsHelper.GetStoreOriginalFiles(),
            KeepNewFileName = await filesSettingsHelper.GetKeepNewFileName(),
            DisplayFileExtension = await filesSettingsHelper.GetDisplayFileExtension(),
            HideConfirmCancelOperation = await filesSettingsHelper.GetHideConfirmCancelOperation(),
            HideConfirmConvertSave = await filesSettingsHelper.GetHideConfirmConvertSave(),
            HideConfirmConvertOpen = await filesSettingsHelper.GetHideConfirmConvertOpen(),
            HideConfirmRoomLifetime = await filesSettingsHelper.GetHideConfirmRoomLifetime(),
            DefaultOrder = await filesSettingsHelper.GetDefaultOrder(),
            Forcesave = filesSettingsHelper.GetForcesave(),
            StoreForcesave = filesSettingsHelper.GetStoreForcesave(),
            RecentSection = await filesSettingsHelper.GetRecentSection(),
            FavoritesSection = await filesSettingsHelper.GetFavoritesSection(),
            TemplatesSection = await filesSettingsHelper.GetTemplatesSection(),
            DownloadTarGz = await filesSettingsHelper.GetDownloadTarGz(),
            AutomaticallyCleanUp = await filesSettingsHelper.GetAutomaticallyCleanUp(),
            CanSearchByContent = await searchSettingsHelper.CanSearchByContentAsync<DbFile>(),
            DefaultSharingAccessRights = await filesSettingsHelper.GetDefaultSharingAccessRights(),
            MaxUploadThreadCount = setupInfo.MaxUploadThreadCount,
            ChunkUploadSize = setupInfo.ChunkUploadSize,
            OpenEditorInSameTab = await filesSettingsHelper.GetOpenEditorInSameTabAsync(),
            OrganizeRoomsGrouping = await filesSettingsHelper.GetOrganizeRoomsGroupingAsync(),
            DefaultShareLinkInternal = await filesSettingsHelper.GetDefaultShareLinkInternal(),
            ExternalShareApplyToDocuments = await filesSettingsHelper.GetExternalShareApplyToDocuments(),
            ExternalShareApplyToRooms = await filesSettingsHelper.GetExternalShareApplyToRooms(),
            BlockExistingLinksOnRestrict = await filesSettingsHelper.GetBlockExistingLinksOnRestrict(),
            ExtsFilesVectorized = vectorizationGlobalSettings.SupportedFormats.ToList(),
            MaxVectorizationFileSize = vectorizationGlobalSettings.MaxContentLength
        };
    }
}
