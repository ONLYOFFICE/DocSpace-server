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

namespace ASC.Web.Api.Tests.Tests._04_Portal.ApiSystem;

/// <summary>
/// POST apisystem/portal/signin with a password hash no account of the address has. The lookup refused it with an
/// exception that ended in the call's catch-all as a 500; it is now 401 with the same text, as a wrong password at the
/// portal's own sign-in. The owner address of the suite is the stack's autotest address, so the per-address limit and
/// reCAPTCHA do not apply.
/// </summary>
[Trait("Category", "Portal")]
public class SignInToPortalCredentialsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SignIn_WrongPasswordHash_Returns401()
    {
        // Arrange
        var body = new
        {
            email = Initializer.OwnerEmail,
            passwordHash = Initializer.GetClientPassword("Wrong-Pass-" + Guid.NewGuid().ToString("N")[..8])
        };

        // Act
        using var response = await Fixture.ApiSystem.PostAsync("portal/signin", body, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var error = await ApiSystemError.ReadAsync(response);
        error.Message.Should().Be("Invalid login or password.");
    }

    [Fact]
    public async Task SignIn_OwnerCredentials_ListsThisPortal()
    {
        // Arrange: the guard that the refusal is not wider than wrong credentials
        var body = new { email = Initializer.OwnerEmail, passwordHash = Owner.PasswordHash };

        // Act
        using var response = await Fixture.ApiSystem.PostAsync("portal/signin", body, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(json);
        var portalNames = document.RootElement.GetProperty("tenants").EnumerateArray()
            .Select(t => t.GetProperty("portalName").GetString())
            .ToList();
        portalNames.Should().Contain(n => n != null && n.Contains($"//{PortalName}."));
    }
}
