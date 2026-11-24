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
/// The key routes of a user are on record in the audit trail: <c>POST privacyroom/keys</c> writes
/// <c>PrivacyRoomKeyCreated</c>, <c>PUT</c> writes <c>PrivacyRoomKeyUpdated</c> and <c>DELETE privacyroom/keys/{id}</c>
/// writes <c>PrivacyRoomKeyDeleted</c>, each naming the key.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivacyRoomKeyAuditTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private const int KeyCreated = 5161;
    private const int KeyUpdated = 5162;
    private const int KeyDeleted = 5163;

    private Task<long> Records(int action, Guid keyId) => AuditRecords(action, keyId);

    [Fact]
    public async Task TheKeyRoutes_RecordTheCreation_TheReplacement_AndTheDeletion()
    {
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var keyId = Guid.NewGuid();

        await SetFakeKeys(Guid.Empty, "first");
        await SetFakeKeys(keyId, "second");
        (await PollUntil(() => Records(KeyCreated, keyId), count => count > 0)).Should().Be(1);
        (await Records(KeyUpdated, keyId)).Should().Be(0);

        await _privacyRoomApi.ReplaceKeyAsync(new EncryptionKeyRequestDto(keyId, $"third-{Guid.NewGuid():N}", $"prv-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);
        (await PollUntil(() => Records(KeyUpdated, keyId), count => count > 0)).Should().Be(1);
        (await Records(KeyDeleted, keyId)).Should().Be(0);

        await _privacyRoomApi.DeleteKeysAsync(keyId, TestContext.Current.CancellationToken);
        (await PollUntil(() => Records(KeyDeleted, keyId), count => count > 0)).Should().Be(1);
    }
}
