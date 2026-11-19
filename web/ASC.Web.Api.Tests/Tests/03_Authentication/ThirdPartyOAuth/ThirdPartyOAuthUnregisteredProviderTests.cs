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

namespace ASC.Web.Api.Tests.Tests._03_Authentication.ThirdPartyOAuth;

/// <summary>
/// GET api/2.0/thirdparty/{provider} for one of the seven supported providers that the installation does not
/// register among its authorization consumers.
/// </summary>
/// <remarks>
/// The stand runs on the default consumer configuration (<c>autofac.consumers.json</c> of buildtools), which
/// registers Google, Dropbox, Box and OneDrive but not DocuSign, WordPress or GitHub. The unregistered provider
/// came back as a bare instance with no name, and building the URL failed with a 500 (a null name; for DocuSign a
/// consent address with no host). It now answers like a provider the operation does not support: 200 with no URL.
/// If the stand ever registers one of these three, its case returns a URL and fails here - pick another one.
/// </remarks>
[Trait("Category", "Authentication")]
public class ThirdPartyOAuthUnregisteredProviderTests(AspireAppFixture fixture) : ThirdPartyOAuthTestBase(fixture)
{
    [Theory]
    [InlineData(LoginProvider.Github)]
    [InlineData(LoginProvider.Wordpress)]
    [InlineData(LoginProvider.Docusign)]
    public async Task GetThirdPartyCode_OwnerProviderNotRegistered_ReturnsNoUrl(LoginProvider provider)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var response = await ThirdParty.GetThirdPartyCodeAsync(provider, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetThirdPartyCode_OwnerRegisteredProvider_StillReturnsConsentUrl()
    {
        // Arrange: Box is registered on the stand; the registration check must not hide its URL
        await _webApiClient.Authenticate(Owner);

        // Act
        var response = await ThirdParty.GetThirdPartyCodeAsync(LoginProvider.Box, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().Contain("app.box.com");

        // state carries the address of the code page URL-encoded (%2f for /), so it is decoded before the comparison;
        // the server builds it from the scheme, host and port of the request
        var webApi = _webApiClient.BaseAddress!;
        var state = HttpUtility.ParseQueryString(new Uri(response.Response).Query)["state"];
        state.Should().Be(new UriBuilder(webApi.Scheme, webApi.Host, webApi.Port, "thirdparty/box/code").Uri.ToString());
    }
}
