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
/// Keys stored by a release that knew only one key pair per user. Such a release kept the pair in
/// <c>account_links</c> (provider <c>e2e</c>) as a single <c>{PrivateKeyEnc, PublicKey, UserId}</c>
/// object, where the current code stores an array — and read it as an array, so every read of an
/// upgraded user's keys failed with 500 (<c>JsonException</c>, "could not be converted to List").
/// No API call writes the old shape any more, so the row is planted in the database directly,
/// encrypted the way the services encrypt it.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "PrivacyRoom")]
public class LegacyKeysFormatTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    [Fact]
    public async Task GetUserKeys_LegacySingleObject_ReadAsOneKeyWithEmptyId()
    {
        await PlantLegacyKeys(Owner.Id, "pk-legacy", "prv-legacy");
        await _filesClient.Authenticate(Owner);

        var keys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;

        var key = keys.Should().ContainSingle().Subject;
        key.Id.Should().Be(Guid.Empty);
        key.UserId.Should().Be(Owner.Id);
        key.PublicKey.Should().Be("pk-legacy");
        key.PrivateKeyEnc.Should().Be("prv-legacy");
        key.Date.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public async Task GetUserKeys_LegacySingleObject_ReportsTheSameDateOnEveryRead()
    {
        // The old object carries no date, and the DTO initializer used to fill in the time of the
        // read, so two GETs in a row reported two different dates for the same pair.
        await PlantLegacyKeys(Owner.Id, "pk-legacy", "prv-legacy");
        await _filesClient.Authenticate(Owner);

        var first = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response.Single();
        await Task.Delay(1100, TestContext.Current.CancellationToken);
        var second = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response.Single();

        second.Date.Should().Be(first.Date);
    }

    [Fact]
    public async Task SetKeys_OverLegacySingleObject_KeepsTheLegacyKey()
    {
        await PlantLegacyKeys(Owner.Id, "pk-legacy", "prv-legacy");
        await _filesClient.Authenticate(Owner);

        var id = Guid.NewGuid();
        var added = await SetFakeKeys(id, "pk-new");

        var keys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;

        keys.Should().HaveCount(2);
        keys.Should().ContainSingle(k => k.Id == Guid.Empty && k.PublicKey == "pk-legacy");
        keys.Should().ContainSingle(k => k.Id == id && k.PublicKey == added.PublicKey);
    }

    [Fact]
    public async Task DeleteKeys_EmptyIdOverLegacySingleObject_RemovesTheLegacyKey()
    {
        await PlantLegacyKeys(Owner.Id, "pk-legacy", "prv-legacy");
        await _filesClient.Authenticate(Owner);

        await _privacyRoomApi.DeleteKeysAsync(Guid.Empty, TestContext.Current.CancellationToken);

        var keys = (await _privacyRoomApi.GetUserKeysAsync(TestContext.Current.CancellationToken)).Response;

        keys.Should().BeEmpty();
    }

    /// <summary>
    /// Writes the user's <c>e2e</c> row exactly as the single-pair release did. Must run before
    /// anything reads that user's keys: the services keep linked profiles in a per-node memory cache
    /// for ten minutes, and a row written behind their back is not seen until it expires.
    /// </summary>
    private async Task PlantLegacyKeys(Guid userId, string publicKey, string privateKeyEnc)
    {
        // Property order and casing as JsonSerializer.Serialize(EncryptionKeyPairDto) produced them.
        var legacyJson = JsonSerializer.Serialize(new { PrivateKeyEnc = privateKeyEnc, PublicKey = publicKey, UserId = userId });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:machinekey"] = Fixture.GetAppHostSetting("core:machinekey") })
            .Build();
        var encrypted = await new InstanceCrypto(new MachinePseudoKeys(configuration)).EncryptAsync(legacyJson);

        // LoginProfile.ToString(): Id, provider (APr) and name (N), joined by '→' and '·'.
        var profile = $"Id→{userId}·APr→e2e·N→{encrypted}";

        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO account_links (id, uid, provider, profile, linked)
            VALUES (@id, @uid, 'e2e', @profile, UTC_TIMESTAMP());
            """;

        command.Parameters.AddWithValue("@id", userId.ToString());
        command.Parameters.AddWithValue("@uid", HashHelper.MD5($"e2e/{userId}"));
        command.Parameters.AddWithValue("@profile", profile);

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
