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
/// The rendered OAuth callback page used to splice the caller-supplied values into its
/// &lt;script&gt; block without any encoding, so a crafted /login link executed script in the
/// portal origin — the login provider did not even have to exist, because the page is
/// rendered from the exception path as well. The values are now JavaScript-encoded, and the
/// two that are not string literals (callback, return url) are validated.
/// </summary>
[Trait("Bug", "82555")]
public class JsCallbackRenderTests
{
    //What LoginProfileTransport.ToString produces: Base64Url(InstanceCrypto.Encrypt(profile)).
    private const string ProfileTransport = "Zm9vLWJhci1wcm9maWxl";

    [Fact]
    public void RenderCallbackPage_ReturnUrlBreakingOutOfTheStringLiteral_ShouldFallBackToDefault()
    {
        var page = JsCallbackHelper.RenderCallbackPage(ProfileTransport, null, "\";alert(document.domain);//", true);

        page.Should().NotContain("alert");
        page.Should().Contain("window.location.href = \"/\";");
    }

    /// <remarks>
    /// A value that passes the validation can still carry characters that break the string
    /// literal or the script block, so both layers are needed.
    /// </remarks>
    [Fact]
    public void RenderCallbackPage_LocalReturnUrlWithSpecialCharacters_ShouldEncode()
    {
        var page = JsCallbackHelper.RenderCallbackPage(ProfileTransport, null, "/rooms?a=\"&b=<script>", true);

        page.Should().Contain(@"window.location.href = ""/rooms?a=");
        page.Should().NotContain(@"a=""&");
        page.Should().NotContain("<script>");
    }

    [Fact]
    public void RenderCallbackPage_JavascriptSchemeReturnUrl_ShouldFallBackToDefault()
    {
        var page = JsCallbackHelper.RenderCallbackPage(ProfileTransport, null, "javascript:alert(document.domain)", true);

        page.Should().NotContain("javascript:alert");
        page.Should().Contain("window.location.href = \"/\";");
    }

    [Fact]
    public void RenderCallbackPage_CallbackInjectingCode_ShouldFallBackToDefault()
    {
        var page = JsCallbackHelper.RenderCallbackPage(ProfileTransport, "x,alert(document.domain),x", null, false);

        page.Should().NotContain("alert");
        page.Should().Contain($"window.opener.{JsCallbackHelper.DefaultCallback}(\"{ProfileTransport}\");");
    }

    [Fact]
    public void RenderCallbackPage_ProfileClosingTheScriptBlock_ShouldEncode()
    {
        var page = JsCallbackHelper.RenderCallbackPage("a</script><script>alert(document.domain)", "loginCallback", null, false);

        page.Should().NotContain("<script>");
        page.Should().Contain("\\u003c/script\\u003e");
    }

    [Fact]
    public void RenderCallbackPage_DesktopFlowReturnUrl_ShouldKeep()
    {
        const string returnUrl = "https://www.example.com/registration?desktop=true#login";

        var page = JsCallbackHelper.RenderCallbackPage(ProfileTransport, null, returnUrl, true);

        page.Should().Contain("if (true) {");
        page.Should().Contain($"window.location.href = \"{returnUrl}\";");
    }

    [Fact]
    public void RenderCallbackPage_PopupMode_ShouldIgnoreTheReturnUrl()
    {
        var page = JsCallbackHelper.RenderCallbackPage(ProfileTransport, "loginCallback", "https://www.example.com/", false);

        page.Should().Contain("if (false) {");
        page.Should().Contain("window.location.href = \"/\";");
    }
}
