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

namespace ASC.Files.Tests.Tests._03_Rooms.Covers;

/// <summary>
/// PUT /files/rooms/{id}/cover — payload validation. The colour must be a bare six-digit RRGGBB
/// hex string; anything else is rejected, while omitted fields leave the current value alone.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomCoverValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData("#FF5733", "a leading # is not part of the expected format")]
    [InlineData("ZZZZZZ", "non-hex characters")]
    [InlineData("1234567", "seven digits is too long")]
    public async Task ChangeCover_InvalidColor_BadRequest(string color, string because)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Cover Invalid Color {color}");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(
                room.Id,
                new CoverRequestDto(color),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400, because);
    }

    /// <remarks>
    /// BUG 81558: a colour that is too short ("123") is silently accepted with 200 and treated as a
    /// no-op instead of failing validation. Marked <c>test.fail</c> in the TypeScript suite.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81558")]
    public async Task ChangeCover_TooShortColor_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Cover Short Hex Room");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(
                room.Id,
                new CoverRequestDto("123"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task ChangeCover_InvalidCoverId_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Cover Invalid Cover Id Room");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ChangeRoomCoverAsync(
                room.Id,
                new CoverRequestDto(cover: "invalid-cover-id"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Theory]
    [InlineData("ff5733")]
    [InlineData("Ff5733")]
    public async Task ChangeCover_HexColorCasing_Accepted(string color)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Cover Hex Casing {color}");

        // Act
        var updated = (await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto(color),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Logo.Color.Should().BeEquivalentTo("ff5733");
    }

    [Fact]
    public async Task ChangeCover_EmptyColor_ResetsToDefault()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Cover Empty Color Room");

        await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("ABCDEF"),
            TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto(string.Empty),
            TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Logo.Color.Should().NotBeNullOrEmpty();
        info.Logo.Color.Should().NotBe("ABCDEF");
        info.Logo.Color.Should().MatchRegex("^[0-9A-Fa-f]{6}$");
    }

    [Fact]
    public async Task ChangeCover_EmptyBody_LeavesCoverUnchanged()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var coverId = await GetFirstCoverId();
        var room = await CreateCustomRoom("Autotest Cover Empty Body Room");

        await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto("ABCDEF", coverId),
            TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.ChangeRoomCoverAsync(
            room.Id,
            new CoverRequestDto(),
            TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Logo.Color.Should().Be("ABCDEF");
        info.Logo.Cover.Id.Should().Be(coverId);
    }

}
