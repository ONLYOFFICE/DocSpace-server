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

namespace ASC.Files.Tests.Tests._04_Security.Sharing;

/// <summary>
/// Access control for <c>GET /api/2.0/files/folder/{id}/share</c> (<c>GetFolderSecurityInfo</c>):
/// whether a Guest may look up a room's sharing rights.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class GetFolderSecurityInfoPermissionsTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    /// <summary>
    /// BUG 79219: a Guest is refused unconditionally, before any access check. That is deliberate -
    /// the share list enumerates every member of the room with name, e-mail and access level, and a
    /// Guest is not meant to see who else is in a room. Room access is not the same permission as
    /// membership visibility, so being invited does not open this endpoint.
    /// </summary>
    [Fact]
    [Trait("Bug", "79219")]
    public async Task GetFolderSecurityInfo_GuestWithRoomAccess_Returns403()
    {
        var room = await CreateCollaborationRoom("Autotest Folder Security Info Perm Guest Access");
        var guest = await InviteGuest();
        await InviteToRoom(room.Id, guest, FileShare.Editing);

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFolderSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
    }

    /// <summary>
    /// The other half of the same rule: a Guest with no room access is refused too, so neither the
    /// role guard nor an access check would let this one through.
    /// </summary>
    [Fact]
    [Trait("Bug", "79219")]
    public async Task GetFolderSecurityInfo_GuestWithoutRoomAccess_Returns403()
    {
        var room = await CreateCollaborationRoom("Autotest Folder Security Info Perm Guest No Access");
        var guest = await InviteGuest();

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFolderSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
    }
}
