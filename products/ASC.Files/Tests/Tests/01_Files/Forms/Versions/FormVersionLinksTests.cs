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
/// How a decision about renamed fields joins a version to an earlier table: applied to the saved links
/// (<see cref="FormFillingReportCreator.AddLink"/>, <see cref="FormFillingReportCreator.RetargetLinks"/>) and, before it is
/// applied, already shown in the spreadsheet report (<see cref="FormFillingReportCreator.WithDecidedLinks"/>).
/// Pure logic, no portal.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormVersionLinksTests
{
    [Fact]
    public void WithDecidedLinks_NothingDecided_ReturnsSameState()
    {
        var formFilling = new FormFillingProperties { PendingVersionLinks = [Link(2, 1, Rename("a", "b"))] };

        FormFillingReportCreator.WithDecidedLinks(formFilling).Should().BeSameAs(formFilling);
        FormFillingReportCreator.WithDecidedLinks(null).Should().BeNull();
    }

    [Fact]
    public void WithDecidedLinks_ConfirmedVersion_JoinsTableOfPreviousVersion()
    {
        var formFilling = new FormFillingProperties { PendingVersionLinks = [Confirmed(Link(2, 1, Rename("a", "b")))] };

        var result = FormFillingReportCreator.WithDecidedLinks(formFilling);

        FormFillingReportCreator.GetTableVersion(result, 2).Should().Be(1);
        formFilling.VersionLinks.Should().BeNull();
    }

    [Fact]
    public void WithDecidedLinks_PreviousVersionLinked_JoinsTheFirstTable()
    {
        var formFilling = new FormFillingProperties
        {
            VersionLinks = [Link(2, 1)],
            PendingVersionLinks = [Confirmed(Link(3, 2, Rename("b", "c")))]
        };

        var result = FormFillingReportCreator.WithDecidedLinks(formFilling);

        FormFillingReportCreator.GetTableVersions(result, 1).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void WithDecidedLinks_LaterVersionContinuedDecidedOne_MovesWithIt()
    {
        var formFilling = new FormFillingProperties
        {
            VersionLinks = [Link(3, 2)],
            PendingVersionLinks = [Confirmed(Link(2, 1, Rename("a", "b")))]
        };

        var result = FormFillingReportCreator.WithDecidedLinks(formFilling);

        FormFillingReportCreator.GetTableVersions(result, 1).Should().Equal(1, 3, 2);
        formFilling.VersionLinks.Single().TableVersion.Should().Be(2, "the saved links are not changed");
    }

    [Fact]
    public void WithDecidedLinks_OpenQuestion_IsNotShownAsLinked()
    {
        var formFilling = new FormFillingProperties
        {
            PendingVersionLinks = [Link(2, 1, Rename("a", "b")), Confirmed(Link(3, 2, Rename("b", "c")))]
        };

        var result = FormFillingReportCreator.WithDecidedLinks(formFilling);

        FormFillingReportCreator.GetTableVersion(result, 2).Should().Be(2);
        FormFillingReportCreator.GetTableVersion(result, 3).Should().Be(2);
    }

    [Fact]
    public void AddLink_KeepsKeysAndDropsFieldDescriptions()
    {
        var state = new FormFillingProperties();
        var rename = new FormFieldRename { OldKey = "a", NewKey = "b", Type = "comboBox", Options = ["x"], Tip = "Tip" };

        FormFillingReportCreator.AddLink(state, Link(2, 1, rename));

        var saved = state.VersionLinks.Single().Renames.Single();
        (saved.OldKey, saved.NewKey).Should().Be(("a", "b"));
        saved.Type.Should().BeNull();
        saved.Options.Should().BeNull();
        saved.Tip.Should().BeNull();
    }

    [Fact]
    public void AddLink_SameVersion_ReplacesLink()
    {
        var state = new FormFillingProperties { VersionLinks = [Link(2, 1), Link(3, 1)] };

        FormFillingReportCreator.AddLink(state, Link(2, 5));

        state.VersionLinks.Should().HaveCount(2);
        FormFillingReportCreator.GetTableVersion(state, 2).Should().Be(5);
        FormFillingReportCreator.GetTableVersion(state, 3).Should().Be(1);
    }

    [Fact]
    public void RetargetLinks_MovesLinkAndTheVersionsContinuingIt()
    {
        var state = new FormFillingProperties();
        var link = Confirmed(Link(3, 2, Rename("b", "c")));
        var continuing = Link(4, 3);

        FormFillingReportCreator.RetargetLinks(state, link, [continuing], 1);

        link.TableVersion.Should().Be(1);
        link.Confirmed.Should().BeNull();
        continuing.TableVersion.Should().Be(1);
        state.VersionLinks.Should().ContainSingle().Which.Should().BeSameAs(link);
    }

    private static FormFieldRename Rename(string oldKey, string newKey) => new() { OldKey = oldKey, NewKey = newKey };

    private static FormVersionLink Link(int version, int tableVersion, params FormFieldRename[] renames) =>
        new() { Version = version, TableVersion = tableVersion, Renames = [.. renames] };

    private static FormVersionLink Confirmed(FormVersionLink link)
    {
        link.Confirmed = true;
        return link;
    }
}
