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
/// The decisions of the dump of rows (<see cref="NeutralDump"/>): which dump a database restores, which tables a
/// dump leaves out, where its pages lie, and the statements both dialects read and load it with.
/// </summary>
[Trait("Category", "Backup")]
[Trait("Feature", "PostgreSql")]
public class NeutralDumpTests
{
    [Fact]
    public void MissingMigrations_DatabaseOnTheSameMigrations_IsEmpty()
    {
        NeutralDump.MissingMigrations(["a", "b"], ["b", "a"]).Should().BeEmpty();
    }

    [Fact]
    public void MissingMigrations_DatabaseFurtherOn_IsEmpty()
    {
        NeutralDump.MissingMigrations(["a"], ["a", "b"]).Should().BeEmpty();
    }

    [Fact]
    public void MissingMigrations_DumpFurtherOn_NamesWhatTheDatabaseLacksInTheOrderOfTheDump()
    {
        NeutralDump.MissingMigrations(["a", "c", "b"], ["a"]).Should().Equal("c", "b");
    }

    [Fact]
    public void MissingMigrations_MigrationsOfTheOtherDatabase_AreAllMissing()
    {
        // the EF migrations of PostgreSQL and MySQL have ids of their own (migrations/postgre, migrations/mysql)
        string[] postgreSql = ["20261122140000_Standalone_MigrationContextMigrate"];
        string[] mySql = ["20230130103905_Standalone_MigrationContextMigrate", "20260924192531_Standalone_MigrationContext_Upgrade5"];

        NeutralDump.MissingMigrations(postgreSql, mySql).Should().Equal(postgreSql);
    }

    [Theory]
    [InlineData("PostgreSql", "PostgreSql", false)]
    [InlineData("PostgreSql", "MySql", true)]
    [InlineData("MySql", "PostgreSql", true)]
    [InlineData(null, "PostgreSql", true)]
    public void IsOfOtherDatabase_SourceOfTheDump_IsRefusedByADatabaseOfAnotherKind(string? source, string target, bool refused)
    {
        var dialect = target == "PostgreSql" ? BackupSqlDialect.PostgreSql : BackupSqlDialect.MySql;

        NeutralDump.IsOfOtherDatabase(new NeutralDumpManifest { Source = source! }, dialect).Should().Be(refused);
    }

    [Theory]
    [InlineData(1, 500)]
    [InlineData(60, 500)]
    [InlineData(61, 491)]
    [InlineData(1_600, 18)]
    [InlineData(0, 500)]
    public void RowsPerInsert_ColumnsOfATable_KeepAStatementAtMost30000Parameters(int columns, int rows)
    {
        NeutralDump.RowsPerInsert(columns).Should().Be(rows);
    }

    [Fact]
    public void MissingMigrations_IdsDifferingInCase_AreDifferentMigrations()
    {
        NeutralDump.MissingMigrations(["Upgrade1"], ["upgrade1"]).Should().Equal("Upgrade1");
    }

    [Theory]
    [InlineData("__EFMigrationsHistory", true)]
    [InlineData("res_data", true)]
    [InlineData("hosting_instance_registration", true)]
    [InlineData("files_file", false)]
    [InlineData("tenants_tenants", false)]
    public void IsExcluded_Table_IsLeftOutByPrefixAndTheHistory(string table, bool excluded)
    {
        NeutralDump.IsExcluded(table, ["res_", "hosting_instance_registration"]).Should().Be(excluded);
    }

    [Fact]
    public void PageKey_PagesOfATable_LieUnderTheDumpFolderInTheirOrder()
    {
        NeutralDump.PageKey("files_file", 1).Should().Be("dbdump/files_file/000001.xml");
        NeutralDump.PageKey("files_file", 12).Should().Be("dbdump/files_file/000012.xml");
        NeutralDump.ManifestKey.Should().Be("dbdump/manifest.json");
    }

    [Fact]
    public void MigrationHistoryQueryAndSelectAll_QuoteTheNamesOfEachDatabase()
    {
        var table = new TableInfo("files_tag") { Columns = ["id", "title"] };

        BackupSqlDialect.PostgreSql.MigrationHistoryQuery.Should().Be("select \"MigrationId\" from \"__EFMigrationsHistory\";");
        BackupSqlDialect.MySql.MigrationHistoryQuery.Should().Be("select `MigrationId` from `__EFMigrationsHistory`;");
        BackupSqlDialect.PostgreSql.SelectAll(table).Should().Be("select t.\"id\",t.\"title\" from \"files_tag\" as t");
        BackupSqlDialect.MySql.SelectAll(table).Should().Be("select t.`id`,t.`title` from `files_tag` as t");
    }

    [Fact]
    public async Task MySqlDumpLoad_IsNotSupported()
    {
        var begin = () => BackupSqlDialect.MySql.BeginDumpLoadAsync(null!, null!);

        await begin.Should().ThrowAsync<NotSupportedException>();
    }
}
