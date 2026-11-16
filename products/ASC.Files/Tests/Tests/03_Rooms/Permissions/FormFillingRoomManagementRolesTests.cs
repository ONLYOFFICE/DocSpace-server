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

namespace ASC.Files.Tests.Tests._03_Rooms.Permissions;

/// <summary>
/// Room management of a Form filling room by role: only a Room manager edits the room (title, spreadsheet
/// collection and database sync switches), invites, re-roles and removes members and creates room links; a
/// Content creator and a Form Filler get 403 for each and the room stays as it was. Every role reads the room
/// info, the member list and the history. A Room manager is a Room admin, the other roles are held by Users.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "FormFilling")]
public class FormFillingRoomManagementRolesTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>Form filling room role of the acting member and whether it manages the room.</summary>
    public static TheoryData<FileShare, bool> ManageByRole => new()
    {
        { FileShare.RoomManager, true },
        { FileShare.ContentCreator, false },
        { FileShare.FillForms, false }
    };

    [Theory]
    [MemberData(nameof(ManageByRole))]
    public async Task UpdateRoom_FfrMemberRole_OnlyRoomManagerRenamesAndSwitchesFormSettings(FileShare access, bool allowed)
    {
        // Arrange
        var (room, member) = await CreateFfrWithMember(access, $"Autotest FFR Update {access}");
        var newTitle = $"Autotest FFR Updated by {access}";
        var toExternalDb = room.SendFormToExternalDB != true;
        var toXlsx = room.SaveFormAsXLSX != true;
        var request = new UpdateRoomRequest(newTitle, sendFormToExternalDB: toExternalDb, saveFormAsXLSX: toXlsx);

        // Act
        await _filesClient.Authenticate(member);

        if (allowed)
        {
            await _roomsApi.UpdateRoomAsync(room.Id, request, TestContext.Current.CancellationToken);

            // Assert
            var updated = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
            updated.Title.Should().Be(newTitle);
            updated.SendFormToExternalDB.Should().Be(toExternalDb);
            updated.SaveFormAsXLSX.Should().Be(toXlsx);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.UpdateRoomAsync(room.Id, request, TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(403);
            await _filesClient.Authenticate(Owner);
            var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
            info.Title.Should().Be(room.Title);
            info.SendFormToExternalDB.Should().Be(room.SendFormToExternalDB);
            info.SaveFormAsXLSX.Should().Be(room.SaveFormAsXLSX);
        }
    }

    /// <summary>
    /// Inviting a new user, changing the role of a member and removing a member all go through
    /// <c>PUT rooms/{id}/share</c>: a Room manager does all three, a Content creator and a Form Filler are
    /// refused each time.
    /// </summary>
    [Theory]
    [MemberData(nameof(ManageByRole))]
    public async Task SetRoomSecurity_FfrMemberRole_OnlyRoomManagerInvitesChangesRoleAndRemoves(FileShare access, bool allowed)
    {
        // Arrange
        var (room, member) = await CreateFfrWithMember(access, $"Autotest FFR Share {access}");
        var newcomer = await InviteMember(EmployeeType.User);
        var existing = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, existing, FileShare.FillForms);

        // Act
        await _filesClient.Authenticate(member);

        if (allowed)
        {
            await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(newcomer, FileShare.FillForms), cancellationToken: TestContext.Current.CancellationToken);
            await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(existing, FileShare.ContentCreator), cancellationToken: TestContext.Current.CancellationToken);
            var afterChange = await GetMemberAccesses(room.Id);
            await _filesClient.Authenticate(member);
            await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(existing, FileShare.None), cancellationToken: TestContext.Current.CancellationToken);
            var afterRemove = await GetMemberAccesses(room.Id);

            // Assert
            afterChange.Should().Contain(newcomer.Id, FileShare.FillForms);
            afterChange.Should().Contain(existing.Id, FileShare.ContentCreator);
            afterRemove.Should().NotContainKey(existing.Id);
        }
        else
        {
            var invite = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(newcomer, FileShare.FillForms), cancellationToken: TestContext.Current.CancellationToken));
            var change = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(existing, FileShare.ContentCreator), cancellationToken: TestContext.Current.CancellationToken));
            var remove = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(existing, FileShare.None), cancellationToken: TestContext.Current.CancellationToken));

            // Assert
            invite.ErrorCode.Should().Be(403);
            change.ErrorCode.Should().Be(403);
            remove.ErrorCode.Should().Be(403);

            var accesses = await GetMemberAccesses(room.Id);
            accesses.Should().NotContainKey(newcomer.Id);
            accesses.Should().Contain(existing.Id, FileShare.FillForms);
        }
    }

    public static TheoryData<FileShare> FfrMemberRoles => new()
    {
        FileShare.RoomManager,
        FileShare.ContentCreator,
        FileShare.FillForms
    };

    [Theory]
    [MemberData(nameof(FfrMemberRoles))]
    public async Task FfrMemberRole_ReadsRoomInfoMemberListAndHistory(FileShare access)
    {
        // Arrange
        var (room, member) = await CreateFfrWithMember(access, $"Autotest FFR Read {access}");

        // Act
        await _filesClient.Authenticate(member);
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Id.Should().Be(room.Id);
        info.RoomType.Should().Be(RoomType.FillingFormsRoom);
        shares.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id);
        shares.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == Owner.Id);
        history.Should().NotBeNull();
    }

    /// <summary>
    /// The link side of the role split. A Form filling room has exactly one external link, created with the
    /// room, so managing links means editing that primary link: a Room manager renames it, a Content creator and
    /// a Form Filler are refused and the title stays.
    /// </summary>
    [Theory]
    [MemberData(nameof(ManageByRole))]
    public async Task SetRoomLink_FfrMemberRole_OnlyRoomManagerEditsPrimaryLink(FileShare access, bool allowed)
    {
        // Arrange
        var (room, member) = await CreateFfrWithMember(access, $"Autotest FFR Link {access}");
        var primary = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var newTitle = $"Autotest FFR Link by {access}";
        var request = new RoomLinkRequest(
            linkId: primary.SharedLink.Id,
            access: FileShare.FillForms,
            title: newTitle,
            linkType: LinkType.External);

        // Act
        await _filesClient.Authenticate(member);

        if (allowed)
        {
            var link = (await _roomsApi.SetRoomLinkAsync(room.Id, request, TestContext.Current.CancellationToken)).Response;

            // Assert
            link.SharedLink.Id.Should().Be(primary.SharedLink.Id);
            link.SharedLink.Title.Should().Be(newTitle);
            link.Access.Should().Be(FileShare.FillForms);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.SetRoomLinkAsync(room.Id, request, TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(403);
            await _filesClient.Authenticate(Owner);
            var after = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, TestContext.Current.CancellationToken)).Response;
            after.SharedLink.Title.Should().Be(primary.SharedLink.Title);
        }
    }

    private async Task<Dictionary<Guid, FileShare?>> GetMemberAccesses(int roomId)
    {
        await _filesClient.Authenticate(Owner);
        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return shares
            .Where(s => s.SharedToUser is not null)
            .ToDictionary(s => s.SharedToUser.Id, s => s.Access);
    }

    private async Task<(FolderDto Room, User Member)> CreateFfrWithMember(FileShare access, string title)
    {
        var member = await InviteMember(access == FileShare.RoomManager ? EmployeeType.RoomAdmin : EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom(title);
        await InviteToRoom(room.Id, member, access);

        return (room, member);
    }
}
