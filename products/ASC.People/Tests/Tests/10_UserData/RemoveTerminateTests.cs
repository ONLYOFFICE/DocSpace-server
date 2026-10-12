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

namespace ASC.People.Tests.Tests._10_UserData;

/// <summary>
/// <c>PUT /people/remove/terminate</c> - only the Owner and a DocSpaceAdmin may terminate a
/// deactivated member's data removal. The endpoint returns no response body, so a positive case
/// only needs to await the call without it throwing.
/// </summary>
public class RemoveTerminateTests(AspireAppFixture fixture) : UserDataTestBase(fixture)
{
    [Fact]
    public async Task TerminateRemove_AsOwner_Succeeds()
    {
        var (roomAdmin, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Remove Room", "Autotest Remove File");
        await TerminateUser(roomAdmin);

        await _peopleClient.Authenticate(Owner);
        await _userDataApi.StartRemoveAsync(new TerminateRequestDto(roomAdmin.Id), TestContext.Current.CancellationToken);

        await _userDataApi.TerminateRemoveAsync(new TerminateRequestDto(roomAdmin.Id), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TerminateRemove_AsDocSpaceAdmin_Succeeds()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var (roomAdmin, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Remove Room", "Autotest Remove File");
        await TerminateUser(roomAdmin);

        await _peopleClient.Authenticate(admin);
        await _userDataApi.StartRemoveAsync(new TerminateRequestDto(roomAdmin.Id), TestContext.Current.CancellationToken);

        await _userDataApi.TerminateRemoveAsync(new TerminateRequestDto(roomAdmin.Id), TestContext.Current.CancellationToken);
    }

    [Theory]
    [MemberData(nameof(NonAdminRoles))]
    public async Task TerminateRemove_AsNonAdminRole_ThrowsAccessDenied(EmployeeType type)
    {
        var (target, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Remove Room", "Autotest Remove File");
        await TerminateUser(target);

        await _peopleClient.Authenticate(Owner);
        await _userDataApi.StartRemoveAsync(new TerminateRequestDto(target.Id), TestContext.Current.CancellationToken);

        var actor = await InviteMember(type);
        await _peopleClient.Authenticate(actor);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.TerminateRemoveAsync(new TerminateRequestDto(target.Id), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task TerminateRemove_Anonymous_ThrowsUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.TerminateRemoveAsync(new TerminateRequestDto(Guid.Empty), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
