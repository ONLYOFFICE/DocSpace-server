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

namespace ASC.Api.Core.Extensions;

public class RateLimitOperationFilter(
    IOptions<RateLimiterSettings> rateLimiterOptions,
    IOptions<RateLimiterOptions> limiterOptions) : IOperationFilter
{
    private readonly RateLimiterSettings _settings = rateLimiterOptions.Value;
    private readonly bool _hasGlobalLimiter = limiterOptions.Value.GlobalLimiter is not null;

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var rateLimitAttr = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EnableRateLimitingAttribute>()
            .FirstOrDefault();

        (OpenApiHeader Limit, OpenApiHeader Remaining, OpenApiHeader Reset, OpenApiHeader RetryAfter) headers = default;


        if (rateLimitAttr is not null)
        {
            headers = rateLimitAttr.PolicyName switch
            {
                RateLimiterPolicy.SensitiveApi => BuildHeaders(_settings.SensitiveApiLimit, _settings.SensitiveApiWindowMinutes),
                RateLimiterPolicy.PaymentsApi => BuildHeaders(_settings.PaymentsApiLimit, _settings.PaymentsApiWindowMinutes),
                RateLimiterPolicy.EmailInvitationApi => BuildEmailInvitationHeaders(_settings.MaxEmailInvitationsPerDay),
                _ => default
            };
        } else if (_hasGlobalLimiter)
        {
            headers = BuildGlobalHeaders(_settings);
        }

        if (headers == default)
        {
            return;
        }

        if (operation.Responses == null)
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

    private static (OpenApiHeader Limit, OpenApiHeader Remaining, OpenApiHeader Reset, OpenApiHeader RetryAfter) BuildGlobalHeaders(RateLimiterSettings settings) =>
    (
        new OpenApiHeader
        {
            Description =  $"Sliding window rate limit: {settings.SlidingWindowLimit} requests per minute per user/IP.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = settings.SlidingWindowLimit }
        },
        new OpenApiHeader
        {
            Description =
                $"Number of requests remaining in the current sliding window ({settings.SlidingWindowLimit} req/min). " +
                $"Concurrent limits also apply: {settings.ConcurrentGetLimit} parallel GET requests, " +
                $"{settings.DefaultConcurrencyWriteRequests} parallel POST/PUT requests.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 1 }
        },
        new OpenApiHeader
        {
            Description = "Unix timestamp (seconds) when the current sliding window rate limit resets.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 1750000000 }
        },
        new OpenApiHeader
        {
            Description =  $"Seconds to wait before retrying. " +
                           $"Up to 60s for the sliding window ({settings.SlidingWindowLimit} req/min), up to 86400s for the daily POST/PUT limit ({settings.DailyWriteLimit}/day).",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 30 }
        }
    );

    private static (OpenApiHeader Limit, OpenApiHeader Remaining, OpenApiHeader Reset, OpenApiHeader RetryAfter) BuildHeaders(int limitValue, int timeValue) =>
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
            Description = $"Seconds to wait before retrying ({limitValue} req / {timeValue} min limit per user/IP).",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Example = 30 }
        }
    );

    private static (OpenApiHeader Limit, OpenApiHeader Remaining, OpenApiHeader Reset, OpenApiHeader RetryAfter) BuildEmailInvitationHeaders(int? limitValue)
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
