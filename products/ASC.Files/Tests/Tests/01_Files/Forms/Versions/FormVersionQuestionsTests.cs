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
/// The questions about renamed fields: a version waits for an answer while it renames fields and nobody answered;
/// "yes" makes it a decision to apply, "no" removes it. Pure logic, no portal.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormVersionQuestionsTests
{
    [Fact]
    public void GetOpenQuestions_ReturnsOpenOnesOldestFirst()
    {
        var formFilling = State(Pending(5, null, Rename("c", "d")), Pending(3, null, Rename("a", "b")), Pending(4, true, Rename("b", "c")));

        FormFillingReportCreator.GetOpenQuestions(formFilling).Select(l => l.Version).Should().Equal(3, 5);
    }

    [Fact]
    public void GetDecidedVersions_ReturnsAnsweredAndThoseWithoutRenames()
    {
        var formFilling = State(Pending(3, null, Rename("a", "b")), Pending(4, true, Rename("b", "c")), Pending(6, null));

        FormFillingReportCreator.GetDecidedVersions(formFilling).Should().Equal(4, 6);
    }

    [Fact]
    public void SameRenames_SameTableAndPairs_IgnoresFieldDescriptions()
    {
        var a = Pending(3, null, new FormFieldRename { OldKey = "a", NewKey = "b", Type = "text", Tip = "Name" });
        var b = Pending(3, null, Rename("a", "b"));

        FormFillingReportCreator.SameRenames(a, b).Should().BeTrue();
    }

    [Fact]
    public void SameRenames_OtherTableOrPairs_IsFalse()
    {
        var link = Pending(3, null, Rename("a", "b"), Rename("c", "d"));

        FormFillingReportCreator.SameRenames(link, Pending(3, null, Rename("c", "d"), Rename("a", "b"))).Should().BeFalse();
        FormFillingReportCreator.SameRenames(link, Pending(3, null, Rename("a", "b"))).Should().BeFalse();
        FormFillingReportCreator.SameRenames(link, new FormVersionLink { Version = 3, TableVersion = 1, Renames = link.Renames }).Should().BeFalse();
    }

    private static FormFieldRename Rename(string oldKey, string newKey) => new() { OldKey = oldKey, NewKey = newKey };

    // The previous version is the table the pending link would continue.
    private static FormVersionLink Pending(int version, bool? confirmed, params FormFieldRename[] renames) =>
        new() { Version = version, TableVersion = version - 1, Renames = [.. renames], Confirmed = confirmed };

    private static FormFillingProperties State(params FormVersionLink[] pending) => new() { PendingVersionLinks = [.. pending] };
}
