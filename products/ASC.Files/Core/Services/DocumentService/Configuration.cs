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

namespace ASC.Web.Files.Services.DocumentService;

/// <summary>
/// The editor type.
/// </summary>
[EnumExtensions]
public enum EditorType
{
    [Description("Desktop")]
    Desktop,

    [Description("Mobile")]
    Mobile,

    [Description("Embedded")]
    Embedded
}

/// <summary>
/// The place inside a document that a link should open at.
/// </summary>
public class ActionLinkConfig
{
    /// <summary>
    /// The anchor itself. It is passed on to the editor unchanged, so it has to be the value the editor produced for
    /// the comment or the mention it points at.
    /// </summary>
    /// <example>{"data": "section-42", "type": "comment"}</example>
    [JsonPropertyName("action")]
    public ActionConfig Action { get; set; }

    public static string Serialize(ActionLinkConfig actionLinkConfig)
    {
        return JsonSerializer.Serialize(actionLinkConfig);
    }

    /// <summary>
    /// An anchor inside a document, as the editor writes it.
    /// </summary>
    public class ActionConfig
    {
        /// <summary>
        /// The anchor value produced by the editor, opaque to the portal: it names the comment, the mention or the
        /// place the document is scrolled to.
        /// </summary>
        /// <example>section-42</example>
        [JsonPropertyName("data")]
        [StringLength(256)]
        public string Data { get; set; }

        /// <summary>
        /// What the anchor points at, as the editor names it - a comment thread, for instance.
        /// </summary>
        /// <example>comment</example>
        [JsonPropertyName("type")]
        [StringLength(128)]
        public string Type { get; set; }
    }
}

/// <summary>
/// The co-editing mode (fast or strict).
/// </summary>
[EnumExtensions]
public enum CoEditingConfigMode
{
    [Description("Fast")]
    Fast,

    [Description("Strict")]
    Strict
}


/// <summary>
/// The configuration parameters.
/// </summary>
[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class Configuration<T>(
    DocumentConfig<T> document,
    EditorConfiguration<T> editorConfig)
{
    internal static readonly Dictionary<FileType, string> DocType = new()
    {
        { FileType.Document, "word" },
        { FileType.Spreadsheet, "cell" },
        { FileType.Presentation, "slide" },
        { FileType.Pdf, "pdf" },
        { FileType.Diagram, "diagram" }
    };

    /// <summary>
    /// The type of the file for the source viewed or edited document.
    /// </summary>
    private FileType _fileTypeCache = FileType.Unknown;

    /// <summary>
    /// The document configuration parameters.
    /// </summary>
    public DocumentConfig<T> Document { get; } = document;

    /// <summary>
    /// The document type to be opened.
    /// </summary>
    public string GetDocumentType(File<T> file)
    {
        DocType.TryGetValue(GetFileType(file), out var documentType);

        return documentType;
    }

    /// <summary>
    /// The editor configuration parameters.
    /// </summary>
    public EditorConfiguration<T> EditorConfig { get; } = editorConfig;

    /// <summary>
    /// The editor type.
    /// </summary>
    public EditorType EditorType
    {
        set => Document.Info.Type = value;
        get => Document.Info.Type;
    }

    public string Error { get; set; }

    /// <summary>
    /// The platform type used to access the document.
    /// </summary>
    public string Type
    {
        set => EditorType = Enum.Parse<EditorType>(value, true);
        get => EditorType.ToString().ToLower();
    }

    public FileType GetFileType(File<T> file)
    {
        if (_fileTypeCache == FileType.Unknown)
        {
            _fileTypeCache = FileUtility.GetFileTypeByFileName(file.Title);
        }

        return _fileTypeCache;
    }
}

#region Nested Classes

[Transient(GenericArguments = [typeof(int)])]
[Transient(GenericArguments = [typeof(string)])]
public class DocumentConfig<T>(
    DocumentServiceConnector documentServiceConnector,
    PathProvider pathProvider,
    InfoConfig<T> infoConfig,
    TenantManager tenantManager)
{
    private string _fileUri;
    private FileReferenceDataDto _referenceData;
    public string GetFileType(File<T> file) => file.ConvertedExtension.Trim('.');
    public InfoConfig<T> Info { get; } = infoConfig;
    public bool IsLinkedForMe { get; set; }

    public string Key
    {
        set;
        get => DocumentServiceConnector.GenerateRevisionId(field);
    } = string.Empty;

    public PermissionsConfigDto Permissions { get; set; } = new();

    public Options Options { get; set; }
    public string SharedLinkParam { get; set; }
    public string SharedLinkKey { get; set; }
    public FileReferenceDataDto GetReferenceData(File<T> file)
    {
        return _referenceData ??= new FileReferenceDataDto
        {
            FileKey = file.Id.ToString(),
            InstanceId = tenantManager.GetCurrentTenantId().ToString()
        };
    }

    public string Title { get; set; }

    public void SetUrl(string val)
    {
        _fileUri = documentServiceConnector.ReplaceCommunityAddress(val);
    }

    public string GetUrl(File<T> file)
    {
        if (!string.IsNullOrEmpty(_fileUri))
        {
            return _fileUri;
        }

        var last = Permissions.Edit || Permissions.Review || Permissions.Comment;
        _fileUri = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileStreamUrl(file, last));

        return _fileUri;
    }
}

