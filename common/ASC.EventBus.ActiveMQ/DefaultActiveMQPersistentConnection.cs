// (c) Copyright Ascensio System SIA 2009-2024
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

    public bool IsConnected
    {
        get
        {
            return _connection is { IsStarted: true } && !_disposed;
        }
    }

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

        var policy = Policy.Handle<SocketException>()
            .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.WarningActiveMQCouldNotConnect(time.TotalSeconds, ex);
            }
        );

        await policy.ExecuteAsync(async () =>
        {
            _connection = await _connectionFactory
                    .CreateConnectionAsync();

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
