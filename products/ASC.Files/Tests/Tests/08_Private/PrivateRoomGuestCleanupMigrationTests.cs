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
/// The data migration <c>MigrationContext_Upgrade113</c> takes guests made before guests were kept out of private rooms
/// out of them: their access records on a private room and on its items, their file keys and their key pair. A guest
/// can no longer get any of them through the API, so the test plants them in the database, as such a guest left them,
/// and runs the migration's own statements. They act on the whole installation, which is what the migration does;
/// no other test leaves a guest with access to a private room, so they find nothing else to delete. A member who is
/// not a guest keeps everything.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomGuestCleanupMigrationTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<long> Execute(string sql, Dictionary<string, object> parameters)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        var result = await command.ExecuteScalarAsync(TestContext.Current.CancellationToken);

        return result is null or DBNull ? 0 : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private Task<long> Count(string sql, Guid userId, int roomId, int fileId) =>
        Execute(sql, new() { ["@tenant"] = TenantId, ["@user"] = userId.ToString(), ["@room"] = roomId, ["@file"] = fileId });

    private const string AccessRows = "SELECT COUNT(*) FROM files_security WHERE tenant_id = @tenant AND subject = @user AND ((entry_type = 1 AND internal_entry_id = @room) OR (entry_type = 2 AND internal_entry_id = @file));";
    private const string FileKeyRows = "SELECT COUNT(*) FROM files_file_keys WHERE tenant_id = @tenant AND user_id = @user;";
    private const string KeyPairRows = "SELECT COUNT(*) FROM account_links WHERE id = @user AND provider = 'e2e';";

    [Fact]
    public async Task AGuestMadeBefore_LeavesThePrivateRoomWithNoKeys_AndAMemberKeepsBoth()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        var fileId = await UploadEncryptedFile(room.Id, "Encrypted.docx");
        await InviteToRoom(room.Id, member, FileShare.Read);
        (await WriteKeys(fileId, member.Id)).Should().BeEmpty();

        var guest = await InviteGuest();
        var ids = new Dictionary<string, object>
        {
            ["@tenant"] = TenantId, ["@member"] = member.Id.ToString(), ["@guest"] = guest.Id.ToString(), ["@room"] = room.Id,
            ["@file"] = fileId, ["@fileText"] = fileId.ToString(CultureInfo.InvariantCulture), ["@owner"] = Owner.Id.ToString()
        };

        // What a guest of the earlier release held: the member's records, copied for the guest, and a record on the file.
        await Execute("""
            INSERT INTO files_security (tenant_id, entry_id, internal_entry_id, entry_type, subject, owner, security, timestamp, subject_type, options)
            SELECT tenant_id, entry_id, internal_entry_id, entry_type, @guest, owner, security, timestamp, subject_type, options
            FROM files_security WHERE tenant_id = @tenant AND subject = @member AND entry_type = 1 AND internal_entry_id = @room;
            INSERT INTO files_security (tenant_id, entry_id, internal_entry_id, entry_type, subject, owner, security, timestamp, subject_type, options)
            SELECT tenant_id, @fileText, @file, 2, @guest, owner, security, timestamp, subject_type, options
            FROM files_security WHERE tenant_id = @tenant AND subject = @member AND entry_type = 1 AND internal_entry_id = @room;
            INSERT INTO files_file_keys (file_id, tenant_id, user_id, create_on, private_key_enc, public_key_id)
            SELECT file_id, tenant_id, @guest, create_on, private_key_enc, public_key_id FROM files_file_keys WHERE tenant_id = @tenant AND user_id = @member;
            INSERT INTO account_links (id, uid, provider, profile, linked)
            SELECT @guest, uid, provider, profile, linked FROM account_links WHERE id = @member AND provider = 'e2e';
            """, ids);

        (await Count(AccessRows, guest.Id, room.Id, fileId)).Should().Be(2);
        (await Count(FileKeyRows, guest.Id, room.Id, fileId)).Should().Be(1);
        (await Count(KeyPairRows, guest.Id, room.Id, fileId)).Should().Be(1);

        foreach (var statement in new ASC.Migrations.MySql.SaaS.Migrations.MigrationContext_Upgrade113().UpOperations.OfType<SqlOperation>())
        {
            await Execute(statement.Sql, []);
        }

        (await Count(AccessRows, guest.Id, room.Id, fileId)).Should().Be(0);
        (await Count(FileKeyRows, guest.Id, room.Id, fileId)).Should().Be(0);
        (await Count(KeyPairRows, guest.Id, room.Id, fileId)).Should().Be(0);

        (await Count(AccessRows, member.Id, room.Id, fileId)).Should().Be(1);
        (await Count(FileKeyRows, member.Id, room.Id, fileId)).Should().Be(1);
        (await Count(KeyPairRows, member.Id, room.Id, fileId)).Should().Be(1);
    }
}
