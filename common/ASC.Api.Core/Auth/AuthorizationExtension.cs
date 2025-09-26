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

namespace ASC.Api.Core.Auth;

public static class AuthorizationExtension
{
    
    public static readonly Dictionary<string, string[]> ScopesMap = new()
    {
        { "GET api/[0-9].[0-9]/files/rooms", [ "rooms:read", "rooms:write" ] },
        { "(POST|PUT|DELETE|UPDATE) api/[0-9].[0-9]/files/rooms", [ "rooms:write" ] },
        { "GET api/[0-9].[0-9]/files", [ "files:read", "files:write" ] },
        { "(POST|PUT|DELETE|UPDATE) api/[0-9].[0-9]/files", [ "files:write" ] },
        { "GET api/[0-9].[0-9]/people/@self", [ "accounts.self:read", "accounts.self:write" ] },
        { "(POST|PUT|DELETE|UPDATE) api/[0-9].[0-9]/people/@self", [ "accounts.self:write" ] },
        { "GET api/[0-9].[0-9]/people", [ "accounts:read", "accounts:write" ] },
        { "(POST|PUT|DELETE|UPDATE) api/[0-9].[0-9]/people", [ "accounts:write" ] },
        { "(GET|POST|PUT|DELETE|UPDATE) api/[0-9].[0-9]/keys(/.*)?", [ "*" ] },
    };
    
    private static string GetAuthorizePolicy(string routePattern, string httpMethod)
    {
        string[] globalScopes;

        if (httpMethod == "GET")
        {
            globalScopes = [ AuthConstants.Claim_ScopeGlobalRead.Value, AuthConstants.Claim_ScopeGlobalWrite.Value ];
        }
        else
        {
            globalScopes = [ AuthConstants.Claim_ScopeGlobalWrite.Value ];
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
                localScopes = ScopesMap.SelectMany(r => r.Value).Except(["*"]).ToArray();
            }
            
            break;
        }

        var scopes = globalScopes.Concat(localScopes).Distinct().ToArray();
        
        return string.Join(",", scopes);
    }

    public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, ScopesAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

        services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, JwtBearerAuthHandler>(JwtBearerDefaults.AuthenticationScheme, _ => { });

        return services;

    }
    
    public static IServiceCollection AddApiKeyBearerAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, ScopesAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyBearerAuthHandler>(ApiKeyBearerDefaults.AuthenticationScheme,
                _ => { });

        return services;

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


