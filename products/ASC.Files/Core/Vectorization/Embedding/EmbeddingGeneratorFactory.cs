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

#nullable enable

namespace ASC.Files.Core.Vectorization.Embedding;

[Scope]
public class EmbeddingGeneratorFactory
{
    public static EmbeddingModel Model => _model ?? throw new ArgumentNullException(nameof(_model));
    
    private static EmbeddingSettings? _settings;
    private static EmbeddingModel? _model;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiGateway _gateway;
    private readonly SettingsManager _settingsManager;
    private readonly InstanceCrypto _instanceCrypto;
    
    public EmbeddingGeneratorFactory(
        IConfiguration configuration, 
        IHttpClientFactory httpClientFactory, 
        AiGateway gateway,
        SettingsManager settingsManager,
        InstanceCrypto instanceCrypto)
    {
        _httpClientFactory = httpClientFactory;
        _gateway = gateway;
        _settingsManager = settingsManager;
        _instanceCrypto = instanceCrypto;

        if (_settings != null)
        {
            return;
        }

        _settings = configuration.GetSection("ai:embedding").Get<EmbeddingSettings>();
        if (_settings != null)
        {
            _model = new EmbeddingModel
            {
                Id = _settings.ModelId,
                Dimension = _settings.Dimension,
                ContextLength = _settings.ContextLength
            };
        }
    }
    
    public async Task<IEmbeddingGenerator<string, Embedding<float>>> CreateAsync()
    {
        if (_settings == null)
        {
            throw new ArgumentNullException(nameof(_settings));
        }

        string url;
        string key;
        string modelId;

        if (_gateway.Configured)
        {
            url = _gateway.Url;
            key = await _gateway.GetKeyAsync();
            modelId = _settings.ModelId;
        }
        else
        {
            var settings = await _settingsManager.LoadAsync<EncryptedVectorizationSettings>();

            (url, modelId) = settings.ProviderType switch
            {
                EmbeddingProviderType.OpenAi => ("https://api.openai.com/v1", _settings.ModelId),
                EmbeddingProviderType.OpenRouter => ("https://openrouter.ai/api/v1", $"openai/{_settings.ModelId}"),
                EmbeddingProviderType.None => throw new InvalidOperationException("Vectorization settings are not configured"),
                _ => throw new ArgumentOutOfRangeException()
            };

            key = await _instanceCrypto.DecryptAsync(settings.Key);
        }

        ArgumentException.ThrowIfNullOrEmpty(url);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var credential = new ApiKeyCredential(key);
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(url),
            Transport = new HttpClientPipelineTransport(_httpClientFactory.CreateClient())
        };
        
        var client = new OpenAIClient(credential, options);

        return client.GetEmbeddingClient(modelId).AsIEmbeddingGenerator();
    }
}

public class EmbeddingModel
{
    public required string Id { get; init; }
    public int Dimension { get; init; }
    public int ContextLength { get; init; }
}