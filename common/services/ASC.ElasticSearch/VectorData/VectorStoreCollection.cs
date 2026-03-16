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

using HttpMethod = OpenSearch.Net.HttpMethod;

namespace ASC.ElasticSearch.VectorData;

public class VectorStoreCollection<TRecord>(
    OpenSearchClient? openSearchClient,
    VectorCollectionOptions options,
    TaskScheduler scheduler,
    ILogger<VectorStore> logger,
    string name) where TRecord: class
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly IndexSettings _settings = new(new Dictionary<string, object> { { "index.knn", true } });

    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        EnsureClientConfigured();
        
        if (await CollectionExistsAsync(cancellationToken))
        {
            return;
        }

        var properties = OpenSearchVectorMapper.BuildPropertyMappings(typeof(TRecord), options.Dimension);
        var meta = new Dictionary<string, object> { { "model", options.ModelId } };

        await OperationHandler.RunAsync<CreateIndexResponse, OpenSearchClientException>(
            name,
            "create_collection", 
            async () => await openSearchClient!.Indices
                .CreateAsync(new CreateIndexRequest(name) 
                {
                    Settings = _settings,
                    Mappings = new TypeMapping
                    {
                        Properties = properties,
                        Meta = meta
                    }
                }, cancellationToken));
    }

    public async Task UpsertAsync(List<TRecord> records, CancellationToken cancellationToken = default)
    {
        EnsureClientConfigured();

        if (records is { Count: <= 0 })
        {
            return;
        }

        await OperationHandler.RunAsync<BulkResponse, OpenSearchClientException>(
            name,
            "bulk_upsert",
            async () => await openSearchClient.IndexManyAsync(records, name, cancellationToken));
    }

    public async IAsyncEnumerable<TRecord> SearchAsync(
        Expression<Func<TRecord, object>> propertySelector,
        float[] vector,
        int top,
        VectorSearchOptions<TRecord>? searchOptions = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureClientConfigured();
        ValidateSearchArguments(propertySelector, vector, top);
        
        var query = new KnnQuery
        {
            Vector = vector,
            Field = propertySelector.Body,
            K = top
        };
        
        if (searchOptions is { Filter: not null })
        {
            var translator = new OpenSearchFilterTranslator<TRecord>(openSearchClient!.Infer);
            var filter = translator.Translate(searchOptions.Filter);
            query.Filter = filter;
        }

        var response = await OperationHandler.RunAsync<ISearchResponse<TRecord>, OpenSearchClientException>(
            name,
            "semantic_search",
            async () => await openSearchClient!.SearchAsync<TRecord>(
                new SearchRequest(name) { Query = query, Size = top }, 
                cancellationToken));

        foreach (var hit in response.Hits)
        {
            yield return hit.Source;
        }
    }

    public async IAsyncEnumerable<TRecord> HybridSearchAsync(
        HybridSearchQuery<TRecord> searchQuery,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureClientConfigured();
        ValidateHybridSearchQuery(searchQuery);

        var vectorField = ResolveFieldName(searchQuery.VectorField);
        var lexicalFields = searchQuery.LexicalFields
            .Select(ResolveFieldName)
            .ToArray();
        
        var k = searchQuery.K ?? searchQuery.Top;

        var builder = new HybridSearchRequestBuilder(searchQuery.Top)
            .AddMultiMatch(searchQuery.LexicalQuery, lexicalFields)
            .AddKnn(vectorField, searchQuery.Vector, k);

        if (searchQuery.Filter != null)
        {
            var translator = new OpenSearchFilterTranslator<TRecord>(openSearchClient!.Infer);
            builder.WithFilter(translator.TranslateToJsonElement(searchQuery.Filter));
        }

        var request = builder.Build();
        
        var requestParameters = new SearchRequestParameters
        {
            QueryString = new Dictionary<string, object>
            {
                ["search_pipeline"] = HybridSearchPipeline.Name
            }
        };

        var response = await OperationHandler.RunAsync<SearchResponse<TRecord>, OpenSearchClientException>(
            name,
            "hybrid_search",
            async () => await ((IOpenSearchClient)openSearchClient!).LowLevel.DoRequestAsync<SearchResponse<TRecord>>(
                HttpMethod.POST,
                $"/{Uri.EscapeDataString(name)}/_search",
                cancellationToken,
                PostData.String(JsonSerializer.Serialize(request)),
                requestParameters));

        if (response.Hits == null)
        {
            yield break;
        }

        foreach (var hit in response.Hits)
        {
            if (hit.Source != null)
            {
                yield return hit.Source;
            }
        }
    }

    public async Task DeleteAsync(VectorSearchOptions<TRecord>? searchOptions = null, bool immediate = false,
        CancellationToken cancellationToken = default)
    {
        if (openSearchClient is null)
        {
            return;
        }

        if (immediate)
        {
            await DeleteAsync(searchOptions, cancellationToken);
            return;
        }
        
        var task = new Task(async void () =>
        {
            try
            {
                await DeleteAsync(searchOptions, cancellationToken);
            }
            catch (Exception e)
            {
                logger.ErrorWithException("Failed to delete file vector data", e);
            }
        }, cancellationToken, TaskCreationOptions.LongRunning);
        
        task.Start(scheduler);
    }

    private async Task DeleteAsync(VectorSearchOptions<TRecord>? searchOptions = null, CancellationToken cancellationToken = default)
    {
        var request = new DeleteByQueryRequest(name);
        
        if (searchOptions is { Filter: not null })
        {
            var translator = new OpenSearchFilterTranslator<TRecord>(openSearchClient!.Infer);
            var filter = translator.Translate(searchOptions.Filter);
            request.Query = filter;
        }

        await OperationHandler.RunAsync<DeleteByQueryResponse, OpenSearchClientException>(
            name,
            "bulk_delete",
            async () => await openSearchClient!.DeleteByQueryAsync(request, cancellationToken));
    }
    
    private async Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        var response = await OperationHandler.RunAsync<ExistsResponse, OpenSearchClientException>(
            name,
            "exists_check",
            async () => await openSearchClient!.Indices.ExistsAsync(name, ct: cancellationToken));

        return response.ApiCall.HttpStatusCode != 404 && response.Exists;
    }

    private void EnsureClientConfigured()
    {
        if (openSearchClient is null)
        {
            throw new InvalidOperationException("OpenSearch is not configured. Check the OpenSearch connection settings.");
        }
    }

    private static void ValidateSearchArguments(Expression<Func<TRecord, object>> propertySelector, float[] vector, int top)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);
        ArgumentNullException.ThrowIfNull(vector);

        if (top <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(top), @"Top must be greater than 0.");
        }
    }

    private static void ValidateHybridSearchQuery(HybridSearchQuery<TRecord> searchQuery)
    {
        ArgumentNullException.ThrowIfNull(searchQuery);
        ValidateSearchArguments(searchQuery.VectorField, searchQuery.Vector, searchQuery.Top);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchQuery.LexicalQuery);
        ArgumentNullException.ThrowIfNull(searchQuery.LexicalFields);

        if (searchQuery.LexicalFields.Count <= 0)
        {
            throw new ArgumentException(@"At least one lexical field must be specified.", nameof(searchQuery));
        }

        if (searchQuery.K is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(searchQuery), @"K must be greater than 0.");
        }
    }

    private string ResolveFieldName(Expression<Func<TRecord, object>> selector)
    {
        var property = selector.Body switch
        {
            MemberExpression { Member: PropertyInfo propertyInfo } => propertyInfo,
            UnaryExpression 
            { 
                NodeType: ExpressionType.Convert, Operand: MemberExpression 
                { 
                    Member: PropertyInfo propertyInfo 
                } 
            } => propertyInfo,
            _ => throw new NotSupportedException("Only direct property selectors are supported.")
        };

        return openSearchClient!.Infer.Field(property);
    }

    private sealed class HybridSearchRequestBuilder(int size)
    {
        private readonly List<HybridSearchQueryNode> _queries = [];
        private JsonElement? _filter;

        public HybridSearchRequestBuilder AddMultiMatch(string query, string[] fields)
        {
            _queries.Add(new HybridSearchQueryNode
            {
                MultiMatch = new HybridMultiMatchNode { Query = query, Fields = fields }
            });

            return this;
        }

        public HybridSearchRequestBuilder AddKnn(string field, float[] vector, int k)
        {
            _queries.Add(new HybridSearchQueryNode
            {
                Knn = new HybridKnnNode
                {
                    Fields = new Dictionary<string, JsonElement>
                    {
                        [field] = JsonSerializer.SerializeToElement(new HybridKnnFieldNode { Vector = vector, K = k })
                    }
                }
            });

            return this;
        }

        public HybridSearchRequestBuilder WithFilter(JsonElement filter)
        {
            _filter = filter;

            return this;
        }

        public HybridSearchRequest Build()
        {
            var queries = _filter.HasValue
                ? _queries.Select(q => new HybridSearchQueryNode
                {
                    Bool = new HybridBoolNode { Must = [q], Filter = [_filter.Value] }
                }).ToArray()
                : _queries.ToArray();

            return new HybridSearchRequest
            {
                Size = size,
                Query = new HybridSearchQueryNode
                {
                    Hybrid = new HybridNode { Queries = queries }
                }
            };
        }
    }

    private sealed class HybridSearchRequest
    {
        [JsonPropertyName("size")]
        public required int Size { get; init; }

        [JsonPropertyName("query")]
        public required HybridSearchQueryNode Query { get; init; }
    }

    private sealed class HybridSearchQueryNode
    {
        [JsonPropertyName("hybrid")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HybridNode? Hybrid { get; init; }

        [JsonPropertyName("bool")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HybridBoolNode? Bool { get; init; }

        [JsonPropertyName("multi_match")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HybridMultiMatchNode? MultiMatch { get; init; }

        [JsonPropertyName("knn")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HybridKnnNode? Knn { get; init; }
    }

    private sealed class HybridNode
    {
        [JsonPropertyName("queries")]
        public required HybridSearchQueryNode[] Queries { get; init; }
    }

    private sealed class HybridBoolNode
    {
        [JsonPropertyName("must")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HybridSearchQueryNode[]? Must { get; init; }

        [JsonPropertyName("filter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public JsonElement[]? Filter { get; init; }
    }

    private sealed class HybridMultiMatchNode
    {
        [JsonPropertyName("query")]
        public required string Query { get; init; }

        [JsonPropertyName("fields")]
        public required string[] Fields { get; init; }
    }

    private sealed class HybridKnnNode
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Fields { get; init; } = [];
    }

    private sealed class HybridKnnFieldNode
    {
        [JsonPropertyName("vector")]
        public required float[] Vector { get; init; }

        [JsonPropertyName("k")]
        public required int K { get; init; }
    }
}
