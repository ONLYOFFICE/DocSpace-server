﻿// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.EventBus.RabbitMQ;

public class DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory,
        ILogger<DefaultRabbitMQPersistentConnection> logger, int retryCount = 5)
    : IRabbitMQPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private IConnection _connection;
    private bool _disposed;
   
    public bool IsConnected
    {
        get
        {
            return _connection is { IsOpen: true } && !_disposed;
        }
    }

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

        var policy = Policy.Handle<SocketException>()
            .Or<BrokerUnreachableException>()
            .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.WarningRabbitMQCouldNotConnect(time.TotalSeconds, ex);
            }
        );

        await policy.ExecuteAsync(async () =>
        {
            _connection = await _connectionFactory
                    .CreateConnectionAsync();
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
