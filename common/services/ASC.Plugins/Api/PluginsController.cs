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

using System.Net;

using ASC.Common;
using ASC.Common.Web;
using ASC.Core;
using ASC.Web.Api.Routing;
using ASC.Web.Core.PublicResources;
using ASC.Web.Studio.Core;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace ASC.Plugins.Api;

[Scope]
[DefaultRoute]
[ApiController]
public class PluginsController(PermissionContext permissionContext,
    PluginManager pluginManager,
    TenantManager tenantManager,
    IEnumerable<EndpointDataSource> endpointSources) : ControllerBase
{

    [HttpGet("endpoints")]
    public object ListAllEndpoints()
    {
        var endpoints = endpointSources
            .SelectMany(es => es.Endpoints)
            .OfType<RouteEndpoint>();
        var output = endpoints.Select(
            e =>
            {
                var controller = e.Metadata
                    .OfType<ControllerActionDescriptor>()
                    .FirstOrDefault();
                var action = controller != null
                    ? $"{controller.ControllerName}.{controller.ActionName}"
                    : null;
                var controllerMethod = controller != null
                    ? $"{controller.ControllerTypeInfo.FullName}:{controller.MethodInfo.Name}"
                    : null;
                return new
                {
                    Method = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods?[0],
                    Route = $"/{e.RoutePattern.RawText.TrimStart('/')}",
                    Action = action,
                    ControllerMethod = controllerMethod
                };
            }
        );

        return output;
    }

    [HttpPost("")]
    public async Task<string> AddPluginFromFile()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (HttpContext.Request.Form.Files == null || HttpContext.Request.Form.Files.Count == 0)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginNoInputFile);
        }

        if (HttpContext.Request.Form.Files.Count > 1)
        {
            throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginToManyInputFiles);
        }

        var file = HttpContext.Request.Form.Files[0] ?? throw new CustomHttpException(HttpStatusCode.BadRequest, Resource.ErrorWebPluginNoInputFile);

        var plugin = await pluginManager.AddPluginFromFileAsync(file);

       // var outDto = mapper.Map<PluginConfig, PluginDto>(webPlugin);

        return "ok";
    }

    [HttpDelete("{name}")]
    public async Task<string> DeletePlugin(string name)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var plugin = await pluginManager.DeletePluginAsync(name);

        // var outDto = mapper.Map<PluginConfig, PluginDto>(webPlugin);

        return "ok";
    }

    [HttpPost("enable/{name}")]
    public async Task<string> EnablePlugin(string name)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var plugin = await pluginManager.UpdatePluginAsync(name, true, string.Empty);
        return "ok";
    }

    [HttpPost("disenable/{name}")]
    public async Task<string> DisenablePlugin(string name)
    {
        var plugin = await pluginManager.UpdatePluginAsync(name, false, string.Empty);
        return "ok";
    }
}
