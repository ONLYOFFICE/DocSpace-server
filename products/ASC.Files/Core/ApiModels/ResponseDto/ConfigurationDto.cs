// (c) Copyright Ascensio System SIA 2009-2026
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
    /// <example>{"fileType": "docx", "key": "doc-key-123", "title": "Document Title"}</example>
    public required DocumentConfigDto Document { get; set; }

    /// <summary>
    /// The document type.
    /// </summary>
    /// <example>word</example>
    public required string DocumentType { get; set; }

    /// <summary>
    /// The editor configuration.
    /// </summary>
    /// <example>{"lang": "en-US", "mode": "edit"}</example>
    public required EditorConfigurationDto EditorConfig { get; set; }

    /// <summary>
    /// The editor type.
    /// </summary>
    /// <example>0</example>
    public required EditorType EditorType { get; set; }

    /// <summary>
    /// The editor URL.
    /// </summary>
    /// <example>http://localhost/editor</example>
    [Url]
    public required string EditorUrl { get; set; }

    /// <summary>
    /// The token of the file configuration.
    /// </summary>
    /// <example>token-abc-123</example>
    public string Token { get; set; }

    /// <summary>
    /// The platform type.
    /// </summary>
    /// <example>desktop</example>
    public string Type { get; set; }

    /// <summary>
    /// The file parameters.
    /// </summary>
    /// <example>{"id": 10, "title": "document.docx"}</example>
    public required FileDto<T> File { get; set; }

    /// <summary>
    /// The error message.
    /// </summary>
    /// <example>Configuration error</example>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Specifies if the file filling has started or not.
    /// </summary>
    /// <example>false</example>
    public bool? StartFilling { get; set; }

    /// <summary>
    /// The file filling status.
    /// </summary>
    /// <example>false</example>
    public bool? FillingStatus { get; set; }

    /// <summary>
    /// The start filling mode.
    /// </summary>
    /// <example>0</example>
    public StartFillingMode StartFillingMode { get; set; }

    /// <summary>
    /// The file filling session ID.
    /// </summary>
    /// <example>session-123-456</example>
    public string FillingSessionId { get; set; }

    /// <summary>
    /// Indicates which quota scope has been exceeded.
    /// </summary>
    /// <example>0</example>
    public QuotaScope? QuotaExceededScope { get; set; }

    /// <summary>
    /// The generation tool call state. Used to run the agent flow in the editor.
    /// </summary>
    /// <example>{"toolName": "generate_docx", "parameters": {"description": "Create a report"}}</example>
    public EditorToolCallStateDto GenerationToolCallState { get; set; }
}

/// <summary>
/// The quota scope.
/// </summary>
public enum QuotaScope
{
    /// <summary>
    /// The user-level quota.
    /// </summary>
    [Description("User")]
    User,

    /// <summary>
    /// The room-level quota.
    /// </summary>
    [Description("Room")]
    Room,

    /// <summary>
    /// The tenant-level quota.
    /// </summary>
    [Description("Tenant")]
    Tenant
}

/// <summary>
/// The start filling mode.
/// </summary>
public enum StartFillingMode
{
    [Description("None")]
    None,

    [Description("Share to fill out")]
    ShareToFillOut,

    [Description("Start filling")]
    StartFilling,

    [Description("Start filling form room")]
    StartFillingFormRoom
}

/// <summary>
/// The editor configuration parameters.
/// </summary>
public class EditorConfigurationDto
{
    /// <summary>
    /// The callback URL of the editor.
    /// </summary>
    /// <example>http://localhost/callback</example>
    [Url]
    public string CallbackUrl { get; set; }

    /// <summary>
    /// The co-editing configuration parameters.
    /// </summary>
    public CoEditingConfig CoEditing { get; set; }

    /// <summary>
    /// The creation URL of the editor.
    /// </summary>
    /// <example>http://localhost/create</example>
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
    /// <example>en-US</example>
    public required string Lang { get; set; }

    /// <summary>
    /// The mode of the editor configuration.
    /// </summary>
    /// <example>edit</example>
    public required string Mode { get; set; }

    /// <summary>
    /// Specifies if the mode is write of the editor configuration.
    /// </summary>
    /// <example>true</example>
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
    public required UserConfig User { get; set; }

}

/// <summary>
/// The customization config parameters.
/// </summary>
public class CustomizationConfigDto
{
    /// <summary>
    /// Specifies if the customization is about.
    /// </summary>
    /// <example>true</example>
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
    public FeedbackConfig Feedback { get; set; }

    /// <summary>
    /// Specifies if the customization should be force saved.
    /// </summary>
    /// <example>false</example>
    public bool? Forcesave { get; set; }

    /// <summary>
    /// The go back configuration of the customization.
    /// </summary>
    public GobackConfig Goback { get; set; }

    /// <summary>
    /// The review configuration of the customization.
    /// </summary>
    public ReviewConfig Review { get; set; }

