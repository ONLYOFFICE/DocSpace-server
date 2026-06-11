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

    public async Task<HttpResponseMessage> PatchAsync(string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        return await client.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> GetAsync(string path, CancellationToken cancellationToken)
    {
        return await client.GetAsync(path, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken cancellationToken)
    {
        return await client.DeleteAsync(path, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        return await client.SendAsync(request, cancellationToken);
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
