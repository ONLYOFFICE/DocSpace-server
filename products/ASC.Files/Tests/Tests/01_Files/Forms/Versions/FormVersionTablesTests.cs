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
/// Which table a form version writes to and under which column names, once the user confirmed that a later version
/// only renamed fields: the earlier answers stay in the table of the first version, under the latest names.
/// Pure logic, no portal.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormVersionTablesTests
{
    [Fact]
    public void GetTableVersions_ReturnsTableVersionAndItsLinksOnly()
    {
        var formFilling = State(Link(2, 1), Link(3, 1), Link(5, 4));

        FormFillingReportCreator.GetTableVersions(formFilling, 1).Should().Equal(1, 2, 3);
        FormFillingReportCreator.GetTableVersions(formFilling, 4).Should().Equal(4, 5);
        FormFillingReportCreator.GetTableVersions(formFilling, 6).Should().Equal(6);
    }

    [Fact]
    public void MapToTableColumn_RenamedLater_MapsToNewColumn()
    {
        var formFilling = State(Link(2, 1, Rename("отКого", "applicantName")));

        FormFillingReportCreator.MapToTableColumn(formFilling, 1, "col_otkogo").Should().Be("col_applicantname");
        FormFillingReportCreator.MapToTableColumn(formFilling, 2, "col_applicantname").Should().Be("col_applicantname");
    }

    [Fact]
    public void MapToTableColumn_FieldNotRenamed_KeepsColumn()
    {
        var formFilling = State(Link(2, 1, Rename("name", "fullName")));

        FormFillingReportCreator.MapToTableColumn(formFilling, 1, "col_email").Should().Be("col_email");
    }

    [Fact]
    public void MapToTableColumn_ChainOfRenames_MapsToLatestColumn()
    {
        var formFilling = State(Link(2, 1, Rename("a", "b")), Link(3, 1, Rename("b", "c")));

        FormFillingReportCreator.MapToTableColumn(formFilling, 1, "col_a").Should().Be("col_c");
        FormFillingReportCreator.MapToTableColumn(formFilling, 2, "col_b").Should().Be("col_c");
        FormFillingReportCreator.MapToTableColumn(formFilling, 3, "col_c").Should().Be("col_c");
    }

    [Fact]
    public void MapToTableColumn_SwappedKeys_MapsEachToTheOther()
    {
        var formFilling = State(Link(2, 1, Rename("a", "b"), Rename("b", "a")));

        FormFillingReportCreator.MapToTableColumn(formFilling, 1, "col_a").Should().Be("col_b");
        FormFillingReportCreator.MapToTableColumn(formFilling, 1, "col_b").Should().Be("col_a");
    }

    [Fact]
    public void MapToTableColumn_RenameInAnotherTable_IsIgnored()
    {
        // Version 4 continues the table of version 3, not of version 1.
        var formFilling = State(Link(2, 1, Rename("a", "b")), Link(4, 3, Rename("a", "z")));

        FormFillingReportCreator.MapToTableColumn(formFilling, 1, "col_a").Should().Be("col_b");
        FormFillingReportCreator.MapToTableColumn(formFilling, 3, "col_a").Should().Be("col_z");
    }

    [Fact]
    public void MapToLatestKey_ChainOfRenames_MapsToLatestKey()
    {
        var formFilling = State(Link(2, 1, Rename("отКого", "applicantName")), Link(3, 1, Rename("applicantName", "applicant")));

        FormFillingReportCreator.MapToLatestKey(formFilling, 1, "отКого").Should().Be("applicant");
        FormFillingReportCreator.MapToLatestKey(formFilling, 2, "applicantName").Should().Be("applicant");
        FormFillingReportCreator.MapToLatestKey(formFilling, 1, "email").Should().Be("email");
    }

    [Fact]
    public void GetColumnRenames_ReturnsColumnNamesInOrder()
    {
        var links = new[] { Link(2, 1, Rename("a", "b"), Rename("First Name", "firstName")), Link(3, 1, Rename("b", "c")) };

        var result = FormFillingReportCreator.GetColumnRenames(links);

        result.Should().Equal(("col_a", "col_b"), ("col_first_name", "col_firstname"), ("col_b", "col_c"));
    }

    private static FormFieldRename Rename(string oldKey, string newKey) => new() { OldKey = oldKey, NewKey = newKey };

    private static FormVersionLink Link(int version, int tableVersion, params FormFieldRename[] renames) =>
        new() { Version = version, TableVersion = tableVersion, Renames = [.. renames] };

    private static FormFillingProperties State(params FormVersionLink[] links) => new() { VersionLinks = [.. links] };
}
