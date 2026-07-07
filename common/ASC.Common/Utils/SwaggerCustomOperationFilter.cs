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

using Microsoft.OpenApi;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;

public class SwaggerCustomOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parameter in operation.Parameters)
        {
            if (parameter.In == ParameterLocation.Query && parameter.Schema?.Type == JsonSchemaType.Array)
            {
                // "deepObject" is only valid for arrays of objects. For arrays of primitives/enums
                // it produces a style that generators can't serialize, so keep the default "form" +
                // "explode: true" (repeated parameters, e.g. ?folderType=USER&folderType=CustomRoom),
                // which is what the server's default model binding expects.
                var isObjectArray = parameter.Schema.Items?.Type == JsonSchemaType.Object;

                (parameter as OpenApiParameter)?.Style = isObjectArray ? ParameterStyle.DeepObject : ParameterStyle.Form;
                (parameter as OpenApiParameter)?.Explode = true;
            }

        // Remove duplicate example from parameter level if it exists in schema
        if (parameter is OpenApiParameter { Example: not null, Schema.Example: not null } openApiParameter)
        {
            openApiParameter.Example = null;
        }
        }
    }
}

public class ContentTypeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody != null)
        {
            var content = operation.RequestBody.Content
                .Where(c => c.Key != "text/json" && !c.Key.EndsWith("+json"))
                .ToDictionary(c => c.Key, c => c.Value);

            (operation.RequestBody as OpenApiRequestBody)?.Content = content;
        }

        if (operation.Responses != null)
        {
            foreach (var response in operation.Responses)
            {
                var content = response.Value.Content
                    .Where(c => c.Key.Equals("application/json"))
                    .ToDictionary(c => c.Key, c => c.Value);

                (response.Value as OpenApiResponse)?.Content = content;
            }
        }
    }
}

public class SwaggerOperationIdFilter : IOperationFilter
{
    private readonly string _route;
    private readonly string _newOperationId;
    private readonly string _shortName;

    public SwaggerOperationIdFilter(string route, string newOperationId, string shortName)
    {
        _route = route;
        _newOperationId = newOperationId;
        _shortName = shortName;
    }
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath.Equals(_route, StringComparison.OrdinalIgnoreCase) &&
            context.ApiDescription.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            operation.OperationId = _newOperationId;
            operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
            operation.Extensions["x-shortName"] = new JsonNodeExtension(_shortName);
        }
    }
}
