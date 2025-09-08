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

[Transient]
public class VectorizationTask : DistributedTaskProgress
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private int _tenantId;
    private int _fileId;
    private Guid _userId;

    private ILogger _logger;
    private IFileDao<int> _fileDao;
    
    public VectorizationTask() { }
    
    public VectorizationTask(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(int tenantId, Guid userId, int fileId)
    {
        _tenantId = tenantId;
        _fileId = fileId;
        _userId = userId;
    }

    public void Init(string taskId, int tenantId, Guid userId, int fileId)
    {
        Id = taskId;
        Init(tenantId, userId, fileId);
    }

    protected override async Task DoJob()
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            _logger = scope.ServiceProvider.GetRequiredService<ILogger<VectorizationTask>>();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();

            _fileDao = daoFactory.GetFileDao<int>();
            
            var file = await _fileDao.GetFileAsync(_fileId);
            if (file == null)
            {
                throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
            }
            
            var folderDao = daoFactory.GetFolderDao<int>();
            var parents = await folderDao.GetParentFoldersAsync(file.ParentId).ToListAsync();

            if (!parents.Exists(x => x.FolderType == FolderType.Knowledge))
            {
                throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
            }
            
            var room = parents.FirstOrDefault(x => x.FolderType == FolderType.AiRoom);
            if (room == null)
            {
                throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
            }
            
            var fileProcessor = scope.ServiceProvider.GetRequiredService<FileTextProcessor>();
            var generatorFactory = scope.ServiceProvider.GetRequiredService<EmbeddingGeneratorFactory>();
            var vectorStore = scope.ServiceProvider.GetRequiredService<VectorStore>();
            var vectorizationSettings = scope.ServiceProvider.GetRequiredService<VectorizationSettings>();
            
            var embeddingGenerator = generatorFactory.Create();
            var splitterSettings = new SplitterSettings
            {
                MaxTokensPerChunk = (int)(generatorFactory.Model.ContextLength * 0.75),
                ChunkOverlap = vectorizationSettings.ChunkOverlap
            };

            var collection = vectorStore.GetCollection<Chunk>(
                Chunk.IndexName,
                new VectorCollectionOptions
                {
                    Dimension = generatorFactory.Model.Dimension, 
                    ModelId = generatorFactory.Model.Id
                });
            
            await collection.EnsureCollectionExistsAsync(CancellationToken);

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
                        RoomId = room.Id,
                        FileId = file.Id, 
                        TextEmbedding = text, 
                        Embedding = embeddings[index].Vector.ToArray() 
                    }).ToList();

                await collection.UpsertAsync(chunks, CancellationToken);
            
                currentBatch++;

                var progress = (double)currentBatch / totalBatches;
                this.Percentage = Math.Min(99, (int)(progress * 100));
                await PublishChanges();
            }
            
            await _fileDao.SetVectorizationStatusAsync(file.Id, VectorizationStatus.Completed);

            Status = DistributedTaskStatus.Completed;
        }
        catch (Exception e)
        {
            _logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted;
            
            await _fileDao.SetVectorizationStatusAsync(_fileId, VectorizationStatus.Failed);
        }
        finally
        {
            IsCompleted = true;
            this.Percentage = 100;
            await PublishChanges();
        }
    }
}