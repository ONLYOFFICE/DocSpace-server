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

namespace ASC.Files.Tests.Tests._01_Files.EditDiff;

/// <summary>GET /files/file/{fileId}/edit/diff - functional behaviour.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class GetEditDiffTests(
    AspireAppFixture fixture)
    : EditDiffTestBase(fixture)
{
    [Fact]
    public async Task GetEditDiff_LatestVersion_ReturnsDiffUrl()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Edit Diff URL", Owner);

        // Act
        var diff = (await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        diff.Key.Should().NotBeNullOrEmpty();
        diff.Url.Should().NotBeNullOrEmpty();
        // The API reports version 0 when no version parameter is specified, meaning "latest".
        diff.@Version.Should().Be(0);
        diff.FileType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEditDiff_SpecificVersion_ReturnsMatchingVersion()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Edit Diff URL Specific Version", Owner);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 2 }, TestContext.Current.CancellationToken);

        // Act
        var diff = (await _filesApi.GetEditDiffUrlAsync(file.Id, 1, TestContext.Current.CancellationToken)).Response;

        // Assert
        diff.@Version.Should().Be(1);
        diff.Key.Should().NotBeNullOrEmpty();
        diff.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEditDiff_VersionBumpedThroughApi_HasNoPreviousEditHistory()
    {
        // Arrange
        // UpdateFile with lastVersion only increments version metadata; it does not record real
        // editing history, which only the document editor produces via Document Server.
        var file = await CreateFileInMy("Autotest Edit Diff URL With Previous", Owner);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 2 }, TestContext.Current.CancellationToken);

        // Act
        var diff = (await _filesApi.GetEditDiffUrlAsync(file.Id, 2, TestContext.Current.CancellationToken)).Response;

        // Assert
        diff.@Version.Should().Be(2);
        diff.Key.Should().NotBeNullOrEmpty();
        diff.Url.Should().NotBeNullOrEmpty();
        diff.Previous.Should().BeNull();
    }

    [Fact]
    public async Task GetEditDiff_FirstVersion_HasNoPreviousVersionData()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Edit Diff URL No Previous", Owner);

        // Act
        var diff = (await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        diff.@Version.Should().Be(0);
        diff.Previous.Should().BeNull();
    }

    [Fact]
    public async Task GetEditDiff_NonExistentFileId_Returns404()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetEditDiffUrlAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetEditDiff_FileInRoom_ReturnsDiffUrl()
    {
        // Arrange
        var (_, file) = await CreateRoomWithFile("Autotest Edit Diff URL Room", "Autotest Edit Diff URL Room File");

        // Act
        var diff = (await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        diff.Key.Should().NotBeNullOrEmpty();
        diff.Url.Should().NotBeNullOrEmpty();
        diff.FileType.Should().NotBeNullOrEmpty();
    }
}
