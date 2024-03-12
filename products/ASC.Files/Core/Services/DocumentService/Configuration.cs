// (c) Copyright Ascensio System SIA 2010-2023
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
    Desktop,
    Mobile,
    Embedded
}

/// <summary>
/// </summary>
public class ActionLinkConfig
{
    /// <summary>The information about the comment in the document that will be scrolled to</summary>
    [JsonPropertyName("action")]
    public ActionConfig Action { get; set; }

    public static string Serialize(ActionLinkConfig actionLinkConfig)
    {
        return JsonSerializer.Serialize(actionLinkConfig);
    }

    /// <summary>
    /// </summary>
    public class ActionConfig
    {
        /// <summary>Comment data</summary>
        [JsonPropertyName("data")]
        public string Data { get; set; }

        /// <summary>Action type</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}

public class CoEditingConfig
{
    public bool Change { get; set; }
    public bool Fast { get; init; }

    public string Mode
    {
        get { return Fast ? "fast" : "strict"; }
    }
}

[Scope]
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
        set => EditorType = (EditorType)Enum.Parse(typeof(EditorType), value, true);
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

[Transient]
public class DocumentConfig<T>(
    DocumentServiceConnector documentServiceConnector, 
    PathProvider pathProvider, 
    InfoConfig<T> infoConfig, 
    TenantManager tenantManager)
{
    private string _fileUri;
    private string _key = string.Empty;
    private FileReferenceData<T> _referenceData;
    public string GetFileType(File<T> file) => file.ConvertedExtension.Trim('.');
    public InfoConfig<T> Info { get; set; } = infoConfig;
    public bool IsLinkedForMe { get; set; }

    public string Key
    {
        set => _key = value;
        get => DocumentServiceConnector.GenerateRevisionId(_key);
    }

    public PermissionsConfig Permissions { get; set; } = new();
    public string SharedLinkParam { get; set; }
    public string SharedLinkKey { get; set; }
    public async Task<FileReferenceData<T>> GetReferenceData(File<T> file)
    {
        return _referenceData ??= new FileReferenceData<T>
        {
            FileKey = file.Id, 
            InstanceId = (await tenantManager.GetCurrentTenantIdAsync()).ToString()
        };
    }

    public string Title { get; set; }

    public async Task SetUrl(string val)
    {
        _fileUri = await documentServiceConnector.ReplaceCommunityAddressAsync(val);
    }
    
    public async Task<string> GetUrl(File<T> file)
    {
        if (!string.IsNullOrEmpty(_fileUri))
        {
            return _fileUri;
        }

        var last = Permissions.Edit || Permissions.Review || Permissions.Comment;
        _fileUri = await documentServiceConnector.ReplaceCommunityAddressAsync(pathProvider.GetFileStreamUrl(file, SharedLinkKey, SharedLinkParam, last));

        return _fileUri;
    }
}

[Transient]
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
    ExternalShare externalShare)
{
    public PluginsConfig Plugins { get; } = pluginsConfig;
    public CustomizationConfig<T> Customization { get; } = customizationConfig;
    public EncryptionKeysConfig EncryptionKeys { get; set; }

    public string Lang => UserInfo.GetCulture().Name;

    public string Mode => ModeWrite ? "edit" : "view";

    public bool ModeWrite { get; set; }
    
    private UserInfo _userInfo;
    private UserInfo UserInfo => _userInfo ??= userManager.GetUsers(authContext.CurrentAccount.ID);
    
    private UserConfig user;
    public UserConfig User
    {
        get
        {
            if (user != null)
            {
                return user;
                
            }
            
            if (!UserInfo.Id.Equals(ASC.Core.Configuration.Constants.Guest.ID))
            {
                user = new UserConfig
                {
                    Id = UserInfo.Id.ToString(),
                    Name = UserInfo.DisplayUserName(false, displayUserSettingsHelper)
                };
            }

            return user;
        }
    }

    public async Task<string> GetCallbackUrl(string fileId)
    {
        if (!ModeWrite)
        {
            return null;
        }

        var callbackUrl = await documentServiceTrackerHelper.GetCallbackUrlAsync(fileId);

        return externalShare.GetUrlWithShare(callbackUrl);
    }

    public CoEditingConfig CoEditing =>
        !ModeWrite && User == null
            ? new CoEditingConfig
            {
                Fast = false,
                Change = false
            }
            : null;

    public async Task<string> GetCreateUrl(EditorType editorType, FileType fileType)
    {
        if (editorType != EditorType.Desktop)
        {
            return null;
        }

        if (!authContext.IsAuthenticated || await userManager.IsUserAsync(authContext.CurrentAccount.ID))
        {
            return null;
        }
        string title;
        switch (fileType)
        {
            case FileType.Document:
            case FileType.OForm:
            case FileType.OFormTemplate:
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
        if (!authContext.IsAuthenticated || await userManager.IsUserAsync(authContext.CurrentAccount.ID))
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
            FileType.OForm => FilterType.OFormOnly,
            FileType.OFormTemplate => FilterType.OFormTemplateOnly,
            FileType.Spreadsheet => FilterType.SpreadsheetsOnly,
            FileType.Presentation => FilterType.PresentationsOnly,
            _ => FilterType.FilesOnly
        };

        var folderDao = daoFactory.GetFolderDao<int>();
        var files = (await entryManager.GetRecentAsync(filter, false, Guid.Empty, string.Empty, null, false))
            .Cast<File<int>>()
            .Where(file => !Equals(fileId, file.Id))
            .ToList();

        var parentIds = files.Select(r => r.ParentId).Distinct().ToList();
        var parentFolders = await folderDao.GetFoldersAsync(parentIds).ToListAsync();
        
        foreach (var file in files)
        {
            yield return new RecentConfig
            {
                Folder = parentFolders.Find(r => file.ParentId == r.Id).Title,
                Title = file.Title,
                Url = baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebEditorUrl(file.Id))
            };
        }
    }

