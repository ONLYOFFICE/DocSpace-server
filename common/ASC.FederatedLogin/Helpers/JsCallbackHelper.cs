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

public static partial class JsCallbackHelper
{
    public const string DefaultCallback = "loginCallback";
    public const string DefaultReturnUrl = "/";

    //A JS callback is spliced into the callback page as a code identifier
    //(window.opener.<callback>(...)), so it cannot be string-escaped. Only an
    //optionally dotted JS identifier chain is allowed; anything else falls back
    //to the default to prevent script injection.
    [GeneratedRegex(@"^[A-Za-z_$][A-Za-z0-9_$]*(\.[A-Za-z_$][A-Za-z0-9_$]*)*$")]
    private static partial Regex CallbackRegex();

    [GeneratedRegex("%(PROFILE|CALLBACK|RETURNURL|DESKTOP)%")]
    private static partial Regex PlaceholderRegex();

    public static string GetCallbackPage()
    {
        using var reader = new StreamReader(Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("ASC.FederatedLogin.callback.htm"));

        return reader.ReadToEnd();
    }

    public static string RenderCallbackPage(string profileTransport, string callback, string returnUrl, bool desktop)
    {
        //All values are spliced into a live <script> block, so they must be encoded for the
        //JavaScript context to prevent XSS. The callback is a code identifier rather than a
        //string literal, so it is validated instead of escaped; the return url is assigned
        //to window.location.href, where encoding alone is not enough, so it is validated too.
        var profile = HttpUtility.JavaScriptStringEncode(profileTransport, true);
        var safeCallback = GetSafeCallback(callback);
        var safeReturnUrl = HttpUtility.JavaScriptStringEncode(desktop ? GetSafeReturnUrl(returnUrl) : DefaultReturnUrl, true);
        var desktopLiteral = desktop.ToString().ToLowerInvariant();

        //One pass over the template: with sequential replacements a value carrying a
        //placeholder token of its own would be rewritten by a later substitution.
        return PlaceholderRegex().Replace(GetCallbackPage(), match => match.Groups[1].Value switch
        {
            "PROFILE" => profile,
            "CALLBACK" => safeCallback,
            "RETURNURL" => safeReturnUrl,
            "DESKTOP" => desktopLiteral,
            _ => throw new UnreachableException($"No value for the {match.Value} placeholder of the callback page")
        });
    }

    public static string GetSafeCallback(string callback)
    {
        return !string.IsNullOrEmpty(callback) && CallbackRegex().IsMatch(callback) ? callback : DefaultCallback;
    }

    public static string GetSafeReturnUrl(string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return DefaultReturnUrl;
        }

        //Browsers drop every ASCII tab and newline from a url before parsing it, so
        //"/<tab>/host" is navigated to as "//host". Validate and return what the browser
        //will actually parse rather than what the caller sent.
        var url = returnUrl
            .Replace("\t", string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Trim();

        if (url.Length == 0)
        {
            return DefaultReturnUrl;
        }

        //A site-relative path is always safe, but "//host" and "/\host" are
        //protocol-relative urls pointing at another origin, not local paths.
        if (url.StartsWith('/'))
        {
            return url.StartsWith("//") || url.StartsWith("/\\") ? DefaultReturnUrl : url;
        }

        //The url is assigned to window.location.href, so a "javascript:" or "data:" url would
        //execute as script in the portal origin. Redirects to another host are legitimate here,
        //so only the scheme is restricted.
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? url
            : DefaultReturnUrl;
    }
}
