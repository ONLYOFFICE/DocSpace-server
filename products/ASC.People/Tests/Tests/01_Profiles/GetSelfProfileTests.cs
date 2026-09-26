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

namespace ASC.People.Tests.Tests._01_Profiles;

/// <summary>
/// <c>GET /people/@self</c> — every role can read its own profile; only a portal owner has a
/// personal folder created automatically for a brand-new portal, a guest never does.
/// </summary>
public class GetSelfProfileTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80818")]
    public async Task GetSelfProfile_Owner_ShouldReturnOwnerFlagsAndPersonalFolder()
    {
        var self = await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken);

        self.Response.FirstName.Should().Be("Portal");
        self.Response.LastName.Should().Be("Owner");
        self.Response.DisplayName.Should().Be("Portal Owner");
        self.Response.IsOwner.Should().BeTrue();
        self.Response.IsAdmin.Should().BeFalse();
        self.Response.Id.Should().Be(Owner.Id);
        self.Response.HasPersonalFolder.Should().BeTrue();
    }

    public static readonly TheoryData<EmployeeType> NonOwnerRoles = new() { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User };

    [Theory]
    [MemberData(nameof(NonOwnerRoles))]
    public async Task GetSelfProfile_NonOwnerContact_ShouldReturnOwnProfileWithPersonalFolder(EmployeeType actorType)
    {
        var actor = await InviteContact(actorType);
        await _peopleClient.Authenticate(actor);

        var self = await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken);

        self.Response.Id.Should().Be(actor.Id);
        self.Response.HasPersonalFolder.Should().BeTrue();

        switch (actorType)
        {
            case EmployeeType.DocSpaceAdmin:
                self.Response.IsAdmin.Should().BeTrue();
                break;
            case EmployeeType.RoomAdmin:
                self.Response.IsRoomAdmin.Should().BeTrue();
                break;
            case EmployeeType.User:
                self.Response.IsCollaborator.Should().BeTrue();
                break;
        }
    }

    [Fact]
    public async Task GetSelfProfile_Guest_ShouldReturnOwnProfileWithoutPersonalFolder()
    {
        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var self = await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken);

        self.Response.Id.Should().Be(guest.Id);
        self.Response.IsVisitor.Should().BeTrue();
        self.Response.HasPersonalFolder.Should().BeFalse();
    }
}
