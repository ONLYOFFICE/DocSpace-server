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
/// The OAuth callback page calls the caller-supplied callback as code
/// (<c>window.opener.%CALLBACK%(%PROFILE%)</c>), and the value used to be spliced in
/// verbatim, so <c>?callback=x,alert(document.domain),x</c> executed script in the portal
/// origin. It is now accepted only as an optionally dotted JS identifier chain and falls
/// back to the default callback otherwise.
/// </summary>
[Trait("Bug", "82555")]
public class JsCallbackSafeCallbackTests
{
    [Theory]
    [InlineData("loginCallback")]
    [InlineData("myCallback")]
    [InlineData("_callback")]
    [InlineData("$callback")]
    [InlineData("callback2")]
    [InlineData("Common.loginCallback")]
    public void GetSafeCallback_JsIdentifier_ShouldKeep(string callback)
    {
        JsCallbackHelper.GetSafeCallback(callback).Should().Be(callback);
    }

    [Theory]
    [InlineData("x,alert(document.domain),x")]
    [InlineData("loginCallback);alert(document.domain);//")]
    [InlineData("loginCallback+alert(1)")]
    [InlineData("loginCallback(1)")]
    [InlineData("loginCallback\n;alert(1)")]
    [InlineData("login-callback")]
    [InlineData("1callback")]
    [InlineData(" loginCallback")]
    [InlineData(".loginCallback")]
    [InlineData("loginCallback.")]
    public void GetSafeCallback_NotAJsIdentifier_ShouldFallBackToDefault(string callback)
    {
        JsCallbackHelper.GetSafeCallback(callback).Should().Be(JsCallbackHelper.DefaultCallback);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetSafeCallback_Missing_ShouldFallBackToDefault(string? callback)
    {
        JsCallbackHelper.GetSafeCallback(callback).Should().Be(JsCallbackHelper.DefaultCallback);
    }

    /// <remarks>
    /// The pattern is anchored with \A and \z, because a $ anchor still admits one trailing
    /// newline and would accept "myCallback\n" as an identifier.
    /// </remarks>
    [Theory]
    [InlineData(0x09)]
    [InlineData(0x0A)]
    [InlineData(0x0D)]
    [InlineData(0x20)]
    public void GetSafeCallback_IdentifierWithATrailingControlCharacter_ShouldFallBackToDefault(int trailing)
    {
        var callback = "myCallback" + (char)trailing;

        JsCallbackHelper.GetSafeCallback(callback).Should().Be(JsCallbackHelper.DefaultCallback);
    }
}
