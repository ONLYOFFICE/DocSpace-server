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

namespace ASC.Files.ApiModels.ResponseDto;

/// <summary>
/// Everything an editor client needs in order to open one document: the document itself, the editor setup for this
/// caller, and the signature that lets the editors trust both.
/// </summary>
public class ConfigurationDto<T>
{
    /// <summary>
    /// The document as the editors address it: its revision key, title, type, download address and the permissions of
    /// this caller on it.
    /// </summary>
    /// <example>{"fileType": "docx", "key": "doc-key-123", "title": "Document Title"}</example>
    public required DocumentConfigDto Document { get; set; }

    /// <summary>
    /// The editor family the file opens in - `word`, `cell`, `slide`, `pdf` or `diagram`. It comes back empty for a
    /// format no editor handles.
    /// </summary>
    /// <example>word</example>
    public required string DocumentType { get; set; }

    /// <summary>
    /// How the editor is set up for this opening: the mode, the language, the interface customization, the callback
    /// the editors save through, and the account they attribute changes to.
    /// </summary>
    /// <example>{"lang": "en-US", "mode": "edit"}</example>
    public required EditorConfigurationDto EditorConfig { get; set; }

    /// <summary>
    /// The layout the configuration was actually built for. It echoes the requested one except where the room
    /// overruled it, as the templates folder does by forcing the embedded viewer.
    /// </summary>
    /// <example>0</example>
    public required EditorType EditorType { get; set; }

    /// <summary>
    /// The address of the editor api script the client has to load, with the shard key of this document already
    /// appended. Load it as it is given rather than assembling it by hand.
    /// </summary>
    /// <example>https://portal.example.com/web-apps/apps/api/documents/api.js?shardkey=1_512_3</example>
    [Url]
    public required string EditorUrl { get; set; }

    /// <summary>
    /// Signs this whole configuration so that the editors can trust it; anything a client changes in the
    /// configuration invalidates it. It stays empty on a portal that has no signature secret configured for the
    /// document service.
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string Token { get; set; }

    /// <summary>
    /// The layout spelled as a lowercase word - `desktop`, `mobile` or `embedded` - the same value the editor type
    /// carries as a number.
    /// </summary>
    /// <example>desktop</example>
    public string Type { get; set; }

    /// <summary>
    /// The file the configuration was built for, in the same shape the file listings report it.
    /// </summary>
    /// <example>{"id": 10, "title": "document.docx"}</example>
    public required FileDto<T> File { get; set; }

    /// <summary>
    /// Filled in when the document could not be prepared for opening; the rest of the configuration should then not
    /// be handed to the editors.
    /// </summary>
    /// <example>The file is being converted</example>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Whether this caller may start a filling session on the form from inside the editor. It stays empty when the
    /// file is not a form opened where starting is possible at all.
    /// </summary>
    /// <example>false</example>
    public bool? StartFilling { get; set; }

    /// <summary>
    /// True once the caller holds a role in the running filling session of this form. It stays empty outside a
    /// virtual data room, where roles are the only place it is set.
    /// </summary>
    /// <example>false</example>
    public bool? FillingStatus { get; set; }

    /// <summary>
    /// Which filling button the editor offers: none at all, sharing the form out for others to fill, starting a
    /// filling session, or starting one inside the form-filling room.
    /// </summary>
    /// <example>0</example>
    public StartFillingMode StartFillingMode { get; set; }

    /// <summary>
    /// Identifies the filling session this opening belongs to, and is empty when the document is not opened as part
    /// of one. Submissions made in the editor are collected under it.
    /// </summary>
    /// <example>a1b2c3d4-0000-0000-0000-000000000000</example>
    public string FillingSessionId { get; set; }

    /// <summary>
    /// Names the quota that ran out - the user, the room or the portal - and is set only when the document had to be
    /// opened read-only because of it.
    /// </summary>
    /// <example>0</example>
    public QuotaScope? QuotaExceededScope { get; set; }

