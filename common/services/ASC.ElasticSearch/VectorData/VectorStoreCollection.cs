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
namespace ASC.ElasticSearch.VectorData;

public class VectorStoreCollection<TRecord>(
    OpenSearchClient openSearchClient,
    VectorCollectionOptions options,
    string name) where TRecord: class
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly IndexSettings _settings = new(new Dictionary<string, object> { { "index.knn", true } });
    
    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        if (await CollectionExistsAsync(cancellationToken))
        {
            return;
        }

        var properties = OpenSearchVectorMapper.BuildPropertyMappings(typeof(TRecord), options.Dimension);
        var meta = new Dictionary<string, object> { { "model", options.ModelId } };

        await OperationHandler.RunAsync<CreateIndexResponse, OpenSearchClientException>(
            name,
            "create_collection", 
            async () => await openSearchClient.Indices
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
        ArgumentNullException.ThrowIfNull(propertySelector);
        ArgumentNullException.ThrowIfNull(vector);

        if (top <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(top), @"Top must be greater than 0.");
        }
        
        var query = new KnnQuery
        {
            Vector = vector,
            Field = propertySelector.Body,
            K = top
        };
        
        if (searchOptions is { Filter: not null })
        {
            var translator = new OpenSearchFilterTranslator<TRecord>(openSearchClient.Infer);
            var filter = translator.Translate(searchOptions.Filter);
            query.Filter = filter;
        }

        var response = await OperationHandler.RunAsync<ISearchResponse<TRecord>, OpenSearchClientException>(
            name,
            "semantic_search",
            async () => await openSearchClient.SearchAsync<TRecord>(
                new SearchRequest(name) { Query = query, Size = top }, 
                cancellationToken));

        foreach (var hit in response.Hits)
        {
            yield return hit.Source;
        }
    }

    public async Task DeleteAsync(VectorSearchOptions<TRecord>? searchOptions = null, CancellationToken cancellationToken = default)
    {
        var request = new DeleteByQueryRequest(name);
        
        if (searchOptions is { Filter: not null })
        {
            var translator = new OpenSearchFilterTranslator<TRecord>(openSearchClient.Infer);
            var filter = translator.Translate(searchOptions.Filter);
            request.Query = filter;
        }

        await OperationHandler.RunAsync<DeleteByQueryResponse, OpenSearchClientException>(
            name,
            "bulk_delete",
            async () => await openSearchClient.DeleteByQueryAsync(request, cancellationToken));
    }
    
    private async Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        var response = await OperationHandler.RunAsync<ExistsResponse, OpenSearchClientException>(
            name,
            "exists_check",
            async () => await openSearchClient.Indices.ExistsAsync(name, ct: cancellationToken));

        return response.ApiCall.HttpStatusCode != 404 && response.Exists;
    }
}