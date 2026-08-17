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

namespace ASC.Data.Backup.Core.Tests;

/// <summary>
/// Covers how <see cref="FilesModuleSpecifics"/> rewrites <c>files_security</c> rows while a portal is
/// being restored into a new tenant, where every folder and file gets a brand-new id.
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "Restore")]
public class FilesModuleSpecificsTests
{
    // files_security.entry_type as stored in the database: 1 = folder, 2 = file.
    private const int FolderEntryType = 1;

    private const int OldTenantId = 1;
    private const int NewTenantId = 7;
    private const int OldFolderId = 2749;
    private const int NewFolderId = 2774;

    private static readonly string _oldOwner = Guid.NewGuid().ToString();
    private static readonly string _newOwner = Guid.NewGuid().ToString();

    private static TestableFilesModuleSpecifics CreateSpecifics()
    {
        var helpers = new Helpers(new InstanceCrypto(new MachinePseudoKeys(new ConfigurationBuilder().Build())));

        return new TestableFilesModuleSpecifics(helpers);
    }

    private static ColumnMapper CreateColumnMapper()
    {
        var columnMapper = new ColumnMapper();
        columnMapper.SetMapping("tenants_tenants", "id", OldTenantId, NewTenantId);
        columnMapper.SetMapping("files_folder", "id", OldFolderId, NewFolderId);
        columnMapper.SetMapping("core_user", "id", _oldOwner, _newOwner);
        columnMapper.Commit();

        return columnMapper;
    }

    private static TableInfo CreateSecurityTable() => new("files_security", "tenant_id") { UserIDColumns = ["owner"] };

