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
/// GET /files/tags (getRoomTagsInfo) — response shape, catalog contents (auto-created via
/// <c>createRoomTag</c>/<c>addRoomTags</c>, deduplicated across rooms) and <c>filterValue</c>
/// filtering. Pagination, input validation, garbage collection and scope isolation live in
/// <see cref="RoomTagsInfoPaginationTests"/>, split purely to stay under the ~24-case
/// class-size guideline. Role-based visibility of the catalog is already covered by
/// <c>Permissions/RoomTagCreatePermissionsTests</c> (<c>GetTags_*</c> cases) and is not
/// duplicated here.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagsInfoTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetRoomTagsInfo_ReturnsArrayOfStrings()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("ShapeTag"), TestContext.Current.CancellationToken);

        // Act — inspect the raw element type before it is unwrapped to a string.
        var rawTags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        rawTags.Should().NotBeEmpty();
        rawTags.Should().AllSatisfy(t => t.Should().BeOfType<string>());
    }

    [Fact]
    public async Task GetRoomTagsInfo_CleanPortal_ReturnsEmptyArray()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var tags = await GetTagCatalog();

        // Assert
        tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomTagsInfo_TagCreatedViaCreateRoomTag_AppearsInList()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("ContractCreatedTag"), TestContext.Current.CancellationToken);

        // Act
        var tags = await GetTagCatalog();

        // Assert
        tags.Should().Contain("ContractCreatedTag");
    }

    [Fact]
    public async Task GetRoomTagsInfo_AddRoomTagsWithSeveralNewTags_AllAppearInCatalog()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        string[] names = ["MultiTagA", "MultiTagB", "MultiTagC"];
        var room = await CreateCustomRoom("Autotest Multi Tag Room");

        // Act
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto([.. names]), TestContext.Current.CancellationToken);

        // Assert
        var tags = await GetTagCatalog();
        tags.Should().Contain(names);
    }

    [Fact]
    public async Task GetRoomTagsInfo_SameTagOnSeveralRooms_AppearsOnce()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "SharedAcrossRooms";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);

        var room1 = await CreateCustomRoom("Autotest Shared Tag Room A");
        var room2 = await CreateCustomRoom("Autotest Shared Tag Room B");
        await _roomsApi.AddRoomTagsAsync(room1.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);
        await _roomsApi.AddRoomTagsAsync(room2.Id, new BatchTagsRequestDto([name]), TestContext.Current.CancellationToken);

        // Act
        var tags = await GetTagCatalog();

        // Assert
        tags.Count(t => t == name).Should().Be(1);
    }

    [Fact]
    public async Task GetRoomTagsInfo_FilterValueCaseInsensitive()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("CaseSearchTag"), TestContext.Current.CancellationToken);

        // Act
        var tags = await GetTagCatalog(filterValue: "casesearchtag");

        // Assert
        tags.Should().Contain("CaseSearchTag");
    }

    [Fact]
    public async Task GetRoomTagsInfo_FilterValueReturnsOnlyMatchingTags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        foreach (var name in new[] { "AlphaTag", "BetaTag", "GammaTag" })
        {
            await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        }

        // Act
        var tags = await GetTagCatalog(filterValue: "Alpha");

        // Assert
        tags.Should().Contain("AlphaTag").And.NotContain("BetaTag").And.NotContain("GammaTag");
    }

    [Fact]
    public async Task GetRoomTagsInfo_FilterValueMatchesSubstring()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("ReleaseSmokeTag"), TestContext.Current.CancellationToken);

        // Act
        var tags = await GetTagCatalog(filterValue: "Smoke");

        // Assert
        tags.Should().Contain("ReleaseSmokeTag");
    }

    [Fact]
    public async Task GetRoomTagsInfo_FilterValueNoMatches_ReturnsEmptyArray()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("RealTag"), TestContext.Current.CancellationToken);

        // Act
        var tags = await GetTagCatalog(filterValue: $"nomatch-{Guid.NewGuid():N}");

        // Assert
        tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoomTagsInfo_FilterValueUnicodeCharacters_ReturnsMatchingTags()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string name = "ТестовыйТег";
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);

        // Act
        var tags = await GetTagCatalog(filterValue: "Тест");

        // Assert
        tags.Should().Contain(name);
    }

    [Fact]
    public async Task GetRoomTagsInfo_FilterValueSpecialCharacters_DoesNotError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("RegularTag"), TestContext.Current.CancellationToken);

        // Act
        var tags = await GetTagCatalog(filterValue: "test %_$ #@!");

        // Assert
        tags.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRoomTagsInfo_EmptyFilterValue_SameAsNoFilter()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        foreach (var name in new[] { "EmptyFilterA", "EmptyFilterB" })
        {
            await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        }

        // Act
        var unfiltered = await GetTagCatalog();
        var filtered = await GetTagCatalog(filterValue: "");

        // Assert
        filtered.Should().BeEquivalentTo(unfiltered);
    }

    [Fact]
    public async Task GetRoomTagsInfo_WhitespaceOnlyFilterValue_SameAsNoFilter()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        foreach (var name in new[] { "SpaceFilterA", "SpaceFilterB" })
        {
            await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(name), TestContext.Current.CancellationToken);
        }

        // Act
        var unfiltered = await GetTagCatalog();
        var filtered = await GetTagCatalog(filterValue: "   ");

        // Assert
        filtered.Should().BeEquivalentTo(unfiltered);
    }

    /// <summary>Reads the tag catalog and unwraps it into plain strings.</summary>
    private async Task<List<string>> GetTagCatalog(int? count = null, int? startIndex = null, string? filterValue = null)
    {
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(count, startIndex, filterValue, TestContext.Current.CancellationToken)).Response;

        return tags.ConvertAll(t => t.ToString()!);
    }
}
