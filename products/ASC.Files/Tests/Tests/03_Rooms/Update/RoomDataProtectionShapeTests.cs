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

namespace ASC.Files.Tests.Tests._03_Rooms.Update;

/// <summary>
/// The shape of the lifetime and watermark settings of a room: a lifetime <c>value</c> of 1 to 999 with a Day, Month
/// or Year period, and a watermark <c>text</c> of up to 255 characters with any combination of the
/// <see cref="WatermarkAdditions"/> flags.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomDataProtectionShapeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(1, RoomDataLifetimePeriod.Day)]
    [InlineData(999, RoomDataLifetimePeriod.Month)]
    [InlineData(500, RoomDataLifetimePeriod.Year)]
    public async Task UpdateRoom_LifetimeValueWithinOneTo999_IsStoredWithItsPeriod(int value, RoomDataLifetimePeriod period)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Lifetime Bounds");

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(lifetime: new RoomDataLifetimeDto(deletePermanently: true, period: period, value: value, enabled: true)),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        foreach (var lifetime in new[] { updated.Lifetime, info.Lifetime })
        {
            lifetime.Should().NotBeNull();
            lifetime.Value.Should().Be(value);
            lifetime.Period.Should().Be(period);
            lifetime.DeletePermanently.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(-1)]
    public async Task UpdateRoom_LifetimeValueOutsideOneTo999_Returns400AndKeepsNoLifetime(int value)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Lifetime Out Of Range");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest(lifetime: new RoomDataLifetimeDto(period: RoomDataLifetimePeriod.Day, value: value, enabled: true)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Lifetime.Should().BeNull();
    }

    [Fact]
    public async Task CreateRoom_LifetimeValueOver999_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto(
                    "Autotest Lifetime Create 1000",
                    roomType: RoomType.VirtualDataRoom,
                    lifetime: new RoomDataLifetimeDto(period: RoomDataLifetimePeriod.Day, value: 1000, enabled: true)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateRoom_WatermarkTextOf255Characters_IsStored()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Watermark 255");
        var text = new string('w', 255);

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(watermark: new WatermarkRequestDto(enabled: true, text: text)),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Watermark.Should().NotBeNull();
        updated.Watermark.Text.Should().Be(text);
    }

    [Fact]
    public async Task UpdateRoom_WatermarkTextOf256Characters_Returns400AndKeepsNoWatermark()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Watermark 256");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest(watermark: new WatermarkRequestDto(enabled: true, text: new string('w', 256))),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Watermark.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRoom_WatermarkWithEveryAddition_StoresTheCombinedFlagsAndRotation()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Watermark Additions");
        var all = WatermarkAdditions.UserName | WatermarkAdditions.UserEmail | WatermarkAdditions.UserIpAdress
            | WatermarkAdditions.CurrentDate | WatermarkAdditions.RoomName;

        // Act
        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(watermark: new WatermarkRequestDto(enabled: true, additions: all, text: "Confidential", rotate: -45)),
            TestContext.Current.CancellationToken);

        // Assert
        var watermark = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response.Watermark;
        watermark.Should().NotBeNull();
        ((int)watermark.Additions).Should().Be(31);
        watermark.Text.Should().Be("Confidential");
        watermark.Rotate.Should().Be(-45);
    }

    [Fact]
    public async Task UpdateRoom_WatermarkWithTwoAdditions_StoresExactlyThoseTwo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Watermark Two Additions");

        // Act
        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserEmail | WatermarkAdditions.RoomName)),
            TestContext.Current.CancellationToken);

        // Assert
        var watermark = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response.Watermark;
        watermark.Should().NotBeNull();
        ((int)watermark.Additions).Should().Be(18);
    }
}
