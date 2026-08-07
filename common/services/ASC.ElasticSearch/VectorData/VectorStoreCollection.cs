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

namespace ASC.ElasticSearch.VectorData;

internal static class ExistingCollectionsCache
{
    private static readonly ConcurrentDictionary<string, byte> _collections = new();

    public static bool Contains(string name)
    {
        return _collections.ContainsKey(name);
    }

    public static void Add(string name)
    {
        _collections.TryAdd(name, 0);
    }
}

public class VectorStoreCollection<TRecord>(
    OpenSearchClient? openSearchClient,
    VectorCollectionOptions options,
    string name) where TRecord: class
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly IndexSettings _settings = new(new Dictionary<string, object> { { "index.knn", true } });

    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        EnsureClientConfigured();

        if (ExistingCollectionsCache.Contains(name))
        {
            return;
        }

        if (!await CollectionExistsAsync(cancellationToken))
        {
            await CreateCollectionAsync(cancellationToken);
        }

        ExistingCollectionsCache.Add(name);
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

        QueryContainer lexicalQuery = new MultiMatchQuery
        {
            Query = searchQuery.LexicalQuery,
            Fields = lexicalFields
        };

        QueryContainer knnQuery = new KnnQuery
        {
            Field = vectorField,
            Vector = searchQuery.Vector,
            K = k
        };

        if (searchQuery.Filter != null)
        {
            var translator = new OpenSearchFilterTranslator<TRecord>(openSearchClient!.Infer);
            var filter = translator.Translate(searchQuery.Filter);

            lexicalQuery = new BoolQuery { Must = [lexicalQuery], Filter = [filter] };
            knnQuery = new BoolQuery { Must = [knnQuery], Filter = [filter] };
        }

        var request = new SearchRequest(name)
        {
            Size = searchQuery.Top,
            SearchPipeline = HybridSearchPipeline.Name,
            Query = new HybridQuery { Queries = [lexicalQuery, knnQuery] }
        };

        var response = await OperationHandler.RunAsync<ISearchResponse<TRecord>, OpenSearchClientException>(
            name,
            "hybrid_search",
            async () => await openSearchClient!.SearchAsync<TRecord>(request, cancellationToken));

        foreach (var hit in response.Hits)
        {
            if (hit.Source != null)
            {
                yield return hit.Source;
            }
        }
    }

    public async Task DeleteAsync(VectorSearchOptions<TRecord>? searchOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (openSearchClient is null)
        {
            return;
        }

        await OperationHandler.RunAsync<RefreshResponse, OpenSearchClientException>(
            name,
            "refresh",
            async () => await openSearchClient!.Indices.RefreshAsync(name, ct: cancellationToken));

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

    private async Task CreateCollectionAsync(CancellationToken cancellationToken)
    {
        var properties = OpenSearchVectorMapper.BuildPropertyMappings(typeof(TRecord), options.Dimension);
        var meta = new Dictionary<string, object> { { "model", options.ModelId } };

        try
        {
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
        catch (VectorStoreException)
        {
            if (!await CollectionExistsAsync(cancellationToken))
            {
                throw;
            }
        }
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
}
