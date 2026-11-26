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
/// The domains a personal web plugin may declare in <c>cspDomains</c> (<c>WebPluginManager.IsValidPersonalCspDomain</c>):
/// an ASCII host, with an optional wildcard below a name of two labels or more (the public suffix list is not
/// consulted, so <c>*.co.uk</c> passes), over http(s) or ws(s). A bare scheme, a lone wildcard, a
/// wildcard over a top-level domain and a keyword would open the member's pages to any site, and a space, a <c>;</c>
/// or a quote, even in the path, would add a source or a directive to the member's header.
/// </summary>
public class PersonalCspDomainTests
{
    [Theory]
    [InlineData("https://api.example.com")]
    [InlineData("api.example.com")]
    [InlineData("*.example.com")]
    [InlineData("https://*.example.com")]
    [InlineData("wss://socket.example.com")]
    [InlineData("https://example.com:8443")]
    [InlineData("https://example.com/path")]
    [InlineData("localhost:3000")]
    public void IsValidPersonalCspDomain_Host_Accepted(string domain)
    {
        WebPluginManager.IsValidPersonalCspDomain(domain).Should().BeTrue();
    }

    [Theory]
    [InlineData("https:")]
    [InlineData("data:")]
    [InlineData("https://")]
    [InlineData("*")]
    [InlineData("*.com")]
    [InlineData("https://*.com")]
    [InlineData("'unsafe-inline'")]
    [InlineData("'self'")]
    [InlineData("ftp://files.example.com")]
    [InlineData("https://example.com;script-src")]
    [InlineData("https://example.com https://other.example.com")]
    [InlineData("https://example.com/a 'unsafe-inline'")]
    [InlineData("https://example.com/a;script-src")]
    [InlineData("https://example.com/'self'")]
    [InlineData("https://caf\u00e9.example.com")]
    public void IsValidPersonalCspDomain_NotAHost_Refused(string domain)
    {
        WebPluginManager.IsValidPersonalCspDomain(domain).Should().BeFalse();
    }
}
