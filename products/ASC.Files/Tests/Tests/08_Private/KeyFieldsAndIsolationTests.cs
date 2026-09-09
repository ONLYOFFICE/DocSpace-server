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
/// <c>GET /api/2.0/privacyroom/keys</c> — getUserKeys: response shape and per-user isolation.
/// Basic single-key create/read/delete is already covered by
/// <c>PrivacyRoomTest.CRUD_UserPrivateKey</c>.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "PrivacyRoom")]
public class KeyFieldsAndIsolationTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    [Fact]
    public async Task GetUserKeys_Initially_ReturnsEmptySet()
    {
        await _filesClient.Authenticate(Owner);

        var keys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;

        keys.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserKeys_EveryFieldOfTheKeyDto_IsFilledInCorrectly()
    {
        // Pins the metadata fields beyond a bare "is defined": userId must be the CALLER, date must
        // be a real, recent timestamp, and every key must report the same crypto engine (the engine
        // is portal-wide, so it is identical across a user's keys).
        await _filesClient.Authenticate(Owner);
        var before = DateTime.UtcNow;

        await SetFakeKeys(Guid.NewGuid());
        await SetFakeKeys(Guid.NewGuid());

        var keys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;

        keys.Should().HaveCount(2);

        foreach (var key in keys)
        {
            key.UserId.Should().Be(Owner.Id);
            key.Date.Should().BeAfter(before.AddMinutes(-1));
            key.CryptoEngineId.Should().MatchRegex(@"^\{[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}\}$");
        }

        keys.Select(k => k.CryptoEngineId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task GetUserKeys_ReturnsEveryKeyTheUserHolds()
    {
        await _filesClient.Authenticate(Owner);

        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        foreach (var id in ids)
        {
            await SetFakeKeys(id);
        }

        var keys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;

        keys.Should().HaveCount(3);
        keys.Select(k => k.Id).Should().BeEquivalentTo(ids);
    }

    [Fact]
    public async Task GetUserKeys_AreIsolatedPerUser()
    {
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");

        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(member);
        await SetFakeKeys(publicKeyPrefix: "user");

        // The member deleting their own key must not touch the owner's key.
        await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken);
        var memberAfter = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        memberAfter.Should().BeNullOrEmpty();

        await _filesClient.Authenticate(Owner);
        var ownerAfter = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        ownerAfter.Should().ContainSingle();
        ownerAfter[0].PublicKey.Should().Be(ownerKey.PublicKey);
    }
}
