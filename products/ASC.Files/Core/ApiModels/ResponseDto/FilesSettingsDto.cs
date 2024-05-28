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
    public List<string> ExtsImagePreviewed { get; set; }
    public List<string> ExtsMediaPreviewed { get; set; }
    public List<string> ExtsWebPreviewed { get; set; }
    public List<string> ExtsWebEdited { get; set; }
    public List<string> ExtsWebEncrypt { get; set; }
    public List<string> ExtsWebReviewed { get; set; }
    public List<string> ExtsWebCustomFilterEditing { get; set; }
    public List<string> ExtsWebRestrictedEditing { get; set; }
    public List<string> ExtsWebCommented { get; set; }
    public List<string> ExtsWebTemplate { get; set; }
    public List<string> ExtsCoAuthoring { get; set; }
    public List<string> ExtsMustConvert { get; set; }
    public IDictionary<string, List<string>> ExtsConvertible { get; set; }
    public List<string> ExtsUploadable { get; set; }
    public ImmutableList<string> ExtsArchive { get; set; }
    public ImmutableList<string> ExtsVideo { get; set; }
    public ImmutableList<string> ExtsAudio { get; set; }
    public ImmutableList<string> ExtsImage { get; set; }
    public ImmutableList<string> ExtsSpreadsheet { get; set; }
    public ImmutableList<string> ExtsPresentation { get; set; }
    public ImmutableList<string> ExtsDocument { get; set; }
    public Dictionary<FileType, string> InternalFormats { get; set; }
    public string MasterFormExtension { get; set; }
    public string ParamVersion { get; set; }
    public string ParamOutType { get; set; }
    public string FileDownloadUrlString { get; set; }
    public string FileWebViewerUrlString { get; set; }
    public string FileWebViewerExternalUrlString { get; set; }
    public string FileWebEditorUrlString { get; set; }
    public string FileWebEditorExternalUrlString { get; set; }
    public string FileRedirectPreviewUrlString { get; set; }
    public string FileThumbnailUrlString { get; set; }
    public bool ConfirmDelete { get; set; }
    public bool EnableThirdParty { get; set; }
    public bool ExternalShare { get; set; }
    public bool ExternalShareSocialMedia { get; set; }
    public bool StoreOriginalFiles { get; set; }
    public bool KeepNewFileName { get; set; }
    public bool ConvertNotify { get; set; }
    public bool HideConfirmConvertSave { get; set; }
    public bool HideConfirmConvertOpen { get; set; }
    public OrderBy DefaultOrder { get; set; }
    public bool Forcesave { get; set; }
    public bool StoreForcesave { get; set; }
    public bool RecentSection { get; set; }
    public bool FavoritesSection { get; set; }
    public bool TemplatesSection { get; set; }
    public bool DownloadTarGz { get; set; }
    public AutoCleanUpData AutomaticallyCleanUp { get; set; }
    public bool CanSearchByContent { get; set; }
    public List<FileShare> DefaultSharingAccessRights { get; set; }
    public long ChunkUploadSize { get; set; }
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
            ChunkUploadSize = setupInfo.ChunkUploadSize,
            OpenEditorInSameTab = await filesSettingsHelper.GetOpenEditorInSameTabAsync()
        };
    }
}