[Transient(GenericArguments = [typeof(int)])]
[Transient(GenericArguments = [typeof(string)])]
public class EditorConfiguration<T>(
    UserManager userManager,
    AuthContext authContext,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    FilesLinkUtility filesLinkUtility,
    FileUtility fileUtility,
    BaseCommonLinkUtility baseCommonLinkUtility,
    PluginsConfigDto pluginsConfig,
    EmbeddedConfigDto embeddedConfig,
    CustomizationConfig<T> customizationConfig,
    FilesSettingsHelper filesSettingsHelper,
    IDaoFactory daoFactory,
    EntryManager entryManager,
    DocumentServiceTrackerHelper documentServiceTrackerHelper,
    ExternalShare externalShare,
    UserPhotoManager userPhotoManager,
    GlobalFolderHelper globalFolderHelper,
    TariffService tariffService,
    TenantManager tenantManager)
{
    public PluginsConfigDto Plugins { get; } = pluginsConfig;
    public CustomizationConfig<T> Customization { get; } = customizationConfig;
    public List<EncryptionKeyDto> EncryptionKeys { get; set; }

    public string Lang => UserInfo.GetCulture().Name;

    public string Mode => ModeWrite ? "edit" : "view";

    public bool ModeWrite { get; set; }

    private UserInfo UserInfo => field ??= userManager.GetUsers(authContext.CurrentAccount.ID);

    private UserConfigDto _user;
    public async Task<UserConfigDto> GetUserAsync()
    {
        if (_user != null || UserInfo.Id.Equals(ASC.Core.Configuration.Constants.Guest.ID))
        {
            return _user;
        }

        var customerInfo = await tariffService.GetCustomerInfoAsync(tenantManager.GetCurrentTenantId());

        _user = new UserConfigDto
        {
            Id = UserInfo.Id.ToString(),
            Name = UserInfo.DisplayUserName(false, displayUserSettingsHelper),
            Image = baseCommonLinkUtility.GetFullAbsolutePath(await UserInfo.GetMediumPhotoURLAsync(userPhotoManager)),
            CustomerId = customerInfo?.PortalId
        };

        return _user;
    }

    public async Task<string> GetCallbackUrl(File<T> file)
    {
        if (!ModeWrite)
        {
            return null;
        }

        var callbackUrl = documentServiceTrackerHelper.GetCallbackUrl(file.Id.ToString());

        if (!string.IsNullOrEmpty(file.FormInfo?.FillingSessionId))
        {
            callbackUrl = externalShare.GetUrlWithFillingSessionId(callbackUrl, file.FormInfo.FillingSessionId);
        }

        if (file.ShareRecord is not { IsLink: true } || string.IsNullOrEmpty(file.ShareRecord.Options?.Password))
        {
            return externalShare.GetUrlWithShare(callbackUrl);
        }

        var key = await externalShare.CreateShareKeyAsync(file.ShareRecord.Subject, file.ShareRecord.Options?.Password);
        return externalShare.GetUrlWithShare(callbackUrl, key);
    }

    public async Task<CoEditingConfigDto> GetCoEditingAsync()
    {
        return !ModeWrite && await GetUserAsync() == null
            ? new CoEditingConfigDto
            {
                Fast = false,
                Change = false
            }
            : null;
    }

    public async Task<string> GetCreateUrl(EditorType editorType, FileType fileType)
    {
        if (editorType != EditorType.Desktop)
        {
            return null;
        }

        if (!authContext.IsAuthenticated || await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            return null;
        }
        string title;
        switch (fileType)
        {
            case FileType.Document:
                title = FilesJSResource.TitleNewFileText;
                break;

            case FileType.Spreadsheet:
                title = FilesJSResource.TitleNewFileSpreadsheet;
                break;

            case FileType.Presentation:
                title = FilesJSResource.TitleNewFilePresentation;
                break;

            case FileType.Pdf:
                title = FilesJSResource.TitleNewFilePdfFormText;
                break;

            default:
                return null;
        }

        Configuration<T>.DocType.TryGetValue(fileType, out var documentType);

        return baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FileHandlerPath)
               + "?" + FilesLinkUtility.Action + "=create"
               + "&doctype=" + documentType
               + "&" + FilesLinkUtility.FileTitle + "=" + HttpUtility.UrlEncode(title);
    }

    public EmbeddedConfigDto GetEmbedded(EditorType editorType)
    {
        return editorType == EditorType.Embedded ? embeddedConfig : null;
    }

    public async IAsyncEnumerable<RecentConfigDto> GetRecent(FileType fileType, T fileId)
    {
        if (!authContext.IsAuthenticated || await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            yield break;
        }

        if (!await filesSettingsHelper.GetRecentSection())
        {
            yield break;
        }

        var filter = fileType switch
        {
            FileType.Document => FilterType.DocumentsOnly,
            FileType.Pdf => FilterType.Pdf,
            FileType.Spreadsheet => FilterType.SpreadsheetsOnly,
            FileType.Presentation => FilterType.PresentationsOnly,
            FileType.Diagram => FilterType.DiagramsOnly,
            _ => FilterType.FilesOnly
        };

        var folderDao = daoFactory.GetFolderDao<int>();
        var recentId = await globalFolderHelper.FolderRecentAsync;
        var recent = await folderDao.GetFolderAsync(recentId);

        var (entries, _) = await entryManager.GetEntriesAsync(recent, null, 0, 10, [filter], false, Guid.Empty, Guid.Empty, string.Empty, null, false, false, new OrderBy(SortedByType.LastOpened, false));

        var files = entries.OfType<File<int>>().Where(file => !Equals(fileId, file.Id)).ToList();
        var thirdPartyFiles = entries.OfType<File<string>>().Where(file => !Equals(fileId, file.Id)).ToList();

        await foreach (var config in GetRecentConfigsAsync(files, folderDao))
        {
            yield return config;
        }

        await foreach (var config in GetRecentConfigsAsync(thirdPartyFiles, daoFactory.GetFolderDao<string>()))
        {
            yield return config;
        }
    }

    private async IAsyncEnumerable<RecentConfigDto> GetRecentConfigsAsync<TFile>(List<File<TFile>> files, IFolderDao<TFile> folderDao)
    {
        if (files.Count == 0)
        {
            yield break;
        }

        var parentIds = files.Select(r => r.ParentId).Distinct().ToList();
        var parentFolders = await folderDao.GetFoldersAsync(parentIds).ToListAsync();

        foreach (var file in files)
        {
            var externalMediaAccess = file.ShareRecord is { SubjectType: SubjectType.PrimaryExternalLink or SubjectType.ExternalLink };
            var requestToken = "";
            if (externalMediaAccess)
            {
                requestToken = await externalShare.CreateShareKeyAsync(file.ShareRecord.Subject);
            }

            var webUrl = externalShare.GetUrlWithShare(baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebPreviewUrl(fileUtility, file.Title, file.Id, file.Version, externalMediaAccess)), requestToken);

            yield return new RecentConfigDto
            {
                Folder = parentFolders.FirstOrDefault(r => Equals(file.ParentId, r.Id))?.Title,
                Title = file.Title,
                Url = baseCommonLinkUtility.GetFullAbsolutePath(webUrl)
            };
        }
    }

    public async Task<List<TemplatesConfigDto>> GetTemplates(FileType fileType, string title)
    {
        if (!authContext.IsAuthenticated || await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            return null;
        }

        if (!await filesSettingsHelper.GetTemplatesSection())
        {
            return null;
        }

        var extension = fileUtility.GetInternalExtension(title).TrimStart('.');
        var filter = fileType switch
        {
            FileType.Document => FilterType.DocumentsOnly,
            FileType.Pdf => FilterType.Pdf,
            FileType.Spreadsheet => FilterType.SpreadsheetsOnly,
            FileType.Presentation => FilterType.PresentationsOnly,
            FileType.Diagram => FilterType.DiagramsOnly,
            _ => FilterType.FilesOnly
        };

        var folderDao = daoFactory.GetFolderDao<int>();
        var fileDao = daoFactory.GetFileDao<int>();
        var files = await entryManager.GetTemplatesAsync(folderDao, fileDao, filter, false, Guid.Empty, string.Empty, null, false).ToListAsync();
        var listTemplates = from file in files
                            select
                                new TemplatesConfigDto
                                {
                                    Image = baseCommonLinkUtility.GetFullAbsolutePath("skins/default/images/filetype/thumb/" + extension + ".png"),
                                    Title = file.Title,
                                    Url = baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebEditorUrl(file.Id))
                                };
        return listTemplates.ToList();
    }
}

