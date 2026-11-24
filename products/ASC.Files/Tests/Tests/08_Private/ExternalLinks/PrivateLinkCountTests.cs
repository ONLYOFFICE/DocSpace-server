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
/// An item of a private room reports how many working external links are set on it, to the room owner and the room
/// managers only: the links belong to those who manage the room's links. A revoked link is not counted, and any other
/// member gets no count. The SDK model has no <c>externalLinksCount</c> yet, so the file is read raw.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkCountTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private async Task<JsonElement> FileJson(int fileId)
    {
        var raw = new RawApiClient(_filesClient);
        using var response = await raw.GetAsync($"api/2.0/files/file/{fileId}", TestContext.Current.CancellationToken);
        ((int)response.StatusCode).Should().Be(200);

        return await raw.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);
    }

    private static int? CountIn(JsonElement file) =>
        file.TryGetProperty("externalLinksCount", out var count) && count.ValueKind == JsonValueKind.Number ? count.GetInt32() : null;

    [Fact]
    public async Task AManager_SeesTheWorkingLinks_AndAReaderSeesNoCount()
    {
        var manager = await InviteWithKeys(EmployeeType.RoomAdmin);
        var reader = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);
        await InviteToRoom(room.Id, reader, FileShare.Read);

        await _filesClient.Authenticate(Owner);
        var fileId = await UploadEncryptedFile(room.Id, "counted.docx");
        await StoreFileLink(fileId);
        await StoreFileLink(fileId);
        var revoked = await StoreFileLink(fileId);
        await RevokeFileLink(fileId, revoked);

        await _filesClient.Authenticate(manager);
        CountIn(await FileJson(fileId)).Should().Be(2);

        await _filesClient.Authenticate(Owner);
        CountIn(await FileJson(fileId)).Should().Be(2);

        await _filesClient.Authenticate(reader);
        var asReader = await FileJson(fileId);
        asReader.GetProperty("id").GetInt32().Should().Be(fileId);
        CountIn(asReader).Should().BeNull();
    }
}
