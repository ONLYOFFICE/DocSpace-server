// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Api.Core.Extensions;

public class RateLimitOperationFilter(IOptions<RateLimiterSettings> rateLimiterOptions) : IOperationFilter
{
    private readonly RateLimiterSettings _settings = rateLimiterOptions.Value;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var rateLimitAttr = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EnableRateLimitingAttribute>()
            .FirstOrDefault();

        if (rateLimitAttr is null)
        {
            return;
        }

        var headers = rateLimitAttr.PolicyName switch
        {
            RateLimiterPolicy.SensitiveApi       => BuildHeaders(_settings.SensitiveApiLimit, _settings.SensitiveApiWindowMinutes),
            RateLimiterPolicy.PaymentsApi        => BuildHeaders(_settings.PaymentsApiLimit, _settings.PaymentsApiWindowMinutes),
            RateLimiterPolicy.EmailInvitationApi => BuildEmailInvitationHeaders(_settings.MaxEmailInvitationsPerDay),
            _ => default
        };

        if (headers == default)
        {
            return;
        }

        if (operation.Responses.TryGetValue("200", out var okResponse) && okResponse is OpenApiResponse concreteOkResponse)
        {
            concreteOkResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
            concreteOkResponse.Headers["X-RateLimit-Limit"]     = headers.Limit;
            concreteOkResponse.Headers["X-RateLimit-Remaining"] = headers.Remaining;
            concreteOkResponse.Headers["X-RateLimit-Reset"]     = headers.Reset;
        }

        operation.Responses["429"] = new OpenApiResponse
        {
            Description = "Too Many Requests.",
            Headers = new Dictionary<string, IOpenApiHeader>
            {
                ["Retry-After"] = headers.RetryAfter
            }
        };
    }

    private (OpenApiHeader Limit, OpenApiHeader Remaining, OpenApiHeader Reset, OpenApiHeader RetryAfter) BuildHeaders(int limitValue, int timeValue) =>
    (
        new OpenApiHeader
        {
            Description = $"Rate limit: {limitValue} requests per {timeValue} minutes per user/IP.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = limitValue }
        },
        new OpenApiHeader
        {
            Description = $"Requests remaining in the current {timeValue}-minute window.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 1 }
        },
        new OpenApiHeader
        {
            Description = $"Unix timestamp (seconds) when the current {timeValue}-minute window resets.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 1750000000 }
        },
        new OpenApiHeader
        {
            Description = $"Seconds to wait before retrying ({limitValue} req /{timeValue} min limit per user/IP).",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 30 }
        }
    );

    private (OpenApiHeader Limit, OpenApiHeader Remaining, OpenApiHeader Reset, OpenApiHeader RetryAfter) BuildEmailInvitationHeaders(int? limitValue)
    {
        if (limitValue is null)
        {
            return default;
        }

        return (
            new OpenApiHeader
            {
                Description = $"Rate limit: {limitValue.Value} invitations per day per tenant.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = limitValue.Value }
            },
            new OpenApiHeader
            {
                Description = $"Invitations remaining today per tenant.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 1 }
            },
            new OpenApiHeader
            {
                Description = "Unix timestamp (seconds) when the daily invitation limit resets.",
                Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 1750000000 }
            },
            new OpenApiHeader
            {
                Description = $"Seconds to wait before retrying ({limitValue.Value} invitations/day limit per tenant).",
                Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 30 }
            }
        );
    }
}