    /// <summary>
    /// The generation the editor should run as soon as the document opens. It is set only for a document an AI agent
    /// produced and left waiting for its content, and is empty for every other file.
    /// </summary>
    /// <example>{"toolName": "GenerateDocx", "parameters": {"description": "Create a quarterly report"}}</example>
    public EditorToolCallStateDto GenerationToolCallState { get; set; }
}

/// <summary>
/// How the editors behave for this opening: the mode, the language, the interface, and who is editing.
/// </summary>

public class EditorConfigurationDto
{
    /// <summary>
    /// Where the editors post the document back to when they save it. A client must not call it itself; it is the
    /// address the document service uses.
    /// </summary>
    /// <example>https://portal.example.com/filehandler.ashx?action=track&amp;fileid=512</example>
    [Url]
    public string CallbackUrl { get; set; }

    /// <summary>
    /// How co-editing starts out for this session and whether the user may switch it in the interface.
    /// </summary>
    public CoEditingConfig CoEditing { get; set; }

    /// <summary>
    /// Where the editor sends the user when they ask for a new document of the same type. It is empty when creating
    /// one is not offered here.
    /// </summary>
    /// <example>https://portal.example.com/products/files/?action=create&amp;doctype=word</example>
    public string CreateUrl { get; set; }

    /// <summary>
    /// How the editor interface is dressed for this portal, this document and this layout.
    /// </summary>
    public CustomizationConfigDto Customization { get; set; }

    /// <summary>
    /// The addresses the framed viewer needs. It is filled in only for the embedded layout.
    /// </summary>
    public EmbeddedConfig Embedded { get; set; }

    /// <summary>
    /// The caller's end-to-end encryption keys, added only when the document lies in a private room, so that the
    /// editors can decrypt it in the browser. It is empty everywhere else.
    /// </summary>
    public List<EncryptionKeyDto> EncryptionKeys { get; set; }

    /// <summary>
    /// The culture the editor interface is shown in, taken from the profile of the caller.
    /// </summary>
    /// <example>en-US</example>
    public required string Lang { get; set; }

    /// <summary>
    /// `edit` when this session may write the document, `view` when it may only read it.
    /// </summary>
    /// <example>edit</example>
    public required string Mode { get; set; }

    /// <summary>
    /// Whether this session may write; it is what the mode above says in one word.
    /// </summary>
    /// <example>true</example>
    public bool ModeWrite { get; set; }

    /// <summary>
    /// Which editor plugins are offered. The portal currently offers none, so the list inside comes back empty.
    /// </summary>
    public PluginsConfig Plugins { get; set; }

    /// <summary>
    /// The documents offered in the editor's recent list. It is left out altogether when there is nothing to offer.
    /// </summary>
    /// <example>[]</example>
    public List<RecentConfig> Recent { get; set; }

    /// <summary>
    /// Always empty: the portal no longer passes creation templates through the editor configuration.
    /// </summary>
    /// <example>[]</example>
    public List<TemplatesConfig> Templates { get; set; }

    /// <summary>
    /// The account the editors attribute changes to. It is empty for an anonymous session opened through an external
    /// link, and the editors then ask for a name themselves.
    /// </summary>
    public UserConfig User { get; set; }

}

/// <summary>
/// How the editor interface is dressed: branding, the buttons that lead back into the portal, and the behaviour of
/// review, mentions and form submission.
/// </summary>

public class CustomizationConfigDto
{
    /// <summary>
    /// Whether the About entry of the editor menu is shown.
    /// </summary>
    /// <example>true</example>
    public bool About { get; set; }

    /// <summary>
    /// The branding of the organization running the portal. It is filled in on a server installation only and is
    /// empty in the cloud.
    /// </summary>
    public CustomerConfigDto Customer { get; set; }

