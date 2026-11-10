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

namespace ASC.Files.Tests.Tests._03_Rooms.Create;

/// <summary>
/// <c>roomType</c> is one of 1, 2, 5, 6, 8, 9, 10, 11, 12: the gaps between them (no Private type among them) are
/// refused, and the type of an existing room cannot be changed, because <c>updateRoom</c> has no such property.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTypeValuesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(13)]
    public async Task CreateRoom_RoomTypeBetweenTheListedValues_Returns400AndCreatesNothing(int roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = $"Autotest Type Gap {roomType} {Guid.NewGuid():N}"[..40];

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto(title, roomType: (RoomType)roomType),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var listed = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Any, cancellationToken: TestContext.Current.CancellationToken)).Response;
        (listed.Folders ?? []).Should().NotContain(f => f.Title == title);
    }

    /// <summary>
    /// <c>UpdateRoomRequest</c> carries no <c>roomType</c>, so the typed client cannot send one; the body goes over
    /// raw HTTP.
    /// </summary>
    [Fact]
    public async Task UpdateRoom_WithRoomType_Returns400AndTheTypeStays()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Type Immutable");

        // Act
        using var response = await new RawApiClient(_filesClient).PutRawAsync(
            $"api/2.0/files/rooms/{room.Id}",
            """{"title":"Autotest Type Immutable","roomType":8}""",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.RoomType.Should().Be(RoomType.CustomRoom);
    }

    [Fact]
    public async Task CreateRoom_AiRoom_LandsUnderTheRoomsRootNotInAiAgents()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var aiRoom = await CreateAiRoom("Autotest Ai Room Section");

        // Assert - createRoom parents every type but 10-12 under the Rooms root; an agent of the AI Agents
        // section is created by `POST api/2.0/ai/agents` instead.
        var info = (await _roomsApi.GetRoomInfoAsync(aiRoom.Id, TestContext.Current.CancellationToken)).Response;
        info.RootFolderType.Should().Be(FolderType.VirtualRooms);
        info.RoomType.Should().Be(RoomType.AiRoom);

        var active = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Active, cancellationToken: TestContext.Current.CancellationToken)).Response;
        (active.Folders ?? []).OfType<FolderDto>().Should().Contain(f => f.Id == aiRoom.Id);
    }

    [Fact]
    public async Task CreateRoom_AiRoom_HasNoExternalLinkAndRefusesOne()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var aiRoom = await CreateAiRoom("Autotest Ai Room Link");

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(aiRoom.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                aiRoom.Id,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Autotest Ai Link", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        (links ?? []).Should().BeEmpty("an AI room gets no primary external link on creation");
        exception.ErrorCode.Should().Be(403);
    }
}
