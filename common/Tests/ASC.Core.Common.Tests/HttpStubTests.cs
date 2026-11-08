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


namespace ASC.Core.Common.Tests;

/// <summary>
/// What <see cref="HttpStub"/> promises about its servers: they answer on the loopback only, and they keep a
/// connection for the next request, so nothing in a test depends on a connection being closed after an answer.
/// </summary>
public class HttpStubTests
{
    [Fact]
    public async Task SharedServer_AnswersOnTheLoopbackOnly()
    {
        var marker = $$"""{"stub":"{{Guid.NewGuid():N}}"}""";
        using var stub = new HttpStub(_ => Reply(HttpStatusCode.OK, marker));

        await AssertLoopbackOnlyAsync(stub.Url, marker);
    }

    [Fact]
    public async Task ServerAtRoot_AnswersOnTheLoopbackOnly()
    {
        var marker = $$"""{"stub":"{{Guid.NewGuid():N}}"}""";
        using var stub = HttpStub.AtRoot(_ => Reply(HttpStatusCode.OK, marker));

        await AssertLoopbackOnlyAsync(stub.Url, marker);
    }

    [Fact]
    public async Task Requests_OneAfterAnother_ShareOneConnection()
    {
        // Arrange
        using var stub = new HttpStub(_ => Reply(HttpStatusCode.OK, "{}"));
        var connections = 0;

        using var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                Interlocked.Increment(ref connections);

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                try
                {
                    await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);

                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        using var client = new HttpClient(handler);

        // Act
        using (await client.GetAsync(stub.Url + "/first", TestContext.Current.CancellationToken))
        {
        }

        using (await client.GetAsync(stub.Url + "/second", TestContext.Current.CancellationToken))
        {
        }

        // Assert
        stub.CallCount.Should().Be(2);
        connections.Should().Be(1, "the server keeps the connection open, and the client sends the second request over it");
    }

    // The stub answers over the loopback, and not over an address of the machine on a network. A server started without
    // an address listens on every interface and answers on both. "Answers" means the stub's own reply comes back: an
    // interface that drops the attempt, or a proxy in front of the network that accepts the connection and then finds
    // nothing behind it, does not count as the stub being reachable.
    private static async Task AssertLoopbackOnlyAsync(string url, string marker)
    {
        var stubUri = new Uri(url);

        // One client for the test; each attempt bounds its own wait.
        using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

        (await AnswersAsync(client, stubUri, IPAddress.Loopback, marker, TimeSpan.FromSeconds(10))).Should()
            .BeTrue("the client under test reaches the stub over the loopback");

        // The addresses of the interfaces themselves: a lookup of the host name fails on a host whose name resolves to
        // nothing, before the test could skip.
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(networkInterface => networkInterface.OperationalStatus == OperationalStatus.Up)
            .SelectMany(networkInterface => networkInterface.GetIPProperties().UnicastAddresses)
            .Select(unicast => unicast.Address)
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
            .ToList();

        if (addresses.Count == 0)
        {
            Assert.Skip("The machine has no address on a network to try the stub on.");
        }

        var attempts = await Task.WhenAll(addresses.Select(async address =>
            (Address: address, Answered: await AnswersAsync(client, stubUri, address, marker, TimeSpan.FromSeconds(2)))));

        attempts.Where(attempt => attempt.Answered).Select(attempt => attempt.Address).Should()
            .BeEmpty("the stub must not be reachable from the network");
    }

    // A slow answer over the loopback is still an answer, so that wait is generous; over the network a timeout can only
    // make the stub look unreachable, so a short one does not make the test fail by itself.
    private static async Task<bool> AnswersAsync(HttpClient client, Uri stubUri, IPAddress address, string marker, TimeSpan timeout)
    {
        var target = new UriBuilder(stubUri) { Host = address.ToString(), Path = stubUri.AbsolutePath.TrimEnd('/') + "/probe" }.Uri;

        using var wait = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        wait.CancelAfter(timeout);

        try
        {
            return await client.GetStringAsync(target, wait.Token) == marker;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
