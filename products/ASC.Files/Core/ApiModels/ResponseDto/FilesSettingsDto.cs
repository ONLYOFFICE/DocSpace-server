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

public class FilesSettingsDto
{
    /// <summary>
    /// Exts image previewed
    /// </summary>
    [OpenApiDescription("Exts image previewed")]
    public List<string> ExtsImagePreviewed { get; set; }

    /// <summary>
    /// Exts media previewed
    /// </summary>
    [OpenApiDescription("Exts media previewed")]
    public List<string> ExtsMediaPreviewed { get; set; }

    /// <summary>
    /// Exts web previewed
    /// </summary>
    [OpenApiDescription("Exts web previewed")]
    public List<string> ExtsWebPreviewed { get; set; }

    /// <summary>
    /// Exts web edited
    /// </summary>
    [OpenApiDescription("Exts web edited")]
    public List<string> ExtsWebEdited { get; set; }

    /// <summary>
    /// Exts web encrypt
    /// </summary>
    [OpenApiDescription("Exts web encrypt")]
    public List<string> ExtsWebEncrypt { get; set; }

    /// <summary>
    /// Exts web reviewed
    /// </summary>
    [OpenApiDescription("Exts web reviewed")]
    public List<string> ExtsWebReviewed { get; set; }

    /// <summary>
    /// Exts web custom filter editing
    /// </summary>
    [OpenApiDescription("Exts web custom filter editing")]
    public List<string> ExtsWebCustomFilterEditing { get; set; }

    /// <summary>
    /// Exts web restricted editing
    /// </summary>
    [OpenApiDescription("Exts web restricted editing")]
    public List<string> ExtsWebRestrictedEditing { get; set; }

    /// <summary>
    /// Exts web commented
    /// </summary>
    [OpenApiDescription("Exts web commented")]
    public List<string> ExtsWebCommented { get; set; }

    /// <summary>
    /// Exts web template
    /// </summary>
    [OpenApiDescription("Exts web template")]
    public List<string> ExtsWebTemplate { get; set; }

    /// <summary>
    /// Exts co authoring
    /// </summary>
    [OpenApiDescription("Exts co authoring")]
    public List<string> ExtsCoAuthoring { get; set; }

    /// <summary>
    /// Exts must convert
    /// </summary>
    [OpenApiDescription("Exts must convert")]
    public List<string> ExtsMustConvert { get; set; }

    /// <summary>
    /// Exts convertible
    /// </summary>
    [OpenApiDescription("Exts convertible")]
    public IDictionary<string, List<string>> ExtsConvertible { get; set; }

    /// <summary>
    /// Exts uploadable
    /// </summary>
    [OpenApiDescription("Exts uploadable")]
    public List<string> ExtsUploadable { get; set; }

    /// <summary>
    /// Exts archive
    /// </summary>
    [OpenApiDescription("Exts archive")]
    public ImmutableList<string> ExtsArchive { get; set; }

    /// <summary>
    /// Exts video
    /// </summary>
    [OpenApiDescription("Exts video")]
    public ImmutableList<string> ExtsVideo { get; set; }

    /// <summary>
    /// Exts audio
    /// </summary>
    [OpenApiDescription("Exts audio")]
    public ImmutableList<string> ExtsAudio { get; set; }

    /// <summary>
    /// Exts image
    /// </summary>
    [OpenApiDescription("Exts image")]
    public ImmutableList<string> ExtsImage { get; set; }

    /// <summary>
    /// Exts spreadsheet
    /// </summary>
    [OpenApiDescription("Exts spreadsheet")]
    public ImmutableList<string> ExtsSpreadsheet { get; set; }

    /// <summary>
    /// Exts presentation
    /// </summary>
    [OpenApiDescription("Exts presentation")]
    public ImmutableList<string> ExtsPresentation { get; set; }

    /// <summary>
    /// Exts document
    /// </summary>
    [OpenApiDescription("Exts document")]
    public ImmutableList<string> ExtsDocument { get; set; }

    /// <summary>
    /// Internal formats
    /// </summary>
    [OpenApiDescription("Internal formats")]
    public Dictionary<FileType, string> InternalFormats { get; set; }

    /// <summary>
    /// Master form extension
    /// </summary>
    [OpenApiDescription("Master form extension")]
    public string MasterFormExtension { get; set; }

    /// <summary>
    /// Param version
    /// </summary>
    [OpenApiDescription("Param version")]
    public string ParamVersion { get; set; }

    /// <summary>
    /// Param out type
    /// </summary>
    [OpenApiDescription("Param out type")]
    public string ParamOutType { get; set; }

    /// <summary>
    /// File download url string
    /// </summary>
    [Url]
    [OpenApiDescription("File download url string")]
    public string FileDownloadUrlString { get; set; }

    /// <summary>
    /// File web viewer url string
    /// </summary>
    [OpenApiDescription("File web viewer url string")]
    public string FileWebViewerUrlString { get; set; }

    /// <summary>
    /// File web viewer external url string
    /// </summary>
    [Url]
    [OpenApiDescription("File web viewer external url string")]
    public string FileWebViewerExternalUrlString { get; set; }

    /// <summary>
    /// File web editor url string
    /// </summary>
    [OpenApiDescription("File web editor url string")]
    public string FileWebEditorUrlString { get; set; }

    /// <summary>
    /// File web editor external url string
    /// </summary>
    [Url]
    [OpenApiDescription("File web editor external url string")]
    public string FileWebEditorExternalUrlString { get; set; }

    /// <summary>
    /// File redirect preview url string
    /// </summary>
    [Url]
    [OpenApiDescription("File redirect preview url string")]
    public string FileRedirectPreviewUrlString { get; set; }

