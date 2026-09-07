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
/// Creating a room with <c>private: true</c>. The flag turns the room into an encrypted one, which
/// has two prerequisites: the caller must already hold an encryption key pair, and the room type
/// must not be one that auto-creates an external link — a link and end-to-end encryption cannot
/// coexist. Everything the flag does afterwards (key exchange, membership, file access) is covered
/// by the other suites in this folder.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomCreateTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    /// <summary>The room types that accept <c>private: true</c>.</summary>
    public static TheoryData<RoomType> PrivateSupportedRoomTypes =>
    [
        RoomType.CustomRoom,
        RoomType.EditingRoom,
        RoomType.VirtualDataRoom
    ];

    /// <summary>
    /// The room types that refuse <c>private: true</c>: both auto-create an external link on
    /// creation, which is incompatible with the encrypted flag.
    /// </summary>
    public static TheoryData<RoomType> PrivateUnsupportedRoomTypes =>
    [
        RoomType.PublicRoom,
        RoomType.FillingFormsRoom
    ];

    [Theory]
    [MemberData(nameof(PrivateSupportedRoomTypes))]
    public async Task CreateRoom_PrivateSupportedType_Created(RoomType roomType)
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreatePrivateRoom($"Autotest Private {roomType}", roomType);

        room.Private.Should().BeTrue();
        room.RoomType.Should().Be(roomType);
    }

    [Fact]
    public async Task CreateRoom_Private_FlagPersistsInRoomInfo()
    {
        // The flag is not just echoed back by the create response — a fresh read reports it too.
        await _filesClient.Authenticate(Owner);
        var room = await CreatePrivateRoom("Autotest Private Persist", RoomType.CustomRoom);

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        info.Private.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(PrivateUnsupportedRoomTypes))]
    public async Task CreateRoom_PrivateUnsupportedType_Forbidden(RoomType roomType)
    {
        // The keys exist, so the refusal is about the room type and nothing else.
        await _filesClient.Authenticate(Owner);
        await EnsureEncryptionKeys();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto($"Autotest Private {roomType}", roomType: roomType, @private: true),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateRoom_PrivateWithoutEncryptionKeys_Forbidden()
    {
        // A brand-new portal owner holds no keys yet, and a private room cannot be created without
        // them — the error names the missing key, so this is not a generic 403.
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto("Autotest Private No Keys", roomType: RoomType.CustomRoom, @private: true),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("encryption key");
    }
}
