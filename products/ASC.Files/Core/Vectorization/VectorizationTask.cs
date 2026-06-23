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

using Chunk = ASC.Files.Core.Vectorization.Data.Chunk;

namespace ASC.Files.Core.Vectorization;

[Transient]
public class VectorizationTask : DistributedTaskProgress
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private int _tenantId;
    private Guid _userId;
    private int _fileId;

    private static TimeSpan Timeout => TimeSpan.FromMinutes(3);
    private static TimeSpan PulseInterval => Timeout / 3;

    public VectorizationTask() { }

    public VectorizationTask(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(int tenantId, Guid userId, int fileId)
    {
        _tenantId = tenantId;
        _userId = userId;
        _fileId = fileId;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<VectorizationTask>>();

        SocketManager socketManager = null;
        IFileDao<int> fileDao = null;
        IHeartBeat heartBeat = null;
        VectorStoreCollection<Chunk> collection = null;

        File<int> file = null;

        try
        {
            var heartBeatFactory = scope.ServiceProvider.GetRequiredService<IHeartBeatFactory>();

            var key = VectorizationHelper.GetVectorizationKey(_fileId);
            heartBeat = await heartBeatFactory.CreateAsync(key, Timeout, PulseInterval, CancellationToken);

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            var settingsManager = scope.ServiceProvider.GetRequiredService<SettingsManager>();

            var aiAccessSettings = await settingsManager.LoadAsync<TenantAiAccessSettings>();
            if (!aiAccessSettings.Enabled)
            {
                throw new SecurityException(FilesCommonResource.ErrorMessage_AiServicesDisabled);
            }

            socketManager = scope.ServiceProvider.GetRequiredService<SocketManager>();
            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            fileDao = daoFactory.GetFileDao<int>();
            var folderDao = daoFactory.GetFolderDao<int>();

            var vectorStore = scope.ServiceProvider.GetRequiredService<VectorStore>();
            var generatorFactory = scope.ServiceProvider.GetRequiredService<EmbeddingGeneratorFactory>();
            var fileProcessor = scope.ServiceProvider.GetRequiredService<FileTextProcessor>();
            var vectorizationSettings = scope.ServiceProvider.GetRequiredService<VectorizationGlobalSettings>();

            var splitterSettings = new SplitterSettings
            {
                MaxTokensPerChunk = (int)(vectorizationSettings.ChunkSize * 0.75),
                ChunkOverlap = vectorizationSettings.ChunkOverlap
            };

            collection = vectorStore.GetCollection<Chunk>(
                Chunk.IndexName,
                new VectorCollectionOptions
                {
                    Dimension = vectorizationSettings.Model.Dimension,
                    ModelId = vectorizationSettings.Model.Id
                });

            file = await fileDao.GetFileAsync(_fileId);
            if (file == null)
            {
                throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
            }

            var parents = await folderDao.GetParentFoldersAsync(file.ParentId).ToListAsync();
            if (!parents.Exists(x => x.FolderType == FolderType.Knowledge))
            {
                throw new InvalidOperationException("File is not in knowledge folder");
            }

            var agent = parents.FirstOrDefault(x => x.FolderType == FolderType.AiRoom);
            if (agent == null)
            {
                throw new InvalidOperationException("File is not in ai room");
            }

            await collection.EnsureCollectionExistsAsync(CancellationToken);
            var embeddingGenerator = await generatorFactory.CreateAsync(agent);

            var textChunks = await fileProcessor.GetTextChunksAsync(file, splitterSettings);

            foreach (var batch in textChunks.Chunk(vectorizationSettings.ChunksBatchSize))
            {
                var embeddings = await embeddingGenerator.GenerateAsync(batch, cancellationToken: CancellationToken);
                var chunks = batch.Select((text, index) =>
                    new Chunk
                    {
                        Id = Guid.NewGuid(),
                        TenantId = _tenantId,
                        RoomId = agent.Id,
                        Title = file.Title,
                        FileId = file.Id,
                        TextEmbedding = text,
                        Embedding = embeddings[index].Vector.ToArray()
                    }).ToList();

                await collection.UpsertAsync(chunks, CancellationToken);
                await PublishChanges();
            }

            await fileDao.SetVectorizationStatusAsync(file.Id, VectorizationStatus.Completed);

            try
            {
                await socketManager.UpdateFileAsync(file);
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
        catch (HeartBeatExistsException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted;

            try
            {
                if (fileDao != null)
                {
                    await fileDao.SetVectorizationStatusAsync(_fileId, VectorizationStatus.Failed);
                }

                if (collection != null)
                {
                    await collection.DeleteAsync(
                        new VectorSearchOptions<Chunk>
                        {
                            Filter = x => x.TenantId == _tenantId && x.FileId == _fileId
                        },
                        true);
                }

                if (file != null && socketManager != null)
                {
                    await socketManager.UpdateFileAsync(file);
                }
            }
            catch (Exception exception)
            {
                logger.ErrorWithException(exception);
            }
        }
        finally
        {
            IsCompleted = true;
            Percentage = 100;

            try
            {
                await PublishChanges();
                if (heartBeat != null)
                {
                    await heartBeat.StopAsync();
                }
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
    }
}
