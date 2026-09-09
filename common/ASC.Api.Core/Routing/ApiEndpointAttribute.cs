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

namespace ASC.Web.Api.Routing;

/// <summary>
/// Marks a class as an API controller, sets its route and its public name in a single declaration.
/// Replaces the <c>[ApiController]</c> + <c>[DefaultRoute]</c> + <c>[ControllerName]</c> triple.
/// </summary>
/// <remarks>
/// The attribute is inherited and does not allow multiple usages, so an attribute declared on a controller
/// completely overrides the one declared on its base controller. A derived controller that only refines the
/// route keeps the name of its base controller: the name is looked up along the base type chain.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ApiEndpointAttribute : ApiControllerAttribute, IRouteTemplateProvider, IControllerModelConvention
{
    private const string ApiPrefix = "api/{version:apiVersion}/[controller]";
    private const string InternalPrefix = "internal/[controller]";

    private readonly string _controllerName;

    public ApiEndpointAttribute() { }

    public ApiEndpointAttribute(string controllerName)
    {
        _controllerName = controllerName;
    }

    public ApiEndpointAttribute(string controllerName, string template) : this(controllerName)
    {
        Template = template;
    }

    /// <summary>
    /// An additional route segment appended after the controller name.
    /// </summary>
    public string Template { get; init; }

    /// <summary>
    /// Publishes the controller under the <c>internal/</c> prefix instead of the versioned public API one.
    /// </summary>
    public bool Internal { get; init; }

    string IRouteTemplateProvider.Template
    {
        get
        {
            var prefix = Internal ? InternalPrefix : ApiPrefix;

            return string.IsNullOrEmpty(Template) ? prefix : $"{prefix}/{Template}";
        }
    }

    int? IRouteTemplateProvider.Order => null;

    string IRouteTemplateProvider.Name => null;

    public void Apply(ControllerModel controller)
    {
        var name = _controllerName ?? FindInheritedName(controller.ControllerType);

        if (!string.IsNullOrEmpty(name))
        {
            controller.ControllerName = name;
        }
    }

    private static string FindInheritedName(Type type)
    {
        for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
        {
            var name = baseType.GetCustomAttribute<ApiEndpointAttribute>(false)?._controllerName;

            if (name != null)
            {
                return name;
            }
        }

        return null;
    }
}
