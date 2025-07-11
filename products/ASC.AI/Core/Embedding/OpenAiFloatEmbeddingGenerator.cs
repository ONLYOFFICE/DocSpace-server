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

namespace ASC.AI.Core.Embedding;

public class OpenAiFloatEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private const string Endpoint = "/embeddings";
    private readonly HttpClient _client;
    private readonly string _modelId;
    private readonly Uri _endpoint;

    private bool _disposed;

    public OpenAiFloatEmbeddingGenerator(HttpClient client, GeneratorConfiguration configuration)
    {
        _client = client;
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {configuration.ApiKey}");
        _endpoint = new Uri(configuration.Endpoint.TrimEnd('/') + Endpoint);
        _modelId = configuration.ModelId;
    }
    
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values, 
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        
        var request = new EmbeddingRequest
        {
            Model = options?.ModelId ?? _modelId,
            Input = values
        };
        
        var response = await _client.PostAsJsonAsync(_endpoint, request, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        
        var embeddingResponse = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (embeddingResponse == null)
        {
            throw new Exception("Failed to parse response");
        }
        
        var embeddings = embeddingResponse.Data
            .Select(x => new Embedding<float>(x.Embedding))
            .ToList();

        return new GeneratedEmbeddings<Embedding<float>>(embeddings);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _client.Dispose();
        _disposed = true;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) { return null; }
}

public class GeneratorConfiguration
{
    public required string Endpoint { get; init; }
    public required string ApiKey { get; init; }
    public required string ModelId { get; init; }
}

internal class EmbeddingRequest
{
    public required string Model { get; init; }
    public required IEnumerable<string> Input { get; init; }

    [JsonPropertyName("encoding_format")] 
    public string EncodingFormat { get; } = "float";
}

internal class EmbeddingResponse
{
    public required List<EmbeddingData> Data { get; init; }
}

internal class EmbeddingData
{
    public required float[] Embedding { get; init; }
    public int Index { get; init; }
}