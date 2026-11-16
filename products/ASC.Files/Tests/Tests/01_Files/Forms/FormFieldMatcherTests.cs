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
/// <see cref="FormFieldMatcher.MatchSameLayout"/> and <see cref="FormFieldMatcher.CountShared"/> — how two form versions
/// are compared without the editor's reports: by place when only keys changed, and by the column names they share.
/// Pure logic, no portal: builds the metadata lists in memory.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormFieldMatcherTests
{
    private static FormMetadata Field(string key, string type = "", List<string>? values = null) =>
        new() { Key = key, Type = type, PossibleValues = values };

    private static (string OldKey, string NewKey)[] Pairs(List<FormFieldRename> renames) =>
        renames.Select(r => (r.OldKey, r.NewKey)).ToArray();

    [Fact]
    public void MatchSameLayout_SameFields_ReturnsNoRenames()
    {
        FormFieldMatcher.MatchSameLayout([Field("name"), Field("email")], [Field("name"), Field("email")]).Should().BeEmpty();
    }

    [Fact]
    public void MatchSameLayout_KeysChangedInPlace_PairsByPosition()
    {
        var result = FormFieldMatcher.MatchSameLayout([Field("first"), Field("email"), Field("last")], [Field("firstName"), Field("email"), Field("lastName")]);

        Pairs(result!).Should().Equal(("first", "firstName"), ("last", "lastName"));
    }

    [Fact]
    public void MatchSameLayout_ListRenamedWithSameOptions_IsARename()
    {
        var result = FormFieldMatcher.MatchSameLayout([Field("choice", "comboBox", ["yes", "no"])], [Field("decision", "comboBox", ["yes", "no"])]);

        Pairs(result!).Should().Equal(("choice", "decision"));
    }

    [Fact]
    public void MatchSameLayout_FieldAddedOrRemoved_IsNotTheSameLayout()
    {
        FormFieldMatcher.MatchSameLayout([Field("name")], [Field("name"), Field("email")]).Should().BeNull();
        FormFieldMatcher.MatchSameLayout([Field("name"), Field("email")], [Field("name")]).Should().BeNull();
    }

    [Fact]
    public void MatchSameLayout_TypeChangedInPlace_IsNotTheSameLayout()
    {
        FormFieldMatcher.MatchSameLayout([Field("status")], [Field("state", "comboBox", ["open", "closed"])]).Should().BeNull();
    }

    [Fact]
    public void MatchSameLayout_ListOptionsChanged_IsNotTheSameLayout()
    {
        FormFieldMatcher.MatchSameLayout([Field("color", "comboBox", ["red"])], [Field("colour", "comboBox", ["red", "blue"])]).Should().BeNull();
    }

    // A key that moved to another place means the fields may have been reordered, so no pairing by place is safe.
    [Fact]
    public void MatchSameLayout_KeyMovedToAnotherPlace_IsNotTheSameLayout()
    {
        FormFieldMatcher.MatchSameLayout([Field("a"), Field("b")], [Field("b"), Field("c")]).Should().BeNull();
    }

    [Fact]
    public void MatchSameLayout_DuplicateKeys_CountAsOneField()
    {
        FormFieldMatcher.MatchSameLayout([Field("fullName"), Field("fullName")], [Field("fullName")]).Should().BeEmpty();
    }

    [Fact]
    public void MatchSameLayout_EmptyVersion_IsNotTheSameLayout()
    {
        FormFieldMatcher.MatchSameLayout([], [Field("name")]).Should().BeNull();
        FormFieldMatcher.MatchSameLayout([Field("name")], []).Should().BeNull();
    }

    [Fact]
    public void CountShared_CountsFieldsByColumnName()
    {
        FormFieldMatcher.CountShared([Field("name"), Field("email")], [Field("email"), Field("phone")]).Should().Be(1);
        FormFieldMatcher.CountShared([Field("name")], [Field("phone")]).Should().Be(0);
    }

    [Fact]
    public void CountShared_SameKeyOtherType_CannotShareATable()
    {
        FormFieldMatcher.CountShared([Field("status")], [Field("status", "comboBox", ["open", "closed"])]).Should().BeNull();
    }

    // The options of a list may differ: its column takes them all.
    [Fact]
    public void CountShared_ListOptionsChanged_StillShares()
    {
        FormFieldMatcher.CountShared([Field("color", "comboBox", ["red"])], [Field("color", "comboBox", ["red", "blue"])]).Should().Be(1);
    }
}
