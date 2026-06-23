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

namespace ASC.AI.Core.MCP.Auth;

public class OauthContext
{
    public int TenantId { get; init; }
    public int RoomId { get; init; }
    public Guid UserId { get; init; }
    public Guid ServerId { get; init; }
    public required OauthProvider OauthProvider { get; init; }
    public required OAuth20Token Token { get; set; }
}

public class OauthMessageHandler : DelegatingHandler
{
    private readonly OauthContext _context;
    private readonly McpDao _mcpDao;
    private readonly OAuth20TokenHelper _oAuth20TokenHelper;
    
    public OauthMessageHandler(
        HttpMessageHandler innerHandler,
        McpDao mcpDao,
        OauthContext context,
        OAuth20TokenHelper oAuth20TokenHelper)
    {
        InnerHandler = innerHandler;
        _context = context;
        _mcpDao = mcpDao;
        _oAuth20TokenHelper = oAuth20TokenHelper;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _context.Token.AccessToken);
        
        var response = await base.SendAsync(request, cancellationToken);
        
        if (response.StatusCode is not (HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest)
            || !await TryRefreshTokenAsync())
        {
            return response;
        }

        var clonedRequest = await CloneRequestAsync(request);
        
        clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _context.Token.AccessToken);
        
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

    private async Task<bool> TryRefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_context.Token.RefreshToken))
        {
            return false;
        }
        
        var token = _oAuth20TokenHelper.RefreshToken(_context.OauthProvider.AccessTokenUrl, _context.Token);

        if (token == null)
        {
            return false;
        }

        await _mcpDao.UpdateOauthCredentialsAsync(_context.TenantId, _context.RoomId, _context.UserId, _context.ServerId, token);
        _context.Token = token;
        
        return true;
    }
}