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
/// Functional coverage of <c>DELETE /files/tags</c> (removing tags from the global catalog).
/// Permission coverage lives in <c>Permissions/RoomCustomTagDeletePermissionsTests</c> and
/// <c>Permissions/RoomTagDeletePermissionsTests</c>; malformed-body coverage lives in
/// <c>Permissions/RoomCustomTagValidationPermissionsTests</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomCustomTagDeleteTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task DeleteCustomTags_OwnerDeletesSeveralExistingTagsInOneRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        string[] names = ["BatchTagA", "BatchTagB", "BatchTagC"];
        foreach (var name in names)
        {
            await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        }

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto(names.ToList()), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        foreach (var name in names)
        {
            list.Should().NotContain(name);
        }
    }

    [Fact]
    public async Task DeleteCustomTags_DeletingTagRemovesItFromRoomWhereAttached()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "AttachedTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room With Attached Tag");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        var infoBefore = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        (infoBefore.Tags ?? []).Should().Contain(tagName);

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Assert
        var infoAfter = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        (infoAfter.Tags ?? []).Should().NotContain(tagName);
    }

    [Fact]
    public async Task DeleteCustomTags_BatchDeleteWorksAcrossDifferentRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagA = "MultiRoomTagA";
        const string tagB = "MultiRoomTagB";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagA), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagB), TestContext.Current.CancellationToken);

        var room1 = await CreateCustomRoom("Autotest Multi Room A");
        var room2 = await CreateCustomRoom("Autotest Multi Room B");
        await _roomsApi.AddRoomTagsAsync(room1.Id, new BatchTagsRequestDto([tagA]), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(room2.Id, new BatchTagsRequestDto([tagB]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([tagA, tagB]), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(tagA);
        list.Should().NotContain(tagB);

        var info1 = (await _roomsApi.GetRoomInfoAsync(room1.Id, TestContext.Current.CancellationToken)).Response;
        var info2 = (await _roomsApi.GetRoomInfoAsync(room2.Id, TestContext.Current.CancellationToken)).Response;
        (info1.Tags ?? []).Should().NotContain(tagA);
        (info2.Tags ?? []).Should().NotContain(tagB);
    }

    [Fact]
    public async Task DeleteCustomTags_DuplicateNamesInArray_DeletesOnceWithoutError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "DuplicateDeleteTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([name, name]), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(name);
    }

    [Fact]
    public async Task DeleteCustomTags_DeletingNonExistentTag_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto(["NonExistingTag"]), TestContext.Current.CancellationToken);

        // Assert — no exception means success; DeleteCustomTagsAsync returns plain Task.
    }

    [Fact]
    public async Task DeleteCustomTags_BatchWithExistingAndNonExisting_DeletesTheExistingOne()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string existing = "ExistingMixedTag";
        const string missing = "NonExistingMixedTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(existing), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([existing, missing]), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(existing);
        list.Should().NotContain(missing);
    }

    /// <remarks>
    /// A 10000-character tag name used to be silently accepted (200) instead of producing a
    /// validation error (400) — no length guard on the deleted names.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81689")]
    public async Task DeleteCustomTags_VeryLongTagName_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteCustomTagsAsync(
                new BatchTagsRequestDto([new string('a', 10000)]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task DeleteCustomTags_CyrillicTagName_CanBeDeleted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "Тег Кириллица Delete";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(name);
    }

    [Fact]
    public async Task DeleteCustomTags_TagDeletionIsCaseInsensitive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("CaseSensitiveDeleteTag"), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto(["casesensitivedeletetag"]), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain("CaseSensitiveDeleteTag");
        list.Count(t => t is string s && s.Equals("casesensitivedeletetag", StringComparison.OrdinalIgnoreCase)).Should().Be(0);
    }

    [Fact]
    public async Task DeleteCustomTags_RemovesTagFromAllRoomsWhereAttached()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagName = "GlobalSharedDeleteTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(tagName), TestContext.Current.CancellationToken);

        var room1 = await CreateCustomRoom("Autotest Global Tag Room A");
        var room2 = await CreateCustomRoom("Autotest Global Tag Room B");
        await _roomsApi.AddRoomTagsAsync(room1.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(room2.Id, new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([tagName]), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(tagName);

        var info1 = (await _roomsApi.GetRoomInfoAsync(room1.Id, TestContext.Current.CancellationToken)).Response;
        var info2 = (await _roomsApi.GetRoomInfoAsync(room2.Id, TestContext.Current.CancellationToken)).Response;
        (info1.Tags ?? []).Should().NotContain(tagName);
        (info2.Tags ?? []).Should().NotContain(tagName);
    }

    [Fact]
    public async Task DeleteCustomTags_DoesNotDeleteUnrelatedTags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string tagA = "UnrelatedTagA";
        const string tagB = "UnrelatedTagB";
        const string tagC = "UnrelatedTagC";
        foreach (var name in new[] { tagA, tagB, tagC })
        {
            await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        }

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([tagA]), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(tagA);
        list.Should().Contain(tagB);
        list.Should().Contain(tagC);
    }

    [Fact]
    public async Task DeleteCustomTags_DoesNotDeleteTagsWithSimilarNamesOrPrefixes()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string exact = "PrefixTag";
        string[] similar = ["PrefixTag1", "PrefixTag-1", "PrefixTagExtra"];
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(exact), TestContext.Current.CancellationToken);
        foreach (var name in similar)
        {
            await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        }

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([exact]), TestContext.Current.CancellationToken);

        // Assert
        var list = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Should().NotContain(exact);
        foreach (var name in similar)
        {
            list.Should().Contain(name);
        }
    }

    [Fact]
    public async Task DeleteCustomTags_RepeatedDeleteOfSameTag_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "RepeatedDeleteTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Assert — the second call did not throw.
    }

    [Fact]
    public async Task DeleteCustomTags_DeletedTagNameCanBeReusedAndAttachedAgain()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "ReusedAfterDeleteTag";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        await _roomsApi.DeleteCustomTagsAsync(new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Room Reuse Tag");
        var attached = (await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken)).Response;

        // Assert
        (attached.Tags ?? []).Should().Contain(name);
    }
}
