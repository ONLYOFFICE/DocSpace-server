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
/// The file settings parameters.
/// </summary>
public class FilesSettingsDto
{
    /// <summary>
    /// The list of extensions of the viewed images.
    /// </summary>
    /// <example>[".bmp", ".gif", ".jpeg", ".jpg", ".png", ".svg"]</example>
    public List<string> ExtsImagePreviewed { get; set; }

    /// <summary>
    /// The list of extensions of the viewed media files.
    /// </summary>
    /// <example>[".mp4", ".webm", ".mp3", ".ogg"]</example>
    public List<string> ExtsMediaPreviewed { get; set; }

    /// <summary>
    /// The list of extensions of the viewed files.
    /// </summary>
    /// <example>[".docx", ".xlsx", ".pptx", ".pdf"]</example>
    public List<string> ExtsWebPreviewed { get; set; }

    /// <summary>
    /// The list of extensions of the edited files.
    /// </summary>
    /// <example>[".docx", ".xlsx", ".pptx"]</example>
    public List<string> ExtsWebEdited { get; set; }

    /// <summary>
    /// The list of extensions of the encrypted files.
    /// </summary>
    /// <example>[".docx", ".xlsx", ".pptx"]</example>
    public List<string> ExtsWebEncrypt { get; set; }

    /// <summary>
    /// The list of extensions of the reviewed files.
    /// </summary>
    /// <example>[".docx"]</example>
    public List<string> ExtsWebReviewed { get; set; }

    /// <summary>
    /// The list of extensions of the custom filter files.
    /// </summary>
    /// <example>[".xlsx"]</example>
    public List<string> ExtsWebCustomFilterEditing { get; set; }

    /// <summary>
    /// The list of extensions of the files that are restricted for editing.
    /// </summary>
    /// <example>[".pdf"]</example>
    public List<string> ExtsWebRestrictedEditing { get; set; }

    /// <summary>
    /// The list of extensions of the commented files.
    /// </summary>
    /// <example>[".docx"]</example>
    public List<string> ExtsWebCommented { get; set; }

    /// <summary>
    /// The list of extensions of the template files.
    /// </summary>
    /// <example>[".docx", ".xlsx", ".pptx"]</example>
    public List<string> ExtsWebTemplate { get; set; }

    /// <summary>
    /// The list of extensions of the files that must be converted.
    /// </summary>
    /// <example>[".doc", ".xls", ".ppt"]</example>
    public List<string> ExtsMustConvert { get; set; }

    /// <summary>
    /// The list of the convertible extensions.
    /// </summary>
    /// <example>null</example>
    public IDictionary<string, List<string>> ExtsConvertible { get; set; }

    /// <summary>
    /// The list of the uploadable extensions.
    /// </summary>
    /// <example>[".docx", ".xlsx", ".pdf"]</example>
    public List<string> ExtsUploadable { get; set; }

    /// <summary>
    /// The list of extensions of the archive files.
    /// </summary>
    /// <example>[".zip", ".rar", ".7z"]</example>
    public ImmutableList<string> ExtsArchive { get; set; }

    /// <summary>
    /// The list of the video extensions.
    /// </summary>
    /// <example>[".mp4", ".webm", ".avi"]</example>
    public ImmutableList<string> ExtsVideo { get; set; }

    /// <summary>
    /// The list of the audio extensions.
    /// </summary>
    /// <example>[".mp3", ".ogg", ".wav"]</example>
    public ImmutableList<string> ExtsAudio { get; set; }

    /// <summary>
    /// The list of the image extensions.
    /// </summary>
    /// <example>[".png", ".jpg", ".gif"]</example>
    public ImmutableList<string> ExtsImage { get; set; }

    /// <summary>
    /// The list of the spreadsheet extensions.
    /// </summary>
    /// <example>[".xlsx", ".xls", ".ods"]</example>
    public ImmutableList<string> ExtsSpreadsheet { get; set; }