    public async Task<List<TemplatesConfig>> GetTemplates(FileType fileType, string title)
    {
            if (!authContext.IsAuthenticated || await userManager.IsUserAsync(authContext.CurrentAccount.ID))
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
                FileType.OForm => FilterType.OFormOnly,
                FileType.OFormTemplate => FilterType.OFormTemplateOnly,
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

[Transient]
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

        if (!securityContext.IsAuthenticated || await userManager.IsUserAsync(securityContext.CurrentAccount.ID))
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
            return await fileSharing.GetSharedInfoShortFileAsync(file.Id);
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
    public bool ChangeHistory { get; set; }
    public bool Comment { get; set; } = true;
    public bool Download { get; set; } = true;
    public bool Edit { get; set; } = true;
    public bool FillForms { get; set; } = true;
    public bool ModifyFilter { get; set; } = true;
    public bool Print { get; set; } = true;
    public bool Rename { get; set; }
    public bool Review { get; set; } = true;
    public bool Copy { get; set; } = true;
}

/// <summary>
/// </summary>
public class FileReference<T>
{
    /// <summary>File reference data</summary>
    /// <type>ASC.Web.Files.Services.DocumentService.FileReferenceData, ASC.Files.Core</type>
    public FileReferenceData<T> ReferenceData { get; set; }

    /// <summary>Error</summary>
    /// <type>System.String, System</type>
    public string Error { get; set; }

    /// <summary>Path</summary>
    /// <type>System.String, System</type>
    public string Path { get; set; }

    /// <summary>URL</summary>
    /// <type>System.String, System</type>
    public string Url { get; set; }

    /// <summary>File type</summary>
    /// <type>System.String, System</type>
    public string FileType { get; set; }

    /// <summary>Key</summary>
    /// <type>System.String, System</type>
    public string Key { get; set; }

    /// <summary>Link</summary>
    /// <type>System.String, System</type>
    public string Link { get; set; }

