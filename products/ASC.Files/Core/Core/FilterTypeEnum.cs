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

namespace ASC.Files.Core;

/// <summary>
/// The filter type.
/// </summary>
public enum FilterType
{
    [Description("None")]
    [EnumMember] None = 0,

    [Description("Files  only")]
    [EnumMember] FilesOnly = 1,

    [Description("Folders only")]
    [EnumMember] FoldersOnly = 2,

    [Description("Documents only")]
    [EnumMember] DocumentsOnly = 3,

    [Description("Presentations only")]
    [EnumMember] PresentationsOnly = 4,

    [Description("Spreadsheets only")]
    [EnumMember] SpreadsheetsOnly = 5,

    [Description("Images only")]
    [EnumMember] ImagesOnly = 7,

    [Description("By user")]
    [EnumMember] ByUser = 8,

    [Description("By department")]
    [EnumMember] ByDepartment = 9,

    [Description("Archive only")]
    [EnumMember] ArchiveOnly = 10,

    [Description("By extension")]
    [EnumMember] ByExtension = 11,

    [Description("Media only")]
    [EnumMember] MediaOnly = 12,

    [Description("Filling forms rooms")]
    [EnumMember] FillingFormsRooms = 13,

    [Description("Editing rooms")]
    [EnumMember] EditingRooms = 14,

    [Description("Custom rooms")]
    [EnumMember] CustomRooms = 17,

    [Description("Public rooms")]
    [EnumMember] PublicRooms = 20,

    [Description("Pdf")]
    [EnumMember] Pdf = 22,

    [Description("Pdf form")]
    [EnumMember] PdfForm = 23,

    [Description("Virtual data rooms")]
    [EnumMember] VirtualDataRooms = 24,

    [Description("Diagrams only")]
    [EnumMember] DiagramsOnly = 25,
    
    [Description("Ai rooms")]
    [EnumMember] AiRooms = 26
}