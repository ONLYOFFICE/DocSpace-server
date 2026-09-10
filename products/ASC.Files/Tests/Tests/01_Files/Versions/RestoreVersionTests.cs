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

/// <summary>POST /files/file/{fileId}/restoreversion - restore a previous file version.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class RestoreVersionTests(
    AspireAppFixture fixture)
    : VersionsTestBase(fixture)
{
    [Fact]
    public async Task RestoreVersion_Owner_RestoresPreviousVersion()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore Version");

        // Act
        var response = (await _filesApi.RestoreFileVersionWithHttpInfoAsync(
            file.Id,
            version: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Data.Response;

        // Assert
        response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RestoreVersion_AddsNewEntryToEditHistory()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore Grows History");

        var before = (await _filesApi.GetEditHistoryWithHttpInfoAsync(file.Id, TestContext.Current.CancellationToken)).Data.Response;
        var countBefore = before.Count;

        // Act
        var after = (await _filesApi.RestoreFileVersionWithHttpInfoAsync(
            file.Id,
            version: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Data.Response;

        // Assert
        after.Count.Should().BeGreaterThan(countBefore);
    }

    [Fact]
    public async Task RestoreVersion_ReturnsCorrectEditHistoryStructure()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore Structure");

        // Act
        var response = (await _filesApi.RestoreFileVersionWithHttpInfoAsync(
            file.Id,
            version: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Data.Response;

        // Assert
        var entry = response[0];
        entry.Id.Should().BePositive();
        entry.Key.Should().NotBeNullOrEmpty();
        entry.Version.Should().BePositive();
        entry.VersionGroup.Should().BePositive();
        entry.User.Should().NotBeNull();
        entry.User.Id.Should().NotBeNullOrEmpty();
        entry.Created.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreVersion_FileInRoom_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomFileWithSecondVersion("Autotest Restore Version Room", "Autotest Restore Version Room File");

        // Act
        var response = (await _filesApi.RestoreFileVersionWithHttpInfoAsync(
            file.Id,
            version: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Data.Response;

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreVersion_MissingVersionAndUrl_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore No Params");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(
                file.Id,
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task RestoreVersion_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(
                999999999,
                version: 1,
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task RestoreVersion_FileInArchivedRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomFileWithSecondVersion("Autotest Restore Version Archived Room", "Autotest Restore Version Archived File");

        await ArchiveRoom(room.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(
                file.Id,
                version: 1,
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// The route only supports POST; a raw GET must be rejected outright rather than silently
    /// performing the restore. A GET is trivial to trigger via CSRF (an image tag, a prefetched
    /// link) in a way a POST is not, so this is checked at the transport level rather than through
    /// the generated client, which only ever issues a POST.
    /// </summary>
    /// <remarks>
    /// Bug 78027: kept as a regression guard for a report of GET silently restoring a version. The
    /// action is declared <c>[HttpPost("file/{fileId}/restoreversion")]</c> only, so ASP.NET's
    /// routing rejects a GET before <c>FilesController.RestoreFileVersion</c> ever runs; the trait
    /// stays on the test for traceability.
    /// </remarks>
    [Trait("Bug", "78027")]
    [Fact]
    public async Task RestoreVersion_GetVerb_IsRejectedNotSilentlyApplied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore CSRF GET");

        var countBefore = (await _filesApi.GetEditHistoryAsync(file.Id, TestContext.Current.CancellationToken)).Response.Count;

        // Act
        using var response = await _filesClient.GetAsync(
            $"api/2.0/files/file/{file.Id}/restoreversion?version=1&doc=",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);

        var countAfter = (await _filesApi.GetEditHistoryAsync(file.Id, TestContext.Current.CancellationToken)).Response.Count;
        countAfter.Should().Be(countBefore, "a rejected GET must not have restored the version as a side effect");
    }
}