    /// <summary>Token</summary>
    /// <type>System.String, System</type>
    public string Token { get; set; }
}

/// <summary>
/// </summary>
public class FileReferenceData<T>
{
    /// <summary>File key</summary>
    /// <type>System.Int32, System</type>
    public T FileKey { get; set; }

    /// <summary>Instance ID</summary>
    /// <type>System.String, System</type>
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

    public async Task<string> GetLogo() => baseCommonLinkUtility.GetFullAbsolutePath(await tenantWhiteLabelSettingsHelper.GetAbsoluteDefaultLogoPathAsync(WhiteLabelLogoType.LoginPage, false));

    public async Task<string> GetMail() => (await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>()).Email;

    public async Task<string> GetName() => (await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>()).CompanyName;

    public async Task<string> GetWww() => (await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>()).Site;
}

[Transient]
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

    public async Task<bool> GetSubmitForm(File<T> file, bool modeWrite)
    {
        if (!modeWrite || FileUtility.GetFileTypeByFileName(file.Title) != FileType.Pdf)
        {
            return false;
        }

        var linkDao = daoFactory.GetLinkDao();
        var sourceId = await linkDao.GetSourceAsync(file.Id.ToString());

        if (sourceId == null)
        {
            return false;
        }

        var properties = int.TryParse(sourceId, out var sourceInt)
            ? await daoFactory.GetFileDao<int>().GetProperties(sourceInt)
            : await daoFactory.GetFileDao<string>().GetProperties(sourceId);

        return properties is { FormFilling.CollectFillForm: true };
    }

    private FileSharing FileSharing { get; } = fileSharing;
}

[Transient]
public class EmbeddedConfig(BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility)
{
    public string EmbedUrl => baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FilesBaseAbsolutePath + FilesLinkUtility.EditorPage + "?" + FilesLinkUtility.Action + "=embedded" + ShareLinkParam);

    public string SaveUrl => baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FileHandlerPath + "?" + FilesLinkUtility.Action + "=download" + ShareLinkParam);

    public string ShareLinkParam { get; set; }

    public string ShareUrl => baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FilesBaseAbsolutePath + FilesLinkUtility.EditorPage + "?" + FilesLinkUtility.Action + "=view" + ShareLinkParam);

    public string ToolbarDocked => "top";
}

public class EncryptionKeysConfig
{
    public string CryptoEngineId => "{FFF0E1EB-13DB-4678-B67D-FF0A41DBBCEF}";
    public string PrivateKeyEnc { get; set; }
    public string PublicKey { get; set; }
}

public class FeedbackConfig
{
    public string Url { get; set; }
    public bool Visible { get => true; }
}

public class GobackConfig
{
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
}

[Transient]
public class PluginsConfig()
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

            return Array.Empty<string>();
        }
    }
}

public class RecentConfig
{
    public string Folder { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
}

public class TemplatesConfig
{
    public string Image { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
}

public class UserConfig
{
    public string Id { get; set; }
    public string Name { get; set; }
}

public static class ConfigurationFilesExtension
{
    public static void Register(DIHelper services)
    {
        services.TryAdd<Configuration<int>>();
        services.TryAdd<Configuration<string>>();
        
        services.TryAdd<DocumentConfig<string>>();
        services.TryAdd<DocumentConfig<int>>();

        services.TryAdd<InfoConfig<string>>();
        services.TryAdd<InfoConfig<int>>();

        services.TryAdd<EditorConfiguration<string>>();
        services.TryAdd<EditorConfiguration<int>>();

        services.TryAdd<EmbeddedConfig>();

        services.TryAdd<CustomizationConfig<string>>();
        services.TryAdd<CustomizationConfig<int>>();

        services.TryAdd<CustomerConfig>();
        services.TryAdd<CustomerConfig>();

        services.TryAdd<LogoConfig>();
        services.TryAdd<LogoConfig>();
    }
}