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
/// <c>GET /files/file/{fileId}/trackeditfile</c>. <c>FileStorageService.TrackEditFileAsync</c>
/// only compares <c>docKeyForTrack</c> against a locally computed document key and flips the
/// in-memory file tracker - it never contacts a document server, so the whole endpoint is
/// portable without one.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class EditingTrackTests(AspireAppFixture fixture) : EditingTestBase(fixture)
{
    /// <summary>
    /// Merges the TS suite's "Owner tracks editing" and "Owner tracks editing with
    /// docKeyForTrack" cases, which set up and assert exactly the same request.
    /// </summary>
    [Fact]
    public async Task TrackEditFile_Owner_ReturnsBooleanKey()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest TrackEdit Basic Room", "Autotest TrackEdit Basic File");
        var docKey = await GetDocKey(file.Id);

        // Act
        var result = (await _filesApi.TrackEditFileAsync(file.Id, docKeyForTrack: docKey, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TrackEditFile_IsFinishFalse_ReturnsBooleanKey()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest TrackEdit NotFinish Room", "Autotest TrackEdit NotFinish File");
        var docKey = await GetDocKey(file.Id);

        // Act
        var result = (await _filesApi.TrackEditFileAsync(file.Id, docKeyForTrack: docKey, isFinish: false, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TrackEditFile_IsFinishTrue_ReturnsBooleanKey()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest TrackEdit Finish Room", "Autotest TrackEdit Finish File");
        var docKey = await GetDocKey(file.Id);

        // Act
        var result = (await _filesApi.TrackEditFileAsync(file.Id, docKeyForTrack: docKey, isFinish: true, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task TrackEditFile_WithTabId_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest TrackEdit TabId Room", "Autotest TrackEdit TabId File");
        var docKey = await GetDocKey(file.Id);

        // Act
        var result = (await _filesApi.TrackEditFileAsync(file.Id, tabId: Guid.NewGuid(), docKeyForTrack: docKey, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Should().NotBeNull();
    }

    [Trait("Bug", "81219")]
    [Fact]
    public async Task TrackEditFile_NonExistentFile_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.TrackEditFileAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task TrackEditFile_ResponseStructure_HasKeyAndOptionalValue()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest TrackEdit Structure Room", "Autotest TrackEdit Structure File");
        var docKey = await GetDocKey(file.Id);

        // Act
        var result = (await _filesApi.TrackEditFileAsync(file.Id, docKeyForTrack: docKey, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert. Key reports whether the file is being actively edited;
        // FileStorageService.TrackEditFileAsync always returns string.Empty for Value.
        result.Should().NotBeNull();
        result.Key.Should().BeTrue();
        result.Value.Should().Be(string.Empty);
    }
}
