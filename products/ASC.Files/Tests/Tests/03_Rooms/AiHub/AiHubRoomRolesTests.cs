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

namespace ASC.Files.Tests.Tests._03_Rooms.AiHub;

/// <summary>
/// The roles an Instructions room offers a member (Room manager, Content creator, Editor, Viewer) and the ones it
/// refuses with 403 (Reviewer, Commentator, Form Filler), per <c>FileSecurity.AvailableRoomAccesses</c>. The member
/// is a Room admin, who may hold every role, so only the room type decides. The Knowledge room is not here: creating
/// one needs vectorization, which the integration-test stand has switched off (403 on create).
/// </summary>
[Trait("Category", "Rooms")]
public class AiHubRoomRolesTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Theory]
    [InlineData(FileShare.RoomManager, true)]
    [InlineData(FileShare.ContentCreator, true)]
    [InlineData(FileShare.Editing, true)]
    [InlineData(FileShare.Read, true)]
    [InlineData(FileShare.Review, false)]
    [InlineData(FileShare.Comment, false)]
    [InlineData(FileShare.FillForms, false)]
    public async Task SetRoomSecurity_InstructionsRoomRole_AcceptedOnlyForTheRoomTypeSet(FileShare access, bool accepted)
    {
        // Arrange
        var member = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync();

        // Act + Assert
        await AssertInvitation(roomId, member, access, accepted);
    }

    private async Task AssertInvitation(int roomId, User member, FileShare access, bool accepted)
    {
        if (accepted)
        {
            await ShareRoomAsync(roomId, member.Id, access);

            var info = (await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
            info.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id && s.Access == access);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await ShareRoomAsync(roomId, member.Id, access));
            exception.ErrorCode.Should().Be(403);

            var info = (await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
            info.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id);
        }
    }
}
