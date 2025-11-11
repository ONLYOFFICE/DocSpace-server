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

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.exa.ai/search")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web), 
                Encoding.UTF8, 
                "application/json")
        };
        
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
            Contents = new Contents
            {
                Text = new Text
                {
                    MaxCharacters = query.MaxCharacters
                },
                Livecrawl = "preferred"
            }
        };
        
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.exa.ai/contents")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, JsonSerializerOptions.Web), 
                Encoding.UTF8, 
                "application/json")
        };
        
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
                Title = result.Title, Url = result.Url, FaviconUrl = result.Favicon, Text = result.Text
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

class ExaSearchRequest
{
    public required string Query { get; init; }
    public string Type { get; init; } = "auto";
    public int NumResults { get; init; } = 5;
    public required Contents Contents { get; init; }
}

class Contents
{
    public required Text Text { get; init; }
    public string? Livecrawl { get; init; } = "preferred";
}

class Text
{
    public int? MaxCharacters { get; init; }
}

class ExaSearchResponse
{
    public required List<ExaSearchResult> Results { get; init; } = [];
    public string? Context { get; init; }
}

class ExaSearchResult
{
    public string? Title { get; init; }
    public string? Url { get; init; }
    public string? Favicon { get; init; }
    public required string Text { get; init; }
}

class ExaCrawlRequest
{
    public required List<string> Urls { get; init; }
    public required Contents Contents { get; init; }
}