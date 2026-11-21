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


namespace ASC.Files.Tests.Tests._05_Features.Socket;

/// <summary>
/// An AI project has no history, so a change of the project or of an entry inside it sends no <c>s:update-history</c>,
/// while the same change of an Instructions room does.
/// </summary>
[Trait("Category", "Socket")]
[Trait("Feature", "AiHub")]
public class SocketAiProjectHistoryEventsTests(AspireAppFixture fixture) : SocketTestBase(fixture)
{
    private const string UpdateHistory = "s:update-history";

    [Fact]
    public async Task RenameAndCopyIntoAProject_SendNoUpdateHistoryToTheProject()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projectId = (await CreateRoom(new CreateRoomRequestDto($"Autotest Project {Guid.NewGuid():N}", roomType: RoomType.AiProjectRoom))).Id;
        var roomId = (await CreateRoom(new CreateRoomRequestDto($"Autotest Room {Guid.NewGuid():N}", roomType: RoomType.InstructionsRoom))).Id;
        var myFile = await CreateFileInMy("Autotest to project.docx", Owner);
        await using var socket = await ConnectAndWatch(Owner, projectId, roomId);

        // Act
        await _roomsApi.UpdateRoomAsync(projectId, new UpdateRoomRequest(title: $"Autotest Project {Guid.NewGuid():N}"), TestContext.Current.CancellationToken);
        await _filesOperationsApi.CopyBatchItemsAsync(
            new BatchRequestDto
            {
                DestFolderId = new(projectId),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(myFile.Id)],
                FolderIds = [],
                ReturnSingleOperation = true
            },
            TestContext.Current.CancellationToken);
        await WaitLongOperation();
        await _roomsApi.UpdateRoomAsync(roomId, new UpdateRoomRequest(title: $"Autotest Room {Guid.NewGuid():N}"), TestContext.Current.CancellationToken);

        // Assert
        await socket.WaitForAsync(UpdateHistory, HistoryOf(roomId), cancellationToken: TestContext.Current.CancellationToken);
        var projectEvent = await socket.ArrivesWithinAsync(UpdateHistory, HistoryOf(projectId), _noDeliveryWindow, TestContext.Current.CancellationToken);
        projectEvent.Should().BeFalse();
    }

    private static Func<JsonElement, bool> HistoryOf(int folderId)
    {
        return payload => payload.GetProperty("type").GetString() == "folder"
                          && payload.GetProperty("id").GetRawText() == folderId.ToString(CultureInfo.InvariantCulture);
    }
}
