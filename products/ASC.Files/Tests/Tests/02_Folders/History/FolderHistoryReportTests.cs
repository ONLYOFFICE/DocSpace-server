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
/// <c>POST /api/2.0/files/folder/{folderId}/log/report</c> - generating an audit report of a
/// folder's history. Access control lives in <see cref="FolderHistoryReportPermissionsTests"/>.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "History")]
public class FolderHistoryReportTests(
    AspireAppFixture fixture)
    : FolderHistoryTestBase(fixture)
{
    private static void AssertReportStarted(DocumentBuilderTaskDto report)
    {
        report.Should().NotBeNull();
        report.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateReport_Room_ReturnsTaskId()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Folder History Owner");

        // Act
        var report = (await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertReportStarted(report);
    }

    /// <summary>
    /// The TypeScript original only asserts that the three envelope fields are defined, which in C#
    /// is true of an <see cref="int"/> by construction. Asserted here as the values a successful
    /// response actually carries: envelope <c>status</c> 0 (the API's success code, not an HTTP one)
    /// and <c>statusCode</c> 200, with the task itself queued as
    /// <see cref="DistributedTaskStatus.Created"/>.
    /// </summary>
    [Fact]
    public async Task CreateReport_ResponseStructure_HasSuccessEnvelopeAndQueuedTask()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Folder History Structure");

        // Act
        var wrapper = await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        wrapper.Status.Should().Be(0);
        wrapper.StatusCode.Should().Be(200);
        wrapper.Response.Should().NotBeNull();
        wrapper.Response.Id.Should().NotBeNullOrEmpty();
        wrapper.Response.Status.Should().BeOneOf(DistributedTaskStatus.Created, DistributedTaskStatus.Running);
    }

    [Fact]
    public async Task CreateReport_RoomWithRichHistory_ReturnsTaskId()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Folder History Rich");

        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(title: "Autotest Report Folder History Rich Renamed"), TestContext.Current.CancellationToken);

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        await CreateFile("Report Rich History File", room.Id);

        // Act
        var report = (await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertReportStarted(report);
    }

    [Fact]
    public async Task CreateReport_ArchivedRoom_ReturnsTaskId()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Folder History Archived");
        await ArchiveRoom(room.Id);

        // Act
        var report = (await _foldersApi.CreateReportFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertReportStarted(report);
    }

    [Fact]
    public async Task CreateReport_NonExistentFolderId_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateReportFolderHistoryAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateReport_FolderIdZero_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateReportFolderHistoryAsync(0, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateReport_NegativeFolderId_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateReportFolderHistoryAsync(-1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateReport_Subfolder_ReturnsTaskId()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Subfolder");
        var subfolder = await CreateFolder("Autotest Report Subfolder Child", room.Id);

        // Act
        var report = (await _foldersApi.CreateReportFolderHistoryAsync(subfolder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        AssertReportStarted(report);
    }
}
