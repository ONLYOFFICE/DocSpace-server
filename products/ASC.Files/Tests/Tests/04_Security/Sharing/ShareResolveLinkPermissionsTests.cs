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

namespace ASC.Files.Tests.Tests._04_Security.Sharing;

/// <summary>
/// Access control for <c>GET /api/2.0/files/share</c> (<c>GetExternalShareData</c>): how the
/// caller's own identity (or lack of one) is reflected in the resolved link's
/// <c>isAuthenticated</c>/<c>isRoomMember</c> flags. Functional coverage lives in
/// <see cref="ShareResolveLinkTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class ShareResolveLinkPermissionsTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    private async Task<(FolderDtoInteger Room, string RequestToken)> CreateRoomWithPrimaryLink(string title)
    {
        var room = await CreateCustomRoom(title);
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return (room, link.SharedLink.RequestToken);
    }

    [Fact]
    public async Task GetExternalShareData_Owner_IsAuthenticatedTrue_IsRoomMemberFalse()
    {
        var (room, requestToken) = await CreateRoomWithPrimaryLink("Autotest External Share Owner Perm");

        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.IsAuthenticated.Should().BeTrue();
        data.IsRoomMember.Should().BeFalse();
    }

    /// <remarks>
    /// The TypeScript suite expects <c>isAuthenticated: false</c> here, which does not hold on this server:
    /// <c>ExternalLinkHelper.ValidateAsync</c> copies <c>SecurityContext.IsAuthenticated</c> into the response,
    /// so any signed-in caller — member or not — gets <c>true</c>. Only <c>isRoomMember</c> distinguishes the roles.
    /// </remarks>
    [Fact]
    public async Task GetExternalShareData_DocSpaceAdmin_NonMember_IsRoomMemberFalse()
    {
        var (room, requestToken) = await CreateRoomWithPrimaryLink("Autotest External Share DocSpaceAdmin Perm");
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(docSpaceAdmin);
        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.IsAuthenticated.Should().BeTrue();
        data.IsRoomMember.Should().BeFalse();
    }

    /// <remarks>
    /// The TypeScript suite expects <c>isAuthenticated: false</c> here, which does not hold on this server:
    /// <c>ExternalLinkHelper.ValidateAsync</c> copies <c>SecurityContext.IsAuthenticated</c> into the response,
    /// so any signed-in caller — member or not — gets <c>true</c>. Only <c>isRoomMember</c> distinguishes the roles.
    /// </remarks>
    [Fact]
    public async Task GetExternalShareData_RoomAdmin_NonMember_IsRoomMemberFalse()
    {
        var (room, requestToken) = await CreateRoomWithPrimaryLink("Autotest External Share RoomAdmin Perm");
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(roomAdmin);
        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.IsAuthenticated.Should().BeTrue();
        data.IsRoomMember.Should().BeFalse();
    }

    /// <remarks>
    /// The TypeScript suite expects <c>isAuthenticated: false</c> here, which does not hold on this server:
    /// <c>ExternalLinkHelper.ValidateAsync</c> copies <c>SecurityContext.IsAuthenticated</c> into the response,
    /// so any signed-in caller — member or not — gets <c>true</c>. Only <c>isRoomMember</c> distinguishes the roles.
    /// </remarks>
    [Fact]
    public async Task GetExternalShareData_User_NonMember_IsRoomMemberFalse()
    {
        var (room, requestToken) = await CreateRoomWithPrimaryLink("Autotest External Share User Non-Member Perm");
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(user);
        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.IsAuthenticated.Should().BeTrue();
        data.IsRoomMember.Should().BeFalse();
    }

    [Fact]
    public async Task GetExternalShareData_User_RoomMember_IsRoomMemberTrue()
    {
        var (room, requestToken) = await CreateRoomWithPrimaryLink("Autotest External Share User Member Perm");
        var user = await InviteContact(EmployeeType.User);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.IsAuthenticated.Should().BeTrue();
        data.IsRoomMember.Should().BeTrue();
    }

    /// <remarks>
    /// The TypeScript suite expects <c>isAuthenticated: false</c> here, which does not hold on this server:
    /// <c>ExternalLinkHelper.ValidateAsync</c> copies <c>SecurityContext.IsAuthenticated</c> into the response,
    /// so any signed-in caller — member or not — gets <c>true</c>. Only <c>isRoomMember</c> distinguishes the roles.
    /// </remarks>
    [Fact]
    public async Task GetExternalShareData_Guest_NonMember_IsRoomMemberFalse()
    {
        var (room, requestToken) = await CreateRoomWithPrimaryLink("Autotest External Share Guest Perm");
        var guest = await InviteGuest();

        await _filesClient.Authenticate(guest);
        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.IsAuthenticated.Should().BeTrue();
        data.IsRoomMember.Should().BeFalse();
    }

    [Fact]
    public async Task GetExternalShareData_Anonymous_PublicRoom_IsAuthenticatedFalse()
    {
        var room = await CreatePublicRoom("Autotest External Share Anon Perm");
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(null);
        var data = (await _sharingApi.GetExternalShareDataAsync(
            link.SharedLink.RequestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.IsAuthenticated.Should().BeFalse();
    }
}
