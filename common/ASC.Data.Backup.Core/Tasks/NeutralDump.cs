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

namespace ASC.Data.Backup.Tasks;

/// <summary>
/// The server-wide dump of a database as rows rather than statements: the rows of every table, in pages written as
/// the portal backup writes a table (DataTable XML), and a manifest naming the database it was taken on, the tables,
/// their pages and the migrations the database had applied. The schema is not in the archive: the restoring database
/// has it from its own migrations. Only PostgreSQL writes and restores such a dump: the EF migrations of MySQL have
/// ids of their own, so a dump cannot be checked against its schema; a database that lacks a migration of the dump
/// refuses it. A dump of MySQL statements (<see cref="KeyHelper.GetDatabaseSchema()"/>) is the kind MySQL writes.
/// </summary>
public static class NeutralDump
{
    public const int Format = 1;

    /// <summary>The table of the EF migrations, which the restoring database keeps as it is.</summary>
    public const string MigrationHistoryTable = "__EFMigrationsHistory";

    private const string Root = "dbdump";

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public static string ManifestKey => $"{Root}/manifest.json";

    /// <summary>The key of a page of a table, numbered from 1.</summary>
    public static string PageKey(string table, int page)
    {
        return $"{Root}/{table}/{page:D6}.xml";
    }

    /// <summary>
    /// The tables a dump leaves out, as the dump of MySQL statements leaves out their rows: those of the ignored
    /// modules and tables and every table starting with "res_", matched as prefixes, and the migration history.
    /// </summary>
    public static bool IsExcluded(string table, IEnumerable<string> excludedPrefixes)
    {
        return string.Equals(table, MigrationHistoryTable, StringComparison.OrdinalIgnoreCase) || excludedPrefixes.Any(table.StartsWith);
    }

    /// <summary>
    /// The migrations the dump was taken after and the restoring database has not applied, in the order of the
    /// dump. A dump is restored only when this is empty: its rows may need a column or a table that only such a
    /// migration creates. A restoring database further on is accepted.
    /// </summary>
    public static List<string> MissingMigrations(IEnumerable<string> dumped, IEnumerable<string> applied)
    {
        var target = applied.ToHashSet(StringComparer.Ordinal);

        return dumped.Where(migration => !target.Contains(migration)).ToList();
    }

    /// <summary>
    /// True when the dump was taken on a database of another kind than the dialect is for. Its migrations then
    /// cannot be compared with those of the restoring database, so the dump is refused.
    /// </summary>
    public static bool IsOfOtherDatabase(NeutralDumpManifest manifest, BackupSqlDialect dialect)
    {
        return !string.Equals(manifest.Source, dialect.Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// The rows one insert of a restore carries: 500, or fewer for a table with more than 60 columns, so that a
    /// statement has at most 30 000 parameters (PostgreSQL takes at most 65 535, and a table at most 1 600 columns).
    /// </summary>
    public static int RowsPerInsert(int columns)
    {
        return Math.Clamp(30_000 / Math.Max(columns, 1), 1, 500);
    }

    public static async Task<Stream> WriteManifestAsync(NeutralDumpManifest manifest, TempStream tempStream)
    {
        var stream = tempStream.Create();
        await System.Text.Json.JsonSerializer.SerializeAsync(stream, manifest, _jsonOptions);
        stream.Position = 0;

        return stream;
    }

    /// <summary>The manifest of the archive, or null when the archive is not a dump of this kind.</summary>
    public static async Task<NeutralDumpManifest> ReadManifestAsync(IDataReadOperator reader)
    {
        await using var stream = reader.GetEntry(ManifestKey);
        if (stream == null)
        {
            return null;
        }

        var manifest = await System.Text.Json.JsonSerializer.DeserializeAsync<NeutralDumpManifest>(stream, _jsonOptions);
        if (manifest == null || manifest.Format != Format)
        {
            throw new ArgumentException(BackupResource.BackupInvalid);
        }

        return manifest;
    }
}

/// <summary>What a dump holds.</summary>
public sealed class NeutralDumpManifest
{
    public int Format { get; init; } = NeutralDump.Format;

    /// <summary>The database the dump was taken on (<see cref="BackupSqlDialect.Name"/>); only one of the same kind restores it.</summary>
    public string Source { get; init; }

    public List<string> Migrations { get; init; } = [];

    public List<NeutralDumpTable> Tables { get; init; } = [];
}

/// <summary>A table of a dump and the number of its pages.</summary>
public sealed class NeutralDumpTable
{
    public string Name { get; init; }

    public int Pages { get; set; }

    public long Rows { get; set; }
}
