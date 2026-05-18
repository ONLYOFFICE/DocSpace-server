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

using Apache.NMS.AMQP;

namespace ASC.EventBus.ActiveMQ;

public class DefaultActiveMQPersistentConnection(IConnectionFactory connectionFactory,
        ILogger<DefaultActiveMQPersistentConnection> logger, int retryCount = 5)
    : IActiveMQPersistentConnection
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly ILogger<DefaultActiveMQPersistentConnection> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private IConnection _connection;
    private bool _disposed;

    public bool IsConnected => _connection is { IsStarted: true } && !_disposed;

    public async Task<ISession> CreateSessionAsync()
    {
        return await CreateSessionAsync(AcknowledgementMode.AutoAcknowledge);
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
            //_connection.ExceptionListener -= OnExceptionListener;
            //_connection.ConnectionInterruptedListener -= OnConnectionInterruptedListener;
            //_connection.ConnectionResumedListener -= OnConnectionResumedListener;

            _connection.Dispose();
        }
        catch (IOException ex)
        {
            _logger.CriticalDefaultActiveMQPersistentConnection(ex);
        }
    }

    private async Task OnExceptionListenerAsync(Exception exception)
    {
        if (_disposed)
        {
            return;
        }

        _logger.WarningActiveMQConnectionThrowException();

        await TryConnectAsync();
    }

    private async Task OnConnectionResumedListenerAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.WarningActiveMQConnectionThrowException();

        await TryConnectAsync();
    }

    private async Task OnConnectionInterruptedListenerAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.WarningActiveMQConnectionThrowException();

        await TryConnectAsync();
    }

    public async Task<bool> TryConnectAsync()
    {
        _logger.InformationActiveMQTryingConnect();

        var builder = new ResiliencePipelineBuilder();

        var pipeline = builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = retryCount,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<SocketException>(),
            OnRetry = args =>
            {
                _logger.WarningActiveMQCouldNotConnect(args.Duration.TotalSeconds, args.Outcome.Exception);
                return ValueTask.CompletedTask;
            }
        }).Build();

        await pipeline.ExecuteAsync(async _ =>
        {
            _connection = await _connectionFactory.CreateConnectionAsync();
            await _connection.StartAsync();
        });

        if (IsConnected)
        {
            _connection.ExceptionListener += async e => { await OnExceptionListenerAsync(e); };
            _connection.ConnectionInterruptedListener += async () => { await OnConnectionInterruptedListenerAsync(); };
            _connection.ConnectionResumedListener += async () => { await OnConnectionResumedListenerAsync(); };

            if (_connection is NmsConnection connection)
            {
                var hostname = connection.ConnectionInfo.ConfiguredUri.Host;

                _logger.InformationActiveMQAcquiredPersistentConnection(hostname);

            }


            return true;
        }

        _logger.CriticalActiveMQCouldNotBeCreated();

        return false;
    }

    public async Task<ISession> CreateSessionAsync(AcknowledgementMode acknowledgementMode)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No ActiveMQ connections are available to perform this action");
        }

        return await _connection.CreateSessionAsync(acknowledgementMode);
    }
}