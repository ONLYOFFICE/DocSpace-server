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

namespace ASC.AI.Service;

public sealed record ChatCompletionMessage(string Role, string Content);

/// <summary>
/// One-shot, non-streaming chat completion against an OpenAI-compatible endpoint, for short server-side
/// generations. The interactive chat does not go through here — it streams via
/// <see cref="ASC.AI.Api.AiGatewayProxyController"/>.
/// </summary>
// Singleton: a call started during an attach may still be running once that request's scope is gone,
// and nothing here is scope-bound.
[Singleton]
public class OpenAiChatCompletionClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>
    /// Returns the assistant message content, or null when there is none. Throws on transport errors and
    /// non-success status codes — callers decide how to degrade.
    /// </summary>
    public async Task<string?> CompleteAsync(
        string baseUrl,
        string? apiKey,
        string modelId,
        IReadOnlyList<ChatCompletionMessage> messages,
        CancellationToken cancellationToken)
    {
        // Minimal body: temperature and max_tokens are rejected or renamed by some models, and this
        // transport serves both the gateway and bring-your-own providers.
        var payload = new
        {
            model = modelId,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            stream = false
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = JsonContent.Create(payload)
        };

        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

#pragma warning disable CA2000 // HttpClient is short-lived and disposed by the runtime
        var httpClient = httpClientFactory.CreateClient();
#pragma warning restore CA2000

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);

        return completion?.Choices?.FirstOrDefault()?.Message?.Content;
    }

    private sealed record ChatCompletionResponse(List<ChatCompletionChoice>? Choices);

    private sealed record ChatCompletionChoice(ChatCompletionResponseMessage? Message);

    private sealed record ChatCompletionResponseMessage(string? Content);
}
