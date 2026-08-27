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

namespace ASC.Files.Tests.Tests._04_Security.Ssrf;

/// <summary>
/// POST /api/2.0/files/thirdparty (WebDAV) must validate the provider URL before the server performs
/// an outbound PROPFIND to it. <see cref="ThirdPartyIntegrationApi"/> is not wired onto the shared
/// <c>PortalClients</c> bundle, so this suite builds its own instance against the already-authenticated
/// <c>_filesClient</c>, mirroring exactly how <c>PortalClients</c> builds every other Files SDK client.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Ssrf")]
public class ThirdPartySsrfTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<string, string> SsrfProviderUrls => new()
    {
        { "http://127.0.0.1:9999/webdav-canary", "ssrf-webdav-loopback" },
        { "http://169.254.169.254/", "ssrf-webdav-imds" },
        { "http://192.168.0.1/webdav", "ssrf-webdav-rfc1918" },
    };

    [Theory]
    [Trait("Category", "Bug")]
    [Trait("Bug", "82560")]
    [MemberData(nameof(SsrfProviderUrls))]
    public async Task SaveThirdParty_SsrfProviderUrl_IsRejected(string url, string customerTitle)
    {
        // Arrange
        var thirdPartyApi = CreateThirdPartyApi();

        var request = new ThirdPartyRequestDto(
            url: url,
            login: "ssrf-test",
            password: "ssrf-test",
            providerKey: "WebDav",
            customerTitle: customerTitle);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await thirdPartyApi.SaveThirdPartyAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400,
            "the server must reject a WebDAV provider URL pointing at a loopback, link-local or private address before connecting to it");
    }

    private ThirdPartyIntegrationApi CreateThirdPartyApi()
    {
        var config = new Configuration { BasePath = _filesClient.BaseAddress!.ToString().TrimEnd('/') };
        return new ThirdPartyIntegrationApi(_filesClient, config);
    }
}
