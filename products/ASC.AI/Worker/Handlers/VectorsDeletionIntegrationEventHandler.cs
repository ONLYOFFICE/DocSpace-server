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

namespace ASC.AI.Worker.Handlers;

[Scope]
public class VectorsDeletionIntegrationEventHandler(
    ILogger<VectorsDeletionIntegrationEventHandler> logger,
    TenantManager tenantManager,
    IHeartBeatFactory heartBeatFactory,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    VectorStore vectorStore)
    : IIntegrationEventHandler<VectorsDeletionIntegrationEvent>
{
    private static TimeSpan Timeout => TimeSpan.FromMinutes(1);
    private static TimeSpan PulseInterval => Timeout / 3;

    public async Task Handle(VectorsDeletionIntegrationEvent @event)
    {
        CustomSynchronizationContext.CreateContext();
        using (logger.BeginScope(new[] { new KeyValuePair<string, object>("integrationEventContext", $"{@event.Id}-{Program.AppName}") }))
        {
            logger.InformationHandlingIntegrationEvent(@event.Id, Program.AppName, @event);

            await tenantManager.SetCurrentTenantAsync(@event.TenantId);

            IHeartBeat heartBeat;
            try
            {
                var key = VectorizationHelper.GetVectorizationKey(@event.FileId);
                heartBeat = await heartBeatFactory.CreateAsync(key, Timeout, PulseInterval);
            }
            catch (HeartBeatExistsException)
            {
                return;
            }

            try
            {
                await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

                if (!await filesDbContext.IsVectorizationDeletedAsync(@event.TenantId, @event.FileId))
                {
                    return;
                }

                var collection = vectorStore.GetCollection<Chunk>(Chunk.IndexName, null);
                await collection.DeleteAsync(
                    new VectorSearchOptions<Chunk>
                    {
                        Filter = x => x.TenantId == @event.TenantId && x.FileId == @event.FileId
                    });

                await filesDbContext.DeleteVectorizationIfDeletedAsync(@event.TenantId, @event.FileId);
            }
            finally
            {
                await heartBeat.StopAsync();
            }
        }
    }
}
