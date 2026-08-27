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

namespace ASC.Files.Tests.Tests._03_Rooms.Templates;

/// <summary>
/// Title validation of POST /files/roomtemplate. The template title is validated exactly like the
/// room title on POST /files/rooms: it is required and bounded, and a rejected request must leave
/// no template behind.
/// </summary>
/// <remarks>
/// Bug 81690: these three requests used to answer 200 and queue a background operation that then
/// hung, so the template id never became positive and the caller was left polling a status that
/// never completed. Fixed — the title is validated synchronously before the operation is queued.
/// </remarks>
[Trait("Category", "Rooms")]
[Trait("Bug", "81690")]
public class RoomTemplateCreateValidationTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task CreateRoomTemplate_MissingTitle_BadRequest()
    {
        // RoomTemplateDto rejects a null title in its constructor, so the body without a title can
        // only be sent raw — the typed client would never put this request on the wire.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest NoTitle Source");

        var response = await SendRawRoomTemplateCreate($$"""{"roomId":{{room.Id}}}""");

        ((int)response.StatusCode).Should().Be(400);

        var titles = await GetTemplateTitles();
        titles.Should().NotContain("Autotest NoTitle Source");
    }

    [Fact]
    public async Task CreateRoomTemplate_EmptyTitle_BadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest EmptyTitle Source");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(
                new RoomTemplateDto(room.Id, string.Empty),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var titles = await GetTemplateTitles();
        titles.Should().NotContain(string.Empty);
    }

    [Fact]
    public async Task CreateRoomTemplate_TooLongTitle_BadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest LongTitle Source");
        var longTitle = new string('A', 1000);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomTemplateAsync(
                new RoomTemplateDto(room.Id, longTitle),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var titles = await GetTemplateTitles();
        titles.Should().NotContain(longTitle);
    }

    /// <summary>
    /// Sends a raw POST /api/2.0/files/roomtemplate, for a body the generated DTO cannot express.
    /// </summary>
    private async Task<HttpResponseMessage> SendRawRoomTemplateCreate(string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/2.0/files/roomtemplate")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
