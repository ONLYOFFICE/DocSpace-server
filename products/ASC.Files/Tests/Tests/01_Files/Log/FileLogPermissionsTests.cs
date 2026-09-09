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

namespace ASC.Files.Tests.Tests._01_Files.Log;

/// <summary>
/// GET /files/file/{id}/log — who may read a file's activity log.
/// </summary>
/// <remarks>
/// These tests assert only that the read is accepted or refused, never what the log contains: file
/// audit events are published to the event bus and persisted by <c>EventDataIntegrationEventHandler</c>,
/// which lives in ASC.Web.Studio — a service the <c>integration-test</c> launch profile does not start
/// (<c>common/ASC.AppHost/Program.cs</c>). A file's log is therefore always empty here, so the
/// functional suite that asserted entries was removed and only the access checks remain. Restore it
/// together with ASC.Web.Studio if that service is ever added to the profile.
/// </remarks>
[Trait("Category", "Files")]
public class FileLogPermissionsTests(
    AspireAppFixture fixture)
    : FileLogTestBase(fixture)
{
    /// <summary>
    /// A portal admin may read the log of a file in someone else's room.
    /// </summary>
    [Fact]
    public async Task GetFileHistory_DocSpaceAdmin_CanReadHistoryInOwnersRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest History Admin Room");
        var file = await CreateFile("Autotest History Admin File.docx", room.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var response = await _filesApi.GetFileHistoryWithHttpInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// A room admin may read the log of a file in the room they manage.
    /// </summary>
    [Fact]
    public async Task GetFileHistory_RoomAdmin_CanReadHistoryInTheirRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest History RoomAdmin Room");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        var file = await CreateFile("Autotest History RoomAdmin File.docx", room.Id);

        await _filesClient.Authenticate(roomAdmin);

        // Act
        var response = await _filesApi.GetFileHistoryWithHttpInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// A member invited into the room with read access may read the log of a file in it.
    /// </summary>
    [Fact]
    public async Task GetFileHistory_UserWithRoomAccess_CanReadHistory()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest History User Room");

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        var file = await CreateFile("Autotest History User File.docx", room.Id);

        await _filesClient.Authenticate(user);

        // Act
        var response = await _filesApi.GetFileHistoryWithHttpInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFileHistory_UserWithoutRoomAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest History NoAccess Room");
        var file = await CreateFile("Autotest History NoAccess File.docx", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileHistoryAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileHistory_GuestWithoutAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest History Guest Room");
        var file = await CreateFile("Autotest History Guest File.docx", room.Id);

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileHistoryAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileHistory_Unauthenticated_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest History Unauth Room");
        var file = await CreateFile("Autotest History Unauth File.docx", room.Id);
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileHistoryAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
