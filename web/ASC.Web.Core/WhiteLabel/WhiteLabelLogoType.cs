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

using ASC.Web.Core.Files;

using NetEscapades.EnumGenerators;

namespace ASC.Web.Core.WhiteLabel;

[EnumExtensions]
public enum WhiteLabelLogoType
{
    [Description("Light small")]
    LightSmall = 1,

    [Description("Login page")]
    LoginPage = 2,

    [Description("Favicon")]
    Favicon = 3,

    [Description("Docs editor")]
    DocsEditor = 4,

    [Description("Docs editor embed")]
    DocsEditorEmbed = 5,

    [Description("Left menu")]
    LeftMenu = 6,

    [Description("About page")]
    AboutPage = 7,

    [Description("Notification")]
    Notification = 8,

    [Description("Spreadsheet editor")]
    SpreadsheetEditor = 9,

    [Description("Spreadsheet editor embed")]
    SpreadsheetEditorEmbed = 10,

    [Description("Presentation editor")]
    PresentationEditor = 11,

    [Description("Presentation editor embed")]
    PresentationEditorEmbed = 12,

    [Description("Pdf editor")]
    PdfEditor = 13,

    [Description("Pdf editor embed")]
    PdfEditorEmbed = 14,

    [Description("Diagram editor")]
    DiagramEditor = 15,

    [Description("Diagram editor embed")]
    DiagramEditorEmbed = 16
}

public static class WhiteLabelLogoTypeHelper
{
    public static WhiteLabelLogoType GetEditorLogoType(FileType fileType, bool embed)
    {
        return fileType switch
        {
            FileType.Spreadsheet => embed ? WhiteLabelLogoType.SpreadsheetEditorEmbed : WhiteLabelLogoType.SpreadsheetEditor,
            FileType.Presentation => embed ? WhiteLabelLogoType.PresentationEditorEmbed : WhiteLabelLogoType.PresentationEditor,
            FileType.Pdf => embed ? WhiteLabelLogoType.PdfEditorEmbed : WhiteLabelLogoType.PdfEditor,
            FileType.Diagram => embed ? WhiteLabelLogoType.DiagramEditorEmbed : WhiteLabelLogoType.DiagramEditor,
            _ => embed ? WhiteLabelLogoType.DocsEditorEmbed : WhiteLabelLogoType.DocsEditor
        };
    }
}