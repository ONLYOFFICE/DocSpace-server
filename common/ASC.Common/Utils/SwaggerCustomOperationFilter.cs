// (c) Copyright Ascensio System SIA 2009-2025
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

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ASC.Api.Core.Extensions;

public class SwaggerCustomOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parameter in operation.Parameters)
        {
            if (parameter.In == ParameterLocation.Query && parameter.Schema.Type == "array")
            {
                parameter.Style = ParameterStyle.DeepObject;
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

            operation.RequestBody.Content = content;
        }

        if (operation.Responses != null)
        {
            foreach (var response in operation.Responses)
            {
                var content = response.Value.Content
                    .Where(c => c.Key.Equals("application/json"))
                    .ToDictionary(c => c.Key, c => c.Value);

                response.Value.Content = content;
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
        if (context.ApiDescription.RelativePath.Equals(_route, System.StringComparison.OrdinalIgnoreCase) &&
            context.ApiDescription.HttpMethod.Equals("GET", System.StringComparison.OrdinalIgnoreCase))
        {
            operation.OperationId = _newOperationId;
            operation.Extensions["x-shortName"] = new OpenApiString(_shortName);
        }
    }
}