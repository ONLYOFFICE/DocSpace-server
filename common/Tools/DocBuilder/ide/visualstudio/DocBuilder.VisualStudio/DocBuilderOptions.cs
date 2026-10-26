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

namespace DocBuilder.VisualStudio;

/// <summary>Tools > Options > DocBuilder Scripts.</summary>
public sealed class DocBuilderOptions : DialogPage
{
    [Category("Language server")]
    [DisplayName("Node.js")]
    [Description("node.exe that runs the language server. Empty: node from PATH, then the Node.js that comes with Visual Studio. Applies after Visual Studio restarts.")]
    public string NodePath { get; set; } = "";

    [Category("Preview")]
    [DisplayName("Document Builder")]
    [Description("docbuilder.exe that runs the scripts. Empty: ONLYOFFICE Document Builder or Document Server in their default install folders, or DOCBUILDER_PATH. Applies after Visual Studio restarts.")]
    public string BuilderPath { get; set; } = "";

    [Category("Preview")]
    [DisplayName("Samples folder")]
    [Description("Folder with <Template>.sample.json files holding the placeholder values a preview runs with. Consulted when there is none next to the template. Applies after Visual Studio restarts.")]
    public string SamplesFolder { get; set; } = "";

    [Category("Preview")]
    [DisplayName("Run on save")]
    [Description("Rebuild the preview every time a .docbuilder file is saved.")]
    public bool RunOnSave { get; set; } = true;

    [Category("Preview")]
    [DisplayName("Show as")]
    [Description("Pdf: the document printed to PDF, needs nothing but Document Builder. Viewer: the document opened read-only in a Document Server editor, with sheets, filters and formats as users see them. Editor: the same, editable; the preview window lists the changes and the assistant carries them into the template. Also switched in the preview window.")]
    public PreviewMode PreviewMode { get; set; } = PreviewMode.Pdf;

    [Category("Viewer")]
    [DisplayName("Document Server")]
    [Description("Address of the Document Server the viewer uses, e.g. http://localhost:8085. Empty: the first local one that answers at http://localhost:8085, http://localhost or http://localhost:8080. Applies after Visual Studio restarts.")]
    public string DocumentServerUrl { get; set; } = "";

    [Category("Viewer")]
    [DisplayName("JWT secret")]
    [Description("The Document Server's JWT secret, to sign the viewer configuration. Empty: read from the local.json of a Document Server installed on this machine. Applies after Visual Studio restarts.")]
    [PasswordPropertyText(true)]
    public string DocumentServerSecret { get; set; } = "";

    [Category("Viewer")]
    [DisplayName("File host")]
    [Description("Host name under which Document Server downloads the document from this machine. Empty: 127.0.0.1, right for a Document Server installed here; host.docker.internal for one in a container. Applies after Visual Studio restarts.")]
    public string FileHost { get; set; } = "";

    [Category("Assistant")]
    [DisplayName("DocSpace portal")]
    [Description("Address of the DocSpace portal whose AI the assistant uses, e.g. https://example.onlyoffice.com. The portal needs AI turned on (its ONLYOFFICE AI profile, paid from the portal's wallet). Applies after Visual Studio restarts.")]
    public string PortalUrl { get; set; } = "";

    [Category("Assistant")]
    [DisplayName("API key")]
    [Description("A DocSpace API key of a portal user who may use AI (profile > API keys). Sent to the portal only. Applies after Visual Studio restarts.")]
    [PasswordPropertyText(true)]
    public string PortalApiKey { get; set; } = "";

    [Category("Assistant")]
    [DisplayName("AI profile")]
    [Description("Id of the portal's AI profile to use. Empty: the portal's ONLYOFFICE AI profile. Applies after Visual Studio restarts.")]
    public string ProfileId { get; set; } = "";
}

public enum PreviewMode
{
    Pdf,
    Viewer,
    Editor,
}
