// Copyright (C) Ascensio System SIA, 2009-2026
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

namespace ASC.AI.Tests.Tests.AttachmentTests;

/// <summary>
/// <c>POST /api/2.0/ai/attachments/suggested-questions</c>. The request is <c>{ id: &lt;attachment uuid&gt; }</c> — the id
/// <c>save-file</c> returns, not the DocSpace file id — and the response is <c>{ status, questions }</c>. Every attachment
/// that can be set up through the API (plain files, PDF forms) reads <c>status: "unavailable"</c> with an empty
/// <c>questions</c> list, without a model call: <c>canAnalyze</c> is false for all of them. The branch where questions are
/// generated has never been observed, so the content of <c>questions</c> is not asserted. The route answers an owner and a
/// member alike, an anonymous caller 401, and gives away neither whether an attachment exists nor whose it is.
/// </summary>
/// <remarks>
/// Raw on purpose — an SDK defect: the generated <c>AiAttachmentsGetSuggestedQuestions</c> answers an
/// <see cref="AiSuccessResponse"/> (<c>{success}</c>), which drops both <c>status</c> and <c>questions</c>, the only fields
/// that are asserted. A Guest also gets 200 here although every other attachments route is closed to a Guest; whether that
/// is intended is a question for the developers, so there is no Guest test.
/// </remarks>
[Trait("Category", "Attachments")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentSuggestedQuestionsTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    private Task<RawOutcome> SuggestAsync(string id) => PostRawAsync("suggested-questions", Json(new { id }));

    private static void ExpectUnavailable(RawOutcome outcome)
    {
        outcome.Status.Should().Be(200, outcome.Body);
        using var document = JsonDocument.Parse(outcome.Body);
        document.RootElement.GetProperty("status").GetString().Should().Be("unavailable");
        document.RootElement.GetProperty("questions").ValueKind.Should().Be(JsonValueKind.Array);
        document.RootElement.GetProperty("questions").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task SuggestedQuestions_ValidAttachmentId_Answers200WithAStatusAndAQuestionsList()
    {
        var draftId = await SaveDraftAsync(Owner, "Autotest suggested questions.docx", "Quarterly revenue grew twelve percent.");
        // Why the observed branch is the unavailable one: the draft is a plain file, not a form the portal can analyse.
        (await ExpectStoredAsync(draftId)).CanAnalyze.Should().BeFalse();

        ExpectUnavailable(await SuggestAsync(draftId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{"attachmentId":"01a1112f-0000-7000-8000-000000000000"}""")]
    [InlineData("""{"id":""}""")]
    [InlineData("""{"id":null}""")]
    [InlineData("""{"id":123}""")]
    [InlineData("""{"id":true}""")]
    [InlineData("""{"id":{"value":"01a1112f-0000-7000-8000-000000000000"}}""")]
    [InlineData("""{"id":["01a1112f-0000-7000-8000-000000000000"]}""")]
    [InlineData("""{"id":"not-a-uuid"}""")]
    [InlineData("""{"id":"3708066"}""")]
    [InlineData("""{"id":"01a1112f-0000-7000-8000-00000000000"}""")]
    public async Task SuggestedQuestions_MissingEmptyNullMistypedOrMalformedId_Is400(string? body)
    {
        // A missing body, an empty object, null, an array, another field name, an empty or null id, a number, a boolean,
        // an object, an array of ids, text that is not a uuid, a DocSpace file id and a uuid with a character cut off.
        var refused = await PostRawAsync("suggested-questions", body);

        refused.Status.Should().Be(400, refused.Body);
        refused.Body.Should().NotContain("questions", "a refusal carries no list");
    }

    [Fact]
    public async Task SuggestedQuestions_UnknownButWellFormedId_Answers200UnavailableAndEmpty()
    {
        ExpectUnavailable(await SuggestAsync(UnknownAttachmentId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SuggestedQuestions_Anonymous_Gets401(bool withAValidBody)
    {
        await ActAsAsync(null);

        var outcome = await PostRawAsync("suggested-questions", withAValidBody ? Json(new { id = UnknownAttachmentId }) : "{}");

        outcome.Status.Should().Be(401);
    }

    [Fact]
    public async Task SuggestedQuestions_OwnerAndAUser_EachGet200OnTheirOwnAttachment()
    {
        var ownerDraft = await SaveDraftAsync(Owner, "Autotest owner suggested questions.docx", "owner text");
        var ownerResult = await SuggestAsync(ownerDraft);

        var member = await AddMemberAsync(EmployeeType.User);
        var memberDraft = await SaveDraftAsync(member, "Autotest user suggested questions.docx", "user text");
        var memberResult = await SuggestAsync(memberDraft);

        ExpectUnavailable(ownerResult);
        ExpectUnavailable(memberResult);
    }

    [Fact]
    public async Task SuggestedQuestions_AnotherUsersAttachmentId_AnswersExactlyWhatAnUnknownIdAnswers()
    {
        var ownerDraft = await SaveDraftAsync(Owner, "Autotest foreign suggested questions.docx", "OWNER-SECRET");

        var member = await AddMemberAsync(EmployeeType.User);
        await ActAsAsync(member);

        // Control: the id really is somebody else's — the User cannot read it through the attachments store, so an
        // identical answer is not just the User seeing their own draft.
        (await ReadOnceAsync(ownerDraft)).Should().BeNull("the owner's draft is invisible to the User");

        var foreign = await SuggestAsync(ownerDraft);
        var unknown = await SuggestAsync(UnknownAttachmentId);

        foreign.Status.Should().Be(200);
        unknown.Status.Should().Be(200);
        System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse(foreign.Body),
            System.Text.Json.Nodes.JsonNode.Parse(unknown.Body)).Should().BeTrue($"{foreign.Body} vs {unknown.Body}");
        foreign.Body.Should().NotContain("OWNER-SECRET");
    }
}
