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
/// The field renames the editor reports per session (<see cref="FormFillingReportCreator.KeepEditorRenames"/>) and how
/// the sessions saved as one version are joined (<see cref="FormFillingReportCreator.MatchFields"/>): a session one user
/// opens on top of another's unsaved renames must not wipe them out. Pure logic, no portal.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Forms")]
public class FormEditorSessionsTests
{
    private static readonly Guid _user = Guid.NewGuid();

    [Fact]
    public void KeepEditorRenames_TwoSessionsOfOneUser_KeepTheirOwnRenames()
    {
        var state = new FormFillingProperties();

        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, "first", [Rename("fromWhom", "applicant")]).Should().BeTrue();
        FormFillingReportCreator.KeepEditorRenames(state, 1, 200, _user, "second", [Rename("email", "mail")]).Should().BeTrue();

        state.EditorFieldRenames.Select(s => (s.SessionId, s.Renames.Single().NewKey)).Should()
            .Equal(("first", "applicant"), ("second", "mail"));
    }

    [Fact]
    public void KeepEditorRenames_SameSession_LaterReportReplacesAndUndoes()
    {
        var state = new FormFillingProperties();

        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, "tab", [Rename("fromWhom", "applicant")]);
        FormFillingReportCreator.KeepEditorRenames(state, 1, 200, _user, "tab", []).Should().BeTrue();

        state.EditorFieldRenames.Should().ContainSingle().Which.Renames.Should().BeEmpty();
    }

    [Fact]
    public void KeepEditorRenames_OlderReportOfSession_IsDropped()
    {
        var state = new FormFillingProperties();

        FormFillingReportCreator.KeepEditorRenames(state, 1, 200, _user, "tab", [Rename("fromWhom", "applicant")]);
        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, "tab", [Rename("fromWhom", "wrong")]).Should().BeFalse();

        state.EditorFieldRenames.Single().Renames.Single().NewKey.Should().Be("applicant");
    }

    [Fact]
    public void KeepEditorRenames_ReportOfAnotherSession_IsNotComparedByNumber()
    {
        // Another device of the user has its clock behind: its first report has a lower number and is still taken.
        var state = new FormFillingProperties();

        FormFillingReportCreator.KeepEditorRenames(state, 1, 200, _user, "laptop", [Rename("fromWhom", "applicant")]);
        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, "phone", [Rename("email", "mail")]).Should().BeTrue();

        state.EditorFieldRenames.Should().HaveCount(2);
    }

    [Fact]
    public void KeepEditorRenames_WithoutSessionId_ReplacesAsBefore()
    {
        var state = new FormFillingProperties();

        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, null, [Rename("fromWhom", "applicant")]);
        FormFillingReportCreator.KeepEditorRenames(state, 1, 200, _user, null, [Rename("email", "mail")]);

        state.EditorFieldRenames.Should().ContainSingle().Which.Renames.Single().NewKey.Should().Be("mail");
    }

    [Fact]
    public void KeepEditorRenames_EmptySessionId_IsTakenForNone()
    {
        var state = new FormFillingProperties();

        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, "", [Rename("fromWhom", "applicant")]);
        FormFillingReportCreator.KeepEditorRenames(state, 1, 200, _user, null, [Rename("email", "mail")]);

        state.EditorFieldRenames.Should().ContainSingle().Which.SessionId.Should().BeNull();
    }

    [Fact]
    public void KeepEditorRenames_TooManySessions_IgnoresTheNextOne()
    {
        var state = new FormFillingProperties();
        for (var i = 0; i < FormFillingReportCreator.MaxEditorSessionsPerUser; i++)
        {
            FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, $"s{i}", []);
        }

        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, "one-more", []).Should().BeFalse();
        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, Guid.NewGuid(), "another-user", []).Should().BeTrue();
        FormFillingReportCreator.KeepEditorRenames(state, 1, 200, _user, "s0", [Rename("a", "b")]).Should().BeTrue();
    }

    [Fact]
    public void MatchFields_TwoSessionsSavedAsOneVersion_KeepBothRenames()
    {
        var formFilling = new FormFillingProperties { EditorFieldRenames = [Saved("first", Rename("fromWhom", "applicant")), Saved("second", Rename("email", "mail"))] };

        var renames = FormFillingReportCreator.MatchFields(2, 1, Previous("fromWhom", "email"), Fields("applicant", "mail"), formFilling);

        renames.Select(r => (r.OldKey, r.NewKey)).Should().Equal(("fromWhom", "applicant"), ("email", "mail"));
    }

    [Fact]
    public void MatchFields_SecondSessionRenamesTheFirstOnesKey_JoinsThem()
    {
        var formFilling = new FormFillingProperties
        {
            EditorFieldRenames = [Saved("first", Rename("fromWhom", "applicant")), Saved("second", Rename("applicant", "applicantName"))]
        };

        var renames = FormFillingReportCreator.MatchFields(2, 1, Previous("fromWhom", "email"), Fields("applicantName", "email"), formFilling);

        renames.Select(r => (r.OldKey, r.NewKey)).Should().Equal(("fromWhom", "applicantName"));
    }

    // v2 swapped the keys and took a table of its own without answers; v3 is then compared with v1 and must see the swap.
    [Fact]
    public void ApplyVersionDecision_NoLinkThenRenamed_NextVersionSeesTheSwap()
    {
        var formFilling = new FormFillingProperties { EditorFieldRenames = [Session(1, 2, Rename("first", "second"), Rename("second", "first"))] };

        FormFillingReportCreator.ApplyVersionDecision(formFilling, 2, new FormVersionLink(), 1);
        formFilling.EditorFieldRenames.Add(Session(2, 3, Rename("first", "third")));

        FormFillingReportCreator.MatchFields(3, 1, Previous("first", "second"), Fields("second", "third"), formFilling).Should().BeNull();
    }

    // Without a link only what the next comparison can still read stays: sessions after the compared version.
    [Fact]
    public void ApplyVersionDecision_NoLink_DropsSessionsUpToTheComparedVersion()
    {
        var formFilling = new FormFillingProperties
        {
            EditorFieldRenames = [Session(1, 2, Rename("a", "b")), Unsaved(1), Session(2, 3, Rename("c", "d")), Unsaved(3)]
        };

        FormFillingReportCreator.ApplyVersionDecision(formFilling, 3, new FormVersionLink(), 2);

        formFilling.EditorFieldRenames.Select(s => (s.OpenedVersion, s.SavedVersion)).Should().Equal((2, 3), (3, null));
        formFilling.PendingVersionLinks.Should().BeEmpty();
    }

    [Fact]
    public void ApplyVersionDecision_Link_DropsSessionsUpToTheVersionAndKeepsTheLink()
    {
        var formFilling = new FormFillingProperties { EditorFieldRenames = [Session(2, 3, Rename("c", "d")), Unsaved(3)] };
        var link = new FormVersionLink { Version = 3, TableVersion = 1, Renames = [Rename("c", "d")] };

        FormFillingReportCreator.ApplyVersionDecision(formFilling, 3, link, 2);

        formFilling.EditorFieldRenames.Select(s => s.OpenedVersion).Should().Equal(3);
        formFilling.PendingVersionLinks.Should().ContainSingle().Which.Should().BeSameAs(link);
    }

    // name was renamed to fullName; a new field later took the key name and was renamed itself: v1 answers stay in fullName.
    [Fact]
    public void MatchFields_FreedKeyTakenByNewFieldAndRenamed_KeepsTheEarlierRename()
    {
        var formFilling = new FormFillingProperties
        {
            PendingVersionLinks = [new FormVersionLink { Version = 2, TableVersion = 1, Renames = [Rename("name", "fullName")] }],
            EditorFieldRenames = [Session(2, 3, Rename("name", "nickname"))]
        };

        var renames = FormFillingReportCreator.MatchFields(3, 1, Previous("name"), Fields("fullName", "nickname"), formFilling);

        renames.Select(r => (r.OldKey, r.NewKey)).Should().Equal(("name", "fullName"));
    }

    // Two co-editors opened v1; the second renamed the field the first had renamed, and both report from v1's keys.
    [Fact]
    public void MatchFields_CoEditorsRenameOneFieldFromTheOpenedVersion_TakesTheLastName()
    {
        var formFilling = new FormFillingProperties
        {
            EditorFieldRenames = [Saved("first", Rename("fromWhom", "applicant")), Saved("second", Rename("fromWhom", "applicantName"))]
        };

        var renames = FormFillingReportCreator.MatchFields(2, 1, Previous("fromWhom", "email"), Fields("applicantName", "email"), formFilling);

        renames.Select(r => (r.OldKey, r.NewKey)).Should().Equal(("fromWhom", "applicantName"));
    }

    // A was vacated in v2 and its field renamed further in v3; a new field that took A at v2 and was renamed is not A's.
    [Fact]
    public void MatchFields_KeyVacatedThenRenamedFurther_SkipsTheNewFieldsRename()
    {
        var formFilling = new FormFillingProperties
        {
            PendingVersionLinks =
            [
                new FormVersionLink { Version = 2, TableVersion = 1, Renames = [Rename("a", "b")] },
                new FormVersionLink { Version = 3, TableVersion = 1, Renames = [Rename("b", "c")] }
            ],
            EditorFieldRenames = [Session(2, 4, Rename("a", "x"))]
        };

        var renames = FormFillingReportCreator.MatchFields(4, 1, Previous("a"), Fields("c", "x"), formFilling);

        renames.Select(r => (r.OldKey, r.NewKey)).Should().Equal(("a", "c"));
    }

    // A co-editor reported a->b and closed; another renamed the field back before the save, so v2 still has a.
    [Fact]
    public void MatchFields_SessionReportOlderThanTheSave_DoesNotVacateTheKey()
    {
        var formFilling = new FormFillingProperties { EditorFieldRenames = [Session(1, 2, Rename("a", "b")), Session(2, 3, Rename("a", "x"))] };

        var renames = FormFillingReportCreator.MatchFields(3, 1, Previous("a"), Fields("x"), formFilling);

        renames.Select(r => (r.OldKey, r.NewKey)).Should().Equal(("a", "x"));
    }

    // v2 added a field and is decided but never filled: its field is not a column yet, so v3 may rename into it.
    [Fact]
    public void GetTableColumns_DecidedVersionWithoutSubmissions_AddsNoColumns()
    {
        var formFilling = new FormFillingProperties { PendingVersionLinks = [new FormVersionLink { Version = 2, TableVersion = 1 }] };
        var tableColumns = FormFillingReportCreator.GetTableColumns(formFilling, 1, [Previous(1, "name"), Previous(2, "name", "extra")], 3);

        tableColumns.Should().ContainSingle();
        FormFillingReportCreator.MatchFields(3, 1, Previous(1, "name"), Fields("extra"), formFilling, tableColumns)
            .Select(r => (r.OldKey, r.NewKey)).Should().Equal(("name", "extra"));
    }

    // v2 removed the field old; its column with v1 answers stays in the table, so no field may be renamed into it.
    [Fact]
    public void MatchFields_RenamedToColumnOfRemovedField_TakesTableOfItsOwn()
    {
        var formFilling = new FormFillingProperties
        {
            VersionLinks = [new FormVersionLink { Version = 2, TableVersion = 1 }],
            EditorFieldRenames = [Session(2, 3, Rename("name", "old"))]
        };
        var tableColumns = FormFillingReportCreator.GetTableColumns(formFilling, 1, [Previous(1, "name", "old"), Previous(2, "name")], 3);

        FormFillingReportCreator.MatchFields(3, 1, Previous(2, "name"), Fields("old"), formFilling, tableColumns).Should().BeNull();
    }

    [Fact]
    public void MatchFields_SameLayoutKeyOfRemovedField_TakesTableOfItsOwn()
    {
        var formFilling = new FormFillingProperties { VersionLinks = [new FormVersionLink { Version = 2, TableVersion = 1 }] };
        var tableColumns = FormFillingReportCreator.GetTableColumns(formFilling, 1, [Previous(1, "name", "old"), Previous(2, "name")], 3);

        tableColumns.Should().HaveCount(2);
        FormFillingReportCreator.MatchFields(3, 1, Previous(2, "name"), Fields("old"), formFilling, tableColumns).Should().BeNull();
        FormFillingReportCreator.MatchFields(3, 1, Previous(2, "name"), Fields("fullName"), formFilling, tableColumns)
            .Select(r => (r.OldKey, r.NewKey)).Should().Equal(("name", "fullName"));
    }

    // v2 is checked again after it was overwritten: its stale fields and its earlier decision are not columns of the table.
    [Fact]
    public void MatchFields_VersionCheckedAgain_IgnoresItsOwnStaleColumns()
    {
        var formFilling = new FormFillingProperties { PendingVersionLinks = [new FormVersionLink { Version = 2, TableVersion = 1 }] };
        var tableColumns = FormFillingReportCreator.GetTableColumns(formFilling, 1, [Previous(1, "name", "email"), Previous(2, "name", "email", "extra")], 2);

        tableColumns.Should().HaveCount(2);
        FormFillingReportCreator.MatchFields(2, 1, Previous(1, "name", "email"), Fields("extra", "email"), formFilling, tableColumns)
            .Select(r => (r.OldKey, r.NewKey)).Should().Equal(("name", "extra"));
    }

    private static FormFieldRename Rename(string oldKey, string newKey) => new() { OldKey = oldKey, NewKey = newKey };

    private static FormEditorFieldRenames Session(int opened, int saved, params FormFieldRename[] renames) =>
        new() { OpenedVersion = opened, UserId = _user, SessionId = $"{opened}-{saved}", SavedVersion = saved, Sequence = 1, Renames = [.. renames] };

    private static FormEditorFieldRenames Unsaved(int opened) =>
        new() { OpenedVersion = opened, UserId = _user, SessionId = $"unsaved-{opened}", Sequence = 1, Renames = [] };

    private static DbFormsMetadataSearch Previous(int version, params string[] keys) => new() { OriginalFormVersion = version, Metadata = Fields(keys) };

    private static FormEditorFieldRenames Saved(string sessionId, params FormFieldRename[] renames) =>
        new() { OpenedVersion = 1, UserId = _user, SessionId = sessionId, SavedVersion = 2, Sequence = 1, Renames = [.. renames] };

    private static List<FormMetadata> Fields(params string[] keys) => [.. keys.Select(k => new FormMetadata { Key = k, Type = "text" })];

    private static DbFormsMetadataSearch Previous(params string[] keys) => new() { OriginalFormVersion = 1, Metadata = Fields(keys) };
}
