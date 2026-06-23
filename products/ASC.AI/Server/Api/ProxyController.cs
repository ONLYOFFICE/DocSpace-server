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

using System.Net.Http.Headers;

namespace ASC.AI.Api;

[Scope]
[DefaultRoute]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[AiFeature]
[ControllerName("ai")]
public class ProxyController(
    IHttpClientFactory httpClientFactory,
    AiProviderService service,
    AiGateway aiGateway
) : ControllerBase
{
    [HttpGet("openai/{providerId}/v1/{*path}")]
    [HttpPost("openai/{providerId}/v1/{*path}")]
    [HttpPut("openai/{providerId}/v1/{*path}")]
    [HttpDelete("openai/{providerId}/v1/{*path}")]
    public async Task<IActionResult> OpenAiProxyRequest([FromRoute] int providerId, [FromRoute] string path)
    {
        var provider = await service.GetProviderAsync(providerId);
            
        var uri = path ?? string.Empty;
        if (!string.IsNullOrEmpty(Request.QueryString.Value))
        {
            uri += Request.QueryString.Value;
        }
            
        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), uri);
        switch (provider.Type)
        {
            case ProviderType.Anthropic:
                provider.Url = "https://api.anthropic.com/v1/";
                request.Headers.Add("x-api-key", provider.Key);
                break;
            case ProviderType.GoogleAi:
                provider.Url = "https://generativelanguage.googleapis.com/v1beta/openai/";
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.Key);
                break;
            default:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.Key);
                break;
        }

#pragma warning disable CA2000
            var client = httpClientFactory.CreateClient();
#pragma warning restore CA2000
        client.BaseAddress = new Uri(provider.Url.EndsWith('/') ? provider.Url : provider.Url + '/');

        if (Request.ContentLength is > 0)
        {
            request.Content = new StreamContent(Request.Body);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(Request.Headers.ContentType.FirstOrDefault() ?? "application/json");
        }

        var upstreamResponse = await client.SendAsync(
            request, 
            HttpCompletionOption.ResponseHeadersRead, 
            Request.HttpContext.RequestAborted);
            
        HttpContext.Response.RegisterForDispose(upstreamResponse);

        Response.StatusCode = (int)upstreamResponse.StatusCode;
        Response.Headers.ContentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        Response.Headers.ContentLength = upstreamResponse.Content.Headers.ContentLength;
            
        await using var upstream = await upstreamResponse.Content.ReadAsStreamAsync(Request.HttpContext.RequestAborted);
        await upstream.CopyToAsync(Response.Body, Request.HttpContext.RequestAborted);
        await Response.Body.FlushAsync(HttpContext.RequestAborted);
        await Response.CompleteAsync();
            
        return new EmptyResult();
    }
        
    [HttpPost("web-search/v1/{*path}")]
    public async Task<IActionResult> SearchProxyRequest([FromRoute] string path)
    {
        var key = await aiGateway.GetKeyAsync();

#pragma warning disable CA2000
            var client = httpClientFactory.CreateClient();
#pragma warning restore CA2000
        var baseUrl = aiGateway.Url;
        client.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + '/');

        var uri = path;
        if (!string.IsNullOrEmpty(Request.QueryString.Value))
        {
            uri += Request.QueryString.Value;
        }

        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        if (Request.ContentLength is > 0)
        {
            request.Content = new StreamContent(Request.Body);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(Request.Headers.ContentType.FirstOrDefault() ?? "application/json");
        }

        var response = await client.SendAsync(request, 
            HttpCompletionOption.ResponseHeadersRead, 
            Request.HttpContext.RequestAborted);
            
        HttpContext.Response.RegisterForDispose(response);

        Response.StatusCode = (int)response.StatusCode;
        Response.Headers.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        Response.Headers.ContentLength = response.Content.Headers.ContentLength;
            
        await using var stream = await response.Content.ReadAsStreamAsync();
        await stream.CopyToAsync(Response.Body);
        await Response.Body.FlushAsync(HttpContext.RequestAborted);
        await Response.CompleteAsync();
            
        return new EmptyResult();
    }
}