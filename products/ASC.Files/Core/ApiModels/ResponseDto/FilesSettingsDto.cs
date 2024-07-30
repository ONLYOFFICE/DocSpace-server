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
    [SwaggerSchemaCustom(Example = "some text", Description = "Exts image previewed")]
    public List<string> ExtsImagePreviewed { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts media previewed")]
    public List<string> ExtsMediaPreviewed { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts web previewed")]
    public List<string> ExtsWebPreviewed { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts web edited")]
    public List<string> ExtsWebEdited { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts web encrypt")]
    public List<string> ExtsWebEncrypt { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts web reviewed")]
    public List<string> ExtsWebReviewed { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts web custom filter editing")]
    public List<string> ExtsWebCustomFilterEditing { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts web restricted editing")]
    public List<string> ExtsWebRestrictedEditing { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts web commented")]
    public List<string> ExtsWebCommented { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts web template")]
    public List<string> ExtsWebTemplate { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts co authoring")]
    public List<string> ExtsCoAuthoring { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts must convert")]
    public List<string> ExtsMustConvert { get; set; }

    [SwaggerSchemaCustom(Description = "Exts convertible")]
    public IDictionary<string, List<string>> ExtsConvertible { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts uploadable")]
    public List<string> ExtsUploadable { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts archive")]
    public ImmutableList<string> ExtsArchive { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts video")]
    public ImmutableList<string> ExtsVideo { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts audio")]
    public ImmutableList<string> ExtsAudio { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts image")]
    public ImmutableList<string> ExtsImage { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts spreadsheet")]
    public ImmutableList<string> ExtsSpreadsheet { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts presentation")]
    public ImmutableList<string> ExtsPresentation { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Exts document")]
    public ImmutableList<string> ExtsDocument { get; set; }

    [SwaggerSchemaCustom(Description = "Internal formats")]
    public Dictionary<FileType, string> InternalFormats { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Master form extension")]
    public string MasterFormExtension { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Param version")]
    public string ParamVersion { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Param out type")]
    public string ParamOutType { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "File download url string", Format = "uri")]
    public string FileDownloadUrlString { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "File web viewer url string", Format = "uri")]
    public string FileWebViewerUrlString { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "File web viewer external url string", Format = "uri")]
    public string FileWebViewerExternalUrlString { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "File web editor url string", Format = "uri")]
    public string FileWebEditorUrlString { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "File web editor external url string", Format = "uri")]
    public string FileWebEditorExternalUrlString { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "File redirect preview url string", Format = "uri")]
    public string FileRedirectPreviewUrlString { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "File thumbnail url string", Format = "uri")]
    public string FileThumbnailUrlString { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Confirm delete")]
    public bool ConfirmDelete { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "EnableT third party")]
    public bool EnableThirdParty { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "External share")]
    public bool ExternalShare { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "External share social media")]
    public bool ExternalShareSocialMedia { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Store original files")]
    public bool StoreOriginalFiles { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Keep new file name")]
    public bool KeepNewFileName { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Convert notify")]
    public bool ConvertNotify { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "HideC confirm convert save")]
    public bool HideConfirmConvertSave { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Hide confirm convert open")]
    public bool HideConfirmConvertOpen { get; set; }

    [SwaggerSchemaCustom(Description = "DefaultO order")]
    public OrderBy DefaultOrder { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Forcesave")]
    public bool Forcesave { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Store forcesave")]
    public bool StoreForcesave { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Recent section")]
    public bool RecentSection { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Favorites section")]
    public bool FavoritesSection { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Templates section")]
    public bool TemplatesSection { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Download tar gz")]
    public bool DownloadTarGz { get; set; }

    [SwaggerSchemaCustom(Description = "Automatically clean up")]
    public AutoCleanUpData AutomaticallyCleanUp { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Can search by content")]
    public bool CanSearchByContent { get; set; }

    [SwaggerSchemaCustom(Example = "None", Description = "Default sharing access rights")]
    public List<FileShare> DefaultSharingAccessRights { get; set; }


    [SwaggerSchemaCustom(Example = "1234", Description = "Max upload thread count", Format = "int32")]    public int MaxUploadThreadCount { get; set; }

    [SwaggerSchemaCustom(Example = "1234", Description = "Chunk upload size", Format = "int64")]    public long ChunkUploadSize { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Open editor in same tab")]
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