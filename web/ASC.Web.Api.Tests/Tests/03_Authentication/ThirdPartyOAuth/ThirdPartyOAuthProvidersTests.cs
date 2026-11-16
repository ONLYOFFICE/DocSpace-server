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
/// The seven services the consent URL is built for. The four this installation has defaults for point at their own
/// authorization host; DocuSign, WordPress and GitHub have no OAuth application configured on the test stack.
/// </summary>
[Trait("Category", "Authentication")]
public class ThirdPartyOAuthProvidersTests(AspireAppFixture fixture) : ThirdPartyOAuthTestBase(fixture)
{
    [Theory]
    [InlineData(LoginProvider.Google, "google.com")]
    [InlineData(LoginProvider.Dropbox, "dropbox.com")]
    [InlineData(LoginProvider.Box, "box.com")]
    [InlineData(LoginProvider.OneDrive, "login.")]
    public async Task GetThirdPartyCode_SupportedProvider_ReturnsConsentUrlOfThatService(LoginProvider provider, string hostFragment)
    {
        // Act
        var url = (await ThirdParty.GetThirdPartyCodeAsync(provider, TestContext.Current.CancellationToken)).Response;

        // Assert
        url.Should().NotBeNullOrEmpty();
        new Uri(url).Host.Should().Contain(hostFragment);
    }

    /// <summary>
    /// SI11: a supported service without an OAuth application on the installation fails while the consent URL is
    /// built (a null dereference for WordPress and GitHub, an unparsable address for DocuSign), so the caller gets 500
    /// instead of an answer without a URL or a refusal.
    /// </summary>
    [Theory]
    [InlineData(LoginProvider.Docusign)]
    [InlineData(LoginProvider.Wordpress)]
    [InlineData(LoginProvider.Github)]
    [Trait("Category", "Bug")]
    public async Task GetThirdPartyCode_SupportedProviderWithoutOAuthApplication_DoesNotFailWithServerError(LoginProvider provider)
    {
        // Act
        var exception = await Record.ExceptionAsync(
            async () => await ThirdParty.GetThirdPartyCodeAsync(provider, TestContext.Current.CancellationToken));

        // Assert
        if (exception is ApiException apiException)
        {
            apiException.ErrorCode.Should().BeLessThan(500, apiException.ErrorContent!.ToString());
        }
        else
        {
            exception.Should().BeNull();
        }
    }
}
