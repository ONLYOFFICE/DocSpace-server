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
/// How a form is downloaded from the form gallery when a file is created from it, and what the portal settings report
/// about the gallery (bug 78009: in a closed network the editor page waited for the gallery and opened blank).
/// </summary>
/// <remarks>
/// The gallery is a WireMock stub that answers its list and its files under the stub's own path, so the request manager
/// runs with its real HTTP client, its parsing and its error mapping, over a real FusionCache in memory. A gallery that
/// does not answer in time is a stub that answers later than the timeout given to the client of the test.
/// </remarks>
[Trait("Bug", "78009")]
public sealed class OFormRequestManagerTests : IDisposable
{
    private const string Pdf = "%PDF-1.7\n%form\n";
    private const string List = """{"data":[{"id":1}]}""";

    private readonly List<ServiceProvider> _services = [];
    private readonly List<HttpStub> _stubs = [];
    private readonly FusionCache _cache = new(new FusionCacheOptions());

    // The address of the file the list names; set when the gallery stub is made, read when the list is answered
    private string _fileUrl = "";

    public void Dispose()
    {
        foreach (var stub in _stubs)
        {
            stub.Dispose();
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

    [Fact]
    public async Task Get_GalleryIsUnreachable_Returns502()
    {
        await AssertGalleryUnavailableAsync(CreateManager(Unreachable().Url));
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
    public async Task IsAvailableAsync_NoGalleryConfigured_IsFalse()
    {
        (await CreateManager(null).IsAvailableAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_FirstCheckIsSlow_IsTrueWithinSecondsThenTheAnswer()
    {
        // The settings request must not wait for a gallery that does not answer: with no answer known it gets true
        // after the 2-second hard timeout, and the check stores its answer when it ends
        var gallery = Gallery(_ => new StubResponse(HttpStatusCode.BadGateway).After(TimeSpan.FromSeconds(4)));
        var manager = CreateManager(gallery.Url);

        var watch = Stopwatch.StartNew();
        (await manager.IsAvailableAsync()).Should().BeTrue();
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));

        var deadline = DateTime.UtcNow.AddSeconds(15);
        bool available;
        do
        {
            await Task.Delay(500, TestContext.Current.CancellationToken);
            available = await manager.IsAvailableAsync();
        }
        while (available && DateTime.UtcNow < deadline);

        available.Should().BeFalse();
    }

    private HttpStub Gallery(Func<StubRequest, StubResponse> respond)
    {
        var stub = new HttpStub(respond);
        _stubs.Add(stub);
        _fileUrl = stub.Url + "/uploads/file.pdf";

        return stub;
    }

    private HttpStub Unreachable()
    {
        var stub = HttpStub.Unreachable();
        _stubs.Add(stub);

        return stub;
    }

    /// <param name="domain">The address of the gallery; <c>null</c> configures none.</param>
    /// <param name="clientTimeout">The timeout of the HTTP client, shorter than the manager's own to test it quickly.</param>
    private OFormRequestManager CreateManager(string? domain, TimeSpan? clientTimeout = null)
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
                ["files:oform:path"] = domain is null ? null : "/dashboard/v2/api/oforms/"
            })
            .Build();

        return new OFormRequestManager(NullLogger<OFormRequestManager>.Instance, configuration, provider.GetRequiredService<IHttpClientFactory>(), _cache);
    }

    private static bool IsFile(StubRequest request)
    {
        return request.Path.EndsWith("/file.pdf", StringComparison.Ordinal);
    }

    private string ListWithFile()
    {
        return $$"""{"data":[{"id":1,"file_oform":[{"url":"{{_fileUrl}}","name":"form","ext":".pdf"}]}]}""";
    }

    private static async Task AssertGalleryUnavailableAsync(OFormRequestManager manager)
    {
        var exception = await Assert.ThrowsAsync<CustomHttpException>(() => manager.Get(1, ".pdf"));

        exception.StatusCode.Should().Be((int)HttpStatusCode.BadGateway);
    }
}
