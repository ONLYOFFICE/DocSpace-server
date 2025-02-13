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

namespace ASC.Web.Files.Services.DocumentService;

[EnumExtensions]
public enum EditorType
{
    [SwaggerEnum(Description = "Desktop")]
    Desktop,

    [SwaggerEnum(Description = "Mobile")]
    Mobile,

    [SwaggerEnum(Description = "Embedded")]
    Embedded
}

public class ActionLinkConfig
{
    /// <summary>
    /// The information about the comment in the document that will be scrolled to
    /// </summary>
    [JsonPropertyName("action")]
    public ActionConfig Action { get; set; }

    public static string Serialize(ActionLinkConfig actionLinkConfig)
    {
        return JsonSerializer.Serialize(actionLinkConfig);
    }

    public class ActionConfig
    {
        /// <summary>
    /// Comment data
    /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; }

        /// <summary>
    /// Action type
    /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}

public class CoEditingConfig
{
    /// <summary>
    /// Change
    /// </summary>
    public bool Change { get; set; }

    /// <summary>
    /// Fast
    /// </summary>
    public bool Fast { get; init; }

    /// <summary>
    /// Mode
    /// </summary>
    public CoEditingConfigMode Mode
    {
        get { return Fast ? CoEditingConfigMode.Fast : CoEditingConfigMode.Strict; }
    }
}

[EnumExtensions]
public enum CoEditingConfigMode
{
    [SwaggerEnum("Fast")]
    Fast,

