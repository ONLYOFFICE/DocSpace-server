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
/// <c>POST /files/rooms/{id}/indexexport</c> and <c>GET /files/rooms/indexexport</c> - the room
/// index export lifecycle. Access control lives in
/// <see cref="ASC.Files.Tests.Tests._03_Rooms.Permissions.RoomIndexExportStartPermissionsTests"/> and
/// <see cref="ASC.Files.Tests.Tests._03_Rooms.Permissions.RoomIndexExportTerminatePermissionsTests"/>;
/// request validation lives in <see cref="RoomIndexExportValidationTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomIndexExportTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <remarks>
    /// Bug 81110: enabling indexing on a VDR room after creation - through <c>UpdateRoom</c> rather
    /// than at <c>CreateRoom</c> time - used to leave the room in a state where an index export
    /// started on it never completed without an error. This pins the fix: the export must reach
    /// <c>isCompleted=true</c> with no error and a populated result file within the deadline.
    /// </remarks>
    [Fact(Skip = "The index export cannot complete in the integration-test profile: the document " +
                 "server fails at the download step (DocumentServiceException 'download'). Three " +
                 "causes were found and are unfixed here - openresty's upstream map is built from " +
                 "fixed ports while the services get ephemeral ones, the editors container publishes " +
                 "no host port, and files:docservice:url:internal is only set under isDocker. " +
                 "Skipped for the environment, not for the bug: 81110 itself is untested here.")]
    [Trait("Bug", "81110")]
    public async Task StartRoomIndexExport_IndexingEnabledAfterCreation_CompletesWithoutError()
    {
        // Arrange - the room starts without indexing and only gets it turned on afterwards.
        await _filesClient.Authenticate(Owner);

        // Mirrors the `getMyFolder` call the TypeScript original makes before starting the export.
        // The task saves its result into the initiator's My Documents and looks that folder up with
        // GetFolderIDUserAsync(createIfNotExists: false) (RoomIndexExportTask.ProcessSourceFileAsync),
        // while BaseTest.InitializeAsync deliberately leaves the owner's root tree unprovisioned.
        await GetUserFolderIdAsync(Owner);

        var room = await CreateVirtualRoom("Autotest Index Export Enabled After Create", indexing: false);
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(indexing: true), TestContext.Current.CancellationToken);

        var noActiveExport = (await _roomsApi.GetRoomIndexExportAsync(TestContext.Current.CancellationToken)).Response;
        noActiveExport.Should().BeNull();

        // Act
        var started = (await _roomsApi.StartRoomIndexExportAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        started.Id.Should().NotBeNullOrEmpty();
        started.Error.Should().BeNullOrEmpty();

        var completed = await WaitForIndexExportCompleted();

        // Assert
        completed.Error.Should().BeNullOrEmpty();
        completed.ResultFileId.Should().NotBeNull();
    }

    /// <summary>
    /// Polls the caller's own index export until it is completed, deadline 30s (matching the
    /// TypeScript suite's own retry window). Returns the last observed status so a timed-out
    /// assertion still shows what the export was actually doing.
    /// </summary>
    private async Task<DocumentBuilderTaskDto> WaitForIndexExportCompleted()
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var status = (await _roomsApi.GetRoomIndexExportAsync(TestContext.Current.CancellationToken)).Response;

            if (status is { IsCompleted: true } || DateTime.UtcNow >= deadline)
            {
                status.Should().NotBeNull("the index export status must be reported within 30 seconds");
                status.IsCompleted.Should().BeTrue("the index export must complete within 30 seconds (error '{0}')", status.Error);
                return status;
            }

            await Task.Delay(1_000, TestContext.Current.CancellationToken);
        }
    }
}
