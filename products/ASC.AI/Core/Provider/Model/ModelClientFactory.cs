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

[Scope]
public class ModelClientFactory(IHttpClientFactory httpClientFactory)
{
    public IModelClient Create(ProviderType type, string url, string apiKey)
    {
        // CA2000: HttpClient ownership transferred to model client classes which manage their lifecycle
#pragma warning disable CA2000
        return type switch
        {
            ProviderType.OpenAi =>
                new OpenAiModelClient(httpClientFactory.CreateClient(), url, apiKey),
            ProviderType.TogetherAi =>
                new TogetherAiModelClient(httpClientFactory.CreateClient(), url, apiKey),
            ProviderType.Anthropic =>
                new AnthropicModelClient(httpClientFactory.CreateClient(), url, apiKey),
            ProviderType.PortalAi =>
                new InternalModelClient(httpClientFactory.CreateClient(), url, apiKey),
            ProviderType.OpenRouter =>
                new OpenRouterModelClient(httpClientFactory.CreateClient(), url, apiKey),
            ProviderType.GoogleAi =>
                new GoogleModelClient(httpClientFactory, apiKey),
            ProviderType.XAi =>
                new XAiModelClient(httpClientFactory.CreateClient(), url, apiKey),
            ProviderType.OpenAiCompatible or ProviderType.DeepSeek =>
                new OpenAiCompatibleModelClient(httpClientFactory.CreateClient(), url, apiKey),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
#pragma warning restore CA2000
    }

    public IModelClient Create(EmbeddingProviderType type, string url, string apiKey)
    {
        // CA2000: HttpClient ownership transferred to model client classes which manage their lifecycle
#pragma warning disable CA2000
        return type switch
        {
            EmbeddingProviderType.OpenAi =>
                new OpenAiModelClient(httpClientFactory.CreateClient(), url, apiKey),
            EmbeddingProviderType.OpenRouter =>
                new OpenRouterModelClient(httpClientFactory.CreateClient(), url, apiKey),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
#pragma warning restore CA2000
    }
}
