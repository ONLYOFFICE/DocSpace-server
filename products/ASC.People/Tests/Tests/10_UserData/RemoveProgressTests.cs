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
/// <c>GET /people/remove/progress/{userid}</c> - only the Owner and a DocSpaceAdmin may check a
/// deactivated member's data-removal progress.
/// </summary>
public class RemoveProgressTests(AspireAppFixture fixture) : UserDataTestBase(fixture)
{
    [Fact]
    public async Task GetRemoveProgress_AsOwner_ReturnsProgress()
    {
        var (roomAdmin, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Remove Room", "Autotest Remove File");
        await TerminateUser(roomAdmin);

        await _peopleClient.Authenticate(Owner);
        await _userDataApi.StartRemoveAsync(new TerminateRequestDto(roomAdmin.Id), TestContext.Current.CancellationToken);

        var progress = (await _userDataApi.GetRemoveProgressAsync(roomAdmin.Id, TestContext.Current.CancellationToken)).Response;

        progress.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRemoveProgress_AsDocSpaceAdmin_ReturnsProgress()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var (roomAdmin, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Remove Room", "Autotest Remove File");
        await TerminateUser(roomAdmin);

        await _peopleClient.Authenticate(admin);
        await _userDataApi.StartRemoveAsync(new TerminateRequestDto(roomAdmin.Id), TestContext.Current.CancellationToken);

        var progress = (await _userDataApi.GetRemoveProgressAsync(roomAdmin.Id, TestContext.Current.CancellationToken)).Response;

        progress.Error.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(NonAdminRoles))]
    public async Task GetRemoveProgress_AsNonAdminRole_ThrowsAccessDenied(EmployeeType type)
    {
        var (target, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Remove Room", "Autotest Remove File");

        var actor = await InviteMember(type);
        await _peopleClient.Authenticate(actor);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.GetRemoveProgressAsync(target.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetRemoveProgress_Anonymous_ThrowsUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.GetRemoveProgressAsync(Guid.Empty, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
