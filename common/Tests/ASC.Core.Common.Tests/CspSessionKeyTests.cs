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
/// The key the header of a signed-in session is stored under (<c>CspSettingsHelper.GetSessionKey</c>). The proxy
/// (<c>buildtools/config/nginx/onlyoffice.conf</c>) builds the same key itself: the host key, <c>:personal:</c>, the
/// generation of the portal or <c>0</c> while there is none, and the lowercase SHA-256 of the session cookie it got.
/// </summary>
public class CspSessionKeyTests
{
    [Fact]
    public void GetSessionKey_WithAGeneration_HashesTheSessionCookie()
    {
        // Act
        var key = CspSettingsHelper.GetSessionKey("csp:portal.example.com", "8d1f", "t1Hz/ab+c=");

        // Assert: the hash is the one the proxy computes for the cookie value t1Hz%2Fab%2Bc%3D
        key.Should().Be("csp:portal.example.com:personal:8d1f:0a77e33d87028293bd1d419269d414dbf028a387a34729946dd30594adc378ba");
    }

    [Fact]
    public void GetSessionKey_WithoutAGeneration_UsesZero()
    {
        // Act
        var key = CspSettingsHelper.GetSessionKey("csp:portal.example.com", null, "t1Hz/ab+c=");

        // Assert
        key.Should().Be("csp:portal.example.com:personal:0:0a77e33d87028293bd1d419269d414dbf028a387a34729946dd30594adc378ba");
    }

    [Theory]
    [InlineData("asc_auth_key=a; other=b", 1)]
    [InlineData("asc_auth_key=a; asc_auth_key=b", 2)]
    [InlineData("ASC_AUTH_KEY=a; asc_auth_key=b", 2)]
    [InlineData("x_asc_auth_key=a; asc_auth_key=b", 1)]
    [InlineData("zz=1, asc_auth_key=a; asc_auth_key=b", 2)]
    [InlineData("other=b", 0)]
    public void CountCookies_CookieHeader_CountsTheCookiesOfThatName(string header, int expected)
    {
        // Act
        var count = CspSettingsHelper.CountCookies([header], "asc_auth_key");

        // Assert
        count.Should().Be(expected);
    }

    [Fact]
    public void CountCookies_SeveralCookieHeaders_CountsAcrossThem()
    {
        // Act
        var count = CspSettingsHelper.CountCookies(["asc_auth_key=a", "asc_auth_key=b"], "asc_auth_key");

        // Assert
        count.Should().Be(2);
    }

    [Theory]
    [InlineData(null, "asc_auth_key=a; other=b", "a", true)]
    [InlineData("Basic dXNlcjpwYXNz", "asc_auth_key=a", "a", false)]
    [InlineData(null, "asc_auth_key=a; ASC_AUTH_KEY=b", "a", false)]
    [InlineData(null, "zz=1, asc_auth_key=a; asc_auth_key=b", "b", false)]
    [InlineData(null, "asc_auth_key=", "", false)]
    public void CanCacheSessionHeader_Request_CachesOnlyForOneSessionCookieAndNoAuthorizationHeader(string? authorization, string cookieHeader, string authKey, bool expected)
    {
        // Act
        var canCache = CspSettingsHelper.CanCacheSessionHeader(authorization, [cookieHeader], authKey);

        // Assert
        canCache.Should().Be(expected);
    }

    [Theory]
    [InlineData(new[] { "a.example" }, new[] { "A.example" }, false, false)]
    [InlineData(new[] { "a.example" }, new[] { "a.example", "b.example" }, true, true)]
    [InlineData(new[] { "a.example", "b.example" }, new[] { "a.example" }, true, false)]
    [InlineData(new string[0], new string[0], false, false)]
    public void PersonalDomains_BeforeAndAfter_SayWhetherTheyChangedAndGrew(string[] before, string[] after, bool changed, bool added)
    {
        // Act and assert
        CspSettingsHelper.PersonalDomainsChanged(before, after).Should().Be(changed);
        CspSettingsHelper.PersonalDomainsAdded(before, after).Should().Be(added);
    }

    [Fact]
    public void PersonalDomains_BeforeUnknown_CountAsChangedButNoneAsAdded()
    {
        // Act and assert
        CspSettingsHelper.PersonalDomainsChanged(null, []).Should().BeTrue();
        CspSettingsHelper.PersonalDomainsAdded(null, ["a.example"]).Should().BeFalse();
    }

    [Fact]
    public void GetGenerationHostKeys_AliasAndRequestHost_AreAddedToTheHeaderKeys()
    {
        // Act
        var keys = CspSettingsHelper.GetGenerationHostKeys(["csp:mapped.example.com"], "portal.onlyoffice.io", "Alias.Example.com");

        // Assert
        keys.Should().BeEquivalentTo("csp:mapped.example.com", "csp:portal.onlyoffice.io", "csp:alias.example.com");
    }
}
