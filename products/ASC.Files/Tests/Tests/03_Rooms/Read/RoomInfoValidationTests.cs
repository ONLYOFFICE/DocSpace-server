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

namespace ASC.Files.Tests.Tests._03_Rooms.Read;

/// <summary>
/// GET /files/rooms/:id - how the endpoint reacts to an id that cannot resolve to a room.
/// </summary>
/// <remarks>
/// The TS suite also covers a null and a missing <c>id</c>, both rejected by the TypeScript SDK
/// client-side before any request is sent. There is no equivalent in C#: <c>GetRoomInfoAsync</c>
/// takes a non-nullable <c>int id</c>, so the compiler already makes both values impossible to
/// pass — there is nothing left to assert.
/// </remarks>
[Trait("Category", "Rooms")]
public class RoomInfoValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(999999999)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetRoomInfo_NonExistingOrOutOfRangeId_Returns404(int id)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetRoomInfo_NonNumericId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act - the typed SDK signature takes an int, so a non-numeric id can only be sent raw
        using var response = await _filesClient.GetAsync("api/2.0/files/rooms/abc", TestContext.Current.CancellationToken);

        // Assert
        ((int)response.StatusCode).Should().Be(404);
    }
}
