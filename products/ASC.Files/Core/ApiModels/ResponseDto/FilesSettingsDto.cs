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

/// <summary>
/// The file settings parameters.
/// </summary>
public class FilesSettingsDto
{
    /// <summary>
    /// The list of extensions of the viewed images.
    /// </summary>
    public List<string> ExtsImagePreviewed { get; set; }

    /// <summary>
    /// The list of extensions of the viewed media files.
    /// </summary>
    public List<string> ExtsMediaPreviewed { get; set; }

    /// <summary>
    /// The list of extensions of the viewed files.
    /// </summary>
    public List<string> ExtsWebPreviewed { get; set; }

    /// <summary>
    /// The list of extensions of the edited files.
    /// </summary>
    public List<string> ExtsWebEdited { get; set; }

    /// <summary>
    /// The list of extensions of the encrypted files.
    /// </summary>
    public List<string> ExtsWebEncrypt { get; set; }

    /// <summary>
    /// The list of extensions of the reviewed files.
    /// </summary>
    public List<string> ExtsWebReviewed { get; set; }

    /// <summary>
    /// The list of extensions of the custom filter files.
    /// </summary>
    public List<string> ExtsWebCustomFilterEditing { get; set; }

    /// <summary>
    /// The list of extensions of the files that are restricted for editing.
    /// </summary>
    public List<string> ExtsWebRestrictedEditing { get; set; }

    /// <summary>
    /// The list of extensions of the commented files.
    /// </summary>
    public List<string> ExtsWebCommented { get; set; }

    /// <summary>
    /// The list of extensions of the template files.
    /// </summary>
    public List<string> ExtsWebTemplate { get; set; }

    /// <summary>
    /// The list of extensions of the files that must be converted.
    /// </summary>
    public List<string> ExtsMustConvert { get; set; }

    /// <summary>
    /// The list of the convertible extensions.
    /// </summary>
    public IDictionary<string, List<string>> ExtsConvertible { get; set; }

    /// <summary>
    /// The list of the uploadable extensions.
    /// </summary>
    public List<string> ExtsUploadable { get; set; }

    /// <summary>
    /// The list of extensions of the archive files.
    /// </summary>
    public ImmutableList<string> ExtsArchive { get; set; }

    /// <summary>
    /// The list of the video extensions.
    /// </summary>
    public ImmutableList<string> ExtsVideo { get; set; }

    /// <summary>
    /// The list of the audio extensions.
    /// </summary>
    public ImmutableList<string> ExtsAudio { get; set; }

    /// <summary>
    /// The list of the image extensions.
    /// </summary>
    public ImmutableList<string> ExtsImage { get; set; }

    /// <summary>
    /// The list of the spreadsheet extensions.
    /// </summary>
    public ImmutableList<string> ExtsSpreadsheet { get; set; }

    /// <summary>
    /// The list of the presentation extensions.
    /// </summary>
    public ImmutableList<string> ExtsPresentation { get; set; }

    /// <summary>
    /// The list of the text document extensions. 
    /// </summary>
    public ImmutableList<string> ExtsDocument { get; set; }

    /// <summary>
    /// The list of the diagram extensions.
    /// </summary>
    public ImmutableList<string> ExtsDiagram { get; set; }

    /// <summary>
    /// The internal file formats.
    /// </summary>
    public Dictionary<FileType, string> InternalFormats { get; set; }

    /// <summary>
    /// The master form extension.
    /// </summary>
    public string MasterFormExtension { get; set; }

    /// <summary>
    /// The URL parameter which specifies the file version.
    /// </summary>
    public string ParamVersion { get; set; }

    /// <summary>
    /// The URL parameter which specifies the output type of the converted file.
    /// </summary>
    public string ParamOutType { get; set; }

    /// <summary>
    /// The URL to download a file.
    /// </summary>
    [Url]
    public string FileDownloadUrlString { get; set; }

    /// <summary>
    /// The URL to the file web viewer.
    /// </summary>
    public string FileWebViewerUrlString { get; set; }

    /// <summary>
    /// The external URL to the file web viewer.
    /// </summary>
    [Url]
    public string FileWebViewerExternalUrlString { get; set; }

    /// <summary>
    /// The URL to the file web editor.
    /// </summary>
    public string FileWebEditorUrlString { get; set; }

