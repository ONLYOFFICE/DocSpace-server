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
    [SwaggerSchemaCustom<DocumentConfigDto>("Document config")]
    public DocumentConfigDto Document { get; set; }

    [SwaggerSchemaCustom("Document type")]
    public string DocumentType { get; set; }

    [SwaggerSchemaCustom<EditorConfigurationDto<int>>("Editor config")]
    public EditorConfigurationDto<T> EditorConfig { get; set; }

    [SwaggerSchemaCustomString("Editor type", Example = "Desktop")]
    public EditorType EditorType { get; set; }

    [SwaggerSchemaCustom("Editor URL", Format = "uri")]
    public string EditorUrl { get; set; }

    [SwaggerSchemaCustom("Token")]
    public string Token { get; set; }

    [SwaggerSchemaCustom("Platform type")]
    public string Type { get; set; }

    [SwaggerSchemaCustom<FileDto<int>>("File parameters")]
    public FileDto<T> File { get; set; }

    [SwaggerSchemaCustom("Error message")]
    public string ErrorMessage { get; set; }

    [SwaggerSchemaCustom("Specifies if the filling has started or not")]
    public bool? StartFilling { get; set; }

    [SwaggerSchemaCustom("Filling session Id")]
    public string FillingSessionId { get; set; }
}

public class EditorConfigurationDto<T>
{
    [SwaggerSchemaCustom("Callback url", Format = "uri")]
    public string CallbackUrl { get; set; }

    [SwaggerSchemaCustom<CoEditingConfig>("Co editing")]
    public CoEditingConfig CoEditing { get; set; }

    [SwaggerSchemaCustom("Create url", Format = "uri")]
    public string CreateUrl { get; set; }

    [SwaggerSchemaCustom<CustomizationConfigDto<int>>("Customization")]
    public CustomizationConfigDto<T> Customization { get; set; }

    [SwaggerSchemaCustom<EmbeddedConfig>("Embedded")]
    public EmbeddedConfig Embedded { get; set; }

    [SwaggerSchemaCustom<EncryptionKeysConfig>("Encryption keys")]
    public EncryptionKeysConfig EncryptionKeys { get; set; }

    [SwaggerSchemaCustom("Lang")]
    public string Lang { get; set; }

    [SwaggerSchemaCustom("Mode")]
    public string Mode { get; set; }

    [SwaggerSchemaCustom("Mode write")]
    public bool ModeWrite { get; set; }

    [SwaggerSchemaCustom<PluginsConfig>("Plugins")]
    public PluginsConfig Plugins { get; set; }

    [SwaggerSchemaCustom<List<RecentConfig>>("Recent")]
    public List<RecentConfig> Recent { get; set; }

    [SwaggerSchemaCustom<List<TemplatesConfig>>("Templates")]
    public List<TemplatesConfig> Templates { get; set; }

    [SwaggerSchemaCustom<UserConfig>("User")]
    public UserConfig User { get; set; }
}
public class CustomizationConfigDto<T>
{
    [SwaggerSchemaCustom("About")]
    public bool About { get; set; }

    [SwaggerSchemaCustom<CustomerConfigDto>("Customer")]
    public CustomerConfigDto Customer { get; set; }

    [SwaggerSchemaCustom<AnonymousConfigDto>("Anonymous")]
    public AnonymousConfigDto Anonymous { get; set; }

    [SwaggerSchemaCustom<FeedbackConfig>("Feedback")]
    public FeedbackConfig Feedback  { get; set; }

    [SwaggerSchemaCustom("Forcesave")]
    public bool? Forcesave { get; set; }

    [SwaggerSchemaCustom<GobackConfig>("Go back")]
    public GobackConfig Goback { get; set; }

    [SwaggerSchemaCustom<LogoConfigDto>("Logo")]
    public LogoConfigDto Logo { get; set; }

    [SwaggerSchemaCustom("MentionShare")]
    public bool MentionShare { get; set; }

    [SwaggerSchemaCustom("Review display")]
    public string ReviewDisplay { get; set; }

    [SwaggerSchemaCustom("Submit form")]
    public bool SubmitForm { get; set; }
}

public class LogoConfigDto
{
    [SwaggerSchemaCustom("Image")]
    public string Image { get; set; }

    [SwaggerSchemaCustom("Image dark")]
    public string ImageDark { get; set; }

    [SwaggerSchemaCustom("Image embedded")]
    public string ImageEmbedded { get; set; }

    [SwaggerSchemaCustom("Url", Format = "uri")]
    public string Url { get; set; }

    [SwaggerSchemaCustom("Visible")]
    public bool Visible { get; set; }
}

public class AnonymousConfigDto
{
    [SwaggerSchemaCustom("Request")]
    public bool Request { get; set; }
}

