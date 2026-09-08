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

namespace ASC.Files.Tests.Tests._03_Rooms.IndexExport;

/// <summary>
/// <c>POST /files/rooms/{id}/indexexport</c> - request validation: what a well-formed but wrong id
/// gets back. Functional coverage lives in <see cref="RoomIndexExportTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomIndexExportValidationTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <remarks>
    /// Bug 82368: an out-of-range room id (0 or negative) should fail request validation with 400,
    /// but the API does not pre-validate the id and instead reports it the same way as any other
    /// non-existent room - 404 "folder not found".
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [Trait("Bug", "82368")]
    public async Task StartRoomIndexExport_OutOfRangeId_ShouldReturnBadRequest(int id)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.StartRoomIndexExportAsync(id, TestContext.Current.CancellationToken));

        // Assert - current (buggy) behaviour: 404, not the 400 a validation error should produce.
        exception.ErrorCode.Should().Be(400);
    }

    /// <remarks>
    /// Bug 82369: starting an index export on an archived room should be forbidden (403), consistent
    /// with reorder and other write operations on archived rooms, but the API currently accepts it
    /// and starts the export anyway (200).
    /// </remarks>
    [Fact]
    [Trait("Bug", "82369")]
    public async Task StartRoomIndexExport_ArchivedRoom_ShouldBeForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Index Export Archived Start");
        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.StartRoomIndexExportAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert - current (buggy) behaviour would be a 200 that actually starts the export; the
        // product is supposed to reject it with 403.
        exception.ErrorCode.Should().Be(403);
    }
}
