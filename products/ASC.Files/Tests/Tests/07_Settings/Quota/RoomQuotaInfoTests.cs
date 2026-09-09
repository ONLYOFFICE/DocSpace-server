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
/// How <c>GET /api/2.0/rooms/{id}</c> reports a room's quota once one has been set through
/// <c>PUT /api/2.0/files/rooms/roomquota</c>: the exhaustion error on file creation, and the
/// <c>usedSpace</c> counter the room accumulates as files are created in it.
/// </summary>
[Trait("Category", "Settings")]
[Trait("Feature", "Quota")]
public class RoomQuotaInfoTests(
    AspireAppFixture fixture)
    : QuotaTestBase(fixture)
{
    [Fact]
    public async Task CreateFile_RoomQuotaExhausted_ReturnsError()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota();

        var room = await CreateCustomRoom("Autotest Quota Exhausted Room " + Guid.NewGuid().ToString()[..8]);

        await _quotaApi.UpdateRoomsQuotaAsync(
            new UpdateRoomsQuotaRequestDtoInteger([new(room.Id)], 1),
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ApiException>(async () =>
            await CreateFile("Autotest Quota File.docx", room.Id));
    }

    [Fact]
    public async Task GetRoomInfo_ReflectsQuotaLimitAndUsedSpace_AfterQuotaSet()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota();

        var room = await CreateCustomRoom("Autotest Quota Info Room " + Guid.NewGuid().ToString()[..8]);

        await _quotaApi.UpdateRoomsQuotaAsync(
            new UpdateRoomsQuotaRequestDtoInteger([new(room.Id)], QuotaMinimalBytes),
            TestContext.Current.CancellationToken);

        await CreateFile("Autotest Quota Info File.docx", room.Id);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        info.QuotaLimit.Should().Be(QuotaMinimalBytes);
        info.UsedSpace.Should().NotBeNull();
        info.UsedSpace!.Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task RoomUsedSpace_StaysWithinQuota_AfterCreateFile()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        await EnableRoomQuota();

        var room = await CreateCustomRoom("Autotest Quota Used Space Room " + Guid.NewGuid().ToString()[..8]);

        await _quotaApi.UpdateRoomsQuotaAsync(
            new UpdateRoomsQuotaRequestDtoInteger([new(room.Id)], QuotaMinimalBytes),
            TestContext.Current.CancellationToken);

        await CreateFile("Autotest Quota Track File.docx", room.Id);

        // usedSpace is updated asynchronously after the file is created, so poll for it rather
        // than trusting a single read right after the write.
        var info = await WaitForRoomInfo(room.Id, r => r.UsedSpace is > 0);

        info.UsedSpace.Should().NotBeNull();
        info.UsedSpace!.Value.Should().BeLessThanOrEqualTo(info.QuotaLimit!.Value);
    }

    private async Task<FolderDtoInteger> WaitForRoomInfo(int roomId, Func<FolderDtoInteger, bool> until)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;

            if (until(info) || DateTime.UtcNow >= deadline)
            {
                return info;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
