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