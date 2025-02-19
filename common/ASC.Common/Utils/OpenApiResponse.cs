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

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace ASC.Api.Core.Extensions;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class OpenApiResponseAttribute : ProducesResponseTypeAttribute
{
    public string Description { get; }

    public OpenApiResponseAttribute(Type type, int statusCode, string description)
        : base(CreateSuccessResponseType(type), statusCode)
    {
        Description = description;
    }
    public OpenApiResponseAttribute(int statusCode, string description)
        : base(typeof(void), statusCode)
    {
        Description = description;
    }

    private static Type CreateSuccessResponseType(Type innerType)
    {
        var successResponseType = typeof(SuccessApiResponse<>).MakeGenericType(innerType);
        return successResponseType;
    }
}

public class SuccessApiResponse<T>
{
    [OpenApiDescription("Response")]
    public T Response { get; set; }

    [OpenApiDescription("Count")]
    public int Count { get; set; }

    [OpenApiDescription("Links")]
    public List<LinkDto> Links { get; set; } = new();

    [OpenApiDescription("Status")]
    public int Status { get; set; }

    [OpenApiDescription("Status code")]
    public int StatusCode { get; set; }
}

public class LinkDto
{
    [OpenApiDescription("Href")]
    public string Href { get; set; }

    [OpenApiDescription("Action")]
    public string Action { get; set; }
}
public class OpenApiResponseDescriptionTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                var descriptionGroup = context.DescriptionGroups
                    .SelectMany(r => r.Items)
                    .Where(r => r.HttpMethod.Equals(operation.Key.ToString().ToUpper()) && ("/" + r.RelativePath).Equals(path.Key)).FirstOrDefault();
                if (descriptionGroup == null) 
                {
                    continue;
                }
                var metadata = descriptionGroup.ActionDescriptor.EndpointMetadata;
                foreach (var response in operation.Value.Responses)
                {
                    if (int.TryParse(response.Key, out var statusCode))
                    {
                        var responseAtribute = metadata.OfType<OpenApiResponseAttribute>().FirstOrDefault(attr =>  attr.StatusCode == statusCode);
                        response.Value.Description = responseAtribute?.Description;
                    }
                }
            }
        }
        return Task.CompletedTask;
    }
}