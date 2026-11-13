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

[Trait("Category", "Backup")]
[Trait("Feature", "Restore")]
public class AiIntegrationModuleSpecificsTests
{
    private const int OldTenantId = 1;
    private const int NewTenantId = 7;
    private const int OldProjectId = 2749;
    private const int NewProjectId = 2774;
    private const int OldArtifactsFolderId = 2750;
    private const int NewArtifactsFolderId = 2775;
    private const int OldAttachmentsFolderId = 2751;
    private const int NewAttachmentsFolderId = 2776;

    private static readonly string _oldAuthor = Guid.NewGuid().ToString();
    private static readonly string _newAuthor = Guid.NewGuid().ToString();

    [Fact]
    public async Task TryPrepareRow_Threads_RemapsProjectAndThreadFolders()
    {
        var specifics = CreateSpecifics();

        var row = CreateThreadRow();
        row.SetValue("artifacts_folder_id", OldArtifactsFolderId);
        row.SetValue("attachments_folder_id", OldAttachmentsFolderId);

        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "ai_integration_threads"), row);

        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["tenant_id"]).Should().Be(NewTenantId);
        Convert.ToInt32(preparedRow["entry_id"]).Should().Be(NewProjectId);
        Convert.ToInt32(preparedRow["artifacts_folder_id"]).Should().Be(NewArtifactsFolderId);
        Convert.ToInt32(preparedRow["attachments_folder_id"]).Should().Be(NewAttachmentsFolderId);
        preparedRow["created_by"].Should().Be(_newAuthor);
    }

    [Fact]
    public async Task TryPrepareRow_Threads_WithoutFolders_KeepsEmptyFolderColumns()
    {
        var specifics = CreateSpecifics();

        var row = CreateThreadRow();
        row.SetValue("artifacts_folder_id", null);
        row.SetValue("attachments_folder_id", null);

        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "ai_integration_threads"), row);

        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["entry_id"]).Should().Be(NewProjectId);
        preparedRow["artifacts_folder_id"].Should().BeNull();
        preparedRow["attachments_folder_id"].Should().BeNull();
    }

    [Fact]
    public async Task TryPrepareRow_Threads_ArchiveWithoutFolderColumns_IsPrepared()
    {
        var specifics = CreateSpecifics();

        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "ai_integration_threads"), CreateThreadRow());

        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["entry_id"]).Should().Be(NewProjectId);
        preparedRow.Should().NotContainKey("artifacts_folder_id");
        preparedRow.Should().NotContainKey("attachments_folder_id");
    }

    private static TestableAiIntegrationModuleSpecifics CreateSpecifics()
    {
        var helpers = new Helpers(new InstanceCrypto(new MachinePseudoKeys(new ConfigurationBuilder().Build())));

        return new TestableAiIntegrationModuleSpecifics(helpers);
    }

    private static ColumnMapper CreateColumnMapper()
    {
        var columnMapper = new ColumnMapper();
        columnMapper.SetMapping("tenants_tenants", "id", OldTenantId, NewTenantId);
        columnMapper.SetMapping("files_folder", "id", OldProjectId, NewProjectId);
        columnMapper.SetMapping("files_folder", "id", OldArtifactsFolderId, NewArtifactsFolderId);
        columnMapper.SetMapping("files_folder", "id", OldAttachmentsFolderId, NewAttachmentsFolderId);
        columnMapper.SetMapping("core_user", "id", _oldAuthor, _newAuthor);
        columnMapper.Commit();

        return columnMapper;
    }

    private static DataRowInfo CreateThreadRow()
    {
        var row = new DataRowInfo("ai_integration_threads");
        row.SetValue("id", Guid.CreateVersion7().ToString());
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("title", "Quarterly report");
        row.SetValue("profile_id", null);
        row.SetValue("entry_id", OldProjectId);
        row.SetValue("created_by", _oldAuthor);

        return row;
    }

    private static TableInfo DeclaredTable(TestableAiIntegrationModuleSpecifics specifics, string name)
    {
        var table = specifics.Tables.SingleOrDefault(t => t.Name == name);

        table.Should().NotBeNull($"{name} has to be declared in AiIntegrationModuleSpecifics to be backed up at all");

        return table!;
    }

    private sealed class TestableAiIntegrationModuleSpecifics(Helpers helpers)
        : AiIntegrationModuleSpecifics(NullLogger<ModuleProvider>.Instance, helpers)
    {
        public Task<(bool, Dictionary<string, object>)> PrepareRowAsync(ColumnMapper columnMapper, TableInfo table, DataRowInfo row)
        {
            return TryPrepareRow(false, null!, columnMapper, table, row);
        }
    }
}
