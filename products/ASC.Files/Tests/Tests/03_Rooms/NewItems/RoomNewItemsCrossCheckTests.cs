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

namespace ASC.Files.Tests.Tests._03_Rooms.NewItems;

/// <summary>
/// A room is also a folder, so <c>GET /files/rooms/{id}/news</c> and <c>GET /files/{id}/news</c>
/// address the same entity and must agree on what is new in it.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomNewItemsCrossCheckTests(
    AspireAppFixture fixture)
    : RoomNewItemsTestBase(fixture)
{
    [Fact]
    public async Task EmptyRoom_BothEndpointsReturnNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest News Cross Empty");

        // Act
        var roomNews = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var folderNews = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        FlattenItems(roomNews).Should().BeEmpty();
        folderNews.Should().BeEmpty();
    }

    [Fact]
    public async Task SameRoom_BothEndpointsReportTheSameEntry()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest News Cross Parity", FileShare.Read);
        await CreateFile("Autotest News Cross File.docx", room.Id);

        // Act
        await _filesClient.Authenticate(member);
        await PollNewsTitles(room.Id, t => t.Contains("Autotest News Cross File.docx"));

        var roomNews = (await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var folderNews = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        TitlesOf(roomNews).Should().Contain("Autotest News Cross File.docx");
        folderNews.Select(e => e.Title).Should().Contain("Autotest News Cross File.docx");
    }
}
