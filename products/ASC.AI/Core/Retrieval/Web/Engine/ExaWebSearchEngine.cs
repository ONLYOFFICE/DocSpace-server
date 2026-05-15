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

namespace ASC.AI.Core.Retrieval.Web.Engine;

public class ExaConfig : EngineConfig
{
    public required string ApiKey { get; init; }
    
    public override bool CrawlingSupported()
    {
        return true;
    }
}

public class ExaWebSearchEngine(HttpClient httpClient, ExaConfig config) : IWebSearchEngine
{
    public async Task<IEnumerable<WebSearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var requestBody = new ExaSearchRequest
        {
            Query = query.Query,
            NumResults = query.MaxResults,
            Contents = new Contents
            {
                Text = new Text
                {
                    MaxCharacters = 3000
                },
                Livecrawl = "preferred"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.exa.ai/search");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web),
            Encoding.UTF8,
            "application/json");

        request.Headers.Add("x-api-key", config.ApiKey);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent =
                await response.Content.ReadFromJsonAsync<ExaSearchResponse>(cancellationToken: cancellationToken);
            if (responseContent == null || responseContent.Results.Count == 0)
            {
                return [];
            }

            return responseContent.Results.Select(x => new WebSearchResult
            {
                Title = x.Title, 
                Url = x.Url, 
                FaviconUrl = x.Favicon, 
                Text = x.Text
            });
        }
        catch (HttpRequestException e)
        {
            throw ProcessError(e);
        }
    }

    public async Task<WebSearchResult?> GetPageContentAsync(PageContentQuery query, CancellationToken cancellationToken = default)
    {
        var requestBody = new ExaCrawlRequest 
        { 
            Urls = [query.Url], 
            Livecrawl = "preferred",
            Context = true,
            Text = new Text { MaxCharacters = query.MaxCharacters }
        };
        
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.exa.ai/contents");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web),
            Encoding.UTF8,
            "application/json");

        request.Headers.Add("x-api-key", config.ApiKey);

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent =
                await response.Content.ReadFromJsonAsync<ExaSearchResponse>(cancellationToken: cancellationToken);
            if (responseContent == null || responseContent.Results.Count == 0)
            {
                return null;
            }

            var result = responseContent.Results[0];
            
            return new WebSearchResult
            {
                Title = result.Title, 
                Url = result.Url, 
                FaviconUrl = result.Favicon, 
                Text = responseContent.Context ?? result.Text
            };
        }
        catch (HttpRequestException e)
        {
            throw ProcessError(e);
        }
    }
    
    private static Exception ProcessError(HttpRequestException e)
    {
        return e.StatusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new
                ArgumentException("The specified API key is invalid or does not have access rights. " +
                                  "Verify that the key is correct and try again"),
            HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.TooManyRequests =>
                new Exception("Web search service unavailable"),
            _ => e
        };
    }
}

internal class ExaSearchRequest
{
    public required string Query { get; init; }
    public string Type { get; init; } = "auto";
    public int NumResults { get; init; } = 5;
    public required Contents Contents { get; init; }
}

internal class Contents
{
    public required Text Text { get; init; }
    public string? Livecrawl { get; init; } = "preferred";
}

internal class Text
{
    public int? MaxCharacters { get; init; }
}

internal class ExaSearchResponse
{
    public required List<ExaSearchResult> Results { get; init; } = [];
    public string? Context { get; init; }
}

internal class ExaSearchResult
{
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? Favicon { get; init; }
    public required string Text { get; init; }
}

internal class ExaCrawlRequest
{
    public required List<string> Urls { get; init; }
    public required Text Text { get; init; }
    public bool Context { get; init; }
    public string Livecrawl { get; init; } = "preferred";
}