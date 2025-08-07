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

using ASC.AI.Core.Embedding;
using ASC.AI.Service.Vectorization.Data;
using ASC.Common.Threading;
using ASC.ElasticSearch.VectorData;

using SecurityContext = ASC.Core.SecurityContext;

namespace ASC.AI.Core.Vectorization;

public abstract class VectorizationTask<T>(IServiceScopeFactory serviceScopeFactory) 
    : DistributedTaskProgress where T : VectorizationTaskData
{
    protected T Data { get; private set; } = null!;

    private const int BatchSize = 10;
    
    // ReSharper disable once StaticMemberInGenericType
    private static readonly SplitterSettings _splitterSettings = new()
    {
        MaxTokensPerChunk = 384, 
        ChunkOverlap = 0.2f
    };
    
    private int _tenantId;
    private Guid _userId;

    public void Init(string taskId, int tenantId, Guid userId, T data)
    {
        Id = taskId;
        Status = DistributedTaskStatus.Created;
        _tenantId = tenantId;
        _userId = userId;
        Data = data;
    }
    
    public void Init(int tenantId, Guid userId, T data)
    {
        _tenantId = tenantId;
        _userId = userId;
        Data = data;
    }

    protected abstract IAsyncEnumerable<File<int>> GetFilesAsync(IServiceProvider serviceProvider);
    
    protected override async Task DoJob()
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        
        var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
        await tenantManager.SetCurrentTenantAsync(_tenantId);
        
        var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
        await securityContext.AuthenticateMeWithoutCookieAsync(_userId);
        
        var fileProcessor = scope.ServiceProvider.GetRequiredService<FileTextProcessor>();
        var embeddingGeneratorFactory = scope.ServiceProvider.GetRequiredService<EmbeddingGeneratorFactory>();
        var embeddingGenerator = embeddingGeneratorFactory.Create();
        var vectorStore = scope.ServiceProvider.GetRequiredService<VectorStore>();
        
        var collection = vectorStore.GetCollection<Chunk>("embedding", new VectorCollectionOptions
        {
            Dimension = 1024,
            ModelId = "intfloat/multilingual-e5-large-instruct"
        });

        await collection.EnsureCollectionExistsAsync(CancellationToken);

        await foreach (var file in GetFilesAsync(scope.ServiceProvider))
        {
            await VectorizeFileAsync(file, fileProcessor, embeddingGenerator, collection);
        }
        
        this.Percentage = 100;
        await PublishChanges();
    }
    
    private async Task VectorizeFileAsync(
        File<int> file, 
        FileTextProcessor fileProcessor,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        VectorStoreCollection<Chunk> vectorStoreCollection)
    {
        var textChunks = await fileProcessor.GetTextChunksAsync(file, _splitterSettings);

        foreach (var batch in textChunks.Chunk(BatchSize))
        {
            var embeddings = await embeddingGenerator.GenerateAsync(batch, cancellationToken: CancellationToken);
            var chunks = batch.Select((text, index) => 
                new Chunk 
                { 
                    Id = Guid.NewGuid(), 
                    TenantId = _tenantId,
                    FileId = file.Id, 
                    TextEmbedding = text, 
                    Embedding = embeddings[index].Vector.ToArray() 
                }).ToList();

            await vectorStoreCollection.UpsertAsync(chunks, CancellationToken);
        }
    }
}