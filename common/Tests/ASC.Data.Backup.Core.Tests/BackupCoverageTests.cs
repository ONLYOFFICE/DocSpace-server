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
/// Guards the coverage of the tenant-level portal backup.
/// <para>
/// A table is only backed up when some <c>*ModuleSpecifics</c> declares it in its <c>Tables</c>
/// collection — see <see cref="BackupPortalTask"/> and <see cref="RestoreDbModuleTask"/>, both of
/// which iterate the modules. A table nobody declares is silently absent from the archive, which is
/// how room groups and the AI integration layer were lost.
/// </para>
/// <para>
/// Note that <c>_ignoredTables</c> cannot express this intent: it only subtracts from the declared
/// set (and from the full-server dump), so listing an undeclared table there changes nothing. The
/// registries below are the explicit record instead, and this test is what enforces them.
/// </para>
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "Coverage")]
public class BackupCoverageTests
{
    /// <summary>
    /// Kept in step with <c>DbContextActivator</c>, which pins the same version when it builds this
    /// context's model without a connection.
    /// </summary>
    private const string MySqlServerVersion = "9.2.0";

    /// <summary>
    /// Tables deliberately left out of a portal backup, with the reason. Data here either belongs to
    /// the source installation or is transient, so carrying it over would be wrong rather than merely
    /// unnecessary.
    /// </summary>
    private static readonly Dictionary<string, string> _intentionallySkipped = new(StringComparer.OrdinalIgnoreCase)
    {
        ["short_links"] = "Shortened links are bound to the source installation and break on restore.",
        ["account_links"] = "External account (OAuth) links break on restore.",
        ["backup_backup"] = "Backup records are carried over by BackupRepository.MigrationBackupRecordsAsync.",
        ["tenants_tariff"] = "The tariff is assigned by billing for the target portal.",
        ["tenants_tariffrow"] = "The tariff is assigned by billing for the target portal.",
        ["tenants_partners"] = "Partner binding belongs to the source installation.",
        ["notify_queue"] = "Transient notification queue.",
        ["notify_info"] = "Transient notification state.",
        ["event_bus_integration_event_log"] = "Transient event-bus outbox.",
        ["hosting_instance_registration"] = "Instance registry of the source installation; also in _ignoredTables.",
        ["identity_shedlock"] = "Distributed lock state.",
        ["dbip_lookup"] = "GeoIP reference data, rebuilt from its own source.",
        ["webstudio_index"] = "Search index bookkeeping, rebuilt on demand.",
        ["files_properties"] = "Data is an EntryProperties JSON blob carrying room, folder and file ids " +
                               "inside it, which relations cannot remap; restoring it would point form " +
                               "filling at the source portal's entries."
    };

