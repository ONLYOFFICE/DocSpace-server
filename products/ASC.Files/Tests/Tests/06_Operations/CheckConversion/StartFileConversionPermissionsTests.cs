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
/// <c>PUT /api/2.0/files/file/{fileId}/checkconversion</c> (<c>startFileConversion</c>) — access
/// control. Functional coverage is in <see cref="CheckConversionStatusTests"/>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class StartFileConversionPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private static CheckConversionRequestDtoInteger ConvertToPdf()
    {
        return new CheckConversionRequestDtoInteger(startConvert: true, outputType: "pdf");
    }

    [Fact]
    public async Task StartFileConversion_Owner_Returns200()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest StartConversion Perm Owner.docx", Owner);

        // Act & Assert
        await _filesOperationsApi.StartFileConversionAsync(file.Id, ConvertToPdf(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartFileConversion_DocSpaceAdmin_Returns200()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        var file = await CreateFileInMy("Autotest StartConversion Perm DocSpaceAdmin File.docx", admin);

        // Act & Assert
        await _filesOperationsApi.StartFileConversionAsync(file.Id, ConvertToPdf(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartFileConversion_RoomAdminWithRoomManagerAccess_Returns200()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest StartConversion Perm RoomAdmin Room");
        var file = await CreateFile("Autotest StartConversion Perm RoomAdmin File.docx", room.Id);

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        // Act & Assert
        await _filesClient.Authenticate(roomAdmin);
        await _filesOperationsApi.StartFileConversionAsync(file.Id, ConvertToPdf(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartFileConversion_UserWithContentCreatorAccess_CanConvertOwnFileInRoom_Returns200()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest StartConversion Perm ContentCreator Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        await _filesClient.Authenticate(user);
        var file = await CreateFile("Autotest StartConversion Perm ContentCreator File.docx", room.Id);

        // Act & Assert
        await _filesOperationsApi.StartFileConversionAsync(file.Id, ConvertToPdf(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartFileConversion_UserWithoutRoomAccess_Forbidden()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest StartConversion Perm No Access Room");
        var file = await CreateFile("Autotest StartConversion Perm No Access File.docx", room.Id);

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.StartFileConversionAsync(file.Id, ConvertToPdf(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task StartFileConversion_Anonymous_Unauthorized()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest StartConversion Perm Anon File.docx", Owner);

        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.StartFileConversionAsync(file.Id, ConvertToPdf(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