    private static DataRowInfo CreateSecurityRow(bool withInternalEntryId, object entryId)
    {
        var row = new DataRowInfo("files_security");
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("entry_id", entryId);

        if (withInternalEntryId)
        {
            row.SetValue("internal_entry_id", OldFolderId);
        }

        row.SetValue("entry_type", FolderEntryType);
        row.SetValue("subject", Guid.NewGuid().ToString());
        row.SetValue("owner", _oldOwner);

        return row;
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "82056")]
    public async Task TryPrepareRow_FilesSecurity_RemapsInternalEntryIdAlongsideEntryId()
    {
        // Arrange — a share record of a public room whose folder gets a new id on restore
        var specifics = CreateSpecifics();
        var row = CreateSecurityRow(withInternalEntryId: true, entryId: OldFolderId.ToString());

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), CreateSecurityTable(), row);

        // Assert — internal_entry_id must follow entry_id, otherwise every query joining on it
        // stops matching the restored folder and the room looks unshared
        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["entry_id"]).Should().Be(NewFolderId);
        Convert.ToInt32(preparedRow["internal_entry_id"]).Should().Be(NewFolderId);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "82056")]
    public async Task TryPrepareRow_FilesSecurity_WhenInternalEntryIdMissingFromDump_DerivesItFromEntryId()
    {
        // Arrange — a backup taken before the internal_entry_id column existed
        var specifics = CreateSpecifics();
        var row = CreateSecurityRow(withInternalEntryId: false, entryId: OldFolderId.ToString());

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), CreateSecurityTable(), row);

        // Assert — the column is filled in rather than left at the database default of 0
        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["internal_entry_id"]).Should().Be(NewFolderId);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "82056")]
    public async Task TryPrepareRow_FilesSecurity_ThirdPartyEntry_KeepsInternalEntryIdZero()
    {
        // Arrange — third-party entries are addressed by hash and have no internal id
        const string oldHash = "0123456789abcdef0123456789abcdef";
        const string newHash = "fedcba9876543210fedcba9876543210";

        var specifics = CreateSpecifics();
        var columnMapper = CreateColumnMapper();
        columnMapper.SetMapping("files_thirdparty_id_mapping", "hash_id", oldHash, newHash);
        columnMapper.Commit();

        var row = CreateSecurityRow(withInternalEntryId: false, entryId: oldHash);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(columnMapper, CreateSecurityTable(), row);

        // Assert — matches how SecurityDao writes third-party share records
        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        preparedRow!["entry_id"].Should().Be(newHash);
        Convert.ToInt32(preparedRow["internal_entry_id"]).Should().Be(0);
    }

    [Fact]
    public async Task TryPrepareRow_FilesGroup_RemapsTenantAndOwner()
    {
        // Arrange — a room group belongs to the user who created it
        var specifics = CreateSpecifics();
        var table = DeclaredTable(specifics, "files_group");

        var row = new DataRowInfo("files_group");
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("name", "My rooms");
        row.SetValue("icon", "folder");
        row.SetValue("user_id", _oldOwner);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), table, row);

        // Assert — the id column is left to the autoincrement, so only tenant and owner are rewritten
        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["tenant_id"]).Should().Be(NewTenantId);
        preparedRow["user_id"].Should().Be(_newOwner);
        preparedRow.Should().NotContainKey("id");
    }

    [Fact]
    public async Task TryPrepareRow_FilesRoomGroup_InternalRoom_RemapsGroupAndRoom()
    {
        // Arrange — a link between a room group and an internal room
        const int oldGroupId = 11;
        const int newGroupId = 42;

        var specifics = CreateSpecifics();
        var columnMapper = CreateColumnMapper();
        columnMapper.SetMapping("files_group", "id", oldGroupId, newGroupId);
        columnMapper.Commit();

        var row = CreateRoomGroupRow(oldGroupId, internalRoomId: OldFolderId, thirdPartyRoomId: null);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(columnMapper, DeclaredTable(specifics, "files_roomgroup"), row);

        // Assert — both the group and the room must follow their new ids
        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["group_id"]).Should().Be(newGroupId);
        Convert.ToInt32(preparedRow["internal_room_id"]).Should().Be(NewFolderId);
    }

    /// <summary>
    /// The internal id of a third-party link is unset, and the archive can present that as a missing
    /// value, an empty string or <see cref="DBNull"/> — all three must be treated the same.
    /// </summary>
    public static TheoryData<object?> UnsetInternalRoomIds => [null, "", DBNull.Value];

    [Theory]
    [MemberData(nameof(UnsetInternalRoomIds))]
    public async Task TryPrepareRow_FilesRoomGroup_ThirdPartyRoom_RemapsProviderId(object? unsetInternalRoomId)
    {
        // Arrange — a third-party room is addressed as "{selector}-{providerId}", the shape stored by
        // RoomGroupDao and seen in a real archive as "drive-1"
        const int oldGroupId = 11;
        const int newGroupId = 42;
        const int oldProviderId = 1;
        const int newProviderId = 9;

        var specifics = CreateSpecifics();
        var columnMapper = CreateColumnMapper();
        columnMapper.SetMapping("files_group", "id", oldGroupId, newGroupId);
        columnMapper.SetMapping("files_thirdparty_account", "id", oldProviderId, newProviderId);
        columnMapper.Commit();

        var row = new DataRowInfo("files_roomgroup");
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("group_id", oldGroupId);
        row.SetValue("internal_room_id", unsetInternalRoomId);
        row.SetValue("thirdparty_room_id", $"drive-{oldProviderId}");

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(columnMapper, DeclaredTable(specifics, "files_roomgroup"), row);

        // Assert — the provider id inside the string follows the restored account
        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["group_id"]).Should().Be(newGroupId);
        preparedRow["thirdparty_room_id"].Should().Be($"drive-{newProviderId}");
    }

    [Theory]
    // entry_type as stored in the database: 1 = folder, 2 = file.
    [InlineData(1)]
    [InlineData(2)]
    public async Task TryPrepareRow_FilesOrder_RemapsEntryAndParentFolder(int entryType)
    {
        // Arrange — the manual position of an entry inside its parent folder
        const int oldParentId = 100;
        const int newParentId = 500;

        var specifics = CreateSpecifics();
        var columnMapper = CreateColumnMapper();
        columnMapper.SetMapping(entryType == 1 ? "files_folder" : "files_file", "id", OldFolderId, NewFolderId);
        columnMapper.SetMapping("files_folder", "id", oldParentId, newParentId);
        columnMapper.Commit();

        var row = new DataRowInfo("files_order");
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("entry_id", OldFolderId);
        row.SetValue("entry_type", entryType);
        row.SetValue("parent_folder_id", oldParentId);
        row.SetValue("order", 3);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(columnMapper, DeclaredTable(specifics, "files_order"), row);

        // Assert — both the ordered entry and the folder it is ordered in have to follow their new ids
        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["entry_id"]).Should().Be(NewFolderId);
        Convert.ToInt32(preparedRow["parent_folder_id"]).Should().Be(newParentId);
        Convert.ToInt32(preparedRow["order"]).Should().Be(3);
    }

    /// <summary>
    /// Takes the table definition from the module instead of restating it, so these tests also fail when
    /// the declaration itself is missing rather than only when the id relations are.
    /// </summary>
    private static TableInfo DeclaredTable(TestableFilesModuleSpecifics specifics, string name)
    {
        var table = specifics.Tables.SingleOrDefault(t => t.Name == name);

        table.Should().NotBeNull($"{name} has to be declared in FilesModuleSpecifics to be backed up at all");

        return table!;
    }

    private static DataRowInfo CreateRoomGroupRow(int groupId, int? internalRoomId, string? thirdPartyRoomId)
    {
        var row = new DataRowInfo("files_roomgroup");
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("group_id", groupId);
        row.SetValue("internal_room_id", internalRoomId);
        row.SetValue("thirdparty_room_id", thirdPartyRoomId);

        return row;
    }

    /// <summary>
    /// Exposes the protected row-preparation hook. The restore path never touches the connection for
    /// <c>files_security</c> columns, so it is safe to leave it unset here.
    /// </summary>
    private sealed class TestableFilesModuleSpecifics(Helpers helpers)
        : FilesModuleSpecifics(NullLogger<ModuleProvider>.Instance, helpers)
    {
        public Task<(bool, Dictionary<string, object>)> PrepareRowAsync(ColumnMapper columnMapper, TableInfo table, DataRowInfo row)
        {
            return TryPrepareRow(false, null!, columnMapper, table, row);
        }
    }
}
