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

namespace ASC.AI.Core.WebSearch;

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
                Text = true
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
        
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadFromJsonAsync<ExaSearchResponse>(cancellationToken: cancellationToken);
        if (responseContent == null || responseContent.Results.Count == 0)
        {
            return [];
        }

        return responseContent.Results.Select(x => new WebSearchResult
        {
            Title = x.Title,
            Url = x.Url,
            Text = x.Text
        });
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
    public bool Text { get; init; }
}

class ExaSearchResponse
{
    public required List<ExaSearchResult> Results { get; init; } = [];
}

class ExaSearchResult
{
    public string? Title { get; init; }
    public string? Url { get; init; }
    public required string Text { get; init; }
}