    /// <summary>
    /// Coverage debt: tenant data that a portal backup loses today. Every entry names the ticket that
    /// is meant to add it, and must be deleted once that ticket lands — <see cref="KnownGaps_DoNotOverlapDeclaredTables"/>
    /// fails if an entry is left behind after the table starts being backed up.
    /// </summary>
    private static readonly Dictionary<string, string> _knownGaps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["files_form_role_mapping"] = "T3 — form filling roles",
        ["files_link"] = "T3 — linked files",
        ["files_chat_message_attachment"] = "T4 — AI chat attachments",
        ["ai_integration_assignments"] = "T5 — AI integration",
        ["ai_integration_attachments"] = "T5 — AI integration",
        ["ai_integration_mcp_servers"] = "T5 — AI integration",
        ["ai_integration_messages"] = "T5 — AI integration",
        ["ai_integration_preferences"] = "T5 — AI integration",
        ["ai_integration_profiles"] = "T5 — AI integration",
        ["ai_integration_prompt_folders"] = "T5 — AI integration",
        ["ai_integration_prompts"] = "T5 — AI integration",
        ["ai_integration_threads"] = "T5 — AI integration",
        ["ai_integration_tool_preferences"] = "T5 — AI integration",
        ["ai_providers_default"] = "T5 — AI integration; confirm whether this is seeded data",
        ["core_userdav"] = "T6 — WebDAV credentials",
        ["telegram_users"] = "T6 — Telegram binding",
        ["invitation_link"] = "T6 — invitation links",
        ["app_settings"] = "T6 — app settings",
        ["core_user_api_key"] = "T7 — needs a security decision",
        ["files_thirdparty_app"] = "T7 — encrypted token, needs the files_thirdparty_account treatment",
        ["files_file_keys"] = "T7 — encrypted private key, may not survive a restore",
        ["files_file_vectorization"] = "T7 — regenerable AI state",
        ["firebase_users"] = "T7 — device tokens must not reach another installation"
    };

    [Fact]
    public void EveryTenantScopedTable_IsDeclaredForBackup_OrExplicitlyAccountedFor()
    {
        var (_, tenantTables) = ReadSchema();
        var declared = ReadDeclaredTables();

        var unclassified = tenantTables
            .Where(t => !declared.Contains(t))
            .Where(t => !_intentionallySkipped.ContainsKey(t))
            .Where(t => !_knownGaps.ContainsKey(t))
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

        unclassified.Should().BeEmpty(
            "every table with a tenant_id must be declared by a backup module, or recorded in " +
            $"_intentionallySkipped / _knownGaps in {nameof(BackupCoverageTests)}. Unclassified: " +
            string.Join(", ", unclassified));
    }

    [Fact]
    public void KnownGaps_DoNotOverlapDeclaredTables()
    {
        var declared = ReadDeclaredTables();

        var stale = _knownGaps.Keys.Where(declared.Contains)
            .Concat(_intentionallySkipped.Keys.Where(declared.Contains))
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

        stale.Should().BeEmpty(
            "these tables are already declared for backup, so their registry entries are stale and " +
            "should be deleted: " + string.Join(", ", stale));
    }

    [Fact]
    public void RegistryEntries_ReferenceTablesThatStillExist()
    {
        var (allTables, _) = ReadSchema();

        var unknown = _knownGaps.Keys.Concat(_intentionallySkipped.Keys)
            .Where(t => !allTables.Contains(t))
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();

        unknown.Should().BeEmpty(
            "these registry entries do not match any table in the model — a typo, or the table was " +
            "dropped and the entry should go with it: " + string.Join(", ", unknown));
    }

    [Fact]
    public void SchemaAndModuleEnumeration_ReturnPlausibleResults()
    {
        var (allTables, tenantTables) = ReadSchema();
        var declared = ReadDeclaredTables();

        // Floors, not exact counts: they exist so that a broken enumeration fails loudly instead of
        // letting the guard above pass over an empty set.
        allTables.Count.Should().BeGreaterThan(80);
        tenantTables.Count.Should().BeGreaterThan(50);
        declared.Count.Should().BeGreaterThan(40);
    }

    /// <summary>
    /// Reads the table inventory from the EF model rather than a hard-coded list, so a newly added
    /// table shows up here without anyone remembering to update this test.
    /// </summary>
    private static (HashSet<string> AllTables, HashSet<string> TenantTables) ReadSchema()
    {
        // Building the model needs a provider but never opens a connection. The pinned version keeps
        // the provider from probing the server and matches what DbContextActivator uses for exactly
        // this case (skipConnection + MySQL); UseMicrosoftJson mirrors BaseDbContext, without which
        // json columns fail to map. MySQL is the reference provider: ModelBuilderWrapper defaults to
        // it, and the PostgreSQL branch of the model does not currently validate.
        var options = new DbContextOptionsBuilder<MigrationContext>()
            .UseMySql(
                "Server=localhost;Database=docspace",
                ServerVersion.Parse(MySqlServerVersion),
                providerOptions => providerOptions.UseMicrosoftJson())
            .Options;

        using var context = new MigrationContext(options);

        var allTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tenantTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            var table = entityType.GetTableName();
            if (string.IsNullOrEmpty(table))
            {
                continue;
            }

            allTables.Add(table);

            if (entityType.FindProperty("TenantId") != null)
            {
                tenantTables.Add(table);
            }
        }

        return (allTables, tenantTables);
    }

    private static HashSet<string> ReadDeclaredTables()
    {
        var helpers = new Helpers(new InstanceCrypto(new MachinePseudoKeys(new ConfigurationBuilder().Build())));

        // CoreSettings is only dereferenced while remapping tenants_tenants rows, never while reading
        // the table declarations, so it can stay unset here.
        var moduleProvider = new ModuleProvider(NullLogger<ModuleProvider>.Instance, helpers, null!);

        return moduleProvider.AllModules
            .SelectMany(m => m.Tables)
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
