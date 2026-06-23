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

namespace ASC.Api.Core.Auth;

public static class AuthorizationExtension
{

    public static readonly Dictionary<string, string[]> ScopesMap = new()
    {
        { "GET api/[^/]+/files/rooms", [ "rooms:read", "rooms:write" ] },
        { "(POST|PUT|DELETE|UPDATE) api/[^/]+/files/rooms", [ "rooms:write" ] },
        { "GET api/[^/]+/files", [ "files:read", "files:write" ] },
        { "(POST|PUT|DELETE|UPDATE) api/[^/]+/files", [ "files:write" ] },
        { "GET api/[^/]+/people/@self", [ "accounts.self:read", "accounts.self:write" ] },
        { "(POST|PUT|DELETE|UPDATE) api/[^/]+/people/@self", [ "accounts.self:write" ] },
        { "GET api/[^/]+/people", [ "accounts:read", "accounts:write" ] },
        { "(POST|PUT|DELETE|UPDATE) api/[^/]+/people", [ "accounts:write" ] },
        { "GET api/[^/]+/group", [ "accounts:read" ] },
        { "(POST|PUT|DELETE|UPDATE) api/[^/]+/group", [ "accounts:write" ] },
        { "GET api/[^/]+/keys/@self?", [ "*" ] }
    };

    internal static string GetAuthorizePolicy(string routePattern, string httpMethod)
    {
        string[] globalScopes;

        if (httpMethod == "GET")
        {
            globalScopes = [AuthConstants.Claim_ScopeGlobalRead.Value, AuthConstants.Claim_ScopeGlobalWrite.Value];
        }
        else
        {
            globalScopes = [AuthConstants.Claim_ScopeGlobalWrite.Value];
        }

        string[] localScopes = [];

        foreach (var regexPattern in ScopesMap.Keys)
        {
            var regex = new Regex(regexPattern);

            if (!regex.IsMatch($"{httpMethod} {routePattern}"))
            {
                continue;
            }

            localScopes = ScopesMap[regexPattern];

            if (localScopes.Length == 1 && localScopes[0] == "*")
            {
                return null;
            }

            break;
        }

        var scopes = globalScopes.Concat(localScopes).Distinct().ToArray();

        return string.Join(",", scopes);
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddJwtBearerAuthentication()
        {
            services.AddSingleton<IAuthorizationHandler, ScopesAuthorizationHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, JwtBearerAuthHandler>(JwtBearerDefaults.AuthenticationScheme, _ => { });

            return services;

        }

        public IServiceCollection AddApiKeyBearerAuthentication()
        {
            services.AddSingleton<IAuthorizationHandler, ScopesAuthorizationHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, ApiKeyBearerAuthHandler>(ApiKeyBearerDefaults.AuthenticationScheme,
                    _ => { });

            return services;

        }
    }

    public static TBuilder WithRequirementAuthorization<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            var httpMethodMetadata = endpointBuilder.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault();
            var allowAnonymousAttribute = endpointBuilder.Metadata.OfType<AllowAnonymousAttribute>().FirstOrDefault();
            var authorizeAttribute = endpointBuilder.Metadata.OfType<AuthorizeAttribute>().FirstOrDefault();

            var httpMethod = httpMethodMetadata?.HttpMethods.FirstOrDefault();

            var authorizePolicy = GetAuthorizePolicy(((RouteEndpointBuilder)endpointBuilder).RoutePattern.RawText, httpMethod);

            if (allowAnonymousAttribute == null && authorizeAttribute == null && authorizePolicy != null)
            {
                authorizeAttribute = new AuthorizeAttribute(authorizePolicy);

                endpointBuilder.Metadata.Add(authorizeAttribute);
            }
        });

        return builder;
    }
}