[Transient(GenericArguments = [typeof(int)])]
[Transient(GenericArguments = [typeof(string)])]
public class InfoConfig<T>(
    BreadCrumbsManager breadCrumbsManager,
    FileSharing fileSharing,
    SecurityContext securityContext,
    UserManager userManager)
{
    private string _breadCrumbs;
    private bool? _favorite;
    private bool _favoriteIsSet;

    public async Task<bool?> GetFavorite(File<T> file)
    {
        if (_favoriteIsSet)
        {
            return _favorite;
        }

        if (!securityContext.IsAuthenticated || await userManager.IsGuestAsync(securityContext.CurrentAccount.ID))
        {
            return null;
        }

        if (file.ParentId == null || file.Encrypted)
        {
            return null;
        }

        return file.IsFavorite;
    }
    public void SetFavorite(bool? newValue)
    {
        _favoriteIsSet = true;
        _favorite = newValue;
    }

    public async Task<string> GetFolder(File<T> file)
    {
        if (Type == EditorType.Embedded)
        {
            return null;
        }

        if (string.IsNullOrEmpty(_breadCrumbs))
        {
            const string separator = " \\ ";

            var breadCrumbsList = await breadCrumbsManager.GetBreadCrumbsAsync(file.ParentId);
            _breadCrumbs = string.Join(separator, breadCrumbsList.Select(folder => folder.Title).ToArray());
        }

        return _breadCrumbs;
    }

    public string GetOwner(File<T> file) => file.CreateByString;

    public async Task<List<AceShortDto>> GetSharingSettings(File<T> file)
    {
        if (Type == EditorType.Embedded || !await fileSharing.CanSetAccessAsync(file))
        {
            return null;
        }

        try
        {
            return await fileSharing.GetSharedInfoShortFileAsync(file);
        }
        catch
        {
            return null;
        }
    }

    public EditorType Type { get; set; } = EditorType.Desktop;

    public string GetUploaded(File<T> file) => file.CreateOnString;
}

