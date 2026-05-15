// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.AI.Tests.ApiFactories;

public class AiApiClient(HttpClient client)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<HttpResponseMessage> PostAsync(string path, object? body, CancellationToken cancellationToken)
    {
        using var content = JsonContent.Create(body, options: JsonOptions);
        return await client.PostAsync(path, content, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostRawAsync(string path, string rawJson, CancellationToken cancellationToken)
    {
        using var content = new StringContent(rawJson, Encoding.UTF8, "application/json");
        return await client.PostAsync(path, content, cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsync(string path, object? body, CancellationToken cancellationToken)
    {
        using var content = JsonContent.Create(body, options: JsonOptions);
        return await client.PutAsync(path, content, cancellationToken);
    }

    public async Task<HttpResponseMessage> GetAsync(string path, CancellationToken cancellationToken)
    {
        return await client.GetAsync(path, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken cancellationToken)
    {
        return await client.DeleteAsync(path, cancellationToken);
    }

    public async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.EnsureSuccessStatusCode();

        var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions, cancellationToken);
        if (wrapper is null || wrapper.Response is null)
        {
            throw new InvalidOperationException($"Empty response body for {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}.");
        }

        return wrapper.Response;
    }
}
