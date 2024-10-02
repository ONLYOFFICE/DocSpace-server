// (c) Copyright Ascensio System SIA 2009-2024
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

using System.Net.Http.Json;

namespace ASC.Api.Core.Extensions;

public static class HttpClientTokenIntrospectionExtensions
{
    /// <summary>
    /// Sends an OAuth token introspection request.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static async Task<TokenIntrospectionResponse> IntrospectTokenAsync(this HttpClient client, TokenIntrospectionRequest request, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string>
        {
            { "token", request.Token }
        };
     
        using var req = new HttpRequestMessage(HttpMethod.Post, request.Address) { Content = new FormUrlEncodedContent(dict) };
        using var response = await client.SendAsync(req, cancellationToken);
        
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TokenIntrospectionResponse>(cancellationToken);
    }
}

public class TokenIntrospectionRequest
{
    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    /// <value>
    /// The token.
    /// </value>
    public string Token { get; set; } = default!;

    /// <summary>
    /// Gets or sets the endpoint address
    /// </summary>
    /// <value>
    /// The address.
    /// </value>
    public string Address { get; set; }
}

public class TokenIntrospectionResponse
{
    /// <summary>
    /// Gets a value indicating whether the token is active.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the token is active; otherwise, <c>false</c>.
    /// </value>
    [JsonPropertyName("active")]
    public bool IsActive { get; set; }
}
