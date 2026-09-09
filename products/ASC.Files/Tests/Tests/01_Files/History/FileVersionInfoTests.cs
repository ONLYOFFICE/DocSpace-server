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

/// <summary>GET /files/file/{fileId}/history - version info.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileVersionInfoTests(
    AspireAppFixture fixture)
    : HistoryTestBase(fixture)
{
    [Fact]
    public async Task GetFileVersionInfo_NewFile_HasOneVersionInHistory()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File Version History", Owner);

        // Act
        var result = (await _filesApi.GetFileVersionInfoWithHttpInfoAsync(file.Id, TestContext.Current.CancellationToken)).Data;

        // Assert
        result.Response.Should().HaveCount(1);
        result.Count.Should().Be(1);
        result.Response[0].Version.Should().Be(1);
        result.Response[0].VersionGroup.Should().Be(1);
    }

    [Fact]
    public async Task GetFileVersionInfo_VersionItem_HasCorrectStructure()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File Version Structure", Owner);

        // Act
        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        var version = versions[0];
        version.Id.Should().Be(file.Id);
        version.Title.Should().Be("Autotest File Version Structure.docx");
        version.Version.Should().Be(1);
        version.VersionGroup.Should().Be(1);
        version.FolderId.Should().BePositive();
        version.FileExst.Should().Be(".docx");
        version.WebUrl.Should().NotBeNullOrEmpty();
        version.ViewUrl.Should().NotBeNullOrEmpty();
        version.CreatedBy.Should().NotBeNull();
        version.UpdatedBy.Should().NotBeNull();
        (version.Locked ?? false).Should().BeFalse();
        (version.Encrypted ?? false).Should().BeFalse();
    }

    [Fact]
    public async Task GetFileVersionInfo_FileInRoom_AlsoHasVersionHistory()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest Room For Version History", "Autotest Room File Version");

        // Act
        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        versions.Should().HaveCount(1);
        versions[0].Version.Should().Be(1);
        versions[0].Id.Should().Be(file.Id);
    }

    [Fact]
    public async Task GetFileVersionInfo_NonExistentFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileVersionInfoAsync(999999999, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileVersionInfo_FirstVersion_HasCommentCreatedAndFileStatusZero()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File Version Comment", Owner);

        // Act
        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        versions[0].Comment.Should().Be("Created");
        versions[0].FileStatus.Should().Be(FileStatus.None);
    }

    [Fact]
    public async Task GetFileInfo_WithVersion_ReturnsSpecificVersion()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest File Version Specific", Owner);
        await BumpToSecondVersion(file.Id);

        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        var firstVersion = versions.Min(v => v.Version);

        // Act
        var specificVersion = (await _filesApi.GetFileInfoAsync(file.Id, version: firstVersion, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        specificVersion.Id.Should().Be(file.Id);
        specificVersion.Version.Should().Be(firstVersion);
    }
}
