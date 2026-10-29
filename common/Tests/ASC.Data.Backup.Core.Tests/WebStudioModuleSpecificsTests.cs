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

    private static ColumnMapper CreateColumnMapper(bool portalTransfer = false)
    {
        var columnMapper = new ColumnMapper { PortalTransfer = portalTransfer };
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
    public async Task TryPrepareRow_WebhooksLogs_OfAnUnmappedSubscription_IsDropped()
    {
        // Arrange — a delivery record of a subscription that was not restored, dropped with its unmapped creator
        var specifics = CreateSpecifics();

        var row = new DataRowInfo("webhooks_logs");
        row.SetValue("id", 330);
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("config_id", 99);
        row.SetValue("retry_of", null);
        row.SetValue("uid", _oldUser);

        // Act
        var (prepared, _) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — the old config_id would point to no subscription, or to another subscription of the new tenant
        prepared.Should().BeFalse();
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_PendingAttemptOfATransfer_KeepsSchedule()
    {
        // Arrange — a record that was waiting for its next attempt, and not leased by a sender, when the portal was moved
        var specifics = CreateSpecifics();
        var nextAttemptOn = DateTime.UtcNow.AddMinutes(5);
        var row = PendingRow(nextAttemptOn, responsePayload: null, attempts: 1, leased: false);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(portalTransfer: true), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — the pending retry survives a portal transfer
        prepared.Should().BeTrue();
        preparedRow!["next_attempt_on"].Should().Be(nextAttemptOn);
        preparedRow["lease_id"].Should().BeNull();
        preparedRow["response_payload"].Should().BeNull();
        preparedRow["uid"].Should().Be(_newUser);
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_InFlightRowOfATransfer_IsNotSentAgainAndSaysWhy()
    {
        // Arrange — a first attempt a sender of the source held a lease on when the portal was moved; next_attempt_on
        // is the lease expiry then
        var specifics = CreateSpecifics();
        var row = PendingRow(DateTime.UtcNow.AddMinutes(5), responsePayload: null, attempts: 0);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(portalTransfer: true), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — the source may have finished the request: sending it again under a new event.id would be a duplicate
        prepared.Should().BeTrue();
        preparedRow!["next_attempt_on"].Should().BeNull();
        preparedRow["lease_id"].Should().BeNull();
        preparedRow["response_payload"].Should().Be(DbWorker.MovedInFlightNote, "nothing was restored: the portal was moved");
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_InFlightRetryOfATransfer_KeepsTheLastAnswer()
    {
        // Arrange — a retry in flight when the portal was moved, after the target had answered an earlier attempt
        const string lastAnswer = "Service Unavailable";
        var specifics = CreateSpecifics();
        var row = PendingRow(DateTime.UtcNow.AddMinutes(5), responsePayload: lastAnswer, attempts: 2);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(portalTransfer: true), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — not sent again, and the record still shows what the target said
        prepared.Should().BeTrue();
        preparedRow!["next_attempt_on"].Should().BeNull();
        preparedRow["response_payload"].Should().Be(lastAnswer);
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_UnsentRowOfARestore_IsNotSentAndSaysWhy()
    {
        // Arrange — an event waiting for its first attempt when the backup was taken
        var specifics = CreateSpecifics();
        var row = PendingRow(DateTime.UtcNow.AddMinutes(5), responsePayload: null, attempts: 0);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — the portal may have delivered it after the backup: sending it again would be a duplicate
        prepared.Should().BeTrue();
        preparedRow!["next_attempt_on"].Should().BeNull();
        preparedRow["lease_id"].Should().BeNull();
        preparedRow["response_payload"].Should().Be(DbWorker.RestoredFromBackupNote);
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_UnansweredAttemptsOfARestore_AreNotCalledNotSent()
    {
        // Arrange — attempts that never got an answer (a timeout, a refused connection), the next one still pending
        var specifics = CreateSpecifics();
        var row = PendingRow(DateTime.UtcNow.AddMinutes(5), responsePayload: null, attempts: 3);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — the retry is dropped, but the event was sent, so the journal must not say it was not
        prepared.Should().BeTrue();
        preparedRow!["next_attempt_on"].Should().BeNull();
        preparedRow["response_payload"].Should().BeNull();
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_RetryOfARestore_IsNotSentAndKeepsTheLastAnswer()
    {
        // Arrange — a failed attempt waiting for its retry when the backup was taken
        const string lastAnswer = """{"error":"busy"}""";
        var specifics = CreateSpecifics();
        var row = PendingRow(DateTime.UtcNow.AddMinutes(5), lastAnswer, attempts: 1);

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — the retry is dropped, the answer of the failed attempt stays the record of what happened
        prepared.Should().BeTrue();
        preparedRow!["next_attempt_on"].Should().BeNull();
        preparedRow["response_payload"].Should().Be(lastAnswer);
    }

    private static DataRowInfo PendingRow(DateTime nextAttemptOn, string? responsePayload, int attempts, bool leased = true)
    {
        var row = new DataRowInfo("webhooks_logs");
        row.SetValue("id", OldOriginalLogId);
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("config_id", OldConfigId);
        row.SetValue("uid", _oldUser);
        row.SetValue("attempts", attempts);
        row.SetValue("request_headers", "{}");
        row.SetValue("response_payload", responsePayload);
        row.SetValue("next_attempt_on", nextAttemptOn);
        row.SetValue("lease_id", leased ? Guid.NewGuid().ToString() : null);

        return row;
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

    [Fact]
    public async Task TryPrepareRow_WebhooksLogs_EventOfARemovedUser_KeepsTheRecordWithTheEmptyUser()
    {
        // Arrange — the member who caused the event is gone, so their id has no mapping
        var specifics = CreateSpecifics();

        var row = new DataRowInfo("webhooks_logs");
        row.SetValue("id", OldOriginalLogId);
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("config_id", OldConfigId);
        row.SetValue("uid", Guid.NewGuid().ToString());

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_logs"), row);

        // Assert — the delivery history survives the user
        prepared.Should().BeTrue();
        preparedRow!["uid"].Should().Be(Guid.Empty.ToString());
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksConfig_EditedByARemovedUser_KeepsTheSubscription()
    {
        // Arrange — created by a member who is still there, last edited by one who is not
        var specifics = CreateSpecifics();

        var row = new DataRowInfo("webhooks_config");
        row.SetValue("id", OldConfigId);
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("created_by", _oldUser);
        row.SetValue("modified_by", Guid.NewGuid().ToString());

        // Act
        var (prepared, preparedRow) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_config"), row);

        // Assert
        prepared.Should().BeTrue();
        preparedRow!["created_by"].Should().Be(_newUser);
        preparedRow["modified_by"].Should().BeNull();
    }

    [Fact]
    public async Task TryPrepareRow_WebhooksConfig_OfARemovedCreator_IsDropped()
    {
        // Arrange — the owner of the subscription is gone
        var specifics = CreateSpecifics();

        var row = new DataRowInfo("webhooks_config");
        row.SetValue("id", OldConfigId);
        row.SetValue("tenant_id", OldTenantId);
        row.SetValue("created_by", Guid.NewGuid().ToString());

        // Act
        var (prepared, _) = await specifics.PrepareRowAsync(CreateColumnMapper(), DeclaredTable(specifics, "webhooks_config"), row);

        // Assert — an owner-less subscription would skip the access checks made for its owner
        prepared.Should().BeFalse();
    }

    [Theory]
    [InlineData("webhooks_config")]
    [InlineData("webhooks_logs")]
    public void WebhookTable_DeclaresNoDateColumns_SoARestoreKeepsTheHistory(string tableName)
    {
        // Act
        var table = DeclaredTable(CreateSpecifics(), tableName);

        // Assert — the dates are UTC moments of what has already happened (an event queued, an attempt made, the last
        // success of a subscription); a restore that replaces dates must not rewrite them, and the schedule is
        // handled by TryPrepareRow instead
        table.DateColumns.Should().BeEmpty();
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
}
