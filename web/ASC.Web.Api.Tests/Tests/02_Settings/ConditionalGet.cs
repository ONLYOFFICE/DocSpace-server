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

namespace ASC.Web.Api.Tests.Tests._02_Settings;

/// <summary>
/// Conditional reads of the settings endpoints that answer 304 (<c>If-Modified-Since</c>, <c>If-None-Match</c>).
/// Raw HTTP on purpose: the server's <c>Last-Modified</c> is <c>DateTime.ToString(InvariantCulture)</c>
/// (<c>11/11/2026 10:00:00</c>), not an HTTP date, and its <c>ETag</c> is an unquoted <c>W/&lt;md5&gt;</c>. The
/// generated client adds request headers with validation, so it cannot send either value back; a browser echoes
/// them verbatim, which is what these requests do.
/// </summary>
internal static class ConditionalGet
{
    public static async Task<HttpResponseMessage> SendAsync(HttpClient client, string path, string header, string value)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation(header, value);

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static async Task<HttpResponseMessage> GetAsync(HttpClient client, string path)
    {
        return await client.GetAsync(path, TestContext.Current.CancellationToken);
    }

    /// <summary>The raw value of a response header, wherever .NET filed it (Last-Modified is a content header).</summary>
    public static string? Header(HttpResponseMessage response, string name)
    {
        if (response.Headers.NonValidated.TryGetValues(name, out var values))
        {
            return values.ToString();
        }

        return response.Content.Headers.NonValidated.TryGetValues(name, out var contentValues)
            ? contentValues.ToString()
            : null;
    }
}
