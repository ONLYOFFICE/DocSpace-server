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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Lifecycle;

/// <summary>
/// POST apisystem/portal/validateportalname — the rules a portal name has to pass, anonymously, on a server
/// installation (the tenant service checks the name; no hosting cache).
/// </summary>
[Trait("Category", "Portal")]
public class SystemApiPortalNameTests(AspireAppFixture fixture) : PortalLifecycleTestBase(fixture)
{
    [Fact]
    public async Task ValidatePortalName_FreeName_IsReadyToRegister()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/validateportalname", new { portalName = FreePortalName() });

        status.Should().Be(HttpStatusCode.OK);
        body.GetProperty("message").GetString().Should().Be("portalNameReadyToRegister");
    }

    [Fact]
    public async Task ValidatePortalName_TakenNameInUpperCaseWithSpaces_IsTakenWithVariants()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/validateportalname", new { portalName = $"  {Clients.PortalName.ToUpperInvariant()} " });

        status.Should().Be(HttpStatusCode.BadRequest, "the name is trimmed and lower-cased before it is checked");
        body.GetProperty("error").GetString().Should().Be("portalNameExist");
        body.GetProperty("variants").EnumerateArray().Select(v => v.GetString()).Should().Contain(Clients.PortalName);
    }

    [Theory]
    [InlineData("", "portalNameEmpty")]
    [InlineData("ab", "tooShortError")]
    [InlineData("a234567890123456789012345678901234567890123456789012345678901234", "tooShortError")]
    [InlineData("bad_name", "portalNameIncorrect")]
    [InlineData("-abcdefg", "portalNameIncorrect")]
    [InlineData("abcdefg-", "portalNameIncorrect")]
    [InlineData("xn--80akhbyknj4f", "portalNameIncorrect")]
    public async Task ValidatePortalName_BrokenRule_ReturnsItsCode(string portalName, string expectedError)
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/validateportalname", new { portalName });

        status.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().Should().Be(expectedError);
    }

    [Fact]
    public async Task ValidatePortalName_ConfiguredMinimumAndSixtyThreeCharacters_AreAccepted()
    {
        // buildtools/config/appsettings.json lowers web:alias:min from the default 6 to 3; the maximum stays 63.
        var shortest = "s" + Guid.NewGuid().ToString("N")[..2];
        var sixtyThree = "l" + string.Concat(Enumerable.Repeat(Guid.NewGuid().ToString("N"), 2))[..62];

        foreach (var portalName in new[] { shortest, sixtyThree })
        {
            var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/validateportalname", new { portalName });

            status.Should().Be(HttpStatusCode.OK, $"{portalName.Length} characters are within the configured 3-63");
            body.GetProperty("message").GetString().Should().Be("portalNameReadyToRegister");
        }
    }
}
