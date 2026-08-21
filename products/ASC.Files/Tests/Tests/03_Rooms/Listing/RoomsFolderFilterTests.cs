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

namespace ASC.Files.Tests.Tests._03_Rooms.Listing;

/// <summary>
/// GET /files/rooms - basic response shape, and the type / filterValue / tags / subject filters.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomsFolderFilterTests(
    AspireAppFixture fixture)
    : RoomsFolderTestBase(fixture)
{
    [Fact]
    public async Task GetRoomsFolder_ResponseIncludesCurrentFolderMetadata()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var folder = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        folder.Current.Should().NotBeNull();
        folder.Current.Id.Should().NotBe(0);
        folder.Current.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetRoomsFolder_ReturnedRoom_HasExpectedFields()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = "Autotest Shape Room " + Guid.NewGuid().ToString()[..8];
        var created = await CreateCustomRoom(title);

        // Act - filterValue is served from the search index, so poll instead of reading once.
        // The typed model also drops id/roomType (see RoomsFolderTestBase.GetRoomsFolderRawAsync).
        var raw = await PollAsync(
            () => GetRoomsFolderRawAsync(filterValue: title),
            r => r.Folders.Any(f => f.Id == created.Id));

        // Assert
        var folder = raw.Folders.Should().ContainSingle(f => f.Id == created.Id).Which;
        folder.Title.Should().Be(title);
        folder.RoomType.Should().Be(RoomType.CustomRoom);

        var typed = await PollAsync(
            async () => (await _roomsApi.GetRoomsFolderAsync(filterValue: title, cancellationToken: TestContext.Current.CancellationToken)).Response,
            r => r.Folders.Any(f => f.Title == title));
        var typedFolder = typed.Folders.Should().ContainSingle(f => f.Title == title).Which;
        typedFolder.Created.Should().NotBeNull();
        typedFolder.Updated.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRoomsFolder_TypeFilter_ReturnsOnlyMatchingType()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateAllRoomTypesAsync();

        // Act
        var raw = await GetRoomsFolderRawAsync(type: [RoomType.PublicRoom]);

        // Assert
        raw.Folders.Should().HaveCount(1);
        raw.Folders.Should().OnlyContain(f => f.RoomType == RoomType.PublicRoom);
    }

    [Fact]
    public async Task GetRoomsFolder_TypeFilterWithMultipleValues_ReturnsAnyMatchingType()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateAllRoomTypesAsync();

        // Act
        var raw = await GetRoomsFolderRawAsync(type: [RoomType.CustomRoom, RoomType.PublicRoom]);

        // Assert
        raw.Folders.Should().HaveCount(2);
        raw.Folders.Select(f => f.RoomType).Should().OnlyContain(t => t == RoomType.CustomRoom || t == RoomType.PublicRoom);
        raw.Folders.Select(f => f.RoomType).Should().Contain(RoomType.CustomRoom).And.Contain(RoomType.PublicRoom);
    }

    [Fact]
    public async Task GetRoomsFolder_TypeFilter_ExcludesOtherTypes()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateAllRoomTypesAsync();

        // Act
        var raw = await GetRoomsFolderRawAsync(type: [RoomType.VirtualDataRoom]);

        // Assert
        var types = raw.Folders.Select(f => f.RoomType).ToList();
        types.Should().NotContain(RoomType.CustomRoom);
        types.Should().NotContain(RoomType.PublicRoom);
        types.Should().NotContain(RoomType.EditingRoom);
        types.Should().NotContain(RoomType.FillingFormsRoom);
    }

    [Fact]
    public async Task GetRoomsFolder_FilterValue_FindsRoomByExactTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var exactTitle = "Autotest Exact Match Title " + Guid.NewGuid().ToString()[..8];
        var room = await CreateCustomRoom(exactTitle);

        // Act - filterValue is served from the search index, so poll instead of reading once.
        var result = await PollAsync(
            async () => (await _roomsApi.GetRoomsFolderAsync(filterValue: exactTitle, cancellationToken: TestContext.Current.CancellationToken)).Response,
            r => r.Folders.Any(f => f.Title == exactTitle));

        // Assert
        result.Folders.Should().ContainSingle();
        result.Folders[0].Title.Should().Be(exactTitle);

        // room.Id has no counterpart on FileEntryBaseDto (see GetRoomsFolderRawAsync), so this
        // test can only assert the room was created, not that the returned entry is the same one.
        room.RoomType.Should().Be(RoomType.CustomRoom);
    }

    [Fact]
    public async Task GetRoomsFolder_FilterValue_IsCaseInsensitive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = "Autotest CaseSensitive Room " + Guid.NewGuid().ToString()[..8];
        await CreateCustomRoom(title);

        // Act - filterValue is served from the search index, so poll instead of reading once.
        var result = await PollAsync(
            async () => (await _roomsApi.GetRoomsFolderAsync(
                filterValue: title.ToLowerInvariant(), cancellationToken: TestContext.Current.CancellationToken)).Response,
            r => r.Folders.Any(f => f.Title == title));

        // Assert
        result.Folders.Select(f => f.Title).Should().Contain(title);
    }

    [Fact]
    public async Task GetRoomsFolder_FilterValueWithNoMatches_ReturnsEmptyFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateCustomRoom("Autotest Any Room " + Guid.NewGuid().ToString()[..8]);

        // Act
        var result = (await _roomsApi.GetRoomsFolderAsync(
            filterValue: "NonExistentNeedle_zzz_xyz_123", cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Folders.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetRoomsFolder_FilterValueCombinedWithType_ReturnsIntersection()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var shared = "Autotest Intersect " + Guid.NewGuid().ToString()[..8];
        await CreateCustomRoom($"{shared} Custom");
        await CreatePublicRoom($"{shared} Public");
        await CreateCustomRoom("Autotest Unrelated " + Guid.NewGuid().ToString()[..8]);

        // Act - filterValue is served from the search index, so poll instead of reading once.
        var raw = await PollAsync(
            () => GetRoomsFolderRawAsync(filterValue: shared, type: [RoomType.CustomRoom]),
            r => r.Folders.Count > 0);

        // Assert
        raw.Folders.Should().ContainSingle();
        raw.Folders[0].Title.Should().Be($"{shared} Custom");
        raw.Folders[0].RoomType.Should().Be(RoomType.CustomRoom);
    }

    /// <remarks>
    /// Bug 81808: filtering GET /files/rooms by <c>tags</c> (JSON-serialized, as the endpoint
    /// documents it) fails instead of returning only the tagged room.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81808")]
    public async Task GetRoomsFolder_TagsFilter_ReturnsOnlyRoomsWithSelectedTag()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var tag = "AutotestFilterTag" + Guid.NewGuid().ToString()[..8];
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tag), TestContext.Current.CancellationToken);

        var tagged = await CreateCustomRoom("Autotest Room With Tag");
        await _roomsApi.AddRoomTagsAsync(tagged.Id, new BatchTagsRequestDto([tag]), TestContext.Current.CancellationToken);
        var untagged = await CreateCustomRoom("Autotest Room No Tag");

        // Act
        var raw = await GetRoomsFolderRawAsync(tags: JsonSerializer.Serialize(new[] { tag }));

        // Assert
        var ids = raw.Folders.Select(f => f.Id).ToList();
        ids.Should().Contain(tagged.Id);
        ids.Should().NotContain(untagged.Id);
    }

    /// <inheritdoc cref="GetRoomsFolder_TagsFilter_ReturnsOnlyRoomsWithSelectedTag"/>
    [Fact]
    [Trait("Bug", "81808")]
    public async Task GetRoomsFolder_TagsFilterWithMultipleTags_ReturnsRoomsMatchingAny()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var tagA = "AutotestTagA" + Guid.NewGuid().ToString()[..8];
        var tagB = "AutotestTagB" + Guid.NewGuid().ToString()[..8];
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagA), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagB), TestContext.Current.CancellationToken);

        var withA = await CreateCustomRoom("Autotest A");
        await _roomsApi.AddRoomTagsAsync(withA.Id, new BatchTagsRequestDto([tagA]), TestContext.Current.CancellationToken);
        var withB = await CreateCustomRoom("Autotest B");
        await _roomsApi.AddRoomTagsAsync(withB.Id, new BatchTagsRequestDto([tagB]), TestContext.Current.CancellationToken);
        var withNeither = await CreateCustomRoom("Autotest None");

        // Act
        var raw = await GetRoomsFolderRawAsync(tags: JsonSerializer.Serialize(new[] { tagA, tagB }));

        // Assert
        var ids = raw.Folders.Select(f => f.Id).ToList();
        ids.Should().Contain(withA.Id);
        ids.Should().Contain(withB.Id);
        ids.Should().NotContain(withNeither.Id);
    }

    [Fact]
    public async Task GetRoomsFolder_WithoutTags_ExcludesRoomsWithAnyTag()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var tag = "AutotestExcludedTag" + Guid.NewGuid().ToString()[..8];
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tag), TestContext.Current.CancellationToken);

        var tagged = await CreateCustomRoom("Autotest Tagged");
        await _roomsApi.AddRoomTagsAsync(tagged.Id, new BatchTagsRequestDto([tag]), TestContext.Current.CancellationToken);
        var untagged = await CreateCustomRoom("Autotest Untagged");

        // Act
        var raw = await GetRoomsFolderRawAsync(withoutTags: true);

        // Assert
        var ids = raw.Folders.Select(f => f.Id).ToList();
        ids.Should().Contain(untagged.Id);
        ids.Should().NotContain(tagged.Id);
    }

    [Fact]
    public async Task GetRoomsFolder_WithoutTagsTrue_ReturnsRoomWithoutTags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var created = await CreateCustomRoom("Autotest Untagged Only");

        // Act
        var raw = await GetRoomsFolderRawAsync(withoutTags: true);

        // Assert
        raw.Folders.Select(f => f.Id).Should().Contain(created.Id);
    }

    [Fact]
    public async Task GetRoomsFolder_SubjectOwnerId_ReturnsRoomsOwnedBySubject()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        var ownerRoom = await CreateCustomRoom("Autotest Owner Room " + Guid.NewGuid().ToString()[..8]);

        await _filesClient.Authenticate(admin);
        var adminRoom = await CreateCustomRoom("Autotest Admin Room " + Guid.NewGuid().ToString()[..8]);

        await _filesClient.Authenticate(Owner);

        // Act
        var raw = await GetRoomsFolderRawAsync(subjectOwnerId: admin.Id);

        // Assert
        var ids = raw.Folders.Select(f => f.Id).ToList();
        ids.Should().Contain(adminRoom.Id);
        ids.Should().NotContain(ownerRoom.Id);
    }

    [Fact]
    public async Task GetRoomsFolder_ExcludeSubject_ExcludesRoomsRelatedToSubject()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        var ownerRoom = await CreateCustomRoom("Autotest Owner Excl " + Guid.NewGuid().ToString()[..8]);

        await _filesClient.Authenticate(admin);
        var adminRoom = await CreateCustomRoom("Autotest Admin Excl " + Guid.NewGuid().ToString()[..8]);

        await _filesClient.Authenticate(Owner);

        // Act
        var raw = await GetRoomsFolderRawAsync(subjectOwnerId: admin.Id, excludeSubject: true);

        // Assert
        var ids = raw.Folders.Select(f => f.Id).ToList();
        ids.Should().Contain(ownerRoom.Id);
        ids.Should().NotContain(adminRoom.Id);
    }

    [Fact]
    public async Task GetRoomsFolder_NonExistingSubjectId_ReturnsEmptyFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateCustomRoom("Autotest Existing Room " + Guid.NewGuid().ToString()[..8]);

        // Act
        var result = (await _roomsApi.GetRoomsFolderAsync(
            subjectOwnerId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Folders.Should().BeEmpty();
        result.Total.Should().Be(0);
    }
}
