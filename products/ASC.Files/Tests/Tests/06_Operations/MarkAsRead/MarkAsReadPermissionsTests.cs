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

namespace ASC.Files.Tests.Tests._06_Operations.MarkAsRead;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/markasread</c> — access control. Every authenticated role can
/// call the endpoint with an empty body; the only case actually denied is an unauthenticated
/// caller. A caller with no access to the room holding the target file is silently accepted too
/// (see <see cref="MarkAsRead_UserWithoutRoomAccess_Returns200"/>) - the endpoint does not check
/// per-item access before marking as read.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class MarkAsReadPermissionsTests(
    AspireAppFixture fixture)
    : MarkAsReadTestBase(fixture)
{
    [Fact]
    public async Task MarkAsRead_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MarkAsReadAsync(new BaseBatchRequestDto(folderIds: [], fileIds: []), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task MarkAsRead_Owner_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        await _filesOperationsApi.MarkAsReadAsync(new BaseBatchRequestDto(folderIds: [], fileIds: []), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task MarkAsRead_Member_Returns200(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var member = await InviteMember(employeeType);

        // Act & Assert
        await _filesClient.Authenticate(member);
        await _filesOperationsApi.MarkAsReadAsync(new BaseBatchRequestDto(folderIds: [], fileIds: []), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MarkAsRead_UserWithoutRoomAccess_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest MarkAsRead No Access");
        var file = await CreateFile("Autotest MarkAsRead No Access File.docx", room.Id);

        var user = await InviteMember(EmployeeType.User);

        // Act & Assert - the user is never invited into the room, yet the request still succeeds.
        await _filesClient.Authenticate(user);
        await _filesOperationsApi.MarkAsReadAsync(MarkAsReadFiles(file.Id), TestContext.Current.CancellationToken);
    }
}
