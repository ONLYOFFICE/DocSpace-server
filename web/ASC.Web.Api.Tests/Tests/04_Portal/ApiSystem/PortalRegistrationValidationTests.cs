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
/// POST apisystem/portal/register refusing what the caller sent. A name the caller chose was not checked before the
/// insert and failed on the unique alias of the database, and an empty first name next to a last name failed inside the
/// registration with a plain exception: both were a 500 and are now 400. A missing email is checked first now, as
/// registerbyemail and provision check it. The stack is a server installation, the only one where the requested name is
/// kept. The owner address of the suite is the stack's autotest address, so neither reCAPTCHA nor the per-address limit
/// stands before the checks.
/// </summary>
[Trait("Category", "Portal")]
public class PortalRegistrationValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task Register_NameOfExistingPortal_Returns400PortalNameExist()
    {
        // Arrange: the name of this test's own portal
        var body = RegistrationBody(PortalName, firstName: "Portal", email: Initializer.OwnerEmail);

        // Act
        using var response = await Fixture.ApiSystem.PostAsync("portal/register", body, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ApiSystemError.ReadAsync(response);
        error.Error.Should().Be("portalNameExist");
        error.Message.Should().Be("Portal already exists");
    }

    [Fact]
    public async Task Register_WithoutEmail_Returns400EmailEmpty()
    {
        // Arrange: without an email the autotest address does not apply, so before the fix the call stopped at
        // reCAPTCHA on this stack (a 400 with another error); with a reCAPTCHA passed it was a 500
        var portalName = NewPortalName();
        var body = RegistrationBody(portalName, firstName: "Portal", email: null);

        // Act
        using var response = await Fixture.ApiSystem.PostAsync("portal/register", body, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ApiSystemError.ReadAsync(response);
        error.Error.Should().Be("emailEmpty");
        error.Message.Should().Be("Email is required");
        await AssertPortalNameFreeAsync(portalName);
    }

    [Fact]
    public async Task Register_EmptyFirstNameWithLastName_Returns400AndCreatesNoPortal()
    {
        // Arrange: the joined name passes the call's own check, the empty first name fails the one of the registration
        var portalName = NewPortalName();
        var body = RegistrationBody(portalName, firstName: "", email: Initializer.OwnerEmail);

        // Act
        using var response = await Fixture.ApiSystem.PostAsync("portal/register", body, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ApiSystemError.ReadAsync(response);
        error.Error.Should().Be("registerNewTenantError");
        error.Message.Should().Be("Incorrect firstname or lastname");
        await AssertPortalNameFreeAsync(portalName);
    }

    private static string NewPortalName()
    {
        // As AspireHostFixture.CreatePortalAsync names its portals: lowercase, a letter first, 13 characters.
        return "t" + Guid.NewGuid().ToString("N")[..12];
    }

    private static object RegistrationBody(string portalName, string firstName, string? email)
    {
        return new
        {
            portalName,
            firstName,
            lastName = "Owner",
            email,
            password = Initializer.OwnerPassword,
            language = "en-US",
            timeZoneName = "UTC",
            skipWelcome = true
        };
    }

    /// <summary>The name is still free: no portal was registered with it.</summary>
    private async Task AssertPortalNameFreeAsync(string portalName)
    {
        using var response = await Fixture.ApiSystem.PostAsync(
            "portal/validateportalname", new { portalName }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