    /// <summary>
    /// How an anonymous participant is treated in this session.
    /// </summary>
    public AnonymousConfigDto Anonymous { get; set; }

    /// <summary>
    /// The support link the editor offers behind its feedback button.
    /// </summary>
    public FeedbackConfig Feedback { get; set; }

    /// <summary>
    /// Whether the editors write intermediate revisions while the document stays open. It is empty when the portal
    /// leaves the decision to the editors themselves.
    /// </summary>
    /// <example>false</example>
    public bool? Forcesave { get; set; }

    /// <summary>
    /// Where the editor returns the user to when they leave the document. It is empty when there is nowhere to go
    /// back to, as in an embedded opening.
    /// </summary>
    public GobackConfig Goback { get; set; }

    /// <summary>
    /// How tracked changes are displayed when the document opens; it depends on whether this session may write.
    /// </summary>
    public ReviewConfig Review { get; set; }

    /// <summary>
    /// The logo the editor shows, in the variants the current layout and file type need.
    /// </summary>
    public LogoConfigDto Logo { get; set; }

    /// <summary>
    /// Whether mentioning a user who cannot yet open the document offers to share it with them, instead of silently
    /// notifying nobody.
    /// </summary>
    /// <example>true</example>
    public bool MentionShare { get; set; }

    /// <summary>
    /// The submit button of a form: whether it is shown and what it says.
    /// </summary>
    public SubmitForm SubmitForm { get; set; }

    /// <summary>
    /// The button that starts filling out the form. It is empty when this opening offers no such button.
    /// </summary>
    public StartFillingForm StartFillingForm { get; set; }
}

/// <summary>
/// The button the editor shows to begin filling out a form.
/// </summary>
public class StartFillingForm
{
    /// <summary>
    /// The caption to put on the button, already translated into the language of the caller.
    /// </summary>
    /// <example>Start filling</example>
    public string Text { get; set; }
}

/// <summary>
/// The logo the editor shows, resolved for the file type and the layout of this opening.
/// </summary>

public class LogoConfigDto
{
    /// <summary>
    /// The logo for the current layout and file type, as the portal branding defines it.
    /// </summary>
    /// <example>https://portal.example.com/logo/editor.png</example>
    public string Image { get; set; }

    /// <summary>
    /// The variant for a dark interface theme.
    /// </summary>
    /// <example>https://portal.example.com/logo/editor-dark.png</example>
    public string ImageDark { get; set; }

    /// <summary>
    /// The variant for a light interface theme.
    /// </summary>
    /// <example>https://portal.example.com/logo/editor-light.png</example>
    public string ImageLight { get; set; }

    /// <summary>
    /// The variant for the framed viewer. It is empty in every layout but the embedded one.
    /// </summary>
    /// <example>https://portal.example.com/logo/editor-embedded.png</example>
    public string ImageEmbedded { get; set; }

    /// <summary>
    /// Where clicking the logo takes the user.
    /// </summary>
    /// <example>https://portal.example.com</example>
    public string Url { get; set; }

    /// <summary>
    /// Whether the logo is shown at all; the mobile layout hides it.
    /// </summary>
    /// <example>true</example>
    public bool Visible { get; set; }
}

/// <summary>
/// How the editors treat a participant who opened the document without an account.
/// </summary>

public class AnonymousConfigDto
{
    /// <summary>
    /// Whether the editors ask an anonymous participant for a display name before letting them in. It follows the
    /// chat permission of the document, since a nameless participant cannot take part in one.
    /// </summary>
    /// <example>false</example>
    public required bool Request { get; set; }
}

/// <summary>
/// The branding of the organization running the portal, as the editor About panel shows it. It is reported on a
/// server installation only.
/// </summary>

public class CustomerConfigDto
{
    /// <summary>
    /// The postal address from the portal branding settings; empty when none was entered.
    /// </summary>
    /// <example>20A-6 Ernesta Birznieka-Upisha Street, Riga</example>
    public string Address { get; set; }