public class CustomerConfigDto
{
    [SwaggerSchemaCustom("Address")]
    public string Address { get; set; }

    [SwaggerSchemaCustom("Logo")]
    public string Logo { get; set; }

    public string LogoDark { get; set; }

    [SwaggerSchemaCustom("Mail")]
    public string Mail { get; set; }

    [SwaggerSchemaCustom("Name")]
    public string Name  { get; set; }

    [SwaggerSchemaCustom("Www")]
    public string Www  { get; set; }
}

public class DocumentConfigDto
{
    [SwaggerSchemaCustom("File type")]
    public string FileType  { get; set; }

    [SwaggerSchemaCustom<InfoConfigDto>("Info")]
    public InfoConfigDto Info { get; set; }

    [SwaggerSchemaCustom("Is linked for me")]
    public bool IsLinkedForMe { get; set; }

    [SwaggerSchemaCustom("Key")]
    public string Key { get; set; }

    [SwaggerSchemaCustom<PermissionsConfig>("Permissions")]
    public PermissionsConfig Permissions { get; set; }

    [SwaggerSchemaCustom("Shared link param")]
    public string SharedLinkParam { get; set; }

    [SwaggerSchemaCustom("Shared link key")]
    public string SharedLinkKey { get; set; }

    [SwaggerSchemaCustom<FileReferenceData>("Reference data")]
    public FileReferenceData ReferenceData { get; set; }

    [SwaggerSchemaCustom("Title")]
    public string Title { get; set; }

    [SwaggerSchemaCustom("Url", Format = "uri")]
    public string Url { get; set; }
}

public class InfoConfigDto
{
    [SwaggerSchemaCustom("Favorite")]
    public bool? Favorite { get; set; }

    [SwaggerSchemaCustom("Folder")]
    public string Folder { get; set; }

    [SwaggerSchemaCustom("Owner")]
    public string Owner { get; set; }

    [SwaggerSchemaCustom<List<AceShortWrapper>>("Sharing settings")]
    public List<AceShortWrapper> SharingSettings{ get; set; }

    [SwaggerSchemaCustomString("Type", Example = "Desktop")]
    public EditorType Type { get; set; }

    [SwaggerSchemaCustom("Uploaded")]
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
    DocumentServiceHelper documentServiceHelper)
{
    public async Task<ConfigurationDto<T>> Convert(Configuration<T> source, File<T> file, string fillingSessionId = "")
    {   
        if (source == null)
        {
            return null;
        }

        var result = new ConfigurationDto<T>
        {
            Document = await documentConfigConverter.Convert(source.Document, file),
            DocumentType = source.GetDocumentType(file),
            EditorConfig = await editorConfigurationConverter.Convert(source, file, fillingSessionId),
            EditorType = source.EditorType,
            EditorUrl = commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.DocServiceApiUrl),
            ErrorMessage = source.Error
        };
        
        result.Token = documentServiceHelper.GetSignature(result);
        result.File = await fileDtoHelper.GetAsync(file);
        result.Type = source.Type;
        return result;
    }
}

[Scope(GenericArguments = [typeof(int)])]
[Scope(GenericArguments = [typeof(string)])]
public class EditorConfigurationConverter<T>(CustomizationConfigConverter<T> configConverter)
{
    public async Task<EditorConfigurationDto<T>> Convert(Configuration<T> configuration, File<T> file, string fillingSessionId)
    {
        var source = configuration.EditorConfig;
        
        if (source == null)
        {
            return null;
        }

        var fileType = configuration.GetFileType(file);
        var result = new EditorConfigurationDto<T>
        {
            CallbackUrl = await source.GetCallbackUrl(file.Id.ToString(), fillingSessionId),
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
    public async Task<CustomizationConfigDto<T>> Convert(Configuration<T> configuration, File<T> file)
    {    
        var source = configuration.EditorConfig?.Customization;
        
        if (source == null)
        {
            return null;
        }

        var result = new CustomizationConfigDto<T>
        {
            About = source.About,
            Customer = coreBaseSettings.Standalone ? await customerConfigConverter.Convert(source.Customer) : null,
            Feedback = await source.GetFeedback(),
            Forcesave = source.GetForceSave(file),
            Goback = await source.GetGoBack(configuration.EditorType, file),
            Logo = await configConverter.Convert(configuration),
            MentionShare = await source.GetMentionShare(file),
            ReviewDisplay = source.GetReviewDisplay(configuration.EditorConfig.ModeWrite),
            SubmitForm = await source.GetSubmitForm(file, configuration.EditorConfig.ModeWrite),
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
            ReferenceData = await source.GetReferenceData(file),
            Title = source.Title ?? file.Title,
            Url = await source.GetUrl(file)
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