/// <summary>
/// The document options.
/// </summary>
public class Options
{
    /// <summary>
    /// The document watermark parameters.
    /// </summary>
    [JsonPropertyName("watermark_on_draw")]
    public WatermarkOnDraw WatermarkOnDraw { get; set; }

    public string GetMD5Hash()
    {
        if (WatermarkOnDraw == null)
        {
            return null;
        }

        var stringBuilder = new StringBuilder();

        _ = stringBuilder.Append(WatermarkOnDraw.Width.ToString(CultureInfo.InvariantCulture));
        _ = stringBuilder.Append(WatermarkOnDraw.Height.ToString(CultureInfo.InvariantCulture));
        _ = stringBuilder.Append(string.Join(',', WatermarkOnDraw.Margins));
        _ = stringBuilder.Append(WatermarkOnDraw.Fill);
        _ = stringBuilder.Append(WatermarkOnDraw.Rotate);
        _ = stringBuilder.Append(WatermarkOnDraw.Transparent.ToString(CultureInfo.InvariantCulture));

        if (WatermarkOnDraw.Paragraphs != null)
        {
            foreach (var paragraph in WatermarkOnDraw.Paragraphs)
            {
                if (paragraph.Runs != null)
                {
                    foreach (var run in paragraph.Runs)
                    {
                        if (run.UsedInHash)
                        {
                            _ = stringBuilder.Append(run.Text);
                        }
                    }
                }
            }
        }

        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(stringBuilder.ToString()));

