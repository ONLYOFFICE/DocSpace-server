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

namespace ASC.AI.Tests.Tests.SkillsTests;

[Trait("Category", "Read")]
[Trait("Feature", "AI/Skills")]
public class RoomsSkillsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetSkills_RoomWithSkills_ReturnsThemSortedByFileTitle()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Catalog");
        var second = await UploadSkillAsync(roomId, "pdf-review", "Reviews PDF documents");
        var first = await UploadSkillAsync(roomId, "brand-voice", "Writes in the brand voice");

        // Act
        var result = await GetRoomsSkillsAsync(roomId);

        // Assert
        var room = result.Rooms.Should().ContainSingle().Subject;
        room.Id.Should().Be(roomId);
        room.Available.Should().BeTrue();
        room.Title.Should().Be("Autotest Skills Catalog");
        room.Skills.Select(s => (s.Id, s.Name, s.Description)).Should().Equal(
            (first.ToString(), "brand-voice", "Writes in the brand voice"),
            (second.ToString(), "pdf-review", "Reviews PDF documents"));
    }

    [Fact]
    public async Task GetSkills_EmptyRoom_ReturnsAvailableRoomWithoutSkills()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();

        // Act
        var result = await GetRoomsSkillsAsync(roomId);

        // Assert
        var room = result.Rooms.Should().ContainSingle().Subject;
        room.Available.Should().BeTrue();
        room.Skills.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkills_SeveralRoomsAndDuplicateIds_ReturnsOneEntryPerRoom()
    {
        // Arrange
        var firstRoom = await CreateInstructionsRoomAsync();
        var secondRoom = await CreateInstructionsRoomAsync();
        await UploadSkillAsync(firstRoom, "pdf-review");
        await UploadSkillAsync(secondRoom, "brand-voice");

        // Act
        var result = await GetRoomsSkillsAsync(firstRoom, secondRoom, firstRoom);

        // Assert
        result.Rooms.Should().HaveCount(2);
        result.Rooms.Single(r => r.Id == firstRoom).Skills.Select(s => s.Name).Should().Equal("pdf-review");
        result.Rooms.Single(r => r.Id == secondRoom).Skills.Select(s => s.Name).Should().Equal("brand-voice");
    }

    [Fact]
    public async Task GetSkills_UnknownRoom_ReturnsUnavailableRoom()
    {
        // Act
        var result = await GetRoomsSkillsAsync(int.MaxValue);

        // Assert
        var room = result.Rooms.Should().ContainSingle().Subject;
        room.Id.Should().Be(int.MaxValue);
        room.Available.Should().BeFalse();
        room.Title.Should().BeNull();
        room.Skills.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkills_RoomOfAnotherType_ReturnsUnavailableRoom()
    {
        // Arrange
        var agentRoomId = await CreateRoomAsync();

        // Act
        var result = await GetRoomsSkillsAsync(agentRoomId);

        // Assert
        result.Rooms.Should().ContainSingle().Which.Available.Should().BeFalse();
    }

    [Fact]
    public async Task GetSkills_RoomTheCallerCannotRead_ReturnsUnavailableRoom()
    {
        // Arrange
        var roomId = await CreateInstructionsRoomAsync();
        await UploadSkillAsync(roomId, "pdf-review");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(roomAdmin);

        // Act
        var result = await GetRoomsSkillsAsync(roomId);

        // Assert
        var room = result.Rooms.Should().ContainSingle().Subject;
        room.Available.Should().BeFalse();
        room.Title.Should().BeNull();
        room.Skills.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkills_NoRoomIds_Returns400()
    {
        // Act
        using var response = await GetRoomsSkillsResponseAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSkills_MoreThanTwentyRoomIds_Returns400()
    {
        // Act
        using var response = await GetRoomsSkillsResponseAsync([.. Enumerable.Range(1, 21)]);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
