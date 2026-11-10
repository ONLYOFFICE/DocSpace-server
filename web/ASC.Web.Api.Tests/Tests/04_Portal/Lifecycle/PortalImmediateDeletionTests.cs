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
/// DELETE api/2.0/portal/deleteportalimmediately — hidden from the API document (raw HTTP): the owner of a
/// portal whose address matches the installation's test-address pattern deletes it with an ordinary sign-in.
/// </summary>
[Trait("Category", "Portal")]
public class PortalImmediateDeletionTests(AspireAppFixture fixture) : PortalLifecycleTestBase(fixture)
{
    [Fact]
    public async Task DeletePortalImmediately_OwnerWithTheTestAddress_DeletesThePortalAndFreesItsName()
    {
        await _webApiClient.Authenticate(Owner);

        using var response = await _webApi.DeleteAsync("api/2.0/portal/deleteportalimmediately", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Not through Web.Api: the alias is renamed at once, and a server installation answers an address it does
        // not know as its default portal. The system API tells it: the old name is free to register again.
        var (status, body) = await ApiSystemAsync(HttpMethod.Post, "portal/validateportalname", new { portalName = Clients.PortalName });

        status.Should().Be(HttpStatusCode.OK, "the old name can be registered again");
        body.GetProperty("message").GetString().Should().Be("portalNameReadyToRegister");
    }

    [Fact]
    public async Task DeletePortalImmediately_DocSpaceAdmin_ReturnsAccessDenied()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        using var response = await _webApi.DeleteAsync("api/2.0/portal/deleteportalimmediately", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "only the owner may delete the portal");

        await _webApiClient.Authenticate(Owner);

        (await _portalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken)).Response.TenantId.Should().Be(TenantId);
    }
}
