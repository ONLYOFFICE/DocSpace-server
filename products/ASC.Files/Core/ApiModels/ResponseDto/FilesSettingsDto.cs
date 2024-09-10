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
    public List<string> ExtsImagePreviewed { get; set; }

    /// <summary>
    /// Exts media previewed
    /// </summary>
    public List<string> ExtsMediaPreviewed { get; set; }

    /// <summary>
    /// Exts web previewed
    /// </summary>
    public List<string> ExtsWebPreviewed { get; set; }

    /// <summary>
    /// Exts web edited
    /// </summary>
    public List<string> ExtsWebEdited { get; set; }

    /// <summary>
    /// Exts web encrypt
    /// </summary>
    public List<string> ExtsWebEncrypt { get; set; }

    /// <summary>
    /// Exts web reviewed
    /// </summary>
    public List<string> ExtsWebReviewed { get; set; }

    /// <summary>
    /// Exts web custom filter editing
    /// </summary>
    public List<string> ExtsWebCustomFilterEditing { get; set; }

    /// <summary>
    /// Exts web restricted editing
    /// </summary>
    public List<string> ExtsWebRestrictedEditing { get; set; }

    /// <summary>
    /// Exts web commented
    /// </summary>
    public List<string> ExtsWebCommented { get; set; }

    /// <summary>
    /// Exts web template
    /// </summary>
    public List<string> ExtsWebTemplate { get; set; }

    /// <summary>
    /// Exts co authoring
    /// </summary>
    public List<string> ExtsCoAuthoring { get; set; }

    /// <summary>
    /// Exts must convert
    /// </summary>
    public List<string> ExtsMustConvert { get; set; }

    /// <summary>
    /// Exts convertible
    /// </summary>
    public IDictionary<string, List<string>> ExtsConvertible { get; set; }

    /// <summary>
    /// Exts uploadable
    /// </summary>
    public List<string> ExtsUploadable { get; set; }

    /// <summary>
    /// Exts archive
    /// </summary>
    public ImmutableList<string> ExtsArchive { get; set; }

    /// <summary>
    /// Exts video
    /// </summary>
    public ImmutableList<string> ExtsVideo { get; set; }

    /// <summary>
    /// Exts audio
    /// </summary>
    public ImmutableList<string> ExtsAudio { get; set; }

    /// <summary>
    /// Exts image
    /// </summary>
    public ImmutableList<string> ExtsImage { get; set; }

    /// <summary>
    /// Exts spreadsheet
    /// </summary>
    public ImmutableList<string> ExtsSpreadsheet { get; set; }

    /// <summary>
    /// Exts presentation
    /// </summary>
    public ImmutableList<string> ExtsPresentation { get; set; }

    /// <summary>
    /// Exts document
    /// </summary>
    public ImmutableList<string> ExtsDocument { get; set; }

    /// <summary>
    /// Internal formats
    /// </summary>
    public Dictionary<FileType, string> InternalFormats { get; set; }

    /// <summary>
    /// Master form extension
    /// </summary>
    public string MasterFormExtension { get; set; }

    /// <summary>
    /// Param version
    /// </summary>
    public string ParamVersion { get; set; }

    /// <summary>
    /// Param out type
    /// </summary>
    public string ParamOutType { get; set; }

    [SwaggerSchemaCustom("File download url string", Format = "uri")]
    public string FileDownloadUrlString { get; set; }

    [SwaggerSchemaCustom("File web viewer url string", Format = "uri")]
    public string FileWebViewerUrlString { get; set; }

    [SwaggerSchemaCustom("File web viewer external url string", Format = "uri")]
    public string FileWebViewerExternalUrlString { get; set; }

    [SwaggerSchemaCustom("File web editor url string", Format = "uri")]
    public string FileWebEditorUrlString { get; set; }

    [SwaggerSchemaCustom("File web editor external url string", Format = "uri")]
    public string FileWebEditorExternalUrlString { get; set; }

    [SwaggerSchemaCustom("File redirect preview url string", Format = "uri")]
    public string FileRedirectPreviewUrlString { get; set; }

    [SwaggerSchemaCustom("File thumbnail url string", Format = "uri")]
    public string FileThumbnailUrlString { get; set; }

    /// <summary>
    /// Confirm delete
    /// </summary>
    public bool ConfirmDelete { get; set; }

    /// <summary>
    /// EnableT third party
    /// </summary>
    public bool EnableThirdParty { get; set; }

    /// <summary>
    /// External share
    /// </summary>
    public bool ExternalShare { get; set; }

    /// <summary>
    /// External share social media
    /// </summary>
    public bool ExternalShareSocialMedia { get; set; }

    /// <summary>
    /// Store original files
    /// </summary>
    public bool StoreOriginalFiles { get; set; }

    /// <summary>
    /// Keep new file name
    /// </summary>
    public bool KeepNewFileName { get; set; }

    /// <summary>
    /// Convert notify
    /// </summary>
    public bool ConvertNotify { get; set; }

    /// <summary>
    /// HideC confirm convert save
    /// </summary>
    public bool HideConfirmConvertSave { get; set; }

    /// <summary>
    /// Hide confirm convert open
    /// </summary>
    public bool HideConfirmConvertOpen { get; set; }

    /// <summary>
    /// Default order
    /// </summary>
    public OrderBy DefaultOrder { get; set; }

    /// <summary>
    /// Forcesave
    /// </summary>
    public bool Forcesave { get; set; }

    /// <summary>
    /// Store forcesave
    /// </summary>
    public bool StoreForcesave { get; set; }

    /// <summary>
    /// Recent section
    /// </summary>
    public bool RecentSection { get; set; }

    /// <summary>
    /// Favorites section
    /// </summary>
    public bool FavoritesSection { get; set; }

    /// <summary>
    /// Templates section
    /// </summary>
    public bool TemplatesSection { get; set; }

    /// <summary>
    /// Download tar gz
    /// </summary>
    public bool DownloadTarGz { get; set; }

    /// <summary>
    /// Automatically clean up
    /// </summary>
    public AutoCleanUpData AutomaticallyCleanUp { get; set; }

    /// <summary>
    /// Can search by content
    /// </summary>
    public bool CanSearchByContent { get; set; }

    /// <summary>
    /// Default sharing access rights
    /// </summary>
    public List<FileShare> DefaultSharingAccessRights { get; set; }


    /// <summary>
    /// Max upload thread count
    /// </summary>    
    public int MaxUploadThreadCount { get; set; }

    /// <summary>
    /// Chunk upload size
    /// </summary>    
    public long ChunkUploadSize { get; set; }

    /// <summary>
    /// Open editor in same tab
    /// </summary>
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
            ConvertNotify = await filesSettingsHelper.GetConvertNotify(),
            HideConfirmConvertSave = await filesSettingsHelper.GetHideConfirmConvertSave(),
            HideConfirmConvertOpen = await filesSettingsHelper.GetHideConfirmConvertOpen(),
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