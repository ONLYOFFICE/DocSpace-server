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

namespace ASC.Files.Tests.Tests._03_Rooms.Pin;

/// <summary>
/// <c>PUT /files/rooms/{id}/pin</c> can pin every non-AI room type. The AI room type has its own
/// coverage in <see cref="RoomPinLimitTests"/>, because it is exempt from (or, per BUG 81852,
/// meant to be exempt from) the regular 10-room pin limit.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomPinRoomTypesTests(
    AspireAppFixture fixture)
    : RoomPinTestsBase(fixture)
{
    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.FillingFormsRoom)]
    [InlineData(RoomType.EditingRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task PinRoom_OfEachType_Pins(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var searchArea = SearchAreaFor(roomType);

        var room = await CreateRoomOfType(roomType, $"Autotest Pin {roomType}");
        // An extra unpinned room so the "floats to the top" check is meaningful.
        await CreateRoomOfType(roomType, $"Autotest Pin {roomType} Other");

        // Act
        var response = await _roomsApi.PinRoomWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Data.Response.Pinned.Should().BeTrue();

        // Effect: the room is actually pinned in the list and sits above unpinned rooms.
        await ExpectPinnedOnTop(room.Id, searchArea);
    }
}
