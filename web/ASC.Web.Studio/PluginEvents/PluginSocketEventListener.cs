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

namespace ASC.Web.Studio.PluginEvents;

/// <summary>
/// Forwards the socket events integration plugins publish (<see cref="PluginSocketEvent"/>, JSON) to the socket service.
/// </summary>
/// <remarks>
/// Plugins get their own exchange so that they never need access to the internal event bus: give a plugin's
/// RabbitMQ user write permission on <c>asc_plugin_events</c> only (no configure, no read). The sender is the
/// AMQP <c>user_id</c> property, which the broker checks against the user of the connection, and it may only
/// address the portals listed for it in <c>plugins:socketEvents:senders:{user}</c> - <c>*</c> or comma-separated
/// tenant ids. Everything else is rejected, so an unconfigured sender reaches no portal at all.
/// </remarks>
public partial class PluginSocketEventListener(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<PluginSocketEventListener> logger) : BackgroundService
{
    public const string ExchangeName = "asc_plugin_events";
    public const string RoutingKey = "socket";
    private const string QueueName = "asc_plugin_events_socket";
    private const string SendersSection = "plugins:socketEvents:senders";
    private const ushort PrefetchCount = 10;

    private static readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(15);
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    private IChannel _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // the plugin exchange lives in RabbitMQ only; with another bus there is nothing to listen to
        var persistentConnection = serviceProvider.GetService<IRabbitMQPersistentConnection>();
        if (persistentConnection is null)
        {
            logger.InformationDisabled();
            return;
        }

        using var timer = new PeriodicTimer(_checkInterval);

        try
        {
            do
            {
                try
                {
                    await EnsureConsumingAsync(persistentConnection);
                }
                catch (Exception ex)
                {
                    logger.ErrorStartConsuming(ex);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel != null)
        {
            await _channel.DisposeAsync();
        }
    }

    private async Task EnsureConsumingAsync(IRabbitMQPersistentConnection persistentConnection)
    {
        if (_channel is { IsOpen: true })
        {
            return;
        }

        var connection = await persistentConnection.GetConnection();

        // while the connection recovers, the client restores the channel and its consumer on its own;
        // only a channel closed by the broker (say, the queue was deleted) has to be opened again
        if (_channel != null && connection is not { IsOpen: true })
        {
            return;
        }

        if (_channel != null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        var channel = await persistentConnection.CreateModelAsync();

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false);
        await channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Direct, durable: true);
        await channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) => HandleAsync(channel, args);

        await channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);

        _channel = channel;
        logger.InformationConsuming(ExchangeName, QueueName);
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs args)
    {
        var sender = args.BasicProperties.UserId;
        var messageId = args.BasicProperties.MessageId;

        try
        {
            var rejection = await ForwardAsync(sender, args.Body);
            if (rejection is null)
            {
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false);
                return;
            }

            logger.WarningRejected(messageId, sender, rejection);
        }
        catch (Exception ex)
        {
            logger.ErrorForwarding(messageId, sender, ex);
        }

        // a plugin event is a notification: a bad or failed one is dropped, never redelivered in a loop
        await channel.BasicRejectAsync(args.DeliveryTag, requeue: false);
    }

    /// <returns>Why the event was refused, or null once it is handed to the socket service.</returns>
    private async Task<string> ForwardAsync(string sender, ReadOnlyMemory<byte> body)
    {
        if (string.IsNullOrEmpty(sender))
        {
            return "the message has no user_id property";
        }

        PluginSocketEvent @event;
        try
        {
            @event = JsonSerializer.Deserialize<PluginSocketEvent>(body.Span, _options);
        }
        catch (JsonException)
        {
            @event = null;
        }

        if (@event is null)
        {
            return "the body is not a PluginSocketEvent JSON object";
        }

        if (!NameRegex().IsMatch(@event.Room ?? string.Empty) || !NameRegex().IsMatch(@event.Event ?? string.Empty))
        {
            return "room and event must be 1-128 letters, digits or the characters _ . : -";
        }

        if (!IsAllowed(sender, @event.TenantId))
        {
            return $"the sender may not address tenant {@event.TenantId}";
        }

        CustomSynchronizationContext.CreateContext();

        await using var scope = serviceProvider.CreateAsyncScope();

        var tenant = await scope.ServiceProvider.GetRequiredService<TenantManager>().GetTenantAsync(@event.TenantId);
        if (tenant is not { Status: TenantStatus.Active })
        {
            return $"tenant {@event.TenantId} does not exist or is not active";
        }

        await scope.ServiceProvider.GetRequiredService<PluginSocketManager>().SendAsync(@event);

        return null;
    }

    private bool IsAllowed(string sender, int tenantId)
    {
        // a user name with a colon would address a nested configuration key: such a sender is never configured
        if (tenantId <= 0 || sender.Contains(':'))
        {
            return false;
        }

        var allowed = configuration[$"{SendersSection}:{sender}"];
        if (string.IsNullOrWhiteSpace(allowed))
        {
            return false;
        }

        if (allowed.Trim() == "*")
        {
            return true;
        }

        return allowed
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(id => int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var allowedId) && allowedId == tenantId);
    }

    [GeneratedRegex("^[A-Za-z0-9_.:-]{1,128}$")]
    private static partial Regex NameRegex();
}
