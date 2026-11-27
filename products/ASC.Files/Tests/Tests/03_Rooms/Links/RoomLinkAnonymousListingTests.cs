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

namespace ASC.Files.Tests.Tests._03_Rooms.Links;

/// <summary>
/// A visitor who opens a public room through its link lists the files of the room. Each file is reached through the
/// room's link, not through a link of its own, so <c>external</c> is false for every file. A request keeps the room's
/// record found for one file for the next ones; before, the kept copy was given the id of each next file, so those files
/// read as having a link of their own. The listing builds the files in parallel (<c>files:folders-dto:parallelism</c>,
/// 5 by default), so only the files checked after one of them is resolved read the kept record: the room holds more
/// files than that.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLinkAnonymousListingTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    private const int FileCount = 12;

    [Fact]
    public async Task ListPublicRoomThroughItsLink_AnonymousVisitor_NoFileReadsAsHavingALinkOfItsOwn()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreatePublicRoom("Autotest Public Room Anonymous Listing");
        for (var i = 1; i <= FileCount; i++)
        {
            await CreateFile($"Autotest Anonymous Listing {i}.docx", room.Id);
        }

        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, link.SharedLink.RequestToken);
        string body;
        try
        {
            // the typed listing item has no `external`, so the answer is read as it comes
            body = (await _foldersApi.GetFolderByFolderIdWithHttpInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).RawContent;
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        }

        // Assert
        using var json = System.Text.Json.JsonDocument.Parse(body);
        var files = json.RootElement.GetProperty("response").GetProperty("files").EnumerateArray().ToList();
        files.Should().HaveCount(FileCount);
        files.Select(f => f.TryGetProperty("external", out var external) && external.ValueKind == System.Text.Json.JsonValueKind.True)
            .Should().AllBeEquivalentTo(false);
    }
}
