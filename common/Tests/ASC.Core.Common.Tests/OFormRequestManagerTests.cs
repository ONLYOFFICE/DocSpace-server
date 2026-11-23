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
/// How a template is downloaded from the template gallery when a file is created from it, and how the portal checks
/// whether the gallery answers (bug 78009: in a closed network the editor page waited for the gallery and opened blank).
/// </summary>
/// <remarks>
/// The gallery is a WireMock stub that answers its list and its files under the stub's own path, so the request manager
/// runs with its real HTTP client, its parsing and its error mapping, over a real FusionCache in memory with the default
/// entry options the services register (common/ASC.Api.Core ServiceCollectionExtension: a 5-second lock timeout and
/// factory hard timeout), without the distributed level and the backplane of the services; the test of two nodes
/// shares a distributed cache in memory between two of them. A gallery that does not
/// answer in time is a stub that answers later than the timeout given to the client of the test, so the manager's own
/// 30-second and 4-second deadlines are not what these tests reach.
/// </remarks>
[Trait("Bug", "78009")]
public sealed class OFormRequestManagerTests : IDisposable
{
    private const string Pdf = "%PDF-1.7\n%form\n";
    private const string List = """{"data":[{"id":1}]}""";
    private const string GalleryPath = "/dashboard/v2/api/oforms";

    private readonly List<ServiceProvider> _services = [];
    private readonly List<HttpStub> _stubs = [];
    private readonly List<TruncatingServer> _truncating = [];
    private readonly FusionCache _cache = new(new FusionCacheOptions
    {
        DefaultEntryOptions = new FusionCacheEntryOptions
        {
            Duration = TimeSpan.MaxValue,
            LockTimeout = TimeSpan.FromSeconds(5),
            FactoryHardTimeout = TimeSpan.FromSeconds(5)
        }
    });

    // The address of the file the list names; set when the gallery stub is made, read when the list is answered
    private string _fileUrl = "";

    public void Dispose()
    {
        foreach (var stub in _stubs)
        {
            stub.Dispose();
        }

        foreach (var server in _truncating)
        {
            server.Dispose();
        }

        foreach (var services in _services)
        {
            services.Dispose();
        }

        _cache.Dispose();
    }

