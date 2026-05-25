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

using Module = ASC.Api.Core.Module;

namespace ASC.Web.Api.Controllers;

/// <remarks>
/// Modules API.
/// </remarks>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("modules")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ModulesController(WebItemManagerSecurity webItemManagerSecurity) : ControllerBase
{
    /// <remarks>
    /// Returns a list of all the portal modules.
    /// </remarks>
    /// <summary>
    /// Get modules
    /// </summary>
    /// <path>api/2.0/modules</path>
    /// <collection>list</collection>
    [Tags("Modules")]
    [SwaggerResponse(200, "List of modules", typeof(IEnumerable<string>))]
    [HttpGet]
    public async Task<IEnumerable<string>> GetAllPortalModules()
    {
        var result = new List<string>();

        foreach (var a in await webItemManagerSecurity.GetItemsAsync(WebZoneType.StartProductList))
        {
            result.Add(a.ApiURL);
        }

        return result;
    }

    /// <remarks>
    /// Returns a list of all the portal modules with their information.
    /// </remarks>
    /// <summary>
    /// Get modules information
    /// </summary>
    /// <path>api/2.0/modules/info</path>
    /// <collection>list</collection>
    [Tags("Modules")]
    [SwaggerResponse(200, "List of modules with their information", typeof(IEnumerable<Module>))]
    [HttpGet("info")]
    public async Task<IEnumerable<Module>> GetAllWithInfo()
    {
        var result = new List<Module>();

        foreach (var a in await webItemManagerSecurity.GetItemsAsync(WebZoneType.StartProductList))
        {
            if (a is Product product)
            {
                product.Init();
                result.Add(new Module(product));
            }
        }

        return result;
    }
}