    /// <summary>
    /// The external URL to the file web editor.
    /// </summary>
    [Url]
    public string FileWebEditorExternalUrlString { get; set; }

    /// <summary>
    /// The redirect URL to the file viewer.
    /// </summary>
    [Url]
    public string FileRedirectPreviewUrlString { get; set; }

    /// <summary>
    /// The URL to the file thumbnail.
    /// </summary>
    [Url]
    public string FileThumbnailUrlString { get; set; }

    /// <summary>
    /// Specifies whether to confirm the file deletion or not.
    /// </summary>
    public bool ConfirmDelete { get; set; }

    /// <summary>
    /// Specifies whether to allow users to connect the third-party storages.
    /// </summary>
    public bool EnableThirdParty { get; set; }

    /// <summary>
    /// Specifies whether to enable sharing external links to the files.
    /// </summary>
    public bool ExternalShare { get; set; }

    /// <summary>
    /// Specifies whether to enable sharing files on social media.
    /// </summary>
    public bool ExternalShareSocialMedia { get; set; }

    /// <summary>
    /// Specifies whether to enable storing original files.
    /// </summary>
    public bool StoreOriginalFiles { get; set; }

    /// <summary>
    /// Specifies whether to keep the new file name.
    /// </summary>
    public bool KeepNewFileName { get; set; }

    /// <summary>
    /// Specifies whether to display the file extension.
    /// </summary>
    public bool DisplayFileExtension { get; set; }

    /// <summary>
    /// Specifies whether to display the conversion notification.
    /// </summary>    
    public bool ConvertNotify { get; set; }

    /// <summary>
    /// Specifies whether to hide the confirmation dialog for the cancel operation.
    /// </summary>
    public bool HideConfirmCancelOperation { get; set; }

    /// <summary>
    /// Specifies whether to hide the confirmation dialog
    /// for saving the file copy in the original format when converting a file.
    /// </summary>
    public bool HideConfirmConvertSave { get; set; }

    /// <summary>
    /// Specifies whether to hide the confirmation dialog
    /// for opening the conversion result.
    /// </summary>
    public bool HideConfirmConvertOpen { get; set; }

    /// <summary>
    /// Specifies whether to hide the confirmation dialog about the file lifetime in the room.
    /// </summary>
    public bool HideConfirmRoomLifetime { get; set; }

    /// <summary>
    /// The default order of files.
    /// </summary>
    public OrderBy DefaultOrder { get; set; }

    /// <summary>
    /// Specifies whether to forcesave the files or not.
    /// </summary>
    public bool Forcesave { get; set; }

    /// <summary>
    /// Specifies whether to store the forcesaved file versions or not.
    /// </summary>
    public bool StoreForcesave { get; set; }

    /// <summary>
    /// Specifies if the "Recent" section is displayed or not.
    /// </summary>
    public bool RecentSection { get; set; }

    /// <summary>
    /// Specifies if the "Favorites" section is displayed or not.
    /// </summary>
    public bool FavoritesSection { get; set; }

    /// <summary>
    /// Specifies if the "Templates" section is displayed or not.
    /// </summary>
    public bool TemplatesSection { get; set; }

    /// <summary>
    /// Specifies whether to download the .tar.gz files or not.
    /// </summary>
    public bool DownloadTarGz { get; set; }

    /// <summary>
    /// The auto-clearing setting parameters.
    /// </summary>
    public AutoCleanUpData AutomaticallyCleanUp { get; set; }

    /// <summary>
    /// Specifies whether the file can be searched by its content or not.
    /// </summary>
    public bool CanSearchByContent { get; set; }

    /// <summary>
    /// The default access rights in sharing settings.
    /// </summary>
    public List<FileShare> DefaultSharingAccessRights { get; set; }

    /// <summary>
    /// The maximum number of upload threads.
    /// </summary>    
    public int MaxUploadThreadCount { get; set; }

    /// <summary>
    /// The size of a large file that is uploaded in chunks.
    /// </summary>    
    public long ChunkUploadSize { get; set; }

    /// <summary>
    /// Specifies whether to open the editor in the same tab or not.
    /// </summary>
    public bool OpenEditorInSameTab { get; set; }
    
    /// <summary>
    /// List of extensions available for vectorization
    /// </summary>
    public List<string> ExtsFilesVectorized { get; set; }
    
    /// <summary>
    /// The maximum file size for vectorization
    /// </summary>
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