    [SwaggerEnum("Strict")]
    Strict
}


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
        { FileType.Pdf, "pdf" }
    };

    private FileType _fileTypeCache = FileType.Unknown;

    /// <summary>Document config</summary>
    /// <type>ASC.Web.Files.Services.DocumentService.DocumentConfig, ASC.Files.Core</type>
    public DocumentConfig<T> Document { get; } = document;

    /// <summary>Document type</summary>
    /// <type>System.String, System</type>
    public string GetDocumentType(File<T> file)
    {
        DocType.TryGetValue(GetFileType(file), out var documentType);

        return documentType;
    }

    /// <summary>Editor config</summary>
    /// <type>ASC.Web.Files.Services.DocumentService.EditorConfiguration, ASC.Files.Core</type>
    public EditorConfiguration<T> EditorConfig { get; } = editorConfig;

    /// <summary>Editor type</summary>
    /// <type>ASC.Web.Files.Services.DocumentService.EditorType, ASC.Files.Core</type>
    public EditorType EditorType
    {
        set => Document.Info.Type = value;
        get => Document.Info.Type;
    }
    
    public string Error { get; set; }

    /// <summary>Platform type</summary>
    /// <type>System.String, System</type>
    public string Type
    {
        set => EditorType = Enum.Parse<EditorType>(value, true);
        get => EditorType.ToString().ToLower();
    }

    internal FileType GetFileType(File<T> file)
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
    private string _key = string.Empty;
    private FileReferenceData _referenceData;
    public string GetFileType(File<T> file) => file.ConvertedExtension.Trim('.');
    public InfoConfig<T> Info { get; } = infoConfig;
    public bool IsLinkedForMe { get; set; }

    public string Key
    {
        set => _key = value;
        get => DocumentServiceConnector.GenerateRevisionId(_key);
    }

    public PermissionsConfig Permissions { get; set; } = new();
    
	public Options Options { get; set; }
    public string SharedLinkParam { get; set; }
    public string SharedLinkKey { get; set; }
    public FileReferenceData GetReferenceData(File<T> file)
    {
        return _referenceData ??= new FileReferenceData
        {
            FileKey = file.Id.ToString(), 
            InstanceId = (tenantManager.GetCurrentTenantId()).ToString()
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
    PluginsConfig pluginsConfig,
    EmbeddedConfig embeddedConfig,
    CustomizationConfig<T> customizationConfig,
    FilesSettingsHelper filesSettingsHelper,
    IDaoFactory daoFactory,
    EntryManager entryManager,
    DocumentServiceTrackerHelper documentServiceTrackerHelper, 
    ExternalShare externalShare,
    UserPhotoManager userPhotoManager)
{
    public PluginsConfig Plugins { get; } = pluginsConfig;
    public CustomizationConfig<T> Customization { get; } = customizationConfig;
    public EncryptionKeysConfig EncryptionKeys { get; set; }

    public string Lang => UserInfo.GetCulture().Name;

    public string Mode => ModeWrite ? "edit" : "view";

    public bool ModeWrite { get; set; }
    
    private UserInfo _userInfo;
    private UserInfo UserInfo => _userInfo ??= userManager.GetUsers(authContext.CurrentAccount.ID);

    private UserConfig _user;
    public async Task<UserConfig> GetUserAsync()
    {
        if (_user != null)
        {
            return _user;

        }

        if (!UserInfo.Id.Equals(ASC.Core.Configuration.Constants.Guest.ID))
        {
            _user = new UserConfig
            {
                Id = UserInfo.Id.ToString(),
                Name = UserInfo.DisplayUserName(false, displayUserSettingsHelper),
                Image = baseCommonLinkUtility.GetFullAbsolutePath(await UserInfo.GetMediumPhotoURLAsync(userPhotoManager))
            };
        }

        return _user;
    }

    public async Task<string> GetCallbackUrl(File<T> file)
    {
        if (!ModeWrite)
        {
            return null;
        }

        var callbackUrl = documentServiceTrackerHelper.GetCallbackUrl(file.Id.ToString());

        if (file.ShareRecord is not { IsLink: true } || string.IsNullOrEmpty(file.ShareRecord.Options?.Password))
        {
            return externalShare.GetUrlWithShare(callbackUrl);
        }

        var key = await externalShare.CreateShareKeyAsync(file.ShareRecord.Subject, file.ShareRecord.Options?.Password);
        return externalShare.GetUrlWithShare(callbackUrl, key);
    }

    public async Task<CoEditingConfig> GetCoEditingAsync()
    {
        return !ModeWrite && await GetUserAsync() == null
            ? new CoEditingConfig
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

            default:
                return null;
        }

        Configuration<T>.DocType.TryGetValue(fileType, out var documentType);

        return baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FileHandlerPath)
               + "?" + FilesLinkUtility.Action + "=create"
               + "&doctype=" + documentType
               + "&" + FilesLinkUtility.FileTitle + "=" + HttpUtility.UrlEncode(title);
    }
    
    public EmbeddedConfig GetEmbedded(EditorType editorType)
    {
        return editorType == EditorType.Embedded ? embeddedConfig : null;
    }
    
    public async IAsyncEnumerable<RecentConfig> GetRecent(FileType fileType, T fileId)
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
            _ => FilterType.FilesOnly
        };

        var folderDao = daoFactory.GetFolderDao<int>();
        var files = (await entryManager.GetRecentAsync(filter, false, Guid.Empty, string.Empty, null, false))
            .Cast<File<int>>()
            .Where(file => file != null && !Equals(fileId, file.Id))
            .ToList();

        var parentIds = files.Select(r => r.ParentId).Distinct().ToList();
        var parentFolders = await folderDao.GetFoldersAsync(parentIds).ToListAsync();
        
        foreach (var file in files)
        {
            yield return new RecentConfig
            {
                Folder = parentFolders.FirstOrDefault(r => file.ParentId == r.Id)?.Title,
                Title = file.Title,
                Url = baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebEditorUrl(file.Id))
            };
        }
    }

    public async Task<List<TemplatesConfig>> GetTemplates(FileType fileType, string title)
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
                _ => FilterType.FilesOnly
            };

            var folderDao = daoFactory.GetFolderDao<int>();
            var fileDao = daoFactory.GetFileDao<int>();
            var files = await entryManager.GetTemplatesAsync(folderDao, fileDao, filter, false, Guid.Empty, string.Empty, null, false).ToListAsync();
            var listTemplates = from file in files
                                select
                                    new TemplatesConfig
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

    public async Task<List<AceShortWrapper>> GetSharingSettings(File<T> file)
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

