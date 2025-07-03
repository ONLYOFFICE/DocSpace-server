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

namespace ASC.Web.Files.Services.DocumentService;

/// <summary>
/// The editor type.
/// </summary>
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

/// <summary>
/// The config parameter which contains the information about the action in the document that will be scrolled to.
/// </summary>
public class ActionLinkConfig
{
    /// <summary>
    /// The information about the action in the document that will be scrolled to.
    /// </summary>
    [JsonPropertyName("action")]
    public ActionConfig Action { get; set; }

    public static string Serialize(ActionLinkConfig actionLinkConfig)
    {
        return JsonSerializer.Serialize(actionLinkConfig);
    }

    /// <summary>
    /// The information about the action in the document that will be scrolled to.
    /// </summary>
    public class ActionConfig
    {
        /// <summary>
        /// The action data that will be scrolled to.
        /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; }

        /// <summary>
        /// The action type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}

/// <summary>
/// The co-editing configuration parameters.
/// </summary>
public class CoEditingConfig
{
    /// <summary>
    /// Specifies if the co-editing mode can be changed in the editor interface or not. 
    /// </summary>
    public bool Change { get; set; }

    /// <summary>
    /// Specifies if the co-editing mode is fast.
    /// </summary>
    public bool Fast { get; init; }

    /// <summary>
    /// The co-editing mode (fast or strict).
    /// </summary>
    public CoEditingConfigMode Mode
    {
        get { return Fast ? CoEditingConfigMode.Fast : CoEditingConfigMode.Strict; }
    }
}

/// <summary>
/// The co-editing mode (fast or strict).
/// </summary>
[EnumExtensions]
public enum CoEditingConfigMode
{
    [SwaggerEnum("Fast")]
    Fast,

    [SwaggerEnum("Strict")]
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
            FileType.Diagram => FilterType.DiagramsOnly,
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
                FileType.Diagram => FilterType.DiagramsOnly,
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

/// <summary>
/// The permissions configuration parameters.
/// </summary>
public class PermissionsConfig
{
    /// <summary>
    /// Defines if the document can be commented or not.
    /// </summary>
    public bool Comment { get; set; } = true;

    /// <summary>
    /// Defines if the chat functionality is enabled in the document or not.
    /// </summary>
    public bool Chat { get; set; } = true;

    /// <summary>
    /// Defines if the document can be downloaded or only viewed or edited online.
    /// </summary>
    public bool Download { get; set; } = true;

    /// <summary>
    /// Defines if the document can be edited or only viewed.
    /// </summary>
    public bool Edit { get; set; } = true;

    /// <summary>
    /// Defines if the forms can be filled.
    /// </summary>
    public bool FillForms { get; set; } = true;

    /// <summary>
    /// Defines if the filter can be applied globally (true) affecting all the other users,
    /// or locally (false), i.e. for the current user only. 
    /// </summary>
    public bool ModifyFilter { get; set; } = true;

    /// <summary>
    /// Defines if the "Protection" tab on the toolbar and the "Protect" button in the left menu are displayedor hidden.
    /// </summary>
    public bool Protect { get; set; } = true;

    /// <summary>
    /// Defines if the document can be printed or not.
    /// </summary>
    public bool Print { get; set; } = true;

    /// <summary>
    /// Specifies whether to display the "Rename..." button when using the "onRequestRename" event.
    /// </summary>
    public bool Rename { get; set; }

    /// <summary>
    /// Defines if the document can be reviewed or not.
    /// </summary>
    public bool Review { get; set; } = true;

    /// <summary>
    /// Defines if the content can be copied to the clipboard or not.
    /// </summary>
    public bool Copy { get; set; } = true;
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
    [JsonPropertyName("width")]
    public double Width { get; init; } = widthInPixels == 0 ? 100 : widthInPixels / DotsPerMm;

    /// <summary>
    /// Defines the watermark height measured in millimeters.
    /// </summary>
    [JsonPropertyName("height")]
    public double Height { get; init; } = heightInPixels == 0 ? 100 : heightInPixels / DotsPerMm;

    /// <summary>
    /// Defines the watermark margins measured in millimeters.
    /// </summary>
    [JsonPropertyName("margins")]
    public int[] Margins { get; init; } = [0, 0, 0, 0];

    /// <summary>
    /// Defines the watermark fill color.
    /// </summary>
    [JsonPropertyName("fill")]
    public string Fill { get; init; } = fill;