    /// <summary>
    /// The logo of the customization.
    /// </summary>
    public LogoConfigDto Logo { get; set; }

    /// <summary>
    /// Specifies if the share should be mentioned.
    /// </summary>
    /// <example>true</example>
    public bool MentionShare { get; set; }

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
    /// <example>true</example>
    public bool Visible { get; set; }
    /// <summary>
    /// A message displayed after forms are submitted.
    /// </summary>
    /// <example>Form submitted successfully</example>
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
    /// <example>Start Filling</example>
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
    /// <example>http://localhost/logo.png</example>
    public string Image { get; set; }

    /// <summary>
    /// The dark image of the logo.
    /// </summary>
    /// <example>http://localhost/logo-dark.png</example>
    public string ImageDark { get; set; }

    /// <summary>
    /// The light image of the logo.
    /// </summary>
    /// <example>http://localhost/logo-light.png</example>
    public string ImageLight { get; set; }

    /// <summary>
    /// The embedded image of the logo.
    /// </summary>
    /// <example>http://localhost/logo-embedded.png</example>
    public string ImageEmbedded { get; set; }

    /// <summary>
    /// The url link of the logo.
    /// </summary>
    /// <example>http://localhost</example>
    public string Url { get; set; }

    /// <summary>
    /// Specifies if the logo is visible.
    /// </summary>
    /// <example>true</example>
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
    /// <example>false</example>
    public required bool Request { get; set; }
}

/// <summary>
/// The customer config parameters.
/// </summary>
public class CustomerConfigDto
{
    /// <summary>
    /// The address of the customer configuration.
    /// </summary>
    /// <example>123 Main Street, City</example>
    public string Address { get; set; }

    /// <summary>
    /// The logo of the customer configuration.
    /// </summary>
    /// <example>http://localhost/customer-logo.png</example>
    public string Logo { get; set; }

    /// <summary>
    /// The dark logo of the customer configuration.
    /// </summary>
    /// <example>http://localhost/customer-logo-dark.png</example>
    public string LogoDark { get; set; }

    /// <summary>
    /// The mail address of the customer configuration.
    /// </summary>
    /// <example>contact@example.com</example>
    public string Mail { get; set; }

    /// <summary>
    /// The name of the customer configuration.
    /// </summary>
    /// <example>ONLYOFFICE</example>
    public string Name { get; set; }

    /// <summary>
    /// The site web address of the customer configuration.
    /// </summary>
    /// <example>https://www.example.com</example>
    public string Www { get; set; }
}

/// <summary>
/// The document config parameters.
/// </summary>
public class DocumentConfigDto
{
    /// <summary>
    /// The file type of the document.
    /// </summary>
    /// <example>docx</example>
    public string FileType { get; set; }

    /// <summary>
    /// The configuration information of the document.
    /// </summary>
    public InfoConfigDto Info { get; set; }

    /// <summary>
    /// Specifies if the documnet is linked for current user.
    /// </summary>
    /// <example>false</example>
    public bool IsLinkedForMe { get; set; }

    /// <summary>
    /// The document key.
    /// </summary>
    /// <example>doc-key-123-abc</example>
    public string Key { get; set; }

    /// <summary>
    /// The document permissions.
    /// </summary>
    public PermissionsConfig Permissions { get; set; }

    /// <summary>
    /// The shared link parameter of the document.
    /// </summary>
    /// <example>share-param-123</example>
    public string SharedLinkParam { get; set; }

    /// <summary>
    /// The shared link key of the document.
    /// </summary>
    /// <example>share-key-abc</example>
    public string SharedLinkKey { get; set; }

    /// <summary>
    /// The reference data of the document.
    /// </summary>
    public FileReferenceData ReferenceData { get; set; }

    /// <summary>
    /// The document title.
    /// </summary>
    /// <example>Document Title</example>
    public string Title { get; set; }

    /// <summary>
    /// The document url.
    /// </summary>
    /// <example>http://localhost/documents/doc.docx</example>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// Indicates whether this is a form.
    /// </summary>
    /// <example>false</example>
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
    /// <example>false</example>
    public bool? Favorite { get; set; }

    /// <summary>
    /// The folder of the file.
    /// </summary>
    /// <example>My Documents</example>
    public string Folder { get; set; }

    /// <summary>
    /// The file owner.
    /// </summary>
    /// <example>John Doe</example>
    public string Owner { get; set; }

    /// <summary>
    /// The sharing settings of the file.
    /// </summary>
    public List<AceShortWrapper> SharingSettings { get; set; }

    /// <summary>
    /// The editor type of the file.
    /// </summary>
    public EditorType Type { get; set; }

