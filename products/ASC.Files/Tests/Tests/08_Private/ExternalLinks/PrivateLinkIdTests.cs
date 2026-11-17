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
/// The id of a link of a private item: the key store also holds the file keys of users under their own ids, so a new
/// link takes only an id nobody holds, and a revocation touches only a link of the item it names.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkIdTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private async Task<(FolderDto Room, User Member, int FileId)> RoomWithAMemberKey()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, member, FileShare.Read);
        var fileId = await UploadEncryptedFile(room.Id, "keyed.docx");

        (await WriteKeys(fileId, member.Id)).Should().BeEmpty();
        (await GetMissing(room.Id, member.Id)).Should().BeEmpty();

        return (room, member, fileId);
    }

    private async Task<string?> PublicKeyOfOnlyLink(int fileId)
    {
        var links = await FileLinksJson(fileId);
        links.GetArrayLength().Should().Be(1);

        return links[0].GetProperty("sharedTo").GetProperty("linkPublicKey").GetString();
    }

    [Fact]
    public async Task RevokeLink_WithAUsersId_KeepsTheUsersFileKeys()
    {
        var (room, member, _) = await RoomWithAMemberKey();
        var folderId = await CreateFolderIn(room.Id);

        await RevokeFolderLink(folderId, member.Id);

        (await GetMissing(room.Id, member.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeLink_WithTheIdOfAnotherItemsLink_KeepsThatLinksKeys()
    {
        var room = await CreateRoomAsOwner();
        var linkedFileId = await UploadEncryptedFile(room.Id, "linked.docx");
        var otherFileId = await UploadEncryptedFile(room.Id, "other.docx");
        var body = new LinkBody();
        var linkId = await StoreFileLink(linkedFileId, body);

        await RevokeFileLink(otherFileId, linkId);

        (await PublicKeyOfOnlyLink(linkedFileId)).Should().Be(body.LinkPublicKey);
    }

    /// <summary>
    /// A revocation deletes the key material of the link: only then is its id free for a new link, which an id that still
    /// holds key material is not.
    /// </summary>
    [Fact]
    public async Task RevokeLink_DeletesItsKeyMaterial_SoItsIdIsFreeAgain()
    {
        var room = await CreateRoomAsOwner();
        var revokedFileId = await UploadEncryptedFile(room.Id, "revoked.docx");
        var otherFileId = await UploadEncryptedFile(room.Id, "other.docx");
        var linkId = await StoreFileLink(revokedFileId);

        await RevokeFileLink(revokedFileId, linkId);

        using var response = await PutFileLink(otherFileId, new LinkBody { LinkId = linkId });

        ((int)response.StatusCode).Should().Be(200);
        (await FileLinkCount(otherFileId)).Should().Be(1);
    }

    [Fact]
    public async Task NewLink_WithTheIdOfAnOrdinaryLink_IsRefused()
    {
        var ordinaryRoom = await CreateCustomRoom("Autotest ordinary room");
        var ordinaryLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(ordinaryRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink;
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "reused.docx");

        using var response = await PutFileLink(fileId, new LinkBody { LinkId = ordinaryLink.Id });

        ((int)response.StatusCode).Should().Be(400);
        (await FileLinkCount(fileId)).Should().Be(0);
    }

    [Fact]
    public async Task NewLink_WithAUsersId_IsRefusedAndKeepsTheUsersFileKeys()
    {
        var (room, member, fileId) = await RoomWithAMemberKey();

        using var response = await PutFileLink(fileId, new LinkBody { LinkId = member.Id });

        ((int)response.StatusCode).Should().Be(400);
        (await FileLinkCount(fileId)).Should().Be(0);
        (await GetMissing(room.Id, member.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task NewLink_WithTheIdOfAnotherItemsLink_IsRefusedAndKeepsThatLink()
    {
        var room = await CreateRoomAsOwner();
        var linkedFileId = await UploadEncryptedFile(room.Id, "linked.docx");
        var otherFileId = await UploadEncryptedFile(room.Id, "other.docx");
        var body = new LinkBody();
        var linkId = await StoreFileLink(linkedFileId, body);

        using var response = await PutFileLink(otherFileId, new LinkBody { LinkId = linkId });

        ((int)response.StatusCode).Should().Be(400);
        (await FileLinkCount(otherFileId)).Should().Be(0);
        (await PublicKeyOfOnlyLink(linkedFileId)).Should().Be(body.LinkPublicKey);
    }
}