    /// <summary>
    /// Defines the watermark rotation angle.
    /// </summary>
    [JsonPropertyName("rotate")]
    public int Rotate { get; init; } = rotate;

    /// <summary>
    /// Defines the watermark transparency percentage.
    /// </summary>
    [JsonPropertyName("transparent")]
    public double Transparent { get; init; } = 0.4;

    /// <summary>
    /// The list of paragraphs of the watermark.
    /// </summary>
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
    [JsonPropertyName("align")]
    public int Align { get; set; }

    /// <summary>
    /// The list of text runs from the paragraph.
    /// </summary>
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
    [JsonPropertyName("fill")]
    public int[] Fill { get; set; } = [124, 124, 124];

    /// <summary>
    /// The run text.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = text;

    /// <summary>
    /// The font size of the text run in points.
    /// </summary>
    [JsonPropertyName("font-size")]
    public string FontSize { get; set; } = "26";
}

/// <summary>
/// The file reference parameters.
/// </summary>
public class FileReference
{
    /// <summary>
    /// An object that is generated by the integrator to uniquely identify a file in its system.
    /// </summary>
    public FileReferenceData ReferenceData { get; set; }

    /// <summary>
    /// The error message text.
    /// </summary>
    public string Error { get; set; }

    /// <summary>
    /// The file name or relative path for the formula editor.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// The URL address to download the current file.
    /// </summary>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// An extension of the document specified with the url parameter.
    /// </summary>
    public string FileType { get; set; }

    /// <summary>
    /// The unique document identifier used by the service to take the data from the co-editing session.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The file URL.
    /// </summary>
    public string Link { get; set; }

    /// <summary>
    /// The encrypted signature added to the parameter in the form of a token.
    /// </summary>
    public string Token { get; set; }
}

/// <summary>
/// An object that is generated by the integrator to uniquely identify a file in its system.
/// </summary>
public class FileReferenceData
{
    /// <summary>
    /// The unique document identifier used by the service to get a link to the file.
    /// </summary>
    public string FileKey { get; set; }

    /// <summary>
    /// The unique system identifier.
    /// </summary>
    public string InstanceId { get; set; }

    /// <summary>
    /// Room ID
    /// </summary>
    public string RoomId { get; set; }

    /// <summary>
    /// Specifies if the room can be edited out or not.
    /// </summary>
    public bool CanEditRoom { get; set; }
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

    public bool About => !coreBaseSettings.Standalone && !coreBaseSettings.CustomMode;

    public CustomerConfig Customer { get; set; } = customerConfig;

    public async Task<FeedbackConfig> GetFeedback()
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
        
        if (GobackUrl != null)
        {
            return new GobackConfig
            {
                Url = GobackUrl
            };
        }
        
        Folder<T> parent;
        var folderDao = daoFactory.GetFolderDao<T>();
        var (shareRight, key) = await CheckLinkAsync(file);
        
        if (!authContext.IsAuthenticated)
        {
            if (shareRight != FileShare.Restrict && !string.IsNullOrEmpty(key))
            {           
                parent = await folderDao.GetFolderAsync(file.ParentId);
                return new GobackConfig
                {
                    Url = pathProvider.GetFolderUrl(parent, key)
                };
            }
        }
        
