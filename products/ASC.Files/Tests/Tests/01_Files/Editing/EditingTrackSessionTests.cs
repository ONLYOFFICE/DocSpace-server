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

namespace ASC.Files.Tests.Tests._01_Files.Editing;

/// <summary>
/// <c>GET /files/file/{fileId}/trackeditfile</c> - the document key check and what <c>isFinish</c> does to the
/// editing mark of the tab it names.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class EditingTrackSessionTests(AspireAppFixture fixture) : EditingTestBase(fixture)
{
    [Fact]
    public async Task TrackEditFile_KeyOfAnotherRevision_ReturnsForbidden()
    {
        // Arrange - a key the file never had, the way a key of an older revision looks to the check
        var (_, file) = await CreateRoomWithFile("Autotest Track Stale Room", "Autotest Track Stale File.docx");
        var staleKey = await GetDocKey(file.Id) + "-stale";

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.TrackEditFileAsync(file.Id, docKeyForTrack: staleKey, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task TrackEditFile_IsFinishForTheClaimedTab_DropsTheEditingMark()
    {
        // Arrange - a claim to edit alone is held by the all-zero tab
        var (_, file) = await CreateRoomWithFile("Autotest Track Finish Room", "Autotest Track Finish File.docx");
        var key = (await _filesApi.StartEditFileAsync(file.Id, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken)).Response;
        (await Status(file.Id)).HasFlag(FileStatus.IsEditing).Should().BeTrue();

        // Act
        var result = (await _filesApi.TrackEditFileAsync(file.Id, Guid.Empty, key, true, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Key.Should().BeTrue();
        (await Status(file.Id)).HasFlag(FileStatus.IsEditing).Should().BeFalse();
    }

    [Fact]
    public async Task TrackEditFile_IsFinishForAnotherTab_KeepsTheEditingMark()
    {
        // Arrange
        var (_, file) = await CreateRoomWithFile("Autotest Track Other Tab Room", "Autotest Track Other Tab File.docx");
        var key = (await _filesApi.StartEditFileAsync(file.Id, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken)).Response;

        // Act
        await _filesApi.TrackEditFileAsync(file.Id, Guid.NewGuid(), key, true, TestContext.Current.CancellationToken);

        // Assert
        (await Status(file.Id)).HasFlag(FileStatus.IsEditing).Should().BeTrue();
    }

    private async Task<FileStatus> Status(int fileId)
    {
        var file = (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return file.FileStatus ?? FileStatus.None;
    }
}
