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

public class InternalWebSearchConfig : EngineConfig
{
    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }

    public override bool CrawlingSupported()
    {
        return true;
    }
}

public class InternalWebSearchEngine(
    HttpClient client,
    InternalWebSearchConfig config,
    Dictionary<string, string>? metadata) : IWebSearchEngine
{
    public async Task<IEnumerable<WebSearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var requestBody = new SearchRequest
        {
            Engine = "exa",
            Query = query.Query,
            NumResults = query.MaxResults,
            MaxTextCharacters = 3000,
            Metadata = metadata
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/search");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web),
            Encoding.UTF8,
            "application/json");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: cancellationToken);
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

    public async Task<WebSearchResult?> GetPageContentAsync(PageContentQuery query, CancellationToken cancellationToken = default)
    {
        var requestBody = new ContentsRequest
        {
            Engine = "exa",
            Urls = [query.Url],
            Metadata = metadata
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/contents");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web),
            Encoding.UTF8,
            "application/json");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken: cancellationToken);
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
            Text = result.Text
        };
    }
}

public class SearchRequest
{
    public required string Engine { get; init; }
    public required string Query { get; init; }
    public int NumResults { get; init; } = 5;
    public int MaxTextCharacters { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public class ContentsRequest
{
    public required string Engine { get; init; }
    public required string[] Urls { get; init; }
    public int? MaxTextCharacters { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public class SearchResponse
{
    public required List<SearchResult> Results { get; init; }
}

public class SearchResult
{
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? Favicon { get; init; }
    public required string Text { get; init; }
}
