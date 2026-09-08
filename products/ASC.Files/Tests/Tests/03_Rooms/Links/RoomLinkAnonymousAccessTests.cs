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

namespace ASC.Files.Tests.Tests._03_Rooms.Links;

/// <summary>
/// <c>GET /files/share</c> (<c>GetExternalShareData</c>) resolving a room's own primary external
/// link for a fully anonymous visitor - no <c>Authorization</c> header at all, as opposed to an
/// authenticated non-member. Room-links access control for the SDK-typed link endpoints themselves
/// lives in <see cref="RoomLinkPrimaryTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLinkAnonymousAccessTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    public static TheoryData<string, RoomType> PrivateUnsupportedRoomTypes => new()
    {
        { "Public", RoomType.PublicRoom },
        { "FormFilling", RoomType.FillingFormsRoom },
    };

    /// <remarks>
    /// Bug 83319 (bug 83166 was closed and refiled as this one): an anonymous visitor - no
    /// <c>Authorization</c> header at all - resolving a room's own primary external link through
    /// <c>GetExternalShareData</c> gets <c>shared=false</c>, even though the same response correctly
    /// resolves the link to the room (200, <c>isRoom=true</c>, the right <c>entityId</c>) and manual
    /// verification confirms the link itself opens fine for an anonymous visitor. <c>shared</c>
    /// should be <c>true</c>.
    /// </remarks>
    [Theory]
    [MemberData(nameof(PrivateUnsupportedRoomTypes))]
    [Trait("Bug", "83319")]
    [Trait("Bug", "83166")]
    public async Task GetExternalShareData_AnonymousVisitor_PrimaryLinkOfRoom_ShouldReportShared(string label, RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoom($"Autotest Anonymous External Link {label}", roomType);
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var requestToken = link.SharedLink.RequestToken;

        // Act - no Authorization header at all, as a visitor with no portal account gets when they
        // follow the room's public share link.
        await _filesClient.Authenticate(null);
        var data = (await _sharingApi.GetExternalShareDataAsync(
            requestToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert - the link does resolve to the right room even for a fully anonymous caller; this
        // half stays true regardless of the bug below.
        data.IsRoom.Should().BeTrue();
        data.EntityId.Should().Be(room.Id.ToString());

        // Assert - the product is supposed to report shared=true here; the current (buggy)
        // behaviour reports false, which is what makes this test red while the bug is open.
        data.Shared.Should().BeTrue();
    }

    private async Task<FolderDtoInteger> CreateRoom(string title, RoomType roomType)
    {
        return roomType switch
        {
            RoomType.PublicRoom => await CreatePublicRoom(title),
            RoomType.FillingFormsRoom => await CreateFillingFormsRoom(title),
            _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, "Unsupported room type for this suite.")
        };
    }
}
