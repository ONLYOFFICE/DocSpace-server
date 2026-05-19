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

namespace ASC.AI.Core.Provider.Model;

public class OpenRouterModelClient(
    HttpClient httpClient,
    string url,
    string apiKey) : OpenAiModelClientBase(httpClient, url, apiKey)
{
    protected override string ModelsEndpoint => "models";
    protected override string PingEndpoint => "models/user";

    private static readonly HashSet<string> _restrictedKeywords = ["robotics", "image", "computer", "lyria", "audio",
        "realtime", "tts", "transcribe", "whisper", "babbage"];

    protected override async Task<IEnumerable<ModelInfo>> GetModelsDataAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadFromJsonAsync<OpenRouterResponse>();
        if (content?.Data is null)
        {
            return [];
        }

        return content.Data
            .Where(m => !_restrictedKeywords.Any(y => m.Id.Contains(y)))
            .OrderBy(x => x.Id.Split('/').FirstOrDefault() ?? x.Id)
            .ThenByDescending(x => x.Created)
            .Select(m => new ModelInfo
            {
                Id = m.Id,
                Created = m.Created,
                Alias = m.Name,
                Capabilities = new AiModelCapabilities
                {
                    Vision = m.Architecture?.InputModalities?.Contains("image") is true,
                    ToolCalling = m.SupportedParameters?.Contains("tools") is true,
                    Thinking = m.SupportedParameters?.Contains("reasoning") is true
                }
            });
    }

    private class OpenRouterResponse
    {
        public required List<OpenRouterModel> Data { get; init; }
    }

    public class OpenRouterModel
    {
        public required string Id { get; init; }
        public int Created { get; init; }
        public string? Name { get; init; }
        public OpenRouterArchitecture? Architecture { get; init; }

        [JsonPropertyName("supported_parameters")]
        public IReadOnlyList<string>? SupportedParameters { get; init; }
    }

    public class OpenRouterArchitecture
    {
        [JsonPropertyName("input_modalities")]
        public IReadOnlyList<string>? InputModalities { get; init; }
    }
}
