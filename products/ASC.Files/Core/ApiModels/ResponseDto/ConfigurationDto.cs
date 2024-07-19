﻿// (c) Copyright Ascensio System SIA 2009-2024
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
    [SwaggerSchemaCustom(Description = "Document config")]
    public DocumentConfigDto Document { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Document type")]
    public string DocumentType { get; set; }

    [SwaggerSchemaCustom(Description = "Editor config")]
    public EditorConfigurationDto<T> EditorConfig { get; set; }

    [SwaggerSchemaCustom(Example = "Desktop", Description = "Editor type")]
    public EditorType EditorType { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Editor URL", Format = "uri")]
    public string EditorUrl { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Token")]
    public string Token { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Platform type")]
    public string Type { get; set; }

    [SwaggerSchemaCustom(Description = "File parameters")]
    public FileDto<T> File { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Error message")]
    public string ErrorMessage { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Specifies if the filling has started or not", Nullable = true)]
    public bool? StartFilling { get; set; }
}

public class EditorConfigurationDto<T>
{
    [SwaggerSchemaCustom(Example = "some text", Description = "Callback url", Format = "uri")]
    public string CallbackUrl { get; set; }

    [SwaggerSchemaCustom(Description = "Co editing")]
    public CoEditingConfig CoEditing { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Create url", Format = "uri")]
    public string CreateUrl { get; set; }

    [SwaggerSchemaCustom(Description = "Customization")]
    public CustomizationConfigDto<T> Customization { get; set; }

    [SwaggerSchemaCustom(Description = "Embedded")]
    public EmbeddedConfig Embedded { get; set; }

    [SwaggerSchemaCustom(Description = "Encryption keys")]
    public EncryptionKeysConfig EncryptionKeys { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Lang")]
    public string Lang { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Mode")]
    public string Mode { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Mode write")]
    public bool ModeWrite { get; set; }

    [SwaggerSchemaCustom(Description = "Plugins")]
    public PluginsConfig Plugins { get; set; }

    [SwaggerSchemaCustom(Description = "Recent")]
    public List<RecentConfig> Recent { get; set; }

    [SwaggerSchemaCustom(Description = "Templates")]
    public List<TemplatesConfig> Templates { get; set; }

    [SwaggerSchemaCustom(Description = "User")]
    public UserConfig User { get; set; }
}
public class CustomizationConfigDto<T>
{
    [SwaggerSchemaCustom(Example = "true", Description = "About")]
    public bool About { get; set; }

    [SwaggerSchemaCustom(Description = "Customer")]
    public CustomerConfigDto Customer { get; set; }

    [SwaggerSchemaCustom(Description = "Anonymous")]
    public AnonymousConfigDto Anonymous { get; set; }

    [SwaggerSchemaCustom(Description = "Feedback")]
    public FeedbackConfig Feedback  { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Forcesave", Nullable = true)]
    public bool? Forcesave { get; set; }

    [SwaggerSchemaCustom(Description = "Go back")]
    public GobackConfig Goback { get; set; }

    [SwaggerSchemaCustom(Description = "Logo")]
    public LogoConfigDto Logo { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "MentionShare")]
    public bool MentionShare { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Review display")]
    public string ReviewDisplay { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Submit form")]
    public bool SubmitForm { get; set; }
}

public class LogoConfigDto
{
    [SwaggerSchemaCustom(Example = "some text", Description = "Image")]
    public string Image { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Image dark")]
    public string ImageDark { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Image embedded")]
    public string ImageEmbedded { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Url", Format = "uri")]
    public string Url { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Visible")]
    public bool Visible { get; set; }
}

public class AnonymousConfigDto
{
    [SwaggerSchemaCustom(Example = "true", Description = "Request")]
    public bool Request { get; set; }
}

public class CustomerConfigDto
{
    [SwaggerSchemaCustom(Example = "some text", Description = "Address")]
    public string Address { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Logo")]
    public string Logo { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Mail")]
    public string Mail { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Name")]
    public string Name  { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Www")]
    public string Www  { get; set; }
}

public class DocumentConfigDto
{
    [SwaggerSchemaCustom(Example = "some text", Description = "File type")]
    public string FileType  { get; set; }

    [SwaggerSchemaCustom(Description = "Info")]
    public InfoConfigDto Info { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Is linked for me")]
    public bool IsLinkedForMe { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Key")]
    public string Key { get; set; }

    [SwaggerSchemaCustom(Description = "Permissions")]
    public PermissionsConfig Permissions { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Shared link param")]
    public string SharedLinkParam { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Shared link key")]
    public string SharedLinkKey { get; set; }

    [SwaggerSchemaCustom(Description = "Reference data")]
    public FileReferenceData ReferenceData { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Title")]
    public string Title { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Url", Format = "uri")]
    public string Url { get; set; }
}

public class InfoConfigDto
{
    [SwaggerSchemaCustom(Example = "true", Description = "Favorite", Nullable = true)]
    public bool? Favorite { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Folder")]
    public string Folder { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Owner")]
    public string Owner { get; set; }

    [SwaggerSchemaCustom(Description = "Sharing settings")]
    public List<AceShortWrapper> SharingSettings{ get; set; }

    [SwaggerSchemaCustom(Example = "Desktop", Description = "Type")]
    public EditorType Type { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Uploaded")]
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
    public async Task<EditorConfigurationDto<T>> Convert(Configuration<T> configuration, File<T> file)
    {
        var source = configuration.EditorConfig;
        
        if (source == null)
        {
            return null;
        }

        var fileType = configuration.GetFileType(file);
        var result = new EditorConfigurationDto<T>
        {
            CallbackUrl = await source.GetCallbackUrl(file.Id.ToString()),
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