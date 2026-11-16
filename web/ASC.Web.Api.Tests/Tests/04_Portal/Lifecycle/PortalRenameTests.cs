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
/// PUT api/2.0/portal/portalrename — hidden from the API document (raw HTTP). A rename moves this test's own
/// portal to a new address, so the successful case is the last call of its test.
/// </summary>
[Trait("Category", "Portal")]
public class PortalRenameTests(AspireAppFixture fixture) : PortalLifecycleTestBase(fixture)
{
    private Task<HttpResponseMessage> RenameAsync(string alias)
    {
        return _webApi.PutAsync("api/2.0/portal/portalrename", new { alias }, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData("new name")]
    public async Task Rename_EmptyOrWhitespaceAlias_ReturnsBadRequest(string alias)
    {
        await _webApiClient.Authenticate(Owner);

        using var response = await RenameAsync(alias);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rename_SameAlias_ReturnsEmptyString_AndChangesNothing()
    {
        await _webApiClient.Authenticate(Owner);

        using var response = await RenameAsync(Clients.PortalName.ToUpperInvariant());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await _webApi.ReadAsync<string>(response, TestContext.Current.CancellationToken)).Should().BeEmpty();

        (await _portalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken)).Response.TenantAlias.Should().Be(Clients.PortalName);
    }

    [Fact]
    public async Task Rename_AliasOfAnotherPortal_ReturnsBadRequest()
    {
        var other = await Fixture.RegisterPortalAsync(new RegisterPortalModel
        {
            PortalName = FreePortalName(),
            FirstName = "Portal",
            LastName = "Owner",
            Email = Initializer.OwnerEmail,
            Password = Initializer.OwnerPassword,
            Language = "en-US",
            TimeZoneName = "UTC",
            SkipWelcome = true
        }, TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(Owner);

        using var response = await RenameAsync(other.Tenant.PortalName);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "the name is taken");

        (await _portalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken)).Response.TenantAlias.Should().Be(Clients.PortalName);
    }

    [Fact]
    public async Task Rename_TooShortAlias_ReturnsBadRequest()
    {
        await _webApiClient.Authenticate(Owner);

        // Shorter than the configured minimum of 3 (web:alias:min in buildtools/config/appsettings.json).
        using var response = await RenameAsync("ab");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, HttpStatusCode.BadRequest)]
    [InlineData(EmployeeType.RoomAdmin, HttpStatusCode.Forbidden)]
    [InlineData(EmployeeType.User, HttpStatusCode.Forbidden)]
    public async Task Rename_ByRole_NeedsThePortalSettingsRight(EmployeeType role, HttpStatusCode expected)
    {
        var member = await InviteContact(role);
        await _webApiClient.Authenticate(member);

        // An empty alias: a member with the right gets past the permission check to the 400, without a rename.
        using var response = await RenameAsync("");

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task Rename_FreeAlias_ReturnsASignInLinkUnderTheNewAddress()
    {
        await _webApiClient.Authenticate(Owner);

        var newAlias = FreePortalName();

        using var response = await RenameAsync(newAlias.ToUpperInvariant());

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var link = await _webApi.ReadAsync<string>(response, TestContext.Current.CancellationToken);

        link.Should().Contain(newAlias, "the alias is saved lower-cased").And.Contain("type=Auth");
    }
}
