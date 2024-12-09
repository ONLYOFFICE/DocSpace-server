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

public class ConfigurationDto<T>
{
    /// <summary>
    /// Document config
    /// </summary>
    public DocumentConfigDto Document { get; set; }

    /// <summary>
    /// Document type
    /// </summary>
    public string DocumentType { get; set; }

    /// <summary>
    /// Editor config
    /// </summary>
    public EditorConfigurationDto EditorConfig { get; set; }

    /// <summary>
    /// Editor type
    /// </summary>
    public EditorType EditorType { get; set; }

    /// <summary>
    /// Editor URL
    /// </summary>
    [Url]
    public string EditorUrl { get; set; }

    /// <summary>
    /// Token
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Platform type
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// File parameters
    /// </summary>
    public FileDto<T> File { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Specifies if the filling has started or not
    /// </summary>
    public bool? StartFilling { get; set; }

    /// <summary>
    /// Filling session Id
    /// </summary>
    public string FillingSessionId { get; set; }
}

public class EditorConfigurationDto
{
    /// <summary>
    /// Callback url
    /// </summary>
    [Url]
    public string CallbackUrl { get; set; }

    /// <summary>
    /// Co editing
    /// </summary>
    public CoEditingConfig CoEditing { get; set; }

    /// <summary>
    /// Create url
    /// </summary>
    public string CreateUrl { get; set; }

    /// <summary>
    /// Customization
    /// </summary>
    public CustomizationConfigDto Customization { get; set; }

    /// <summary>
    /// Embedded
    /// </summary>
    public EmbeddedConfig Embedded { get; set; }

    /// <summary>
    /// Encryption keys
    /// </summary>
    public EncryptionKeysConfig EncryptionKeys { get; set; }

    /// <summary>
    /// Lang
    /// </summary>
    public string Lang { get; set; }

    /// <summary>
    /// Mode
    /// </summary>
    public string Mode { get; set; }

    /// <summary>
    /// Mode write
    /// </summary>
    public bool ModeWrite { get; set; }

    /// <summary>
    /// Plugins
    /// </summary>
    public PluginsConfig Plugins { get; set; }

    /// <summary>
    /// Recent
    /// </summary>
    public List<RecentConfig> Recent { get; set; }

    /// <summary>
    /// Templates
    /// </summary>
    public List<TemplatesConfig> Templates { get; set; }

    /// <summary>
    /// User
    /// </summary>
    public UserConfig User { get; set; }
}
public class CustomizationConfigDto
{
    /// <summary>
    /// About
    /// </summary>
    public bool About { get; set; }

    /// <summary>
    /// Customer
    /// </summary>
    public CustomerConfigDto Customer { get; set; }

    /// <summary>
    /// Anonymous
    /// </summary>
    public AnonymousConfigDto Anonymous { get; set; }

    /// <summary>
    /// Feedback
    /// </summary>
    public FeedbackConfig Feedback  { get; set; }

    /// <summary>
    /// Forcesave
    /// </summary>
    public bool? Forcesave { get; set; }

    /// <summary>
    /// Go back
    /// </summary>
    public GobackConfig Goback { get; set; }

    /// <summary>
    /// Logo
    /// </summary>
    public LogoConfigDto Logo { get; set; }

    /// <summary>
    /// MentionShare
    /// </summary>
    public bool MentionShare { get; set; }

    /// <summary>
    /// Review display
    /// </summary>
    public string ReviewDisplay { get; set; }

    /// <summary>
    /// Submit form
    /// </summary>
    public bool SubmitForm { get; set; }
}

public class LogoConfigDto
{
    /// <summary>
    /// Image
    /// </summary>
    public string Image { get; set; }

    /// <summary>
    /// Image dark
    /// </summary>
    public string ImageDark { get; set; }

    /// <summary>
    /// Image embedded
    /// </summary>
    public string ImageEmbedded { get; set; }

    /// <summary>
    /// Url
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Visible
    /// </summary>
    public bool Visible { get; set; }
}

public class AnonymousConfigDto
{
    /// <summary>
    /// Request
    /// </summary>
    public bool Request { get; set; }
}

public class CustomerConfigDto
{
    /// <summary>
    /// Address
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// Logo
    /// </summary>
    public string Logo { get; set; }

