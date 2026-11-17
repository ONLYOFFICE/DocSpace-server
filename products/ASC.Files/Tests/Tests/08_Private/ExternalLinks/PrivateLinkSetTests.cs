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
/// "External link of a private item" and "Links of a private item need key material": a link with key material is stored on a
/// private room, folder or file, for reading only and by those who manage the room's links; every refusal stores
/// nothing.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkSetTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    public static TheoryData<RoomType> PrivateRoomTypes => [RoomType.CustomRoom, RoomType.EditingRoom, RoomType.VirtualDataRoom];

    [Fact]
    public async Task SetRoomLink_WithKeyMaterial_IsStoredWithItsPublicKey()
    {
        var room = await CreateRoomAsOwner();
        var body = new LinkBody();

        using var response = await PutRoomLink(room.Id, body);

        ((int)response.StatusCode).Should().Be(200);
        var link = await SharedLinkOf(response);
        link.GetProperty("linkPublicKey").GetString().Should().Be(body.LinkPublicKey);
        link.GetProperty("passwordRequired").GetBoolean().Should().BeTrue();
        (await RoomLinkCount(room.Id)).Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(PrivateRoomTypes))]
    public async Task SetFolderLink_WithKeyMaterial_IsStored(RoomType roomType)
    {
        var room = await CreateRoomAsOwner(roomType);
        var folderId = await CreateFolderIn(room.Id);

        using var response = await PutFolderLink(folderId, new LinkBody());

        ((int)response.StatusCode).Should().Be(200);
        (await FolderLinkCount(folderId)).Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(PrivateRoomTypes))]
    public async Task SetFileLink_WithKeyMaterial_IsStored(RoomType roomType)
    {
        var room = await CreateRoomAsOwner(roomType);
        var fileId = await UploadEncryptedFile(room.Id, "linked.docx");

        using var response = await PutFileLink(fileId, new LinkBody());

        ((int)response.StatusCode).Should().Be(200);
        (await FileLinkCount(fileId)).Should().Be(1);
    }

    [Fact]
    public async Task CreateFilePrimaryLink_WithKeyMaterial_IsStoredForReading()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "primary.pdf");

        using var response = await PostFilePrimaryLink(fileId, new LinkBody());

        ((int)response.StatusCode).Should().Be(200);
        var answer = await new RawApiClient(_filesClient).ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
        answer.GetProperty("access").GetInt32().Should().Be((int)FileShare.Read);
        answer.GetProperty("sharedTo").GetProperty("primary").GetBoolean().Should().BeTrue();
        (await FileLinkCount(fileId)).Should().Be(1);
    }

    [Fact]
    public async Task CreateFolderPrimaryLink_WithKeyMaterial_IsStored()
    {
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);

        using var response = await PostFolderPrimaryLink(folderId, new LinkBody());

        ((int)response.StatusCode).Should().Be(200);
        (await FolderLinkCount(folderId)).Should().Be(1);
    }

    [Fact]
    public async Task SetFileLink_InPrivateVirtualDataRoom_IsForDocSpaceUsersOnly()
    {
        var room = await CreateRoomAsOwner(RoomType.VirtualDataRoom);
        var fileId = await UploadEncryptedFile(room.Id, "vdr.docx");

        using var response = await PutFileLink(fileId, new LinkBody());

        ((int)response.StatusCode).Should().Be(200);
        (await SharedLinkOf(response)).GetProperty("internal").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task SetRoomLink_PastTheLimitOfACustomRoom_IsRefused()
    {
        var room = await CreateRoomAsOwner();

        for (var i = 0; i < 5; i++)
        {
            using var stored = await PutRoomLink(room.Id, new LinkBody());
            ((int)stored.StatusCode).Should().Be(200);
        }

        using var response = await PutRoomLink(room.Id, new LinkBody());

        ((int)response.StatusCode).Should().Be(403);
        (await RoomLinkCount(room.Id)).Should().Be(5);
    }

    [Theory]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.Review)]
    [InlineData(FileShare.Comment)]
    [InlineData(FileShare.FillForms)]
    public async Task SetFileLink_WithAnotherAccess_IsRefusedAndStoresNothing(FileShare access)
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "form.pdf");

        using var response = await PutFileLink(fileId, new LinkBody { Access = (int)access });

        ((int)response.StatusCode).Should().Be(403);
        (await FileLinkCount(fileId)).Should().Be(0);
    }

    [Fact]
    public async Task SetFileLink_WithDenyDownload_IsRefusedAndStoresNothing()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "deny.docx");

        using var response = await PutFileLink(fileId, new LinkBody { DenyDownload = true });

        ((int)response.StatusCode).Should().Be(403);
        (await FileLinkCount(fileId)).Should().Be(0);
    }

    [Fact]
    public async Task SetFileLink_ByAContentCreator_IsRefused()
    {
        var creator = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, creator, FileShare.ContentCreator);

        await _filesClient.Authenticate(creator);
        var fileId = await UploadEncryptedFile(room.Id, "own.docx");

        using var response = await PutFileLink(fileId, new LinkBody());

        ((int)response.StatusCode).Should().Be(403);

        await _filesClient.Authenticate(Owner);
        (await FileLinkCount(fileId)).Should().Be(0);
    }
}
