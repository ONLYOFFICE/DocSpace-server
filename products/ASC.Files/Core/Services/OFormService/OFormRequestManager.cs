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

namespace ASC.Files.Core.Services.OFormService;

[Singleton]
public class OFormRequestManager(
    ILogger<OFormRequestManager> logger,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IFusionCache cache,
    ResiliencePipelineRegistry<string> resiliencePipelineRegistry,
    TelemetryOptionsAccessor telemetryOptions,
    TimeProvider timeProvider = null)
{
    public const string DownloadPipelineName = "oform-gallery-download";
    public const string ProbePipelineName = "oform-gallery-probe";

    /// <summary>
    /// The client the download asks the gallery with: every request of it gets an attempt timeout, retries and a circuit
    /// breaker per host (<see cref="ConfigureHttpClientPipeline"/>), all inside the download deadline. It is a named
    /// client of its own for that policy of failures, which no standard client has; like customHttpClientNoCookie it
    /// keeps no cookies, so no tenant gets the cookies another tenant's download was given. The availability check uses
    /// customHttpClientNoCookie: it is not retried, its answer is cached.
    /// </summary>
    /// <remarks>
    /// The breaker of a host is shared by every portal of the process. It opens on a failed request when at least 5
    /// requests to the host were made within 30 seconds and at least half of them failed: could not connect, timed
    /// out, or got 5xx, 408 or 429. While it is open (30 seconds), every creation
    /// from a template on that host answers 502 at once, while the availability check, which does not go through the
    /// breaker, may still answer a cached <c>true</c>. An error status of a template file is not a failure for the
    /// breaker, which counts it as an answered request: one broken template file must not fail the others on its host.
    /// The template list is asked per template too, yet its failures count: they are nearly always failures of the
    /// gallery itself, and a gallery that answers every list with 503 must open the circuit.
    /// </remarks>
    public const string HttpClientName = "templateGallery";

    public const string HttpClientPipelineName = "oform-gallery-http";

    /// <summary>
    /// The standard client the availability check asks with (AddStandardHttpClients): no retries, no cookies.
    /// </summary>
    public const string NoCookieHttpClientName = "customHttpClientNoCookie";

    /// <summary>
    /// How long one request of the download waits for the headers of its answer before it is retried.
    /// </summary>
    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The first wait before a retry of the download; the next waits grow from it.
    /// </summary>
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private const int MaxRetryAttempts = 2;

    /// <summary>
    /// How long a gallery template is downloaded, both requests together: the editor page waits for the request that
    /// creates the file, so a gallery that does not answer must fail it rather than hold it for the 100 seconds of the
    /// client.
    /// </summary>
    public static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long the availability check waits for the gallery: below the 5-second lock timeout of this process's cache,
    /// so the readers of this process that arrive meanwhile wait for this check instead of starting their own.
    /// </summary>
    public static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(4);

    /// <summary>
    /// The largest template file the download accepts: the file is held in memory, so a proxy that answers with a large
    /// or endless body fails the download instead of filling the memory for the whole deadline. It limits the file only;
    /// the answers with the template list are limited by <see cref="MaxListSize"/>.
    /// </summary>
    public const long MaxTemplateSize = 50L * 1024 * 1024;

    /// <summary>
    /// The largest answer with the template list that the download and the availability check read: both ask for one
    /// template, so a larger answer is the page of a proxy, not the list.
    /// </summary>
    public const long MaxListSize = 1024 * 1024;

    // The buffer of the file grows from at most this size, whatever length the gallery announces
    private const int MaxInitialBufferSize = 1024 * 1024;

    // A PDF reader looks for the PDF header in the first 1024 bytes of the file
    private const int SignatureLength = 1024;

    // The events of Polly whose level the gallery's pipelines set: the manager logs a failure of the gallery itself
    private const string TimeoutEventName = "OnTimeout";
    private const string RetryEventName = "OnRetry";
    private const string CircuitOpenedEventName = "OnCircuitOpened";
    private const string ExecutionAttemptEventName = "ExecutionAttempt";

    // Marks the request for a template file: the breaker takes its error status as an answered request, not a failure
    private static readonly HttpRequestOptionsKey<bool> _fileRequestKey = new("ASC.Files.TemplateGallery.FileRequest");

    private static readonly TimeSpan _availableDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// How long "unavailable" is kept, and how long a known "available" is kept after a failed check before the gallery
    /// is asked again.
    /// </summary>
    private static readonly TimeSpan _unavailableDuration = TimeSpan.FromMinutes(1);

    private static readonly TimeSpan _availabilityFailSafe = TimeSpan.FromDays(1);

    private static readonly byte[] _pdfSignature = "%PDF-"u8.ToArray();

    // DOCX, XLSX and PPTX are ZIP packages
    private static readonly byte[] _zipSignature = [0x50, 0x4B, 0x03, 0x04];

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly OFormSettings _configuration = configuration.GetSection("files:oform").Get<OFormSettings>();

    // The clock of the availability check; a test passes its own
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    // Checked once: a bad address is logged when the manager is created, not on every request
    private readonly Uri _domain = ParseDomain(configuration["files:oform:domain"], logger);

    private bool IsConfigured => !string.IsNullOrEmpty(_configuration?.Domain) && !string.IsNullOrEmpty(_configuration.Path);

    /// <summary>
    /// Downloads the file of a gallery template in the format <paramref name="ext"/>.
    /// </summary>
    /// <exception cref="ItemNotFoundException">
    /// No gallery is configured, or the gallery has no such template, or no file of this format for it.
    /// </exception>
    /// <exception cref="CustomHttpException">
    /// 502: the gallery address is not an http or https address, the gallery cannot be reached, dropped the connection,
    /// did not answer in time, answered with an error status, with a body that is not the template list or with a list
    /// larger than <see cref="MaxListSize"/>, has an open circuit (<see cref="HttpClientName"/>), named a file at an
    /// address that cannot be requested, or returned a file larger than <see cref="MaxTemplateSize"/>, a PDF without a
    /// PDF header in its first 1024 bytes or a DOCX, XLSX or PPTX file without a ZIP signature.
    /// </exception>
    /// <remarks>
    /// The download deadline (<see cref="DownloadTimeout"/>) is the outermost strategy: the retries of the client
    /// (<see cref="HttpClientName"/>) run inside it, so they never hold the editor longer than that.
    /// </remarks>
    public async Task<Stream> Get(int id, string ext)
    {
        if (!IsConfigured)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!TryGetDomain(out var domain))
        {
            throw GalleryUnavailable();
        }

        try
        {
            return await GetPipeline(DownloadPipelineName, DownloadTimeout).ExecuteAsync(token => DownloadAsync(domain, id, ext, token));
        }
        catch (Exception e) when (IsGalleryFailure(e))
        {
            logger.WarningOFormGalleryUnreachable(e);

            throw GalleryUnavailable();
        }
        catch (Exception e) when (e is not ItemNotFoundException and not CustomHttpException)
        {
            logger.ErrorOFormGalleryDownloadFailed(e);

            throw;
        }
    }

    private async ValueTask<Stream> DownloadAsync(Uri domain, int id, string ext, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        using var response = await httpClient.GetAsync($"{GetListUrl()}?filters[id][$eq]={id}&locale=*&populate[file_oform][fields]=url&populate[file_oform][fields]=name&populate[file_oform][fields]=ext&populate[file_oform][filters][url][$endsWith]={Uri.EscapeDataString(ext)}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.WarningOFormGalleryStatus(response.RequestMessage?.RequestUri, (int)response.StatusCode);

            throw GalleryUnavailable();
        }

        var data = await ReadListAsync(response, cancellationToken);
        if (data?.Data == null)
        {
            throw GalleryUnavailable();
        }

        var file = GetFile(data, ext) ?? throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);

        // A gallery that keeps its files itself (the local upload provider of Strapi) names them by a path from the root
        // of its host, where Strapi serves /uploads; the configured path of the list is not part of it. A gallery behind
        // a path prefix of a proxy must name its files by full addresses: a path from the root does not reach them and the
        // download answers 502
        if (!Uri.TryCreate(domain, file.Url, out var fileUri) || !IsHttp(fileUri))
        {
            logger.WarningOFormGalleryFileAddress(WithoutQuery(file.Url));

            throw GalleryUnavailable();
        }

        using var fileRequest = new HttpRequestMessage(HttpMethod.Get, fileUri);
        fileRequest.Options.Set(_fileRequestKey, true);

        using var streamResponse = await httpClient.SendAsync(fileRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!streamResponse.IsSuccessStatusCode)
        {
            logger.WarningOFormGalleryFileStatus(WithoutQuery(streamResponse.RequestMessage?.RequestUri), (int)streamResponse.StatusCode);

            throw GalleryUnavailable();
        }

        var length = streamResponse.Content.Headers.ContentLength;
        if (length > MaxTemplateSize)
        {
            logger.WarningOFormGalleryFileTooLarge(WithoutQuery(fileUri), MaxTemplateSize);

            throw GalleryUnavailable();
        }

        await using var body = await streamResponse.Content.ReadAsStreamAsync(cancellationToken);
        var head = new byte[SignatureLength];
        var read = await body.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, cancellationToken);

        // A proxy of a closed network answers 200 with a page of its own: such a page saved as the template opens as
        // a blank editor. The rest of the file is not read when its start is not the format's
        if (!HasFormatSignature(head.AsSpan(0, read), ext))
        {
            logger.WarningOFormGalleryNotForm(WithoutQuery(fileUri));

            throw GalleryUnavailable();
        }

        // The announced length only sizes the start of the buffer: a gallery that announces almost the limit and sends
        // a short body must not take that much memory for each request
        var stream = new MemoryStream(Math.Max(read, (int)Math.Min(length ?? read, MaxInitialBufferSize)));

        try
        {
            stream.Write(head, 0, read);

            if (!await CopyAtMostAsync(body, stream, MaxTemplateSize - read, cancellationToken))
            {
                logger.WarningOFormGalleryFileTooLarge(WithoutQuery(fileUri), MaxTemplateSize);

                throw GalleryUnavailable();
            }

            stream.Position = 0;
        }
        catch
        {
            await stream.DisposeAsync();

            throw;
        }

        return stream;
    }

    /// <summary>
    /// Copies the rest of <paramref name="source"/> unless it is longer than <paramref name="limit"/> bytes.
    /// </summary>
    /// <returns><c>false</c> when the source has more bytes than the limit; the copy then stops.</returns>
    private static async Task<bool> CopyAtMostAsync(Stream source, Stream destination, long limit, CancellationToken cancellationToken)
    {
        var buffer = new byte[81920];
        var copied = 0L;
        int read;

        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            copied += read;
            if (copied > limit)
            {
                return false;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Reads the answer with the template list unless it is larger than <see cref="MaxListSize"/>. Logs why it gives
    /// no list: an answer too large, or one without the list.
    /// </summary>
    /// <returns>The list, or <c>null</c> when the answer is not one.</returns>
    /// <exception cref="JsonException">The answer is not JSON, the page of a proxy.</exception>
    private async Task<OFromRequestData> ReadListAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var uri = response.RequestMessage?.RequestUri;

        if (response.Content.Headers.ContentLength > MaxListSize)
        {
            logger.WarningOFormGalleryAnswerTooLarge(uri, MaxListSize);

            return null;
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();

        if (!await CopyAtMostAsync(body, buffer, MaxListSize, cancellationToken))
        {
            logger.WarningOFormGalleryAnswerTooLarge(uri, MaxListSize);

            return null;
        }

        buffer.Position = 0;

        // The gallery answers in UTF-8; a body in another charset is not its list
        var data = await JsonSerializer.DeserializeAsync<OFromRequestData>(buffer, _options, cancellationToken);
        if (data?.Data == null)
        {
            logger.WarningOFormGalleryNoList(uri);
        }

        return data;
    }

    /// <summary>
    /// Whether the gallery is configured and answers with its template list. "Available" is cached per list URL,
    /// kept in the distributed cache for 10 minutes. A failed check of any kind keeps a known "available" by the
    /// fail-safe of the cache, so a short failure of the gallery or the network never hides a gallery that answered
    /// before: for a day after the gallery last answered, then a failed check means "unavailable". The time of that
    /// answer is kept in the cached value, since every failed check stores the entry again and would otherwise keep it
    /// for as long as the checks keep failing. Without a distributed cache (Redis off), a restart forgets it. With nothing known, a failed check means "unavailable" for
    /// up to 1 minute, kept by this process only, so one process that cannot reach the gallery never hides it from the
    /// others. A "true" another process writes ends it earlier, unless both checked at almost the same moment: the
    /// backplane drops a notification older than the local entry, and the "unavailable" stays for its minute. When the
    /// next check takes longer than 200 ms, a reader with an expired answer gets that answer while the check continues
    /// in the background; the check then caches what it gets, or, when it fails with "available" known, the fail-safe
    /// keeps that answer. A check past the cache's 5-second hard timeout with nothing known answers "unavailable" after
    /// that timeout, and its late answer still goes into the cache. The CMS probe times out after
    /// <see cref="ProbeTimeout"/>. Distributed cache access and waiting for its local lock can add to the time. The check
    /// does not go through the circuit breaker of the download client (<see cref="HttpClientName"/>), so it may answer
    /// a cached "available" while the downloads fail at once on an open circuit.
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        if (!IsConfigured)
        {
            return false;
        }

        try
        {
            var answer = await cache.GetOrSetAsync<AvailabilityAnswer>(GetAvailabilityCacheKey(GetListUrl()), async (context, cancellationToken) =>
            {
                var now = _timeProvider.GetUtcNow();
                if (await ProbeAsync(cancellationToken))
                {
                    return new AvailabilityAnswer(true, now);
                }

                // A failed check is no answer: the fail-safe keeps a known "available" and asks again after a minute,
                // until a day has passed since the gallery last answered
                if (context.HasStaleValue && context.StaleValue.Value is { Available: true } known && now - known.AnsweredAt < _availabilityFailSafe)
                {
                    return context.Fail("The template gallery did not answer its check");
                }

                // "Unavailable" stays in this process: it never replaces the answer other processes got
                context.Options.Duration = _unavailableDuration;
                context.Options.SkipDistributedCacheWrite = true;
                context.Options.SkipBackplaneNotifications = true;

                return new AvailabilityAnswer(false, now);
            }, entry => entry
                .SetDuration(_availableDuration)
                .SetFailSafe(true, _availabilityFailSafe, _unavailableDuration)
                .SetFactoryTimeouts(TimeSpan.FromMilliseconds(200)));

            return answer.Available;
        }
        catch (SyntheticTimeoutException)
        {
            // The check ran past the cache's hard timeout with no answer known: no answer is "unavailable" for this
            // reader, while the check completes in the background and caches what it gets
            return false;
        }
    }

    /// <summary>
    /// The key of the availability answer for the list URL, scoped to the installation.
    /// </summary>
    public static string GetAvailabilityCacheKey(string listUrl)
    {
        var normalizedUrl = Uri.TryCreate(listUrl, UriKind.Absolute, out var uri) ? uri.AbsoluteUri : listUrl;
        var urlHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedUrl)));

        return Tenant.DefaultTenant + "oformgalleryanswer" + urlHash;
    }

    /// <summary>
    /// The cached answer of the availability check: whether the gallery answered with its template list, and when the
    /// check got this answer. The key holds this record, not the bare flag of the earlier versions, so an entry they
    /// wrote is never read as one.
    /// </summary>
    public sealed record AvailabilityAnswer(bool Available, DateTimeOffset AnsweredAt);

    /// <summary>
    /// Asks the gallery for one template of its list. Never throws: a gallery that does not answer in time, answers
    /// with an error status or with a body that is not the list (the page of a proxy) fails the check, and so does an
    /// exception that is not a failure of the gallery, logged as an error.
    /// </summary>
    private async Task<bool> ProbeAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await GetPipeline(ProbePipelineName, ProbeTimeout).ExecuteAsync(AskForListAsync, cancellationToken);
        }
        catch (Exception e) when (IsGalleryFailure(e))
        {
            logger.WarningOFormGalleryProbeFailed(e);

            return false;
        }
        catch (Exception e)
        {
            // Not a failure of the gallery, yet the check has to answer: with nothing known, a 500 of the endpoint would
            // replace "unavailable", and a known "available" is kept by the fail-safe either way
            logger.ErrorOFormGalleryProbeError(e);

            return false;
        }
    }

    private async ValueTask<bool> AskForListAsync(CancellationToken cancellationToken)
    {
        if (!TryGetDomain(out _))
        {
            return false;
        }

        // Not the download client: the check is not retried, its deadline is short and its answer is cached. Without
        // cookies, as the download: the default client would keep the gallery's cookies for every caller of the process
        var httpClient = httpClientFactory.CreateClient(NoCookieHttpClientName);
        using var response = await httpClient.GetAsync($"{GetListUrl()}?pagination[pageSize]=1&fields[0]=updatedAt", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.WarningOFormGalleryProbeStatus(response.RequestMessage?.RequestUri, (int)response.StatusCode);

            return false;
        }

        var data = await ReadListAsync(response, cancellationToken);

        return data?.Data != null;
    }

    /// <summary>
    /// The configured domain of the gallery, when it is an absolute http or https address.
    /// </summary>
    private bool TryGetDomain(out Uri domain)
    {
        domain = _domain;

        return domain is not null;
    }

    /// <summary>
    /// The configured domain as an absolute http or https address; otherwise logs it, once for the manager.
    /// </summary>
    /// <returns><c>null</c> when no domain is configured or it is not such an address.</returns>
    private static Uri ParseDomain(string value, ILogger<OFormRequestManager> logger)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var domain) && IsHttp(domain))
        {
            return domain;
        }

        logger.WarningOFormGalleryAddress(value);

        return null;
    }

    /// <summary>
    /// The address of a gallery file without its query, fragment and user info, for the log: a file kept in a private
    /// bucket is named by a signed address whose signature must not stay in the log.
    /// </summary>
    private static string WithoutQuery(Uri uri)
    {
        return uri?.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped);
    }

    /// <summary>
    /// A gallery file address that is not an http or https address, for the log, without its query, fragment and user
    /// info. It may not parse as a URI, so the parts are cut from the text.
    /// </summary>
    private static string WithoutQuery(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        var end = url.IndexOfAny(['?', '#']);
        if (end >= 0)
        {
            url = url[..end];
        }

        var scheme = url.IndexOf("://", StringComparison.Ordinal);
        var start = scheme < 0 ? 0 : scheme + 3;
        var slash = url.IndexOf('/', start);
        var authorityEnd = slash < 0 ? url.Length : slash;
        var at = authorityEnd > start ? url.LastIndexOf('@', authorityEnd - 1, authorityEnd - start) : -1;

        return at < 0 ? url : url[..start] + url[(at + 1)..];
    }

    private static bool IsHttp(Uri uri)
    {
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    /// <summary>
    /// The pipeline registered under <paramref name="name"/> (<see cref="OFormRequestManagerExtension.AddTemplateGalleryPipelines"/>),
    /// or, in a host that registers none, one with the default <paramref name="timeout"/> configured the same way.
    /// GetOrAddPipeline alone would not do: it never looks at the registered pipelines. The registry itself comes with
    /// the standard HTTP clients every host registers (AddStandardHttpClients).
    /// </summary>
    private ResiliencePipeline GetPipeline(string name, TimeSpan timeout)
    {
        return resiliencePipelineRegistry.TryGetPipeline(name, out var pipeline)
            ? pipeline
            : resiliencePipelineRegistry.GetOrAddPipeline(name, builder => ConfigurePipeline(builder, telemetryOptions.Value, timeout));
    }

    /// <summary>
    /// A deadline of <paramref name="timeout"/>, with the levels of <see cref="ConfigureTelemetry{TBuilder}"/>.
    /// </summary>
    internal static void ConfigurePipeline(ResiliencePipelineBuilder builder, TelemetryOptions telemetry, TimeSpan timeout)
    {
        ConfigureTelemetry(builder, telemetry);
        builder.AddTimeout(timeout);
    }

    /// <summary>
    /// The strategies of every request of the download client, the outer one first: retries of a transient failure
    /// (an exception of the network before the headers of the answer, a 5xx, 408 or 429, an attempt past
    /// <paramref name="attemptTimeout"/>), a circuit breaker that fails the requests at once while the gallery keeps
    /// failing, and the timeout of one attempt. The answers are read by their headers, so a connection dropped while the
    /// body is read is past these strategies: it is not retried and fails the download at once. A <c>Retry-After</c> of
    /// the answer is ignored: a proxy or a rate limiter asking for a minute would hold the editor until the download
    /// deadline. They all run inside the download deadline, which ends the retries with the download.
    /// </summary>
    internal static void ConfigureHttpClientPipeline(ResiliencePipelineBuilder<HttpResponseMessage> builder, TelemetryOptions telemetry, TimeSpan retryDelay,
        TimeSpan attemptTimeout)
    {
        ConfigureTelemetry(builder, telemetry);
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = MaxRetryAttempts,
            Delay = retryDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldRetryAfterHeader = false
        });
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30),
            ShouldHandle = args => ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(args.Outcome) && !IsFileAnswer(args.Outcome.Result))
        });
        builder.AddTimeout(attemptTimeout);
    }

    /// <summary>
    /// Whether this is an answer, with whatever status, to the request for a template file: the host answered, so the
    /// error is of that one file, not of the host.
    /// </summary>
    private static bool IsFileAnswer(HttpResponseMessage response)
    {
        return response?.RequestMessage?.Options.TryGetValue(_fileRequestKey, out var isFile) == true && isFile;
    }

    /// <summary>
    /// The levels the telemetry of Polly reports the gallery's events at, set here for all of its pipelines: a timeout
    /// and a failed attempt at Debug and a retry at Information, not at Error and Warning, since the manager logs the
    /// failure of the gallery itself, as a warning; an opened circuit at Warning, not at Error.
    /// </summary>
    private static void ConfigureTelemetry<TBuilder>(TBuilder builder, TelemetryOptions telemetry) where TBuilder : ResiliencePipelineBuilderBase
    {
        var severityProvider = telemetry.SeverityProvider;

        builder.ConfigureTelemetry(new TelemetryOptions(telemetry)
        {
            SeverityProvider = args => args.Event.EventName switch
            {
                TimeoutEventName or ExecutionAttemptEventName => ResilienceEventSeverity.Debug,
                RetryEventName => ResilienceEventSeverity.Information,
                CircuitOpenedEventName => ResilienceEventSeverity.Warning,
                _ => severityProvider?.Invoke(args) ?? args.Event.Severity
            }
        });
    }

    /// <summary>
    /// Whether the exception is a failure of the gallery or of the way to it rather than of this code. The requests get
    /// no token of an HTTP request: the download passes none, and the check passes the token of the cache factory, which
    /// <see cref="IsAvailableAsync"/> starts without one. So a TimeoutRejectedException is the deadline of the pipeline
    /// or of the last attempt, and a cancellation is the timeout of the client. A connection dropped while the body is
    /// read comes as an IOException (HttpIOException), since the body is read from the stream of the answer; an
    /// HttpRequestException is a failure before the headers. A JsonException is an answer that is not JSON. A BrokenCircuitException is a gallery
    /// host whose circuit is open, so the client does not ask it. The addresses are checked before the requests, so
    /// anything else, a NotSupportedException of the serializer among them, is a failure of this code.
    /// </summary>
    private static bool IsGalleryFailure(Exception e)
    {
        return e is TimeoutRejectedException or HttpRequestException or IOException or JsonException or OperationCanceledException or BrokenCircuitException;
    }

    private string GetListUrl()
    {
        return $"{_configuration.Domain.TrimEnd('/')}/{_configuration.Path.Trim('/')}";
    }

    private static CustomHttpException GalleryUnavailable()
    {
        return new CustomHttpException(HttpStatusCode.BadGateway, FilesCommonResource.ErrorMessage_TemplateGalleryUnavailable);
    }

    /// <summary>
    /// Whether the file starts as a file of its format does: a PDF has its header in the first 1024 bytes, where readers
    /// look for it, and a DOCX, XLSX or PPTX starts with the ZIP signature. Other formats are not checked.
    /// </summary>
    private static bool HasFormatSignature(ReadOnlySpan<byte> head, string ext)
    {
        return ext.ToLowerInvariant() switch
        {
            ".pdf" => head.IndexOf(_pdfSignature) >= 0,
            ".docx" or ".xlsx" or ".pptx" => head.StartsWith(_zipSignature),
            _ => true
        };
    }

    private static OFromFileAttribute GetFile(OFromRequestData data, string ext)
    {
        var form = data?.Data?.FirstOrDefault();
        if (form == null)
        {
            return null;
        }

        // Strapi v5 keeps the files right on the form, v4 wraps them into
        // "attributes" and every file into a "data" envelope of its own.
        var files = form.Files ?? form.Attributes?.File?.Data?.Select(f => f.Attributes);

        return files?.FirstOrDefault(f => f?.Ext == ext);
    }
}

