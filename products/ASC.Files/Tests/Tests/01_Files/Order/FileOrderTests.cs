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

namespace ASC.Files.Tests.Tests._01_Files.Order;

/// <summary>
/// Functional coverage of <c>PUT /files/{fileId}/order</c>, the single-entry order endpoint.
/// Permission coverage lives in <see cref="FileOrderPermissionsTests"/>; the bulk endpoint
/// (<c>PUT /files/order</c>) lives in <see cref="FilesBulkOrderTests"/>.
/// </summary>
[Trait("Category", "Files")]
public class FileOrderTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetFileOrder_SpecificValue_ReturnsUpdatedFile()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Order File", Owner);

        // Act
        var result = await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(5), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Id.Should().Be(file.Id);
        result.Response.Title.Should().Be("Autotest Order File.docx");
    }

    [Fact]
    public async Task SetFileOrder_UpdatedToNewValue_ReturnsUpdatedFile()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Order Update File", Owner);
        await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(3), TestContext.Current.CancellationToken);

        // Act
        var result = await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(7), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Id.Should().Be(file.Id);
    }

    [Fact]
    public async Task SetFileOrder_ZeroValue_ReturnsBadRequest()
    {
        // Arrange - order must be between 1 and 2147483647, 0 is not valid
        var file = await CreateFileInMy("Autotest Order Zero File", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(0), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetFileOrder_NonExistentFile_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFileOrderAsync(999999999, new OrderRequestDto(1), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>
    /// The single-entry endpoint also works on a file living inside a room, not just My Documents.
    /// Verified against the room's actual folder content rather than just the echoed entity, so a
    /// silent no-op would be caught here even though the TS suite only checked the returned id.
    /// </summary>
    [Fact]
    public async Task SetFileOrder_FileInVdrRoom_ReflectedInFolderContent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Order Single In Room");
        var file = await CreateFile("Autotest Order Single In Room File", room.Id);

        // Act
        var result = await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(10), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Id.Should().Be(file.Id);

        var content = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var updated = content.Files.FirstOrDefault(f => f.Title == file.Title);
        updated.Should().NotBeNull();
    }
}
