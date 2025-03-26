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

/// <summary>
/// The configuration parameters.
/// </summary>
public class ConfigurationDto<T>
{
    /// <summary>
    /// The document configuration.
    /// </summary>
    public DocumentConfigDto Document { get; set; }

    /// <summary>
    /// The document type.
    /// </summary>
    public string DocumentType { get; set; }

    /// <summary>
    /// The editor configuration.
    /// </summary>
    public EditorConfigurationDto EditorConfig { get; set; }

    /// <summary>
    /// The editor type.
    /// </summary>
    public EditorType EditorType { get; set; }

    /// <summary>
    /// The editor URL.
    /// </summary>
    [Url]
    public string EditorUrl { get; set; }

    /// <summary>
    /// The encrypted signature added to the config in the form of a token.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// The platform type ("desktop", "mobile", or "embedded").
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// The file parameters.
    /// </summary>
    public FileDto<T> File { get; set; }

    /// <summary>
    /// The error message.
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Specifies if the file filling has started or not.
    /// </summary>
    public bool? StartFilling { get; set; }

    /// <summary>
    /// The file filling session ID.
    /// </summary>
    public string FillingSessionId { get; set; }
}

/// <summary>
/// The editor configuration parameters.
/// </summary>
public class EditorConfigurationDto
{
    /// <summary>
    /// The callback URL of the editor.
    /// </summary>
    [Url]
    public string CallbackUrl { get; set; }

    /// <summary>
    /// The co-editing configuration parameters.
    /// </summary>
    public CoEditingConfig CoEditing { get; set; }

    /// <summary>
    /// The absolute URL of the document where it will be created and available after creation.
    /// </summary>
    public string CreateUrl { get; set; }

    /// <summary>
    /// The customization configuration.
    /// </summary>
    public CustomizationConfigDto Customization { get; set; }

    /// <summary>
    /// The embedded configuration parameters for embedded documents.
    /// </summary>
    public EmbeddedConfig Embedded { get; set; }

    /// <summary>
    /// The encryption keys of the editor configuration.
    /// </summary>
    public EncryptionKeysConfig EncryptionKeys { get; set; }

    /// <summary>
    /// The editor interface language which is set using the two letter (de, ru, it, etc.) language codes.
    /// </summary>
    public string Lang { get; set; }

    /// <summary>
    /// The editor opening mode ("view" or "edit").
    /// </summary>
    public string Mode { get; set; }

    /// <summary>
    /// Specifies whether the user can write any data to the document.
    /// </summary>
    public bool ModeWrite { get; set; }

    /// <summary>
    /// The plugins configuration.
    /// </summary>
    public PluginsConfig Plugins { get; set; }

    /// <summary>
    /// The presence or absence of the documents in the "Open Recent..." menu option.
    /// </summary>
    public List<RecentConfig> Recent { get; set; }

    /// <summary>
    /// The presence or absence of the templates in the "Create New..." menu option.
    /// </summary>
    public List<TemplatesConfig> Templates { get; set; }

    /// <summary>
    /// The user currently viewing or editing the document.
    /// </summary>
    public UserConfig User { get; set; }
}

/// <summary>
/// The customization config parameters.
/// </summary>
public class CustomizationConfigDto
{
    /// <summary>
    /// Defines if the "About" menu button is displayed or hidden.
    /// </summary>
    public bool About { get; set; }

    /// <summary>
    /// The information which will be displayed in the editor "About" section and visible to all the editor users.
    /// </summary>
    public CustomerConfigDto Customer { get; set; }

    /// <summary>
    /// Specifies whether to add a request for the anonymous name.
    /// </summary>
    public AnonymousConfigDto Anonymous { get; set; }

    /// <summary>
    /// The settings for the "Feedback & Support" menu button.
    /// </summary>
    public FeedbackConfig Feedback  { get; set; }

    /// <summary>
    /// Specifies whether to add a request for the file force saving to the callback handler
    /// when saving the document within the document editing service.
    /// </summary>
    public bool? Forcesave { get; set; }

    /// <summary>
    /// The settings for the "Open file location" menu button and upper right corner button.
    /// </summary>
    public GobackConfig Goback { get; set; }

    /// <summary>
    /// The parameters of the image file at the top left corner of the editor header.
    /// </summary>
    public LogoConfigDto Logo { get; set; }

