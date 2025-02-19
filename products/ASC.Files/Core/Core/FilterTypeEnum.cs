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

namespace ASC.Files.Core;

public enum FilterType
{
    [OpenApiEnum("None")]
    [EnumMember] None = 0,

    [OpenApiEnum("Files  only")]
    [EnumMember] FilesOnly = 1,

    [OpenApiEnum("Folders only")]
    [EnumMember] FoldersOnly = 2,

    [OpenApiEnum("Documents only")]
    [EnumMember] DocumentsOnly = 3,

    [OpenApiEnum("Presentations only")]
    [EnumMember] PresentationsOnly = 4,

    [OpenApiEnum("Spreadsheets only")]
    [EnumMember] SpreadsheetsOnly = 5,

    [OpenApiEnum("Images only")]
    [EnumMember] ImagesOnly = 7,

    [OpenApiEnum("By user")]
    [EnumMember] ByUser = 8,

    [OpenApiEnum("By department")]
    [EnumMember] ByDepartment = 9,

    [OpenApiEnum("Archive only")]
    [EnumMember] ArchiveOnly = 10,

    [OpenApiEnum("By extension")]
    [EnumMember] ByExtension = 11,

    [OpenApiEnum("Media only")]
    [EnumMember] MediaOnly = 12,

    [OpenApiEnum("Filling forms rooms")]
    [EnumMember] FillingFormsRooms = 13,

    [OpenApiEnum("Editing rooms")]
    [EnumMember] EditingRooms = 14,

    [OpenApiEnum("Custom rooms")]
    [EnumMember] CustomRooms = 17,

    [OpenApiEnum("Public rooms")]
    [EnumMember] PublicRooms = 20,

    [OpenApiEnum("Pdf")]
    [EnumMember] Pdf = 22,
    [OpenApiEnum("Pdf form")]
    [EnumMember] PdfForm = 23,
    [EnumMember] VirtualDataRooms = 24
}