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

namespace ASC.Files.Tests.Tests._01_Files.Editing;

/// <summary>
/// <c>POST /files/file/{fileId}/startedit</c> with <c>editingAlone: true</c>. That branch never
/// contacts the document server - it only reserves the file for exclusive editing and returns a
/// locally computed document key (<c>FileStorageService.StartEditAsync</c>).
///
/// <c>editingAlone: false</c> (and the request with no body at all, which defaults to it) takes
/// the co-authoring branch, which calls out to the document server
/// (<c>DocumentServiceTracker.StartTrackAsync</c> -&gt; <c>DocumentServiceConnector.CommandAsync</c>).
/// The integration-test host has no document server, so those two TS cases
/// ("BUG 81267: editingAlone: false returns 403" and "BUG 81267: Request without editingAlone
/// field returns 403") are not portable and were dropped.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class EditingStartTests(AspireAppFixture fixture) : EditingTestBase(fixture)
{
    [Fact]
    public async Task StartEditFile_OwnerAlone_ReturnsDocKey()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest Start Edit Alone Room", "Autotest Start Edit Alone File");

        // Act
        var result = (await _filesApi.StartEditFileAsync(file.Id, new StartEdit(editingAlone: true), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// A member invited with <c>RoomManager</c> access can start editing exclusively, whatever the
    /// portal role that access was granted to - both <c>DocSpaceAdmin</c> and <c>RoomAdmin</c> may
    /// receive <c>RoomManager</c> (<c>FileSecurity.AvailableUserAccesses</c>).
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task StartEditFile_InvitedRoomManager_ReturnsDocKey(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomWithFile($"Autotest Start Edit {employeeType} Room", $"Autotest Start Edit {employeeType} File");

        var member = await InviteContact(employeeType);
        await InviteToRoom(room.Id, member, FileShare.RoomManager);

        await _filesClient.Authenticate(member);

        // Act
        var result = (await _filesApi.StartEditFileAsync(file.Id, new StartEdit(editingAlone: true), TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task StartEditFile_NonExistentFile_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartEditFileAsync(999999999, new StartEdit(editingAlone: true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("The required file was not found");
    }

    /// <summary>
    /// Merges the TS suite's "Second user starts editing a file already being edited" and
    /// "editingAlone: true when file is already being edited" cases - both set up the same
    /// conflict (one exclusive editing session already open) and assert the same outcome. The
    /// original TS tests invited the second user with <c>FileShare.ReadWrite</c>, which is not a
    /// legal room access for a <c>User</c> subject (<c>FileSecurity.AvailableRoomAccesses</c>);
    /// <c>Editing</c> is used here instead, since it is the smallest access level the conflict
    /// actually depends on.
    /// </summary>
    [Fact]
    public async Task StartEditFile_AlreadyEditingAlone_SecondUserForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomWithFile("Autotest Concurrent Edit Room", "Autotest Concurrent Edit File");

        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Editing);

        var ownerResult = (await _filesApi.StartEditFileAsync(file.Id, new StartEdit(editingAlone: true), TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartEditFileAsync(file.Id, new StartEdit(editingAlone: true), TestContext.Current.CancellationToken));

        // Assert
        ownerResult.Should().NotBeNullOrEmpty();
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("This document is being edited by you in another tab");
    }
}