        return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }
}

/// <summary>
/// The document watermark parameters.
/// </summary>
public class WatermarkOnDraw(double widthInPixels, double heightInPixels, string fill, int rotate, List<Paragraph> paragraphs)
{
    private const double DotsPerInch = 96;
    private const double DotsPerMm = DotsPerInch / 25.4;

    /// <summary>
    /// Defines the watermark width measured in millimeters.
    /// </summary>
    /// <example>150</example>
    [JsonPropertyName("width")]
    public double Width { get; init; } = widthInPixels == 0 ? 100 : widthInPixels / DotsPerMm;

    /// <summary>
    /// Defines the watermark height measured in millimeters.
    /// </summary>
    /// <example>100</example>
    [JsonPropertyName("height")]
    public double Height { get; init; } = heightInPixels == 0 ? 100 : heightInPixels / DotsPerMm;

    /// <summary>
    /// Defines the watermark margins measured in millimeters.
    /// </summary>
    /// <example>[10, 10, 10, 10]</example>
    [JsonPropertyName("margins")]
    public int[] Margins { get; init; } = [0, 0, 0, 0];

    /// <summary>
    /// Defines the watermark fill color.
    /// </summary>
    /// <example>#FF0000</example>
    [JsonPropertyName("fill")]
    public string Fill { get; init; } = fill;

    /// <summary>
    /// Defines the watermark rotation angle.
    /// </summary>
    /// <example>45</example>
    [JsonPropertyName("rotate")]
    public int Rotate { get; init; } = rotate;

    /// <summary>
    /// Defines the watermark transparency percentage.
    /// </summary>
    /// <example>0.4</example>
    [JsonPropertyName("transparent")]
    public double Transparent { get; init; } = 0.4;

    /// <summary>
    /// The list of paragraphs of the watermark.
    /// </summary>
    /// <example>[ { "align": 2, "runs": [{"fill": [124, 124, 124], "text": "CONFIDENTIAL", "fontSize": 26}] } ]</example>
    [JsonPropertyName("paragraphs")]
    public List<Paragraph> Paragraphs { get; init; } = paragraphs;
}

/// <summary>
/// The paragraph parameters.
/// </summary>
public class Paragraph
{
    public Paragraph(List<Run> runs)
    {
        Runs = runs;
        Align = 2;
    }

    /// <summary>
    /// The paragraph align.
    /// </summary>
    /// <example>2</example>
    [JsonPropertyName("align")]
    public int Align { get; set; }

    /// <summary>
    /// The list of text runs from the paragraph.
    /// </summary>
    /// <example>[{"fill": [124, 124, 124], "text": "CONFIDENTIAL", "fontSize": 26}]</example>
    [JsonPropertyName("runs")]
    public List<Run> Runs { get; set; }
}
/// <summary>
/// The text run parameters.
/// </summary>
public class Run(string text, bool usedInHash = true)
{
    internal bool UsedInHash => usedInHash;

    /// <summary>
    /// The fill color of the text run in RGB format.
    /// </summary>
    /// <example>[124, 124, 124]</example>
    [JsonPropertyName("fill")]
    public int[] Fill { get; set; } = [124, 124, 124];

    /// <summary>
    /// The run text.
    /// </summary>
    /// <example>CONFIDENTIAL</example>
    [JsonPropertyName("text")]
    public string Text { get; set; } = text;

    /// <summary>
    /// The font size of the text run in points.
    /// </summary>
    /// <example>26</example>
    [JsonPropertyName("font-size")]
    public string FontSize { get; set; } = "26";
}

#endregion Nested Classes

