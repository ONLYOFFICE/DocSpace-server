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

namespace ASC.Web.Files.Services.DocumentService;

/// <summary>
/// Tells whether the connected document server can edit encrypted files. It does not report that itself
/// yet, so the presence of the crypto library it serves to the editors stands in for the answer.
/// </summary>
[Scope]
public class DocumentServiceEncryptionChecker(
    IFusionCacheProvider cacheProvider,
    IHttpClientFactory clientFactory,
    FilesLinkUtility filesLinkUtility,
    TenantManager tenantManager)
{
    private const string CryptoLibraryPath = "sdkjs/common/crypto/onlyoffice-crypto.js";
    private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(5);

    public async Task<bool> IsSupportedAsync()
    {
        var serviceUrl = filesLinkUtility.GetDocServiceUrlInternal();
        if (string.IsNullOrEmpty(serviceUrl))
        {
            return false;
        }

        var url = $"{serviceUrl.TrimEnd('/')}/{CryptoLibraryPath}";

        return await cacheProvider.GetMemoryCache().GetOrSetAsync<bool>(
            $"{tenantManager.GetCurrentTenantId()}docservice-encryption-{url}",
            async (_, token) => await ProbeAsync(url, token),
            TimeSpan.FromMinutes(10));
    }

    private async Task<bool> ProbeAsync(string url, CancellationToken cancellationToken)
    {
        var sslVerification = await filesLinkUtility.GetDocServiceSslVerificationAsync();
        var client = sslVerification ? clientFactory.CreateClient() : clientFactory.CreateClient("customHttpClientSslIgnore");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_requestTimeout);

        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
            return false;
        }
    }
}