    [Fact]
    public async Task Get_GalleryAnswersWithThePdf_ReturnsIt()
    {
        var gallery = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.OK, Pdf)
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));

        await using var stream = await CreateManager(gallery.Url).Get(1, ".pdf");

        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(Pdf);
    }

    [Fact]
    public async Task Get_GalleryHasNoSuchForm_Returns404()
    {
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, """{"data":[]}"""));

        await Assert.ThrowsAsync<ItemNotFoundException>(() => CreateManager(gallery.Url).Get(1, ".pdf"));
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Get_ListAnswersWithAnError_Returns502(HttpStatusCode status)
    {
        var gallery = Gallery(_ => StubResponse.Json(status, """{"error":{}}"""));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"data":null}""")]
    public async Task Get_ListAnswersWithoutItsForms_Returns502(string body)
    {
        // The probe reads the same answer as "unavailable": the download must not call it "no such form"
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, body));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url));
    }

    [Fact]
    public async Task Get_ListIsAPageOfAProxy_Returns502()
    {
        var gallery = Gallery(_ => new StubResponse(HttpStatusCode.OK, "<html>Access denied</html>"));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url));
    }

    [Fact]
    public async Task Get_FileIsAPageOfAProxy_Returns502()
    {
        // The list answers, the file host is cut off: a page saved as the form opened as a blank editor
        var gallery = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.OK, "<html>Access denied</html>")
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url));
    }

    // The gallery holds document, spreadsheet and presentation templates besides the PDF forms
    [Theory]
    [InlineData(".docx")]
    [InlineData(".xlsx")]
    [InlineData(".pptx")]
    public async Task Get_OfficeFileIsAPageOfAProxy_Returns502(string ext)
    {
        var gallery = Gallery(request => request.Path.EndsWith("/file" + ext, StringComparison.Ordinal)
            ? new StubResponse(HttpStatusCode.OK, "<html>Access denied</html>")
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile(ext)));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url), ext);
    }

    [Theory]
    [InlineData(".docx", "word/document.xml")]
    [InlineData(".xlsx", "xl/workbook.xml")]
    [InlineData(".pptx", "ppt/presentation.xml")]
    public async Task Get_GalleryAnswersWithTheOfficeFile_ReturnsIt(string ext, string part)
    {
        var file = "PK\u0003\u0004" + part;
        var gallery = Gallery(request => request.Path.EndsWith("/file" + ext, StringComparison.Ordinal)
            ? new StubResponse(HttpStatusCode.OK, file)
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile(ext)));

        await using var stream = await CreateManager(gallery.Url).Get(1, ext);

        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(file);
    }

    [Fact]
    public async Task Get_FileAnswersWithAnError_Returns502()
    {
        var gallery = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.Forbidden)
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url));
    }

    [Fact]
    public async Task Get_GalleryDoesNotAnswerInTime_Returns502()
    {
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, List).After(TimeSpan.FromSeconds(5)));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url, clientTimeout: TimeSpan.FromMilliseconds(500)));
    }

    [Theory]
    [InlineData("http://exa mple:x/file.pdf")]
    [InlineData("ftp://gallery.example/file.pdf")]
    public async Task Get_FileAddressCannotBeRequested_Returns502(string fileUrl)
    {
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK,
            $$"""{"data":[{"id":1,"file_oform":[{"url":"{{fileUrl}}","name":"form","ext":".pdf"}]}]}"""));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url));
    }

    [Fact]
    public async Task Get_GalleryIsUnreachable_Returns502()
    {
        await AssertGalleryUnavailableAsync(CreateManager(Unreachable().Url));
    }

    [Fact]
    public async Task Get_ListConnectionDropsMidBody_Returns502()
    {
        await AssertGalleryUnavailableAsync(CreateManager(Truncating().Url));
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".docx")]
    public async Task Get_FileConnectionDropsMidBody_Returns502(string ext)
    {
        var file = Truncating();
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK,
            $$"""{"data":[{"id":1,"file_oform":[{"url":"{{file.Url}}/file{{ext}}","name":"form","ext":"{{ext}}"}]}]}"""));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url), ext);

        file.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Get_NoGalleryConfigured_Returns404()
    {
        await Assert.ThrowsAsync<ItemNotFoundException>(() => CreateManager(null).Get(1, ".pdf"));
    }

    [Fact]
    public async Task IsAvailableAsync_GalleryAnswersWithItsList_IsTrueAndAskedOnce()
    {
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, List));
        var manager = CreateManager(gallery.Url);

        (await manager.IsAvailableAsync()).Should().BeTrue();
        (await manager.IsAvailableAsync()).Should().BeTrue();

        gallery.CallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, """{"error":{}}""")]
    [InlineData(HttpStatusCode.BadGateway, "")]
    [InlineData(HttpStatusCode.OK, "<html>Access denied</html>")]
    [InlineData(HttpStatusCode.OK, """{"error":{}}""")]
    public async Task IsAvailableAsync_GalleryAnswersWithoutItsList_IsFalse(HttpStatusCode status, string body)
    {
        var gallery = Gallery(_ => new StubResponse(status, body));

        (await CreateManager(gallery.Url).IsAvailableAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_GalleryDoesNotAnswerInTime_IsFalse()
    {
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, List).After(TimeSpan.FromSeconds(5)));

        (await CreateManager(gallery.Url, clientTimeout: TimeSpan.FromMilliseconds(500)).IsAvailableAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_GalleryIsUnreachable_IsFalse()
    {
        (await CreateManager(Unreachable().Url).IsAvailableAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_ConnectionDropsMidBody_IsFalse()
    {
        (await CreateManager(Truncating().Url).IsAvailableAsync()).Should().BeFalse();
    }

    [Theory]
    [InlineData("http://exa mple:x")]
    [InlineData("ftp://gallery.example")]
    public async Task IsAvailableAsync_GalleryAddressCannotBeRequested_IsFalse(string domain)
    {
        (await CreateManager(domain).IsAvailableAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_NoGalleryConfigured_IsFalse()
    {
        (await CreateManager(null).IsAvailableAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_SharedCacheChecksAChangedGalleryUrl()
    {
        var original = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, List));
        var replacement = Gallery(_ => new StubResponse(HttpStatusCode.BadGateway));

        (await CreateManager(original.Url).IsAvailableAsync()).Should().BeTrue();
        (await CreateManager(replacement.Url).IsAvailableAsync()).Should().BeFalse();

        original.CallCount.Should().Be(1);
        replacement.CallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway, "")]
    [InlineData(HttpStatusCode.Forbidden, "<html>Access denied</html>")]
    [InlineData(HttpStatusCode.OK, "<html>Maintenance</html>")]
    public async Task IsAvailableAsync_CheckFailsWithAvailableKnown_StaysTrue(HttpStatusCode status, string body)
    {
        // A failed check of any kind, the maintenance page of a CDN included, must not hide a gallery that answered:
        // no false "unavailable" in SaaS
        var failing = false;
        var gallery = Gallery(_ => failing ? new StubResponse(status, body) : StubResponse.Json(HttpStatusCode.OK, List));
        var manager = CreateManager(gallery.Url);

        (await manager.IsAvailableAsync()).Should().BeTrue();

        failing = true;
        var refreshFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _cache.Events.FactoryError += (_, _) => refreshFailed.TrySetResult();
        _cache.Events.BackgroundFactoryError += (_, _) => refreshFailed.TrySetResult();
        await _cache.ExpireAsync(AvailabilityCacheKey(gallery), token: TestContext.Current.CancellationToken);

        (await manager.IsAvailableAsync()).Should().BeTrue();
        await refreshFailed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        gallery.CallCount.Should().Be(2);
        (await manager.IsAvailableAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_GalleryAnswersWithUnavailableKnown_BecomesTrue()
    {
        var failing = true;
        var gallery = Gallery(_ => failing
            ? new StubResponse(HttpStatusCode.BadGateway)
            : StubResponse.Json(HttpStatusCode.OK, List));
        var manager = CreateManager(gallery.Url);

        (await manager.IsAvailableAsync()).Should().BeFalse();

        failing = false;
        await _cache.ExpireAsync(AvailabilityCacheKey(gallery), token: TestContext.Current.CancellationToken);

        // The check may outlast the 200 ms the reader waits for it: the answer then comes with a later call
        var deadline = DateTime.UtcNow.AddSeconds(10);
        bool available;
        do
        {
            available = await manager.IsAvailableAsync();
            if (!available)
            {
                await Task.Delay(100, TestContext.Current.CancellationToken);
            }
        }
        while (!available && DateTime.UtcNow < deadline);

        available.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_ExpiredAnswerAndSlowCheck_ReturnsTheAnswerWithinTheWait()
    {
        var slow = false;
        var gallery = Gallery(_ => slow
            ? StubResponse.Json(HttpStatusCode.OK, List).After(TimeSpan.FromSeconds(3))
            : StubResponse.Json(HttpStatusCode.OK, List));
        var manager = CreateManager(gallery.Url);

        (await manager.IsAvailableAsync()).Should().BeTrue();

        slow = true;
        await _cache.ExpireAsync(AvailabilityCacheKey(gallery), token: TestContext.Current.CancellationToken);

        var watch = Stopwatch.StartNew();
        (await manager.IsAvailableAsync()).Should().BeTrue();
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task IsAvailableAsync_FailedChecksWithNothingKnown_StayOnTheNodeThatChecked()
    {
        // In SaaS one process that cannot reach the gallery must not show it as unavailable on every node, also after
        // its second failed check: the other node, sharing the distributed cache, asks the gallery itself
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        using var first = CreateNodeCache(distributed);
        using var second = CreateNodeCache(distributed);
        var failing = true;
        var gallery = Gallery(_ => failing
            ? new StubResponse(HttpStatusCode.BadGateway)
            : StubResponse.Json(HttpStatusCode.OK, List));
        var firstManager = CreateManager(gallery.Url, cache: first);

        (await firstManager.IsAvailableAsync()).Should().BeFalse();

        var key = AvailabilityCacheKey(gallery);
        await first.ExpireAsync(key, token: TestContext.Current.CancellationToken);
        (await firstManager.IsAvailableAsync()).Should().BeFalse();
        await WaitForCachedAnswerAsync(first, key, expected: false);
        gallery.CallCount.Should().Be(2);

        failing = false;
        (await CreateManager(gallery.Url, cache: second).IsAvailableAsync()).Should().BeTrue();
        gallery.CallCount.Should().Be(3);
    }

    private static string AvailabilityCacheKey(HttpStub gallery)
    {
        return OFormRequestManager.GetAvailabilityCacheKey(gallery.Url + GalleryPath);
    }

    private static async Task WaitForCachedAnswerAsync(IFusionCache cache, string key, bool expected)
    {
        var token = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow.AddSeconds(10);

        do
        {
            var answer = await cache.TryGetAsync<bool>(key, options => options.AllowStaleOnReadOnly = false, token);
            if (answer.HasValue && answer.Value == expected)
            {
                return;
            }

            await Task.Delay(100, token);
        }
        while (DateTime.UtcNow < deadline);

        throw new TimeoutException($"The gallery availability cache did not store {expected}.");
    }

    private static FusionCache CreateNodeCache(IDistributedCache distributed)
    {
        var cache = new FusionCache(new FusionCacheOptions
        {
            DistributedCacheKeyModifierMode = CacheKeyModifierMode.None,
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.MaxValue,
                LockTimeout = TimeSpan.FromSeconds(5),
                FactoryHardTimeout = TimeSpan.FromSeconds(5)
            }
        });
        cache.SetupDistributedCache(distributed, new FusionCacheSystemTextJsonSerializer());

        return cache;
    }

    private HttpStub Gallery(Func<StubRequest, StubResponse> respond)
    {
        var stub = new HttpStub(respond);
        _stubs.Add(stub);
        _fileUrl = stub.Url + "/uploads/file";

        return stub;
    }

    private TruncatingServer Truncating()
    {
        var server = new TruncatingServer();
        _truncating.Add(server);

        return server;
    }

    private HttpStub Unreachable()
    {
        var stub = HttpStub.Unreachable();
        _stubs.Add(stub);

        return stub;
    }

    /// <param name="domain">The address of the gallery; <c>null</c> configures none.</param>
    /// <param name="clientTimeout">The timeout of the HTTP client, shorter than the manager's own to test it quickly.</param>
    private OFormRequestManager CreateManager(string? domain, TimeSpan? clientTimeout = null, IFusionCache? cache = null)
    {
        var services = new ServiceCollection();
        services.AddHttpClient(Options.DefaultName, client =>
        {
            if (clientTimeout is { } timeout)
            {
                client.Timeout = timeout;
            }
        });

        var provider = services.BuildServiceProvider();
        _services.Add(provider);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["files:oform:domain"] = domain,
                ["files:oform:path"] = domain is null ? null : GalleryPath + "/"
            })
            .Build();

        return new OFormRequestManager(NullLogger<OFormRequestManager>.Instance, configuration, provider.GetRequiredService<IHttpClientFactory>(), cache ?? _cache);
    }

    private static bool IsFile(StubRequest request)
    {
        return request.Path.EndsWith("/file.pdf", StringComparison.Ordinal);
    }

    private string ListWithFile(string ext = ".pdf")
    {
        return $$"""{"data":[{"id":1,"file_oform":[{"url":"{{_fileUrl}}{{ext}}","name":"form","ext":"{{ext}}"}]}]}""";
    }

    private static async Task AssertGalleryUnavailableAsync(OFormRequestManager manager, string ext = ".pdf")
    {
        var exception = await Assert.ThrowsAsync<CustomHttpException>(() => manager.Get(1, ext));

        exception.StatusCode.Should().Be((int)HttpStatusCode.BadGateway);
    }
    /// <summary>
    /// A server that answers 200 with a Content-Length longer than the body it writes, then closes the connection: the
    /// gallery, or a proxy of a closed network, dropping the connection while the body is read. WireMock cannot do
    /// that, its faults either answer in full or fail before the headers.
    /// </summary>
    private sealed class TruncatingServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private int _callCount;

        public TruncatingServer()
        {
            _listener.Start();
            _ = Task.Run(AcceptAsync);
        }

        public string Url => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";

        public int CallCount => Volatile.Read(ref _callCount);

        public void Dispose()
        {
            _listener.Stop();
        }

        private async Task AcceptAsync()
        {
            while (true)
            {
                Socket connection;

                try
                {
                    connection = await _listener.AcceptSocketAsync();
                }
                catch (Exception e) when (e is SocketException or ObjectDisposedException)
                {
                    // The listener is stopped at the end of the test
                    return;
                }

                _ = Task.Run(() => AnswerAsync(connection));
            }
        }

        private async Task AnswerAsync(Socket connection)
        {
            using (connection)
            {
                try
                {
                    var request = new StringBuilder();
                    var buffer = new byte[4096];

                    while (!request.ToString().Contains("\r\n\r\n", StringComparison.Ordinal))
                    {
                        var read = await connection.ReceiveAsync(buffer);
                        if (read == 0)
                        {
                            return;
                        }

                        request.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    }

                    Interlocked.Increment(ref _callCount);

                    await connection.SendAsync(Encoding.ASCII.GetBytes(
                        "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: 4096\r\n\r\n{\"data\":[{\"id\":1"));
                    connection.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                    // The client gave up first; the connection is gone either way
                }
            }
        }
    }
}
