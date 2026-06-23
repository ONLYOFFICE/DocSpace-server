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

namespace ASC.EventBus.InMemory;

/// <summary>
/// In-process event bus implementation that dispatches events directly to handlers
/// within the same process. No external message broker required.
/// Designed for monolith/standalone deployment mode.
/// </summary>
public class EventBusInMemory(
    IEventBusSubscriptionsManager subsManager,
    IServiceProvider serviceProvider,
    ILogger<EventBusInMemory> logger) : IEventBus
{
    public async Task PublishAsync(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;

        if (!subsManager.HasSubscriptionsForEvent(eventName))
        {
            logger.DebugNoSubscriptions(eventName);
            return;
        }

        var subscriptions = subsManager.GetHandlersForEvent(eventName);

        foreach (var subscription in subscriptions)
        {
            try
            {
                if (subscription.IsDynamic)
                {
                    await ProcessDynamicHandler(subscription.HandlerType, @event);
                }
                else
                {
                    await ProcessTypedHandler(subscription.HandlerType, @event);
                }
            }
            catch (Exception ex)
            {
                logger.ErrorHandlingEvent(eventName, subscription.HandlerType.Name, ex);
            }
        }
    }

    public Task SubscribeAsync<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = subsManager.GetEventKey<T>();
        logger.InformationSubscribing(eventName, typeof(TH).Name);

        subsManager.AddSubscription<T, TH>();

        return Task.CompletedTask;
    }

    public Task SubscribeDynamicAsync<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        logger.InformationSubscribingDynamic(eventName, typeof(TH).Name);

        subsManager.AddDynamicSubscription<TH>(eventName);

        return Task.CompletedTask;
    }

    public void Unsubscribe<T, TH>()
        where TH : IIntegrationEventHandler<T>
        where T : IntegrationEvent
    {
        subsManager.RemoveSubscription<T, TH>();
    }

    public void UnsubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        subsManager.RemoveDynamicSubscription<TH>(eventName);
    }

    private async Task ProcessTypedHandler(Type handlerType, IntegrationEvent @event)
    {
        using var scope = serviceProvider.CreateScope();

        var handler = scope.ServiceProvider.GetService(handlerType);
        if (handler == null)
        {
            logger.WarningNoHandler(handlerType.Name);
            return;
        }

        var eventType = @event.GetType();
        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var method = concreteType.GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.Handle));

        await (Task)method!.Invoke(handler, [@event])!;
    }

    private async Task ProcessDynamicHandler(Type handlerType, IntegrationEvent @event)
    {
        using var scope = serviceProvider.CreateScope();

        if (scope.ServiceProvider.GetService(handlerType) is IDynamicIntegrationEventHandler handler)
        {
            await handler.Handle(@event);
        }
        else
        {
            logger.WarningNoDynamicHandler(handlerType.Name);
        }
    }
}
