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

    // The editor repeats its last report or one arrives late after the save; a forcesave saves the version mid-session.
    [Fact]
    public void KeepEditorRenames_ReportAfterSave_DropsOldAndContinuesNewer()
    {
        var state = new FormFillingProperties();
        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, "s1", [Rename("a", "b")]).Should().BeTrue();
        FormFillingReportCreator.MarkEditorRenamesSaved(state, 2).Should().BeTrue();

        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, "s1", [Rename("a", "b")]).Should().BeFalse();
        FormFillingReportCreator.KeepEditorRenames(state, 1, 90, _user, "s1", [Rename("a", "x")]).Should().BeFalse();
        state.EditorFieldRenames.Should().ContainSingle();

        FormFillingReportCreator.KeepEditorRenames(state, 1, 200, _user, "s1", [Rename("a", "b"), Rename("c", "d")]).Should().BeTrue();
        state.EditorFieldRenames.Select(s => (s.SavedVersion, s.Renames.Count)).Should().Equal(((int?)2, 1), ((int?)null, 2));
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

    // The first renamed a to b without reporting yet; a co-editor who opened the form after it saw b, renamed x to a and
    // reported first. Joined in the list order, x would continue into b and the answers of a and x would share columns.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MatchFields_KeyOfPreviousVersionPassedBetweenSessions_TakesTableOfItsOwn(bool takerReportedFirst)
    {
        FormEditorFieldRenames giver = Saved("first", Rename("a", "b")), taker = Saved("second", Rename("x", "a"));
        var formFilling = new FormFillingProperties { EditorFieldRenames = takerReportedFirst ? [taker, giver] : [giver, taker] };

        FormFillingReportCreator.MatchFields(2, 1, Previous("a", "x"), Fields("b", "a"), formFilling).Should().BeNull();
    }

    // The second renamed the first one's new key but reported before it: the field loses its link, not its column.
    [Fact]
    public void MatchFields_NewKeyRenamedBeforeItsTakerReported_KeepsTheColumnsApart()
    {
        var formFilling = new FormFillingProperties
        {
            EditorFieldRenames = [Saved("second", Rename("applicant", "applicantName")), Saved("first", Rename("fromWhom", "applicant"))]
        };

        FormFillingReportCreator.MatchFields(2, 1, Previous("fromWhom", "email"), Fields("applicantName", "email"), formFilling).Should().BeEmpty();
    }

    // v2 was saved without answers, so v3 is compared with v1 while its sessions report from v2's keys.
    [Fact]
    public void MatchFields_SessionsOpenedOnLaterVersionPassAKey_TakesTableOfItsOwn()
    {
        var formFilling = new FormFillingProperties { EditorFieldRenames = [Session(2, 3, Rename("a", "b")), Session(2, 3, Rename("b", "c"))] };
        formFilling.EditorFieldRenames[1].SessionId = "other";

        FormFillingReportCreator.MatchFields(3, 1, Previous("a"), Fields("c"), formFilling).Should().BeNull();
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

        formFilling.EditorFieldRenames.Where(s => !FormFillingReportCreator.IsConsumedTrace(s))
            .Select(s => (s.OpenedVersion, s.SavedVersion)).Should().Equal((2, 3), (3, null));
        formFilling.EditorFieldRenames.Where(FormFillingReportCreator.IsConsumedTrace).Select(s => s.SessionId).Should().Equal("1-2");
        formFilling.PendingVersionLinks.Should().BeEmpty();
    }

    [Fact]
    public void ApplyVersionDecision_Link_DropsSessionsUpToTheVersionAndKeepsTheLink()
    {
        var formFilling = new FormFillingProperties { EditorFieldRenames = [Session(2, 3, Rename("c", "d")), Unsaved(3)] };
        var link = new FormVersionLink { Version = 3, TableVersion = 1, Renames = [Rename("c", "d")] };

        FormFillingReportCreator.ApplyVersionDecision(formFilling, 3, link, 2);

        formFilling.EditorFieldRenames.Where(s => !FormFillingReportCreator.IsConsumedTrace(s)).Select(s => s.OpenedVersion).Should().Equal(3);
        formFilling.EditorFieldRenames.Where(FormFillingReportCreator.IsConsumedTrace).Select(s => s.SessionId).Should().Equal("2-3");
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
    public void MatchFields_SessionRenamesKeyAnEarlierSessionVacated_TakesTableOfItsOwn()
    {
        // The second session renames a: the old field, if the first one's last report was stale, or a new field that took
        // its key. The reports cannot tell which, so its answers must not continue the old a's.
        var formFilling = new FormFillingProperties { EditorFieldRenames = [Session(1, 2, Rename("a", "b")), Session(2, 3, Rename("a", "x"))] };

        FormFillingReportCreator.MatchFields(3, 1, Previous("a"), Fields("x"), formFilling).Should().BeNull();
    }

    // The scenario of the review: a renamed to b and a new field a added in v2, the new a renamed to c in v3; v3 is
    // compared with v1, and the answers to the old a must not show under c.
    [Fact]
    public void MatchFields_RenamedThenKeyTakenAndRenamedAgain_TakesTableOfItsOwn()
    {
        var formFilling = new FormFillingProperties { EditorFieldRenames = [Session(1, 2, Rename("a", "b")), Session(2, 3, Rename("a", "c"))] };

        FormFillingReportCreator.MatchFields(3, 1, Previous("a"), Fields("b", "c"), formFilling).Should().BeNull();
    }

    // A field renamed on by a later session is the same field: its chain continues.
    [Fact]
    public void MatchFields_LaterSessionRenamesTheRenamedField_ContinuesTheChain()
    {
        var formFilling = new FormFillingProperties { EditorFieldRenames = [Session(1, 2, Rename("a", "b")), Session(2, 3, Rename("b", "c"))] };

        FormFillingReportCreator.MatchFields(3, 1, Previous("a"), Fields("c"), formFilling)
            .Select(r => (r.OldKey, r.NewKey)).Should().Equal(("a", "c"));
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
        var tableColumns = FormFillingReportCreator.GetTableColumns(formFilling, 1, [Previous(1, "name", "old", "email"), Previous(2, "name", "email")], 3);

        tableColumns.Should().HaveCount(3);
        FormFillingReportCreator.MatchFields(3, 1, Previous(2, "name", "email"), Fields("old", "email"), formFilling, tableColumns).Should().BeNull();
        FormFillingReportCreator.MatchFields(3, 1, Previous(2, "name", "email"), Fields("fullName", "email"), formFilling, tableColumns)
            .Select(r => (r.OldKey, r.NewKey)).Should().Equal(("name", "fullName"));
    }

    // Without the editor's reports and without one field that kept its key, two forms of as many text fields would be
    // taken for one form renamed, and the answers of another form would join the same columns.
    [Fact]
    public void MatchFields_SameLayoutWithoutAnUnchangedKey_TakesTableOfItsOwn()
    {
        var formFilling = new FormFillingProperties();

        FormFillingReportCreator.MatchFields(2, 1, Previous("name", "email"), Fields("product", "price"), formFilling).Should().BeNull();
        FormFillingReportCreator.MatchFields(2, 1, Previous("name", "email"), Fields("fullName", "email"), formFilling)
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

    // v2 removed a, v3 adds a new field under the same key: its answers would land next to the old a's.
    [Fact]
    public void MatchFields_NewFieldTakesKeyOfRemovedField_TakesTableOfItsOwn()
    {
        var formFilling = new FormFillingProperties { VersionLinks = [new FormVersionLink { Version = 2, TableVersion = 1 }] };
        var tableColumns = FormFillingReportCreator.GetTableColumns(formFilling, 1, [Previous(1, "a", "c"), Previous(2, "c")], 3);

        FormFillingReportCreator.MatchFields(3, 1, Previous(2, "c"), Fields("c", "a"), formFilling, tableColumns).Should().BeNull();
        FormFillingReportCreator.MatchFields(3, 1, Previous(2, "c"), Fields("c", "b"), formFilling, tableColumns).Should().BeEmpty();
    }

    [Fact]
    public void KeepEditorRenames_VersionAlreadyCompared_IsDropped()
    {
        var state = new FormFillingProperties { FieldsCheckedVersion = 3, LastSubmittedVersion = 3 };

        FormFillingReportCreator.KeepEditorRenames(state, 2, 100, _user, "tab", [Rename("fromWhom", "applicant")]).Should().BeFalse();
        FormFillingReportCreator.KeepEditorRenames(state, 3, 100, _user, "tab", [Rename("fromWhom", "applicant")]).Should().BeTrue();

        state.EditorFieldRenames.Should().ContainSingle().Which.OpenedVersion.Should().Be(3);
    }

    // An editor opens v3, which has responses, a forcesave writes v4 and its check takes the saved part of the session;
    // the editor goes on renaming. v4 has no data, so v5 is compared with v3 and needs every rename since v3.
    [Fact]
    public void KeepEditorRenames_SessionGoesOnAfterAForcesaveIsChecked_IsKeptForTheNextSave()
    {
        var state = new FormFillingProperties { LastSubmittedVersion = 3 };
        FormFillingReportCreator.KeepEditorRenames(state, 3, 100, _user, "tab", [Rename("a", "b")]).Should().BeTrue();
        FormFillingReportCreator.MarkEditorRenamesSaved(state, 4).Should().BeTrue();
        FormFillingReportCreator.ConsumeEditorRenames(state, 4);
        state.FieldsCheckedVersion = 4;

        FormFillingReportCreator.KeepEditorRenames(state, 3, 200, _user, "tab", [Rename("a", "b"), Rename("c", "d")]).Should().BeTrue();
        FormFillingReportCreator.MarkEditorRenamesSaved(state, 5).Should().BeTrue();

        var session = state.EditorFieldRenames.Where(s => !FormFillingReportCreator.IsConsumedTrace(s)).Should().ContainSingle().Subject;
        (session.OpenedVersion, session.SavedVersion).Should().Be((3, (int?)5));
        session.Renames.Select(r => (r.OldKey, r.NewKey)).Should().Equal(("a", "b"), ("c", "d"));
    }

    // A report sent before the forcesave but arriving after its check took the session: the trace of the session keeps
    // its last report number, so the late report is not taken for a continuation and cannot bring back an older rename.
    [Fact]
    public void KeepEditorRenames_LateReportAfterTheSessionWasTaken_IsDropped()
    {
        var state = new FormFillingProperties { LastSubmittedVersion = 3 };
        FormFillingReportCreator.KeepEditorRenames(state, 3, 150, _user, "tab", [Rename("a", "b")]).Should().BeTrue();
        FormFillingReportCreator.MarkEditorRenamesSaved(state, 4).Should().BeTrue();
        FormFillingReportCreator.ConsumeEditorRenames(state, 4);
        state.FieldsCheckedVersion = 4;

        FormFillingReportCreator.KeepEditorRenames(state, 3, 100, _user, "tab", [Rename("a", "x")]).Should().BeFalse();
        FormFillingReportCreator.KeepEditorRenames(state, 3, 150, _user, "tab", [Rename("a", "b")]).Should().BeFalse();

        state.EditorFieldRenames.Should().OnlyContain(s => FormFillingReportCreator.IsConsumedTrace(s));
    }

    // v4 was linked but got no responses, so v5 is compared with v3 and the trace of the session saved as v4 is newer
    // than v3. It must not count as a tracked session: that would turn off the match by the order of the fields.
    [Fact]
    public void MatchFields_TraceNewerThanTheComparedVersion_KeepsTheMatchByOrder()
    {
        var formFilling = new FormFillingProperties
        {
            EditorFieldRenames = [new FormEditorFieldRenames { OpenedVersion = 3, UserId = _user, SessionId = "tab", SavedVersion = 4, Sequence = 150 }]
        };

        FormFillingReportCreator.MatchFields(5, 1, Previous(3, "name", "email"), Fields("fullName", "email"), formFilling)
            .Select(r => (r.OldKey, r.NewKey)).Should().Equal(("name", "fullName"));
    }

    // Making room for a report takes saved sessions with renames; a trace holds none, so it stays and keeps its guard.
    [Fact]
    public void KeepEditorRenames_BudgetRunsOut_TracesStay()
    {
        var big = Enumerable.Range(0, 1500).Select(i => Rename($"old{i}", $"new{i}")).ToList();
        var state = new FormFillingProperties
        {
            EditorFieldRenames =
            [
                new FormEditorFieldRenames { OpenedVersion = 1, UserId = _user, SessionId = "trace", SavedVersion = 2, Sequence = 10 },
                new FormEditorFieldRenames { OpenedVersion = 2, UserId = _user, SessionId = "saved", SavedVersion = 3, Sequence = 20, Renames = big }
            ]
        };

        var report = Enumerable.Range(0, 600).Select(i => Rename($"a{i}", $"b{i}")).ToList();
        FormFillingReportCreator.KeepEditorRenames(state, 3, 30, _user, "new", report).Should().BeTrue();

        state.EditorFieldRenames.Select(s => s.SessionId).Should().BeEquivalentTo("trace", "new");
    }

    // A trace carries no renames into a comparison, and only the latest traces are kept.
    [Fact]
    public void ConsumeEditorRenames_ManySessions_KeepsTheLatestTracesOnly()
    {
        var state = new FormFillingProperties();
        for (var session = 0; session < FormFillingReportCreator.MaxConsumedEditorSessions + 5; session++)
        {
            FormFillingReportCreator.KeepEditorRenames(state, 1, 100 + session, Guid.NewGuid(), $"tab{session}", [Rename("a", "b")]).Should().BeTrue();
        }

        FormFillingReportCreator.MarkEditorRenamesSaved(state, 2);
        FormFillingReportCreator.ConsumeEditorRenames(state, 2);

        state.EditorFieldRenames.Should().HaveCount(FormFillingReportCreator.MaxConsumedEditorSessions)
            .And.OnlyContain(s => FormFillingReportCreator.IsConsumedTrace(s));
        state.EditorFieldRenames.Min(s => s.Sequence).Should().Be(105);
    }

    // The rule reads the versions below the current one only: whether responses to the forcesaved v4 gave it a link of
    // its own is not known before its next check, which then drops the session if it did.
    [Fact]
    public void KeepEditorRenames_ResponsesToTheCurrentVersion_DoNotMakeTheSessionOutdated()
    {
        var state = new FormFillingProperties { LastSubmittedVersion = 4, FieldsCheckedVersion = 4 };

        FormFillingReportCreator.KeepEditorRenames(state, 3, 200, _user, "tab", [Rename("c", "d")], formVersion: 4).Should().BeTrue();
    }

    // Once a later version follows the forcesaved one that got responses, it is compared with that one, so a session
    // opened before it can no longer count and is not kept.
    [Fact]
    public void KeepEditorRenames_LaterVersionFollowsAForcesaveWithResponses_SessionIsDropped()
    {
        var state = new FormFillingProperties { LastSubmittedVersion = 4, FieldsCheckedVersion = 5 };

        FormFillingReportCreator.KeepEditorRenames(state, 3, 200, _user, "tab", [Rename("c", "d")], formVersion: 5).Should().BeFalse();
        state.EditorFieldRenames.Should().BeNullOrEmpty();
    }

    // Reports of the largest allowed size from many sessions over many saved versions: the stored state keeps a fixed
    // budget, so the form's properties never outgrow the column they are kept in.
    [Fact]
    public void KeepEditorRenames_AdversarialReportsAcrossVersions_StayWithinBudget()
    {
        var state = new FormFillingProperties();
        var renames = Enumerable.Range(0, 500).Select(i => Rename($"{i}".PadLeft(255, 'o'), $"{i}".PadLeft(255, 'n'))).ToList();

        for (var version = 1; version <= 10; version++)
        {
            for (var session = 0; session < FormFillingReportCreator.MaxEditorSessionsPerUser; session++)
            {
                FormFillingReportCreator.KeepEditorRenames(state, version, 100, Guid.NewGuid(), $"s{session}", renames);
            }

            FormFillingReportCreator.MarkEditorRenamesSaved(state, version + 1);
        }

        state.EditorFieldRenames.Sum(s => s.Renames?.Count ?? 0).Should().BeLessThanOrEqualTo(FormFillingReportCreator.MaxEditorRenamesPerForm);
        state.EditorFieldRenames.Should().OnlyContain(s => s.Renames != null);
        JsonSerializer.Serialize(state).Length.Should().BeLessThan(FormFillingReportCreator.MaxEditorRenamesPerForm * 600);
    }

    // A report that cannot fit even after the saved sessions go leaves no empty session behind.
    [Fact]
    public void KeepEditorRenames_ReportPastTheBudget_IsDroppedWithoutASession()
    {
        var state = new FormFillingProperties();
        var renames = Enumerable.Range(0, 500).Select(i => Rename($"o{i}", $"n{i}")).ToList();
        for (var session = 0; session < FormFillingReportCreator.MaxEditorRenamesPerForm / renames.Count; session++)
        {
            FormFillingReportCreator.KeepEditorRenames(state, 1, 100, Guid.NewGuid(), "tab", renames).Should().BeTrue();
        }

        FormFillingReportCreator.KeepEditorRenames(state, 1, 100, _user, "late", renames).Should().BeFalse();

        state.EditorFieldRenames.Should().NotContain(s => s.SessionId == "late");
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
