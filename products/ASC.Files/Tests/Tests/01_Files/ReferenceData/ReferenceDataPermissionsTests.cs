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

namespace ASC.Files.Tests.Tests._01_Files.ReferenceData;

/// <summary>
/// <c>POST /files/file/referencedata</c> - access control. Unauthenticated is rejected by the
/// standard <c>[Authorize]</c> pipeline (401) before the endpoint runs at all. Below that,
/// <c>FileStorageService.GetReferenceDataAsync{T}</c> checks <c>fileSecurity.CanReadAsync</c> but,
/// on failure, returns a <see cref="FileReference"/> with <c>Error</c> set instead of throwing a
/// security exception - so a caller without room access currently gets 200, not 403. That is
/// tracked as bug 81414; the two tests below assert the 403 the product should return, not the 200
/// it currently does.
/// </summary>
[Trait("Category", "Files")]
public class ReferenceDataPermissionsTests(
    AspireAppFixture fixture)
    : ReferenceDataTestBase(fixture)
{
    [Fact]
    public async Task GetReferenceData_DocSpaceAdmin_CanReadFileInOwnersRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest RefData Admin Room", "Autotest RefData Admin File.docx");
        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        // Act
        await _filesClient.Authenticate(admin);
        var result = await _filesApi.GetReferenceDataAsync(
            new GetReferenceDataDtoInteger(fileKey, instanceId),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Url.Should().NotBeNullOrEmpty();
        result.Response.ReferenceData.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReferenceData_RoomManagerAccess_CanReadFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest RefData RoomAdmin Room");

        // FileSecurity.AvailableRoomAccesses only allows FileShare.RoomManager to be granted to a RoomAdmin.
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        var file = await CreateFile("Autotest RefData RoomAdmin File.docx", room.Id);
        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        // Act
        await _filesClient.Authenticate(roomAdmin);
        var result = await _filesApi.GetReferenceDataAsync(
            new GetReferenceDataDtoInteger(fileKey, instanceId),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.Url.Should().NotBeNullOrEmpty();
        result.Response.ReferenceData.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReferenceData_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest RefData Unauth Room", "Autotest RefData Unauth File.docx");
        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        // Act & Assert
        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetReferenceDataAsync(
                new GetReferenceDataDtoInteger(fileKey, instanceId),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Trait("Bug", "81414")]
    [Fact]
    public async Task GetReferenceData_UserWithoutRoomAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest RefData No Access Room", "Autotest RefData No Access File.docx");
        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        var user = await InviteMember(EmployeeType.User);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetReferenceDataAsync(
                new GetReferenceDataDtoInteger(fileKey, instanceId),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetReferenceData_GuestWithReadAccess_CanEditRoomIsFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomWithFile("Autotest RefData Guest Room", "Autotest RefData Guest File.docx");

        var guest = await InviteMember(EmployeeType.Guest);
        await InviteToRoom(room.Id, guest, FileShare.Read);

        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        // Act
        await _filesClient.Authenticate(guest);
        var result = await _filesApi.GetReferenceDataAsync(
            new GetReferenceDataDtoInteger(fileKey, instanceId),
            TestContext.Current.CancellationToken);

        // Assert
        result.Response.ReferenceData.CanEditRoom.Should().BeFalse();
    }

    [Trait("Bug", "81414")]
    [Fact]
    public async Task GetReferenceData_GuestWithoutRoomAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest RefData Guest No Access Room", "Autotest RefData Guest No Access File.docx");
        var (fileKey, instanceId) = await OpenEditAndGetReferenceKeys(file.Id);

        var guest = await InviteMember(EmployeeType.Guest);

        // Act & Assert
        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetReferenceDataAsync(
                new GetReferenceDataDtoInteger(fileKey, instanceId),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
