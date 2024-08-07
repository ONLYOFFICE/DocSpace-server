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
    [SwaggerSchemaCustomString("Name", Example = "Item name")]
    public string Name { get; set; }

    [SwaggerSchemaCustomString("Icon", Example = "Item icon path")]
    public string Icon { get; set; }

    [SwaggerSchemaCustomBoolean("Specifies if the module space is disabled or not", Example = false)]
    public bool Disabled { get; set; }

    [SwaggerSchemaCustomString("Size", Example = "0 Byte")]
    public string Size { get; set; }

    [SwaggerSchemaCustomString("URL", Example = "Item url", Format = "uri")]
    public string Url { get; set; }

    public static UsageSpaceStatItemDto GetSample()
    {
        return new UsageSpaceStatItemDto
        {
            Name = "Item name",
            Icon = "Item icon path",
            Disabled = false,
            Size = "0 Byte",
            Url = "Item url"
        };
    }
}

public class ChartPointDto
{
    [SwaggerSchemaCustomString("Display date")]
    public string DisplayDate { get; set; }

    [SwaggerSchemaCustomDateTime("Date")]
    public DateTime Date { get; init; }

    [SwaggerSchemaCustomInt("Hosts", Example = 0, Format = "int32")]
    public int Hosts { get; set; }

    [SwaggerSchemaCustomInt("Hits", Example = 0, Format = "int32")]
    public int Hits { get; set; }

    public static ChartPointDto GetSample()
    {
        return new ChartPointDto
        {
            DisplayDate = DateTime.Now.ToShortDateString(),
            Date = DateTime.Now,
            Hosts = 0,
            Hits = 0
        };
    }
}