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
    ResiliencePipelineRegistry<string> resiliencePipelineRegistry)
{
    public const string DownloadPipelineName = "oform-gallery-download";
    public const string ProbePipelineName = "oform-gallery-probe";

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

    private bool IsConfigured => !string.IsNullOrEmpty(_configuration?.Domain) && !string.IsNullOrEmpty(_configuration.Path);

    /// <summary>
    /// Downloads the file of a gallery template in the format <paramref name="ext"/>.
    /// </summary>
    /// <exception cref="ItemNotFoundException">The gallery has no such template, or no file of this format for it.</exception>
    /// <exception cref="CustomHttpException">
    /// 502: the gallery did not answer in time, answered with an error status or with a body that is not the template
    /// list, named a file at an address that cannot be requested, or returned a PDF without a PDF header or a DOCX, XLSX
    /// or PPTX file without a ZIP signature.
    /// </exception>
    public async Task<Stream> Get(int id, string ext)
    {
        if (!IsConfigured)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        try
        {
            return await GetPipeline(DownloadPipelineName, DownloadTimeout).ExecuteAsync(token => DownloadAsync(id, ext, token));
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

    private async ValueTask<Stream> DownloadAsync(int id, string ext, CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync($"{GetListUrl()}?filters[id][$eq]={id}&locale=*&populate[file_oform][fields]=url&populate[file_oform][fields]=name&populate[file_oform][fields]=ext&populate[file_oform][filters][url][$endsWith]={Uri.EscapeDataString(ext)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.WarningOFormGalleryStatus(response.RequestMessage?.RequestUri, (int)response.StatusCode);

            throw GalleryUnavailable();
        }

        var data = JsonSerializer.Deserialize<OFromRequestData>(await response.Content.ReadAsStringAsync(cancellationToken), _options);
        if (data?.Data == null)
        {
            logger.WarningOFormGalleryNoList(response.RequestMessage?.RequestUri);

            throw GalleryUnavailable();
        }

        var file = GetFile(data, ext) ?? throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);

        // A gallery that keeps its files itself (the local upload provider of Strapi) names them by a path on its host
        if (!Uri.TryCreate(new Uri(_configuration.Domain), file.Url, out var fileUri))
        {
            logger.WarningOFormGalleryFileAddress(file.Url);

            throw GalleryUnavailable();
        }

        using var streamResponse = await httpClient.GetAsync(fileUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!streamResponse.IsSuccessStatusCode)
        {
            logger.WarningOFormGalleryStatus(streamResponse.RequestMessage?.RequestUri, (int)streamResponse.StatusCode);

            throw GalleryUnavailable();
        }

        var stream = new MemoryStream();

        try
        {
            await streamResponse.Content.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;

            // A proxy of a closed network answers 200 with a page of its own: such a page saved as the template opens as
            // a blank editor
            if (!await HasFormatSignatureAsync(stream, ext))
            {
                logger.WarningOFormGalleryNotForm(fileUri);

                throw GalleryUnavailable();
            }
        }
        catch
        {
            await stream.DisposeAsync();

            throw;
        }

        return stream;
    }

    /// <summary>
    /// Whether the gallery is configured and answers with its template list. "Available" is cached per list URL,
    /// kept in the distributed cache for 10 minutes. A failed check of any kind keeps a known "available" by the
    /// fail-safe of the cache, so a short failure of the gallery or the network never hides a gallery that answered
    /// before: for a day after the last answer. Repeated failed checks, or a process that copies the entry from the
    /// distributed cache to its memory (a restart, an eviction), may keep it longer; no test pins how much. Without a
    /// distributed cache (Redis off), a restart forgets it. With nothing known, a failed check means "unavailable" for
    /// up to 1 minute, kept by this process only, so one process that cannot reach the gallery never hides it from the
    /// others. A "true" another process writes ends it earlier, unless both checked at almost the same moment: the
    /// backplane drops a notification older than the local entry, and the "unavailable" stays for its minute. When the
    /// next check takes longer than 200 ms, a reader with an expired answer gets that answer while the check continues
    /// in the background; the check then caches what it gets, or, when it fails with "available" known, the fail-safe
    /// keeps that answer. A check past the cache's 5-second hard timeout with nothing known answers "unavailable" after
    /// that timeout, and its late answer still goes into the cache. The CMS probe times out after
    /// <see cref="ProbeTimeout"/>. Distributed cache access and waiting for its local lock can add to the time.
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        if (!IsConfigured)
        {
            return false;
        }

        try
        {
            return await cache.GetOrSetAsync<bool>(GetAvailabilityCacheKey(GetListUrl()),async (context, cancellationToken) =>
            {
                if (await ProbeAsync(cancellationToken))
                {
                    return true;
                }

                // A failed check is no answer: the fail-safe keeps a known "available" and asks again after a minute
                if (context.HasStaleValue && context.StaleValue.Value)
                {
                    throw new InvalidOperationException("The template gallery did not answer its check");
                }

                // "Unavailable" stays in this process: it never replaces the answer other processes got
                context.Options.Duration = _unavailableDuration;
                context.Options.SkipDistributedCacheWrite = true;
                context.Options.SkipBackplaneNotifications = true;

                return false;
            }, entry => entry
                .SetDuration(_availableDuration)
                .SetFailSafe(true, _availabilityFailSafe, _unavailableDuration)
                .SetFactoryTimeouts(TimeSpan.FromMilliseconds(200)));
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

        return Tenant.DefaultTenant + "oformgallery" + urlHash;
    }

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
        var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync($"{GetListUrl()}?pagination[pageSize]=1&fields[0]=updatedAt", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.WarningOFormGalleryProbeStatus(response.RequestMessage?.RequestUri, (int)response.StatusCode);

            return false;
        }

        var data = JsonSerializer.Deserialize<OFromRequestData>(await response.Content.ReadAsStringAsync(cancellationToken), _options);
        if (data?.Data == null)
        {
            logger.WarningOFormGalleryNoList(response.RequestMessage?.RequestUri);

            return false;
        }

        return true;
    }

    /// <summary>
    /// The pipeline registered under <paramref name="name"/> (<see cref="OFormRequestManagerExtension.AddTemplateGalleryPipelines"/>),
    /// or, in a host that registers none, one with the default <paramref name="timeout"/>. GetOrAddPipeline alone would
    /// not do: it never looks at the registered pipelines.
    /// </summary>
    private ResiliencePipeline GetPipeline(string name, TimeSpan timeout)
    {
        return resiliencePipelineRegistry.TryGetPipeline(name, out var pipeline)
            ? pipeline
            : resiliencePipelineRegistry.GetOrAddPipeline(name, builder => builder.AddTimeout(timeout));
    }

    /// <summary>
    /// Whether the exception is a failure of the gallery or of the way to it rather than of this code. No token of a
    /// caller reaches the requests: a TimeoutRejectedException is the deadline of the pipeline, and a cancellation is the
    /// timeout of the client. A connection dropped
    /// while the body is read comes as an HttpRequestException from HttpContent; IOException covers a read the runtime
    /// does not wrap (HttpIOException). An InvalidOperationException is a charset of the
    /// answer the runtime cannot decode, or a gallery address without a scheme. UriFormatException and
    /// NotSupportedException cover malformed addresses and unsupported schemes, in the configuration or in the file
    /// address the gallery gave.
    /// </summary>
    private static bool IsGalleryFailure(Exception e)
    {
        return e is TimeoutRejectedException or HttpRequestException or IOException or JsonException or OperationCanceledException or UriFormatException or NotSupportedException
            or InvalidOperationException and not ObjectDisposedException;
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
    private static async Task<bool> HasFormatSignatureAsync(Stream stream, string ext)
    {
        var buffer = new byte[1024];
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false);
        stream.Position = 0;
        var head = buffer.AsSpan(0, read);

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
    /// Registers the deadlines of the template gallery: the download of a template and the availability check. A host
    /// that does not call this gets the same deadlines, <see cref="OFormRequestManager.DownloadTimeout"/> and
    /// <see cref="OFormRequestManager.ProbeTimeout"/>; the parameters are for tests, which cannot wait that long.
    /// </summary>
    public static IServiceCollection AddTemplateGalleryPipelines(this IServiceCollection services, TimeSpan? downloadTimeout = null, TimeSpan? probeTimeout = null)
    {
        services.AddResiliencePipeline(OFormRequestManager.DownloadPipelineName,
            builder => builder.AddTimeout(downloadTimeout ?? OFormRequestManager.DownloadTimeout));
        services.AddResiliencePipeline(OFormRequestManager.ProbePipelineName,
            builder => builder.AddTimeout(probeTimeout ?? OFormRequestManager.ProbeTimeout));

        return services;
    }
}
