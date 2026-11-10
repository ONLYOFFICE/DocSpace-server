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

namespace ASC.Files.Tests.Tests._03_Rooms.ThirdParty;

/// <summary>
/// A connected storage cannot hold a private room: the keys of its files have nowhere to be kept, so the room used to be
/// created and nobody could ever open what was put into it. Skipped unless Nextcloud is configured in the environment.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "PrivacyRoom")]
public class ThirdPartyPrivateRoomTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrivateRoom_IsRefusedWithOrWithoutKeys_AndTheConnectionStaysFree(bool withKeys)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await _filesClient.Authenticate(Owner);

        if (withKeys)
        {
            await _privacyRoomApi.SetKeysAsync(
                new EncryptionKeyRequestDto(Guid.Empty, $"pk-{Guid.NewGuid():N}", $"prv-{Guid.NewGuid():N}"),
                cancellationToken);
        }

        var connection = await ConnectNextcloud("Autotest Private ThirdParty");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.CreateRoomThirdPartyAsync(
            connection.Id,
            new CreateThirdPartyRoomRequest(title: "Autotest Private ThirdParty", roomType: RoomType.CustomRoom, @private: true, createAsNewFolder: true),
            cancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("third-party");

        var title = $"Autotest Ordinary ThirdParty {Guid.NewGuid():N}";
        var room = await _roomsApi.CreateRoomThirdPartyAsync(
            connection.Id,
            new CreateThirdPartyRoomRequest(title: title, roomType: RoomType.CustomRoom, createAsNewFolder: false),
            cancellationToken);

        room.Response.Id.Should().NotBeNullOrEmpty();
    }
}
