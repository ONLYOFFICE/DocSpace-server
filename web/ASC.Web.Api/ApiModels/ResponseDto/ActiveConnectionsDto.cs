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

namespace ASC.Web.Api.ApiModels.ResponseDto;

public class ActiveConnectionsDto
{
    [SwaggerSchemaCustom(Example = "1234", Description = "Login event", Format = "int32")]
    public int LoginEvent { get; set; }

    [SwaggerSchemaCustom(Description = "Items")]
    public List<ActiveConnectionsItemDto> Items { get; set; }
}

public class ActiveConnectionsItemDto
{
    [SwaggerSchemaCustom(Example = "1234", Description = "Id", Format = "int32")]
    public int Id { get; set; }

    [SwaggerSchemaCustom(Example = "1234", Description = "Tenant id", Format = "int32")]
    public int TenantId { get; set; }

    [SwaggerSchemaCustom(Example = "9924256A-739C-462b-AF15-E652A3B1B6EB", Description = "User id")]
    public Guid UserId { get; set; }

    [SwaggerSchemaCustom(Example = "true", Description = "Mobile")]
    public bool Mobile {  get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Ip")]
    public string Ip { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Country")]
    public string Country { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "City")]
    public string City { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Browser")]
    public string Browser { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Platform")]
    public string Platform { get; set; }

    [SwaggerSchemaCustom(Example = "2008-04-10T06-30-00.000Z", Description = "Date")]
    public ApiDateTime Date { get; set; }

    [SwaggerSchemaCustom(Example = "some text", Description = "Page")]
    public string Page { get; set; }
}