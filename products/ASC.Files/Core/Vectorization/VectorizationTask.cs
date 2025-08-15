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

using Chunk = ASC.Files.Core.Vectorization.Data.Chunk;

namespace ASC.Files.Core.Vectorization;

public enum VectorizationTaskType
{
    Copy,
    Upload
}

public abstract class VectorizationTask : DistributedTaskProgress
{
    public Guid UserId { get; set; }
    public VectorizationTaskType Type { get; set; }
}

public abstract class VectorizationTask<T> 
    : VectorizationTask where T : VectorizationTaskData
{
    private readonly IServiceScopeFactory _serviceScopeFactory = null!;

    protected VectorizationTask() { }

    protected VectorizationTask(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    protected T Data { get; private set; } = null!;
    protected ILogger Logger { get; private set; } = null!;

    private int _tenantId;
    
    public virtual void Init(int tenantId, Guid userId, T data)
    {
        _tenantId = tenantId;
        UserId = userId;
        Data = data;
    }

    public void Init(string taskId, int tenantId, Guid userId, T data)
    {
        Id = taskId;
        Init(tenantId, userId, data);
    }

    protected abstract IAsyncEnumerable<File<int>> GetFilesAsync(IServiceProvider serviceProvider);
    protected abstract int GetTotalFilesCount();
    
    protected override async Task DoJob()
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            Logger = scope.ServiceProvider.GetRequiredService<ILogger<VectorizationTask<T>>>();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(UserId);

            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            var fileProcessor = scope.ServiceProvider.GetRequiredService<FileTextProcessor>();
            var generatorFactory = scope.ServiceProvider.GetRequiredService<EmbeddingGeneratorFactory>();
            var vectorStore = scope.ServiceProvider.GetRequiredService<VectorStore>();
            var socketManager = scope.ServiceProvider.GetRequiredService<SocketManager>();
            var settings = scope.ServiceProvider.GetRequiredService<VectorizationSettings>();

            var fileDao = daoFactory.GetFileDao<int>();
            var embeddingGenerator = generatorFactory.Create();
            
            var splitterSettings = new SplitterSettings
            {
                MaxTokensPerChunk = (int)(generatorFactory.Model.ContextLength * 0.75),
                ChunkOverlap = settings.ChunkOverlap
            };
            
            var folderDao = daoFactory.GetFolderDao<int>();
            var room = await folderDao.GetParentFoldersAsync(Data.ParentId)
                .FirstOrDefaultAsync(x => x.FolderType == FolderType.AiRoom);

            var collection = vectorStore.GetCollection<Chunk>(Chunk.IndexName,
                new VectorCollectionOptions
                {
                    Dimension = generatorFactory.Model.Dimension, 
                    ModelId = generatorFactory.Model.Id
                });
            
            await collection.EnsureCollectionExistsAsync(CancellationToken);

            var totalFiles = GetTotalFilesCount();
            var currentFileIndex = 0;

            await foreach (var file in GetFilesAsync(scope.ServiceProvider))
            {
                var notify = false;
                
                try
                {
                    await VectorizeFileAsync(totalFiles, currentFileIndex, room.Id, file, fileProcessor, splitterSettings, 
                        settings, embeddingGenerator, collection);
                    currentFileIndex++;
                    notify = true;
                }
                catch (Exception e)
                {
                    Logger.ErrorWithException(e);
                    await ClenUpAsync(fileDao, file.Id);
                    currentFileIndex++;
                }
                
                if (notify)
                {
                    await socketManager.CreateFileAsync(file);
                }
            }

            if (Status <= DistributedTaskStatus.Running)
            {
                Status = DistributedTaskStatus.Completed;
            }
        }
        catch (Exception e)
        {
            Logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted;
        }
        finally
        {
            IsCompleted = true;
            this.Percentage = 100;
            await PublishChanges();
        }
    }
    
    private async Task VectorizeFileAsync(
        int totalFiles,
        int currentFileIndex,
        int roomId,
        File<int> file,
        FileTextProcessor fileProcessor,
        SplitterSettings splitterSettings,
        VectorizationSettings vectorizationSettings,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        VectorStoreCollection<Chunk> vectorStoreCollection)
    {
        var textChunks = await fileProcessor.GetTextChunksAsync(file, splitterSettings);
        var totalBatches = (textChunks.Count + vectorizationSettings.ChunksBatchSize - 1) / vectorizationSettings.ChunksBatchSize;
        var currentBatch = 0;

        foreach (var batch in textChunks.Chunk(vectorizationSettings.ChunksBatchSize))
        {
            var embeddings = await embeddingGenerator.GenerateAsync(batch, cancellationToken: CancellationToken);
            var chunks = batch.Select((text, index) => 
                new Chunk 
                { 
                    Id = Guid.NewGuid(), 
                    TenantId = _tenantId,
                    RoomId = roomId,
                    FileId = file.Id, 
                    TextEmbedding = text, 
                    Embedding = embeddings[index].Vector.ToArray() 
                }).ToList();

            await vectorStoreCollection.UpsertAsync(chunks, CancellationToken);
            
            currentBatch++;

            var fileProgress = (double)currentBatch / totalBatches;
            var totalProgress = (currentFileIndex + fileProgress) / totalFiles;
            this.Percentage = Math.Min(99, (int)(totalProgress * 100));
            await PublishChanges();
        }
    }

    private async Task ClenUpAsync(IFileDao<int> fileDao, int fileId)
    {
        try
        {
            await fileDao.DeleteFileAsync(fileId);
        }
        catch (Exception e)
        {
            Logger.ErrorWithException(e);
            Exception = e;
        }
    }
}