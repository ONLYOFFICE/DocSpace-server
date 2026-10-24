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

namespace ASC.Files.Tests.Tests._05_Features.ExternalDbSync;

/// <summary>
/// GET /files/rooms/{id}/externaldbsync — status of the external database sync job of a form
/// filling room.
/// </summary>
[Trait("Category", "Rooms")]
public class ExternalDbSyncStatusTests(
    AspireAppFixture fixture)
    : ExternalDbSyncTestBase(fixture)
{
    [Fact]
    public async Task GetStatus_FormRoomThatNeverRanASync_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest DbSync No Job");

        // Act
        var status = await _roomsApi.GetExternalDbSyncStatusAsync(room.Id, TestContext.Current.CancellationToken);

        // Assert
        status?.Response.Should().BeNull();
    }

    [Fact]
    public async Task GetStatus_RepeatedCalls_ArePureReadsAndNoJobAppears()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest DbSync Repeat GET");

        // Act + Assert
        for (var i = 0; i < 3; i++)
        {
            await AssertNoJob(room.Id);
        }
    }

    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    [InlineData(RoomType.EditingRoom)]
    public async Task GetStatus_NotAFormFillingRoom_Forbidden(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoom(new CreateRoomRequestDto($"Autotest DbSync GET {roomType}", roomType: roomType));

        // Act
        var exception = await ExpectGetRefused(room.Id);

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetStatus_UnknownRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await ExpectGetRefused(999999999);

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetStatus_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest DbSync GET Anon");
        await _filesClient.Authenticate(null);

        // Act
        var exception = await ExpectGetRefused(room.Id);

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