public static class OFormRequestManagerExtension
{
    /// <summary>
    /// Registers the resilience of the template gallery: the deadlines of the download of a template and of the
    /// availability check, and the client the download asks the gallery with
    /// (<see cref="OFormRequestManager.HttpClientName"/>: retries, a circuit breaker per host and an attempt timeout).
    /// A host that does not call this gets the same deadlines, <see cref="OFormRequestManager.DownloadTimeout"/> and
    /// <see cref="OFormRequestManager.ProbeTimeout"/>, as long as it has the pipeline registry, which every host gets
    /// from AddStandardHttpClients, but its download client is the default one, with no retries and no breaker: the
    /// factory does not refuse a name never registered. Files.Server, and the Monolith through it, call this. The
    /// parameters are for tests, which cannot wait that long.
    /// </summary>
    public static IServiceCollection AddTemplateGalleryPipelines(this IServiceCollection services, TimeSpan? downloadTimeout = null, TimeSpan? probeTimeout = null,
        TimeSpan? retryDelay = null, TimeSpan? attemptTimeout = null)
    {
        services.AddResiliencePipeline(OFormRequestManager.DownloadPipelineName, (builder, context) =>
            OFormRequestManager.ConfigurePipeline(builder, GetTelemetryOptions(context.ServiceProvider), downloadTimeout ?? OFormRequestManager.DownloadTimeout));
        services.AddResiliencePipeline(OFormRequestManager.ProbePipelineName, (builder, context) =>
            OFormRequestManager.ConfigurePipeline(builder, GetTelemetryOptions(context.ServiceProvider), probeTimeout ?? OFormRequestManager.ProbeTimeout));

        // A list and its files may come from different hosts (a CDN): each host gets a breaker of its own. The cookies a
        // gallery sets would otherwise be shared by all tenants for the life of the handler (bug 83399)
        services.AddHttpClient(OFormRequestManager.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler { UseCookies = false })
            .AddResilienceHandler(OFormRequestManager.HttpClientPipelineName, (builder, context) =>
                OFormRequestManager.ConfigureHttpClientPipeline(builder, GetTelemetryOptions(context.ServiceProvider), retryDelay ?? OFormRequestManager.RetryDelay,
                    attemptTimeout ?? OFormRequestManager.AttemptTimeout))
            .SelectPipelineByAuthority();

        return services;
    }

    private static TelemetryOptions GetTelemetryOptions(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<TelemetryOptionsAccessor>().Value;
    }
}