    /// <summary>
    /// The uploaded file.
    /// </summary>
    /// <example>2025-01-01T00:00:00</example>
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
    ExternalShare externalShare,
    EditorToolCallStateStore callStateStore)
{
    public async Task<ConfigurationDto<T>> Convert(Configuration<T> source, File<T> file)
    {
        if (source == null)
        {
            return null;
        }

        var fileDto = await fileDtoHelper.GetAsync(file);
        var result = new ConfigurationDto<T>
        {
            Document = await documentConfigConverter.Convert(source.Document, file),
            DocumentType = source.GetDocumentType(file),
            EditorConfig = await editorConfigurationConverter.Convert(source, file),
            EditorType = source.EditorType,
            EditorUrl = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.DocServiceApiUrl),
            ErrorMessage = source.Error,
            File = fileDto
        };

        result.EditorUrl = FilesLinkUtility.AddQueryString(result.EditorUrl, new Dictionary<string, string> {
            { FilesLinkUtility.ShardKey, result.Document?.Key }
        });

        result.Token = documentServiceHelper.GetSignature(result);
        result.Type = source.Type;

        if (source.EditorType == EditorType.Embedded)
        {
            var shareParam = file.ShareRecord != null
                ? $"&{FilesLinkUtility.ShareKey}={await externalShare.CreateShareKeyAsync(file.ShareRecord.Subject)}"
                : "";

            result.EditorConfig.Embedded.ShareLinkParam = $"&{FilesLinkUtility.FileId}={file.Id}{shareParam}";
        }

        if (file.Id is int fileId)
        {
            var callState = await callStateStore.GetAsync(fileId);
            result.GenerationToolCallState = callState?.MapToDto();
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
            Recent = await source.GetRecent(fileType, file.Id).ToListAsync(),
            Templates = [], // await source.GetTemplates(fileType, configuration.Document.Title),
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
            Logo = await configConverter.Convert(configuration, file),
            MentionShare = await source.GetMentionShare(file),
            Review = source.GetReview(configuration.EditorConfig.ModeWrite),
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
    public async Task<LogoConfigDto> Convert(Configuration<T> configuration, File<T> file)
    {
        var source = configuration.EditorConfig?.Customization?.Logo;

        if (source == null)
        {
            return null;
        }

        var fileType = FileUtility.GetFileTypeByFileName(file.Title);

        var result = new LogoConfigDto
        {
            Image = await source.GetImage(fileType, configuration.EditorType),
            ImageDark = await source.GetImageDark(fileType),
            ImageLight = await source.GetImageLight(fileType),
            ImageEmbedded = await source.GetImageEmbedded(fileType, configuration.EditorType),
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
public class DocumentConfigConverter<T>(InfoConfigConverter<T> configConverter, FileChecker fileChecker)
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

/// <summary>
/// The editor tool call state. Used to run the agent flow in the editor.
/// </summary>
public class EditorToolCallStateDto
{
    /// <summary>
    /// The tool name.
    /// </summary>
    /// <example>GenerateDocx</example>
    public required string ToolName { get; init; }

    /// <summary>
    /// The tool call parameters.
    /// </summary>
    /// <example>{}</example>
    public required EditorToolCallParametersDto Parameters { get; init; }
}

/// <summary>
/// The editor tool call parameters.
/// </summary>
[JsonDerivedType(typeof(GenerateDocxToolCallParametersDto))]
[JsonDerivedType(typeof(GenerateFormToolCallParametersDto))]
[JsonDerivedType(typeof(GeneratePresentationToolCallParametersDto))]
public abstract class EditorToolCallParametersDto;

/// <summary>
/// The generate docx tool call parameters.
/// </summary>
public class GenerateDocxToolCallParametersDto : EditorToolCallParametersDto
{
    /// <summary>
    /// The description of the document to generate.
    /// </summary>
    public required string Description { get; init; }
}

/// <summary>
/// The generate form tool call parameters.
/// </summary>
public class GenerateFormToolCallParametersDto : EditorToolCallParametersDto
{
    /// <summary>
    /// The description of the form to generate.
    /// </summary>
    public required string Description { get; init; }
}

/// <summary>
/// The generate presentation tool call parameters.
/// </summary>
public class GeneratePresentationToolCallParametersDto : EditorToolCallParametersDto
{
    /// <summary>
    /// The presentation topic.
    /// </summary>
    public string Topic { get; init; }

    /// <summary>
    /// The number of slides.
    /// </summary>
    public string SlideCount { get; init; }

    /// <summary>
    /// The visual style.
    /// </summary>
    public string Style { get; init; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class EditorToolCallStateMapper
{
    public static partial EditorToolCallStateDto MapToDto(this EditorToolCallState source);

    [MapDerivedType<GenerateDocxToolCallParameters, GenerateDocxToolCallParametersDto>]
    [MapDerivedType<GenerateFormToolCallParameters, GenerateFormToolCallParametersDto>]
    [MapDerivedType<GeneratePresentationToolCallParameters, GeneratePresentationToolCallParametersDto>]
    private static partial EditorToolCallParametersDto MapToDto(EditorToolCallParameters source);
}