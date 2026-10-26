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
/// Covers how <see cref="WebStudioModuleSpecifics"/> rewrites webhook delivery rows while a portal is being
/// restored into a new tenant, where every delivery record gets a brand-new id.
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "Restore")]
public class WebStudioModuleSpecificsTests
{
    private const int OldTenantId = 1;
    private const int NewTenantId = 7;
    private const int OldConfigId = 12;
    private const int NewConfigId = 30;
    private const int OldOriginalLogId = 310;
    private const int NewOriginalLogId = 452;

    private static readonly string _oldUser = Guid.NewGuid().ToString();
    private static readonly string _newUser = Guid.NewGuid().ToString();

    private static TestableWebStudioModuleSpecifics CreateSpecifics()
    {
        var helpers = new Helpers(new InstanceCrypto(new MachinePseudoKeys(new ConfigurationBuilder().Build())));

        return new TestableWebStudioModuleSpecifics(helpers);
    }

    private static ColumnMapper CreateColumnMapper()
    {
        var columnMapper = new ColumnMapper();
        columnMapper.SetMapping("tenants_tenants", "id", OldTenantId, NewTenantId);
        columnMapper.SetMapping("webhooks_config", "id", OldConfigId, NewConfigId);
        columnMapper.SetMapping("webhooks_logs", "id", OldOriginalLogId, NewOriginalLogId);
        columnMapper.SetMapping("core_user", "id", _oldUser, _newUser);
        columnMapper.Commit();

        return columnMapper;
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_ManualRetry_RemapsOriginalAndRetriedBy()
    {
        // Arrange — a manual retry, pointing to the event's original record
        var specifics = CreateSpecifics();

        var row = new DataRowInfo("webhooks_logs");
        row.SetValue("id", 311);
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("config_id", OldConfigId);
        row.SetValue("retry_of", OldOriginalLogId);
        row.SetValue("retried_by", _oldUser);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — the retry keeps pointing to its event, which is what the receiver sees as event.id
        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        Convert.ToInt32(preparedRow!["tenant_id"]).Should().Be(NewTenantId);
        Convert.ToInt32(preparedRow["config_id"]).Should().Be(NewConfigId);
        Convert.ToInt32(preparedRow["retry_of"]).Should().Be(NewOriginalLogId);
        preparedRow["retried_by"].Should().Be(_newUser);
        preparedRow.Should().NotContainKey("id");
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_OriginalRecord_KeepsEmptyRetryColumns()
    {
        // Arrange — the first delivery of an event, sent by the portal itself
        var specifics = CreateSpecifics();

        var row = new DataRowInfo("webhooks_logs");
        row.SetValue("id", OldOriginalLogId);
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("config_id", OldConfigId);
        row.SetValue("retry_of", null);
        row.SetValue("retried_by", null);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — empty retry columns must not drop the record
        prepared.Should().BeTrue();
        preparedRow.Should().NotBeNull();
        preparedRow!["retry_of"].Should().BeNull();
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_RetryOfAnUnmappedOriginal_IsDropped()
    {
        // Arrange — a retry whose original record has not been restored, so its new id is unknown
        var specifics = CreateSpecifics();

        var row = new DataRowInfo("webhooks_logs");
        row.SetValue("id", 320);
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("config_id", OldConfigId);
        row.SetValue("retry_of", 315);
        row.SetValue("retried_by", _oldUser);

        // Act
        var (prepared, _) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — keeping the old id would point the retry, and the event.id it sends, at an unrelated record
        prepared.Should().BeFalse();
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_PendingAttempt_IsNotScheduledAgain()
    {
        // Arrange — a record that was due for its next attempt, and leased by a sender, when the backup was taken
        var specifics = CreateSpecifics();

        var row = new DataRowInfo("webhooks_logs");
        row.SetValue("id", OldOriginalLogId);
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("config_id", OldConfigId);
        row.SetValue("uid", _oldUser);
        row.SetValue("next_attempt_on", DateTime.UtcNow.AddMinutes(5));
        row.SetValue("lease_id", Guid.NewGuid().ToString());

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — the restored portal keeps the history but sends nothing the source portal had queued
        prepared.Should().BeTrue();
        preparedRow!["next_attempt_on"].Should().BeNull();
        preparedRow["lease_id"].Should().BeNull();
        preparedRow["uid"].Should().Be(_newUser);
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_EventOfABackgroundJob_KeepsTheEmptyUser()
    {
        // Arrange — an event no user caused, written with the empty user id
        var specifics = CreateSpecifics();

        var row = new DataRowInfo("webhooks_logs");
        row.SetValue("id", OldOriginalLogId);
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("config_id", OldConfigId);
        row.SetValue("uid", Guid.Empty.ToString());

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — an unmappable user drops a row, but the empty user is not one
        prepared.Should().BeTrue();
        preparedRow!["uid"].Should().Be(Guid.Empty.ToString());
    }

    private static TableInfo DeclaredTable(TestableWebStudioModuleSpecifics specifics, string name)
    {
        var table = specifics.Tables.SingleOrDefault(t => t.Name == name);

        table.Should().NotBeNull($"{name} has to be declared in WebStudioModuleSpecifics to be backed up at all");

        return table!;
    }

    private sealed class TestableWebStudioModuleSpecifics(Helpers helpers)
        : WebStudioModuleSpecifics(NullLogger<ModuleProvider>.Instance, helpers)
    {
        public Task<(bool, Dictionary<string, object>)> PrepareRowAsync(ColumnMapper columnMapper, TableInfo table, DataRowInfo row)
        {
            return TryPrepareRow(false, null!, columnMapper, table, row);
        }
    }
}
