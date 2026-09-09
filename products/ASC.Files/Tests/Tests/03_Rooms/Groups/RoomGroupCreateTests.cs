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

namespace ASC.Files.Tests.Tests._03_Rooms.Groups;

/// <summary>POST /files/group — positive creation scenarios.</summary>
[Trait("Category", "Rooms")]
public class RoomGroupCreateTests(
    AspireAppFixture fixture)
    : RoomGroupsTestBase(fixture)
{
    [Fact]
    public async Task Create_AllRoomTypes_Succeeds()
    {
        // Arrange
        var rooms = new[]
        {
            await CreateCustomRoom("Autotest Group Custom"),
            await CreateCollaborationRoom("Autotest Group Collaboration"),
            await CreateFillingFormsRoom("Autotest Group FormFilling"),
            await CreatePublicRoom("Autotest Group Public"),
            await CreateVDRRoom("Autotest Group VDR")
        };

        // Act
        var created = await CreateRoomGroup("Autotest Group", rooms.Select(r => r.Id));

        // Assert
        created.Name.Should().Be("Autotest Group");
        created.Id.Should().BeGreaterThan(0);
        created.TotalRooms.Should().Be(rooms.Length);

        var info = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Name.Should().Be("Autotest Group");
        info.TotalRooms.Should().Be(rooms.Length);
    }

    [Fact]
    public async Task Create_OneRoom_Succeeds()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Single Room");

        // Act
        var created = await CreateRoomGroup("One Room", [roomId]);

        // Assert
        AssertRoomGroupShape(created);
        created.Name.Should().Be("One Room");
        created.Icon.Id.Should().Be("star");
        created.UserId.Should().NotBeEmpty();
        created.TotalRooms.Should().Be(1);
        created.Rooms.Select(r => r.Title).Should().Contain("Single Room");
    }

    [Fact]
    public async Task Create_SeveralRooms_Succeeds()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(3, "Multi Room");

        // Act
        var created = await CreateRoomGroup("Many Rooms", ids);

        // Assert
        AssertRoomGroupShape(created);
        created.TotalRooms.Should().Be(3);
        var titles = created.Rooms.Select(r => r.Title).ToList();
        titles.Should().Contain(["Multi Room 1", "Multi Room 2", "Multi Room 3"]);
    }

    [Fact]
    public async Task Create_PrivateRoom_Succeeds()
    {
        // Arrange
        var room = await CreatePrivateRoom("Private Group Room", RoomType.CustomRoom);

        // Act
        var created = await CreateRoomGroup("Private Group", [room.Id]);

        // Assert
        created.TotalRooms.Should().Be(1);
        created.Rooms.Select(r => r.Title).Should().Contain("Private Group Room");
    }

    [Fact]
    public async Task Create_SeveralGroups_DistinctIds()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "Distinct");

        // Act
        var g1 = await CreateRoomGroup("Group One", [ids[0]]);
        var g2 = await CreateRoomGroup("Group Two", [ids[1]]);

        // Assert
        g1.Id.Should().NotBe(g2.Id);

        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var names = list.Select(g => g.Name).ToList();
        names.Should().Contain(["Group One", "Group Two"]);
    }

    [Fact]
    public async Task Create_DuplicateNames_Allowed()
    {
        // Arrange
        var ids = await CreateGroupRoomIds(2, "DupName");

        // Act
        var g1 = await CreateRoomGroup("Same Name", [ids[0]]);
        var g2 = await CreateRoomGroup("Same Name", [ids[1]]);

        // Assert
        g1.Id.Should().NotBe(g2.Id);
    }

    [Fact]
    public async Task Create_TwoGroupsShareSameRoom_Allowed()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Shared Room");

        // Act & Assert — neither create fails.
        await CreateRoomGroup("Shares A", [roomId]);
        await CreateRoomGroup("Shares B", [roomId]);
    }

    public static TheoryData<string> ValidIcons => [.. _validGroupIcons];

    [Theory]
    [MemberData(nameof(ValidIcons))]
    public async Task Create_ValidIcon_Accepted(string icon)
    {
        // Arrange
        var roomId = await CreateGroupRoomId($"Icon Room {icon}");

        // Act
        var created = await CreateRoomGroup($"Icon {icon}", [roomId], icon);

        // Assert
        created.Icon.Id.Should().Be(icon);
    }

    [Fact]
    public async Task Create_LongValidName_Accepted()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Long Name Room");
        var name = new string('n', 64);

        // Act
        var created = await CreateRoomGroup(name, [roomId]);

        // Assert
        created.Name.Should().Be(name);
    }

    /// <summary>
    /// Every character here is inside the Basic Multilingual Plane, which is what the name column
    /// can hold — see <see cref="Create_NameOutsideBmp_Rejected"/> for the rest.
    /// </summary>
    public static TheoryData<string, string> UnicodeNames => new()
    {
        { "cyrillic", "Мои любимые комнаты" },
        { "hieroglyphs", "我的房间列表" },
        { "combining", "Café Ñoño déjà" },
        { "internal-spaces", "My favorite rooms" }
    };

    [Theory]
    [MemberData(nameof(UnicodeNames))]
    public async Task Create_UnicodeName_StoredIntact(string label, string name)
    {
        // Arrange
        var roomId = await CreateGroupRoomId($"Uni {label}");

        // Act
        var created = await CreateRoomGroup(name, [roomId]);

        // Assert
        created.Name.Should().Be(name);
    }

    /// <summary>
    /// A name carrying a character outside the Basic Multilingual Plane — an emoji — is refused.
    /// <c>files_group.name</c> is declared <c>utf8</c> / <c>utf8_general_ci</c>
    /// (<c>products/ASC.Files/Core/Core/EF/DbFilesGroup.cs</c>), and MySQL's <c>utf8</c> holds at most
    /// three bytes per character, so a four-byte one cannot be stored. That is the accepted behaviour
    /// for room groups, not a defect, which is why this asserts the refusal rather than a round-trip.
    /// The rejection surfaces from the database write, so the status is 500 rather than a validated
    /// 400 — this test pins the refusal, and will start failing if the column ever moves to
    /// <c>utf8mb4</c> or the name is validated up front, both of which are worth noticing.
    /// </summary>
    [Fact]
    public async Task Create_NameOutsideBmp_Rejected()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Uni emoji");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateRoomGroup("Rooms 😀🚀🌟", [roomId]));

        // Assert
        exception.ErrorCode.Should().Be(500);

        var groups = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        groups.Should().NotContain(g => g.Name.Contains("😀"), "the refused name must not be stored");
    }

    //
    /// <summary>
    /// Contract: leading/trailing spaces in the name must be trimmed. The server currently stores
    /// the name verbatim (no trim).
    /// </summary>
    [Fact]
    [Trait("Bug", "82573")]
    public async Task Create_PaddedName_Trimmed()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Trim Room");

        // Act
        var created = await CreateRoomGroup("  Padded Name  ", [roomId]);

        // Assert
        created.Name.Should().Be("Padded Name");
    }

    [Fact]
    public async Task Create_Group_RetrievableViaGetInfo()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Retrieve Room");
        var created = await CreateRoomGroup("Retrievable", [roomId], "heart");

        // Act
        var got = (await _roomGroupsApi.GetRoomGroupInfoAsync(created.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        got.Id.Should().Be(created.Id);
        got.Name.Should().Be(created.Name);
        got.Icon.Id.Should().Be(created.Icon.Id);
        got.TotalRooms.Should().Be(created.TotalRooms);
    }

    [Fact]
    public async Task Create_Group_AppearsInGetRoomGroups()
    {
        // Arrange
        var roomId = await CreateGroupRoomId("Listed Room");
        var created = await CreateRoomGroup("In The List", [roomId]);

        // Act
        var list = (await _roomGroupsApi.GetRoomGroupsAsync(0, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var found = list.SingleOrDefault(g => g.Id == created.Id);
        found.Should().NotBeNull();
        found!.Name.Should().Be("In The List");
    }
}
