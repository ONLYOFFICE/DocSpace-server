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

namespace ASC.Files.Tests.Tests._06_Operations.Copy;

[Trait("Category", "Operations")]
[Trait("Feature", "Folders")]
public class FolderCopyTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CopyFolder_ToItsSubfolder_ReturnError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceFolder = await CreateFolder("source_folder", FolderType.USER, Owner);
        var subFolder = await CreateFolder("subfolder", sourceFolder.Id);

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(subFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourceFolder.Id)],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFolder_NoCopyPermissions_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var targetFolder = await CreateFolderInMy("target_folder", Owner);
        var sourceFolder = await CreateFolderInMy("source_folder", Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourceFolder.Id)],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }


    [Fact]
    public async Task CopyFolder_ToFormFillingRoom_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var sourceRoom = await CreateFillingFormsRoom("source_room");
        var targertRoom = await CreateFillingFormsRoom("target_room");

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targertRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourceRoom.Id)],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFolder_FolderNotFound_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var targetFolderId = await GetUserFolderIdAsync(Owner);
        var sourceFolderId = 999999999;

        // Act
        var copyParams = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(targetFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(sourceFolderId)],
            ReturnSingleOperation = true
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                copyParams,
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(500);
    }
}
