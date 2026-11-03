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
/// Listing a folder's contents (<c>GET /files/{folderId}</c>) must not clear an individual file's New mark.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomNewItemsFolderBrowseTests(
    AspireAppFixture fixture)
    : RoomNewItemsTestBase(fixture)
{
    /// <summary>
    /// Confirmed with the product (2026-10-30) as not the intended design, although it matches the documented
    /// contract of <c>GET /files/{folderId}</c>: a New badge is meant to clear once the file itself is opened in
    /// the editor, listing the folder to pick a file for another action is not enough. This test deliberately
    /// never calls <c>getRoomInfo</c> after the file is created, to isolate the folder-listing mechanism alone.
    /// </summary>
    [Fact]
    [Trait("Bug", "84797")]
    public async Task GetFolder_ByMemberWithoutOpeningTheFile_KeepsTheFilesNewMark()
    {
        // Arrange
        var (room, member) = await CreateRoomWithVisitor("Autotest News Folder Browse", FileShare.Read);
        await CreateFile("Autotest News Folder Browse File.docx", room.Id);

        await _filesClient.Authenticate(member);
        var before = await PollNewsTitles(room.Id, t => t.Contains("Autotest News Folder Browse File.docx"));
        before.Should().Contain("Autotest News Folder Browse File.docx");

        // Act - list the folder's contents without opening the file
        await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var after = TitlesOf((await _roomsApi.GetNewRoomItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response);
        after.Should().Contain("Autotest News Folder Browse File.docx");
    }
}
