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

namespace ASC.Web.Api.Tests.Tests._05_Security.AuditTrail;

/// <summary>
/// The <c>context</c> of an audit event about a room: <c>Rooms: {title}</c>, and for <c>RoomRenamed</c> the title
/// the room had before the rename.
/// </summary>
[Trait("Category", "Security")]
public class AuditEventContextTests(
    AspireAppFixture fixture)
    : AuditTrailTestBase(fixture)
{
    protected override bool NeedsLazyResources => true;

    [Fact]
    public async Task RoomCreated_ContextIsRoomsAndTheTitle()
    {
        // Arrange
        var title = $"context-room-{Guid.NewGuid():N}";
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);

        // Act
        await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(title: title, roomType: RoomType.CustomRoom), TestContext.Current.CancellationToken);

        // Assert
        var events = await PollAuditEventsByFilterAsync(all => all.Exists(e => e.ActionId == MessageAction.RoomCreated));
        events.Find(e => e.ActionId == MessageAction.RoomCreated)!.Context.Should().Be($"Rooms: {title}");
    }

    [Fact]
    public async Task RoomRenamed_ContextNamesTheOldTitle()
    {
        // Arrange
        var oldTitle = $"context-old-{Guid.NewGuid():N}";
        var newTitle = $"context-new-{Guid.NewGuid():N}";
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(title: oldTitle, roomType: RoomType.CustomRoom), TestContext.Current.CancellationToken)).Response;

        // Act
        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(title: newTitle), TestContext.Current.CancellationToken);

        // Assert
        var events = await PollAuditEventsByFilterAsync(all => all.Exists(e => e.ActionId == MessageAction.RoomRenamed));
        events.Find(e => e.ActionId == MessageAction.RoomRenamed)!.Context.Should().Be($"Rooms: {oldTitle}");
    }
}
