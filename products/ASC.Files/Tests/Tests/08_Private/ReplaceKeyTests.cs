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
/// <c>PUT /api/2.0/privacyroom/keys</c> — replaceKey: rotating an existing encryption key.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "PrivacyRoom")]
public class ReplaceKeyTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    [Fact]
    public async Task ReplaceKey_UpdatesStoredKeyAndDropsTheOldValue()
    {
        await _filesClient.Authenticate(Owner);

        await SetFakeKeys(publicKeyPrefix: "old");
        var newPublicKey = $"new-{Guid.NewGuid():N}";

        var replaced = (await _privacyRoomApi.ReplaceKeyAsync(
            new EncryptionKeyRequestDto(publicKey: newPublicKey, privateKeyEnc: "new-prv"),
            TestContext.Current.CancellationToken)).Response;

        replaced.Should().ContainSingle();
        replaced[0].PublicKey.Should().Be(newPublicKey);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().ContainSingle();
        after[0].PublicKey.Should().Be(newPublicKey);
        after[0].PrivateKeyEnc.Should().Be("new-prv");
    }

    [Fact]
    public async Task ReplaceKey_UpdatesOnlyTheTargetedKey()
    {
        await _filesClient.Authenticate(Owner);

        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var keyA = await SetFakeKeys(idA, "pkA");
        await SetFakeKeys(idB, "pkB");

        var pkBNew = $"pkB-new-{Guid.NewGuid():N}";
        await _privacyRoomApi.ReplaceKeyAsync(
            new EncryptionKeyRequestDto(idB, pkBNew, "b2"),
            TestContext.Current.CancellationToken);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().HaveCount(2);
        after.Single(k => k.Id == idA).PublicKey.Should().Be(keyA.PublicKey);
        after.Single(k => k.Id == idB).PublicKey.Should().Be(pkBNew);
    }

    [Fact]
    public async Task ReplaceKey_RepeatedOnSameId_UpdatesInPlaceWithoutAccumulating()
    {
        await _filesClient.Authenticate(Owner);

        // No id supplied -> every call targets the zero-GUID key.
        await SetFakeKeys(publicKeyPrefix: "v1");
        await _privacyRoomApi.ReplaceKeyAsync(new EncryptionKeyRequestDto(publicKey: "v2", privateKeyEnc: "p2"), TestContext.Current.CancellationToken);
        await _privacyRoomApi.ReplaceKeyAsync(new EncryptionKeyRequestDto(publicKey: "v3", privateKeyEnc: "p3"), TestContext.Current.CancellationToken);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().ContainSingle();
        after[0].Id.Should().Be(Guid.Empty);
        after[0].PublicKey.Should().Be("v3");
    }

    [Fact]
    public async Task ReplaceKey_MalformedId_Rejected400()
    {
        // A malformed id is validated on replaceKey (400), consistent with setKeys.
        await _filesClient.Authenticate(Owner);

        using var response = await SendRawPrivacyRoomRequest(
            HttpMethod.Put,
            "api/2.0/privacyroom/keys",
            """{"id":"not-a-guid","publicKey":"pk","privateKeyEnc":"prv"}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Bug", "82545")]
    public async Task ReplaceKey_NoExistingKey_Rejected()
    {
        // BUG 82545: replacing a key that does not exist must return a controlled error (404)
        // rather than silently succeeding with a no-op.
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.ReplaceKeyAsync(
                new EncryptionKeyRequestDto(publicKey: "pk", privateKeyEnc: "prv"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();
    }

    public static TheoryData<string, string, string> DestructivePutInputs => new()
    {
        { "Empty publicKey", "", "prv-new" },
        { "Empty privateKeyEnc", "pk-new", "" },
        { "Whitespace-only publicKey", "   ", "prv-new" },
        { "Whitespace-only privateKeyEnc", "pk-new", "   " },
        { "Missing publicKey", null!, "prv-new" },
        { "Missing privateKeyEnc", "pk-new", null! },
        { "Both fields missing/null", null!, null! }
    };

    [Theory]
    [Trait("Bug", "82802")]
    [MemberData(nameof(DestructivePutInputs))]
    public async Task ReplaceKey_DestructiveInput_MustNotOverwriteStoredKey(string label, string publicKey, string privateKeyEnc)
    {
        // BUG 82802: replaceKey validates nothing and behaves as a full, unmerged overwrite —
        // whatever the request omits or blanks out is written over the stored key. `label`
        // documents which case is under test in the xUnit output.
        _ = label;
        await _filesClient.Authenticate(Owner);

        var original = await SetFakeKeys(publicKeyPrefix: "orig");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.ReplaceKeyAsync(
                new EncryptionKeyRequestDto(publicKey: publicKey, privateKeyEnc: privateKeyEnc),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().ContainSingle();
        after[0].PublicKey.Should().Be(original.PublicKey);
        after[0].PrivateKeyEnc.Should().Be(original.PrivateKeyEnc);
    }

    [Fact]
    [Trait("Bug", "82802")]
    public async Task ReplaceKey_NoRequestBody_MustNotWipeStoredKey()
    {
        // BUG 82802: a body-less request binds to a default DTO, which finds and erases the
        // zero-GUID key's fields. A request that supplies no data must not be able to destroy data.
        await _filesClient.Authenticate(Owner);

        var original = await SetFakeKeys(publicKeyPrefix: "orig");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.ReplaceKeyAsync(
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().ContainSingle();
        after[0].PublicKey.Should().Be(original.PublicKey);
        after[0].PrivateKeyEnc.Should().Be(original.PrivateKeyEnc);
    }

    [Fact]
    [Trait("Bug", "82802")]
    public async Task ReplaceKey_OnlyPublicKeySupplied_MustNotEraseStoredPrivateKey()
    {
        // BUG 82802: rotating only the public half used to lose the private half. A partial body is
        // now rejected outright — replaceKey writes both halves or neither — so the stored key
        // is left exactly as it was.
        await _filesClient.Authenticate(Owner);

        var original = await SetFakeKeys(publicKeyPrefix: "orig");
        var newPublicKey = $"pk-rotated-{Guid.NewGuid():N}";

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.ReplaceKeyAsync(
                new EncryptionKeyRequestDto(publicKey: newPublicKey),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().ContainSingle();
        after[0].PublicKey.Should().Be(original.PublicKey);
        after[0].PrivateKeyEnc.Should().Be(original.PrivateKeyEnc);
    }

    [Fact]
    [Trait("Bug", "82800")]
    public async Task ReplaceKey_OversizedPublicKey_MustNotDestroyTheCallersEntireKeySet()
    {
        // BUG 82800: the worst of the replaceKey cases — a publicKey too large to persist takes
        // out EVERY key the caller holds, including keys the request never named.
        await _filesClient.Authenticate(Owner);

        var zeroKey = await SetFakeKeys(publicKeyPrefix: "zero");
        var otherId = Guid.NewGuid();
        var otherKey = await SetFakeKeys(otherId, "other");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.ReplaceKeyAsync(
                new EncryptionKeyRequestDto(Guid.Empty, new string('x', 65536), "bp"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().HaveCount(2);
        after.Single(k => k.Id == otherId).PublicKey.Should().Be(otherKey.PublicKey);
        after.Single(k => k.Id == Guid.Empty).PublicKey.Should().Be(zeroKey.PublicKey);
    }
}
