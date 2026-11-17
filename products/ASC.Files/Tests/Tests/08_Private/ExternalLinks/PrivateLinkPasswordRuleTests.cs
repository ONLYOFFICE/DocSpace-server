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
/// "A link of a private item without a password is limited": only a link to one file with an expiration date may go
/// without a password, and the password itself never reaches the server.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkPasswordRuleTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    /// <summary>The SDK model of a link has no <c>passwordRequired</c> yet, so the list is read as JSON.</summary>
    private async Task<JsonElement> FileLinks(int fileId)
    {
        var raw = new RawApiClient(_filesClient);
        using var response = await raw.GetAsync($"api/2.0/files/file/{fileId}/links", TestContext.Current.CancellationToken);

        return await raw.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task FileLink_WithExpirationAndNoPassword_IsStored()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "open.docx");

        using var response = await PutFileLink(fileId, new LinkBody { PasswordRequired = false, ExpirationDate = InAWeek() });

        ((int)response.StatusCode).Should().Be(200);
        (await SharedLinkOf(response)).GetProperty("passwordRequired").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task FileLink_WithoutPasswordRequired_AsksForAPassword()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "default.docx");

        using var response = await PutFileLink(fileId, new LinkBody { PasswordRequired = null });

        ((int)response.StatusCode).Should().Be(200);
        (await SharedLinkOf(response)).GetProperty("passwordRequired").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task FileLink_WithoutExpirationAndPassword_IsRefused()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "forever.docx");

        using var response = await PutFileLink(fileId, new LinkBody { PasswordRequired = false });

        ((int)response.StatusCode).Should().Be(403);
        (await FileLinkCount(fileId)).Should().Be(0);
    }

    [Fact]
    public async Task FolderLink_WithoutPassword_IsRefused()
    {
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);

        using var response = await PutFolderLink(folderId, new LinkBody { PasswordRequired = false, ExpirationDate = InAWeek() });

        ((int)response.StatusCode).Should().Be(403);
        (await FolderLinkCount(folderId)).Should().Be(0);
    }

    [Fact]
    public async Task RoomLink_WithoutPassword_IsRefused()
    {
        var room = await CreateRoomAsOwner();

        using var response = await PutRoomLink(room.Id, new LinkBody { PasswordRequired = false, ExpirationDate = InAWeek() });

        ((int)response.StatusCode).Should().Be(403);
        (await RoomLinkCount(room.Id)).Should().Be(0);
    }

    [Fact]
    public async Task FileLink_ExpirationRemovedLater_IsRefusedAndKeepsItsDate()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "dated.docx");
        var linkId = Guid.NewGuid();

        using (var created = await PutFileLink(fileId, new LinkBody { LinkId = linkId, PasswordRequired = false, ExpirationDate = InAWeek() }))
        {
            ((int)created.StatusCode).Should().Be(200);
        }

        using var response = await PutFileLink(fileId, new LinkBody { LinkId = linkId, PasswordRequired = false }.WithoutKeys());

        ((int)response.StatusCode).Should().Be(403);
        var links = await FileLinks(fileId);
        links.GetArrayLength().Should().Be(1);
        links[0].GetProperty("sharedTo").GetProperty("expirationDate").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task FileLink_WithPassword_IsRefusedAndStoresNothing()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "secret.docx");

        using var response = await PutFileLink(fileId, new LinkBody { Password = "Never-sent-1" });

        ((int)response.StatusCode).Should().Be(400);
        (await FileLinkCount(fileId)).Should().Be(0);
    }
}
