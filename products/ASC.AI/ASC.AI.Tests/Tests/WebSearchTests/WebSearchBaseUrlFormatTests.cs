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

namespace ASC.AI.Tests.Tests.WebSearchTests;

/// <summary>
/// The shape rules of a <c>baseUrl</c> on the three routes that take one: an unresolvable name, something that is
/// not a URL, a scheme other than http(s) and credentials in the URL are each a 400 naming the rule.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchBaseUrlFormatTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    private static readonly (string Url, string Message)[] _badUrls =
    [
        ("https://web-search-attacker.invalid", "baseUrl host could not be resolved"),
        ("not-a-url", "baseUrl is not a valid URL"),
        ("file:///etc/passwd", "baseUrl must use http or https"),
        ("ftp://example.com/", "baseUrl must use http or https"),
        ("http://user:pw@127.0.0.1:1/", "baseUrl must not contain credentials")
    ];

    public static TheoryData<string, string, string> BadUrls()
    {
        var data = new TheoryData<string, string, string>();
        foreach (var route in BaseUrlRoutes)
        {
            foreach (var (url, message) in _badUrls)
            {
                data.Add(route, url, message);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(BadUrls))]
    public async Task MalformedBaseUrl_IsRefusedWith400NamingTheRule(string route, string url, string message)
    {
        var reply = await SendToRouteAsync(route, "onlyoffice", url);

        reply.Status.Should().Be(400, reply.ToString());
        reply.Error.Should().Be(message);
        await ExpectNothingStoredAsync();
    }
}
