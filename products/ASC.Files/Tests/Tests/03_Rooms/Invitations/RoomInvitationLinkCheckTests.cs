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

namespace ASC.Files.Tests.Tests._03_Rooms.Invitations;

/// <summary>
/// <c>POST /authentication/confirm</c> for a room's invitation link: what the check answers and what it
/// does to the room, per kind of visitor.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomInvitationLinkCheckTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    private async Task<(int RoomId, string Key)> CreateRoomWithLinkAsync(User visitor)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Link Check Room");
        var link = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Editing, title: "Invite", linkType: LinkType.Invitation),
            TestContext.Current.CancellationToken)).Response;

        await _webApiClient.Authenticate(visitor);
        var fullLink = await _webApiClient.GetAsync(new Uri(link.SharedLink.ShareLink).PathAndQuery, TestContext.Current.CancellationToken);

        return (room.Id, HttpUtility.ParseQueryString(fullLink.RequestMessage?.RequestUri?.Query!)["key"]!);
    }

    private async Task<ValidationResult> CheckAsync(string key) =>
        (await _authenticationApi.CheckConfirmAsync(
            new CheckConfirmRequestDto(key, uiD: Owner.Id, type: ConfirmType.LinkInvite),
            TestContext.Current.CancellationToken)).Response.Result;

    private async Task<FileShareDto?> MemberOfRoomAsync(int roomId, Guid userId)
    {
        var shares = await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken);

        return shares.Response.FirstOrDefault(s => s.SharedToUser?.Id == userId);
    }

    [Fact]
    public async Task CheckConfirm_ExistingAccountNotInRoom_AnswersUserExistedAndAddsIt()
    {
        var user = await InviteMember(EmployeeType.User);
        var (roomId, key) = await CreateRoomWithLinkAsync(user);

        var result = await CheckAsync(key);

        result.Should().Be(ValidationResult.UserExisted);
        (await MemberOfRoomAsync(roomId, user.Id))!.Access.Should().Be(FileShare.Editing);
    }

    [Fact]
    public async Task CheckConfirm_ExistingMember_AnswersUserExistedAndKeepsItsAccess()
    {
        var user = await InviteMember(EmployeeType.User);
        var (roomId, key) = await CreateRoomWithLinkAsync(user);
        await InviteToRoom(roomId, user, FileShare.Read);

        var result = await CheckAsync(key);

        result.Should().Be(ValidationResult.UserExisted);
        (await MemberOfRoomAsync(roomId, user.Id))!.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task CheckConfirm_PreviouslyRemovedMember_AnswersUserExcluded()
    {
        var user = await InviteMember(EmployeeType.User);
        var (roomId, key) = await CreateRoomWithLinkAsync(user);
        await InviteToRoom(roomId, user, FileShare.Read);
        await InviteToRoom(roomId, user, FileShare.None);

        var result = await CheckAsync(key);

        result.Should().Be(ValidationResult.UserExcluded);
        (await MemberOfRoomAsync(roomId, user.Id)).Should().BeNull();
    }

    [Fact]
    public async Task CheckConfirm_ForeignKey_AnswersInvalid()
    {
        var user = await InviteMember(EmployeeType.User);
        await _webApiClient.Authenticate(user);

        var result = await CheckAsync("not-a-real-key");

        result.Should().Be(ValidationResult.Invalid);
    }
}
