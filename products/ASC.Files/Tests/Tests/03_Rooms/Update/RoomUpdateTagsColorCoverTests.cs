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

namespace ASC.Files.Tests.Tests._03_Rooms.Update;

/// <summary>
/// PUT /files/rooms/{id} - tags, color and cover, which together make up the room "logo" and
/// classification surface: add/replace/clear/dedupe semantics for tags, and the set/replace/reset
/// contract for color and cover, plus the validation each of them rejects.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomUpdateTagsColorCoverTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateRoom_Tags_AddReplaceClearDedupe()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Tags Room");

        // Act & Assert - add a single tag
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(tags: ["AutotestTagA"]), TestContext.Current.CancellationToken);
        (await TagsOf(room.Id)).Should().Equal("AutotestTagA");

        // Act & Assert - add multiple tags (order not guaranteed)
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(tags: ["AutotestTagB", "AutotestTagC"]), TestContext.Current.CancellationToken);
        (await TagsOf(room.Id)).Should().Contain(["AutotestTagB", "AutotestTagC"]);

        // Act & Assert - replace the list
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(tags: ["AutotestTagD"]), TestContext.Current.CancellationToken);
        (await TagsOf(room.Id)).Should().Equal("AutotestTagD");

        // Act & Assert - clear via empty array
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(tags: []), TestContext.Current.CancellationToken);
        (await TagsOf(room.Id)).Should().BeEmpty();

        // Act & Assert - duplicates are deduplicated
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(tags: ["AutotestDup", "AutotestDup"]), TestContext.Current.CancellationToken);
        (await TagsOf(room.Id)).Should().Equal("AutotestDup");
    }

    [Fact]
    public async Task UpdateRoom_OverlongTag_IsRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Long Tag Room");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest(tags: [new string('T', 300)]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await TagsOf(room.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateRoom_TagWithForbiddenChars_IsStoredVerbatim()
    {
        // Arrange - tags are NOT sanitized the way titles are: forbidden chars are stored as-is.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Tag Chars Room");
        const string tag = "Bad<>/\"\\";

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(tags: [tag]), TestContext.Current.CancellationToken);

        // Assert
        (await TagsOf(room.Id)).Should().Contain(tag);
    }

    [Fact]
    public async Task UpdateRoom_Color_SetReplaceEmptyResetsNullNoOp()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Color Room");

        // Act & Assert - set a valid color
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(color: "FF5733"), TestContext.Current.CancellationToken);
        (await ColorOf(room.Id)).Should().Be("FF5733");

        // Act & Assert - replace the color
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(color: "00AA00"), TestContext.Current.CancellationToken);
        (await ColorOf(room.Id)).Should().Be("00AA00");

        // Act & Assert - an empty string resets to a new, different, valid hex color
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(color: ""), TestContext.Current.CancellationToken);
        var reset = await ColorOf(room.Id);
        reset.Should().MatchRegex("^[0-9A-Fa-f]{6}$");
        reset.Should().NotBe("00AA00");

        // Act & Assert - null is a no-op
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(color: null), TestContext.Current.CancellationToken);
        (await ColorOf(room.Id)).Should().Be(reset);
    }

    [Fact]
    public async Task UpdateRoom_ColorWithLeadingHash_IsRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Color Hash Room");
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(color: "FF5733"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest(color: "#FF5733"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await ColorOf(room.Id)).Should().Be("FF5733");
    }

    /// <summary>
    /// The API validates some color formats (rejects a leading '#' with 400) but still accepts
    /// clearly-invalid non-hex values, which is inconsistent.
    /// </summary>
    [Theory]
    [Trait("Bug", "82364")]
    [InlineData("ZZZZZZ")]
    [InlineData("123")]
    public async Task UpdateRoom_InvalidNonHexColor_ShouldBeRejected(string color)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Bad Color Room");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest(color: color),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateRoom_Cover_SetReplaceEmptyClears()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var covers = (await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken)).Response;
        var coverId = covers[0].Id;
        var coverId2 = covers[1].Id;
        var room = await CreateCustomRoom("Autotest Cover Room");

        // Act & Assert - set a valid cover
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(cover: coverId), TestContext.Current.CancellationToken);
        (await CoverOf(room.Id)).Should().Be(coverId);

        // Act & Assert - replace the cover
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(cover: coverId2), TestContext.Current.CancellationToken);
        (await CoverOf(room.Id)).Should().Be(coverId2);

        // Act & Assert - an empty string clears the cover
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(cover: ""), TestContext.Current.CancellationToken);
        (await CoverOf(room.Id)).Should().BeNull();
    }

    [Fact]
    public async Task UpdateRoom_InvalidCoverId_IsRejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Bad Cover Room");
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(cover: coverId), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest(cover: "does-not-exist"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await CoverOf(room.Id)).Should().Be(coverId);
    }

    private async Task<List<string>> TagsOf(int roomId)
    {
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        return info.Tags ?? [];
    }

    private async Task<string?> ColorOf(int roomId)
    {
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        return info.Logo?.Color;
    }

    private async Task<string?> CoverOf(int roomId)
    {
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        return info.Logo?.Cover?.Id;
    }
}
