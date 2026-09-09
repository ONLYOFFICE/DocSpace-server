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

namespace ASC.Files.Tests.Tests._03_Rooms.Tags;

/// <summary>
/// Functional coverage of <c>GET /files/tags/{tagName}/haslinks</c>. Permission coverage
/// (who is allowed to call it) already lives in <c>Permissions/RoomTagDeletePermissionsTests</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagHasLinksTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task HasTagLinks_TagLinkedToMultipleRooms_ReturnsTrue()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "MultiRoomLinkedTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);

        foreach (var title in new[] { "Autotest HasLinks Room A", "Autotest HasLinks Room B" })
        {
            var room = await CreateCustomRoom(title);
            await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
        }

        // Act
        var hasLinks = (await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken)).Response;

        // Assert
        hasLinks.Should().BeTrue();
    }

    [Fact]
    public async Task HasTagLinks_DetachingTheOnlyLink_ReturnsFalseButTagStaysInCatalog()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "DetachTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Detach Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        var before = (await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken)).Response;
        before.Should().BeTrue();

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
        var after = (await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken)).Response;

        // Assert
        after.Should().BeFalse();

        // Detaching from a room does NOT remove the tag from the catalog (unlike deleting the
        // room, which garbage-collects single-use tags).
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().Contain(tagName);
    }

    [Fact]
    public async Task HasTagLinks_TagRemovedFromOneOfTwoRooms_StillReturnsTrue()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "PartialDetachTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);

        var room1 = await CreateCustomRoom("Autotest Partial Detach A");
        var room2 = await CreateCustomRoom("Autotest Partial Detach B");
        await _roomsApi.AddRoomTagsAsync(room1.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(room2.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room1.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
        var hasLinks = (await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken)).Response;

        // Assert
        hasLinks.Should().BeTrue();
    }

    [Fact]
    public async Task HasTagLinks_DeletingTheOnlyRoom_GarbageCollectsTagAndReturns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "GcTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest GC Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        var before = (await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken)).Response;
        before.Should().BeTrue();

        // Act
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // The tag was attached only to the deleted room, so it is garbage-collected from the
        // catalog; the endpoint then reports the tag as non-existent (404).
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);

        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(tagName);
    }

    [Theory]
    [InlineData("NoSuchTagEver")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasTagLinks_NonExistentOrBlankTagName_Returns404(string tagName)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task HasTagLinks_LookupIsCaseInsensitive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("CaseSensitiveTag"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Case Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["CaseSensitiveTag"]), TestContext.Current.CancellationToken);

        // Act
        var hasLinks = (await _roomsApi.HasTagLinksAsync("casesensitivetag", "casesensitivetag", TestContext.Current.CancellationToken)).Response;

        // Assert
        hasLinks.Should().BeTrue();
    }

    [Theory]
    [InlineData("Tag/Slash")]
    [InlineData("ТестТег")]
    [InlineData("C++")]
    public async Task HasTagLinks_SpecialCharacterTagNames_AreMatched(string tagName)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Special Chars Room {tagName.GetHashCode()}");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        var hasLinks = (await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken)).Response;

        // Assert
        hasLinks.Should().BeTrue();
    }

    [Fact]
    public async Task HasTagLinks_OnPathQueryMismatch_QueryParamDeterminesResult()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("MismatchLinkedTag"), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("MismatchUnlinkedTag"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Mismatch Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["MismatchLinkedTag"]), TestContext.Current.CancellationToken);

        // Act
        var pathLinkedQueryUnlinked = (await _roomsApi.HasTagLinksAsync("MismatchLinkedTag", "MismatchUnlinkedTag", TestContext.Current.CancellationToken)).Response;
        var pathUnlinkedQueryLinked = (await _roomsApi.HasTagLinksAsync("MismatchUnlinkedTag", "MismatchLinkedTag", TestContext.Current.CancellationToken)).Response;

        // Assert
        pathLinkedQueryUnlinked.Should().BeFalse();
        pathUnlinkedQueryLinked.Should().BeTrue();
    }

    [Fact]
    public async Task HasTagLinks_MultipleTagsOnOneRoom_AreDetectedIndependently()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("RoomTagOne"), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("RoomTagTwo"), TestContext.Current.CancellationToken);
        // RoomTagThree exists in the catalog but is not attached to any room.
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("RoomTagThree"), TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom("Autotest Multi-Tag Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["RoomTagOne", "RoomTagTwo"]), TestContext.Current.CancellationToken);

        // Act
        var one = (await _roomsApi.HasTagLinksAsync("RoomTagOne", "RoomTagOne", TestContext.Current.CancellationToken)).Response;
        var two = (await _roomsApi.HasTagLinksAsync("RoomTagTwo", "RoomTagTwo", TestContext.Current.CancellationToken)).Response;
        var three = (await _roomsApi.HasTagLinksAsync("RoomTagThree", "RoomTagThree", TestContext.Current.CancellationToken)).Response;

        // Assert
        one.Should().BeTrue();
        two.Should().BeTrue();
        three.Should().BeFalse();
    }

    [Fact]
    public async Task HasTagLinks_DetectsTagLinkedToPublicRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "PublicRoomTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);
        var room = await CreatePublicRoom("Autotest Public HasLinks Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        var hasLinks = (await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken)).Response;

        // Assert
        hasLinks.Should().BeTrue();
    }

    [Fact]
    public async Task HasTagLinks_RepeatedCalls_ReturnStableResult()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "StableTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Stable Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        var first = (await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken)).Response;
        var second = (await _roomsApi.HasTagLinksAsync(tagName, tagName, TestContext.Current.CancellationToken)).Response;

        // Assert
        first.Should().BeTrue();
        second.Should().BeTrue();
    }
}
