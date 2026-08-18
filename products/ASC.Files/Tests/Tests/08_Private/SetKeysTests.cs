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
/// <c>POST /api/2.0/privacyroom/keys</c> — setKeys: creating and validating encryption keys.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "PrivacyRoom")]
public class SetKeysTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    [Fact]
    public async Task SetKeys_NewId_CreatesAdditionalKeyWithoutOverwritingTheFirst()
    {
        await _filesClient.Authenticate(Owner);

        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var keyA = await SetFakeKeys(idA, "pkA");
        var keyB = await SetFakeKeys(idB, "pkB");

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;

        after.Should().HaveCount(2);
        after.Should().ContainSingle(k => k.Id == idA && k.PublicKey == keyA.PublicKey);
        after.Should().ContainSingle(k => k.Id == idB && k.PublicKey == keyB.PublicKey);
    }

    [Fact]
    [Trait("Bug", "82544")]
    public async Task SetKeys_DuplicateId_RejectedWithConflict()
    {
        // BUG 82544: re-POSTing an id that already exists silently no-ops with 200 instead of
        // rejecting the conflict, so no exception is thrown today. The stored key must survive
        // unchanged either way, which is asserted once the call fails as it should.
        await _filesClient.Authenticate(Owner);

        var id = Guid.NewGuid();
        var original = await SetFakeKeys(id, "pk-orig");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.SetKeysAsync(
                new EncryptionKeyRequestDto(id, "pk-new", "prv-new"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(409);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().ContainSingle(k => k.Id == id);
        after.Single(k => k.Id == id).PublicKey.Should().Be(original.PublicKey);
    }

    [Theory]
    [Trait("Bug", "82554")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetKeys_BlankPublicKey_Rejected(string publicKey)
    {
        // BUG 82554: setKeys performs no input validation on the public key; an empty or
        // whitespace-only value is accepted (200) and stored instead of being rejected with 400.
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.SetKeysAsync(
                new EncryptionKeyRequestDto(publicKey: publicKey, privateKeyEnc: "prv-enc"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();
    }

    public static TheoryData<string, string, string> InvalidSetKeysInputs => new()
    {
        { "Empty privateKeyEnc", "pk", "" },
        { "Whitespace-only privateKeyEnc", "pk", "   " },
        { "Missing publicKey", null!, "prv-enc" },
        { "Missing privateKeyEnc", "pk", null! }
    };

    [Theory]
    [Trait("Bug", "82554")]
    [MemberData(nameof(InvalidSetKeysInputs))]
    public async Task SetKeys_InvalidInput_Rejected(string label, string publicKey, string privateKeyEnc)
    {
        // BUG 82554: setKeys performs no input validation — invalid or absent key data is
        // accepted (200) and stored instead of being rejected with 400. `label` documents which
        // case is under test in the xUnit output.
        _ = label;
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.SetKeysAsync(
                new EncryptionKeyRequestDto(publicKey: publicKey, privateKeyEnc: privateKeyEnc),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("Bug", "82554")]
    public async Task SetKeys_NoRequestBody_Rejected()
    {
        // BUG 82554: an entirely absent DTO binds to defaults and is still accepted (200).
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.SetKeysAsync(
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task SetKeys_LongPublicKey_StoredIntact()
    {
        // Positive control for the oversized case below: 8192 chars round-trips byte-for-byte, so
        // a rejection at a larger size is a length limit and not a general failure to handle long
        // values.
        await _filesClient.Authenticate(Owner);

        var publicKey = new string('x', 8192);

        var response = await _privacyRoomApi.SetKeysWithHttpInfoAsync(
            new EncryptionKeyRequestDto(publicKey: publicKey, privateKeyEnc: "prv"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().ContainSingle();
        after[0].PublicKey.Should().Be(publicKey);
    }

    [Fact]
    [Trait("Bug", "82800")]
    public async Task SetKeys_OversizedPublicKey_MustNotBeSilentlyDropped()
    {
        // BUG 82800: a publicKey too large to persist must be refused with 400. Actual: the call
        // answers 200 with a success-shaped body, yet nothing is stored.
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.SetKeysAsync(
                new EncryptionKeyRequestDto(publicKey: new string('x', 65536), privateKeyEnc: "prv"),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task SetKeys_MalformedId_Rejected400()
    {
        // Unlike the missing/empty value cases above, a malformed id IS validated — this one is a
        // regular (non-bug) positive assertion. A typed Guid parameter cannot carry "not-a-guid",
        // so this goes over raw HTTP.
        await _filesClient.Authenticate(Owner);

        using var response = await SendRawPrivacyRoomRequest(
            HttpMethod.Post,
            "api/2.0/privacyroom/keys",
            """{"id":"not-a-guid","publicKey":"pk","privateKeyEnc":"prv"}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("Bug", "82546")]
    public async Task SetKeys_Create_ShouldReturn201()
    {
        // BUG 82546: creating a resource should answer 201 Created; the API currently answers 200.
        await _filesClient.Authenticate(Owner);

        var publicKey = $"pk-{Guid.NewGuid():N}";
        var privateKeyEnc = $"prv-{Guid.NewGuid():N}";

        var response = await _privacyRoomApi.SetKeysWithHttpInfoAsync(
            new EncryptionKeyRequestDto(publicKey: publicKey, privateKeyEnc: privateKeyEnc),
            TestContext.Current.CancellationToken);

        response.Data.Response.Should().ContainSingle();
        response.Data.Response[0].PublicKey.Should().Be(publicKey);
        response.Data.Response[0].PrivateKeyEnc.Should().Be(privateKeyEnc);

        ((int)response.StatusCode).Should().Be(201);
    }
}
