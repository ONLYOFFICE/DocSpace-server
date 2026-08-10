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
/// Access control for the four per-caller key-management endpoints. Encryption keys are personal:
/// every authenticated role manages only their own keys, and there is no parameter to target
/// another user's keys (cross-user isolation is covered separately in
/// <see cref="CrossUserIsolationTests"/>). Anonymous requests get 401. Guests currently get 403 on
/// set/replace/delete, which is BUG 82524 — they should be able to manage their own keys.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class KeyAccessPermissionsTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    #region GET /api/2.0/privacyroom/keys - access control

    [Fact]
    public async Task GetUserKeys_Owner_ReadsBackOwnKey()
    {
        await _filesClient.Authenticate(Owner);
        var key = await SetFakeKeys();

        var keys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;

        keys.Select(k => k.PublicKey).Should().BeEquivalentTo([key.PublicKey]);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task GetUserKeys_Member_ReadsBackOwnKey(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);
        var key = await SetFakeKeys();

        var keys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;

        keys.Select(k => k.PublicKey).Should().BeEquivalentTo([key.PublicKey]);
    }

    [Fact]
    public async Task GetUserKeys_Guest_ReadsOwnEmptyKeySet()
    {
        // A guest cannot create a key (BUG 82524), so reading returns their OWN empty set (200,
        // no keys) — never another user's keys.
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var keys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;

        keys.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserKeys_Anonymous_Unauthorized()
    {
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    #endregion

    #region POST /api/2.0/privacyroom/keys - access control

    [Fact]
    [Trait("Bug", "82546")]
    public async Task SetKeys_Owner_CanSetKeys()
    {
        await SetKeysAndAssertCreateStatus(Owner);
    }

    [Theory]
    [Trait("Bug", "82546")]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task SetKeys_Member_CanSetKeys(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await SetKeysAndAssertCreateStatus(member);
    }

    private async Task SetKeysAndAssertCreateStatus(User user)
    {
        // BUG 82546: creating a resource should answer 201 Created; the API answers 200. The
        // authorization side-effect (the role really could create its key) is checked first.
        await _filesClient.Authenticate(user);

        var response = await _privacyRoomApi.SetKeysWithHttpInfoAsync(
            new EncryptionKeyRequestDto(publicKey: $"pk-{Guid.NewGuid():N}", privateKeyEnc: "prv"),
            TestContext.Current.CancellationToken);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().ContainSingle();

        ((int)response.StatusCode).Should().Be(201);
    }

    [Fact]
    [Trait("Bug", "82524")]
    public async Task SetKeys_Guest_CanSetKeys()
    {
        // BUG 82524: guests should be able to manage their own encryption keys, but the API
        // currently denies them.
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var status = await StatusOf(() => _privacyRoomApi.SetKeysWithHttpInfoAsync(
            new EncryptionKeyRequestDto(publicKey: $"pk-{Guid.NewGuid():N}", privateKeyEnc: "prv"),
            TestContext.Current.CancellationToken));

        status.Should().Be(200);
    }

    [Fact]
    public async Task SetKeys_Anonymous_Unauthorized()
    {
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.SetKeysAsync(
                new EncryptionKeyRequestDto(publicKey: "pk", privateKeyEnc: "prv"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    #endregion

    #region PUT /api/2.0/privacyroom/keys - access control

    [Fact]
    public async Task ReplaceKey_Owner_CanReplaceOwnKey()
    {
        await ReplaceKeyAndAssertOk(Owner);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task ReplaceKey_Member_CanReplaceOwnKey(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await ReplaceKeyAndAssertOk(member);
    }

    private async Task ReplaceKeyAndAssertOk(User user)
    {
        await _filesClient.Authenticate(user);
        await SetFakeKeys();

        var response = await _privacyRoomApi.ReplaceKeyWithHttpInfoAsync(
            new EncryptionKeyRequestDto(publicKey: $"pk-{Guid.NewGuid():N}", privateKeyEnc: "prv"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Bug", "82524")]
    public async Task ReplaceKey_Guest_ReplaceOwnKeyFlowIsBlockedByGuestSetKeysBug()
    {
        // Replacing requires an existing key, but a guest cannot create one because setKeys is
        // denied today (BUG 82524), so the replace-own-key flow cannot be exercised.
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var status = await StatusOf(() => _privacyRoomApi.SetKeysWithHttpInfoAsync(
            new EncryptionKeyRequestDto(publicKey: $"pk-{Guid.NewGuid():N}", privateKeyEnc: "prv"),
            TestContext.Current.CancellationToken));

        status.Should().Be(200);
    }

    [Fact]
    public async Task ReplaceKey_Anonymous_Unauthorized()
    {
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.ReplaceKeyAsync(
                new EncryptionKeyRequestDto(publicKey: "pk", privateKeyEnc: "prv"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    #endregion

    #region DELETE /api/2.0/privacyroom/keys/{id} - access control

    [Fact]
    [Trait("Bug", "82551")]
    public async Task DeleteKeys_Owner_CanDeleteOwnKey()
    {
        await DeleteKeyAndAssertNoContent(Owner);
    }

    [Theory]
    [Trait("Bug", "82551")]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task DeleteKeys_Member_CanDeleteOwnKey(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await DeleteKeyAndAssertNoContent(member);
    }

    private async Task DeleteKeyAndAssertNoContent(User user)
    {
        // BUG 82551: deleting an existing resource should answer 204 No Content; the API answers
        // 200. The authorization side-effect (the key is actually gone) is checked first.
        await _filesClient.Authenticate(user);
        await SetFakeKeys();

        var status = await StatusOf(() => _privacyRoomApi.DeleteKeysWithHttpInfoAsync(Guid.Empty, TestContext.Current.CancellationToken));

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();

        status.Should().Be(204);
    }

    [Fact]
    [Trait("Bug", "82524")]
    public async Task DeleteKeys_Guest_DeleteOwnKeyFlowIsBlockedByGuestSetKeysBug()
    {
        // A guest cannot create a key because setKeys is denied today (BUG 82524), so there is
        // never a real key to delete.
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var status = await StatusOf(() => _privacyRoomApi.SetKeysWithHttpInfoAsync(
            new EncryptionKeyRequestDto(publicKey: $"pk-{Guid.NewGuid():N}", privateKeyEnc: "prv"),
            TestContext.Current.CancellationToken));

        status.Should().Be(200);
    }

    [Fact]
    public async Task DeleteKeys_Anonymous_Unauthorized()
    {
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    #endregion
}
