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

namespace ASC.FederatedLogin.Helpers;

[Singleton]
public class RequestHelper(IHttpClientFactory httpClientFactory)
{
    public string PerformRequest(string uri, string contentType = "", string method = "GET", string body = "", Dictionary<string, string> headers = null, int timeout = 30000)
    {
        if (string.IsNullOrEmpty(uri))
        {
            throw new ArgumentNullException(nameof(uri));
        }

        using var request = new HttpRequestMessage
        {
            RequestUri = new Uri(uri),
            Method = new HttpMethod(method)
        };

        using var httpClient = httpClientFactory.CreateClient("customHttpClientNoCookie");
        httpClient.Timeout = TimeSpan.FromMilliseconds(timeout);

        if (headers != null)
        {
            foreach (var key in headers.Keys)
            {
                request.Headers.Add(key, headers[key]);
            }
        }

        var bytes = Encoding.UTF8.GetBytes(body ?? "");
        if (request.Method != HttpMethod.Get && bytes.Length > 0)
        {
            request.Content = new ByteArrayContent(bytes, 0, bytes.Length);
            if (!string.IsNullOrEmpty(contentType))
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }
        }

        using var response = httpClient.Send(request);
        using var stream = response.Content.ReadAsStream();
        using var readStream = new StreamReader(stream);

        return readStream.ReadToEnd();
    }
}