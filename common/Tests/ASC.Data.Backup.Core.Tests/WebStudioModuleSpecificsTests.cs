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
/// Covers the tables of <see cref="WebStudioModuleSpecifics"/> that a portal backup has to carry over.
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "Restore")]
public class WebStudioModuleSpecificsTests
{
    private const int OldTenantId = 1;
    private const int NewTenantId = 7;

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "84721")]
    public async Task TryPrepareRow_AppSettings_RemapsTenantAndKeepsState()
    {
        // Arrange — an application the portal switched off, with its own settings document
        const string settings = """{"limit":5}""";
        var lastModified = new DateTime(2026, 3, 14, 9, 26, 53, DateTimeKind.Utc);

        var specifics = new TestableWebStudioModuleSpecifics(
            new Helpers(new InstanceCrypto(new MachinePseudoKeys(new ConfigurationBuilder().Build()))));
        var table = specifics.Tables.SingleOrDefault(t => t.Name == "app_settings");

        var columnMapper = new ColumnMapper();
        columnMapper.SetMapping("tenants_tenants", "id", OldTenantId, NewTenantId);
        columnMapper.Commit();

        var row = new DataRowInfo("app_settings");
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("id", "ai-rooms");
        row.SetValue("enabled", false);
        row.SetValue("settings", settings);
        row.SetValue("last_modified", lastModified);

        // Act
        table.Should().NotBeNull("app_settings has to be declared in WebStudioModuleSpecifics to be backed up at all");
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(columnMapper, table!, row);

        // Assert — the application id is a natural key and is kept, only the tenant moves
        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["tenant_id"]).Should().Be(NewTenantId);
        preparedRow["id"].Should().Be("ai-rooms");
        preparedRow["enabled"].Should().Be(false);
        preparedRow["settings"].Should().Be(settings);
        preparedRow["last_modified"].Should().Be(lastModified);
    }

    /// <summary>
    /// Exposes the protected row-preparation hook. The restore path never touches the connection for
    /// <c>app_settings</c> columns, so it is safe to leave it unset here.
    /// </summary>
    private sealed class TestableWebStudioModuleSpecifics(Helpers helpers)
        : WebStudioModuleSpecifics(NullLogger<ModuleProvider>.Instance, helpers)
    {
        public Task<(bool, Dictionary<string, object>)> PrepareRowAsync(ColumnMapper columnMapper, TableInfo table, DataRowInfo row)
        {
            return TryPrepareRow(false, null!, columnMapper, table, row);
        }
    }
}