    /// <summary>
    /// The About-panel logo of the organization.
    /// </summary>
    /// <example>https://portal.example.com/logo/about.png</example>
    public string Logo { get; set; }

    /// <summary>
    /// The About-panel logo for a dark interface theme.
    /// </summary>
    /// <example>https://portal.example.com/logo/about-dark.png</example>
    public string LogoDark { get; set; }

    /// <summary>
    /// The contact address from the portal branding settings.
    /// </summary>
    /// <example>support@example.com</example>
    public string Mail { get; set; }

    /// <summary>
    /// The organization name shown in the editor.
    /// </summary>
    /// <example>Example Ltd</example>
    public string Name { get; set; }

    /// <summary>
    /// The website of the organization.
    /// </summary>
    /// <example>https://www.example.com</example>
    public string Www { get; set; }
}

/// <summary>
/// The document itself as the editors address it: what to fetch, under which revision key, and what this caller may
/// do with it.
/// </summary>

public class DocumentConfigDto
{
    /// <summary>
    /// The format the editors treat the content as, without the leading dot. For a file that had to be converted this
    /// is the format it was converted to, not the one it is stored under.
    /// </summary>
    /// <example>docx</example>
    public string FileType { get; set; }

    /// <summary>
    /// The facts the editor information panel shows about the document.
    /// </summary>
    public InfoConfigDto Info { get; set; }

    /// <summary>
    /// Whether the caller opened the original document rather than a link pointing at it, which matters only for
    /// formats whose editing is restricted through links.
    /// </summary>
    /// <example>false</example>
    public bool IsLinkedForMe { get; set; }

    /// <summary>
    /// Identifies the exact revision to the editors: everyone who receives the same key joins the same co-editing
    /// session, and the key changes as soon as the document is saved.
    /// </summary>
    /// <example>1_512_3</example>
    public string Key { get; set; }

    /// <summary>
    /// What this caller may do inside the editor - edit, comment, review, fill, download, print, copy and chat.
    /// </summary>
    public PermissionsConfig Permissions { get; set; }

    /// <summary>
    /// The name of the query parameter that carries the external share key. It is set only when the document was
    /// opened through an external link.
    /// </summary>
    /// <example>share</example>
    public string SharedLinkParam { get; set; }

    /// <summary>
    /// The external share key this opening runs under, empty when the caller opened the document as a portal member.
    /// The editors pass it back on every request they make for the document.
    /// </summary>
    /// <example>HkQd9nT2</example>
    public string SharedLinkKey { get; set; }

    /// <summary>
    /// How another spreadsheet names this document in a formula. Pass it to `POST api/2.0/files/file/referencedata`
    /// to resolve such a reference.
    /// </summary>
    public FileReferenceData ReferenceData { get; set; }

    /// <summary>
    /// The name the editors display. When a past version was opened, the moment that version was created is appended
    /// to it in brackets.
    /// </summary>
    /// <example>Budget 2026.xlsx</example>
    public string Title { get; set; }

    /// <summary>
    /// Where the editors fetch the content. It is addressed to the host the document service can reach, which is not
    /// necessarily the address a browser should follow.
    /// </summary>
    /// <example>https://portal.example.com/filehandler.ashx?action=download&amp;fileid=512</example>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// Whether the document is a fillable PDF form. A PDF that the portal has never classified is inspected while the
    /// configuration is built, so the answer is trustworthy even for a freshly uploaded file.
    /// </summary>
    /// <example>false</example>
    public bool IsForm { get; set; }

    /// <summary>
    /// Extra instructions for the editors, currently the watermark to draw over the document. It is empty when the
    /// room sets no watermark.
    /// </summary>
    public Options Options { get; set; }
}

/// <summary>
/// The facts the editor information panel shows about the open document.
/// </summary>

