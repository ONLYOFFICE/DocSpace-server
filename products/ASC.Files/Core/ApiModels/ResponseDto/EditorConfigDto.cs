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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>
/// The permissions configuration parameters.
/// </summary>
public class PermissionsConfigDto
{
    /// <summary>
    /// Defines if the document can be commented or not.
    /// </summary>
    /// <example>true</example>
    public bool Comment { get; set; } = true;

    /// <summary>
    /// Defines if the chat functionality is enabled in the document or not.
    /// </summary>
    /// <example>true</example>
    public bool Chat { get; set; } = true;

    /// <summary>
    /// Defines if the document can be downloaded or only viewed or edited online.
    /// </summary>
    /// <example>true</example>
    public bool Download { get; set; } = true;

    /// <summary>
    /// Defines if the document can be edited or only viewed.
    /// </summary>
    /// <example>true</example>
    public bool Edit { get; set; } = true;

    /// <summary>
    /// Defines if the forms can be filled.
    /// </summary>
    /// <example>true</example>
    public bool FillForms { get; set; } = true;

    /// <summary>
    /// Defines if the filter can be applied globally (true) affecting all the other users,
    /// or locally (false), i.e. for the current user only.
    /// </summary>
    /// <example>true</example>
    public bool ModifyFilter { get; set; } = true;

    /// <summary>
    /// Defines if the "Protection" tab on the toolbar and the "Protect" button in the left menu are displayedor hidden.
    /// </summary>
    /// <example>true</example>
    public bool Protect { get; set; } = true;

    /// <summary>
    /// Defines if the document can be printed or not.
    /// </summary>
    /// <example>true</example>
    public bool Print { get; set; } = true;

    /// <summary>
    /// Defines if the document can be reviewed or not.
    /// </summary>
    /// <example>true</example>
    public bool Review { get; set; } = true;

    /// <summary>
    /// Defines if the content can be copied to the clipboard or not.
    /// </summary>
    /// <example>true</example>
    public bool Copy { get; set; } = true;
}

/// <summary>
/// How co-editing is preset when the document opens, and whether the user may switch it afterwards.
/// </summary>
public class CoEditingConfigDto
{
    /// <summary>
    /// Whether the user may switch between the two co-editing modes from the editor interface, or is held to the one
    /// the portal preset.
    /// </summary>
    /// <example>true</example>
    public bool Change { get; set; }

    /// <summary>
    /// Whether other participants see each change as it is typed. Left off, changes are exchanged only when a
    /// participant saves, and the paragraph being edited is locked for the others meanwhile.
    /// </summary>
    /// <example>false</example>
    public bool Fast { get; init; }

    /// <summary>
    /// The mode the two settings above amount to, as the editors name it.
    /// </summary>
    /// <example>Strict</example>
    public CoEditingConfigMode Mode => Fast ? CoEditingConfigMode.Fast : CoEditingConfigMode.Strict;
}

/// <summary>
/// The addresses the framed viewer needs. It is reported for the embedded layout only.
/// </summary>
[Transient]
public class EmbeddedConfigDto(BaseCommonLinkUtility baseCommonLinkUtility, FilesLinkUtility filesLinkUtility)
{
    /// <summary>
    /// The page to put into the frame. It is empty when the opening carries no external share key, since a framed
    /// viewer cannot authenticate a portal member.
    /// </summary>
    /// <example>https://portal.example.com/products/files/doceditor?action=embedded&amp;share=HkQd9nT2</example>
    public string EmbedUrl
    {
        get => field ?? (ShareLinkParam != null && ShareLinkParam.Contains(FilesLinkUtility.ShareKey, StringComparison.Ordinal) ? baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FilesBaseAbsolutePath + FilesLinkUtility.EditorPage + "?" + FilesLinkUtility.Action + "=embedded" + ShareLinkParam) : null);
        set;
    }

    /// <summary>
    /// Where the download button of the framed viewer leads.
    /// </summary>
    /// <example>https://portal.example.com/filehandler.ashx?action=download&amp;share=HkQd9nT2</example>
    public string SaveUrl => baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FileHandlerPath + "?" + FilesLinkUtility.Action + "=download" + ShareLinkParam);

    /// <summary>
    /// The query fragment carrying the external share key, ampersand included, out of which the addresses around it
    /// are built.
    /// </summary>
    /// <example>&amp;fileid=512&amp;share=HkQd9nT2</example>
    public string ShareLinkParam { get; set; }

    /// <summary>
    /// The address behind the share button of the framed viewer, the document opened full-screen for reading. It is
    /// empty when the opening carries no external share key.
    /// </summary>
    /// <example>https://portal.example.com/products/files/doceditor?action=view&amp;share=HkQd9nT2</example>
    public string ShareUrl
    {
        get => field ?? (ShareLinkParam != null && ShareLinkParam.Contains(FilesLinkUtility.ShareKey) ? baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FilesBaseAbsolutePath + FilesLinkUtility.EditorPage + "?" + FilesLinkUtility.Action + "=view" + ShareLinkParam) : null);
        set;
    }

    /// <summary>
    /// Where the framed viewer puts its toolbar. The portal always asks for the top.
    /// </summary>
    /// <example>top</example>
    public string ToolbarDocked => "top";
}

