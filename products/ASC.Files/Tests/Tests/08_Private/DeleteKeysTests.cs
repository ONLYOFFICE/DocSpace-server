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
/// <c>DELETE /api/2.0/privacyroom/keys/{id}</c> — deleteKeys.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "PrivacyRoom")]
public class DeleteKeysTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "82551")]
    public async Task DeleteKeys_ExistingKey_ShouldReturnNoContent()
    {
        // BUG 82551: deleting an existing resource should answer 204 No Content; the API answers
        // 200. The key is verified gone first, so only the status assertion is expected to fail.
        await _filesClient.Authenticate(Owner);

        await SetFakeKeys();

        var response = await _privacyRoomApi.DeleteKeysWithHttpInfoAsync(Guid.Empty, TestContext.Current.CancellationToken);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    [Trait("Bug", "82552")]
    public async Task DeleteKeys_ValidButNonExistentKey_ShouldReturnNotFound()
    {
        // BUG 82552: deleting a key that does not exist should be 404; the API answers 200 for any
        // syntactically valid GUID whether or not the key exists (a silent idempotent no-op).
        await _filesClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.DeleteKeysAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "82553")]
    public async Task DeleteKeys_MalformedId_ShouldReturnBadRequest()
    {
        // BUG 82553: a malformed (non-GUID) id should be a 400 bad request, the same way setKeys
        // and replaceKey already reject a malformed id; deleteKeys returns 404 instead, which looks
        // like a route-constraint miss and is inconsistent with the other two.
        await _filesClient.Authenticate(Owner);

        using var response = await SendRawPrivacyRoomRequest(HttpMethod.Delete, "api/2.0/privacyroom/keys/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteKeys_WithoutId_ReturnsMethodNotAllowed()
    {
        // Not an id-validation case: an empty id collapses the URL to the collection route
        // DELETE /api/2.0/privacyroom/keys, which exists for POST/PUT but has no DELETE handler.
        await _filesClient.Authenticate(Owner);

        using var response = await SendRawPrivacyRoomRequest(HttpMethod.Delete, "api/2.0/privacyroom/keys/");

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    [Trait("Bug", "82552")]
    public async Task DeleteKeys_AlreadyDeletedKey_ShouldReturnNotFoundOnSecondDelete()
    {
        // BUG 82552: the first delete removes the key (should be 204); a second delete targets a
        // now-missing key and should return 404. Actual: both return 200 (silent idempotent no-op).
        await _filesClient.Authenticate(Owner);

        await SetFakeKeys();
        await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().BeNullOrEmpty();
    }

    [Fact]
    [Trait("Bug", "82551")]
    public async Task DeleteKeys_DeletesOnlyTheTargetedKey_OthersRemain()
    {
        await _filesClient.Authenticate(Owner);

        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();
        await SetFakeKeys(idA);
        await SetFakeKeys(idB);
        await SetFakeKeys(idC);

        var response = await _privacyRoomApi.DeleteKeysWithHttpInfoAsync(idB, TestContext.Current.CancellationToken);

        var after = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;
        after.Should().HaveCount(2);
        after.Select(k => k.Id).Should().BeEquivalentTo([idA, idC]);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
