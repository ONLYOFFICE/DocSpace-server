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
    IFusionCache cache)
{
    /// <summary>
    /// How long a form is downloaded, both requests together: the editor page waits for the request that creates the
    /// file, so a gallery that does not answer must fail it rather than hold it for the 100 seconds of the client.
    /// </summary>
    private static readonly TimeSpan _downloadTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan _probeTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How long an answer of the availability check is reused: a gallery that did not answer is asked again sooner,
    /// so a short failure of the network does not hide it for long.
    /// </summary>
    private static readonly TimeSpan _availableDuration = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan _unavailableDuration = TimeSpan.FromMinutes(1);

    private static readonly byte[] _pdfSignature = "%PDF-"u8.ToArray();

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly OFormSettings _configuration = configuration.GetSection("files:oform").Get<OFormSettings>();

    private bool IsConfigured => !string.IsNullOrEmpty(_configuration?.Domain) && !string.IsNullOrEmpty(_configuration.Path);

    /// <summary>
    /// Downloads the file of a gallery form in the format <paramref name="ext"/>.
    /// </summary>
    /// <exception cref="ItemNotFoundException">The gallery has no such form, or no file of this format for it.</exception>
    /// <exception cref="CustomHttpException">
    /// 502: the gallery did not answer in time, answered with an error status or with a body that is not the form list,
    /// or gave a file that is not a PDF for a PDF form.
    /// </exception>
    public async Task<Stream> Get(int id, string ext)
    {
        if (!IsConfigured)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        using var cts = new CancellationTokenSource(_downloadTimeout);

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync($"{GetListUrl()}?filters[id][$eq]={id}&locale=*&populate[file_oform][fields]=url&populate[file_oform][fields]=name&populate[file_oform][fields]=ext&populate[file_oform][filters][url][$endsWith]={ext}", cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.WarningOFormGalleryStatus(response.RequestMessage?.RequestUri, (int)response.StatusCode);

                throw GalleryUnavailable();
            }

            var data = JsonSerializer.Deserialize<OFromRequestData>(await response.Content.ReadAsStringAsync(cts.Token), _options);

            var file = GetFile(data, ext) ?? throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);

            using var streamResponse = await httpClient.GetAsync(file.Url, cts.Token);
            if (!streamResponse.IsSuccessStatusCode)
            {
                logger.WarningOFormGalleryStatus(streamResponse.RequestMessage?.RequestUri, (int)streamResponse.StatusCode);

                throw GalleryUnavailable();
            }

            var stream = new MemoryStream();
            await streamResponse.Content.CopyToAsync(stream, cts.Token);
            stream.Position = 0;

            // A proxy of a closed network answers 200 with a page of its own: such a page saved as the form opens as a
            // blank editor
            if (FileUtility.GetFileTypeByExtention(ext) == FileType.Pdf && !await HasPdfSignatureAsync(stream))
            {
                await stream.DisposeAsync();
                logger.WarningOFormGalleryNotPdf(file.Url);

                throw GalleryUnavailable();
            }

            return stream;
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or OperationCanceledException)
        {
            // No token of a caller reaches here: a cancellation is a timeout, of this download or of the client
            logger.WarningOFormGalleryUnreachable(e);

            throw GalleryUnavailable();
        }
    }

    /// <summary>
    /// Whether the gallery is configured and answered its last check with its form list. The answer is one per
    /// installation, kept in the distributed cache: a reader with none known waits at most 2 seconds and then gets
    /// <c>true</c> while the check goes on in the background; an expired answer is still returned while the next check
    /// runs.
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        if (!IsConfigured)
        {
            return false;
        }

        try
        {
            return await cache.GetOrSetAsync<bool>(GetAvailabilityCacheKey(), async (context, cancellationToken) =>
            {
                var available = await ProbeAsync(cancellationToken);
                if (!available)
                {
                    context.Options.Duration = _unavailableDuration;
                }

                return available;
            }, entry => entry
                .SetDuration(_availableDuration)
                .SetFailSafe(true)
                .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2)));
        }
        catch (SyntheticTimeoutException)
        {
            // The first check of the installation is still running and stores its answer when it ends
            return true;
        }
    }

    /// <summary>
    /// The key of the availability answer: one per installation, so it carries the installation's own tenant.
    /// </summary>
    public static string GetAvailabilityCacheKey()
    {
        return Tenant.DefaultTenant + "oformgallery";
    }

    /// <summary>
    /// Asks the gallery for one form of its list. Never throws: a gallery that does not answer in time, answers with an
    /// error status or with a body that is not the list (the page of a proxy) is not available.
    /// </summary>
    private async Task<bool> ProbeAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_probeTimeout);

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync($"{GetListUrl()}?pagination[pageSize]=1&fields[0]=updatedAt", cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.DebugOFormGalleryProbeStatus(response.RequestMessage?.RequestUri, (int)response.StatusCode);

                return false;
            }

            var data = JsonSerializer.Deserialize<OFromRequestData>(await response.Content.ReadAsStringAsync(cts.Token), _options);

            return data?.Data != null;
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or OperationCanceledException)
        {
            logger.DebugOFormGalleryProbeFailed(e);

            return false;
        }
    }

    private string GetListUrl()
    {
        return $"{_configuration.Domain.TrimEnd('/')}/{_configuration.Path.Trim('/')}";
    }

    private static CustomHttpException GalleryUnavailable()
    {
        return new CustomHttpException(HttpStatusCode.BadGateway, FilesCommonResource.ErrorMessage_FormGalleryUnavailable);
    }

    /// <summary>
    /// Whether the PDF header is in the first 1024 bytes, where readers look for it.
    /// </summary>
    private static async Task<bool> HasPdfSignatureAsync(Stream stream)
    {
        var buffer = new byte[1024];
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false);
        stream.Position = 0;

        return buffer.AsSpan(0, read).IndexOf(_pdfSignature) >= 0;
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
