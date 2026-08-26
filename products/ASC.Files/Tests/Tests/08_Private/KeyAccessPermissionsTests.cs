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
/// <see cref="CrossUserIsolationTests"/>). Anonymous requests get 401.
///
/// A Guest is read-only on the key surface, by design: reading answers 200 with an always-empty
/// set, while set/replace are refused with 403. This was once reported as BUG 82524 and is not a
/// bug — a Guest is not meant to own encryption key material. As a knock-on effect a Guest can
/// never be a member of a private room, since membership requires the invitee to hold a key.
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
        // Reading is open to a guest (200); creating is not (403, see SetKeys_Guest_Denied), so a
        // guest's own set is always empty — and it is never another user's keys. The owner's
        // populated read is the positive control: it proves the endpoint does report keys in this
        // portal, so the guest's empty read is scoping and not a broken read.
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys();
        var ownerKeys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        ownerKeys.Should().ContainSingle();

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
    public async Task SetKeys_Guest_Denied()
    {
        // Guests hold no encryption keys by design, which is also what makes them uninvitable to a
        // private room — see RoomAccessKeysPermissionsTests.Invite_GuestToPrivateRoom_AlwaysRefused.
        // Nothing is stored: the refused call must not leave a key behind, so the key set is read
        // back afterwards — a 403 that still wrote a key could not pass.
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.SetKeysAsync(
                new EncryptionKeyRequestDto(publicKey: $"pk-{Guid.NewGuid():N}", privateKeyEnc: "prv"),
                TestContext.Current.CancellationToken));

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent.ToString().Should().Contain("Access denied");
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
    public async Task ReplaceKey_Guest_Denied()
    {
        // ReplaceKey shares the create path's access check, so a guest is refused here for the same
        // by-design reason as on POST: no key material for guests. A guest also never has a key to
        // replace in the first place — the empty read afterwards pins both halves of that, and the
        // missing key that would otherwise make this a 404 never comes into play.
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.ReplaceKeyAsync(
                new EncryptionKeyRequestDto(publicKey: $"pk-{Guid.NewGuid():N}", privateKeyEnc: "prv"),
                TestContext.Current.CancellationToken));

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent.ToString().Should().Contain("Access denied");
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

        var response = await _privacyRoomApi.DeleteKeysWithHttpInfoAsync(Guid.Empty, TestContext.Current.CancellationToken);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteKeys_Guest_Denied()
    {
        // Refused for being a guest, not for holding no key: the answer is 403 and not the 404 an
        // ordinary member would get for the same call.
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
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
