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

namespace ASC.Files.Tests.Tests._06_Operations.CheckConversion;

/// <summary>
/// <c>GET /api/2.0/files/file/{fileId}/checkconversion</c> — access control. Functional coverage is
/// in <see cref="CheckConversionStatusTests"/>; the Editor-access bug is in
/// <see cref="CheckConversionStatusBugTests"/>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class CheckConversionStatusPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CheckConversionStatus_Owner_Returns200()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest CheckConversion Owner File.docx", Owner);

        // Act & Assert
        await _filesOperationsApi.CheckConversionStatusAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CheckConversionStatus_RoomAdminWithRoomManagerAccess_Returns200()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest CheckConversion RoomAdmin Room");
        var file = await CreateFile("Autotest CheckConversion RoomAdmin File.docx", room.Id);

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        // Act & Assert
        await _filesClient.Authenticate(roomAdmin);
        await _filesOperationsApi.CheckConversionStatusAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CheckConversionStatus_UserWithoutRoomAccess_Forbidden()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest CheckConversion No Access Room");
        var file = await CreateFile("Autotest CheckConversion No Access File.docx", room.Id);

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CheckConversionStatusAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CheckConversionStatus_Anonymous_Unauthorized()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest CheckConversion Anon File.docx", Owner);

        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CheckConversionStatusAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