/// <summary>
/// The settings for the "Feedback &amp; Support" menu button.
/// </summary>
public class FeedbackConfigDto
{
    /// <summary>
    /// The absolute URL to the website address which will be opened when clicking the "Feedback &amp; Support" menu button.
    /// </summary>
    /// <example>https://portal.example.com/support</example>
    public string Url { get; set; }

    /// <summary>
    /// Whether the support button is shown. The portal always asks for it to be shown.
    /// </summary>
    /// <example>true</example>
    public bool Visible => true;
}

/// <summary>
/// The settings for the "Open file location" menu button and upper right corner button.
/// </summary>
public class GobackConfigDto
{
    /// <summary>
    /// Where the user is taken when they leave the document, normally the folder or the room it lies in. It is empty
    /// when there is nowhere to return to, as in a framed opening.
    /// </summary>
    /// <example>https://portal.example.com/rooms/shared/42</example>
    public string Url { get; set; }
}

/// <summary>
/// How tracked changes are displayed when the document opens.
/// </summary>
public class ReviewConfigDto
{
    /// <summary>
    /// How the editors render tracked changes at first: with the markup, in a simplified markup, as the final text,
    /// or as the original text. A session that may not write opens on the final text.
    /// </summary>
    /// <example>original</example>
    public string ReviewDisplay { get; private set; }

    /// <summary>
    /// Sets the review display value using enum representation.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ReviewDisplayEnum ReviewDisplayEnum { set => ReviewDisplay = value.ToStringLowerFast(); }
}

/// <summary>
/// Which editor add-ons the portal connects. It currently connects none.
/// </summary>
[Transient]
public class PluginsConfigDto
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
    /// <example>
    /// [
    ///   "https://portal.example.com/ThirdParty/plugin/easybib/config.json",
    ///   "https://portal.example.com/ThirdParty/plugin/wordpress/config.json"
    /// ]
    /// </example>
    public string[] PluginsData =>
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
        [];
}

/// <summary>
/// One entry of the recent-documents list the editor offers.
/// </summary>
public class RecentConfigDto
{
    /// <summary>
    /// The folder shown next to the entry, as a readable name rather than an id.
    /// </summary>
    /// <example>My documents</example>
    public string Folder { get; set; }

    /// <summary>
    /// The name shown for the entry.
    /// </summary>
    /// <example>Report 2026.docx</example>
    public string Title { get; set; }

    /// <summary>
    /// Where the entry opens.
    /// </summary>
    /// <example>https://portal.example.com/doceditor?fileid=512</example>
    [Url]
    public string Url { get; set; }
}

/// <summary>
/// One creation template offered in the editor. The portal no longer offers any, so this never appears in an editor
/// configuration.
/// </summary>
public class TemplatesConfigDto
{
    /// <summary>
    /// The absolute URL to the image for template.
    /// </summary>
    /// <example>https://portal.example.com/templates/template1.png</example>
    public string Image { get; set; }

    /// <summary>
    /// The template title that will be displayed in the "Create New..." menu option.
    /// </summary>
    /// <example>Blank Document</example>
    public string Title { get; set; }

    /// <summary>
    /// The absolute URL to the document where it will be created and available after creation.
    /// </summary>
    /// <example>https://portal.example.com/editor/new?template=blank</example>
    [Url]
    public string Url { get; set; }
}

/// <summary>
/// The account the editors attribute the changes of this session to.
/// </summary>
public class UserConfigDto
{
    /// <summary>
    /// The account the changes are recorded under. Two sessions carrying the same value are taken by the editors for
    /// the same person.
    /// </summary>
    /// <example>9924256b-447c-4f19-9dbd-8ad8c39e8ff5</example>
    public string Id { get; set; }

    /// <summary>
    /// The name shown next to the changes and in the list of participants.
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; set; }

    /// <summary>
    /// An absolute address of the avatar shown for this participant.
    /// </summary>
    /// <example>https://portal.example.com/storage/userphotos/9924256b_medium.png</example>
    public string Image { get; set; }

    /// <summary>
    /// The filling roles this participant holds in the form being filled out. It is set only for a form in a virtual
    /// data room, where the role decides which fields open for them.
    /// </summary>
    /// <example>["Manager"]</example>
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public List<string> Roles { get; set; }

    /// <summary>
    /// Identifies the paying customer this participant belongs to, on deployments where the editors are licensed per
    /// customer.
    /// </summary>
    /// <example>cust_001</example>
    public string CustomerId { get; set; }
}

public class AiConfigDto
{
    /// <summary>
    /// Indicates whether the AI feature is disabled.
    /// </summary>
    /// <example>
    /// true
    /// </example>
    public bool Disabled { get; set; }
}

/// <summary>
/// The "Complete &amp; Submit" button settings.
/// </summary>
public class SubmitFormDto
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
