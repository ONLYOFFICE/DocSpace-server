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
/// Column names as each forms database keeps them: PostgreSQL cuts a name to 63 characters without an error, MySQL to
/// 64, so the names the database reports are compared with the names cut the same way. Pure logic, no database.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormColumnNameTests
{
    private static readonly string _longName = "col_" + new string('a', 66);

    [Theory]
    [InlineData(CoreExternalDatabaseType.PostgreSql, 63)]
    [InlineData(CoreExternalDatabaseType.MySql, 64)]
    [InlineData(CoreExternalDatabaseType.Sqlite, 70)]
    public void ToStoredColumnName_LongName_IsCutLikeTheDatabaseDoes(CoreExternalDatabaseType databaseType, int length)
    {
        var stored = ExternalDatabaseClient.ToStoredColumnName(_longName, databaseType);

        stored.Should().Be(_longName[..length]);
        ExternalDatabaseClient.ToStoredColumnName(stored, databaseType).Should().Be(stored, "a name the database reported stays as it is");
    }

    [Fact]
    public void ToStoredColumnName_ShortName_StaysAsItIs()
    {
        ExternalDatabaseClient.ToStoredColumnName("col_applicant", CoreExternalDatabaseType.PostgreSql).Should().Be("col_applicant");
    }

    // Two fields that differ only after the 63rd character would make one column on PostgreSQL: the first column keeps
    // it, and a row keeps the value of that field, whatever order its keys come in.
    [Fact]
    public void ToStoredRow_NamesCutToOne_KeepsTheValueOfTheFieldTheColumnWasMadeFor()
    {
        var leftOut = new HashSet<string>();
        var columns = ExternalDatabaseClient.ToStoredColumns(
            [new DbColumnDefinition("form_id", DbColumnType.Integer), new DbColumnDefinition(_longName + "x", DbColumnType.Text), new DbColumnDefinition(_longName + "y", DbColumnType.Text)],
            CoreExternalDatabaseType.PostgreSql, leftOut);
        var row = new Dictionary<string, object> { ["form_id"] = 7, [_longName + "y"] = "second", [_longName + "x"] = "first" };

        var stored = ExternalDatabaseClient.ToStoredRow(row, CoreExternalDatabaseType.PostgreSql, leftOut);

        columns.Select(c => c.Name).Should().Equal("form_id", _longName[..63]);
        leftOut.Should().Equal(_longName + "y");
        stored.Should().HaveCount(2);
        stored[_longName[..63]].Should().Be("first");
        ExternalDatabaseClient.ToStoredRow(row, CoreExternalDatabaseType.Sqlite).Should().HaveCount(3);
    }

    // Quoted names are case sensitive in PostgreSQL only.
    [Theory]
    [InlineData(CoreExternalDatabaseType.PostgreSql, 2)]
    [InlineData(CoreExternalDatabaseType.MySql, 1)]
    public void ToStoredColumns_NamesDifferingInCase_FollowTheDatabase(CoreExternalDatabaseType databaseType, int count)
    {
        var columns = ExternalDatabaseClient.ToStoredColumns(
            [new DbColumnDefinition("col_name", DbColumnType.Text), new DbColumnDefinition("col_Name", DbColumnType.Text)], databaseType, null);

        columns.Should().HaveCount(count);
    }
}