    /// <summary>
    /// The list of the presentation extensions.
    /// </summary>
    /// <example>[".pptx", ".ppt", ".odp"]</example>
    public ImmutableList<string> ExtsPresentation { get; set; }

    /// <summary>
    /// The list of the text document extensions.
    /// </summary>
    /// <example>[".docx", ".doc", ".odt"]</example>
    public ImmutableList<string> ExtsDocument { get; set; }

    /// <summary>
    /// The list of the diagram extensions.
    /// </summary>
    /// <example>[".vsdx"]</example>
    public ImmutableList<string> ExtsDiagram { get; set; }

    /// <summary>
    /// The internal file formats.
    /// </summary>
    /// <example>null</example>
    public Dictionary<FileType, string> InternalFormats { get; set; }

    /// <summary>
    /// The master form extension.
    /// </summary>
    /// <example>.docxf</example>
    public string MasterFormExtension { get; set; }

    /// <summary>
    /// The URL parameter which specifies the file version.
    /// </summary>
    /// <example>ver</example>
    public string ParamVersion { get; set; }

    /// <summary>
    /// The URL parameter which specifies the output type of the converted file.
    /// </summary>
    /// <example>otype</example>
    public string ParamOutType { get; set; }

    /// <summary>
    /// The URL to download a file.
    /// </summary>
    /// <example>https://example.com/products/files/httphandlers/filehandler.ashx?action=download&amp;fileid={0}</example>
    [Url]
    public string FileDownloadUrlString { get; set; }

    /// <summary>
    /// The URL to the file web viewer.
    /// </summary>
    /// <example>/products/files/doceditor?fileid={0}&amp;action=view</example>
    public string FileWebViewerUrlString { get; set; }

    /// <summary>
    /// The external URL to the file web viewer.
    /// </summary>
    /// <example>https://example.com/products/files/doceditor?fileid={0}&amp;action=view</example>
    [Url]
    public string FileWebViewerExternalUrlString { get; set; }

    /// <summary>
    /// The URL to the file web editor.
    /// </summary>
    /// <example>/products/files/doceditor?fileid={0}&amp;action=edit</example>
    public string FileWebEditorUrlString { get; set; }

    /// <summary>
    /// The external URL to the file web editor.
    /// </summary>
    /// <example>https://example.com/products/files/doceditor?fileid={0}&amp;action=edit</example>
    [Url]
    public string FileWebEditorExternalUrlString { get; set; }

    /// <summary>
    /// The redirect URL to the file viewer.
    /// </summary>
    /// <example>https://example.com/products/files/{0}</example>
    [Url]
    public string FileRedirectPreviewUrlString { get; set; }

    /// <summary>
    /// The URL to the file thumbnail.
    /// </summary>
    /// <example>https://example.com/products/files/httphandlers/filehandler.ashx?action=thumb&amp;fileid={0}</example>
    [Url]
    public string FileThumbnailUrlString { get; set; }

    /// <summary>
    /// Specifies whether to confirm the file deletion or not.
    /// </summary>
    /// <example>true</example>
    public bool ConfirmDelete { get; set; }

    /// <summary>
    /// Specifies whether to allow users to connect the third-party storages.
    /// </summary>
    /// <example>true</example>
    public bool EnableThirdParty { get; set; }

    /// <summary>
    /// Specifies whether to enable sharing external links to the files.
    /// </summary>
    /// <example>true</example>
    public bool ExternalShare { get; set; }

    /// <summary>
    /// Specifies whether to enable sharing files on social media.
    /// </summary>
    /// <example>true</example>
    public bool ExternalShareSocialMedia { get; set; }

    /// <summary>
    /// Specifies whether to enable storing original files.
    /// </summary>
    /// <example>true</example>
    public bool StoreOriginalFiles { get; set; }

    /// <summary>
    /// Specifies whether to keep the new file name.
    /// </summary>
    /// <example>false</example>
    public bool KeepNewFileName { get; set; }

