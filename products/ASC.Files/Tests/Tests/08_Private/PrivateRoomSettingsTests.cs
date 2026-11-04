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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// The room settings that need the content in the clear, or that make the server touch the content
/// by itself, do not apply to a private room: creating or updating one with them is refused.
/// Turning such a setting off stays possible, so a room created before the check can be repaired.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomSettingsTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private static RoomDataLifetimeDto Lifetime(bool enabled = true) =>
        new(deletePermanently: false, period: RoomDataLifetimePeriod.Day, value: 30, enabled: enabled);

    private static WatermarkRequestDto Watermark(bool enabled = true) =>
        new(enabled: enabled, additions: WatermarkAdditions.UserName, text: "Confidential");

    private async Task AssertCreateRefused(CreateRoomRequestDto request)
    {
        await _filesClient.Authenticate(Owner);
        await EnsureEncryptionKeys();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(request, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    private async Task AssertUpdateRefused(UpdateRoomRequest request)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(room.Id, request, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public Task CreateRoom_WithIndexing_IsRefused() =>
        AssertCreateRefused(new CreateRoomRequestDto("Autotest", indexing: true, roomType: RoomType.CustomRoom, @private: true));

    [Fact]
    public Task CreateRoom_WithLifetime_IsRefused() =>
        AssertCreateRefused(new CreateRoomRequestDto("Autotest", lifetime: Lifetime(), roomType: RoomType.CustomRoom, @private: true));

    [Fact]
    public Task CreateRoom_WithWatermark_IsRefused() =>
        AssertCreateRefused(new CreateRoomRequestDto("Autotest", watermark: Watermark(), roomType: RoomType.CustomRoom, @private: true));

    [Fact]
    public Task UpdateRoom_EnablingIndexing_IsRefused() => AssertUpdateRefused(new UpdateRoomRequest(indexing: true));

    [Fact]
    public Task UpdateRoom_SettingLifetime_IsRefused() => AssertUpdateRefused(new UpdateRoomRequest(lifetime: Lifetime()));

    [Fact]
    public Task UpdateRoom_SettingWatermark_IsRefused() => AssertUpdateRefused(new UpdateRoomRequest(watermark: Watermark()));

    [Fact]
    public async Task UpdateRoom_TurningTheSettingsOff_IsAllowed()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(indexing: false, lifetime: Lifetime(enabled: false), watermark: Watermark(enabled: false)),
            TestContext.Current.CancellationToken)).Response;

        updated.Indexing.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateRoom_Rename_IsAllowed()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        var updated = (await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest("Renamed"), TestContext.Current.CancellationToken)).Response;

        updated.Title.Should().Be("Renamed");
    }
}
