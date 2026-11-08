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
/// What a MySQL forms database gets for a form table, where each ALTER commits at once: ENUM options the column collation
/// takes for one are declared once, and a merge finds a source value the target column cannot take before any column
/// changes. Pure logic, no database.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormMySqlSchemaTests
{
    // Under the default collation MySQL takes options differing only in case, accents or trailing spaces for one, and
    // refuses to declare both.
    [Fact]
    public void BuildWidenOptions_DefaultCollation_SkipsOptionsDifferingInCaseAccentsOrSpaces()
    {
        ExternalDatabaseClient.BuildWidenOptions("form_1_v1", "col_answer", ["Yes", "No", "Ne"], ["yes", "No ", "Né"], null, CoreExternalDatabaseType.MySql)
            .Should().BeEmpty();
        ExternalDatabaseClient.BuildWidenOptions("form_1_v1", "col_answer", ["Yes", "No"], ["yes", "Maybe"], null, CoreExternalDatabaseType.MySql)
            .Should().Equal("ALTER TABLE `form_1_v1` MODIFY COLUMN `col_answer` ENUM('Yes', 'No', 'Maybe')");
    }

    // A case or accent sensitive collation keeps such options apart, so the list takes them.
    [Theory]
    [InlineData("utf8mb4_bin", "ENUM('Yes', 'Ne', 'yes', 'Né')")]
    [InlineData("utf8mb4_0900_as_cs", "ENUM('Yes', 'Ne', 'yes', 'Né')")]
    [InlineData("utf8mb4_0900_as_ci", "ENUM('Yes', 'Ne', 'Né')")]
    [InlineData("utf8mb4_0900_ai_ci", null)]
    [InlineData("utf8mb4_cs_0900_ai_ci", null)]
    public void BuildWidenOptions_ByCollation_KeepsWhatTheColumnTellsApart(string collation, string expected)
    {
        var comparer = ExternalDatabaseClient.GetOptionComparer(CoreExternalDatabaseType.MySql, collation);

        var result = ExternalDatabaseClient.BuildWidenOptions("form_1_v1", "col_answer", ["Yes", "Ne"], ["yes", "Né"], null, CoreExternalDatabaseType.MySql, comparer);

        string[] statements = expected == null ? [] : [$"ALTER TABLE `form_1_v1` MODIFY COLUMN `col_answer` {expected}"];
        result.Should().Equal(statements);
    }

    [Fact]
    public void BuildWidenOptions_PostgreSqlOptionDiffersOnlyInCase_IsAdded()
    {
        ExternalDatabaseClient.BuildWidenOptions("form_1_v1", "col_answer", ["Yes"], ["yes"], "form_1_v1_col_answer_check", CoreExternalDatabaseType.PostgreSql)
            .Should().HaveCount(2);
    }

    [Fact]
    public void MapType_OptionsOneUnderTheCollation_AreDeclaredOnce()
    {
        var column = new DbColumnDefinition("col_kind", DbColumnType.Enum, ["Paid", "Unpaid", "paid", "Ne", "Né"]);

        ExternalDatabaseClient.MapType(column, CoreExternalDatabaseType.MySql).Should().Be("ENUM('Paid', 'Unpaid', 'Ne')");
        ExternalDatabaseClient.MapType(column, CoreExternalDatabaseType.MySql, ExternalDatabaseClient.GetOptionComparer(CoreExternalDatabaseType.MySql, "utf8mb4_bin"))
            .Should().Be("ENUM('Paid', 'Unpaid', 'paid', 'Ne', 'Né')");
        ExternalDatabaseClient.MapType(column, CoreExternalDatabaseType.PostgreSql).Should().Contain("'paid'");
    }

    [Fact]
    public void BuildMySqlCopyCheck_TextIntoList_LooksForValuesOutsideTheOptions()
    {
        var check = ExternalDatabaseClient.BuildMySqlCopyCheck("form_1_v2", "col_kind", "text", "enum('Paid','Unpaid')");

        check.Should().NotBeNull();
        check.Value.Sql.Should().Be("SELECT EXISTS (SELECT 1 FROM `form_1_v2` WHERE `col_kind` IS NOT NULL AND TRIM(TRAILING ' ' FROM `col_kind`) NOT IN (@option0, @option1))");
        check.Value.Parameters.Values.Should().Equal("Paid", "Unpaid");
    }

    // The source list widens the target one; whether 'paid' is 'Paid' is up to the collation, so it is checked as well.
    [Fact]
    public void BuildMySqlCopyCheck_ListIntoList_ChecksAgainstTheWidenedList()
    {
        var check = ExternalDatabaseClient.BuildMySqlCopyCheck("form_1_v2", "col_kind", "enum('paid','Sick')", "enum('Paid','Unpaid')");

        check.Should().NotBeNull();
        check.Value.Parameters.Values.Should().Equal("Paid", "Unpaid", "Sick");

        var binary = ExternalDatabaseClient.BuildMySqlCopyCheck("form_1_v2", "col_kind", "enum('paid','Sick')", "enum('Paid','Unpaid')",
            ExternalDatabaseClient.GetOptionComparer(CoreExternalDatabaseType.MySql, "utf8mb4_bin"));
        binary.Value.Parameters.Values.Should().Equal("Paid", "Unpaid", "paid", "Sick");
    }

    [Fact]
    public void BuildMySqlCopyCheck_TextIntoVarchar_LooksForLongerValues()
    {
        var check = ExternalDatabaseClient.BuildMySqlCopyCheck("form_1_v2", "col_name", "text", "varchar(255)");

        check.Should().NotBeNull();
        check.Value.Sql.Should().EndWith("AND CHAR_LENGTH(TRIM(TRAILING ' ' FROM `col_name`)) > 255)");
        check.Value.Parameters.Should().BeEmpty();
    }

    [Theory]
    [InlineData("varchar(255)", "varchar(255)")]
    [InlineData("text", "text")]
    [InlineData("int", "int")]
    public void BuildMySqlCopyCheck_ColumnTakesAnySourceValue_ChecksNothing(string sourceType, string targetType)
    {
        ExternalDatabaseClient.BuildMySqlCopyCheck("form_1_v2", "col_name", sourceType, targetType).Should().BeNull();
    }
}
