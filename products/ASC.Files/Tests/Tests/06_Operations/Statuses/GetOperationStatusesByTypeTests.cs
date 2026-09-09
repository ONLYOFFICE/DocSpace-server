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

namespace ASC.Files.Tests.Tests._06_Operations.Statuses;

/// <summary>
/// <c>GET /api/2.0/files/fileops/{operationType}</c> — functional coverage.
/// </summary>
[Trait("Category", "Operations")]
public class GetOperationStatusesByTypeTests(
    AspireAppFixture fixture)
    : OperationsStatusesTestBase(fixture)
{
    /// <summary>
    /// Every readable operation type except <c>Move</c>: the generated client puts the enum name on
    /// the route, and <c>GET fileops/move</c> is shadowed by the literal
    /// <c>checkMoveOrCopyBatchItems</c> route, so the Move statuses can never be read by name — the
    /// request lands on the check endpoint and dies on its missing <c>destFolderId</c>. An
    /// API-design quirk worth knowing, not a test defect.
    /// </summary>
    public static TheoryData<FileOperationType> WorkingOperationTypes =>
    [
        FileOperationType.Delete, FileOperationType.Copy,
        FileOperationType.Duplicate, FileOperationType.Download, FileOperationType.MarkAsRead
    ];

    [Theory]
    [MemberData(nameof(WorkingOperationTypes))]
    public async Task GetOperationStatusesByType_NoActiveOperations_ReturnsEmptyArray(FileOperationType operationType)
    {
        // Act
        var statuses = await GetStatusesByType(operationType);

        // Assert
        statuses.Should().BeEmpty();
    }

    /// <summary>
    /// The TS suite files this as a bug (82225): filtering by <see cref="FileOperationType.Convert"/>
    /// returns 400 instead of 200 with an empty list, same as every other operation type.
    /// </summary>
    [Fact]
    [Trait("Bug", "82225")]
    public async Task GetOperationStatusesByType_Convert_ReturnsEmptyArray()
    {
        // Act
        var statuses = await GetStatusesByType(FileOperationType.Convert);

        // Assert
        statuses.Should().BeEmpty();
    }

    /// <summary>
    /// The TS suite files this as a bug (82225): filtering by <see cref="FileOperationType.Import"/>
    /// returns 400 instead of 200 with an empty list, same as every other operation type.
    /// </summary>
    [Fact]
    [Trait("Bug", "82225")]
    public async Task GetOperationStatusesByType_Import_ReturnsEmptyArray()
    {
        // Act
        var statuses = await GetStatusesByType(FileOperationType.Import);

        // Assert
        statuses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOperationStatusesByType_ValidTypeWithNonExistentId_ReturnsEmptyArray()
    {
        // Act
        var statuses = await GetStatusesByType(FileOperationType.Delete, "9999999");

        // Assert
        statuses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOperationStatusesByType_InvalidOperationType_ThrowsBadRequest()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await GetStatusesByType((FileOperationType)99));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }
}
