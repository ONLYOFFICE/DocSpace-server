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
/// shares a distributed cache in memory between two of them. The deadlines of the download and of the check come from
/// the pipelines the manager registers (AddTemplateGalleryPipelines), which the tests register with shorter values and
/// the HTTP client without its timeout: a test of a deadline fails when the manager stops using its pipeline. No test
/// waits for the 30-second download deadline. The 4-second deadline of the check is waited for once, by the test of a
/// host that registers no pipeline (IsAvailableAsync_DeadlineOfTheCheck_IsNotLoggedAsAnErrorByPolly): it pins only
/// that this deadline ends the check before the 5-second hard timeout of the cache, not its exact value.
/// </remarks>
[Trait("Bug", "78009")]
public sealed class OFormRequestManagerTests : IDisposable
{
    private const string Pdf = "%PDF-1.7\n%form\n";
    private const string List = """{"data":[{"id":1}]}""";
    private const string GalleryPath = "/dashboard/v2/api/oforms";

    private readonly List<ServiceProvider> _services = [];
    private readonly List<HttpStub> _stubs = [];
    private readonly List<RawServer> _rawServers = [];
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

        foreach (var server in _rawServers)
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
    public async Task Get_GalleryNamesTheFileByAPathOnItsHost_ReturnsIt()
    {
        // The local upload provider of Strapi keeps the files on the gallery's host and names them by a path from its
        // root: the stub answers at the root of a server of its own, so that path reaches it
        var gallery = HttpStub.AtRoot(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.OK, Pdf)
            : StubResponse.Json(HttpStatusCode.OK, """{"data":[{"id":1,"file_oform":[{"url":"/uploads/file.pdf","name":"form","ext":".pdf"}]}]}"""));
        _stubs.Add(gallery);

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

