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

using Module = ASC.Api.Core.Module;

namespace ASC.Web.Api.Controllers;

/// <summary>
/// Modules API.
/// </summary>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("modules")]
public class ModulesController(WebItemManagerSecurity webItemManagerSecurity) : ControllerBase
{
    /// <summary>
    /// Returns a list of all the portal modules.
    /// </summary>
    /// <short>
    /// Get modules
    /// </short>
    /// <path>api/2.0/modules</path>
    /// <collection>list</collection>
    [Tags("Modules")]
    [EndpointSummary("Get modules")]
    [EndpointDescription("Returns a list of all the portal modules.")]
    [OpenApiResponse(typeof(IEnumerable<string>), 200, "List of modules")]
    [HttpGet]
    public IEnumerable<string> GetAllPortalModules()
    {
        var result = new List<string>();

        foreach (var a in webItemManagerSecurity.GetItems(WebZoneType.StartProductList))
        {
            result.Add(a.ApiURL);
        }

        return result;
    }

    /// <summary>
    /// Returns a list of all the portal modules with their information.
    /// </summary>
    /// <short>
    /// Get modules information
    /// </short>
    /// <path>api/2.0/modules/info</path>
    /// <collection>list</collection>
    [Tags("Modules")]
    [EndpointSummary("Get modules information")]
    [EndpointDescription("Returns a list of all the portal modules with their information.")]
    [OpenApiResponse(typeof(IEnumerable<Module>), 200, "List of modules with their information")]
    [HttpGet("info")]
    public IEnumerable<Module> GetAllWithInfo()
    {
        foreach (var a in webItemManagerSecurity.GetItems(WebZoneType.StartProductList))
        {
            if (a is Product product)
            {
                product.Init();
                yield return new Module(product);
            }

        }
    }
}
