// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
