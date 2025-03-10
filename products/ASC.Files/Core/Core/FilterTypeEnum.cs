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
    [SwaggerEnum("None")]
    [EnumMember] None = 0,

    [SwaggerEnum("Files  only")]
    [EnumMember] FilesOnly = 1,

    [SwaggerEnum("Folders only")]
    [EnumMember] FoldersOnly = 2,

    [SwaggerEnum("Documents only")]
    [EnumMember] DocumentsOnly = 3,

    [SwaggerEnum("Presentations only")]
    [EnumMember] PresentationsOnly = 4,

    [SwaggerEnum("Spreadsheets only")]
    [EnumMember] SpreadsheetsOnly = 5,

    [SwaggerEnum("Images only")]
    [EnumMember] ImagesOnly = 7,

    [SwaggerEnum("By user")]
    [EnumMember] ByUser = 8,

    [SwaggerEnum("By department")]
    [EnumMember] ByDepartment = 9,

    [SwaggerEnum("Archive only")]
    [EnumMember] ArchiveOnly = 10,

    [SwaggerEnum("By extension")]
    [EnumMember] ByExtension = 11,

    [SwaggerEnum("Media only")]
    [EnumMember] MediaOnly = 12,

    [SwaggerEnum("Filling forms rooms")]
    [EnumMember] FillingFormsRooms = 13,

    [SwaggerEnum("Editing rooms")]
    [EnumMember] EditingRooms = 14,

    [SwaggerEnum("Custom rooms")]
    [EnumMember] CustomRooms = 17,

    [SwaggerEnum("Public rooms")]
    [EnumMember] PublicRooms = 20,

    [SwaggerEnum("Pdf")]
    [EnumMember] Pdf = 22,
    [SwaggerEnum("Pdf form")]
    [EnumMember] PdfForm = 23,
    [EnumMember] VirtualDataRooms = 24
}