    /// <summary>
    /// File thumbnail url string
    /// </summary>
    [Url]
    [OpenApiDescription("File thumbnail url string")]
    public string FileThumbnailUrlString { get; set; }

    /// <summary>
    /// Confirm delete
    /// </summary>
    [OpenApiDescription("Confirm delete")]
    public bool ConfirmDelete { get; set; }

    /// <summary>
    /// EnableT third party
    /// </summary>
    [OpenApiDescription("Enable third party")]
    public bool EnableThirdParty { get; set; }

    /// <summary>
    /// External share
    /// </summary>
    [OpenApiDescription("External share")]
    public bool ExternalShare { get; set; }

    /// <summary>
    /// External share social media
    /// </summary>
    [OpenApiDescription("External share social media")]
    public bool ExternalShareSocialMedia { get; set; }

    /// <summary>
    /// Store original files
    /// </summary>
    [OpenApiDescription("Store original files")]
    public bool StoreOriginalFiles { get; set; }

    /// <summary>
    /// Keep new file name
    /// </summary>
    [OpenApiDescription("Keep new file name")]
    public bool KeepNewFileName { get; set; }

    /// <summary>
    /// Display file extension
    /// </summary>
    [OpenApiDescription("Display file extension")]
    public bool DisplayFileExtension { get; set; }

    /// <summary>
    /// Convert notify
    /// </summary>    
    [OpenApiDescription("Convert notify")]
    public bool ConvertNotify { get; set; }

    /// <summary>
    /// Hide confirm cancel operation
    /// </summary>
    [OpenApiDescription("Hide confirm cancel operation")]
    public bool HideConfirmCancelOperation { get; set; }
    
    /// <summary>
    /// Hide confirm convert save
    /// </summary>
    [OpenApiDescription("Hide confirm convert save")]
    public bool HideConfirmConvertSave { get; set; }

    /// <summary>
    /// Hide confirm convert open
    /// </summary>
    [OpenApiDescription("Hide confirm convert open")]
    public bool HideConfirmConvertOpen { get; set; }

    /// <summary>
    /// Hide confirm room lifetime
    /// </summary>
    [OpenApiDescription("Hide confirm room lifetime")]
    public bool HideConfirmRoomLifetime { get; set; }

    /// <summary>
    /// Default order
    /// </summary>
    [OpenApiDescription("Default order")]
    public OrderBy DefaultOrder { get; set; }

    /// <summary>
    /// Forcesave
    /// </summary>
    [OpenApiDescription("Forcesave")]
    public bool Forcesave { get; set; }

    /// <summary>
    /// Store forcesave
    /// </summary>
    [OpenApiDescription("Store forcesave")]
    public bool StoreForcesave { get; set; }

    /// <summary>
    /// Recent section
    /// </summary>
    [OpenApiDescription("Recent section")]
    public bool RecentSection { get; set; }

    /// <summary>
    /// Favorites section
    /// </summary>
    [OpenApiDescription("Favorites section")]
    public bool FavoritesSection { get; set; }

    /// <summary>
    /// Templates section
    /// </summary>
    [OpenApiDescription("Templates section")]
    public bool TemplatesSection { get; set; }

    /// <summary>
    /// Download tar gz
    /// </summary>
    [OpenApiDescription("Download tar gz")]
    public bool DownloadTarGz { get; set; }

    /// <summary>
    /// Automatically clean up
    /// </summary>
    [OpenApiDescription("Automatically clean up")]
    public AutoCleanUpData AutomaticallyCleanUp { get; set; }

    /// <summary>
    /// Can search by content
    /// </summary>
    [OpenApiDescription("Can search by content")]
    public bool CanSearchByContent { get; set; }

    /// <summary>
    /// Default sharing access rights
    /// </summary>
    [OpenApiDescription("Default sharing access rights")]
    public List<FileShare> DefaultSharingAccessRights { get; set; }

    /// <summary>
    /// Max upload thread count
    /// </summary>    
    [OpenApiDescription("Max upload thread count")]
    public int MaxUploadThreadCount { get; set; }

    /// <summary>
    /// Chunk upload size
    /// </summary>    
    [OpenApiDescription("Chunk upload size")]
    public long ChunkUploadSize { get; set; }

    /// <summary>
    /// Open editor in same tab
    /// </summary>
    [OpenApiDescription("Open editor in same tab")]
    public bool OpenEditorInSameTab { get; set; }
}


[Scope]
public class FilesSettingsDtoConverter(
    FileUtility fileUtility,
    FilesLinkUtility filesLinkUtility,
    FilesSettingsHelper filesSettingsHelper,
    SetupInfo setupInfo,
    SearchSettingsHelper searchSettingsHelper)
{
    public async Task<FilesSettingsDto> Get()
    {
        return new FilesSettingsDto
        {
            ExtsImagePreviewed = fileUtility.ExtsImagePreviewed,
            ExtsMediaPreviewed =  fileUtility.ExtsMediaPreviewed,
            ExtsWebPreviewed = fileUtility.ExtsWebPreviewed,
            ExtsWebEdited = fileUtility.ExtsWebEdited,
            ExtsWebEncrypt = fileUtility.ExtsWebEncrypt,
            ExtsWebReviewed =  fileUtility.ExtsWebReviewed,
            ExtsWebCustomFilterEditing = fileUtility.ExtsWebCustomFilterEditing,
            ExtsWebRestrictedEditing = fileUtility.ExtsWebRestrictedEditing,
            ExtsWebCommented = fileUtility.ExtsWebCommented,
            ExtsWebTemplate = fileUtility.ExtsWebTemplate,
            ExtsCoAuthoring = fileUtility.ExtsCoAuthoring,
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
            OpenEditorInSameTab = await filesSettingsHelper.GetOpenEditorInSameTabAsync()
        };
    }
}