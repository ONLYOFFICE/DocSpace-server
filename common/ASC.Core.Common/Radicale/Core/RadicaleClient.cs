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

namespace ASC.Common.Radicale;

[Singleton]
public class RadicaleClient(ILogger<RadicaleClient> logger)
{
    public async Task<DavResponse> CreateAsync(DavRequest davRequest)
    {
        davRequest.Method = "MKCOL";
        var response = await RequestAsync(davRequest);
        return GetDavResponse(response);
    }

    public async Task<DavResponse> GetAsync(DavRequest davRequest)
    {
        davRequest.Method = "GET";
        var response = await RequestAsync(davRequest);
        var davResponse = new DavResponse
        {
            StatusCode = (int)response.StatusCode
        };

        if (response.StatusCode == HttpStatusCode.OK)
        {
            davResponse.Completed = true;
            davResponse.Data = await response.Content.ReadAsStringAsync();
        }
        else
        {
            davResponse.Completed = false;
            davResponse.Error = response.ReasonPhrase;
        }

        return davResponse;
    }


    public async Task<DavResponse> UpdateItemAsync(DavRequest davRequest)
    {
        davRequest.Method = "PUT";
        var response = await RequestAsync(davRequest);
        return GetDavResponse(response);
    }

    public async Task<DavResponse> UpdateAsync(DavRequest davRequest)
    {
        davRequest.Method = "PROPPATCH";
        var response = await RequestAsync(davRequest);
        return GetDavResponse(response);
    }

    public async Task RemoveAsync(DavRequest davRequest)
    {
        davRequest.Method = "DELETE";
        await RequestAsync(davRequest);
    }

    private async Task<HttpResponseMessage> RequestAsync(DavRequest davRequest)
    {
        try
        {
            using var hc = new HttpClient();

            hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(davRequest.Authorization)));
            if (!string.IsNullOrEmpty(davRequest.Header))
            {
                hc.DefaultRequestHeaders.Add("X_REWRITER_URL", davRequest.Header);
            }

            var method = new HttpMethod(davRequest.Method);
            using var request = new HttpRequestMessage(method, davRequest.Url);

            if (davRequest.Data != null)
            {
                request.Content = new StringContent(davRequest.Data);
            }

            return await hc.SendAsync(request).ConfigureAwait(false);
        }
        catch (AggregateException ex)
        {
            throw new RadicaleException(ex.Message);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
            throw new RadicaleException(ex.Message);
        }
    }


    private DavResponse GetDavResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return new DavResponse
            {
                Completed = true,
                Data = response.IsSuccessStatusCode ? response.RequestMessage.RequestUri.ToString() : response.ReasonPhrase
            };

        }

        return new DavResponse
        {
            Completed = false,
            StatusCode = (int)response.StatusCode,
            Error = response.ReasonPhrase
        };

    }
}