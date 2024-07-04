// (c) Copyright Ascensio System SIA 2010-2022
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

using System.Collections.Specialized;

namespace ASC.Api.Core.Auth;

public static class AuthorizationExtension
{
    private static readonly NameValueCollection _scopesMap = new NameValueCollection()
    {
        { "GET api/[0-9].[0-9]/files/rooms", "rooms:read" },
        { "(POST|PUT|DELETE|UPDATE) api/[0-9].[0-9]/files/rooms", "rooms:write" },
        { "GET api/[0-9].[0-9]/files", "files:read,files:write" },
        { "(POST|PUT|DELETE|UPDATE) api/[0-9].[0-9]/files", "files:write" },
        { "GET api/[0-9].[0-9]/people/@self", "accounts.self:read" },
        { "(POST|PUT|DELETE|UPDATE) api/[0-9].[0-9]/people/@self", "account.self:write" },
        { "GET api/[0-9].[0-9]/people", "accounts:read" },
        { "(POST|PUT|DELETE|UPDATE) api/[0-9].[0-9]/people", "accounts:write" },
    };

    private static readonly string[] _allScopes = new[] {
    "files:read",
    "files:write",
    "rooms:read",
    "rooms:write",
    "account.self:read",
    "account.self:write",
    "accounts:read",
    "accounts:write" };

    private static string GetAuthorizePolicy(string routePattern, string httpMethod)
    {
        foreach (var regexPattern in _scopesMap.AllKeys)
        {
            var regex = new Regex(regexPattern);

            if (!regex.IsMatch($"{httpMethod} {routePattern}")) continue;

            var scopes = _scopesMap[regexPattern];

            return scopes;
        }

        return null;
    }

    public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, ScopesAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

        services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, JwtBearerAuthHandler>(JwtBearerDefaults.AuthenticationScheme, _ => { });

        return services;

    }

    public static TBuilder WithRequirementAuthorization<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder =>
        {
            var httpMethodMetadata = endpointBuilder.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault();
            var authorizeAttribute = endpointBuilder.Metadata.OfType<AuthorizeAttribute>().FirstOrDefault();
            var httpMethod = httpMethodMetadata?.HttpMethods.FirstOrDefault();

            var authorizePolicy = GetAuthorizePolicy(((RouteEndpointBuilder)endpointBuilder).RoutePattern.RawText, httpMethod);

            if (authorizeAttribute == null && authorizePolicy != null)
            {
                authorizeAttribute = new AuthorizeAttribute(authorizePolicy);

                endpointBuilder.Metadata.Add(authorizeAttribute);
            }
        });

        return builder;
    }
}


