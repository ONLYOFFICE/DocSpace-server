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

namespace ASC.Files.Tests.Tests._01_Files.Update;

/// <summary>
/// Title-normalisation and edge-case behaviour of <c>PUT /files/file/:fileId</c>. Basic rename
/// and the 165-character length limit are covered by <see cref="FileUpdateTests"/>.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileRenameTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateFile_TitleWithoutExtension_KeepsOriginalExtension()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest No Ext Original.docx", Owner);

        // Act
        var updated = (await _filesApi.UpdateFileAsync(
            file.Id, new UpdateFile { Title = "Autotest No Ext Renamed" }, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Autotest No Ext Renamed.docx");
        updated.FileExst.Should().Be(".docx");
    }

    [Fact]
    public async Task UpdateFile_TitleWithMatchingExtension_UpdatesCorrectly()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest With Ext Original.docx", Owner);

        // Act
        var updated = (await _filesApi.UpdateFileAsync(
            file.Id, new UpdateFile { Title = "Autotest With Ext Renamed.docx" }, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Autotest With Ext Renamed.docx");
        updated.FileExst.Should().Be(".docx");
    }

    /// <summary>
    /// BUG 80774: updating a non-existent file used to fail with the wrong status/message.
    /// Now correctly reports 404 with the standard "not found" message.
    /// </summary>
    [Trait("Bug", "80774")]
    [Fact]
    public async Task UpdateFile_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(
                999999999, new UpdateFile { Title = "Autotest Non-existent" }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The required file was not found");
    }

    /// <summary>
    /// UpdateFile cannot change the file extension: the original extension is always preserved
    /// and appended after the requested title, e.g. "Renamed.txt" becomes "Renamed.txt.docx".
    /// </summary>
    [Fact]
    public async Task UpdateFile_TitleWithDifferentExtension_OriginalExtensionIsPreserved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Change Ext Original.docx", Owner);

        // Act
        var updated = (await _filesApi.UpdateFileAsync(
            file.Id, new UpdateFile { Title = "Autotest Change Ext Renamed.txt" }, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Autotest Change Ext Renamed.txt.docx");
        updated.FileExst.Should().Be(".docx");
    }

    [Fact]
    public async Task UpdateFile_EmptyTitle_IsIgnoredAndReturns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Empty Title Original.docx", Owner);

        // Act
        var updated = (await _filesApi.UpdateFileAsync(
            file.Id, new UpdateFile { Title = "" }, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Should().NotBeNull();
        updated.Title.Should().Be("Autotest Empty Title Original.docx");
    }

    [Fact]
    public async Task UpdateFile_WhitespaceOnlyTitle_IsIgnoredAndReturns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Spaces Title Original.docx", Owner);

        // Act
        var updated = (await _filesApi.UpdateFileAsync(
            file.Id, new UpdateFile { Title = "   " }, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Should().NotBeNull();
        updated.Title.Should().Be("Autotest Spaces Title Original.docx");
    }

    /// <summary>
    /// BUG 80773: submitting a <c>lastVersion</c> equal to the file's current version used to
    /// return 500. It now correctly reports 400 with an explanatory message.
    /// </summary>
    [Trait("Bug", "80773")]
    [Fact]
    public async Task UpdateFile_LastVersionEqualToCurrent_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest LastVersion Original.docx", Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(
                file.Id,
                new UpdateFile { Title = "Autotest LastVersion Renamed", LastVersion = file.Version },
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("The new version cannot be the same as the current one");
    }

    [Fact]
    public async Task UpdateFile_TitleWithAmpersand_IsAccepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Special Chars Original.docx", Owner);

        // Act
        var updated = (await _filesApi.UpdateFileAsync(
            file.Id, new UpdateFile { Title = "Autotest A & B" }, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Autotest A & B.docx");
    }

    /// <summary>
    /// A forward slash is not a valid filename character; the API sanitizes it by replacing it
    /// with an underscore instead of rejecting the request.
    /// </summary>
    [Fact]
    public async Task UpdateFile_TitleWithForwardSlash_IsSanitized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Slash Original.docx", Owner);

        // Act
        var updated = (await _filesApi.UpdateFileAsync(
            file.Id, new UpdateFile { Title = "Autotest A/B" }, TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Title.Should().Be("Autotest A_B.docx");
    }
}