/// <summary>
/// The customer configuration parameters.
/// </summary>
[Transient]
public class CustomerConfig(
    SettingsManager settingsManager,
    BaseCommonLinkUtility baseCommonLinkUtility,
    TenantWhiteLabelSettingsHelper tenantWhiteLabelSettingsHelper)
{
    public async Task<string> GetAddress() => (await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>()).Address;

    public async Task<string> GetLogo() => baseCommonLinkUtility.GetFullAbsolutePath(await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(WhiteLabelLogoType.AboutPage, false));

    public async Task<string> GetLogoDark() => baseCommonLinkUtility.GetFullAbsolutePath(await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(WhiteLabelLogoType.AboutPage, true));

    public async Task<string> GetMail() => (await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>()).Email;

    public async Task<string> GetName() => (await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>()).CompanyName;

    public async Task<string> GetWww() => (await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>()).Site;
}

[Transient(GenericArguments = [typeof(int)])]
[Transient(GenericArguments = [typeof(string)])]
public class CustomizationConfig<T>(
    CoreBaseSettings coreBaseSettings,
    TenantManager tenantManager,
    SettingsManager settingsManager,
    FileUtility fileUtility,
    FilesSettingsHelper filesSettingsHelper,
    AuthContext authContext,
    FileSecurity fileSecurity,
    IDaoFactory daoFactory,
    GlobalFolderHelper globalFolderHelper,
    PathProvider pathProvider,
    CustomerConfig customerConfig,
    LogoConfig logoConfig,
    FileSharing fileSharing,
    CommonLinkUtility commonLinkUtility,
    ExternalShare externalShare,
    ExternalLinkHelper externalLinkHelper)
{
    [JsonIgnore]
    public string GobackUrl;

    public async Task<bool> IsAboutPageVisible()
    {
        if (!coreBaseSettings.Standalone && !coreBaseSettings.CustomMode)
        {
            return true;
        }

        var quota = await tenantManager.GetCurrentTenantQuotaAsync();
        if (!quota.Branding)
        {
            return true;
        }

        var companyWhiteLabelSettings = await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>();
        return !companyWhiteLabelSettings.HideAbout;
    }

    public CustomerConfig Customer { get; set; } = customerConfig;

    public async Task<FeedbackConfigDto> GetFeedback()
    {
        if (coreBaseSettings.Standalone)
        {
            return null;
        }

        var link = await commonLinkUtility.GetSupportLinkAsync(settingsManager);

        if (string.IsNullOrEmpty(link))
        {
            return null;
        }

        return new FeedbackConfigDto
        {
            Url = link
        };
    }

    public bool? GetForceSave(File<T> file)
    {
        return fileUtility.GetCanForcesave()
               && !file.ProviderEntry
               && filesSettingsHelper.GetForcesave();
    }

    public async Task<GobackConfigDto> GetGoBack(EditorType editorType, File<T> file)
    {
        if (GobackUrl != null)
        {
            return new GobackConfigDto
            {
                Url = GobackUrl
            };
        }

        Folder<T> parent;
        var folderDao = daoFactory.GetCacheFolderDao<T>();
        var (shareRight, key) = await CheckLinkAsync(file);

        if (!authContext.IsAuthenticated)
        {
            if (shareRight != FileShare.Restrict && !string.IsNullOrEmpty(key))
            {
                parent = await folderDao.GetFolderAsync(file.ParentId);
                return new GobackConfigDto
                {
                    Url = pathProvider.GetFolderUrl(parent, key)
                };
            }
        }

        try
        {

            parent = await folderDao.GetFolderAsync(file.ParentId);

            if (file.RootFolderType == FolderType.USER && !Equals(file.RootId, await globalFolderHelper.FolderMyAsync))
            {
                if (!await fileSecurity.CanReadAsync(file))
                {
                    return null;
                }

                string url;

                if (parent.FolderType != FolderType.USER && await fileSecurity.CanReadAsync(parent))
                {
                    parent.RootFolderType = FolderType.SHARE;
                    url = pathProvider.GetFolderUrl(parent, key);
                    parent.RootFolderType = FolderType.USER;
                }
                else
                {
                    url = pathProvider.GetFolderUrl(await folderDao.GetFolderAsync(await globalFolderHelper.GetFolderShareAsync<T>()), key);
                }

                return new GobackConfigDto
                {
                    Url = url
                };
            }

            var canReadParent = await fileSecurity.CanReadAsync(parent);

            if (file.Encrypted &&
                file.RootFolderType == FolderType.Privacy &&
                !canReadParent)
            {
                parent = await folderDao.GetFolderAsync(await globalFolderHelper.GetFolderPrivacyAsync<T>());
            }

            if (file.RootFolderType == FolderType.VirtualRooms &&
                !canReadParent)
            {
                parent = await folderDao.GetFolderAsync(await globalFolderHelper.GetFolderShareAsync<T>());
            }

            return new GobackConfigDto
            {
                Url = pathProvider.GetFolderUrl(parent, key)
            };
        }
        catch (Exception)
        {
            return null;
        }

    }

    public LogoConfig Logo { get; set; } = logoConfig;

    public async Task<bool> GetMentionShare(File<T> file)
    {
        return authContext.IsAuthenticated
               && !file.Encrypted
               && await fileSharing.CanSetAccessAsync(file);
    }

    public ReviewConfigDto GetReview(bool modeWrite)
    {
        return modeWrite ? null : new ReviewConfigDto { ReviewDisplayEnum = ReviewDisplayEnum.Markup };
    }

    public async Task<SubmitFormDto> GetSubmitForm(File<T> file)
    {
        if (!file.IsPdf)
        {
            return null;
        }
        var properties = await daoFactory.GetFileDao<T>().GetProperties(file.Id);
        return new SubmitFormDto
        {
            Visible = file.RootFolderType != FolderType.Archive && await fileSecurity.CanFillFormAsync(file, authContext.CurrentAccount.ID) && properties is { FormFilling.StartFilling: true } or { FormFilling.CollectFillForm: true },
            ResultMessage = ""
        };
    }

    public async Task<AiConfigDto> GetAIConfigAsync()
    {
        var settings = await settingsManager.LoadAsync<TenantAiAccessSettings>();
        return new AiConfigDto
        {
            Disabled = !settings.Enabled
        };
    }

    private async Task<(FileShare, string)> CheckLinkAsync(File<T> file)
    {
        var linkRight = FileShare.Restrict;

        var key = externalShare.GetKey();
        if (string.IsNullOrEmpty(key))
        {
            return (linkRight, key);
        }

        var result = await externalLinkHelper.ValidateAsync(key);
        if (result.Access == FileShare.Restrict)
        {
            return (linkRight, key);
        }

        if (file != null && await fileSecurity.CanDownloadAsync(file))
        {
            linkRight = result.Access;
        }

        return (linkRight, key);
    }
}