    /// <summary>
    /// Dark logo
    /// </summary>
    public string LogoDark { get; set; }

    /// <summary>
    /// Mail
    /// </summary>
    public string Mail { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name  { get; set; }

    /// <summary>
    /// Site
    /// </summary>
    public string Www  { get; set; }
}

public class DocumentConfigDto
{
    /// <summary>
    /// File type
    /// </summary>
    public string FileType  { get; set; }

    /// <summary>
    /// Info
    /// </summary>
    public InfoConfigDto Info { get; set; }

    /// <summary>
    /// Is linked for me
    /// </summary>
    public bool IsLinkedForMe { get; set; }

    /// <summary>
    /// Key
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Permissions
    /// </summary>
    public PermissionsConfig Permissions { get; set; }

    /// <summary>
    /// Shared link param
    /// </summary>
    public string SharedLinkParam { get; set; }

    /// <summary>
    /// Shared link key
    /// </summary>
    public string SharedLinkKey { get; set; }

    /// <summary>
    /// Reference data
    /// </summary>
    public FileReferenceData ReferenceData { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Url
    /// </summary>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// Options
    /// </summary>
    public Options Options { get; set; }
}

public class InfoConfigDto
{
    /// <summary>
    /// Favorite
    /// </summary>
    public bool? Favorite { get; set; }

    /// <summary>
    /// Folder
    /// </summary>
    public string Folder { get; set; }

    /// <summary>
    /// Owner
    /// </summary>
    public string Owner { get; set; }

