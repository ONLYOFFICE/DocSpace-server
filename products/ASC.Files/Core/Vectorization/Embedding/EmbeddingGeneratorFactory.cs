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