[EnumExtensions]
public enum ReviewDisplayEnum
{
    Markup,
    Simple,
    Final,
    Original
}

[Transient]
public class LogoConfig(
    CommonLinkUtility commonLinkUtility,
    TenantLogoHelper tenantLogoHelper)
{

    public async Task<string> GetImage(FileType fileType, EditorType editorType)
    {
        var logoType = WhiteLabelLogoTypeHelper.GetEditorLogoType(fileType, editorType == EditorType.Embedded);

        return commonLinkUtility.GetFullAbsolutePath(await tenantLogoHelper.GetLogo(logoType));
    }

    public async Task<string> GetImageLight(FileType fileType)
    {
        var logoType = WhiteLabelLogoTypeHelper.GetEditorLogoType(fileType, embed: true);

        return commonLinkUtility.GetFullAbsolutePath(await tenantLogoHelper.GetLogo(logoType));
    }

    public async Task<string> GetImageDark(FileType fileType)
    {
        var logoType = WhiteLabelLogoTypeHelper.GetEditorLogoType(fileType, embed: false);

        return commonLinkUtility.GetFullAbsolutePath(await tenantLogoHelper.GetLogo(logoType));
    }

    public async Task<string> GetImageEmbedded(FileType fileType, EditorType editorType)
    {
        if (editorType != EditorType.Embedded)
        {
            return null;
        }

        var logoType = WhiteLabelLogoTypeHelper.GetEditorLogoType(fileType, true);

        return commonLinkUtility.GetFullAbsolutePath(await tenantLogoHelper.GetLogo(logoType));
    }

    public string Url => commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetDefault());

    public bool GetVisible(EditorType editorType)
    {
        return editorType != EditorType.Mobile;
    }
}
