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
/// POST apisystem/portal/register on a server installation: the answer of a registration, the name it keeps or
/// generates, and the refusals of the body. Every registration here creates a portal of its own; the suite's
/// owner address is the installation's test address, so neither the throttling nor reCAPTCHA applies.
/// </summary>
[Trait("Category", "Portal")]
public class SystemApiRegistrationTests(AspireAppFixture fixture) : PortalLifecycleTestBase(fixture)
{
    private static object Registration(string? portalName, string? password = null, string firstName = "Portal", string lastName = "Owner")
    {
        return new
        {
            portalName,
            firstName,
            lastName,
            email = Initializer.OwnerEmail,
            password = password ?? Initializer.OwnerPassword,
            language = "en-US",
            timeZoneName = "UTC",
            skipWelcome = true
        };
    }

    [Fact]
    public async Task Register_RequestedName_ReturnsTheTenantUnderThatName()
    {
        var portalName = FreePortalName();

        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/register", Registration(portalName));

        status.Should().Be(HttpStatusCode.OK);
        body.GetProperty("reference").GetString().Should().NotBeNullOrEmpty();

        var tenant = body.GetProperty("tenant");
        tenant.GetProperty("tenantId").GetInt32().Should().BePositive();
        tenant.GetProperty("portalName").GetString().Should().Be(portalName, "a server installation keeps the requested name");
        tenant.GetProperty("domain").GetString().Should().StartWith(portalName);
        tenant.GetProperty("status").GetString().Should().Be("Active");
        tenant.GetProperty("ownerId").GetGuid().Should().NotBeEmpty();
        tenant.GetProperty("language").GetString().Should().Be("en-US");
    }

    [Fact]
    public async Task Register_SkipWelcome_ReturnsTheWelcomeAddressSeparately()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/register", Registration(FreePortalName()));

        status.Should().Be(HttpStatusCode.OK);
        body.GetProperty("referenceWelcome").GetString().Should().NotBeNullOrEmpty("a skipped welcome is handed back to the caller");
    }

    [Fact]
    public async Task Register_NoName_GeneratesOneWithThePrefixAndSixCharacters()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/register", Registration(null));

        status.Should().Be(HttpStatusCode.OK);
        body.GetProperty("tenant").GetProperty("portalName").GetString().Should().MatchRegex("^docspace-[a-z0-9]{6}$");
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsPasswordPolicyError()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/register", Registration(FreePortalName(), password: "1"));

        status.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty("error").GetString().Should().Be("passPolicyError");
    }

    [Fact]
    public async Task Register_EmptyNames_ReturnsNameIsRequired()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/register", Registration(FreePortalName(), firstName: "", lastName: ""));

        status.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty("message").GetString().Should().Be("name is required");
    }

    [Fact]
    public async Task Register_InvalidName_ReturnsNameIsIncorrect()
    {
        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/register", Registration(FreePortalName(), firstName: "<script>", lastName: "!!!"));

        status.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty("message").GetString().Should().Be("name is incorrect");
    }

    [Fact]
    public async Task Register_TakenName_ReturnsRegisterNewTenantError()
    {
        // A server installation does not check the requested name before it creates the tenant: the tenant service
        // refuses it, and the answer is the 500 of a failed creation, not a 400 of validateportalname.
        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/register", Registration(Clients.PortalName));

        status.Should().Be(HttpStatusCode.InternalServerError);
        body.GetProperty("error").GetString().Should().Be("registerNewTenantError");
    }
}
