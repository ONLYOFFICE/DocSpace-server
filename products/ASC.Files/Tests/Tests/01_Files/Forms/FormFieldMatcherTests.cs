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

namespace ASC.Files.Tests.Tests._01_Files.Forms;

/// <summary>
/// <see cref="FormFieldMatcher.Match"/> — compares two form versions and decides which fields were
/// renamed, so earlier answers can stay in one table and report sheet instead of starting a new one.
/// Pure logic, no portal: builds the metadata lists in memory and checks the pairing directly.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormFieldMatcherTests
{
    private static FormMetadata Field(string key, string type = "", List<string> values = null) =>
        new() { Key = key, Type = type, PossibleValues = values };

    private static (string OldKey, string NewKey)[] Pairs(List<FormFieldRename> renames) =>
        renames.Select(r => (r.OldKey, r.NewKey)).ToArray();

    [Fact]
    public void Match_SameFields_ReturnsNoRenames()
    {
        var previous = new[] { Field("name"), Field("email") };
        var current = new[] { Field("name"), Field("email") };

        var result = FormFieldMatcher.Match(previous, current);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Match_OneKeyRenamedSameType_ReturnsThatRename()
    {
        var previous = new[] { Field("name"), Field("email") };
        var current = new[] { Field("fullName"), Field("email") };

        var result = FormFieldMatcher.Match(previous, current);

        Pairs(result).Should().Equal(("name", "fullName"));
    }

    [Fact]
    public void Match_TwoKeysRenamedInOrder_ReturnsBothRenames()
    {
        // An unchanged field in the middle anchors the comparison; the gaps around it pair up.
        var previous = new[] { Field("first"), Field("email"), Field("last") };
        var current = new[] { Field("firstName"), Field("email"), Field("lastName") };

        var result = FormFieldMatcher.Match(previous, current);

        Pairs(result).Should().Equal(("first", "firstName"), ("last", "lastName"));
    }

    [Fact]
    public void Match_AllKeysRenamedSameTypesAndCount_PairsByPosition()
    {
        var previous = new[] { Field("a"), Field("b") };
        var current = new[] { Field("x"), Field("y") };

        var result = FormFieldMatcher.Match(previous, current);

        Pairs(result).Should().Equal(("a", "x"), ("b", "y"));
    }

    [Fact]
    public void Match_FieldAdded_IsNotARename()
    {
        var previous = new[] { Field("name") };
        var current = new[] { Field("name"), Field("email") };

        var result = FormFieldMatcher.Match(previous, current);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Match_FieldRemoved_IsNotARename()
    {
        var previous = new[] { Field("name"), Field("email") };
        var current = new[] { Field("name") };

        var result = FormFieldMatcher.Match(previous, current);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Match_EnumRenamedSameOptions_ReturnsRename()
    {
        var previous = new[] { Field("choice", "comboBox", ["yes", "no"]) };
        var current = new[] { Field("decision", "comboBox", ["yes", "no"]) };

        var result = FormFieldMatcher.Match(previous, current);

        Pairs(result).Should().Equal(("choice", "decision"));
    }

    [Fact]
    public void Match_SameKeyDifferentType_NeedsOwnTable()
    {
        // A field kept its key but changed type: the old answers no longer fit, so a new table is needed.
        var previous = new[] { Field("status") };
        var current = new[] { Field("status", "comboBox", ["open", "closed"]) };

        var result = FormFieldMatcher.Match(previous, current);

        result.Should().BeNull();
    }

    [Fact]
    public void Match_SameKeyEnumOptionsChanged_NeedsOwnTable()
    {
        var previous = new[] { Field("color", "comboBox", ["red"]) };
        var current = new[] { Field("color", "comboBox", ["red", "blue"]) };

        var result = FormFieldMatcher.Match(previous, current);

        result.Should().BeNull();
    }

    [Fact]
    public void Match_AmbiguousPairing_NeedsOwnTable()
    {
        // Two removed fields, one added, all the same type and no anchor: which one became the new
        // field is a guess, so the match refuses and a new table is used.
        var previous = new[] { Field("a"), Field("b") };
        var current = new[] { Field("c") };

        var result = FormFieldMatcher.Match(previous, current);

        result.Should().BeNull();
    }

    [Fact]
    public void Match_DuplicateKeys_CountAsOneField()
    {
        // Two fields given the same key share one column; the duplicate must not look like a rename.
        var previous = new[] { Field("fullName"), Field("fullName") };
        var current = new[] { Field("fullName") };

        var result = FormFieldMatcher.Match(previous, current);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Match_EmptyPreviousVersion_NeedsOwnTable()
    {
        var result = FormFieldMatcher.Match([], [Field("name")]);

        result.Should().BeNull();
    }

    [Fact]
    public void Match_EmptyCurrentVersion_NeedsOwnTable()
    {
        var result = FormFieldMatcher.Match([Field("name")], []);

        result.Should().BeNull();
    }
}
