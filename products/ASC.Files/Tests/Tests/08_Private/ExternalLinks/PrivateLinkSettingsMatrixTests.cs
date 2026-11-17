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

namespace ASC.Files.Tests.Tests._08_Privacy.ExternalLinks;

/// <summary>
/// The private row of "Link settings matrix by location" (`shared-links`): a link of a private Collaboration, VDR or
/// Custom room keeps the "who has access" choice of its room type and accepts a lifetime. Read access, the password
/// and deny-download of that row are covered by <see cref="PrivateLinkSetTests"/> and
/// <see cref="PrivateLinkPasswordRuleTests"/>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkSettingsMatrixTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    public static TheoryData<RoomType, bool> FreeAudienceRooms => new MatrixTheoryData<RoomType, bool>(
        [RoomType.CustomRoom, RoomType.EditingRoom],
        [true, false]);

    private async Task<FileShareLink> OnlyFileLink(int fileId) =>
        (await _filesApi.GetFileLinksAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response.Single().SharedLink;

    private async Task<FileShareLink> OnlyFolderLink(int folderId) =>
        (await _foldersApi.GetFolderLinksAsync(folderId, TestContext.Current.CancellationToken)).Response.Single().SharedLink;

    private async Task<FileShareLink> OnlyRoomLink(int roomId) =>
        (await _roomsApi.GetRoomLinksAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response.Single().SharedLink;

    [Theory]
    [MemberData(nameof(FreeAudienceRooms))]
    public async Task FileLink_WhoHasAccess_IsStoredAsSent(RoomType roomType, bool @internal)
    {
        var room = await CreateRoomAsOwner(roomType);
        var fileId = await UploadEncryptedFile(room.Id, "audience.docx");

        using var response = await PutFileLink(fileId, new LinkBody { Internal = @internal });

        ((int)response.StatusCode).Should().Be(200);
        (await OnlyFileLink(fileId)).Internal.Should().Be(@internal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RoomLink_InACustomRoom_WhoHasAccessIsStoredAsSent(bool @internal)
    {
        var room = await CreateRoomAsOwner();

        using var response = await PutRoomLink(room.Id, new LinkBody { Internal = @internal });

        ((int)response.StatusCode).Should().Be(200);
        (await OnlyRoomLink(room.Id)).Internal.Should().Be(@internal);
    }

    [Fact]
    public async Task FolderLink_InAVirtualDataRoom_IsForDocSpaceUsersWhateverSent()
    {
        var room = await CreateRoomAsOwner(RoomType.VirtualDataRoom);
        var folderId = await CreateFolderIn(room.Id);

        using var response = await PutFolderLink(folderId, new LinkBody { Internal = false });

        ((int)response.StatusCode).Should().Be(200);
        (await OnlyFolderLink(folderId)).Internal.Should().BeTrue();
    }

    [Fact]
    public async Task RoomLink_WithALifetime_IsStored()
    {
        var room = await CreateRoomAsOwner();

        using var response = await PutRoomLink(room.Id, new LinkBody { ExpirationDate = InAWeek() });

        ((int)response.StatusCode).Should().Be(200);
        (await OnlyRoomLink(room.Id)).ExpirationDate.Should().NotBeNull();
    }

    [Fact]
    public async Task FolderLink_WithALifetime_IsStored()
    {
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);

        using var response = await PutFolderLink(folderId, new LinkBody { ExpirationDate = InAWeek() });

        ((int)response.StatusCode).Should().Be(200);
        (await OnlyFolderLink(folderId)).ExpirationDate.Should().NotBeNull();
    }
}
