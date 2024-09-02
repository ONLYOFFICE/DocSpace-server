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

public class WebPluginDto: IMapFrom<WebPlugin>
{
    [SwaggerSchemaCustom("Name")]
    public string Name { get; set; }

    [SwaggerSchemaCustom("Version")]
    public string Version { get; set; }

    [SwaggerSchemaCustom("Description")]
    public string Description { get; set; }

    [SwaggerSchemaCustom("License")]
    public string License { get; set; }

    [SwaggerSchemaCustom("Author")]
    public string Author { get; set; }

    [SwaggerSchemaCustom("Home page")]
    public string HomePage { get; set; }

    [SwaggerSchemaCustom("PluginName")]
    public string PluginName { get; set; }

    [SwaggerSchemaCustom("Scopes")]
    public string Scopes { get; set; }

    [SwaggerSchemaCustom("Image")]
    public string Image { get; set; }

    [SwaggerSchemaCustom("Create by")]
    public EmployeeDto CreateBy { get; set; }

    [SwaggerSchemaCustom("Create on")]
    public DateTime CreateOn { get; set; }

    [SwaggerSchemaCustom("Enabled")]
    public bool Enabled { get; set; }

    [SwaggerSchemaCustom("System")]
    public bool System { get; set; }

    [SwaggerSchemaCustom("Url", Format = "uri")]
    public string Url { get; set; }

    [SwaggerSchemaCustom("Settings")]
    public string Settings { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Guid, EmployeeDto>().ConvertUsing<WebPluginMappingConverter>();
        profile.CreateMap<WebPlugin, WebPluginDto>();
    }
}
