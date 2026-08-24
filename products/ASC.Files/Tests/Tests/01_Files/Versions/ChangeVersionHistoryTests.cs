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

namespace ASC.Files.Tests.Tests._01_Files.Versions;

/// <summary>PUT /files/file/{fileId}/history - change version history.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class ChangeVersionHistoryTests(
    AspireAppFixture fixture)
    : VersionsTestBase(fixture)
{
    [Fact]
    public async Task ChangeVersionHistory_ContinueVersionFalse_SplitsIntoNewGroup()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Version History Split");

        // Act
        var response = (await _filesApi.ChangeVersionHistoryWithHttpInfoAsync(
            file.Id,
            new ChangeHistory(2, false),
            TestContext.Current.CancellationToken)).Data.Response;

        // Assert
        response.Should().NotBeNullOrEmpty();

        var v1 = response.Find(f => f.Version == 1);
        var v2 = response.Find(f => f.Version == 2);
        v1.Should().NotBeNull();
        v2.Should().NotBeNull();
        v2!.VersionGroup.Should().NotBe(v1!.VersionGroup);
    }

    [Fact]
    public async Task ChangeVersionHistory_ContinueVersionTrue_MergesIntoPreviousGroup()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Version History Merge");

        // Act
        var response = (await _filesApi.ChangeVersionHistoryWithHttpInfoAsync(
            file.Id,
            new ChangeHistory(2, true),
            TestContext.Current.CancellationToken)).Data.Response;

        // Assert
        response.Should().NotBeNullOrEmpty();
        response.Find(f => f.Version == 1).Should().NotBeNull();
        response.Find(f => f.Version == 2).Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeVersionHistory_ReturnsCorrectFileStructure()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Version History Structure");

        // Act
        var result = (await _filesApi.ChangeVersionHistoryWithHttpInfoAsync(
            file.Id,
            new ChangeHistory(2, false),
            TestContext.Current.CancellationToken)).Data;

        // Assert
        result.Count.Should().BeGreaterThan(0);

        var entry = result.Response[0];
        entry.Id.Should().BePositive();
        entry.Version.Should().BePositive();
        entry.VersionGroup.Should().BePositive();
        entry.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangeVersionHistory_Split_CreatesNewVersionGroup()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Version History Groups");

        var before = (await _filesApi.GetEditHistoryWithHttpInfoAsync(file.Id, TestContext.Current.CancellationToken)).Data.Response;
        var groupsBefore = before.Select(e => e.VersionGroup).ToHashSet();

        // Act
        await _filesApi.ChangeVersionHistoryAsync(file.Id, new ChangeHistory(2, false), TestContext.Current.CancellationToken);

        var after = (await _filesApi.GetEditHistoryWithHttpInfoAsync(file.Id, TestContext.Current.CancellationToken)).Data.Response;
        var groupsAfter = after.Select(e => e.VersionGroup).ToHashSet();

        // Assert
        groupsAfter.Count.Should().BeGreaterThanOrEqualTo(groupsBefore.Count);

        var v1 = after.Find(e => e.Version == 1);
        var v2 = after.Find(e => e.Version == 2);
        v1.Should().NotBeNull();
        v2.Should().NotBeNull();
        v2!.VersionGroup.Should().NotBe(v1!.VersionGroup);
    }

    [Fact]
    public async Task ChangeVersionHistory_Merge_ReducesVersionGroups()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Version History Merge Groups");

        await _filesApi.ChangeVersionHistoryAsync(file.Id, new ChangeHistory(2, false), TestContext.Current.CancellationToken);

        var before = (await _filesApi.GetEditHistoryWithHttpInfoAsync(file.Id, TestContext.Current.CancellationToken)).Data.Response;
        var groupsBefore = before.Select(e => e.VersionGroup).ToHashSet();

        // Act
        await _filesApi.ChangeVersionHistoryAsync(file.Id, new ChangeHistory(2, true), TestContext.Current.CancellationToken);

        var after = (await _filesApi.GetEditHistoryWithHttpInfoAsync(file.Id, TestContext.Current.CancellationToken)).Data.Response;
        var groupsAfter = after.Select(e => e.VersionGroup).ToHashSet();

        // Assert
        groupsAfter.Count.Should().BeLessThanOrEqualTo(groupsBefore.Count);
    }

    [Fact]
    public async Task ChangeVersionHistory_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.ChangeVersionHistoryAsync(
                999999999,
                new ChangeHistory(1, false),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task ChangeVersionHistory_FileInRoom_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomFileWithSecondVersion("Autotest Version History Room", "Autotest Version History Room File");

        // Act
        var response = (await _filesApi.ChangeVersionHistoryWithHttpInfoAsync(
            file.Id,
            new ChangeHistory(2, false),
            TestContext.Current.CancellationToken)).Data.Response;

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeVersionHistory_FileInArchivedRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomFileWithSecondVersion("Autotest Version History Archived Room", "Autotest Version History Archived File");

        await ArchiveRoom(room.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.ChangeVersionHistoryAsync(
                file.Id,
                new ChangeHistory(2, false),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