    /// <summary>
    /// Specifies whether a hint indicates that the user will receive a notification and access to the document (true)
    /// or only a notification of the mention (false).
    /// </summary>
    public bool MentionShare { get; set; }

    /// <summary>
    /// The review editing mode in the document editor ("markup", "simple", "final", or "original").
    /// </summary>
    public string ReviewDisplay { get; set; }

    /// <summary>
    /// Specifies whether the Complete & Submit button will be displayed or hidden on the top toolbar.
    /// </summary>
    public bool SubmitForm { get; set; }
}

/// <summary>
/// The parameters of the image file at the top left corner of the editor header.
/// </summary>
public class LogoConfigDto
{
    /// <summary>
    /// The path to the image file used to show in the common work mode or in the embedded mode. 
    /// </summary>
    public string Image { get; set; }

    /// <summary>
    /// The path to the image file used for the dark header.
    /// </summary>
    public string ImageDark { get; set; }

    /// <summary>
    /// The path to the image file used to show in the embedded mode.
    /// </summary>
    public string ImageEmbedded { get; set; }

    /// <summary>
    /// The absolute URL which will be used when someone clicks the logo image.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Specifies if the logo is visible.
    /// </summary>
    public bool Visible { get; set; }
}

/// <summary>
/// Specifies whether to add a request for the anonymous name.
/// </summary>
public class AnonymousConfigDto
{
    /// <summary>
    /// Defines if the request is sent or not.
    /// </summary>
    public bool Request { get; set; }
}

/// <summary>
/// The information which will be displayed in the editor "About" section and visible to all the editor users.
/// </summary>
public class CustomerConfigDto
{
    /// <summary>
    /// The postal address of the company or person who gives access to the editors or the editor authors.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// The path to the image logo.
    /// </summary>
    public string Logo { get; set; }

    /// <summary>
    /// The path to the image logo for the dark theme.
    /// </summary>
    public string LogoDark { get; set; }

    /// <summary>
    /// The email address of the company or person who gives access to the editors or the editor authors.
    /// </summary>
    public string Mail { get; set; }

    /// <summary>
    /// The name of the company or person who gives access to the editors or the editor authors.
    /// </summary>
    public string Name  { get; set; }

    /// <summary>
    /// The home website address of the above company or person.
    /// </summary>
    public string Www  { get; set; }
}

/// <summary>
/// The document config parameters.
/// </summary>
public class DocumentConfigDto
{
    /// <summary>
    /// The type of the file for the source viewed or edited document.
    /// </summary>
    public string FileType  { get; set; }

    /// <summary>
    /// The additional parameters of the document.
    /// </summary>
    public InfoConfigDto Info { get; set; }

    /// <summary>
    /// Specifies if the documnet is linked to the current user.
    /// </summary>
    public bool IsLinkedForMe { get; set; }

    /// <summary>
    /// The unique document identifier used by the service to recognize the document.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The document permissions.
    /// </summary>
    public PermissionsConfig Permissions { get; set; }

    /// <summary>
    /// The shared link parameter.
    /// </summary>
    public string SharedLinkParam { get; set; }

    /// <summary>
    /// The shared link key.
    /// </summary>
    public string SharedLinkKey { get; set; }

    /// <summary>
    /// An object that is generated by the integrator to uniquely identify a file in its system.
    /// </summary>
    public FileReferenceData ReferenceData { get; set; }

    /// <summary>
    /// The desired file name for the viewed or edited document
    /// which will also be used as file name when the document is downloaded.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The absolute URL where the source viewed or edited document is stored.
    /// </summary>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// The document options.
    /// </summary>
    public Options Options { get; set; }
}

/// <summary>
/// The additional parameters of the document.
/// </summary>
public class InfoConfigDto
{
    /// <summary>
    /// The highlighting state of the "Favorite" icon.
    /// </summary>
    public bool? Favorite { get; set; }

    /// <summary>
    /// The folder where the document is stored.
    /// </summary>
    public string Folder { get; set; }

    /// <summary>
    /// The name of the document owner/creator.
    /// </summary>
    public string Owner { get; set; }

    /// <summary>
    /// The information about the settings which allow to share the document with other users.
    /// </summary>
    public List<AceShortWrapper> SharingSettings{ get; set; }

    /// <summary>
    /// The editor type.
    /// </summary>
    public EditorType Type { get; set; }

    /// <summary>
    /// The document uploading date.
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
            Options = source.Options
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