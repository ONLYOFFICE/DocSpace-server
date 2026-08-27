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

namespace ASC.Files.Tests.Tests._03_Rooms.Chat;

/// <summary>
/// <c>chatSettings</c> on PUT /files/rooms/{id}. The field belongs to an AI room only — it is part
/// of the room DTO for <see cref="RoomType.AiRoom"/> and absent on every other room type — so
/// sending it for a plain room is a validation error rather than a silent no-op.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomChatSettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <remarks>
    /// Bug 82798: updating a non-AI room with <c>chatSettings</c> answers 200 and drops the field on
    /// the floor, so the caller believes it configured a chat that does not exist. The no-op is
    /// asserted first, so only the status code drives the failure.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82798")]
    public async Task UpdateRoom_ChatSettingsOnCustomRoom_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Chat Settings");

        // Act
        var exception = await Record.ExceptionAsync(
            async () => await _roomsApi.UpdateRoomAsync(
                room.Id,
                new UpdateRoomRequest(chatSettings: new ChatSettings(providerId: -1, modelId: "gpt-5.5", prompt: "Hi")),
                TestContext.Current.CancellationToken));

        // Assert — nothing was stored: a fresh read carries no chat settings (an AI room would)
        var after = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        after.ChatSettings.Should().BeNull();

        exception.Should().BeOfType<ApiException>()
            .Which.ErrorCode.Should().Be(400, "chatSettings is not a field of a non-AI room");
    }

    [Fact]
    public async Task UpdateRoom_ChatSettingsOnAiRoom_Applied()
    {
        // Positive control for the test above: the same field IS honoured on an AI room, so the
        // refusal on a CustomRoom is about the room type and not about the field being unsupported
        // everywhere.
        await _filesClient.Authenticate(Owner);
        var room = await CreateAiRoom("Autotest AI Chat Settings");
        room.ChatSettings.Should().NotBeNull("an AI room carries chat settings from the moment it is created");

        var updated = (await _roomsApi.UpdateRoomAsync(
            room.Id,
            new UpdateRoomRequest(chatSettings: new ChatSettings(prompt: "Autotest prompt")),
            TestContext.Current.CancellationToken)).Response;

        updated.ChatSettings.Prompt.Should().Be("Autotest prompt");

        var after = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        after.ChatSettings.Prompt.Should().Be("Autotest prompt");
    }
}
