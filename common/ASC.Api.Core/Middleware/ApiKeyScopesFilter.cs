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

namespace ASC.Api.Core.Middleware;

// Enforces API key scopes on [AllowAnonymous] endpoints. The authorization pipeline
// (and therefore ScopesAuthorizationHandler) is short-circuited by [AllowAnonymous],
// so a resource filter is the only place where the check still runs for those routes.
[Scope]
public class ApiKeyScopesFilter : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() == null
            || !IsApiKeyRequest(context.HttpContext))
        {
            await next();
            return;
        }

        var policy = AuthorizationExtension.GetAuthorizePolicy((endpoint as RouteEndpoint)?.RoutePattern.RawText, context.HttpContext.Request.Method);

        if (string.IsNullOrEmpty(policy))
        {
            await next();
            return;
        }

        var requiredScopes = policy.Split(",", StringSplitOptions.RemoveEmptyEntries);

        var userScopes = context.HttpContext.User.Claims
            .Where(c => string.Equals(c.Type, "scope", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value)
            .ToHashSet();

        if (!requiredScopes.Any(userScopes.Contains))
        {
            context.Result = new StatusCodeResult((int)HttpStatusCode.Forbidden);
            return;
        }

        await next();
    }

    private static bool IsApiKeyRequest(HttpContext context)
    {
        string authorizationHeader = context.Request.Headers.Authorization;

        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
        {
            return false;
        }

        return authorizationHeader["Bearer ".Length..].Trim().StartsWith("sk-");
    }
}