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

namespace ASC.Core.Common.Tests;

/// <summary>
/// The desktop branch of the OAuth callback page assigns the caller-supplied return url to
/// <c>window.location.href</c>. The value used to be spliced in unquoted, so
/// <c>?returnurl=";alert(document.domain);//</c> broke out of the string literal; escaping
/// alone would still leave <c>javascript:</c> urls executable, so the scheme is validated as
/// well — and, since the value is a navigation away from the portal, an absolute url is
/// accepted only on a host named in the configuration.
/// </summary>
[Trait("Bug", "82555")]
public class JsCallbackSafeReturnUrlTests
{
    //What federated-login:allowed-return-url-hosts carries.
    private static readonly string[] _allowedHosts = ["www.example.com"];

    [Theory]
    [InlineData("/")]
    [InlineData("/rooms/shared")]
    [InlineData("/rooms/shared?desktop=true#login")]
    public void GetSafeReturnUrl_LocalPath_ShouldKeep(string returnUrl)
    {
        JsCallbackHelper.GetSafeReturnUrl(returnUrl).Should().Be(returnUrl);
    }

    [Theory]
    [InlineData("javascript:alert(document.domain)")]
    [InlineData("JavaScript:alert(document.domain)")]
    [InlineData("  javascript:alert(document.domain)")]
    [InlineData("javascript:/**/alert(document.domain)")]
    [InlineData("data:text/html,<script>alert(document.domain)</script>")]
    [InlineData("vbscript:msgbox(1)")]
    public void GetSafeReturnUrl_ScriptScheme_ShouldFallBackToDefault(string returnUrl)
    {
        JsCallbackHelper.GetSafeReturnUrl(returnUrl).Should().Be(JsCallbackHelper.DefaultReturnUrl);
    }

    [Theory]
    [InlineData("https://evil.example.com/rooms")]
    [InlineData("http://evil.example.com/rooms")]
    [InlineData("https://portal.example.com/rooms")]
    [InlineData("https://user:password@evil.example.com/#@portal.example.com/")]
    public void GetSafeReturnUrl_AbsoluteUrlWithNoConfiguredHosts_ShouldFallBackToDefault(string returnUrl)
    {
        JsCallbackHelper.GetSafeReturnUrl(returnUrl).Should().Be(JsCallbackHelper.DefaultReturnUrl);
    }

    [Theory]
    [InlineData("//evil.example.com")]
    [InlineData("/\\evil.example.com")]
    [InlineData("/\\/evil.example.com")]
    [InlineData("\\\\evil.example.com")]
    public void GetSafeReturnUrl_ProtocolRelativeUrl_ShouldFallBackToDefault(string returnUrl)
    {
        JsCallbackHelper.GetSafeReturnUrl(returnUrl).Should().Be(JsCallbackHelper.DefaultReturnUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("rooms/shared")]
    public void GetSafeReturnUrl_MissingOrNotAnAbsoluteUrl_ShouldFallBackToDefault(string? returnUrl)
    {
        JsCallbackHelper.GetSafeReturnUrl(returnUrl).Should().Be(JsCallbackHelper.DefaultReturnUrl);
    }

    /// <remarks>
    /// The configured hosts (<c>federated-login:allowed-return-url-hosts</c>) are what keeps the
    /// desktop registration flow working: it returns to the ONLYOFFICE site, not to the portal.
    /// </remarks>
    [Theory]
    [InlineData("https://www.example.com/registration?desktop=true#login")]
    [InlineData("http://www.example.com/registration")]
    [InlineData("https://WWW.EXAMPLE.COM/registration")]
    [InlineData("https://www.example.com:8443/registration")]
    public void GetSafeReturnUrl_ConfiguredHost_ShouldKeep(string returnUrl)
    {
        JsCallbackHelper.GetSafeReturnUrl(returnUrl, _allowedHosts).Should().Be(returnUrl);
    }

    [Theory]
    [InlineData("https://evil.example.com/rooms")]
    [InlineData("https://www.example.com.evil.example.com/rooms")]
    [InlineData("javascript:alert(document.domain)")]
    [InlineData("//www.example.com")]
    public void GetSafeReturnUrl_NotAConfiguredHost_ShouldFallBackToDefault(string returnUrl)
    {
        JsCallbackHelper.GetSafeReturnUrl(returnUrl, _allowedHosts).Should().Be(JsCallbackHelper.DefaultReturnUrl);
    }

    /// <remarks>
    /// Browsers drop every ASCII tab and newline from a url before parsing it, so a value that
    /// only looks site-relative — "/&lt;tab&gt;/host" — is navigated to as "//host".
    /// </remarks>
    [Theory]
    [InlineData(0x09)]
    [InlineData(0x0A)]
    [InlineData(0x0D)]
    public void GetSafeReturnUrl_ProtocolRelativeUrlHiddenByAStrippedCharacter_ShouldFallBackToDefault(int stripped)
    {
        var returnUrl = "/" + (char)stripped + "/evil.example.com";

        JsCallbackHelper.GetSafeReturnUrl(returnUrl).Should().Be(JsCallbackHelper.DefaultReturnUrl);
    }

    [Theory]
    [InlineData(0x09)]
    [InlineData(0x0A)]
    [InlineData(0x0D)]
    public void GetSafeReturnUrl_BackslashUrlHiddenByAStrippedCharacter_ShouldFallBackToDefault(int stripped)
    {
        var returnUrl = "/" + (char)stripped + "\\evil.example.com";

        JsCallbackHelper.GetSafeReturnUrl(returnUrl).Should().Be(JsCallbackHelper.DefaultReturnUrl);
    }

    [Fact]
    public void GetSafeReturnUrl_LocalPathWithAStrippedCharacter_ShouldReturnWhatTheBrowserParses()
    {
        var returnUrl = "/rooms" + (char)0x09 + "/shared";

        JsCallbackHelper.GetSafeReturnUrl(returnUrl).Should().Be("/rooms/shared");
    }
}
