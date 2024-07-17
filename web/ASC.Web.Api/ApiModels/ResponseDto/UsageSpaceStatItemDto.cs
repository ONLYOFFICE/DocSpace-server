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
    [SwaggerSchemaCustom(Example = "Item name", Description = "Name")]
    public string Name { get; set; }

    [SwaggerSchemaCustom(Example = "Item icon path", Description = "Icon")]
    public string Icon { get; set; }

    [SwaggerSchemaCustom(Example = "false", Description = "Specifies if the module space is disabled or not")]
    public bool Disabled { get; set; }

    [SwaggerSchemaCustom(Example = "0 Byte", Description = "Size")]
    public string Size { get; set; }

    [SwaggerSchemaCustom(Example = "Item url", Description = "URL", Format = "uri")]
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
    [SwaggerSchemaCustom(Example = "some text", Description = "Display date")]
    public string DisplayDate { get; set; }

    [SwaggerSchemaCustom(Example = "2008-04-10T06-30-00.000Z", Description = "Date")]
    public DateTime Date { get; init; }

    [SwaggerSchemaCustom(Example = "0", Description = "Hosts", Format = "int32")]
    public int Hosts { get; set; }

    [SwaggerSchemaCustom(Example = "0", Description = "Hits", Format = "int32")]
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