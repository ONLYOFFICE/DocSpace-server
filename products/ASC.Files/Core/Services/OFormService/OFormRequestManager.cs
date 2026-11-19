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
    IHttpClientFactory httpClientFactory)
{
    private readonly OFormSettings _configuration = configuration.GetSection("files:oform").Get<OFormSettings>();

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Stream> Get(int id, string ext)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient();
            using var response = await SendAsync(httpClient, $"{_configuration.Domain.TrimEnd('/')}/{_configuration.Path.Trim('/')}?filters[id][$eq]={id}&locale=*&populate[file_oform][fields]=url&populate[file_oform][fields]=name&populate[file_oform][fields]=ext&populate[file_oform][filters][url][$endsWith]={ext}");

            OFromRequestData data;
            try
            {
                data = JsonSerializer.Deserialize<OFromRequestData>(await response.Content.ReadAsStringAsync(), _options);
            }
            catch (JsonException e)
            {
                throw GalleryFailed(e.Message, e);
            }

            var file = GetFile(data, ext) ?? throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);

            // Not disposed here: the caller reads and disposes the content stream.
            var streamResponse = await SendAsync(httpClient, file.Url);
            return await streamResponse.Content.ReadAsStreamAsync();
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            throw;
        }
    }

    /// <summary>
    /// The form gallery is an upstream service: when it cannot be reached, times out or answers with an error, the
    /// request fails with 502, as a failing document server does. These were a 500 (an unhandled HttpRequestException,
    /// TaskCanceledException or JsonException), and an error answer to the file download was even saved as the form.
    /// </summary>
    private static async Task<HttpResponseMessage> SendAsync(HttpClient httpClient, string url)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            // No token is passed, so a cancelled request is the client's timeout, not the caller giving up.
            throw GalleryFailed(e.Message, e);
        }

        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            response.Dispose();

            throw GalleryFailed(status.ToString(), null);
        }

        return response;
    }

    private static CustomHttpException GalleryFailed(string reason, Exception inner)
    {
        return new CustomHttpException(HttpStatusCode.BadGateway, $"The form gallery failed: {reason}", inner);
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
