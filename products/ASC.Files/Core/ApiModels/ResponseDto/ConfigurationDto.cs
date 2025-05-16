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
    /// The token of the file configuration.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// The platform type.
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
    /// The file filling status.
    /// </summary>
    public bool? FillingStatus { get; set; }

    /// <summary>
    /// The start filling mode.
    /// </summary>
    public StartFillingMode StartFillingMode { get; set; }

    /// <summary>
    /// The file filling session ID.
    /// </summary>
    public string FillingSessionId { get; set; }
}

/// <summary>
/// The start filling mode.
/// </summary>
public enum StartFillingMode
{
    [SwaggerEnum("None")]
    None,

    [SwaggerEnum("Share to fill out")]
    ShareToFillOut,

    [SwaggerEnum("Start filling")]
    StartFilling
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
    /// The creation URL of the editor.
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
    /// The language of the editor configuration.
    /// </summary>
    public string Lang { get; set; }

    /// <summary>
    /// The mode of the editor configuration.
    /// </summary>
    public string Mode { get; set; }

    /// <summary>
    /// Specifies if the mode is write of the editor configuration.
    /// </summary>
    public bool ModeWrite { get; set; }

    /// <summary>
    /// The plugins configuration.
    /// </summary>
    public PluginsConfig Plugins { get; set; }

    /// <summary>
    /// The recent configuration of the editor.
    /// </summary>
    public List<RecentConfig> Recent { get; set; }

    /// <summary>
    /// The templates of the editor configuration.
    /// </summary>
    public List<TemplatesConfig> Templates { get; set; }

    /// <summary>
    /// The user configuration of the editor.
    /// </summary>
    public UserConfig User { get; set; }
}

/// <summary>
/// The customization config parameters.
/// </summary>
public class CustomizationConfigDto
{
    /// <summary>
    /// Specifies if the customization is about.
    /// </summary>
    public bool About { get; set; }

    /// <summary>
    /// The customization customer configuration.
    /// </summary>
    public CustomerConfigDto Customer { get; set; }

    /// <summary>
    /// The anonymous configuration of the customization.
    /// </summary>
    public AnonymousConfigDto Anonymous { get; set; }

    /// <summary>
    /// The feedback configuration of the customization.
    /// </summary>
    public FeedbackConfig Feedback  { get; set; }

    /// <summary>
    /// Specifies if the customization should be force saved.
    /// </summary>
    public bool? Forcesave { get; set; }

    /// <summary>
    /// The go back configuration of the customization.
    /// </summary>
    public GobackConfig Goback { get; set; }

    /// <summary>
    /// The logo of the customization.
    /// </summary>
    public LogoConfigDto Logo { get; set; }

    /// <summary>
    /// Specifies if the share should be mentioned.
    /// </summary>
    public bool MentionShare { get; set; }

    /// <summary>
    /// The review display of the customization.
    /// </summary>
    public string ReviewDisplay { get; set; }

    /// <summary>
    /// The "Complete &amp; Submit" button settings.
    /// </summary>
    public SubmitForm SubmitForm { get; set; }

    /// <summary>
    /// The parameters of the button that starts filling out the form.
    /// </summary>
    public StartFillingForm StartFillingForm { get; set; }
}

/// <summary>
/// The "Complete &amp; Submit" button settings.
/// </summary>
public class SubmitForm
{
    /// <summary>
    /// Specifies whether the "Complete  &amp; Submit" button will be displayed or hidden on the top toolbar.
    /// </summary>
    public bool Visible { get; set; }
    /// <summary>
    /// A message displayed after forms are submitted.
    /// </summary>
    public string ResultMessage { get; set; }
}

/// <summary>
/// The parameters of the button that starts filling out the form.
/// </summary>
public class StartFillingForm
{
    /// <summary>
    /// The caption of the button that starts filling out the form.
    /// </summary>
    public string Text { get; set; }
}

