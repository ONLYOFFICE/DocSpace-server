// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Files.Tests.Tests._01_Files.Forms.Versions;

/// <summary>
/// The SQL the forms databases get for a form table: option lists escaped for each database (a quote or a backslash in
/// an option once broke the MySQL ENUM), the type classes that tell a renamed column of the same kind, and the column
/// renames of a confirmed version. Pure logic, no database.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormTableSchemaTests
{
    private static readonly DbColumnDefinition _options = new("col_name", DbColumnType.Enum, ["O'Brien", @"a\b"]);

    // MySQL also treats a backslash as an escape; the other two take it literally.
    [Theory]
    [InlineData(CoreExternalDatabaseType.MySql, @"ENUM('O''Brien', 'a\\b')")]
    [InlineData(CoreExternalDatabaseType.PostgreSql, @"TEXT CHECK (""col_name"" IN ('O''Brien', 'a\b'))")]
    [InlineData(CoreExternalDatabaseType.Sqlite, @"TEXT CHECK(""col_name"" IN ('O''Brien', 'a\b'))")]
    public void MapType_Options_AreEscapedForTheDatabase(CoreExternalDatabaseType databaseType, string expected)
    {
        ExternalDatabaseClient.MapType(_options, databaseType).Should().Be(expected);
    }

    [Fact]
    public void MapType_NoOptions_FallsBackToText()
    {
        var column = new DbColumnDefinition("col_name", DbColumnType.Enum);

        ExternalDatabaseClient.MapType(column, CoreExternalDatabaseType.MySql).Should().Be("VARCHAR(255)");
        ExternalDatabaseClient.MapType(column, CoreExternalDatabaseType.PostgreSql).Should().Be("TEXT");
    }

    [Theory]
    [InlineData("tinyint(1)", "boolean")]
    [InlineData("boolean", "boolean")]
    [InlineData("int(11)", "integer")]
    [InlineData("INTEGER", "integer")]
    [InlineData("bigint", "integer")]
    [InlineData("timestamp without time zone", "datetime")]
    [InlineData("datetime", "datetime")]
    [InlineData("date", "date")]
    [InlineData("enum('a','b')", "text")]
    [InlineData("varchar(255)", "text")]
    [InlineData("TEXT", "text")]
    public void GetTypeClass_ComparesTheTypesOfAllDatabases(string type, string expected)
    {
        ExternalDatabaseClient.GetTypeClass(type).Should().Be(expected);
    }

    [Fact]
    public void TryRename_RenamesColumnKeepingItsType()
    {
        var columns = Columns();
        var statements = new List<string>();

        var result = ExternalDatabaseClient.TryRename("form_1_v1", '"', columns, [("col_a", "col_x")], statements);

        result.Should().BeTrue();
        statements.Should().Equal(@"ALTER TABLE ""form_1_v1"" RENAME COLUMN ""col_a"" TO ""col_x""");
        columns.Should().ContainKey("col_x").WhoseValue.Should().Be("text");
        columns.Should().NotContainKey("col_a");
    }

    [Fact]
    public void TryRename_ChainOfRenames_AppliesInOrder()
    {
        var columns = Columns();
        var statements = new List<string>();

        var result = ExternalDatabaseClient.TryRename("form_1_v1", '`', columns, [("col_a", "col_c"), ("col_c", "col_d")], statements);

        result.Should().BeTrue();
        statements.Should().HaveCount(2);
        columns.Keys.Should().BeEquivalentTo("col_d", "col_b");
    }

    [Fact]
    public void TryRename_NameTakenByAnotherColumn_Fails()
    {
        var result = ExternalDatabaseClient.TryRename("form_1_v1", '"', Columns(), [("col_a", "col_b")], []);

        result.Should().BeFalse();
    }

    [Fact]
    public void TryRename_MissingOrUnchangedColumn_IsSkipped()
    {
        var statements = new List<string>();

        var result = ExternalDatabaseClient.TryRename("form_1_v1", '"', Columns(), [("col_z", "col_y"), ("col_a", "col_a")], statements);

        result.Should().BeTrue();
        statements.Should().BeEmpty();
    }

    [Fact]
    public void TryRename_InvalidColumnName_Throws()
    {
        var act = () => ExternalDatabaseClient.TryRename("form_1_v1", '"', Columns(), [("col_a; DROP TABLE x", "col_b")], []);

        act.Should().Throw<ArgumentException>();
    }

    private static Dictionary<string, string> Columns() => new() { ["col_a"] = "text", ["col_b"] = "integer" };
}
