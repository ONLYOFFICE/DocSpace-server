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
/// POST /files/rooms/{id}/externaldbsync — starting the external database sync of a form filling
/// room. The test portals have no external database, so a start on a valid room is always refused.
/// </summary>
[Trait("Category", "Rooms")]
public class ExternalDbSyncStartTests(
    AspireAppFixture fixture)
    : ExternalDbSyncTestBase(fixture)
{
    /// <remarks>
    /// Bug 84473: with no external database configured, <c>FileStorageService.StartExternalDbSyncAsync</c>
    /// throws <c>BadHttpRequestException</c> ("Bad request.") and the endpoint answers 500. The
    /// exception type is a client error, so the response should be a controlled 400. The endpoint's
    /// own SwaggerResponse documents 500 for this case; the test asserts the corrected behaviour.
    /// </remarks>
    [Fact]
    [Trait("Bug", "84473")]
    public async Task StartSync_NoExternalDatabaseConfigured_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest DbSync Start NoDb");

        // Act
        var exception = await ExpectStartRefused(room.Id);

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task StartSync_FailedStart_QueuesNothingAndSecondStartFailsTheSameWay()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest DbSync Start Nothing");

        // Act
        var first = await ExpectStartRefused(room.Id);
        await AssertNoJob(room.Id);
        var second = await ExpectStartRefused(room.Id);

        // Assert: refused (not accepted), identically, and no job was left behind.
        first.ErrorCode.Should().BeGreaterThanOrEqualTo(400);
        second.ErrorCode.Should().Be(first.ErrorCode);
        await AssertNoJob(room.Id);
    }

    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    [InlineData(RoomType.EditingRoom)]
    public async Task StartSync_NotAFormFillingRoom_Forbidden(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoom(new CreateRoomRequestDto($"Autotest DbSync Start {roomType}", roomType: roomType));

        // Act
        var exception = await ExpectStartRefused(room.Id);

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task StartSync_UnknownRoom_NotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await ExpectStartRefused(999999999);

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task StartSync_Anonymous_UnauthorizedAndQueuesNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest DbSync Start Anon");
        await _filesClient.Authenticate(null);

        // Act
        var exception = await ExpectStartRefused(room.Id);

        // Assert
        exception.ErrorCode.Should().Be(401);

        await _filesClient.Authenticate(Owner);
        await AssertNoJob(room.Id);
    }
}