        try
        {

            parent = await folderDao.GetFolderAsync(file.ParentId);
            if (file.RootFolderType == FolderType.USER && 
                !Equals(file.RootId, await globalFolderHelper.FolderMyAsync) && 
                !await fileSecurity.CanReadAsync(parent))
            {
                if (await fileSecurity.CanReadAsync(file))
                {
                    return new GobackConfig
                    {
                        Url = await pathProvider.GetFolderUrlByIdAsync(await globalFolderHelper.FolderRecentAsync, key)
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
                Url =  pathProvider.GetFolderUrl(parent, key)
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

    public string GetReviewDisplay(bool modeWrite)
    {
        return modeWrite ? null : "markup";
    }

    public async Task<SubmitForm> GetSubmitForm(File<T> file)
    {
        var properties = await daoFactory.GetFileDao<T>().GetProperties(file.Id);
        return new SubmitForm
        {
            Visible = file.RootFolderType != FolderType.Archive && await fileSecurity.CanFillFormsAsync(file) && (properties is { FormFilling.StartFilling: true } or { FormFilling.CollectFillForm: true }),
            ResultMessage = ""
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

/// <summary>
/// The configuration parameters for the embedded document type.
/// </summary>
[Transient]
public class EmbeddedConfig(BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility)
{
    private string _embedUrl;
    private string _shareUrl;
    /// <summary>
    /// The absolute URL to the document serving as a source file for the document embedded into the web page.
    /// </summary>
    public string EmbedUrl
    {
        get
        {
            return _embedUrl ?? (ShareLinkParam != null && ShareLinkParam.Contains(FilesLinkUtility.ShareKey, StringComparison.Ordinal) ? baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FilesBaseAbsolutePath + FilesLinkUtility.EditorPage + "?" + FilesLinkUtility.Action + "=embedded" + ShareLinkParam) : null);
        }
        set => _embedUrl = value;
    }

    /// <summary>
    /// The absolute URL that will allow the document to be saved onto the user personal computer.
    /// </summary>
    public string SaveUrl => baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FileHandlerPath + "?" + FilesLinkUtility.Action + "=download" + ShareLinkParam);

    /// <summary>
    /// The shared URL parameter.
    /// </summary>
    public string ShareLinkParam { get; set; }

    /// <summary>
    /// The absolute URL that will allow other users to share this document.
    /// </summary>
    public string ShareUrl
    {
        get
        {
            return _shareUrl ?? (ShareLinkParam != null && ShareLinkParam.Contains(FilesLinkUtility.ShareKey) ? baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FilesBaseAbsolutePath + FilesLinkUtility.EditorPage + "?" + FilesLinkUtility.Action + "=view" + ShareLinkParam) : null);
        }
        set => _shareUrl = value;
    }
    /// <summary>
    /// The place for the embedded viewer toolbar, can be either "top" or "bottom".
    /// </summary>
    public string ToolbarDocked => "top";
}

/// <summary>
/// The encryption keys of the editor configuration.
/// </summary>
public class EncryptionKeysConfig
{
    /// <summary>
    /// The crypto engine ID of the encryption key.
    /// </summary>
    public string CryptoEngineId => "{FFF0E1EB-13DB-4678-B67D-FF0A41DBBCEF}";

    /// <summary>
    /// The private key.
    /// </summary>
    public string PrivateKeyEnc { get; set; }

    /// <summary>
    /// The public key.
    /// </summary>
    public string PublicKey { get; set; }
}

/// <summary>
/// The settings for the "Feedback &amp; Support" menu button.
/// </summary>
public class FeedbackConfig
{
    /// <summary>
    /// The absolute URL to the website address which will be opened when clicking the "Feedback &amp; Support" menu button.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Shows or hides the "Feedback &amp; Support" menu button.
    /// </summary>
    public bool Visible { get => true; }
}

/// <summary>
/// The settings for the "Open file location" menu button and upper right corner button.
/// </summary>
public class GobackConfig
{
    /// <summary>
    /// The absolute URL to the website address which will be opened when clicking the "Open file location" menu button.
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

    public async Task<string> GetImageLight()
    {
        return commonLinkUtility.GetFullAbsolutePath(await tenantLogoHelper.GetLogo(WhiteLabelLogoType.DocsEditorEmbed));
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

/// <summary>
/// The configuration settings to connect the special add-ons.
/// </summary>
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

    /// <summary>
    /// The array of absolute URLs to the plugin configuration files.
    /// </summary>
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

/// <summary>
/// The presence or absence of the documents in the "Open Recent..." menu option.
/// </summary>
public class RecentConfig
{
    /// <summary>
    /// The folder where the document is stored.
    /// </summary>
    public string Folder { get; set; }

    /// <summary>
    /// The document title that will be displayed in the Open Recent... menu option.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The absolute URL to the document where it is stored.
    /// </summary>
    [Url]
    public string Url { get; set; }
}

/// <summary>
/// The presence or absence of the templates in the "Create New..." menu option.
/// </summary>
public class TemplatesConfig
{
    /// <summary>
    /// The absolute URL to the image for template.
    /// </summary>
    public string Image { get; set; }

    /// <summary>
    /// The template title that will be displayed in the "Create New..." menu option.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The absolute URL to the document where it will be created and available after creation.
    /// </summary>
    [Url]
    public string Url { get; set; }
}

/// <summary>
/// The configuration parameters of the user currently viewing or editing the document.
/// </summary>
public class UserConfig
{
    /// <summary>
    /// The user ID.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The full name of the user.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The path to the user's avatar.
    /// </summary>
    public string Image { get; set; }

    /// <summary>
    /// Roles
    /// </summary>
    public List<string> Roles { get; set; }
}