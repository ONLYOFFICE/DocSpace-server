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
/// A working external link of a private room, or of a folder or file in it, is a recipient of file keys: the room's key
/// set lists its public key with <c>isLink</c>, <c>setEncryptionInfo</c> stores a wrap for it when it covers the file,
/// and <c>getRoomMissingFileKeys</c> names it for the encrypted files it covers and holds no wrap for. The SDK models do
/// not carry <c>isLink</c> yet, so the answers are read as JSON.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkFileKeysTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private async Task<JsonElement> RoomKeysJson(int roomId)
    {
        using var response = await SendRawPrivacyRoomRequest(HttpMethod.Get, $"api/2.0/privacyroom/{roomId}/access");
        ((int)response.StatusCode).Should().Be(200);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("response");
    }

    private async Task<JsonElement> MissingJson(int roomId)
    {
        using var response = await SendRawPrivacyRoomRequest(HttpMethod.Get, $"api/2.0/privacyroom/{roomId}/access/missing");
        ((int)response.StatusCode).Should().Be(200);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("response");
    }

    private static JsonElement? EntryOf(JsonElement list, Guid id) =>
        list.EnumerateArray().Cast<JsonElement?>().FirstOrDefault(e => e!.Value.GetProperty("userId").GetGuid() == id);

    private static List<int> FileIdsOf(JsonElement entry) => [.. entry.GetProperty("fileIds").EnumerateArray().Select(f => f.GetInt32())];

    [Fact]
    public async Task RoomKeys_ListAWorkingLink_WithItsPublicKeyAndNoPrivateHalf_UntilItIsRevoked()
    {
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        var body = new LinkBody();
        var linkId = await StoreFolderLink(folderId, body);

        var listed = EntryOf(await RoomKeysJson(room.Id), linkId);
        await RevokeFolderLink(folderId, linkId);
        var afterRevoke = EntryOf(await RoomKeysJson(room.Id), linkId);

        listed.Should().NotBeNull();
        listed!.Value.GetProperty("isLink").GetBoolean().Should().BeTrue();
        listed.Value.GetProperty("id").GetGuid().Should().Be(linkId);
        listed.Value.GetProperty("publicKey").GetString().Should().Be(body.LinkPublicKey);
        (listed.Value.TryGetProperty("privateKeyEnc", out var privateHalf) && privateHalf.ValueKind != JsonValueKind.Null)
            .Should().BeFalse("a link entry never carries a private half");

        afterRevoke.Should().BeNull();
    }

    [Fact]
    public async Task RoomKeys_LeaveOutAnExpiredLink()
    {
        var room = await CreateRoomAsOwner();
        var working = await StoreFileLink(await UploadEncryptedFile(room.Id, "working.docx"), new LinkBody { PasswordRequired = false, ExpirationDate = InAWeek() });
        var expiringFile = await UploadEncryptedFile(room.Id, "expiring.docx");
        var soon = DateTime.UtcNow.AddSeconds(20).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        var expiring = await StoreFileLink(expiringFile, new LinkBody { PasswordRequired = false, ExpirationDate = soon });

        var whileWorking = EntryOf(await RoomKeysJson(room.Id), expiring);

        // The link expires on the server's clock; read until it is gone or a deadline well past the expiration.
        var deadline = DateTime.UtcNow.AddSeconds(60);
        JsonElement keys;
        while (true)
        {
            keys = await RoomKeysJson(room.Id);
            if (EntryOf(keys, expiring) == null || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }

        whileWorking.Should().NotBeNull();
        EntryOf(keys, working).Should().NotBeNull();
        EntryOf(keys, expiring).Should().BeNull();
    }

    [Fact]
    public async Task Missing_NamesTheLinkForTheFilesUnderItsFolderOnly_UntilTheWrapIsWritten()
    {
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        var inside = await UploadEncryptedFile(folderId, "inside.docx");
        var outside = await UploadEncryptedFile(room.Id, "outside.docx");
        var linkId = await StoreFolderLink(folderId);

        var before = EntryOf(await MissingJson(room.Id), linkId);
        var skipped = await WriteKeys(inside, linkId);
        var after = EntryOf(await MissingJson(room.Id), linkId);

        before.Should().NotBeNull();
        before!.Value.GetProperty("isLink").GetBoolean().Should().BeTrue();
        FileIdsOf(before.Value).Should().Equal(inside).And.NotContain(outside);
        skipped.Should().BeEmpty();
        after.Should().BeNull("the link holds a wrap for every file it covers now");
    }

    [Fact]
    public async Task Missing_NamesALinkListedInUserIds()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "listed.docx");
        var linkId = await StoreFileLink(fileId);

        var missing = await GetMissing(room.Id, linkId);

        missing.Should().ContainSingle(m => m.UserId == linkId).Which.FileIds.Should().Equal(fileId);
    }

    [Fact]
    public async Task SetEncryptionInfo_StoresWrapsForLinksOfTheFile_AFolderAbove_AndTheRoom()
    {
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        var fileId = await UploadEncryptedFile(folderId, "covered.docx");
        var fileLink = await StoreFileLink(fileId);
        var folderLink = await StoreFolderLink(folderId);
        using var roomResponse = await PutRoomLink(room.Id, new LinkBody());
        ((int)roomResponse.StatusCode).Should().Be(200);
        var roomLink = (await SharedLinkOf(roomResponse)).GetProperty("id").GetGuid();

        var before = await GetMissing(room.Id, fileLink, folderLink, roomLink);
        var skipped = await WriteKeys(fileId, fileLink, folderLink, roomLink);
        var after = await GetMissing(room.Id, fileLink, folderLink, roomLink);

        before.Select(m => m.UserId).Should().BeEquivalentTo([fileLink, folderLink, roomLink]);
        skipped.Should().BeEmpty();
        after.Should().BeEmpty();
    }

    [Fact]
    public async Task SetEncryptionInfo_SkipsALinkOfAnotherItem_AndARevokedLink()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "target.docx");
        var otherFolder = await CreateFolderIn(room.Id);
        var otherLink = await StoreFolderLink(otherFolder);
        var ownLink = await StoreFileLink(fileId);
        var revokedFile = await UploadEncryptedFile(room.Id, "revoked.docx");
        var revokedLink = await StoreFileLink(revokedFile);
        await RevokeFileLink(revokedFile, revokedLink);

        var skipped = await WriteKeys(fileId, ownLink, otherLink, revokedLink);

        skipped.Should().BeEquivalentTo([otherLink, revokedLink]);
        (await GetMissing(room.Id, ownLink)).Should().BeEmpty("the wrap for the file's own link was stored");
    }
}
