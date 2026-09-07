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
/// <c>GET /api/2.0/files/share</c> (<c>GetExternalShareData</c>) - resolving an external link's key
/// into room/file metadata. Access control for the same endpoint lives in
/// <see cref="ShareResolveLinkPermissionsTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class ShareResolveLinkTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    [Fact]
    public async Task GetExternalShareData_Room_ReturnsRoomMetadata()
    {
        var room = await CreateCustomRoom("Autotest External Share Data Room");

        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var requestToken = link.SharedLink.RequestToken;

        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.Should().NotBeNull();
        data.Status.Should().Be(Status.Ok);
        data.EntityId.Should().Be(room.Id.ToString());
        data.EntityTitle.Should().Be(room.Title);
        data.IsRoom.Should().BeTrue();
        data.Shared.Should().BeTrue();
        data.IsAuthenticated.Should().BeTrue();
        data.IsRoomMember.Should().BeFalse();
        data.LinkId.Should().NotBeEmpty();
        data.TenantId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetExternalShareData_File_EntityTypeIsFile()
    {
        var file = await CreateFileInMy("Autotest External Share Data File.docx", Owner);

        var link = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var requestToken = link.SharedLink.RequestToken;

        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, fileId: file.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.Status.Should().Be(Status.Ok);
        data.Type.Should().Be(FileEntryType.File);
        data.IsRoom.Should().NotBe(true);
        data.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task GetExternalShareData_RoomMember_IsRoomMemberTrue()
    {
        var room = await CreateCustomRoom("Autotest External Share Member Check");
        var user = await InviteContact(EmployeeType.User);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var requestToken = link.SharedLink.RequestToken;

        await _filesClient.Authenticate(user);
        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.IsAuthenticated.Should().BeTrue();
        data.IsRoomMember.Should().BeTrue();
    }

    [Fact]
    public async Task GetExternalShareData_NonMember_IsRoomMemberFalse()
    {
        var room = await CreateCustomRoom("Autotest External Share Non-Member Check");
        var user = await InviteContact(EmployeeType.User);

        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var requestToken = link.SharedLink.RequestToken;

        await _filesClient.Authenticate(user);
        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.IsAuthenticated.Should().BeTrue();
        data.IsRoomMember.Should().BeFalse();
    }

    [Fact]
    public async Task GetExternalShareData_PasswordProtectedLinkWithoutPassword_ReturnsRequiredPassword()
    {
        var room = await CreateCustomRoom("Autotest External Share Password Room");

        var existingLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var link = (await _roomsApi.SetRoomLinkAsync(room.Id, new RoomLinkRequest(
            existingLink.SharedLink.Id,
            FileShare.Read,
            title: "Password Protected Link",
            linkType: LinkType.External,
            password: "Secret123!",
            denyDownload: false), TestContext.Current.CancellationToken)).Response;
        var requestToken = link.SharedLink.RequestToken;

        await _filesClient.Authenticate(null);
        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.Status.Should().Be(Status.RequiredPassword);
    }

    [Fact]
    public async Task GetExternalShareData_NonExistentKey_ReturnsInvalid()
    {
        var data = (await _sharingApi.GetExternalShareDataAsync(
            "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", cancellationToken: TestContext.Current.CancellationToken)).Response;

        data.Status.Should().Be(Status.Invalid);
    }
}
