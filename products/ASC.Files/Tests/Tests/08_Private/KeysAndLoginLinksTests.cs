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
/// Encryption keys live in <c>account_links</c> beside the user's sign-in links. Saving a key used to
/// remove the user's first link of any provider before writing the new set, so a user who signs in
/// through Google or SSO lost that link, and the old key row, left in place, then failed the save
/// with "The account is already in use". The save now removes the user's key row only. The login
/// rows are planted directly because no API call can link an external provider without a real one.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "PrivacyRoom")]
public class KeysAndLoginLinksTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    // Sorts before any MD5 hash, so the login row is the first of the user's rows in the primary key.
    private const string FirstUid = "00000000000000000000000000000000";

    [Fact]
    public async Task SetKeys_UserWithLoginLink_KeepsTheLink()
    {
        await PlantLoginLink(Owner.Id);
        await _filesClient.Authenticate(Owner);

        await SetFakeKeys();

        (await ReadProviders(Owner.Id)).Should().Contain("google").And.Contain("e2e");
    }

    [Fact]
    public async Task ReplaceKey_UserWithLoginLink_KeepsTheLinkAndStoresTheNewKey()
    {
        await PlantLoginLink(Owner.Id);
        await PlantLegacyKeys(Owner.Id, "pk-old", "prv-old");
        await _filesClient.Authenticate(Owner);

        var keys = (await _privacyRoomApi.ReplaceKeyAsync(
            new EncryptionKeyRequestDto(Guid.Empty, "pk-new", "prv-new"),
            TestContext.Current.CancellationToken)).Response;

        keys.Should().ContainSingle(k => k.PublicKey == "pk-new");
        (await ReadProviders(Owner.Id)).Should().Contain("google").And.Contain("e2e");
    }

    [Fact]
    public async Task DeleteKeys_UserWithLoginLink_KeepsTheLink()
    {
        await PlantLoginLink(Owner.Id);
        await _filesClient.Authenticate(Owner);

        var key = await SetFakeKeys();
        await _privacyRoomApi.DeleteKeysAsync(key.Id, TestContext.Current.CancellationToken);

        (await ReadProviders(Owner.Id)).Should().Contain("google");
    }

    private Task PlantLoginLink(Guid userId)
    {
        return PlantAccountLink(userId, "google", FirstUid, $"Id→google-{userId}·APr→google·N→Autotest");
    }

    private async Task<List<string>> ReadProviders(Guid userId)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT provider FROM account_links WHERE id = @id;";
        command.Parameters.AddWithValue("@id", userId.ToString());

        var providers = new List<string>();

        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            providers.Add(reader.GetString(0));
        }

        return providers;
    }
}
