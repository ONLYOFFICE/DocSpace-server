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

    private static FormFieldRename Rename(string oldKey, string newKey) => new() { OldKey = oldKey, NewKey = newKey };

    private static FormEditorFieldRenames Saved(string sessionId, params FormFieldRename[] renames) =>
        new() { OpenedVersion = 1, UserId = _user, SessionId = sessionId, SavedVersion = 2, Sequence = 1, Renames = [.. renames] };

    private static List<FormMetadata> Fields(params string[] keys) => [.. keys.Select(k => new FormMetadata { Key = k, Type = "text" })];

    private static DbFormsMetadataSearch Previous(params string[] keys) => new() { OriginalFormVersion = 1, Metadata = Fields(keys) };
}
