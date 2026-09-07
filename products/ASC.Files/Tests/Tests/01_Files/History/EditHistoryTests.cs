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

namespace ASC.Files.Tests.Tests._01_Files.History;

/// <summary>GET /files/file/{fileId}/edit/history - edit history.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class EditHistoryTests(
    AspireAppFixture fixture)
    : HistoryTestBase(fixture)
{
    [Fact]
    public async Task GetEditHistory_Owner_GetsEditHistoryOfNewFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Edit History File", Owner);

        // Act
        var result = (await _filesApi.GetEditHistoryWithHttpInfoAsync(file.Id, TestContext.Current.CancellationToken)).Data;

        // Assert
        result.Response.Should().HaveCount(1);
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetEditHistory_EditHistoryEntry_HasCorrectStructure()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Edit History Structure", Owner);

        // Act
        var response = (await _filesApi.GetEditHistoryAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().HaveCount(1);

        var entry = response[0];
        entry.Id.Should().Be(file.Id);
        entry.Key.Should().NotBeNullOrEmpty();
        entry.Version.Should().Be(1);
        entry.VersionGroup.Should().Be(1);
        entry.User.Should().NotBeNull();
        entry.User.Id.Should().NotBeNullOrEmpty();
        entry.User.Name.Should().NotBeNullOrEmpty();
        entry.Created.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEditHistory_FileInRoom_HasEditHistory()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest Room For Edit History", "Autotest Room Edit History File");

        // Act
        var response = (await _filesApi.GetEditHistoryAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEditHistory_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetEditHistoryAsync(999999999, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetEditHistory_HistoryGrowsAfterVersionIncrement()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Edit History Versions", Owner);
        await BumpToSecondVersion(file.Id);

        // Act
        var response = (await _filesApi.GetEditHistoryAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task GetEditHistory_FileInArchivedRoom_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomWithFile("Autotest Edit History Archived Room", "Autotest Edit History Archived File");

        await ArchiveRoom(room.Id);

        // Act
        var response = (await _filesApi.GetEditHistoryAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().NotBeNull();
    }
}
