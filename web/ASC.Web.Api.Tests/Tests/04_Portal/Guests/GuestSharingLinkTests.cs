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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Guests;

/// <summary>
/// GET /api/2.0/people/guests/{userid}/share — the sharing link for a guest. Any privileged
/// actor (owner or DocSpace/room admin) who can see the guest can fetch it, regardless of who
/// invited whom.
/// </summary>
[Trait("Category", "Portal")]
public class GuestSharingLinkTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetGuestSharingLink_Owner_ForOwnGuest_ReturnsLink()
    {
        // Arrange
        var guest = await InviteGuest();

        // Act
        var link = await _portalGuestsApi.GetGuestSharingLinkWithHttpInfoAsync(guest.Id, TestContext.Current.CancellationToken);

        // Assert
        AssertIsGuestSharingLink(link);
    }

    [Fact]
    public async Task GetGuestSharingLink_Owner_ForGuestCreatedByDocSpaceAdmin_ReturnsLink()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var guest = await InviteGuest(admin);

        await _peopleClient.Authenticate(Owner);

        // Act
        var link = await _portalGuestsApi.GetGuestSharingLinkWithHttpInfoAsync(guest.Id, TestContext.Current.CancellationToken);

        // Assert
        AssertIsGuestSharingLink(link);
    }

    [Fact]
    public async Task GetGuestSharingLink_DocSpaceAdmin_ForGuestCreatedByOwner_ReturnsLink()
    {
        // Arrange
        var guest = await InviteGuest();

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        // Act
        var link = await _portalGuestsApi.GetGuestSharingLinkWithHttpInfoAsync(guest.Id, TestContext.Current.CancellationToken);

        // Assert
        AssertIsGuestSharingLink(link);
    }

    [Fact]
    public async Task GetGuestSharingLink_DocSpaceAdmin_ForOwnGuest_ReturnsLink()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var guest = await InviteGuest(admin);

        await _peopleClient.Authenticate(admin);

        // Act
        var link = await _portalGuestsApi.GetGuestSharingLinkWithHttpInfoAsync(guest.Id, TestContext.Current.CancellationToken);

        // Assert
        AssertIsGuestSharingLink(link);
    }

    [Fact]
    public async Task GetGuestSharingLink_RoomAdmin_ForOwnGuest_ReturnsLink()
    {
        // Arrange
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var guest = await InviteGuest(roomAdmin);

        await _peopleClient.Authenticate(roomAdmin);

        // Act
        var link = await _portalGuestsApi.GetGuestSharingLinkWithHttpInfoAsync(guest.Id, TestContext.Current.CancellationToken);

        // Assert
        AssertIsGuestSharingLink(link);
    }

    private static void AssertIsGuestSharingLink(ApiResponse<StringWrapper> link)
    {
        link.StatusCode.Should().Be(HttpStatusCode.OK);
        link.Data.Response.Should().NotBeNullOrEmpty();

        var uri = new Uri(link.Data.Response!, UriKind.Absolute);
        uri.AbsolutePath.Should().StartWith("/s/");
    }
}
