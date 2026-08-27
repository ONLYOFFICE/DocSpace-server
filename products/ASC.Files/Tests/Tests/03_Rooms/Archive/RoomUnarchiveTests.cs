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

namespace ASC.Files.Tests.Tests._03_Rooms.Archive;

/// <summary>
/// <c>PUT /files/rooms/{id}/unarchive</c> — request body variants, the async operation contract,
/// id validation and archive/unarchive integration cycles. Owner happy-path lists/metadata/pin
/// coverage lives in <see cref="RoomArchiveTests"/>; access control in
/// <see cref="ASC.Files.Tests.Tests._03_Rooms.Permissions.RoomArchivePermissionsTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomUnarchiveTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    #region Request body variants

    /// <remarks>
    /// The TypeScript suite also had a "works without a body at all" case. It is not portable: the
    /// generated .NET client omits the Content-Type header along with the body, which ASP.NET
    /// refuses with 415 before the controller runs, and a raw request carrying the header with a
    /// zero-length body is refused with 400 because an empty string is not valid JSON. The
    /// meaningful case — a request whose body carries no fields — is
    /// <see cref="UnarchiveRoom_WorksWithEmptyBody"/> right below.
    /// </remarks>
    [Fact]
    public async Task UnarchiveRoom_WorksWithEmptyBody()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateArchivedCustomRoom("Autotest Unarchive Empty Body");

        // Act
        var response = await _roomsApi.UnarchiveRoomWithHttpInfoAsync(room.Id, new ArchiveRoomRequest(), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().OnlyContain(o => o.Finished);
        (await IsInActiveList(room.Title)).Should().BeTrue();
    }

    [Fact]
    public async Task UnarchiveRoom_WorksWithDeleteAfterTrue()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateArchivedCustomRoom("Autotest Unarchive deleteAfter true");

        // Act - deleteAfter: true still restores the room, but enqueues no async operation, so the
        // room moves back to Active synchronously; poll the list directly instead of an operation.
        var response = await _roomsApi.UnarchiveRoomWithHttpInfoAsync(room.Id, new ArchiveRoomRequest(true), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var isActive = await PollUntilActive(room.Title);

        isActive.Should().BeTrue("the room must become active within 30 seconds");
        (await IsInArchiveList(room.Title)).Should().BeFalse();
    }

    [Fact]
    public async Task UnarchiveRoom_InvalidDeleteAfterTypeString_ReturnsBadRequestAndStaysArchived()
    {
        // Arrange - the DTO's deleteAfter is a non-nullable bool, so a JSON string value can only
        // be sent as a raw request.
        await _filesClient.Authenticate(Owner);
        var room = await CreateArchivedCustomRoom("Autotest Unarchive Bad deleteAfter");

        // Act
        using var response = await SendRawUnarchive(room.Id, "{\"deleteAfter\":\"false\"}");

        // Assert - the room must stay archived on a rejected request.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await IsInArchiveList(room.Title)).Should().BeTrue();
    }

    #endregion

    #region Async operation contract

    [Fact]
    public async Task UnarchiveRoom_ResponseIsFileOperationWrapper()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateArchivedCustomRoom("Autotest Unarchive Wrapper");

        // Act
        var response = (await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));
        await WaitLongOperation();

        // Assert
        response.Response.Should().NotBeNull();
        response.Response.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UnarchiveRoom_SecondCallWhileRunning_DoesNotCorruptState()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateArchivedCustomRoom("Autotest Unarchive Double Call");

        // Act
        var first = await _roomsApi.UnarchiveRoomWithHttpInfoAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        HttpStatusCode secondStatus;

        try
        {
            var second = await _roomsApi.UnarchiveRoomWithHttpInfoAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
            secondStatus = second.StatusCode;
        }
        catch (ApiException ex)
        {
            secondStatus = (HttpStatusCode)ex.ErrorCode;
        }

        var operations = await WaitLongOperation();

        // Assert - neither call should 500; the room ends up active and consistent.
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        secondStatus.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden);
        operations.Should().OnlyContain(o => o.Finished && o.Error == "");
        (await IsInActiveList(room.Title)).Should().BeTrue();
        (await IsInArchiveList(room.Title)).Should().BeFalse();
    }

    [Fact]
    public async Task UnarchiveRoom_AlreadyActiveRoom_IsNoOp()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateArchivedCustomRoom("Autotest Unarchive Repeat");
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act - the room is already active; a second unarchive must not error or re-archive it.
        var response = await _roomsApi.UnarchiveRoomWithHttpInfoAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await IsInActiveList(room.Title)).Should().BeTrue();
        (await IsInArchiveList(room.Title)).Should().BeFalse();
    }

    [Fact]
    public async Task UnarchiveRoom_NeverArchivedRoom_StaysActive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unarchive Never Archived");

        // Act
        var response = await _roomsApi.UnarchiveRoomWithHttpInfoAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await IsInActiveList(room.Title)).Should().BeTrue();
        (await IsInArchiveList(room.Title)).Should().BeFalse();
    }

    #endregion

    #region Invalid id validation

    [Fact]
    public async Task UnarchiveRoom_NonExistentRoomId_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnarchiveRoomAsync(999999999, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UnarchiveRoom_DeletedRoomId_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unarchive Deleted Room");
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UnarchiveRoom_IdZero_MustNotSucceed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act / Assert
        await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnarchiveRoomAsync(0, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnarchiveRoom_NegativeId_MustNotSucceed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act / Assert
        await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnarchiveRoomAsync(-1, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnarchiveRoom_NonNumericId_DoesNotSucceed()
    {
        // Arrange - the route's id is a typed int, so a non-numeric value can only be sent as a
        // raw request.
        await _filesClient.Authenticate(Owner);

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/2.0/files/rooms/abc/unarchive")
        {
            Content = new StringContent("{\"deleteAfter\":false}", Encoding.UTF8, "application/json")
        };
        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Integration cycles

    [Fact]
    public async Task UnarchiveRoom_CanBeArchivedUnarchivedThenArchivedAgain()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateArchivedCustomRoom("Autotest Unarchive Re-archive");

        // Act - unarchive
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();
        (await IsInActiveList(room.Title)).Should().BeTrue();

        // Act - archive again
        var response = await _roomsApi.ArchiveRoomWithHttpInfoAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var operations = await WaitLongOperation();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        operations.Should().OnlyContain(o => o.Finished);
        (await IsInArchiveList(room.Title)).Should().BeTrue();
        (await IsInActiveList(room.Title)).Should().BeFalse();
    }

    [Fact]
    public async Task UnarchiveRoom_UnarchivedRoomCanBeRenamedAgain()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateArchivedCustomRoom("Autotest Unarchive Rename");

        // Act / Assert - rename is forbidden while archived (read-only)
        var archivedException = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest("Should Fail While Archived"), TestContext.Current.CancellationToken));
        archivedException.ErrorCode.Should().Be(403);

        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act - allowed once restored
        var renamed = (await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest("Renamed After Unarchive"), TestContext.Current.CancellationToken)).Response;

        // Assert
        renamed.Title.Should().Be("Renamed After Unarchive");
    }

    [Fact]
    public async Task UnarchiveRoom_UnarchivedRoomCanBeSharedAgain()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateArchivedCustomRoom("Autotest Unarchive Share");

        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var response = await _roomsApi.SetRoomSecurityWithHttpInfoAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Editing }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnarchiveRoom_ContentSurvivesArchiveUnarchiveRoundTrip()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Unarchive Content Round-Trip");
        var file = await CreateFile("Autotest File Round-Trip", room.Id);
        var folder = await CreateFolder("Autotest Folder Round-Trip", room.Id);

        await ArchiveRoom(room.Id);
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var content = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert - Files/Folders drop Id (see the tests rule), so membership is asserted by Title.
        content.Files.Should().Contain(f => f.Title == file.Title);
        content.Folders.Should().Contain(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task UnarchiveRoom_UnarchivedVdrRoomCanStartIndexExport()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest Unarchive VDR IndexExport");
        await ArchiveRoom(room.Id);

        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var task = (await _roomsApi.StartRoomIndexExportAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        task.Id.Should().NotBeNullOrEmpty();
        task.Error.Should().BeNullOrEmpty();

        await _roomsApi.TerminateRoomIndexExportAsync(TestContext.Current.CancellationToken);
    }

    #endregion

    #region Pin/unpin

    [Fact]
    public async Task PinRoom_ThenUnpinRoom_BothReturnOk()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Pin Room");

        // Act / Assert - pin
        var pinResponse = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        pinResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act / Assert - unpin
        var unpinResponse = await _roomsApi.UnpinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        unpinResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    private async Task<FolderDtoInteger> CreateArchivedCustomRoom(string title)
    {
        var room = await CreateCustomRoom(title);
        await ArchiveRoom(room.Id);

        return room;
    }

    private async Task<bool> IsInActiveList(string title)
    {
        var list = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Active, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return list.Folders.Exists(f => f.Title == title);
    }

    private async Task<bool> IsInArchiveList(string title)
    {
        var list = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Archive, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return list.Folders.Exists(f => f.Title == title);
    }

    /// <summary>
    /// Polls the Active list on a deadline. <c>deleteAfter: true</c> restores the room
    /// synchronously with no trackable file operation, so the room list itself is the only signal.
    /// </summary>
    private async Task<bool> PollUntilActive(string title)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var isActive = await IsInActiveList(title);

            if (isActive || DateTime.UtcNow >= deadline)
            {
                return isActive;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    private async Task<HttpResponseMessage> SendRawUnarchive(int roomId, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"api/2.0/files/rooms/{roomId}/unarchive")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
