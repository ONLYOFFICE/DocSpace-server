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
/// Functional coverage of <c>DELETE /files/rooms/{id}/tags</c> (detaching tags from a room):
/// positive behavior and how detach matches tag names. Validation of the room id and the names
/// body lives in <see cref="RoomTagDetachValidationTests"/>; metadata preservation and
/// integration with other tag endpoints lives in <see cref="RoomTagDetachIntegrationTests"/>.
/// Permission coverage lives in <c>Permissions/RoomTagDetachPermissionsTests</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagDetachTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task DeleteRoomTags_OwnerDetachesSeveralTagsInOneRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Detach Several");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["TagA", "TagB", "TagC"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["TagA", "TagB"]), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Tags.Should().BeEquivalentTo(["TagC"]);
    }

    [Fact]
    public async Task DeleteRoomTags_DetachedTagRemainsInGlobalCatalog()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "GlobalCatalogTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Detach Keeps Catalog");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().Contain(tagName);
    }

    [Fact]
    public async Task DeleteRoomTags_DetachFromOneRoomDoesNotAffectSameTagOnAnotherRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "SharedRoomsTag";
        var room1 = await CreateCustomRoom("Autotest Shared Tag Room 1");
        var room2 = await CreateCustomRoom("Autotest Shared Tag Room 2");
        await _roomsApi.AddRoomTagsAsync(room1.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(room2.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room1.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Assert
        var info1 = (await _roomsApi.GetRoomInfoAsync(room1.Id, TestContext.Current.CancellationToken)).Response;
        var info2 = (await _roomsApi.GetRoomInfoAsync(room2.Id, TestContext.Current.CancellationToken)).Response;
        (info1.Tags ?? []).Should().NotContain(tagName);
        (info2.Tags ?? []).Should().Contain(tagName);
    }

    [Fact]
    public async Task DeleteRoomTags_DetachOneTagFromRoomWithManyTags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Many Tags");
        string[] all = ["Many1", "Many2", "Many3", "Many4", "Many5"];
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(all.ToList()), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["Many3"]), TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Tags.Should().NotContain("Many3");
        updated.Tags.Should().HaveCount(4);
        updated.Tags.Should().Contain(["Many1", "Many2", "Many4", "Many5"]);
    }

    [Fact]
    public async Task DeleteRoomTags_DetachAllTagsLeavesRoomWithEmptyTags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Detach All");
        string[] names = ["All1", "All2", "All3"];
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(names.ToList()), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(names.ToList()), TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteRoomTags_RepeatedDetachOfSameTag_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Detach Idempotent");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["IdemTag"]), TestContext.Current.CancellationToken);
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["IdemTag"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["IdemTag"]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().NotContain("IdemTag");
    }

    [Fact]
    public async Task DeleteRoomTags_DetachesTagWithCyrillicName()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "Тег Кириллица";
        var room = await CreateCustomRoom("Autotest Cyrillic Tag");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().NotContain(tagName);
    }

    [Fact]
    public async Task DeleteRoomTags_DetachesTagWithSpacesInsideName()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "release candidate";
        var room = await CreateCustomRoom("Autotest Spaces Tag");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().NotContain(tagName);
    }

    [Fact]
    public async Task DeleteRoomTags_DetachesTagWithSpecialCharactersInName()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "tag-1_qa.test";
        var room = await CreateCustomRoom("Autotest Special Chars Tag");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().NotContain(tagName);
    }

    [Fact]
    public async Task DeleteRoomTags_TagInCatalogButNotAttachedToRoom_IsNoOp()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("CatalogOnly"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Detach Not Attached");

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["CatalogOnly"]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().NotContain("CatalogOnly");
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().Contain("CatalogOnly");
    }

    [Fact]
    public async Task DeleteRoomTags_TagThatDoesNotExistInCatalog_IsNoOp()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Detach Ghost Tag");

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["NeverExisted"]), TestContext.Current.CancellationToken);

        // Assert — no exception means success (200).
    }

    [Fact]
    public async Task DeleteRoomTags_MixOfAttachedAndNonAttached_RemovesOnlyAttached()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Mix Attached");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["AttachedX"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["AttachedX", "NotAttachedY"]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Should().NotContain("AttachedX");
        (updated.Tags ?? []).Should().NotContain("NotAttachedY");
    }

    /// <remarks>
    /// Corrected against the product source (<c>BatchTagsRequestDto.Validate</c> in
    /// <c>ASC.Files.ApiModels.RequestDto</c>): a blank entry anywhere in <c>names</c> fails model
    /// validation for the whole request by design ("a tag name is never blank"), it is not
    /// tolerated as a no-op alongside valid names. The TS source assumed the mixed request would
    /// still remove the valid tag; that assumption does not hold against the current DTO.
    /// </remarks>
    [Fact]
    public async Task DeleteRoomTags_MixOfValidAndEmptyStringNames_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Mix Valid Invalid");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["ValidTag"]), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["ValidTag", ""]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task DeleteRoomTags_CaseInsensitive_DifferentCaseNameDetachesTheTag()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Case Insensitive Detach");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["QA"]), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto(["qa"]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (updated.Tags ?? []).Count(t => t.Equals("qa", StringComparison.OrdinalIgnoreCase)).Should().Be(0);
    }

    [Fact]
    public async Task DeleteRoomTags_GlobalTagDeletionAlreadyRemovedTag_SubsequentDetachIsNoOp()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "AboutToVanish";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Detach After Global Delete");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
        var infoAfterGlobalDelete = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        (infoAfterGlobalDelete.Tags ?? []).Should().NotContain(tagName);

        // Act
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Assert — no exception means success (200).
    }

    [Fact]
    public async Task DeleteRoomTags_AddDetachAdd_RestoresTag()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "AddDetachAddTag";
        var room = await CreateCustomRoom("Autotest Add Detach Add");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
        await _roomsApi.DeleteRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        var readded = (await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (readded.Tags ?? []).Should().Contain(tagName);
    }
}
