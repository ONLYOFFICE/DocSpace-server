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

namespace ASC.Files.Tests.Tests._07_Settings.Quota;

/// <summary>
/// <c>PUT /api/2.0/files/rooms/resetquota</c>: the scenarios not already covered by
/// <see cref="QuotaTests"/> - resetting a room that never had a custom quota, empty payloads,
/// archived rooms, the feature toggle and every room type the endpoint accepts.
/// </summary>
[Trait("Category", "Settings")]
[Trait("Feature", "Quota")]
public class RoomQuotaResetTests(
    AspireAppFixture fixture)
    : QuotaTestBase(fixture)
{
    [Fact]
    public async Task ResetRoomQuota_WithoutCustomQuota_ReturnsDefaultAndFlagFalse()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota();

        var room = await CreateCustomRoom("Autotest No Custom Quota Room " + Guid.NewGuid().ToString()[..8]);

        var result = (await _quotaApi.ResetRoomQuotaAsync(
            new UpdateRoomsRoomIdsRequestDtoInteger([new(room.Id)]),
            TestContext.Current.CancellationToken)).Response;

        result.Should().ContainSingle();
        result[0].IsCustomQuota.Should().BeFalse();
    }

    [Fact]
    public async Task ResetRoomQuota_EmptyRoomIds_ReturnsEmptyArray()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota();

        var result = (await _quotaApi.ResetRoomQuotaAsync(
            new UpdateRoomsRoomIdsRequestDtoInteger([]),
            TestContext.Current.CancellationToken)).Response;

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ResetRoomQuota_ArchivedRoom_ReturnsForbidden()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota();

        var room = await CreateCustomRoom("Autotest Archived Reset Room " + Guid.NewGuid().ToString()[..8]);

        await _quotaApi.UpdateRoomsQuotaAsync(
            new UpdateRoomsQuotaRequestDtoInteger([new(room.Id)], QuotaMinimalBytes),
            TestContext.Current.CancellationToken);

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _quotaApi.ResetRoomQuotaAsync(
                new UpdateRoomsRoomIdsRequestDtoInteger([new(room.Id)]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// BUG 82293: the API answered 200 instead of rejecting the reset when the room-quota feature
    /// was never enabled for this portal. Fixed by making <c>FolderQuotaChangeAsync</c> refuse when
    /// <c>TenantRoomQuotaSettings.EnableQuota</c> is off.
    /// </summary>
    [Trait("Bug", "82293")]
    [Fact]
    public async Task ResetRoomQuota_FeatureDisabled_ReturnsForbidden()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Reset Quota Feature Disabled Room " + Guid.NewGuid().ToString()[..8]);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _quotaApi.ResetRoomQuotaAsync(
                new UpdateRoomsRoomIdsRequestDtoInteger([new(room.Id)]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.FillingFormsRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task ResetRoomQuota_RoomTypeVariants_ResetsToDefault(RoomType roomType)
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota();

        var room = await CreateRoomOfType(roomType, "Autotest Reset Quota Room Type " + Guid.NewGuid().ToString()[..8]);

        await _quotaApi.UpdateRoomsQuotaAsync(
            new UpdateRoomsQuotaRequestDtoInteger([new(room.Id)], QuotaMinimalBytes),
            TestContext.Current.CancellationToken);

        var result = (await _quotaApi.ResetRoomQuotaAsync(
            new UpdateRoomsRoomIdsRequestDtoInteger([new(room.Id)]),
            TestContext.Current.CancellationToken)).Response;

        result.Should().ContainSingle();
        result[0].QuotaLimit.Should().Be(DefaultQuotaRoomBytes);
        result[0].IsCustomQuota.Should().BeFalse();
    }
}
