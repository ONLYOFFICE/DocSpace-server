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
/// <c>DELETE /people/{userId}</c> and <c>PUT /people/delete</c> while a reassignment of the account's data is still
/// running. The refusal was a plain <c>Exception</c>, which nothing maps, so a 500 came back; it answers 409 now.
/// The delete follows the start immediately, while the reassignment of a room and a file is still queued or
/// running, as <see cref="ReassignTerminateTests"/> relies on to cancel it.
/// </summary>
public class DeleteDuringReassignTests(AspireAppFixture fixture) : UserDataTestBase(fixture)
{
    [Fact]
    public async Task DeleteMember_ReassignRunning_Returns409()
    {
        // Arrange
        var target = await StartReassignOfTerminatedRoomAdminAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.DeleteMemberAsync(target.Id.ToString(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(409);
        exception.ErrorContent!.ToString().Should().Contain("is not complete.");
    }

    [Fact]
    public async Task RemoveUsers_ReassignRunning_Returns409()
    {
        // Arrange
        var target = await StartReassignOfTerminatedRoomAdminAsync();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.RemoveUsersAsync(
                new UpdateMembersRequestDto([target.Id]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(409);
        exception.ErrorContent!.ToString().Should().Contain("is not complete.");
    }

    /// <summary>A disabled room admin with a room and a file, whose data is being reassigned to the Owner.</summary>
    private async Task<User> StartReassignOfTerminatedRoomAdminAsync()
    {
        var (target, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Reassign Room", "Autotest Reassign File");
        await TerminateUser(target);

        await _peopleClient.Authenticate(Owner);
        await _userDataApi.StartReassignAsync(
            new StartReassignRequestDto(target.Id, Owner.Id),
            cancellationToken: TestContext.Current.CancellationToken);

        return target;
    }
}
