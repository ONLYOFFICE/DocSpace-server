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

#nullable enable

namespace ASC.Files.Core.Vectorization.Embedding;

public class InternalEmbeddingGenerator(
    IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
    EmbeddingGenerationOptions generationOptions) : IEmbeddingGenerator<string, Embedding<float>>
{
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = new())
    {
        return innerGenerator.GenerateAsync(values, generationOptions, cancellationToken);
    }

    public void Dispose()
    {
        innerGenerator.Dispose();
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return innerGenerator.GetService(serviceType, serviceKey);
    }
}

[Scope]
public class EmbeddingGeneratorFactory(
    IHttpClientFactory httpClientFactory,
    AiGateway gateway,
    SettingsManager settingsManager,
    InstanceCrypto instanceCrypto,
    VectorizationGlobalSettings vectorizationGlobalSettings)
{
    public async Task<IEmbeddingGenerator<string, Embedding<float>>> CreateAsync(Folder<int> agent)
    {
        var settings = await settingsManager.LoadAsync<EncryptedVectorizationSettings>();

        var providerType = settings.ProviderType;
        if (providerType == EmbeddingProviderType.None
            && !settings.IsConfigured
            && await gateway.IsEnabledAsync())
        {
            providerType = EmbeddingProviderType.PortalAi;
        }

        switch (providerType)
        {
            case EmbeddingProviderType.PortalAi:
                var generationOptions = new EmbeddingGenerationOptions { RawRepresentationFactory = _ =>
                    {
                        var rawOptions = new OpenAI.Embeddings.EmbeddingGenerationOptions();
                        var metadata = new Dictionary<string, string>
                        {
                            { "agent_title", agent.Title },
                            { "agent_id", agent.Id.ToString() }
                        };

#pragma warning disable SCME0001
                        rawOptions.Patch.Set("$.metadata"u8, JsonSerializer.SerializeToUtf8Bytes(metadata));
#pragma warning restore SCME0001

                        return rawOptions;
                    }
                };

                var innerGenerator = MakeGenerator(
                    await gateway.GetKeyAsync(),
                    gateway.Url,
                    vectorizationGlobalSettings.Model.Id);

                return new InternalEmbeddingGenerator(innerGenerator, generationOptions);
            case EmbeddingProviderType.OpenAi:
                return MakeGenerator(
                    await instanceCrypto.DecryptAsync(settings.Key),
                    VectorizationGlobalSettings.OpenAiBaseUrl,
                    vectorizationGlobalSettings.Model.Id);
            case EmbeddingProviderType.OpenRouter:
                return MakeGenerator(
                    await instanceCrypto.DecryptAsync(settings.Key),
                    VectorizationGlobalSettings.OpenRouterBaseUrl,
                    $"openai/{vectorizationGlobalSettings.Model.Id}");
            case EmbeddingProviderType.None:
                throw new InvalidOperationException("Vectorization settings are not configured");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IEmbeddingGenerator<string, Embedding<float>> MakeGenerator(string key, string url, string modelId)
    {
        var credential = new ApiKeyCredential(key);
#pragma warning disable CA2000 // HttpClient is owned by HttpClientPipelineTransport
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(url),
            Transport = new HttpClientPipelineTransport(httpClientFactory.CreateClient())
        };
#pragma warning restore CA2000

        var client = new OpenAIClient(credential, options);

        return client.GetEmbeddingClient(modelId).AsIEmbeddingGenerator();
    }
}

public class EmbeddingModel
{
    public required string Id { get; init; }
    public int Dimension { get; init; }
}
