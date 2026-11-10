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

namespace ASC.Files.Tests.Tests._03_Rooms.AiHub;

/// <summary>
/// The pin limit of the rooms of the AI rooms section, counted apart from ordinary rooms and AI rooms, and the
/// absence of an external link on those rooms. The pinned SDK cannot deserialize a room of folder type 41, so the
/// pin calls go over raw HTTP; the link calls answer share objects, which it reads.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiHubRoomPinAndLinkTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task PinRoom_AiHubRooms_HaveTheirOwnTenRoomLimitNextToTenPinnedRooms()
    {
        // Arrange - ten ordinary rooms pinned fill the ordinary bucket only.
        await _filesClient.Authenticate(Owner);
        for (var i = 0; i < 10; i++)
        {
            var room = await CreateCustomRoom($"Autotest Hub Pin Room {i}");
            await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken);
        }

        var hubRooms = new List<int>();
        for (var i = 0; i < 11; i++)
        {
            hubRooms.Add(i % 2 == 0 ? await CreateInstructionsRoomAsync() : await CreateProjectAsync());
        }

        // Act - ten rooms of the AI rooms section still pin, the eleventh does not.
        for (var i = 0; i < 10; i++)
        {
            using var pinned = await PinResponseAsync(hubRooms[i]);
            pinned.StatusCode.Should().Be(HttpStatusCode.OK, $"hub room {i} is within the separate limit of ten");
        }

        using var eleventh = await PinResponseAsync(hubRooms[10]);

        // Assert
        eleventh.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await eleventh.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain("You can't pin a room");
        (await IsPinnedAsync(hubRooms[10])).Should().BeFalse();
        (await IsPinnedAsync(hubRooms[0])).Should().BeTrue();
    }

    [Fact]
    public async Task PinRoom_AfterUnpinningAnAiHubRoom_TheEleventhPins()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var hubRooms = new List<int>();
        for (var i = 0; i < 11; i++)
        {
            hubRooms.Add(await CreateInstructionsRoomAsync());
        }

        for (var i = 0; i < 10; i++)
        {
            using var pinned = await PinResponseAsync(hubRooms[i]);
            pinned.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var refused = await PinResponseAsync(hubRooms[10]))
        {
            refused.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the premise is that the AI rooms bucket is full");
        }

        using (var unpinned = await Files.PutAsync($"api/2.0/files/rooms/{hubRooms[0]}/unpin", null, TestContext.Current.CancellationToken))
        {
            unpinned.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Act
        using var eleventh = await PinResponseAsync(hubRooms[10]);

        // Assert
        eleventh.StatusCode.Should().Be(HttpStatusCode.OK);
        (await IsPinnedAsync(hubRooms[10])).Should().BeTrue();
    }

    [Theory]
    [InlineData(InstructionsRoomType)]
    [InlineData(AiProjectRoomType)]
    public async Task AiHubRoom_HasNoExternalLinkAndRefusesOne(int roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = (await CreateHubRoomAsync(roomType, $"Autotest Hub Link {roomType}")).GetProperty("id").GetInt32();

        // Act
        var links = (await _roomsApi.GetRoomLinksAsync(roomId, LinkType.External, TestContext.Current.CancellationToken)).Response;
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomLinkAsync(
                roomId,
                new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Autotest Hub Link", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert
        (links ?? []).Should().BeEmpty("a room of the AI rooms section gets no primary external link on creation");
        exception.ErrorCode.Should().Be(403);
        var after = (await _roomsApi.GetRoomLinksAsync(roomId, LinkType.External, TestContext.Current.CancellationToken)).Response;
        (after ?? []).Should().BeEmpty();
    }

    private Task<HttpResponseMessage> PinResponseAsync(int roomId)
    {
        return Files.PutAsync($"api/2.0/files/rooms/{roomId}/pin", null, TestContext.Current.CancellationToken);
    }

    private async Task<bool> IsPinnedAsync(int roomId)
    {
        using var response = await GetRoomResponseAsync(roomId);
        var room = await Files.ReadAsync<JsonElement>(response, TestContext.Current.CancellationToken);

        return room.TryGetProperty("pinned", out var pinned) && pinned.GetBoolean();
    }
}