public class PermissionsConfig
{
    /// <summary>
    /// Change history
    /// </summary>
    public bool ChangeHistory { get; set; }

    /// <summary>
    /// Comment
    /// </summary>
    public bool Comment { get; set; } = true;

    /// <summary>
    /// Chat
    /// </summary>
    public bool Chat { get; set; } = true;

    /// <summary>
    /// Download
    /// </summary>
    public bool Download { get; set; } = true;

    /// <summary>
    /// Edit
    /// </summary>
    public bool Edit { get; set; } = true;

    /// <summary>
    /// FillForms
    /// </summary>
    public bool FillForms { get; set; } = true;

    /// <summary>
    /// ModifyFilter
    /// </summary>
    public bool ModifyFilter { get; set; } = true;

    /// <summary>
    /// Protect
    /// </summary>
    public bool Protect { get; set; } = true;

    /// <summary>
    /// Print
    /// </summary>
    public bool Print { get; set; } = true;

    /// <summary>
    /// Rename
    /// </summary>
    public bool Rename { get; set; }

    /// <summary>
    /// Review
    /// </summary>
    public bool Review { get; set; } = true;

    /// <summary>
    /// Copy
    /// </summary>
    public bool Copy { get; set; } = true;
}

public class Options
{
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
        _ = stringBuilder.Append(string.Join(',',WatermarkOnDraw.Margins));
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

public class WatermarkOnDraw(double widthInPixels, double heightInPixels, string fill, int rotate, List<Paragraph> paragraphs)
{
    private const double DotsPerInch = 96;
    private const double DotsPerMm = DotsPerInch / 25.4;

    /// <summary>
    /// Defines the watermark width measured in millimeters.
    /// </summary>
    [JsonPropertyName("width")]
    public double Width { get; init; } = widthInPixels == 0 ? 100 : widthInPixels / DotsPerMm;

    /// <summary>
    /// Defines the watermark height measured in millimeters.
    /// </summary>
    [JsonPropertyName("height")]
    public double Height { get; init; } = heightInPixels == 0 ? 100 : heightInPixels / DotsPerMm;

    [JsonPropertyName("margins")]
    public int[] Margins { get; init; } = [0, 0, 0, 0];

    [JsonPropertyName("fill")]
    public string Fill { get; init; } = fill;

    [JsonPropertyName("rotate")]
    public int Rotate { get; init; } = rotate;

    [JsonPropertyName("transparent")]
    public double Transparent { get; init; } = 0.4;

    [JsonPropertyName("paragraphs")]
    public List<Paragraph> Paragraphs { get; init; } = paragraphs;
}

public class Paragraph
{
    public Paragraph(List<Run> runs)
    {
        Runs = runs;
        Align = 2;
    }
    [JsonPropertyName("align")]
    public int Align { get; set; }

    [JsonPropertyName("runs")]
    public List<Run> Runs { get; set; }
}
public class Run(string text, bool usedInHash = true)
{
    internal bool UsedInHash => usedInHash;

    [JsonPropertyName("fill")]
    public int[] Fill { get; set; } = [124, 124, 124];

    [JsonPropertyName("text")]
    public string Text { get; set; } = text;

    [JsonPropertyName("font-size")]
    public string FontSize { get; set; } = "26";
}

public class FileReference
{
    /// <summary>
    /// File reference data
    /// </summary>
    public FileReferenceData ReferenceData { get; set; }

    /// <summary>
    /// Error
    /// </summary>
    public string Error { get; set; }

    /// <summary>
    /// Path
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// URL
    /// </summary>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// File type
    /// </summary>
    public string FileType { get; set; }

    /// <summary>
    /// Key
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Link
    /// </summary>
    public string Link { get; set; }

    /// <summary>
    /// Token
    /// </summary>
    public string Token { get; set; }
}

public class FileReferenceData
{
    /// <summary>
    /// File key
    /// </summary>
    public string FileKey { get; set; }