/// <summary>
/// The logo config parameters.
/// </summary>
public class LogoConfigDto
{
    /// <summary>
    /// The image of the logo.
    /// </summary>
    public string Image { get; set; }

    /// <summary>
    /// The dark image of the logo.
    /// </summary>
    public string ImageDark { get; set; }

    /// <summary>
    /// The embedded image of the logo.
    /// </summary>
    public string ImageEmbedded { get; set; }

    /// <summary>
    /// The url link of the logo.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Specifies if the logo is visible.
    /// </summary>
    public bool Visible { get; set; }
}

/// <summary>
/// The anonymous config parameters.
/// </summary>
public class AnonymousConfigDto
{
    /// <summary>
    /// Specifies if the anonymous is a request.
    /// </summary>
    public bool Request { get; set; }
}

/// <summary>
/// The customer config parameters.
/// </summary>
public class CustomerConfigDto
{
    /// <summary>
    /// The address of the customer configuration.
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// The logo of the customer configuration.
    /// </summary>
    public string Logo { get; set; }

    /// <summary>
    /// The dark logo of the customer configuration.
    /// </summary>
    public string LogoDark { get; set; }

    /// <summary>
    /// The mail address of the customer configuration.
    /// </summary>
    public string Mail { get; set; }

    /// <summary>
    /// The name of the customer configuration.
    /// </summary>
    public string Name  { get; set; }

    /// <summary>
    /// The site web address of the customer configuration.
    /// </summary>
    public string Www  { get; set; }
}

/// <summary>
/// The document config parameters.
/// </summary>
public class DocumentConfigDto
{
    /// <summary>
    /// The file type of the document.
    /// </summary>
    public string FileType  { get; set; }

    /// <summary>
    /// The configuration information of the document.
    /// </summary>
    public InfoConfigDto Info { get; set; }

    /// <summary>
    /// Specifies if the documnet is linked for current user.
    /// </summary>
    public bool IsLinkedForMe { get; set; }

    /// <summary>
    /// The document key.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The document permissions.
    /// </summary>
    public PermissionsConfig Permissions { get; set; }

    /// <summary>
    /// The shared link parameter of the document.
    /// </summary>
    public string SharedLinkParam { get; set; }

    /// <summary>
    /// The shared link key of the document.
    /// </summary>
    public string SharedLinkKey { get; set; }

    /// <summary>
    /// The reference data of the document.
    /// </summary>
    public FileReferenceData ReferenceData { get; set; }

    /// <summary>
    /// The document title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The document url.
    /// </summary>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// Indicates whether this is a form.
    /// </summary>
    public bool IsForm { get; set; }

    /// <summary>
    /// The options of the document.
    /// </summary>
    public Options Options { get; set; }
}

/// <summary>
/// The information config parameters.
/// </summary>
public class InfoConfigDto
{
    /// <summary>
    /// Specifies if the file is favorite or not.
    /// </summary>
    public bool? Favorite { get; set; }

    /// <summary>
    /// The folder of the file.
    /// </summary>
    public string Folder { get; set; }

    /// <summary>
    /// The file owner.
    /// </summary>
    public string Owner { get; set; }

    /// <summary>
    /// The sharing settings of the file.
    /// </summary>
    public List<AceShortWrapper> SharingSettings{ get; set; }

    /// <summary>
    /// The editor type of the file.
    /// </summary>
    public EditorType Type { get; set; }

    /// <summary>
    /// The uploaded file.
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
            About = await source.IsAboutPageVisible(),
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
public class DocumentConfigConverter<T>(InfoConfigConverter<T> configConverter,FileChecker fileChecker)
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

        if (FileUtility.GetFileTypeByExtention(FileUtility.GetFileExtension(file.Title)) == FileType.Pdf && !file.IsForm && (FilterType)file.Category == FilterType.None)
        {
            result.IsForm = await fileChecker.IsFormPDFFile(file);
        }
        else
        {
            result.IsForm = file.IsForm;
        }

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