public class InfoConfigDto
{
    /// <summary>
    /// Whether the caller has this document among their favorites. It is empty when favorites do not apply - for an
    /// anonymous caller, for a guest, and for an encrypted document.
    /// </summary>
    /// <example>false</example>
    public bool? Favorite { get; set; }

    /// <summary>
    /// The place of the document as a readable path, its folders joined from the root downwards. It is empty in the
    /// embedded layout, which shows no such panel.
    /// </summary>
    /// <example>My documents \\ Reports</example>
    public string Folder { get; set; }

    /// <summary>
    /// The display name of the owner of the document. It is empty for an anonymous session.
    /// </summary>
    /// <example>John Doe</example>
    public string Owner { get; set; }

    /// <summary>
    /// Who the document is shared with, as the information panel lists it. An empty list means it is shared with
    /// nobody beyond its owner.
    /// </summary>
    /// <example>[]</example>
    public List<AceShortWrapper> SharingSettings { get; set; }

    /// <summary>
    /// The layout the information panel is rendered for.
    /// </summary>
    /// <example>0</example>
    public EditorType Type { get; set; }

    /// <summary>
    /// When the document was created on the portal, already formatted for reading in the culture of the caller rather
    /// than as a machine timestamp.
    /// </summary>
    /// <example>01/01/2026 12:00 PM</example>
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
public class EditorConfigurationConverter<T>(CustomizationConfigConverter<T> configConverter, AuthContext authContext)
{
    public async Task<EditorConfigurationDto> Convert(Configuration<T> configuration, File<T> file)
    {
        var source = configuration.EditorConfig;

        if (source == null)
        {
            return null;
        }

        var fileType = configuration.GetFileType(file);
        var recent = await source.GetRecent(fileType, file.Id).ToListAsync();
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
            Recent = recent.Count == 0 ? null : recent,
            Templates = [], // await source.GetTemplates(fileType, configuration.Document.Title),
            User = authContext.IsAuthenticated ? await source.GetUserAsync() : null
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
public class InfoConfigConverter<T>(AuthContext authContext)
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
            Owner = authContext.IsAuthenticated ? source.GetOwner(file) : null,
            SharingSettings = await source.GetSharingSettings(file),
            Type = source.Type,
            Uploaded = source.GetUploaded(file)
        };

        return result;
    }
}

/// <summary>
/// A generation the editor is expected to run as soon as the document opens, left behind by an AI agent that created
/// the file but not its content.
/// </summary>

public class EditorToolCallStateDto
{
    /// <summary>
    /// Which generation to run, which also decides the shape of the parameters below.
    /// </summary>
    /// <example>GenerateDocx</example>
    public required string ToolName { get; init; }

    /// <summary>
    /// The arguments of the generation named above.
    /// </summary>
    /// <example>{"description": "Create a quarterly report"}</example>
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
    /// What the generated text document should contain, in the words the request was made in.
    /// </summary>
    /// <example>A quarterly report on sales with a summary table</example>
    public required string Description { get; init; }
}

/// <summary>
/// The generate form tool call parameters.
/// </summary>
public class GenerateFormToolCallParametersDto : EditorToolCallParametersDto
{
    /// <summary>
    /// What the generated fillable form should ask for, in the words the request was made in.
    /// </summary>
    /// <example>An employee onboarding form with name, start date and department</example>
    public required string Description { get; init; }
}

/// <summary>
/// The generate presentation tool call parameters.
/// </summary>
public class GeneratePresentationToolCallParametersDto : EditorToolCallParametersDto
{
    /// <summary>
    /// What the generated presentation is about.
    /// </summary>
    /// <example>Sales results for 2026</example>
    public string Topic { get; init; }

    /// <summary>
    /// How many slides to generate, as the request spelled it.
    /// </summary>
    /// <example>12</example>
    public string SlideCount { get; init; }

    /// <summary>
    /// The visual style the slides should be generated in.
    /// </summary>
    /// <example>minimal</example>
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
