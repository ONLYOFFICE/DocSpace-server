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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// <c>GET /api/2.0/files/rooms</c> — whether a private (E2E-encrypted) room is listed for a caller
/// who is not one of its members.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomVisibilityTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    /// <remarks>
    /// Bug 82956: a private room is end-to-end encrypted, so its key set — and therefore its
    /// content — is only supposed to be reachable by its members (see
    /// <see cref="RoomAccessKeysPermissionsTests.GetUserKeysForRoom_DocSpaceAdminNotAMember_CannotReadTheRoomsE2EKeys"/>).
    /// The room listing does not honour that: a DocSpaceAdmin who was never invited still sees the
    /// private room in <c>GET /files/rooms</c>, the same way they would see any other admin-visible
    /// room. This is still open, so the assertion below is the correct (not yet true) behaviour and
    /// is expected to fail until the bug is fixed.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82956")]
    public async Task GetRoomsFolder_DocSpaceAdminNotAMember_MustNotSeePrivateRoom()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        var roomTitle = "Autotest Privacy Room " + Guid.NewGuid().ToString("N")[..8];
        var room = await CreatePrivateRoom(roomTitle, RoomType.CustomRoom);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var result = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Folders.Select(f => f.Title).Should().NotContain(room.Title);
    }

    /// <remarks>
    /// Bug 82956: same visibility leak as
    /// <see cref="GetRoomsFolder_DocSpaceAdminNotAMember_MustNotSeePrivateRoom"/>, but for the
    /// portal owner instead of an admin — being the owner must not grant visibility into an E2E
    /// room one was never invited to. Still open, so this asserts the correct behaviour and is
    /// expected to fail until fixed.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82956")]
    public async Task GetRoomsFolder_OwnerNotAMember_MustNotSeePrivateRoom()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        await SetFakeKeys(publicKeyPrefix: "roomadmin");
        var roomTitle = "Autotest Privacy Room " + Guid.NewGuid().ToString("N")[..8];
        var room = await CreatePrivateRoom(roomTitle, RoomType.CustomRoom);

        await _filesClient.Authenticate(Owner);

        var result = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Folders.Select(f => f.Title).Should().NotContain(room.Title);
    }
}
