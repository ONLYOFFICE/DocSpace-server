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
/// PUT /files/rooms/{id} - the fields that only make sense on specific room types: indexing and
/// deny-download, watermark and lifetime for a Virtual Data Room, and the form-filling switches
/// for a Filling Forms Room.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomUpdateVdrFieldsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateRoom_ToggleIndexingOff_OnVdrRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest VDR Index Off");
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(indexing: true), TestContext.Current.CancellationToken);

        // Act
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(indexing: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Indexing.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateRoom_CustomRoom_AcceptsIndexingTrue()
    {
        // Arrange - the backend does not restrict `indexing` to VDR rooms: a CustomRoom accepts it too.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Custom Index");

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(indexing: true), TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Indexing.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRoom_ToggleDenyDownload_OnVdrRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Deny Download");

        // Act & Assert - on
        var on = (await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(denyDownload: true), TestContext.Current.CancellationToken)).Response;
        on.DenyDownload.Should().BeTrue();

        // Act & Assert - off
        var off = (await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(denyDownload: false), TestContext.Current.CancellationToken)).Response;
        off.DenyDownload.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateRoom_Watermark_ChangeAndDisable_OnVdrRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Watermark");

        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserName, text: "Conf", rotate: 0, imageScale: 100)),
            TestContext.Current.CancellationToken);

        // Act - change the watermark
        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserEmail, text: "Secret", rotate: 45, imageScale: 50)),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.Watermark.Text.Should().Be("Secret");

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Watermark.Text.Should().Be("Secret");
        info.Watermark.Additions.Should().Be(WatermarkAdditions.UserEmail);
        info.Watermark.Rotate.Should().Be(45);

        // Act - disable the watermark
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(watermark: new WatermarkRequestDto(enabled: false)), TestContext.Current.CancellationToken);

        // Assert
        var afterDisable = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        afterDisable.Watermark.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRoom_Lifetime_ChangeDisableRejectNegative()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Lifetime");

        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(lifetime: new RoomDataLifetimeDto(deletePermanently: true, period: RoomDataLifetimePeriod.Day, value: 30, enabled: true)),
            TestContext.Current.CancellationToken);

        // Act - change the lifetime
        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(lifetime: new RoomDataLifetimeDto(deletePermanently: false, period: RoomDataLifetimePeriod.Month, value: 6, enabled: true)),
            TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Lifetime.Value.Should().Be(6);
        info.Lifetime.Period.Should().Be(RoomDataLifetimePeriod.Month);
        info.Lifetime.DeletePermanently.Should().BeFalse();

        // Act - disable the lifetime
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(lifetime: new RoomDataLifetimeDto(enabled: false)), TestContext.Current.CancellationToken);

        // Assert
        var afterDisable = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        afterDisable.Lifetime.Should().BeNull();

        // Act & Assert - a negative lifetime value is rejected
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest(lifetime: new RoomDataLifetimeDto(deletePermanently: true, period: RoomDataLifetimePeriod.Day, value: -5, enabled: true)),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateRoom_FormSettings_OnFillingFormsRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Form Settings");

        // Act & Assert - enable both
        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(sendFormToExternalDB: true, saveFormAsXLSX: true),
            TestContext.Current.CancellationToken);
        var enabled = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        enabled.SendFormToExternalDB.Should().BeTrue();
        enabled.SaveFormAsXLSX.Should().BeTrue();

        // Act & Assert - disable one (partial update)
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(saveFormAsXLSX: false), TestContext.Current.CancellationToken);
        var partial = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        partial.SendFormToExternalDB.Should().BeTrue();
        partial.SaveFormAsXLSX.Should().BeFalse();

        // Act & Assert - disable both
        await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(sendFormToExternalDB: false, saveFormAsXLSX: false),
            TestContext.Current.CancellationToken);
        var disabled = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        disabled.SendFormToExternalDB.Should().BeFalse();
        disabled.SaveFormAsXLSX.Should().BeFalse();
    }
}
