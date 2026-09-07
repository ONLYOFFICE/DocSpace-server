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

namespace ASC.Files.Tests.Tests._01_Files.Lock;

/// <summary>
/// Functional behaviour of <c>PUT /files/file/:fileId/lock</c>: locking/unlocking as the owner, in
/// My Documents and in a room, idempotency of both directions, and the two documented bugs.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileLockTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task LockFile_InMyDocuments_ReturnsLocked()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Lock File.docx", Owner);

        // Act
        var result = (await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        result.Locked.Should().BeTrue();
    }

    [Fact]
    public async Task UnlockFile_PreviouslyLocked_ReturnsUnlocked()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Unlock File.docx", Owner);
        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);

        // Act
        var result = (await _filesApi.LockFileAsync(file.Id, new LockFileParameters(false), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        // The API omits `false` values, so an unlocked file simply has no Locked flag set.
        result.Locked.Should().NotBe(true);
    }

    [Fact]
    public async Task LockFile_InRoom_ReturnsLocked()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Lock");
        var file = await CreateFile("Autotest Lock Room File.docx", room.Id);

        // Act
        var result = (await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        result.Locked.Should().BeTrue();
    }

    [Fact]
    public async Task LockFile_AlreadyLocked_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Idempotent Lock File.docx", Owner);
        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);

        // Act
        var result = (await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        result.Locked.Should().BeTrue();
    }

    [Fact]
    public async Task UnlockFile_AlreadyUnlocked_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Idempotent Unlock File.docx", Owner);

        // Act
        var result = (await _filesApi.LockFileAsync(file.Id, new LockFileParameters(false), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
        result.Locked.Should().NotBe(true);
    }

    [Trait("Bug", "80788")]
    [Fact]
    public async Task LockFile_NonExistentFile_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.LockFileAsync(999999999, new LockFileParameters(true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Trait("Bug", "82178")]
    [Fact]
    public async Task UnlockFile_OneOfSeveralLockedFiles_LeavesRestLocked()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var files = new List<FileDtoInteger>();
        for (var i = 1; i <= 3; i++)
        {
            files.Add(await CreateFileInMy($"Autotest Lock Multi File {i}.docx", Owner));
        }

        foreach (var file in files)
        {
            await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);
        }

        // Act
        await _filesApi.LockFileAsync(files[1].Id, new LockFileParameters(false), TestContext.Current.CancellationToken);

        // Assert: unlocking the second file must not affect the lock state of the other two.
        var info0 = await GetFile(files[0].Id);
        var info1 = await GetFile(files[1].Id);
        var info2 = await GetFile(files[2].Id);

        info0.Locked.Should().BeTrue();
        info1.Locked.Should().NotBe(true);
        info2.Locked.Should().BeTrue();
    }
}
