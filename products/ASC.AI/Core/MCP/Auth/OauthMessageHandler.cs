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

using System.Net;

using ASC.FederatedLogin;

namespace ASC.AI.Core.MCP.Auth;

public delegate Task<OAuth20Token> RefreshTokenDelegate(CancellationToken cancellationToken);

public class OauthMessageHandler : DelegatingHandler
{
    private readonly RefreshTokenDelegate _refreshTokenDelegate;
    private OAuth20Token _token;
    
    public OauthMessageHandler(HttpMessageHandler innerHandler, OAuth20Token token, RefreshTokenDelegate refreshTokenDelegate)
    {
        InnerHandler = innerHandler;
        _token = token;
        _refreshTokenDelegate = refreshTokenDelegate;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        var token = await _refreshTokenDelegate(cancellationToken);
        if (token == null)
        {
            return response;
        }
        
        _token = token;
            
        var clonedRequest = await CloneRequestAsync(request);
        
        clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        
        return await base.SendAsync(clonedRequest, cancellationToken);
    }
    
    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage originalRequest)
    {
        var clonedRequest = new HttpRequestMessage(originalRequest.Method, originalRequest.RequestUri)
        {
            Version = originalRequest.Version
        };
        
        foreach (var header in originalRequest.Headers)
        {
            clonedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (originalRequest.Content == null)
        {
            return clonedRequest;
        }

        var contentBytes = await originalRequest.Content.ReadAsByteArrayAsync();
        clonedRequest.Content = new ByteArrayContent(contentBytes);
            
        foreach (var header in originalRequest.Content.Headers)
        {
            clonedRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clonedRequest;
    }
}