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
/// Reading, replacing and deleting keys take no parameter to target another user: every call acts
/// on the CALLER's own key set, regardless of role. These tests pin that no combination of caller
/// role and target reaches across the boundary.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class CrossUserIsolationTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    [Fact]
    public async Task User_CannotReplaceAnotherUsersKey()
    {
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        await SetFakeKeys(publicKeyPrefix: "user");

        // The user replaces THEIR OWN zero-GUID key; the owner's key (also zero-GUID in its own
        // namespace) must be untouched.
        await _privacyRoomApi.ReplaceKeyAsync(
            new EncryptionKeyRequestDto(publicKey: $"user-new-{Guid.NewGuid():N}", privateKeyEnc: "up2"),
            TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var ownerKeys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        ownerKeys.Select(k => k.PublicKey).Should().BeEquivalentTo([ownerKey.PublicKey]);
    }

    [Fact]
    public async Task User_CannotDeleteAnotherUsersKey()
    {
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        await SetFakeKeys(publicKeyPrefix: "user");

        await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var ownerKeys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        ownerKeys.Select(k => k.PublicKey).Should().BeEquivalentTo([ownerKey.PublicKey]);
    }

    [Fact]
    public async Task DocSpaceAdmin_CannotReplaceAnotherUsersKey()
    {
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        await SetFakeKeys(publicKeyPrefix: "admin");

        // Admin role grants no cross-user key access: this replaces the admin's own key, not the
        // owner's.
        await _privacyRoomApi.ReplaceKeyAsync(
            new EncryptionKeyRequestDto(publicKey: "admin-new", privateKeyEnc: "ap2"),
            TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var ownerKeys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        ownerKeys.Select(k => k.PublicKey).Should().BeEquivalentTo([ownerKey.PublicKey]);
    }

    [Fact]
    public async Task DocSpaceAdmin_CannotDeleteAnotherUsersKey()
    {
        await _filesClient.Authenticate(Owner);
        var ownerKey = await SetFakeKeys(publicKeyPrefix: "owner");

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        await SetFakeKeys(publicKeyPrefix: "admin");

        await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var ownerKeys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        ownerKeys.Select(k => k.PublicKey).Should().BeEquivalentTo([ownerKey.PublicKey]);
    }

    [Fact]
    public async Task User_HoldingOwnKey_NeverSeesAnotherUsersKey()
    {
        // Reading keys is scoped to the caller and takes no parameter for targeting another user,
        // so the guarantee to pin is that the read returns EXACTLY the caller's own key set — with
        // a key of the caller's own present, so an empty or broken read cannot pass for isolation.
        await _filesClient.Authenticate(Owner);
        var ownerId = Guid.NewGuid();
        var ownerKey = await SetFakeKeys(ownerId, "owner");

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var userId = Guid.NewGuid();
        var userKey = await SetFakeKeys(userId, "user");

        var userKeys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        userKeys.Should().ContainSingle();
        userKeys[0].PublicKey.Should().Be(userKey.PublicKey);
        userKeys[0].Id.Should().Be(userId);
        userKeys.Select(k => k.PublicKey).Should().NotContain(ownerKey.PublicKey);

        await _filesClient.Authenticate(Owner);
        var ownerKeys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        ownerKeys.Select(k => k.PublicKey).Should().BeEquivalentTo([ownerKey.PublicKey]);
    }
}
