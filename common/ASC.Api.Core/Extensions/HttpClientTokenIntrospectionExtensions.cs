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

        using var req = new HttpRequestMessage(HttpMethod.Post, request.Address);
        req.Content = new FormUrlEncodedContent(dict);
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
    public string Token { get; set; } = null!;

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