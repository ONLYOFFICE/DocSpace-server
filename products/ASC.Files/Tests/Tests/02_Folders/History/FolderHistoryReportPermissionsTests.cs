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

namespace ASC.Files.Tests.Tests._02_Folders.History;

/// <summary>
/// <c>POST /api/2.0/files/folder/{folderId}/log/report</c> - access control for the folder-history
/// report generation. Functional coverage of the endpoint itself lives in
/// <see cref="FolderHistoryReportTests"/>.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "History")]
public class FolderHistoryReportPermissionsTests(
    AspireAppFixture fixture)
    : FolderHistoryTestBase(fixture)
{
    /// <summary>Asserts a successful report response carries a non-empty task id.</summary>
    private static void AssertReportStarted(DocumentBuilderTaskDto report)
    {
        report.Should().NotBeNull();
        report.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateReport_Owner_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Owner Perm");

        // Act
        var report = (await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertReportStarted(report);
    }

    [Fact]
    public async Task CreateReport_DocSpaceAdmin_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Admin Perm");

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var report = (await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertReportStarted(report);
    }

    [Fact]
    public async Task CreateReport_RoomAdminWithRoomManagerAccess_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report RoomManager Perm");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var report = (await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertReportStarted(report);
    }

    [Fact]
    public async Task CreateReport_RoomAdminWithEditingAccess_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Editing Perm");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.Editing);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var report = (await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertReportStarted(report);
    }

    [Fact]
    public async Task CreateReport_UserWithReadAccess_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report User Read Perm");

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await _filesClient.Authenticate(user);

        // Act
        var report = (await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertReportStarted(report);
    }

    [Fact]
    public async Task CreateReport_UserWithCommentAccess_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Comment Perm");

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Comment);
        await _filesClient.Authenticate(user);

        // Act
        var report = (await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertReportStarted(report);
    }

    [Fact]
    public async Task CreateReport_UserWithoutRoomAccess_Forbidden()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report User No Access Perm");

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <remarks>
    /// A guest with only <c>Read</c> access to the room should be rejected with 403 by
    /// <c>HistoryApiHelper.DemandFolderHistoryReportPermissionAsync</c>, the same way it is for the
    /// other history endpoints. Instead the server starts building the report before the guest's
    /// lack of access actually stops anything downstream, and the attempt to read/write the report
    /// files the guest cannot see raises a <c>DirectoryNotFoundException</c>, which the framework
    /// turns into 404 - not the 403 a permission failure should produce.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81592")]
    public async Task CreateReport_GuestWithReadAccess_Forbidden()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Guest Read Perm");

        var guest = await InviteGuest();
        await InviteToRoom(room.Id, guest, FileShare.Read);

        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateReport_GuestWithoutRoomAccess_Forbidden()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Guest Perm");

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateReport_Anonymous_Unauthorized()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Anon Perm");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
