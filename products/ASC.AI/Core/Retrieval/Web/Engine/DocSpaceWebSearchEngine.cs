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

namespace ASC.AI.Core.Retrieval.Web.Engine;

public class DocSpaceWebSearchConfig : EngineConfig
{
    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
    
    public override bool CrawlingSupported()
    {
        return true;
    }
}

public class DocSpaceWebSearchEngine(HttpClient client, DocSpaceWebSearchConfig config) : IWebSearchEngine
{
    public async Task<IEnumerable<WebSearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var requestBody = new SearchRequest
        {
            Query = query.Query,
            NumResults = query.MaxResults,
            MaxTextCharacters = 3000
        };
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/search")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web), 
                Encoding.UTF8, 
                "application/json")
        };
        
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
            FaviconUrl = x.FaviconUrl,
            Text = x.Text
        });
    }

    public async Task<WebSearchResult?> GetPageContentAsync(PageContentQuery query, CancellationToken cancellationToken = default)
    {
        var requestBody = new ContentsRequest
        {
            Url = query.Url
        };
        
        var request = new HttpRequestMessage(HttpMethod.Post, $"{config.BaseUrl}/contents")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web), 
                Encoding.UTF8, 
                "application/json")
        };
        
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
            FaviconUrl = result.FaviconUrl,
            Text = result.Text
        };
    }
}

public class SearchRequest
{
    public required string Query { get; init; }
    
    [JsonPropertyName("num_results")]
    public int NumResults { get; init; } = 5;
    
    [JsonPropertyName("max_text_characters")]
    public int MaxTextCharacters { get; init; }
}

public class ContentsRequest
{
    public required string Url { get; init; }
}

public class SearchResponse
{
    public required List<SearchResult> Results { get; init; }
}

public class SearchResult
{
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? FaviconUrl { get; init; }
    public required string Text { get; init; }
}