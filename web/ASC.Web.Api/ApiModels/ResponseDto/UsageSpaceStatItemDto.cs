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

namespace ASC.Web.Api.ApiModel.ResponseDto;

public class UsageSpaceStatItemDto
{
    /// <summary>
    /// Name
    /// </summary>
    [SwaggerSchemaCustom(Example = "Item name")]
    public string Name { get; set; }

    /// <summary>
    /// Icon
    /// </summary>
    [SwaggerSchemaCustom(Example = "Item icon path")]
    public string Icon { get; set; }

    /// <summary>
    /// Specifies if the module space is disabled or not
    /// </summary>
    [SwaggerSchemaCustom(Example = false)]
    public bool Disabled { get; set; }

    /// <summary>
    /// Size
    /// </summary>
    [SwaggerSchemaCustom(Example = "0 Byte")]
    public string Size { get; set; }

    /// <summary>
    /// URL
    /// </summary>
    [SwaggerSchemaCustom(Example = "Item url")]
    public string Url { get; set; }
}

public class ChartPointDto
{
    /// <summary>
    /// Display date
    /// </summary>
    [SwaggerSchemaCustom(Example = "6/1/2024")]
    public string DisplayDate { get; set; }

    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// Hosts
    /// </summary>
    [SwaggerSchemaCustom(Example = 0)]
    public int Hosts { get; set; }

    /// <summary>
    /// Hits
    /// </summary>
    [SwaggerSchemaCustom(Example = 0)]
    public int Hits { get; set; }
}