    /// <summary>
    /// Instance ID
    /// </summary>
    public string InstanceId { get; set; }
}

#endregion Nested Classes

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
    CommonLinkUtility commonLinkUtility)
{
    [JsonIgnore]
    public string GobackUrl;

    public bool About => !coreBaseSettings.Standalone && !coreBaseSettings.CustomMode;

    public CustomerConfig Customer { get; set; } = customerConfig;

    public async Task<FeedbackConfig> GetFeedback()
    {
        if (coreBaseSettings.Standalone)
        {
            return null;
        }

        var link = await commonLinkUtility.GetFeedbackAndSupportLink(settingsManager);

        if (string.IsNullOrEmpty(link))
        {
            return null;
        }

        return new FeedbackConfig
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

    public async Task<GobackConfig> GetGoBack(EditorType editorType, File<T> file)
    {
        if (editorType == EditorType.Embedded)
        {
            return null;
        }

        if (!authContext.IsAuthenticated)
        {
            return null;
        }
        if (GobackUrl != null)
        {
            return new GobackConfig
            {
                Url = GobackUrl
            };
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        try
        {
            var parent = await folderDao.GetFolderAsync(file.ParentId);
            if (file.RootFolderType == FolderType.USER && 
                !Equals(file.RootId, await globalFolderHelper.FolderMyAsync) && 
                !await fileSecurity.CanReadAsync(parent))
            {
                if (await fileSecurity.CanReadAsync(file))
                {
                    return new GobackConfig
                    {
                        Url = await pathProvider.GetFolderUrlByIdAsync(await globalFolderHelper.FolderShareAsync)
                    };
                }

                return null;
            }

            if (file.Encrypted && 
                file.RootFolderType == FolderType.Privacy && 
                !await fileSecurity.CanReadAsync(parent))
            {
                parent = await folderDao.GetFolderAsync(await globalFolderHelper.GetFolderPrivacyAsync<T>());
            }

            return new GobackConfig
            {
                Url = await pathProvider.GetFolderUrlAsync(parent)
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
               && await FileSharing.CanSetAccessAsync(file);
    }

    public string GetReviewDisplay(bool modeWrite)
    {
        return modeWrite ? null : "markup";
    }

    public async Task<bool> GetSubmitForm(File<T> file)
    {

        var properties = await daoFactory.GetFileDao<T>().GetProperties(file.Id);
        return file.RootFolderType != FolderType.Archive && await fileSecurity.CanFillFormsAsync(file) && properties is { FormFilling.CollectFillForm: true };
    }

    private FileSharing FileSharing { get; } = fileSharing;
}

[Transient]
public class EmbeddedConfig(BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility)
{
    /// <summary>
    /// Embed url
    /// </summary>
    public string EmbedUrl => ShareLinkParam != null && ShareLinkParam.Contains(FilesLinkUtility.ShareKey, StringComparison.Ordinal) ? baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FilesBaseAbsolutePath + FilesLinkUtility.EditorPage + "?" + FilesLinkUtility.Action + "=embedded" + ShareLinkParam) : null;

    /// <summary>
    /// Save url
    /// </summary>
    public string SaveUrl => baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FileHandlerPath + "?" + FilesLinkUtility.Action + "=download" + ShareLinkParam);

    /// <summary>
    /// Share link param
    /// </summary>
    public string ShareLinkParam { get; set; }

    /// <summary>
    /// Share url
    /// </summary>
    public string ShareUrl => ShareLinkParam != null && ShareLinkParam.Contains(FilesLinkUtility.ShareKey) ? baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FilesBaseAbsolutePath + FilesLinkUtility.EditorPage + "?" + FilesLinkUtility.Action + "=view" + ShareLinkParam) : null;

    /// <summary>
    /// Toolbar docked
    /// </summary>
    public string ToolbarDocked => "top";
}

public class EncryptionKeysConfig
{
    /// <summary>
    /// Crypto engine id
    /// </summary>
    public string CryptoEngineId => "{FFF0E1EB-13DB-4678-B67D-FF0A41DBBCEF}";

    /// <summary>
    /// Private key enc
    /// </summary>
    public string PrivateKeyEnc { get; set; }

    /// <summary>
    /// Public key
    /// </summary>
    public string PublicKey { get; set; }
}

public class FeedbackConfig
{
    /// <summary>
    /// Url
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Visible
    /// </summary>
    public bool Visible { get => true; }
}

public class GobackConfig
{
    /// <summary>
    /// Url
    /// </summary>
    public string Url { get; set; }
}

[Transient]
public class LogoConfig(
    CommonLinkUtility commonLinkUtility,
    TenantLogoHelper tenantLogoHelper)
{

    public async Task<string> GetImage(EditorType editorType)
    {
        return editorType == EditorType.Embedded
                ? commonLinkUtility.GetFullAbsolutePath(await tenantLogoHelper.GetLogo(WhiteLabelLogoType.DocsEditorEmbed))
                : commonLinkUtility.GetFullAbsolutePath(await tenantLogoHelper.GetLogo(WhiteLabelLogoType.DocsEditor));
    }

    public async Task<string> GetImageDark()
    {
        return commonLinkUtility.GetFullAbsolutePath(await tenantLogoHelper.GetLogo(WhiteLabelLogoType.DocsEditor));
    }

    public async Task<string> GetImageEmbedded(EditorType editorType)
    {
        return editorType != EditorType.Embedded
                ? null
                : commonLinkUtility.GetFullAbsolutePath(await tenantLogoHelper.GetLogo(WhiteLabelLogoType.DocsEditorEmbed));
    }

    public string Url
    {
        get => commonLinkUtility.GetFullAbsolutePath(commonLinkUtility.GetDefault());
    }

    public bool GetVisible(EditorType editorType)
    {
        return editorType != EditorType.Mobile;
    }
}

[Transient]
public class PluginsConfig
    // ConsumerFactory consumerFactory,
    // BaseCommonLinkUtility baseCommonLinkUtility,
    // CoreBaseSettings coreBaseSettings,
    // TenantManager tenantManager)
{
    // private readonly BaseCommonLinkUtility _baseCommonLinkUtility = baseCommonLinkUtility;
    //
    // private readonly ConsumerFactory _consumerFactory = consumerFactory;
    //
    // private readonly CoreBaseSettings _coreBaseSettings = coreBaseSettings;
    // private readonly TenantManager _tenantManager = tenantManager;

    public string[] PluginsData
    {
        get
        {
            //var plugins = new List<string>();

            //if (_coreBaseSettings.Standalone || !_tenantManager.GetCurrentTenantQuota().Free)
            //{
            //    var easyBibHelper = _consumerFactory.Get<EasyBibHelper>();
            //    if (!string.IsNullOrEmpty(easyBibHelper.AppKey))
            //    {
            //        plugins.Add(_baseCommonLinkUtility.GetFullAbsolutePath("ThirdParty/plugin/easybib/config.json"));
            //    }

            //    var wordpressLoginProvider = _consumerFactory.Get<WordpressLoginProvider>();
            //    if (!string.IsNullOrEmpty(wordpressLoginProvider.ClientID) &&
            //        !string.IsNullOrEmpty(wordpressLoginProvider.ClientSecret) &&
            //        !string.IsNullOrEmpty(wordpressLoginProvider.RedirectUri))
            //    {
            //        plugins.Add(_baseCommonLinkUtility.GetFullAbsolutePath("ThirdParty/plugin/wordpress/config.json"));
            //    }
            //}

            //return plugins.ToArray();

            return [];
        }
    }
}

public class RecentConfig
{
    /// <summary>
    /// Folder
    /// </summary>
    public string Folder { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Url
    /// </summary>
    [Url]
    public string Url { get; set; }
}

public class TemplatesConfig
{
    /// <summary>
    /// Image
    /// </summary>
    public string Image { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Url
    /// </summary>
    [Url]
    public string Url { get; set; }
}

public class UserConfig
{
    /// <summary>
    /// Id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Image
    /// </summary>
    public string Image { get; set; }
}