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

namespace ASC.Tests.Common.ApiFactories;

/// <summary>An event the socket service delivered to a <see cref="PortalSocket"/>.</summary>
/// <param name="Name">The event name, such as <c>s:modify-folder</c>.</param>
/// <param name="Payload">The first argument of the event; undefined when it came without one.</param>
public sealed record SocketEvent(string Name, JsonElement Payload);

/// <summary>
/// A real Socket.IO client connected to the running socket service the way the browser is: it authenticates with
/// the <c>asc_auth_key</c> cookie, joins rooms through <c>subscribe</c> and records what it receives. A test therefore
/// covers the whole way of an event: the API call, the C# notifier, the route of the Node service, the room and the
/// delivery.
/// </summary>
/// <remarks>
/// It connects past the router and sends the two headers the router would add: the service builds the portal address
/// it checks the cookie against from them (<c>portalManager.js</c>). The service answers a <c>subscribe</c> with
/// nothing to wait for, but that frame is on the wire before the test starts the request that causes the event, and
/// that request has to pass the API, the database and the background sender to come back to the same Node process.
/// </remarks>
public sealed class PortalSocket : IAsyncDisposable
{
    private const string ConnectionInitEvent = "connection-init";

    private static readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(30);

    // A deletion runs as a queued operation: the event can come long after the request was answered.
    private static readonly TimeSpan _defaultEventTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(50);

    private readonly SocketIO _socket;
    private readonly Lock _eventsLock = new();
    private readonly List<SocketEvent> _events = [];

    private PortalSocket(string portalName, string? token)
    {
        var headers = new Dictionary<string, string>
        {
            ["X-Forwarded-Proto"] = Uri.UriSchemeHttp,
            ["X-Forwarded-Host"] = portalName
        };

        if (token is not null)
        {
            headers["Cookie"] = $"asc_auth_key={token}";
        }

        _socket = new SocketIO(
            new UriBuilder(Uri.UriSchemeHttp, IPAddress.Loopback.ToString(), AppHostConstants.SocketIoPort).Uri,
            new SocketIOOptions
            {
                Transport = TransportProtocol.WebSocket,
                AutoUpgrade = false,
                Reconnection = false,
                ConnectionTimeout = _connectionTimeout,
                ExtraHeaders = headers
            });

        _socket.OnAny(Record);
    }

    /// <summary>
    /// Connects to the socket service and waits until it has finished setting the connection up.
    /// </summary>
    /// <param name="portalName">The portal the connection belongs to.</param>
    /// <param name="token">The value of the auth cookie; null connects without one.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    public static async Task<PortalSocket> ConnectAsync(string portalName, string? token, CancellationToken cancellationToken)
    {
        var socket = new PortalSocket(portalName, token);

        try
        {
            await socket._socket.ConnectAsync(cancellationToken);
            await socket.WaitForAsync(ConnectionInitEvent, _ => true, _connectionTimeout, cancellationToken);

            return socket;
        }
        catch
        {
            await socket.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Joins rooms of the portal by the part after the tenant id (<c>DIR-12</c>, <c>users</c>), as the client does.
    /// </summary>
    /// <param name="individual">Also joins <c>{room}-{userId}</c>: an event with recipients is sent there.</param>
    /// <param name="cancellationToken">Cancels the send.</param>
    /// <param name="roomParts">The room parts to join.</param>
    public Task SubscribeAsync(bool individual, CancellationToken cancellationToken, params string[] roomParts)
    {
        return _socket.EmitAsync("subscribe", [new { roomParts, individual }], cancellationToken);
    }

    /// <summary>Waits for an event that matches, whether it has already arrived or not.</summary>
    /// <exception cref="TimeoutException">Nothing matched in time; the message lists what did arrive.</exception>
    public async Task<SocketEvent> WaitForAsync(
        string name,
        Func<JsonElement, bool> match,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? _defaultEventTimeout);

        while (true)
        {
            var found = Find(name, match);

            if (found is not null)
            {
                return found;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"No matching '{name}' event arrived in time. Received: [{string.Join(", ", Received())}]");
            }

            await Task.Delay(_pollInterval, cancellationToken);
        }
    }

    /// <summary>
    /// Waits until at least <paramref name="count"/> events that match have arrived, counting the ones already there,
    /// and returns the last of them. For an entry that was announced before the action under test: the action has to
    /// add an event of its own, an earlier one in flight cannot stand in for it.
    /// </summary>
    /// <exception cref="TimeoutException">Too few matched in time; the message lists what did arrive.</exception>
    public async Task<SocketEvent> WaitForCountAsync(
        string name,
        Func<JsonElement, bool> match,
        int count,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? _defaultEventTimeout);

        while (true)
        {
            var found = FindAll(name, match);

            if (found.Count >= count)
            {
                return found[^1];
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(
                    $"{found.Count} of {count} matching '{name}' events arrived in time. Received: [{string.Join(", ", Received())}]");
            }

            await Task.Delay(_pollInterval, cancellationToken);
        }
    }

    /// <summary>
    /// Whether an event that matches arrives within the window. For a test that has already seen the event reach
    /// someone else, so the window only has to cover the difference between two deliveries.
    /// </summary>
    public async Task<bool> ArrivesWithinAsync(
        string name,
        Func<JsonElement, bool> match,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.Add(window);

        while (DateTime.UtcNow < deadline)
        {
            if (Find(name, match) is not null)
            {
                return true;
            }

            await Task.Delay(_pollInterval, cancellationToken);
        }

        return Find(name, match) is not null;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_socket.Connected)
            {
                await _socket.DisconnectAsync();
            }
        }
        finally
        {
            _socket.Dispose();
        }
    }

    private SocketEvent? Find(string name, Func<JsonElement, bool> match)
    {
        lock (_eventsLock)
        {
            return _events.Find(e => e.Name == name && match(e.Payload));
        }
    }

    private List<SocketEvent> FindAll(string name, Func<JsonElement, bool> match)
    {
        lock (_eventsLock)
        {
            return _events.FindAll(e => e.Name == name && match(e.Payload));
        }
    }

    private List<string> Received()
    {
        lock (_eventsLock)
        {
            return _events.ConvertAll(e => $"{e.Name} {e.Payload}");
        }
    }

    private Task Record(string name, IEventContext context)
    {
        // RawText is the whole packet: the event name, then its arguments
        using var packet = JsonDocument.Parse(context.RawText);

        var payload = packet.RootElement.GetArrayLength() > 1 ? packet.RootElement[1].Clone() : default;

        lock (_eventsLock)
        {
            _events.Add(new SocketEvent(name, payload));
        }

        return Task.CompletedTask;
    }
}