    [Fact]
    public async Task Get_ListDoesNotAnswerBeforeTheDownloadDeadline_Returns502()
    {
        // The client waits forever: only the download deadline ends the request, before the stub answers with a list
        // that would make the download succeed
        var gallery = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.OK, Pdf)
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile()).After(TimeSpan.FromSeconds(5)));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url, clientTimeout: Timeout.InfiniteTimeSpan,
            downloadTimeout: TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task Get_FileBodyStallsPastTheDownloadDeadline_Returns502()
    {
        // The headers of the file come at once and the body never ends: the client's own timeout does not cover the body
        // of a response read by its headers, so only the download deadline ends the copy
        var file = Truncating(stall: true);
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK,
            $$"""{"data":[{"id":1,"file_oform":[{"url":"{{file.Url}}/file.pdf","name":"form","ext":".pdf"}]}]}"""));
        var manager = CreateManager(gallery.Url, clientTimeout: Timeout.InfiniteTimeSpan, downloadTimeout: TimeSpan.FromMilliseconds(500));

        var exception = await Assert.ThrowsAsync<CustomHttpException>(
            () => manager.Get(1, ".pdf").WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        exception.StatusCode.Should().Be((int)HttpStatusCode.BadGateway);
        file.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_FileAnnouncedLargerThanTheLimit_Returns502WithoutReadingIt()
    {
        // The headers announce a file over the limit and the body never comes: the download refuses it by the headers
        // instead of waiting for the body until the deadline
        var file = RawFile(head: "", contentLength: OFormRequestManager.MaxTemplateSize + 1, padding: 0, stall: true);
        var manager = CreateManager(GalleryWithFileAt(file.Url), clientTimeout: Timeout.InfiniteTimeSpan,
            downloadTimeout: TimeSpan.FromSeconds(10));

        var watch = Stopwatch.StartNew();
        await AssertGalleryUnavailableAsync(manager);

        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        file.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_FileLargerThanTheLimitWithoutItsLength_Returns502()
    {
        // A PDF with no Content-Length that goes past the limit: the copy stops at the limit
        var file = RawFile(head: Pdf, contentLength: null, padding: OFormRequestManager.MaxTemplateSize, stall: false);

        await AssertGalleryUnavailableAsync(CreateManager(GalleryWithFileAt(file.Url), downloadTimeout: TimeSpan.FromSeconds(20)));

        file.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_FileIsAnEndlessPageOfAProxy_Returns502WithoutWaitingForItsEnd()
    {
        // The start of the file is not a PDF and the rest never comes: the signature is checked before the rest is read
        var file = RawFile(head: "<html>" + new string(' ', 2048), contentLength: null, padding: 0, stall: true);
        var manager = CreateManager(GalleryWithFileAt(file.Url), clientTimeout: Timeout.InfiniteTimeSpan,
            downloadTimeout: TimeSpan.FromSeconds(10));

        var watch = Stopwatch.StartNew();
        await AssertGalleryUnavailableAsync(manager);

        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Get_TemplateHasNoFileOfTheFormat_Returns404()
    {
        // The gallery filters the files of the template by the extension, so a template without that format comes
        // with no files
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, """{"data":[{"id":1,"file_oform":[]}]}"""));

        await Assert.ThrowsAsync<ItemNotFoundException>(() => CreateManager(gallery.Url).Get(1, ".pdf"));
    }

    [Fact]
    public async Task Get_ExtensionWithQueryCharacters_ReachesTheGalleryAsOneValue()
    {
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, """{"data":[{"id":1,"file_oform":[]}]}"""));

        await Assert.ThrowsAsync<ItemNotFoundException>(() => CreateManager(gallery.Url).Get(1, ".a&b=c"));

        var query = QueryHelpers.ParseQuery(gallery.LastUri!.Query);
        query["populate[file_oform][filters][url][$endsWith]"].ToString().Should().Be(".a&b=c");
        query.Should().NotContainKey("b");
    }

    [Fact]
    public async Task Get_PdfHeaderAfterOtherBytes_ReturnsIt()
    {
        // Readers look for the header in the first 1024 bytes, not only at the start
        var pdf = new string(' ', 1000) + Pdf;
        var gallery = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.OK, pdf)
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));

        await using var stream = await CreateManager(gallery.Url).Get(1, ".pdf");

        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(pdf);
    }

    [Fact]
    public async Task Get_PdfHeaderPastTheFirst1024Bytes_Returns502()
    {
        var gallery = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.OK, new string(' ', 1024) + Pdf)
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url));
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

    [Theory]
    [InlineData("http://exa mple:x")]
    [InlineData("ftp://gallery.example")]
    public async Task Get_GalleryAddressCannotBeRequested_Returns502(string domain)
    {
        await AssertGalleryUnavailableAsync(CreateManager(domain));
    }

    [Fact]
    public async Task Get_ListLargerThanItsLimitWithoutItsLength_Returns502()
    {
        // An answer that goes on past the limit of the list is refused although it is a valid list naming a valid file:
        // the read stops at that limit instead of holding the answer in memory until the deadline
        var files = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.OK, Pdf)
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));
        var gallery = RawFile(head: ListWithFile(), contentLength: null, padding: OFormRequestManager.MaxListSize, stall: false,
            paddingByte: (byte)' ');

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url));

        gallery.CallCount.Should().Be(1);
        files.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Get_ListAnnouncedLargerThanItsLimit_Returns502WithoutReadingIt()
    {
        var gallery = RawFile(head: "", contentLength: OFormRequestManager.MaxListSize + 1, padding: 0, stall: true);
        var manager = CreateManager(gallery.Url, clientTimeout: Timeout.InfiniteTimeSpan, downloadTimeout: TimeSpan.FromSeconds(10));

        var watch = Stopwatch.StartNew();
        await AssertGalleryUnavailableAsync(manager);

        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
    }

    [Theory]
    [InlineData(typeof(NotSupportedException))]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ObjectDisposedException))]
    public async Task Get_FailureOfThisCode_IsNotCalledAnUnavailableGallery(Type exceptionType)
    {
        // A serializer that cannot read a changed DTO, or a disposed handler, is a fault of the portal: it must reach
        // the error log, not answer every creation from a template with "the gallery is temporarily unavailable"
        var exception = (Exception)Activator.CreateInstance(exceptionType, "fault")!;
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, ListWithFile()));
        var manager = CreateManager(gallery.Url, primaryHandler: () => new ThrowingHandler(exception));

        var thrown = await Assert.ThrowsAnyAsync<Exception>(() => manager.Get(1, ".pdf"));

        thrown.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task Get_GalleryAnswers5xx_IsAskedAgainBeforeA502()
    {
        var gallery = Gallery(_ => new StubResponse(HttpStatusCode.ServiceUnavailable));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url));

        gallery.CallCount.Should().Be(3);
    }

    [Fact]
    public async Task Get_GalleryAnswers5xxOnce_ReturnsTheFile()
    {
        var failedOnce = 0;
        var gallery = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.OK, Pdf)
            : Interlocked.Exchange(ref failedOnce, 1) == 0
                ? new StubResponse(HttpStatusCode.BadGateway)
                : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));

        await using var stream = await CreateManager(gallery.Url).Get(1, ".pdf");

        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(Pdf);
    }

    [Fact]
    public async Task Get_GalleryKeepsFailing_OpensTheCircuitAndAnswers502WithoutAskingIt()
    {
        // While the gallery keeps failing, a creation from a template answers 502 at once instead of waiting for it
        var gallery = Gallery(_ => new StubResponse(HttpStatusCode.ServiceUnavailable));
        var manager = CreateManager(gallery.Url);

        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);
        var asked = gallery.CallCount;

        await AssertGalleryUnavailableAsync(manager);

        gallery.CallCount.Should().Be(asked);
    }

    [Fact]
    public async Task Get_FewerThanFiveFailedRequests_LeaveTheCircuitClosed()
    {
        // One failed creation makes 3 requests, fewer than the 5 the breaker needs: the next creation asks again
        var gallery = Gallery(_ => new StubResponse(HttpStatusCode.ServiceUnavailable));
        var manager = CreateManager(gallery.Url);

        await AssertGalleryUnavailableAsync(manager);
        var asked = gallery.CallCount;

        await AssertGalleryUnavailableAsync(manager);

        asked.Should().Be(3);
        gallery.CallCount.Should().BeGreaterThan(asked);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public async Task Get_AtLeastHalfOfFiveRequestsFailed_OpensTheCircuit(int creations, bool opens)
    {
        // Each creation asks the host twice, for the list and the file; then one creation fails with 3 requests.
        // After one creation 3 of 5 requests failed and the circuit opens; after two, 3 of 7, and it stays closed
        var failing = false;
        var gallery = Gallery(request => Volatile.Read(ref failing)
            ? new StubResponse(HttpStatusCode.ServiceUnavailable)
            : IsFile(request)
                ? new StubResponse(HttpStatusCode.OK, Pdf)
                : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));
        var manager = CreateManager(gallery.Url);

        for (var i = 0; i < creations; i++)
        {
            await using var stream = await manager.Get(1, ".pdf");
        }

        Volatile.Write(ref failing, true);
        await AssertGalleryUnavailableAsync(manager);
        var asked = gallery.CallCount;

        await AssertGalleryUnavailableAsync(manager);

        if (opens)
        {
            gallery.CallCount.Should().Be(asked);
        }
        else
        {
            gallery.CallCount.Should().BeGreaterThan(asked);
        }
    }

    [Fact]
    public async Task Get_ListAnswers4xxEveryTime_LeavesTheCircuitClosed()
    {
        // Only a request that cannot connect, times out or gets 5xx, 408 or 429 counts as failed: a 403 of a proxy is
        // an answer of the host, not retried, and the breaker takes it as an answered request
        var gallery = Gallery(_ => new StubResponse(HttpStatusCode.Forbidden));
        var manager = CreateManager(gallery.Url);

        for (var i = 0; i < 6; i++)
        {
            await AssertGalleryUnavailableAsync(manager);
        }

        gallery.CallCount.Should().Be(6);
    }

    [Fact]
    public async Task Get_ListAnswersAfterRetriesAndTheFileKeepsFailing_AsksSixTimes()
    {
        // A failed creation can make up to 3 list requests and 3 file requests: the list answers on its third
        // attempt, and the file of the same host fails all three
        var lists = 0;
        var gallery = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.ServiceUnavailable)
            : Interlocked.Increment(ref lists) <= 2
                ? new StubResponse(HttpStatusCode.ServiceUnavailable)
                : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url));

        gallery.CallCount.Should().Be(6);
    }

    [Fact]
    public async Task Get_FourFailedRequests_LeaveTheCircuitClosed()
    {
        // The breaker needs 5 requests: after 4 failed ones the next attempt still reaches the gallery and the
        // creation gets its file. Then 4 of 6 requests have failed, yet the circuit stays closed: it opens only on a
        // failed request, so the next creation asks the gallery and gets its file too
        var failures = 0;
        var gallery = Gallery(request => !IsFile(request) && Interlocked.Increment(ref failures) <= 4
            ? new StubResponse(HttpStatusCode.ServiceUnavailable)
            : IsFile(request)
                ? new StubResponse(HttpStatusCode.OK, Pdf)
                : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));
        var manager = CreateManager(gallery.Url);

        await AssertGalleryUnavailableAsync(manager);

        await using (var stream = await manager.Get(1, ".pdf"))
        {
            stream.Length.Should().BeGreaterThan(0);
        }

        var asked = gallery.CallCount;
        await using (var again = await manager.Get(1, ".pdf"))
        {
            again.Length.Should().BeGreaterThan(0);
        }

        gallery.CallCount.Should().Be(asked + 2);
    }

    [Fact]
    public async Task Get_ExactlyHalfOfSixRequestsFailed_OpensTheCircuit()
    {
        // 3 answered requests (a creation with its file, a list answering 403), then a creation whose 3 requests
        // fail: 3 of 6 is half, the circuit opens, and the next creation answers 502 without asking
        var phase = 0;
        var gallery = Gallery(request => Volatile.Read(ref phase) switch
        {
            0 => IsFile(request) ? new StubResponse(HttpStatusCode.OK, Pdf) : StubResponse.Json(HttpStatusCode.OK, ListWithFile()),
            1 => new StubResponse(HttpStatusCode.Forbidden),
            _ => new StubResponse(HttpStatusCode.ServiceUnavailable)
        });
        var manager = CreateManager(gallery.Url);

        await using (await manager.Get(1, ".pdf"))
        {
        }

        Volatile.Write(ref phase, 1);
        await AssertGalleryUnavailableAsync(manager);
        Volatile.Write(ref phase, 2);
        await AssertGalleryUnavailableAsync(manager);
        var asked = gallery.CallCount;

        await AssertGalleryUnavailableAsync(manager);

        asked.Should().Be(6);
        gallery.CallCount.Should().Be(asked);
    }

    [Fact]
    public async Task Get_JustUnderHalfOfThirteenRequestsFailed_LeavesTheCircuitClosed()
    {
        // 7 answered requests (three creations with their files, a list answering 403), then two creations whose 3
        // requests each fail: the last failure makes 6 of 13, just under half, and the next creation still asks the
        // gallery. With a lower ratio (6 of 13 is about 0.46) the circuit would open there
        var phase = 0;
        var gallery = Gallery(request => Volatile.Read(ref phase) switch
        {
            0 => IsFile(request) ? new StubResponse(HttpStatusCode.OK, Pdf) : StubResponse.Json(HttpStatusCode.OK, ListWithFile()),
            1 => new StubResponse(HttpStatusCode.Forbidden),
            _ => new StubResponse(HttpStatusCode.ServiceUnavailable)
        });
        var manager = CreateManager(gallery.Url);

        for (var i = 0; i < 3; i++)
        {
            await using (await manager.Get(1, ".pdf"))
            {
            }
        }

        Volatile.Write(ref phase, 1);
        await AssertGalleryUnavailableAsync(manager);
        Volatile.Write(ref phase, 2);
        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);
        var asked = gallery.CallCount;

        await AssertGalleryUnavailableAsync(manager);

        asked.Should().Be(13);
        gallery.CallCount.Should().BeGreaterThan(asked);
    }

    [Fact]
    public async Task Get_ErrorStatusesOfATemplateFile_CountAsAnsweredRequests()
    {
        // Two creations of a broken template answer 8 requests (a list and three file attempts each). The breaker takes
        // them as answered, so 6 failed list requests after them are 6 of 14 and leave the circuit closed
        var phase = 0;
        var gallery = Gallery(request => Volatile.Read(ref phase) == 0 && !IsFile(request)
            ? StubResponse.Json(HttpStatusCode.OK, ListWithFile())
            : new StubResponse(HttpStatusCode.ServiceUnavailable));
        var manager = CreateManager(gallery.Url);

        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);
        gallery.CallCount.Should().Be(8);

        Volatile.Write(ref phase, 1);
        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);
        var asked = gallery.CallCount;

        await AssertGalleryUnavailableAsync(manager);

        asked.Should().Be(14);
        gallery.CallCount.Should().BeGreaterThan(asked);
    }

    [Fact]
    public async Task Get_RetriesOfTheClient_EndWithTheDownloadDeadline()
    {
        // The deadline of the download is the outer strategy: a retry that would wait past it never starts
        var gallery = Gallery(_ => new StubResponse(HttpStatusCode.ServiceUnavailable));
        // A delay far past the deadline: the jitter never brings the first retry under it
        var manager = CreateManager(gallery.Url, downloadTimeout: TimeSpan.FromMilliseconds(500), retryDelay: TimeSpan.FromHours(1));

        var watch = Stopwatch.StartNew();
        await AssertGalleryUnavailableAsync(manager);

        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        gallery.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_GalleryAsksToRetryAfterAMinute_Answers502WithoutWaiting()
    {
        // A proxy of a closed network or the rate limiter of the gallery answers 503 with Retry-After: 60. The retries
        // keep their own short waits instead of holding the editor until the download deadline
        var gallery = RetryAfter(seconds: 60);
        var manager = CreateManager(gallery.Url, downloadTimeout: TimeSpan.FromSeconds(10));

        var watch = Stopwatch.StartNew();
        await AssertGalleryUnavailableAsync(manager);

        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        gallery.CallCount.Should().Be(3);
    }

    [Fact]
    public async Task Get_FailedAttempts_AreNotLoggedAsWarningsByPolly()
    {
        // The manager logs a gallery that fails as a warning of its own: each failed attempt goes to the Debug level,
        // not one warning of Polly per attempt
        var logs = new RecordingLoggerProvider();
        var gallery = Gallery(_ => new StubResponse(HttpStatusCode.ServiceUnavailable));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url, loggerProvider: logs));

        var attempts = logs.Entries.Where(entry => entry.Message.Contains("Execution attempt", StringComparison.Ordinal)).ToList();
        attempts.Should().NotBeEmpty();
        attempts.Should().OnlyContain(entry => entry.Level == LogLevel.Debug);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "", "")]
    [InlineData(HttpStatusCode.OK, "<html>Access denied</html>", "")]
    [InlineData(HttpStatusCode.OK, Pdf, "ftp://user:secret@gallery.example")]
    public async Task Get_FileAtASignedAddressFails_LogsTheAddressWithoutItsSignature(HttpStatusCode status, string body, string fileHost)
    {
        // A gallery that keeps its files in a private bucket names them by signed addresses: the warning names the
        // file, not a working link to it. The cases: an error status, a file without its signature, an address that
        // cannot be requested, with user info as well
        var logs = new RecordingLoggerProvider();
        var gallery = Gallery(request => IsFile(request)
            ? new StubResponse(status, body)
            : StubResponse.Json(HttpStatusCode.OK,
                $$"""{"data":[{"id":1,"file_oform":[{"url":"{{(fileHost == "" ? _fileUrl : fileHost + "/uploads/file")}}.pdf?X-Amz-Signature=secret","name":"form","ext":".pdf"}]}]}"""));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url, loggerProvider: logs));

        logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning && entry.Message.Contains("/uploads/file.pdf", StringComparison.Ordinal));
        logs.Entries.Should().NotContain(entry => entry.Message.Contains("secret", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_FileOverTheLimitAtASignedAddress_LogsTheAddressWithoutItsSignature(bool announced)
    {
        // Both checks of the size name the file in their warning: by the announced length and while the file is copied
        var logs = new RecordingLoggerProvider();
        var file = announced
            ? RawFile(head: "", contentLength: OFormRequestManager.MaxTemplateSize + 1, padding: 0, stall: true)
            : RawFile(head: Pdf, contentLength: null, padding: OFormRequestManager.MaxTemplateSize, stall: false);
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK,
            $$"""{"data":[{"id":1,"file_oform":[{"url":"{{file.Url}}/file.pdf?X-Amz-Signature=secret","name":"form","ext":".pdf"}]}]}"""));

        await AssertGalleryUnavailableAsync(CreateManager(gallery.Url, clientTimeout: Timeout.InfiniteTimeSpan,
            downloadTimeout: TimeSpan.FromSeconds(20), loggerProvider: logs));

        logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning
            && entry.Message.Contains("larger than", StringComparison.Ordinal) && entry.Message.Contains("/file.pdf", StringComparison.Ordinal));
        logs.Entries.Should().NotContain(entry => entry.Message.Contains("secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GalleryAddressCannotBeRequested_IsLoggedOnce()
    {
        // A bad address in the configuration is logged when the manager is created, not on every creation and check
        var logs = new RecordingLoggerProvider();
        var manager = CreateManager("ftp://gallery.example", loggerProvider: logs);

        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);
        (await manager.IsAvailableAsync()).Should().BeFalse();

        logs.Entries.Count(entry => entry.Message.Contains("ftp://gallery.example", StringComparison.Ordinal)).Should().Be(1);
    }

    [Fact]
    public async Task GalleryAddressCannotBeRequested_IsLoggedWithoutItsUserInfo()
    {
        // An address of the configuration with a password in it: the warning names the address, not the password
        var logs = new RecordingLoggerProvider();
        var manager = CreateManager("ftp://user:secret@gallery.example/cms?token=secret", loggerProvider: logs);

        await AssertGalleryUnavailableAsync(manager);

        logs.Entries.Should().Contain(entry => entry.Level == LogLevel.Warning && entry.Message.Contains("ftp://gallery.example/cms", StringComparison.Ordinal));
        logs.Entries.Should().NotContain(entry => entry.Message.Contains("secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Get_GalleryKeepsFailing_LogsTheFailureOncePerKind()
    {
        // A gallery that keeps refusing is logged on the first creation from a template, not on every one; when it
        // starts failing another way, that is logged at once. A 403 is not retried and does not open the circuit, so
        // each creation reaches the gallery and meets the same failure again
        var logs = new RecordingLoggerProvider();
        var noList = false;
        var gallery = Gallery(_ => Volatile.Read(ref noList)
            ? StubResponse.Json(HttpStatusCode.OK, """{"error":"not the list"}""")
            : new StubResponse(HttpStatusCode.Forbidden));
        var manager = CreateManager(gallery.Url, loggerProvider: logs);

        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);

        Volatile.Write(ref noList, true);
        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);

        gallery.CallCount.Should().Be(5);
        logs.Entries.Count(entry => entry.Level == LogLevel.Warning && Regex.IsMatch(entry.Message, @"^The template gallery answered 403 to http\S+$")).Should().Be(1);
        logs.Entries.Count(entry => entry.Level == LogLevel.Warning && Regex.IsMatch(entry.Message, @"^The template gallery answered http\S+ without its template list$")).Should().Be(1);
    }

    [Fact]
    public async Task Get_FileOfOneTemplateKeepsFailing_DoesNotOpenTheCircuitOfItsHost()
    {
        // The breaker of a host is shared by every portal of the process: one template whose file answers 5xx must not
        // make every template of that host answer 502 for 30 seconds
        var fileFails = true;
        var gallery = Gallery(request => IsFile(request)
            ? fileFails ? new StubResponse(HttpStatusCode.ServiceUnavailable) : new StubResponse(HttpStatusCode.OK, Pdf)
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));
        var manager = CreateManager(gallery.Url);

        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);

        fileFails = false;
        await using var stream = await manager.Get(1, ".pdf");

        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(Pdf);
    }

    [Fact]
    public async Task Get_FileHostKeepsFailing_LeavesTheCircuitOfTheListHostClosed()
    {
        // The list and its files may come from different hosts: a file host that is down opens the circuit of that host
        // only, and the list is still asked
        var fileHost = Unreachable();
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK,
            $$"""{"data":[{"id":1,"file_oform":[{"url":"{{fileHost.Url}}/file.pdf","name":"form","ext":".pdf"}]}]}"""));
        var manager = CreateManager(gallery.Url);

        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);
        await AssertGalleryUnavailableAsync(manager);

        gallery.CallCount.Should().Be(3);
    }

    [Fact]
    public async Task Get_AttemptOutlastsItsTimeout_IsAskedAgain()
    {
        // The first answer of the list comes after the attempt timeout: that attempt is given up and the next one gets
        // the list, well inside the download deadline
        var answered = 0;
        var gallery = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.OK, Pdf)
            : Interlocked.Exchange(ref answered, 1) == 0
                ? StubResponse.Json(HttpStatusCode.OK, ListWithFile()).After(TimeSpan.FromSeconds(5))
                : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));
        var manager = CreateManager(gallery.Url, clientTimeout: Timeout.InfiniteTimeSpan, downloadTimeout: TimeSpan.FromSeconds(10),
            attemptTimeout: TimeSpan.FromMilliseconds(300));

        await using var stream = await manager.Get(1, ".pdf");

        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(Pdf);

        // At least the timed-out attempt and the one that got the list; a busy runner may give up one more
        gallery.Requests.Count(request => !IsFile(request)).Should().BeGreaterThanOrEqualTo(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_FileOfExactlyTheLimit_ReturnsIt(bool announced)
    {
        // "Of at most 50 MB": a file of the limit itself is accepted, by its announced length and by the copy
        var file = RawFile(head: Pdf, contentLength: announced ? OFormRequestManager.MaxTemplateSize : null,
            padding: OFormRequestManager.MaxTemplateSize - Pdf.Length, stall: false);

        await using var stream = await CreateManager(GalleryWithFileAt(file.Url), downloadTimeout: TimeSpan.FromSeconds(20)).Get(1, ".pdf");

        stream.Length.Should().Be(OFormRequestManager.MaxTemplateSize);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_ListOfExactlyItsLimit_ReturnsTheFile(bool announced)
    {
        var files = Gallery(request => IsFile(request)
            ? new StubResponse(HttpStatusCode.OK, Pdf)
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile()));
        var list = ListWithFile();
        var gallery = RawFile(head: list, contentLength: announced ? OFormRequestManager.MaxListSize : null,
            padding: OFormRequestManager.MaxListSize - list.Length, stall: false, paddingByte: (byte)' ');

        await using var stream = await CreateManager(gallery.Url).Get(1, ".pdf");

        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(Pdf);
        files.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_FormatWithoutASignatureCheck_ReturnsTheFileAsSent()
    {
        // Only a PDF and the Office formats have their signature checked: a file of another format is returned as sent
        const string file = "<html>not checked</html>";
        var gallery = Gallery(request => request.Path.EndsWith("/file.oform", StringComparison.Ordinal)
            ? new StubResponse(HttpStatusCode.OK, file)
            : StubResponse.Json(HttpStatusCode.OK, ListWithFile(".oform")));

        await using var stream = await CreateManager(gallery.Url).Get(1, ".oform");

        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).Should().Be(file);
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

    [Fact]
    public async Task IsAvailableAsync_AsksWithTheClientWithoutCookies()
    {
        // The default client and the other standard ones keep the cookies the gallery sets for every caller of the
        // process. The check asks with customHttpClientNoCookie, whose handler keeps none
        // (StandardHttpClientsTests.NoCookieClient_KeepsNoCookies): every other client of this test fails, the gallery's
        // own too, since no pipeline of the gallery is registered here
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, List));
        var manager = CreateManager(gallery.Url, registerPipelines: false, onlyWorkingClient: "customHttpClientNoCookie");

        (await manager.IsAvailableAsync()).Should().BeTrue();
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
    public async Task IsAvailableAsync_GalleryDoesNotAnswerBeforeTheProbeDeadline_KeepsFalse()
    {
        // The stub answers after the 5-second hard timeout of the cache. With the probe deadline the check answers
        // `false` and the cache keeps it; without it the first call would wait for the hard timeout, and the second would
        // wait for the check still running. The time tells the registered deadline from the manager's own 4 seconds
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, List).After(TimeSpan.FromSeconds(10)));
        var manager = CreateManager(gallery.Url, clientTimeout: Timeout.InfiniteTimeSpan, probeTimeout: TimeSpan.FromMilliseconds(500));

        var watch = Stopwatch.StartNew();
        (await manager.IsAvailableAsync()).Should().BeFalse();
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        (await manager.IsAvailableAsync()).Should().BeFalse();

        gallery.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task IsAvailableAsync_CheckOutlastsTheHardTimeoutOfTheCacheWithNothingKnown_IsFalse()
    {
        // The deadline of the check is longer than the hard timeout of the cache: the cache gives up first, and the
        // reader still gets "unavailable", not an exception that the endpoint would turn into a 500
        using var cache = new FusionCache(new FusionCacheOptions
        {
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.MaxValue,
                LockTimeout = TimeSpan.FromSeconds(5),
                FactoryHardTimeout = TimeSpan.FromMilliseconds(300)
            }
        });
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, List).After(TimeSpan.FromSeconds(5)));
        var manager = CreateManager(gallery.Url, clientTimeout: Timeout.InfiniteTimeSpan, cache: cache, probeTimeout: TimeSpan.FromSeconds(10));
        var checkEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        cache.Events.BackgroundFactorySuccess += (_, _) => checkEnded.TrySetResult();
        cache.Events.BackgroundFactoryError += (_, _) => checkEnded.TrySetResult();

        var watch = Stopwatch.StartNew();
        (await manager.IsAvailableAsync()).Should().BeFalse();

        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));

        // The check goes on in the background: the test ends after it, not under it with the cache disposed
        await checkEnded.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsAvailableAsync_DeadlineOfTheCheck_IsNotLoggedAsAnErrorByPolly(bool registered)
    {
        // The manager logs a gallery that does not answer as a warning of its own; the timeout event of Polly goes to the
        // Debug level, both through the registered pipeline and through the one the manager adds in a host without it.
        // That one has the manager's own 4-second deadline: the test pins that it ends the check before the 5-second
        // hard timeout of the cache, which would answer without a timeout of Polly
        var logs = new RecordingLoggerProvider();
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, List).After(TimeSpan.FromSeconds(10)));
        var manager = CreateManager(gallery.Url, clientTimeout: Timeout.InfiniteTimeSpan,
            probeTimeout: registered ? TimeSpan.FromMilliseconds(500) : null, loggerProvider: logs, registerPipelines: registered);

        (await manager.IsAvailableAsync()).Should().BeFalse();

        var timeouts = logs.Entries.Where(entry => entry.Message.Contains("OnTimeout", StringComparison.Ordinal)).ToList();
        timeouts.Should().NotBeEmpty();
        timeouts.Should().OnlyContain(entry => entry.Level == LogLevel.Debug);
    }

    [Fact]
    public async Task IsAvailableAsync_GalleryAnswers5xx_IsFalseWithoutAskingAgain()
    {
        // The check is not retried: its answer is cached, and retries would only push it past the hard timeout
        var gallery = Gallery(_ => new StubResponse(HttpStatusCode.ServiceUnavailable));

        (await CreateManager(gallery.Url).IsAvailableAsync()).Should().BeFalse();

        gallery.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task IsAvailableAsync_ListLargerThanItsLimit_IsFalse()
    {
        // A valid list padded with spaces past the limit: only the limit makes it "unavailable"
        var gallery = RawFile(head: List, contentLength: null, padding: OFormRequestManager.MaxListSize, stall: false,
            paddingByte: (byte)' ');

        (await CreateManager(gallery.Url).IsAvailableAsync()).Should().BeFalse();
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
    public async Task IsAvailableAsync_CheckThrowsForAReasonOtherThanTheGallery_IsFalse()
    {
        // An exception that is not a failure of the gallery, with nothing known: the endpoint answers false, not 500
        var gallery = Gallery(_ => StubResponse.Json(HttpStatusCode.OK, List));
        var manager = CreateManager(gallery.Url, primaryHandler: () => new ThrowingHandler(new ObjectDisposedException("handler")));

        (await manager.IsAvailableAsync()).Should().BeFalse();
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
        _cache.Events.FactorySuccess += (_, _) => refreshFailed.TrySetResult();
        _cache.Events.FactoryError += (_, _) => refreshFailed.TrySetResult();
        _cache.Events.BackgroundFactorySuccess += (_, _) => refreshFailed.TrySetResult();
        _cache.Events.BackgroundFactoryError += (_, _) => refreshFailed.TrySetResult();
        await _cache.ExpireAsync(AvailabilityCacheKey(gallery), token: TestContext.Current.CancellationToken);

        (await manager.IsAvailableAsync()).Should().BeTrue();
        await refreshFailed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        gallery.CallCount.Should().Be(2);

        // The failed check is kept for a minute: the next call does not ask the gallery again
        (await manager.IsAvailableAsync()).Should().BeTrue();
        gallery.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task IsAvailableAsync_ChecksKeepFailing_KeepTheKnownTrueForADayAfterTheLastAnswer()
    {
        // Every failed check stores the entry again, so the fail-safe of the cache alone would keep a known true for as
        // long as the checks keep failing: a gallery cut off for good would stay "available". The time of the last
        // answer ends it a day after the gallery last answered, however many checks fail meanwhile
        var time = new ManualTimeProvider();
        var failing = false;
        var gallery = Gallery(_ => Volatile.Read(ref failing)
            ? new StubResponse(HttpStatusCode.ServiceUnavailable)
            : StubResponse.Json(HttpStatusCode.OK, List));
        var manager = CreateManager(gallery.Url, timeProvider: time);
        var token = TestContext.Current.CancellationToken;

        (await manager.IsAvailableAsync()).Should().BeTrue();

        Volatile.Write(ref failing, true);
        for (var hour = 1; hour < 24; hour++)
        {
            time.Advance(TimeSpan.FromHours(1));
            await _cache.ExpireAsync(AvailabilityCacheKey(gallery), token: token);

            (await manager.IsAvailableAsync()).Should().BeTrue($"the gallery answered {hour} hours ago");
        }

        time.Advance(TimeSpan.FromHours(1));
        await _cache.ExpireAsync(AvailabilityCacheKey(gallery), token: token);

        (await manager.IsAvailableAsync()).Should().BeFalse();

        // The check itself stops keeping the "available", not only the answer to the reader: the cache then holds
        // "unavailable". Waiting for it also lets a check that ended in the background finish before the test does
        await WaitForCachedAnswerAsync(_cache, AvailabilityCacheKey(gallery), expected: false);
    }

    [Fact]
    public async Task IsAvailableAsync_SlowCheckADayAfterTheLastAnswer_IsFalseWithinTheWait()
    {
        // In a closed network the check hangs until its deadline, and the cache answers its stale value while the check
        // runs on. An "available" a day after the gallery last answered is not given out on that path either
        var time = new ManualTimeProvider();
        var slow = false;
        var gallery = Gallery(_ => Volatile.Read(ref slow)
            ? new StubResponse(HttpStatusCode.ServiceUnavailable).After(TimeSpan.FromSeconds(3))
            : StubResponse.Json(HttpStatusCode.OK, List));
        var manager = CreateManager(gallery.Url, timeProvider: time);
        var token = TestContext.Current.CancellationToken;

        (await manager.IsAvailableAsync()).Should().BeTrue();

        Volatile.Write(ref slow, true);
        time.Advance(TimeSpan.FromDays(1));
        await _cache.ExpireAsync(AvailabilityCacheKey(gallery), token: token);
        var checkEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _cache.Events.BackgroundFactorySuccess += (_, _) => checkEnded.TrySetResult();
        _cache.Events.BackgroundFactoryError += (_, _) => checkEnded.TrySetResult();

        var watch = Stopwatch.StartNew();
        (await manager.IsAvailableAsync()).Should().BeFalse();
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));

        // The check goes on in the background: the test ends after it, not under it with the cache disposed
        await checkEnded.Task.WaitAsync(TimeSpan.FromSeconds(15), token);
        (await manager.IsAvailableAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_SlowFailedCheckWithAvailableKnown_IsThrottledToo()
    {
        // A failed check slower than the 200 ms the reader waits for ends in the background: the throttle of the
        // fail-safe has to hold there as well, or every reader starts a new check while the gallery hangs
        var failing = false;
        var gallery = Gallery(_ => failing
            ? new StubResponse(HttpStatusCode.BadGateway).After(TimeSpan.FromSeconds(1))
            : StubResponse.Json(HttpStatusCode.OK, List));
        var manager = CreateManager(gallery.Url);

        (await manager.IsAvailableAsync()).Should().BeTrue();

        failing = true;
        var refreshFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _cache.Events.BackgroundFactorySuccess += (_, _) => refreshFailed.TrySetResult();
        _cache.Events.BackgroundFactoryError += (_, _) => refreshFailed.TrySetResult();
        await _cache.ExpireAsync(AvailabilityCacheKey(gallery), token: TestContext.Current.CancellationToken);

        (await manager.IsAvailableAsync()).Should().BeTrue();
        await refreshFailed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        gallery.CallCount.Should().Be(2);

        (await manager.IsAvailableAsync()).Should().BeTrue();
        gallery.CallCount.Should().Be(2);
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
        var checkEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _cache.Events.BackgroundFactorySuccess += (_, _) => checkEnded.TrySetResult();
        _cache.Events.BackgroundFactoryError += (_, _) => checkEnded.TrySetResult();

        var watch = Stopwatch.StartNew();
        (await manager.IsAvailableAsync()).Should().BeTrue();
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));

        // The check goes on in the background: the test ends after it, not under it with the cache disposed
        await checkEnded.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
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

    [Fact]
    public async Task IsAvailableAsync_LocalFalseAndATrueOfAnotherNode_FailedCheckKeepsTheTrue()
    {
        // A node that failed with nothing known keeps its "unavailable" to itself; another node has since put an
        // "available" into the distributed cache, which has expired there but is kept by its fail-safe. When the node's
        // own "unavailable" has expired too and its check fails, the "available" of the last day wins
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        using var first = CreateNodeCache(distributed);
        using var second = CreateNodeCache(distributed);
        var gallery = Gallery(_ => new StubResponse(HttpStatusCode.BadGateway));
        var firstManager = CreateManager(gallery.Url, cache: first);
        var key = AvailabilityCacheKey(gallery);
        var token = TestContext.Current.CancellationToken;

        (await firstManager.IsAvailableAsync()).Should().BeFalse();

        // The other node's answer, already past its duration in the distributed cache, and the node's own minute over
        await second.SetAsync(key, new OFormRequestManager.AvailabilityAnswer(true, DateTimeOffset.UtcNow), options => options
            .SetDuration(TimeSpan.FromMilliseconds(1))
            .SetFailSafe(true, TimeSpan.FromDays(1)), token);
        await first.ExpireAsync(key, options => options.SkipDistributedCacheWrite = true, token);
        await Task.Delay(TimeSpan.FromMilliseconds(50), token);

        // The failed check keeps the "available", whether it ends within the 200 ms wait or in the background: what
        // holds is the answer the node caches after it, not which path the first reader took
        await firstManager.IsAvailableAsync();
        await WaitForCachedAnswerAsync(first, key, expected: true);
        (await firstManager.IsAvailableAsync()).Should().BeTrue();
        gallery.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task IsAvailableAsync_SlowCheckWithLocalFalseAndATrueOfAnotherNode_ReaderGetsTheLocalFalse()
    {
        // The window the remarks name: a node holds its own expired "unavailable", another node's "available" of the
        // last day is in the distributed cache, and the check outlasts the 200 ms wait. The reader of that wait gets
        // the node's own "unavailable"; the check then keeps the other node's "available"
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        using var first = CreateNodeCache(distributed);
        using var second = CreateNodeCache(distributed);
        var slow = false;
        var gallery = Gallery(_ => Volatile.Read(ref slow)
            ? new StubResponse(HttpStatusCode.BadGateway).After(TimeSpan.FromSeconds(3))
            : new StubResponse(HttpStatusCode.BadGateway));
        var firstManager = CreateManager(gallery.Url, cache: first);
        var key = AvailabilityCacheKey(gallery);
        var token = TestContext.Current.CancellationToken;

        (await firstManager.IsAvailableAsync()).Should().BeFalse();

        await second.SetAsync(key, new OFormRequestManager.AvailabilityAnswer(true, DateTimeOffset.UtcNow), options => options
            .SetDuration(TimeSpan.FromMilliseconds(1))
            .SetFailSafe(true, TimeSpan.FromDays(1)), token);
        await first.ExpireAsync(key, options => options.SkipDistributedCacheWrite = true, token);
        await Task.Delay(TimeSpan.FromMilliseconds(50), token);
        Volatile.Write(ref slow, true);
        var checkEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        first.Events.BackgroundFactorySuccess += (_, _) => checkEnded.TrySetResult();
        first.Events.BackgroundFactoryError += (_, _) => checkEnded.TrySetResult();

        var watch = Stopwatch.StartNew();
        (await firstManager.IsAvailableAsync()).Should().BeFalse();
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));

        await checkEnded.Task.WaitAsync(TimeSpan.FromSeconds(15), token);
        await WaitForCachedAnswerAsync(first, key, expected: true);
    }

    [Theory]
    [InlineData(23, false, true)]
    [InlineData(24, false, false)]
    [InlineData(23, true, true)]
    [InlineData(24, true, false)]
    public async Task IsAvailableAsync_RestartedNodeAndATrueInTheDistributedCache_KeepsItForTheDay(int hours, bool slowCheck, bool expected)
    {
        // A node that starts with an empty memory (a restart, a new pod) while the distributed cache holds an expired
        // "available" of another node: a failed check, fast or slower than the 200 ms wait, keeps that answer for a
        // day after the gallery last answered, and not after it
        var time = new ManualTimeProvider();
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        using var writer = CreateNodeCache(distributed);
        using var restarted = CreateNodeCache(distributed);
        var gallery = Gallery(_ => slowCheck
            ? new StubResponse(HttpStatusCode.BadGateway).After(TimeSpan.FromSeconds(3))
            : new StubResponse(HttpStatusCode.BadGateway));
        var key = AvailabilityCacheKey(gallery);
        var token = TestContext.Current.CancellationToken;

        await writer.SetAsync(key, new OFormRequestManager.AvailabilityAnswer(true, time.GetUtcNow()), options => options
            .SetDuration(TimeSpan.FromMilliseconds(1))
            .SetFailSafe(true, TimeSpan.FromDays(1)), token);
        await Task.Delay(TimeSpan.FromMilliseconds(50), token);
        time.Advance(TimeSpan.FromHours(hours));
        var checkEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        restarted.Events.FactorySuccess += (_, _) => checkEnded.TrySetResult();
        restarted.Events.BackgroundFactorySuccess += (_, _) => checkEnded.TrySetResult();
        restarted.Events.BackgroundFactoryError += (_, _) => checkEnded.TrySetResult();

        var watch = Stopwatch.StartNew();
        (await CreateManager(gallery.Url, cache: restarted, timeProvider: time).IsAvailableAsync()).Should().Be(expected);
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));

        // The check ends before the test does, not under it with the cache disposed. For a fast check the cache then
        // holds what the day allows; a slow one is pinned by the answer within the wait only
        await checkEnded.Task.WaitAsync(TimeSpan.FromSeconds(15), token);
        await WaitForCachedAnswerAsync(restarted, key, expected);
    }

    [Fact]
    public async Task IsAvailableAsync_TrueOfAnotherNode_EndsTheLocalFalseBeforeItsMinute()
    {
        // A process that failed its check keeps "unavailable" to itself for a minute. When another process gets the
        // list, its "available" goes into the distributed cache and its notification through the backplane ends that
        // minute: the first process answers "available" at once, without asking the gallery again
        var hub = new HeldBackplaneHub();
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        using var first = CreateNodeCache(distributed, hub);
        using var second = CreateNodeCache(distributed, hub);
        var answering = false;
        var gallery = Gallery(_ => Volatile.Read(ref answering)
            ? StubResponse.Json(HttpStatusCode.OK, List)
            : new StubResponse(HttpStatusCode.BadGateway));
        var firstManager = CreateManager(gallery.Url, cache: first);

        (await firstManager.IsAvailableAsync()).Should().BeFalse();

        Volatile.Write(ref answering, true);
        (await CreateManager(gallery.Url, cache: second).IsAvailableAsync()).Should().BeTrue();

        (await firstManager.IsAvailableAsync()).Should().BeTrue();
        gallery.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task IsAvailableAsync_TrueOfAnotherNodeDuringTheLocalCheck_EndsItsFalseWhenItArrives()
    {
        // Two processes check at almost the same moment: the second gets the list and writes "available" while the
        // first is still waiting for the gallery, which then fails it. Until the notification of the "available"
        // arrives, the first process answers its own "unavailable"; when it arrives, after that "unavailable" was
        // stored, it still ends it, and the first process answers "available" without asking the gallery again
        var hub = new HeldBackplaneHub();
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        using var first = CreateNodeCache(distributed, hub);
        using var second = CreateNodeCache(distributed, hub);
        var requests = 0;
        var gallery = Gallery(_ => Interlocked.Increment(ref requests) == 1
            ? new StubResponse(HttpStatusCode.BadGateway).After(TimeSpan.FromSeconds(2))
            : StubResponse.Json(HttpStatusCode.OK, List));
        var firstManager = CreateManager(gallery.Url, cache: first);
        var token = TestContext.Current.CancellationToken;

        hub.Hold = true;
        var firstCheck = firstManager.IsAvailableAsync();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (Volatile.Read(ref requests) == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, token);
        }

        (await CreateManager(gallery.Url, cache: second).IsAvailableAsync()).Should().BeTrue();
        (await firstCheck.WaitAsync(TimeSpan.FromSeconds(10), token)).Should().BeFalse();
        (await firstManager.IsAvailableAsync()).Should().BeFalse();

        hub.Hold = false;
        await hub.DeliverHeldAsync();

        (await firstManager.IsAvailableAsync()).Should().BeTrue();
        gallery.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task IsAvailableAsync_SlowFailedCheckWithUnavailableKnown_StaysOnTheNodeThatChecked()
    {
        // The same as above when the failed check outlasts the 200 ms the reader waits for and ends in the background:
        // the options the check sets for "unavailable" must hold on that path too
        var distributed = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        using var first = CreateNodeCache(distributed);
        using var second = CreateNodeCache(distributed);
        var mode = "fail";
        var gallery = Gallery(_ => mode switch
        {
            "fail" => new StubResponse(HttpStatusCode.BadGateway),
            "slow" => new StubResponse(HttpStatusCode.BadGateway).After(TimeSpan.FromSeconds(3)),
            _ => StubResponse.Json(HttpStatusCode.OK, List)
        });
        var firstManager = CreateManager(gallery.Url, cache: first);

        (await firstManager.IsAvailableAsync()).Should().BeFalse();

        mode = "slow";
        var checkEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        first.Events.BackgroundFactorySuccess += (_, _) => checkEnded.TrySetResult();
        await first.ExpireAsync(AvailabilityCacheKey(gallery), token: TestContext.Current.CancellationToken);

        // The expired `false` comes within the wait, well before the slow check could give a new one
        var watch = Stopwatch.StartNew();
        (await firstManager.IsAvailableAsync()).Should().BeFalse();
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        await checkEnded.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        gallery.CallCount.Should().Be(2);

        mode = "answer";
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
        MaybeValue<OFormRequestManager.AvailabilityAnswer> answer;

        do
        {
            answer = await cache.TryGetAsync<OFormRequestManager.AvailabilityAnswer>(key, options => options.AllowStaleOnReadOnly = false, token);
            if (answer.HasValue && answer.Value.Available == expected)
            {
                return;
            }

            await Task.Delay(100, token);
        }
        while (DateTime.UtcNow < deadline);

        var seen = answer.HasValue ? answer.Value.Available.ToString() : "nothing";
        throw new TimeoutException($"The gallery availability cache did not store {expected}; the last read gave {seen}.");
    }

    private static FusionCache CreateNodeCache(IDistributedCache distributed, HeldBackplaneHub? backplane = null)
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
        if (backplane is not null)
        {
            cache.SetupBackplane(backplane.CreateBackplane());
        }

        return cache;
    }

    private HttpStub Gallery(Func<StubRequest, StubResponse> respond)
    {
        var stub = new HttpStub(respond);
        _stubs.Add(stub);
        _fileUrl = stub.Url + "/uploads/file";

        return stub;
    }

    private TruncatingServer Truncating(bool stall = false)
    {
        var server = new TruncatingServer(stall);
        _rawServers.Add(server);

        return server;
    }

    private RawFileServer RawFile(string head, long? contentLength, long padding, bool stall, byte paddingByte = 0)
    {
        var server = new RawFileServer(head, contentLength, padding, stall, paddingByte);
        _rawServers.Add(server);

        return server;
    }

    private RetryAfterServer RetryAfter(int seconds)
    {
        var server = new RetryAfterServer(seconds);
        _rawServers.Add(server);

        return server;
    }

    /// <summary>
    /// A gallery whose list names the PDF file of template 1 at <paramref name="fileHost"/>.
    /// </summary>
    private string GalleryWithFileAt(string fileHost)
    {
        return Gallery(_ => StubResponse.Json(HttpStatusCode.OK,
            $$"""{"data":[{"id":1,"file_oform":[{"url":"{{fileHost}}/file.pdf","name":"form","ext":".pdf"}]}]}""")).Url;
    }

    private HttpStub Unreachable()
    {
        var stub = HttpStub.Unreachable();
        _stubs.Add(stub);

        return stub;
    }

    /// <param name="domain">The address of the gallery; <c>null</c> configures none.</param>
    /// <param name="clientTimeout">The timeout of the HTTP client, shorter than the manager's own to test it quickly.</param>
    /// <param name="primaryHandler">A handler in place of the network, for a fault the stubs cannot produce.</param>
    /// <param name="downloadTimeout">The download deadline in place of the manager's 30 seconds.</param>
    /// <param name="probeTimeout">The deadline of the availability check in place of the manager's 4 seconds.</param>
    /// <param name="loggerProvider">Where the logs of the services (Polly's among them) go; none by default.</param>
    /// <param name="registerPipelines">
    /// <c>false</c> registers the registry without the pipelines of the gallery, as a host that does not call
    /// AddTemplateGalleryPipelines has it: the deadlines are then the manager's own, and the download client has no
    /// retries and no breaker.
    /// </param>
    /// <param name="retryDelay">The first wait before a retry of the download; 50 ms by default, so retries stay quick.</param>
    /// <param name="attemptTimeout">The timeout of one request of the download in place of the manager's 10 seconds.</param>
    /// <param name="timeProvider">The clock of the availability check in place of the system's.</param>
    /// <param name="onlyWorkingClient">
    /// The only client name that reaches the network; every other client of the factory fails, except the gallery's own
    /// when <paramref name="registerPipelines"/> is <c>true</c>, which keeps the handler AddTemplateGalleryPipelines gives it.
    /// </param>
    private OFormRequestManager CreateManager(string? domain, TimeSpan? clientTimeout = null, IFusionCache? cache = null,
        Func<HttpMessageHandler>? primaryHandler = null, TimeSpan? downloadTimeout = null, TimeSpan? probeTimeout = null,
        ILoggerProvider? loggerProvider = null, bool registerPipelines = true, TimeSpan? retryDelay = null, TimeSpan? attemptTimeout = null,
        string? onlyWorkingClient = null, TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();

        if (registerPipelines)
        {
            services.AddTemplateGalleryPipelines(downloadTimeout, probeTimeout, retryDelay ?? TimeSpan.FromMilliseconds(50), attemptTimeout);
        }
        else
        {
            services.AddResiliencePipelineRegistry<string>();
        }

        if (loggerProvider is not null)
        {
            services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace).AddProvider(loggerProvider));
        }

        if (onlyWorkingClient is not null)
        {
            services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() =>
                new ThrowingHandler(new HttpRequestException("not the client the test allows"))));
        }

        // The check asks with the standard client without cookies, the download with the gallery's own
        foreach (var name in new[] { Options.DefaultName, OFormRequestManager.NoCookieHttpClientName, OFormRequestManager.HttpClientName })
        {
            var builder = services.AddHttpClient(name, client =>
            {
                if (clientTimeout is { } timeout)
                {
                    client.Timeout = timeout;
                }
            });

            if (primaryHandler is not null)
            {
                builder.ConfigurePrimaryHttpMessageHandler(primaryHandler);
            }
            else if (name == onlyWorkingClient)
            {
                builder.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler());
            }
        }

        var provider = services.BuildServiceProvider();
        _services.Add(provider);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["files:oform:domain"] = domain,
                ["files:oform:path"] = domain is null ? null : GalleryPath + "/"
            })
            .Build();

        var logger = loggerProvider is null ? NullLogger<OFormRequestManager>.Instance : provider.GetRequiredService<ILogger<OFormRequestManager>>();

        return new OFormRequestManager(logger, configuration, provider.GetRequiredService<IHttpClientFactory>(), cache ?? _cache,
            provider.GetRequiredService<ResiliencePipelineRegistry<string>>(), provider.GetRequiredService<IOptions<TelemetryOptions>>(), timeProvider);
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
    /// A backplane in memory shared by the node caches of a test. While <see cref="Hold"/> is set it keeps the
    /// messages until <see cref="DeliverHeldAsync"/>, so a test can let a notification arrive after a later write.
    /// </summary>
    private sealed class HeldBackplaneHub
    {
        private readonly List<BackplaneSubscriptionOptions> _subscribers = [];
        private readonly ConcurrentQueue<BackplaneMessage> _held = new();

        public bool Hold { get; set; }

        public IFusionCacheBackplane CreateBackplane()
        {
            return new Backplane(this);
        }

        public async Task DeliverHeldAsync()
        {
            while (_held.TryDequeue(out var message))
            {
                await SendAsync(message);
            }
        }

        private async ValueTask SendAsync(BackplaneMessage message)
        {
            BackplaneSubscriptionOptions[] subscribers;
            lock (_subscribers)
            {
                subscribers = [.. _subscribers];
            }

            foreach (var subscriber in subscribers.Where(subscriber => subscriber.CacheInstanceId != message.SourceId))
            {
                if (subscriber.IncomingMessageHandlerAsync is { } handleAsync)
                {
                    await handleAsync(message);
                }
                else
                {
                    subscriber.IncomingMessageHandler?.Invoke(message);
                }
            }
        }

        private sealed class Backplane(HeldBackplaneHub hub) : IFusionCacheBackplane
        {
            private BackplaneSubscriptionOptions? _options;

            public void Subscribe(BackplaneSubscriptionOptions options)
            {
                _options = options;
                lock (hub._subscribers)
                {
                    hub._subscribers.Add(options);
                }

                options.ConnectHandler?.Invoke(new BackplaneConnectionInfo(false));
            }

            public ValueTask SubscribeAsync(BackplaneSubscriptionOptions options)
            {
                Subscribe(options);

                return ValueTask.CompletedTask;
            }

            public void Unsubscribe()
            {
                lock (hub._subscribers)
                {
                    hub._subscribers.Remove(_options!);
                }
            }

            public ValueTask UnsubscribeAsync()
            {
                Unsubscribe();

                return ValueTask.CompletedTask;
            }

            public void Publish(BackplaneMessage message, FusionCacheEntryOptions options, CancellationToken token = default)
            {
                PublishAsync(message, options, token).AsTask().GetAwaiter().GetResult();
            }

            public ValueTask PublishAsync(BackplaneMessage message, FusionCacheEntryOptions options, CancellationToken token = default)
            {
                if (hub.Hold)
                {
                    hub._held.Enqueue(message);

                    return ValueTask.CompletedTask;
                }

                return hub.SendAsync(message);
            }
        }
    }

    /// <summary>
    /// A clock the test moves by hand.
    /// </summary>
    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }

        public void Advance(TimeSpan delta)
        {
            _now += delta;
        }
    }

    /// <summary>
    /// A logger provider that keeps every entry of every category with its level.
    /// </summary>
    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

        public IReadOnlyCollection<(LogLevel Level, string Message)> Entries => _entries.ToArray();

        public ILogger CreateLogger(string categoryName)
        {
            return new Logger(_entries);
        }

        public void Dispose()
        {
        }

        private sealed class Logger(ConcurrentQueue<(LogLevel Level, string Message)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                entries.Enqueue((logLevel, formatter(state, exception)));
            }
        }
    }

    /// <summary>
    /// A handler that throws the given exception on every request.
    /// </summary>
    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(exception);
        }
    }

    /// <summary>
    /// A server that writes its answers to the socket itself, for answers WireMock cannot give: its faults either answer
    /// in full or fail before the headers, and it always sends the length of the body.
    /// </summary>
    private abstract class RawServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _stopped = new();
        private readonly CancellationToken _stoppedToken;
        private int _callCount;

        protected RawServer()
        {
            // The token is taken before the source can be disposed: a connection may still be served after that
            _stoppedToken = _stopped.Token;
            _listener.Start();
            _ = Task.Run(AcceptAsync);
        }

        public string Url => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";

        public int CallCount => Volatile.Read(ref _callCount);

        public void Dispose()
        {
            _stopped.Cancel();
            _listener.Stop();
            _stopped.Dispose();
        }

        /// <summary>
        /// Writes the answer to a request whose headers have been read; <paramref name="stopped"/> ends with the test.
        /// </summary>
        protected abstract Task AnswerAsync(Socket connection, CancellationToken stopped);

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

                _ = Task.Run(() => ServeAsync(connection));
            }
        }

        private async Task ServeAsync(Socket connection)
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

                    await AnswerAsync(connection, _stoppedToken);

                    connection.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException)
                {
                    // The client gave up first; the connection is gone either way
                }
                catch (OperationCanceledException)
                {
                    // The test is over: the stalled connection closes with the server
                }
            }
        }
    }

    /// <summary>
    /// A server that answers 200 with a Content-Length longer than the body it writes, then closes the connection: the
    /// gallery, or a proxy of a closed network, dropping the connection while the body is read. With <c>stall</c> it
    /// keeps the connection open instead and writes nothing more, a body that never ends.
    /// </summary>
    private sealed class TruncatingServer(bool stall) : RawServer
    {
        protected override async Task AnswerAsync(Socket connection, CancellationToken stopped)
        {
            await connection.SendAsync(Encoding.ASCII.GetBytes(
                "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: 4096\r\n\r\n{\"data\":[{\"id\":1"));

            if (stall)
            {
                await Task.Delay(Timeout.Infinite, stopped);
            }
        }
    }

    /// <summary>
    /// A server that answers every request with 503 and a <c>Retry-After</c> of <paramref name="seconds"/>, a header
    /// the stubs of WireMock cannot send.
    /// </summary>
    private sealed class RetryAfterServer(int seconds) : RawServer
    {
        protected override async Task AnswerAsync(Socket connection, CancellationToken stopped)
        {
            await connection.SendAsync(Encoding.ASCII.GetBytes(
                $"HTTP/1.1 503 Service Unavailable\r\nRetry-After: {seconds}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"));
        }
    }

    /// <summary>
    /// A server that answers 200 with <paramref name="head"/> and then <paramref name="padding"/> bytes of
    /// <paramref name="paddingByte"/>, zero by default; a space keeps a JSON head valid JSON. With a
    /// <paramref name="contentLength"/> it announces that length whatever it writes; without one the body ends when the
    /// connection closes. With <paramref name="stall"/> it keeps the connection open after that and writes nothing more.
    /// </summary>
    private sealed class RawFileServer(string head, long? contentLength, long padding, bool stall, byte paddingByte = 0) : RawServer
    {
        protected override async Task AnswerAsync(Socket connection, CancellationToken stopped)
        {
            var length = contentLength is { } value ? $"Content-Length: {value}\r\n" : "Connection: close\r\n";
            await connection.SendAsync(Encoding.ASCII.GetBytes(
                $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\n{length}\r\n{head}"));

            var zeros = new byte[64 * 1024];
            Array.Fill(zeros, paddingByte);
            for (var left = padding; left > 0; left -= zeros.Length)
            {
                await connection.SendAsync(zeros.AsMemory(0, (int)Math.Min(left, zeros.Length)), stopped);
            }

            if (stall)
            {
                await Task.Delay(Timeout.Infinite, stopped);
            }
        }
    }
}