    /// <summary>
    /// Sharing settings
    /// </summary>
    public List<AceShortWrapper> SharingSettings{ get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public EditorType Type { get; set; }

    /// <summary>
    /// Uploaded
    /// </summary>
    public string Uploaded { get; set; }
}

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class ConfigurationConverter<T>(
    CommonLinkUtility commonLinkUtility, 
    FilesLinkUtility filesLinkUtility, 
    FileDtoHelper fileDtoHelper,
    EditorConfigurationConverter<T> editorConfigurationConverter,
    DocumentConfigConverter<T> documentConfigConverter,
    DocumentServiceHelper documentServiceHelper,
    ExternalShare externalShare)
{
    public async Task<ConfigurationDto<T>> Convert(Configuration<T> source, File<T> file)
    {   
        if (source == null)
        {
            return null;
        }

        var result = new ConfigurationDto<T>
        {
            Document = await documentConfigConverter.Convert(source.Document, file),
            DocumentType = source.GetDocumentType(file),
            EditorConfig = await editorConfigurationConverter.Convert(source, file),
            EditorType = source.EditorType,
            EditorUrl = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.DocServiceApiUrl),
            ErrorMessage = source.Error
        };
        
        result.EditorUrl = FilesLinkUtility.AddQueryString(result.EditorUrl, new Dictionary<string, string> {
            { FilesLinkUtility.ShardKey, result.Document?.Key }
        });

        result.Token = documentServiceHelper.GetSignature(result);
        result.File = await fileDtoHelper.GetAsync(file);
        result.Type = source.Type;

        if (source.EditorType == EditorType.Embedded)
        {
            var shareParam = file.ShareRecord != null
                ? $"&{FilesLinkUtility.ShareKey}={await externalShare.CreateShareKeyAsync(file.ShareRecord.Subject)}"
                : "";

            result.EditorConfig.Embedded.ShareLinkParam = $"&{FilesLinkUtility.FileId}={file.Id}{shareParam}";
        }
        return result;
    }
}

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class EditorConfigurationConverter<T>(CustomizationConfigConverter<T> configConverter)
{
    public async Task<EditorConfigurationDto> Convert(Configuration<T> configuration, File<T> file)
    {
        var source = configuration.EditorConfig;
        
        if (source == null)
        {
            return null;
        }

        var fileType = configuration.GetFileType(file);
        var result = new EditorConfigurationDto
        {
            CallbackUrl = await source.GetCallbackUrl(file),
            CoEditing = await source.GetCoEditingAsync(),
            CreateUrl = await source.GetCreateUrl(configuration.EditorType, fileType),
            Customization = await configConverter.Convert(configuration, file),
            Embedded = source.GetEmbedded(configuration.EditorType),
            EncryptionKeys = source.EncryptionKeys,
            Lang = source.Lang,
            Mode = source.Mode,
            ModeWrite = source.ModeWrite,
            Plugins = source.Plugins,
            Templates = await source.GetTemplates(fileType, configuration.Document.Title),
            User = await source.GetUserAsync()
        };

        return result;
    }
}

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class CustomizationConfigConverter<T>(
    LogoConfigConverter<T> configConverter, 
    CustomerConfigConverter customerConfigConverter,
    CoreBaseSettings coreBaseSettings,
    AnonymousConfigConverter<T> anonymousConfigConverter)
{
    public async Task<CustomizationConfigDto> Convert(Configuration<T> configuration, File<T> file)
    {    
        var source = configuration.EditorConfig?.Customization;
        
        if (source == null)
        {
            return null;
        }

        var result = new CustomizationConfigDto
        {
            About = source.About,
            Customer = coreBaseSettings.Standalone ? await customerConfigConverter.Convert(source.Customer) : null,
            Feedback = await source.GetFeedback(),
            Forcesave = source.GetForceSave(file),
            Goback = await source.GetGoBack(configuration.EditorType, file),
            Logo = await configConverter.Convert(configuration),
            MentionShare = await source.GetMentionShare(file),
            ReviewDisplay = source.GetReviewDisplay(configuration.EditorConfig.ModeWrite),
            SubmitForm = await source.GetSubmitForm(file),
            Anonymous = anonymousConfigConverter.Convert(configuration)
        };

        return result;
    }
}

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class LogoConfigConverter<T>
{
    public async Task<LogoConfigDto> Convert(Configuration<T> configuration)
    {
        var source = configuration.EditorConfig?.Customization?.Logo;
        
        if (source == null)
        {
            return null;
        }

        var result = new LogoConfigDto
        {
            Image = await source.GetImage(configuration.EditorType),
            ImageDark = await source.GetImageDark(),
            ImageEmbedded = await source.GetImageEmbedded(configuration.EditorType),
            Url = source.Url,
            Visible = source.GetVisible(configuration.EditorType)
        };

        return result;
    }
}

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class AnonymousConfigConverter<T>
{
    public AnonymousConfigDto Convert(Configuration<T> configuration)
    {
        var source = configuration.EditorConfig?.Customization?.Logo;

        if (source == null)
        {
            return null;
        }

        var result = new AnonymousConfigDto
        {
            Request = configuration.Document.Permissions.Chat
        };

        return result;
    }
}

[Scope]
public class CustomerConfigConverter
{
    public async Task<CustomerConfigDto> Convert(CustomerConfig source)
    {
        if (source == null)
        {
            return null;
        }

        var result = new CustomerConfigDto
        {
            Address = await source.GetAddress(),
            Logo = await source.GetLogo(),
            LogoDark = await source.GetLogoDark(),
            Mail = await source.GetMail(),
            Name = await source.GetName(),
            Www = await source.GetWww()
        };

        return result;
    }
}

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class DocumentConfigConverter<T>(InfoConfigConverter<T> configConverter)
{
    public async Task<DocumentConfigDto> Convert(DocumentConfig<T> source, File<T> file)
    {        
        if (source == null)
        {
            return null;
        }
        
        var result = new DocumentConfigDto
        {
            FileType = source.GetFileType(file),
            Info = await configConverter.Convert(source.Info, file),
            IsLinkedForMe = source.IsLinkedForMe,
            Key = source.Key,
            Permissions = source.Permissions,
            SharedLinkParam = source.SharedLinkParam,
            SharedLinkKey = source.SharedLinkKey,
            ReferenceData = source.GetReferenceData(file),
            Title = source.Title ?? file.Title,
            Url = source.GetUrl(file),
            Options = source.Options,
        };

        return result;
    }
}

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class InfoConfigConverter<T>
{
    public async Task<InfoConfigDto> Convert(InfoConfig<T> source, File<T> file)
    {   
        if (source == null)
        {
            return null;
        }
        
        source.SetFavorite(null);//TODO: add display favorite settings to config
        
        var result = new InfoConfigDto
        {
            Favorite = await source.GetFavorite(file),
            Folder = await source.GetFolder(file),
            Owner = source.GetOwner(file),
            SharingSettings = await source.GetSharingSettings(file),
            Type = source.Type,
            Uploaded = source.GetUploaded(file)
        }; 

        return result;
    }
}