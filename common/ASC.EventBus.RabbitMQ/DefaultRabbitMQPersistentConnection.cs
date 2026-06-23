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

namespace ASC.EventBus.RabbitMQ;

public class DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory,
        ILogger<DefaultRabbitMQPersistentConnection> logger, int retryCount = 5)
    : IRabbitMQPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private IConnection _connection;
    private bool _disposed;

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public async Task<IChannel> CreateModelAsync()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return await _connection.CreateChannelAsync();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
            _connection.CallbackExceptionAsync -= OnCallbackExceptionAsync;
            _connection.ConnectionBlockedAsync -= OnConnectionBlockedAsync;

            _connection.Dispose();
        }
        catch (IOException ex)
        {
            _logger.CriticalDefaultRabbitMQPersistentConnection(ex);
        }
    }

    public async Task<bool> TryConnectAsync()
    {
        _logger.InformationRabbitMQTryingConnect();

        if (_connection != null)
        {
            while (!IsConnected) // waiting automatic recovery connection
            {
                await Task.Delay(1000);
            }

            _logger.InformationRabbitMQAcquiredPersistentConnection(_connection.Endpoint.HostName);

            return true;
        }

        var builder = new ResiliencePipelineBuilder();

        var pipeline = builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = retryCount,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>().Handle<SocketException>(),
            OnRetry = args =>
            {
                _logger.WarningRabbitMQCouldNotConnect(args.Duration.TotalSeconds, args.Outcome.Exception);
                return ValueTask.CompletedTask;
            }
        }).Build();

        await pipeline.ExecuteAsync(async _ =>
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
        });

        if (IsConnected)
        {
            _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
            _connection.CallbackExceptionAsync += OnCallbackExceptionAsync;
            _connection.ConnectionBlockedAsync += OnConnectionBlockedAsync;

            _logger.InformationRabbitMQAcquiredPersistentConnection(_connection.Endpoint.HostName);

            return true;
        }

        _logger.CriticalRabbitMQCouldNotBeCreated();

        return false;

    }

    private async Task OnConnectionBlockedAsync(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        _logger.WarningRabbitMQConnectionShutdown();

        await TryConnectAsync();
    }


    private async Task OnCallbackExceptionAsync(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        _logger.WarningRabbitMQConnectionThrowException();

        await TryConnectAsync();
    }
    private async Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs reason)
    {
        if (_disposed)
        {
            return;
        }

        _logger.WarningRabbitMQConnectionIsOnShutDown();

        await TryConnectAsync();
    }

    public async Task<IConnection> GetConnection()
    {
        await TryConnectAsync();

        return _connection;
    }
}