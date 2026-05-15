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

namespace ASC.MessagingSystem.Data;

[Singleton]
public class MessagesRepository(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MessagesRepository> logger,
    DbLoginEventMapper loginEventMapper,
    DbAuditEventMapper auditEventMapper,
    IEventBus eventBus)
{
    private static readonly HashSet<MessageAction> _forceSaveAuditActions =
    [
        MessageAction.RoomInviteLinkUsed,
        MessageAction.UserSentEmailChangeInstructions,
        MessageAction.UserSentPasswordChangeInstructions,
        MessageAction.UserUpdatedPassword,
        MessageAction.SendJoinInvite,
        MessageAction.RoomRemoveUser,
        MessageAction.PortalRenamed,
        MessageAction.RoomCreated,
        MessageAction.AgentCreated,
        MessageAction.FileOpenedForChange,
        MessageAction.FormOpenedForFilling
    ];

    public async Task<int> AddAsync(EventMessage message)
    {
        if (IsForceSave(message))
        {
            logger.LogDebug("ForceSave: {Action}", message.Action.ToStringFast());

            return await ForceSave(message);
        }

        await eventBus.PublishAsync(new EventDataIntegrationEvent(message.UserId, message.TenantId)
        {
            RequestMessage = message
        });

        return 0;
    }

    internal static bool IsForceSave(EventMessage message)
    {
        // messages with action code < 2000 are related to login-history
        return (int)message.Action < 2000 || _forceSaveAuditActions.Contains(message.Action);
    }

    private async Task<int> ForceSave(EventMessage message)
    {
        int id;
        if (!string.IsNullOrEmpty(message.UaHeader))
        {
            try
            {
                MessageSettings.AddInfoMessage(message);
            }
            catch (Exception e)
            {
                logger.ErrorWithException("Add " + message.Id, e);
            }
        }

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        await using var ef = await scope.ServiceProvider.GetService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync();
        var historySocketManager = scope.ServiceProvider.GetService<HistorySocketManager>();

        if ((int)message.Action < 2000)
        {
            id = await AddLoginEventAsync(message, ef);
        }
        else
        {
            id = await AddAuditEventAsync(message, ef, historySocketManager);
        }

        return id;
    }

    private async Task<int> AddLoginEventAsync(EventMessage message, MessagesContext dbContext)
    {
        var loginEvent = loginEventMapper.MapManual(message);

        await dbContext.LoginEvents.AddAsync(loginEvent);
        await dbContext.SaveChangesAsync();

        return loginEvent.Id;
    }

    private async Task<int> AddAuditEventAsync(EventMessage message, MessagesContext dbContext, HistorySocketManager historySocketManager)
    {
        var auditEvent = auditEventMapper.MapManual(message);

        await dbContext.AuditEvents.AddAsync(auditEvent);
        await dbContext.SaveChangesAsync();

        if (auditEvent.FilesReferences == null || auditEvent.FilesReferences.Count == 0)
        {
            return auditEvent.Id;
        }

        await historySocketManager.UpdateHistoryAsync(auditEvent.TenantId, auditEvent.FilesReferences);

        return auditEvent.Id;
    }
}

public class EventDataIntegrationEventHandler : IIntegrationEventHandler<EventDataIntegrationEvent>
{
    private readonly ILogger _logger;
    private readonly ChannelWriter<EventData> _channelWriter;
    private readonly ITariffService _tariffService;
    private readonly TenantManager _tenantManager;

    private EventDataIntegrationEventHandler()
    {

    }

    public EventDataIntegrationEventHandler(
        ILogger<EventDataIntegrationEventHandler> logger,
        ITariffService tariffService,
        TenantManager tenantManager,
        ChannelWriter<EventData> channelWriter)
    {
        _logger = logger;
        _channelWriter = channelWriter;
        _tariffService = tariffService;
        _tenantManager = tenantManager;
    }


    public async Task Handle(EventDataIntegrationEvent @event)
    {
        CustomSynchronizationContext.CreateContext();
        using (_logger.BeginScope(new[] { new KeyValuePair<string, object>("integrationEventContext", $"{@event.Id}") }))
        {

            await _tenantManager.SetCurrentTenantAsync(@event.TenantId);
            var tariff = await _tariffService.GetTariffAsync(@event.TenantId);

            if (await _channelWriter.WaitToWriteAsync())
            {
                await _channelWriter.WriteAsync(new EventData(@event.RequestMessage, tariff.State));
            }
        }
    }
}

public class MessageSenderService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MessagesRepository> logger,
    ChannelReader<EventData> channelReader,
    IConfiguration configuration,
    DbLoginEventMapper loginEventMapper,
    DbAuditEventMapper auditEventMapper
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!int.TryParse(configuration["messaging:maxDegreeOfParallelism"], out var maxDegreeOfParallelism))
        {
            maxDegreeOfParallelism = 10;
        }

        List<ChannelReader<EventData>> readers = [channelReader];

        if ((int)(maxDegreeOfParallelism * 0.3) > 0)
        {
            var splitter = channelReader.Split(2, (_, _, p) => p.TariffState == TariffState.Paid ? 0 : 1, stoppingToken);
            var premiumChannels = splitter[0].Split((int)(maxDegreeOfParallelism * 0.7), null, stoppingToken);
            var freeChannel = splitter[1].Split((int)(maxDegreeOfParallelism * 0.3), null, stoppingToken);
            readers = premiumChannels.Union(freeChannel).ToList();
        }

        var tasks = readers.Select(reader1 => Task.Run(async () =>
            {
                await foreach (var eventData in reader1.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await using var scope = serviceScopeFactory.CreateAsyncScope();
                        await using var ef = await scope.ServiceProvider.GetService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(stoppingToken);
                        var historySocketManager = scope.ServiceProvider.GetService<HistorySocketManager>();

                        var dict = new Dictionary<string, ClientInfo>();
                        var message = eventData.RequestMessage;
                        var tenantId = message.TenantId;
                        try
                        {
                            var references = new List<DbFilesAuditReference>();
                            if (!string.IsNullOrEmpty(message.UaHeader))
                            {
                                try
                                {
                                    MessageSettings.AddInfoMessage(message, dict);
                                }
                                catch (Exception e)
                                {
                                    logger.ErrorFlushCache(message.Id, e);
                                }
                            }

                            if (!MessagesRepository.IsForceSave(message))
                            {
                                // messages with action code < 2000 are related to login-history
                                if ((int)message.Action < 2000)
                                {
                                    var loginEvent = loginEventMapper.MapManual(message);
                                    await ef.LoginEvents.AddAsync(loginEvent, stoppingToken);
                                }
                                else
                                {
                                    var auditEvent = auditEventMapper.MapManual(message);
                                    await ef.AuditEvents.AddAsync(auditEvent, stoppingToken);

                                    if (auditEvent.FilesReferences is { Count: > 0 })
                                    {
                                        references.AddRange(auditEvent.FilesReferences);
                                    }
                                }
                            }


                            await ef.SaveChangesAsync(stoppingToken);

                            if (references.Count <= 0)
                            {
                                continue;
                            }

                            await historySocketManager.UpdateHistoryAsync(tenantId, references);
                        }
                        catch (Exception e)
                        {
                            logger.ErrorFlushCache(tenantId, e);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.ErrorSendMassage(e);
                    }

                }
            }, stoppingToken))
            .ToList();

        await Task.WhenAll(tasks);
    }
}

public record EventData(EventMessage RequestMessage, TariffState TariffState);