    /// <summary>
    /// Specifies whether to display the file extension.
    /// </summary>
    /// <example>true</example>
    public bool DisplayFileExtension { get; set; }

    /// <summary>
    /// Specifies whether to display the conversion notification.
    /// </summary>
    /// <example>true</example>
    public bool ConvertNotify { get; set; }

    /// <summary>
    /// Specifies whether to hide the confirmation dialog for the cancel operation.
    /// </summary>
    /// <example>false</example>
    public bool HideConfirmCancelOperation { get; set; }

    /// <summary>
    /// Specifies whether to hide the confirmation dialog
    /// for saving the file copy in the original format when converting a file.
    /// </summary>
    /// <example>false</example>
    public bool HideConfirmConvertSave { get; set; }

    /// <summary>
    /// Specifies whether to hide the confirmation dialog
    /// for opening the conversion result.
    /// </summary>
    /// <example>false</example>
    public bool HideConfirmConvertOpen { get; set; }

    /// <summary>
    /// Specifies whether to hide the confirmation dialog about the file lifetime in the room.
    /// </summary>
    /// <example>false</example>
    public bool HideConfirmRoomLifetime { get; set; }

    /// <summary>
    /// The default order of files.
    /// </summary>
    /// <example>null</example>
    public OrderBy DefaultOrder { get; set; }

    /// <summary>
    /// Specifies whether to forcesave the files or not.
    /// </summary>
    /// <example>false</example>
    public bool Forcesave { get; set; }

    /// <summary>
    /// Specifies whether to store the forcesaved file versions or not.
    /// </summary>
    /// <example>false</example>
    public bool StoreForcesave { get; set; }

    /// <summary>
    /// Specifies if the "Recent" section is displayed or not.
    /// </summary>
    /// <example>true</example>
    public bool RecentSection { get; set; }

    /// <summary>
    /// Specifies if the "Favorites" section is displayed or not.
    /// </summary>
    /// <example>true</example>
    public bool FavoritesSection { get; set; }

    /// <summary>
    /// Specifies if the "Templates" section is displayed or not.
    /// </summary>
    /// <example>true</example>
    public bool TemplatesSection { get; set; }

    /// <summary>
    /// Specifies whether to download the .tar.gz files or not.
    /// </summary>
    /// <example>true</example>
    public bool DownloadTarGz { get; set; }

    /// <summary>
    /// The auto-clearing setting parameters.
    /// </summary>
    /// <example>null</example>
    public AutoCleanUpData AutomaticallyCleanUp { get; set; }

    /// <summary>
    /// Specifies whether the file can be searched by its content or not.
    /// </summary>
    /// <example>true</example>
    public bool CanSearchByContent { get; set; }

    /// <summary>
    /// The default access rights in sharing settings.
    /// </summary>
    /// <example>[]</example>
    public List<FileShare> DefaultSharingAccessRights { get; set; }

    /// <summary>
    /// The maximum number of upload threads.
    /// </summary>
    /// <example>10</example>
    public int MaxUploadThreadCount { get; set; }

    /// <summary>
    /// The size of a large file that is uploaded in chunks.
    /// </summary>
    /// <example>10485760</example>
    public long ChunkUploadSize { get; set; }

    /// <summary>
    /// Specifies whether to open the editor in the same tab or not.
    /// </summary>
    /// <example>false</example>
    public bool OpenEditorInSameTab { get; set; }
    
    /// <summary>
    /// List of extensions available for vectorization
    /// </summary>
    /// <example>[".docx", ".pdf", ".txt"]</example>
    public List<string> ExtsFilesVectorized { get; set; }
    
    /// <summary>
    /// The maximum file size for vectorization
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
            ExtsFilesVectorized = vectorizationGlobalSettings.SupportedFormats.ToList(),
            MaxVectorizationFileSize = vectorizationGlobalSettings.MaxContentLength
        };
    }
}