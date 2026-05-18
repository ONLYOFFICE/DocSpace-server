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

namespace ASC.EventBus.Extensions.Logger.Services;

public class IntegrationEventLogService : IIntegrationEventLogService
{
    private readonly List<Type> _eventTypes;
    private readonly IDbContextFactory<IntegrationEventLogContext> _dbContextFactory;

    public IntegrationEventLogService(IDbContextFactory<IntegrationEventLogContext> dbContextFactory)
    {
        _eventTypes = Assembly.Load(Assembly.GetEntryAssembly().FullName)
            .GetTypes()
            .Where(t => t.Name.EndsWith(nameof(IntegrationEvent)))
            .ToList();
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IEnumerable<IntegrationEventLogEntry>> RetrieveEventLogsPendingToPublishAsync(Guid transactionId)
    {
        var tid = transactionId.ToString();

        await using var integrationEventLogContext = await _dbContextFactory.CreateDbContextAsync();

        var result = await Queries.IntegrationEventLogEntriesAsync(integrationEventLogContext, tid).ToListAsync();

        if (result.Count != 0)
        {
            return result
                .OrderBy(o => o.CreateOn)
                .Select(e => e.DeserializeJsonContent(_eventTypes.Find(t => t.Name == e.EventTypeShortName)))
                .ToList();
        }

        return new List<IntegrationEventLogEntry>();
    }

    public async Task SaveEventAsync(IntegrationEvent @event, IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var eventLogEntry = new IntegrationEventLogEntry(@event, transaction.TransactionId);

        await using var integrationEventLogContext = await _dbContextFactory.CreateDbContextAsync();
        await integrationEventLogContext.Database.UseTransactionAsync(transaction.GetDbTransaction());
        integrationEventLogContext.IntegrationEventLogs.Add(eventLogEntry);

        await integrationEventLogContext.SaveChangesAsync();
    }

    public async Task MarkEventAsPublishedAsync(Guid eventId)
    {
        await UpdateEventStatusAsync(eventId, EventState.Published);
    }

    public async Task MarkEventAsInProgressAsync(Guid eventId)
    {
        await UpdateEventStatusAsync(eventId, EventState.InProgress);
    }

    public async Task MarkEventAsFailedAsync(Guid eventId)
    {
        await UpdateEventStatusAsync(eventId, EventState.PublishedFailed);
    }

    private async Task UpdateEventStatusAsync(Guid eventId, EventState status)
    {
        await using var integrationEventLogContext = await _dbContextFactory.CreateDbContextAsync();
        var eventLogEntry = await Queries.IntegrationEventLogEntryAsync(integrationEventLogContext, eventId);
        eventLogEntry.State = status;

        if (status == EventState.InProgress)
        {
            eventLogEntry.TimesSent++;
        }

        integrationEventLogContext.IntegrationEventLogs.Update(eventLogEntry);

        await integrationEventLogContext.SaveChangesAsync();
    }
}

static file class Queries
{
    public static readonly Func<IntegrationEventLogContext, string, IAsyncEnumerable<IntegrationEventLogEntry>>
        IntegrationEventLogEntriesAsync = EF.CompileAsyncQuery(
            (IntegrationEventLogContext ctx, string tid) =>
                ctx.IntegrationEventLogs
                    .Where(e => e.TransactionId == tid && e.State == EventState.NotPublished));

    public static readonly Func<IntegrationEventLogContext, Guid, Task<IntegrationEventLogEntry>>
        IntegrationEventLogEntryAsync = EF.CompileAsyncQuery(
            (IntegrationEventLogContext ctx, Guid eventId) =>
                ctx.IntegrationEventLogs.Single(ie => ie.EventId == eventId));
}