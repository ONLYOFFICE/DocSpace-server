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
/// GET api/2.0/thirdparty/{provider} - the OAuth 2.0 consent URL of one external provider. Only seven of the
/// fifteen <see cref="LoginProvider"/> values produce a URL; the other valid members answer 200 with no URL,
/// while a value outside the enum is rejected by model binding with 400.
/// </summary>
[Trait("Category", "Authentication")]
public class ThirdPartyOAuthGetCodeTests(AspireAppFixture fixture) : ThirdPartyOAuthTestBase(fixture)
{
    [Fact]
    public async Task GetThirdPartyCode_OwnerGoogle_ReturnsConsentUrl()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var response = await ThirdParty.GetThirdPartyCodeAsync(LoginProvider.Google, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().Contain("accounts.google.com");
        response.Response.Should().Contain("response_type=code");
    }

    [Fact]
    public async Task GetThirdPartyCode_OwnerProviderWithoutUrl_ReturnsNoUrl()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var response = await ThirdParty.GetThirdPartyCodeAsync(LoginProvider.Facebook, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetThirdPartyCode_OwnerOutOfRangeProvider_ReturnsBadRequest()
    {
        // Arrange: the typed signature takes the enum, so an out-of-range number has to go over raw HTTP
        await _webApiClient.Authenticate(Owner);

        // Act
        using var response = await _webApi.GetAsync("api/2.0/thirdparty/999", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetThirdPartyCode_OwnerDesktop_CopiesQueryStringIntoCallbackState()
    {
        // Arrange: desktop is not a parameter of the typed method
        await _webApiClient.Authenticate(Owner);
        var path = $"api/2.0/thirdparty/{(int)LoginProvider.Google}";

        // Act
        using var plainResponse = await _webApi.GetAsync(path, TestContext.Current.CancellationToken);
        using var desktopResponse = await _webApi.GetAsync($"{path}?desktop=true&foo=bar", TestContext.Current.CancellationToken);

        // Assert
        var plainUrl = await _webApi.ReadAsync<string>(plainResponse, TestContext.Current.CancellationToken);
        var desktopUrl = await _webApi.ReadAsync<string>(desktopResponse, TestContext.Current.CancellationToken);

        StateOf(plainUrl).Should().EndWith("/thirdparty/google/code");
        StateOf(desktopUrl).Should().EndWith("/thirdparty/google/code?desktop=true&foo=bar");
    }

    private static string StateOf(string consentUrl)
    {
        var state = new Uri(consentUrl).Query.TrimStart('?')
            .Split('&')
            .FirstOrDefault(p => p.StartsWith("state=", StringComparison.Ordinal));

        state.Should().NotBeNull("the consent URL carries a state parameter: {0}", consentUrl);

        return Uri.UnescapeDataString(state!["state=".Length..]);
    }
}
