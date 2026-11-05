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

namespace ASC.AI.Tests.Tests.PromptTests;

/// <summary>
/// The saved prompt, end to end (<c>prompts/create</c>, <c>update</c>, <c>delete</c>, <c>get-by-id</c>): what a create
/// answers and stores, that an update touches only what was sent, how a delete and a read of an id the library does not
/// hold answer, and which content is refused. A taken name used to be a soft 200 <c>{success: false}</c>; it is a
/// 400/409 now (BUG 84307). The over-long name is in <see cref="PromptStorageTests.PromptNameLengthTests"/>.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Prompts")]
public class PromptLifecycleTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    [Fact]
    public async Task Create_Owner_ReadsBackThroughGetAndList()
    {
        (await _aiPrompts.AiPromptsListAsync(cancellationToken: Token)).Should().BeEmpty("the store is per user and starts empty");

        var created = await CreatePromptAsync("Autotest summarize", "Summarize the document in three bullet points.");

        created.Id.Should().NotBeNullOrEmpty();
        created.Name.Should().Be("Autotest summarize");
        created.Text.Should().Be("Summarize the document in three bullet points.");
        created.CreatedAt.Should().BeGreaterThan(0);
        created.UpdatedAt.Should().BeGreaterThan(0);

        var read = await _aiPrompts.AiPromptsGetByIdAsync(created.Id, Token);
        read.Id.Should().Be(created.Id);
        read.Name.Should().Be("Autotest summarize");
        read.Text.Should().Be("Summarize the document in three bullet points.");

        (await ListIdsAsync()).Should().Contain(created.Id);
    }

    [Fact]
    public async Task Update_NameAndText_AreIndependent()
    {
        var prompt = await CreatePromptAsync("Autotest original", "Original body");

        var renamed = await _aiPrompts.AiPromptsUpdateAsync(
            new AiPromptsUpdateRequest(prompt.Id, new AiPromptsUpdateRequestUpdates(name: "Autotest renamed")), Token);
        renamed.Success.Should().BeTrue();

        // The field that was not sent keeps its value instead of being blanked.
        var afterRename = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        afterRename.Name.Should().Be("Autotest renamed");
        afterRename.Text.Should().Be("Original body", "the text survived the rename");

        var retexted = await _aiPrompts.AiPromptsUpdateAsync(
            new AiPromptsUpdateRequest(prompt.Id, new AiPromptsUpdateRequestUpdates(text: "Replaced body")), Token);
        retexted.Success.Should().BeTrue();

        var afterRetext = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        afterRetext.Text.Should().Be("Replaced body");
        afterRetext.Name.Should().Be("Autotest renamed", "the name survived the re-text");
    }

    [Fact]
    public async Task Delete_Prompt_RemovesItAndLeavesTheOthers()
    {
        var doomed = await CreatePromptAsync("Autotest doomed", "Body");
        var keeper = await CreatePromptAsync("Autotest keeper", "Body");

        (await _aiPrompts.AiPromptsDeleteAsync(doomed.Id, Token)).Success.Should().BeTrue();

        (await FindPromptAsync(doomed.Id)).Should().BeNull();
        (await ListIdsAsync()).Should().Equal(keeper.Id);
    }

    [Fact]
    public async Task Delete_MissingId_IsRefused_AndASecondDeleteIsAccepted()
    {
        var prompt = await CreatePromptAsync("Autotest once", "Body");

        var missing = await ExpectRefusalAsync(() => _aiPrompts.AiPromptsDeleteAsync(string.Empty, Token), null, 400);
        missing.ErrorContent?.ToString().Should().Contain("id required");
        (await FindPromptAsync(prompt.Id))!.Id.Should().Be(prompt.Id, "the rejected delete removed nothing");

        (await _aiPrompts.AiPromptsDeleteAsync(prompt.Id, Token)).Success.Should().BeTrue();

        // Deletion is idempotent rather than a 404 on the second call.
        (await _aiPrompts.AiPromptsDeleteAsync(prompt.Id, Token)).Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_MissingId_IsRefused_AndAnUnknownIdIsNull()
    {
        var empty = await ExpectRefusalAsync(() => _aiPrompts.AiPromptsGetByIdAsync(string.Empty, Token), null, 400);
        empty.ErrorContent?.ToString().Should().Contain("id required");

        // A well-formed but unknown id is 200 null, the same shape as threads/get-by-id and profiles/get-by-id.
        (await FindPromptAsync(UnknownPromptId)).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task Create_BlankName_IsRefusedAsAValidationError(string name)
    {
        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput(name, "Body"), Token), NameRequired, 400);

        (await _aiPrompts.AiPromptsListAsync(cancellationToken: Token)).Should().BeEmpty("nothing was stored under that name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankText_IsRefusedWith400(string text)
    {
        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest blank text", text), Token), null, 400);

        (await _aiPrompts.AiPromptsListAsync(cancellationToken: Token)).Should().BeEmpty();
    }

    /// <summary>
    /// A taken name used to be a soft HTTP 200 <c>{success: false}</c> next to the hard 400 this route gives a blank text:
    /// a caller that checked the status alone saw success where the record was never written.
    /// </summary>
    [Fact]
    [Trait("Bug", "84307")]
    public async Task Create_TakenName_IsRefusedWith400Or409()
    {
        var first = await CreatePromptAsync("Autotest duplicate", "First body");

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest duplicate", "Second body"), Token),
            PromptNameTaken,
            400,
            409);

        // The original is untouched and no second record appeared.
        var listed = await _aiPrompts.AiPromptsListAsync(cancellationToken: Token);
        listed.Select(p => p.Id).Should().Equal(first.Id);
        listed[0].Text.Should().Be("First body");
    }

    [Fact]
    public async Task Create_SameTextUnderADifferentName_IsAllowed()
    {
        var first = await CreatePromptAsync("Autotest one", "Identical body");
        var second = await CreatePromptAsync("Autotest two", "Identical body");

        second.Id.Should().NotBe(first.Id);
        (await ListIdsAsync()).Should().BeEquivalentTo([first.Id, second.Id]);
    }

    [Fact]
    public async Task Create_UnicodeEmojiAndMarkdown_SurviveTheRoundTrip()
    {
        const string name = "Autotest translate 🎉";
        var text = string.Join(
            "\n",
            "# Heading",
            string.Empty,
            "Translate **this** text into French:",
            string.Empty,
            "```js",
            "const a = 1 < 2 && 3 > 2;",
            "```",
            string.Empty,
            "| column | \"value\" |");

        var prompt = await CreatePromptAsync(name, text);

        var read = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        read.Name.Should().Be(name);
        read.Text.Should().Be(text, "markdown and unicode are stored verbatim");
    }

    [Fact]
    public async Task Create_FiftyKilobyteText_IsStoredWhole()
    {
        // The name is limited to 255 characters and the text is not: a client cannot assume one check covers both.
        var text = new string('Y', 50000);

        var prompt = await CreatePromptAsync("Autotest long text", text);

        var read = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        read.Text.Length.Should().Be(50000, "the stored body length");
        read.Text.Should().Be(text);
    }

    [Theory]
    [InlineData(UnknownFolderId)]
    [InlineData("not-a-guid")]
    public async Task Create_InANonExistentFolder_IsRefused(string folderId)
    {
        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput($"Autotest folder {folderId}", "Body", folderId), Token),
            $"Folder not found: {folderId}",
            404);

        (await _aiPrompts.AiPromptsListAsync(cancellationToken: Token)).Should().BeEmpty();
    }

    [Fact]
    public async Task Update_UnknownPromptId_IsRefused()
    {
        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsUpdateAsync(
                new AiPromptsUpdateRequest(UnknownPromptId, new AiPromptsUpdateRequestUpdates(name: "Autotest renamed")),
                Token),
            $"Prompt not found: {UnknownPromptId}",
            404);
    }

    /// <summary>
    /// The storage keeps the bytes as sent, which is right for a prompt body: the answer is JSON and has no HTML parsing
    /// context, so escaping would corrupt legitimate prompts. What this pins is that nothing is silently rewritten,
    /// and that it is still the same payload after a round trip through export.
    /// </summary>
    [Fact]
    public async Task Create_HtmlAndScriptPayloads_AreStoredVerbatim()
    {
        const string name = "<img src=x onerror=alert(1)>";
        const string text = "<script>alert(document.cookie)</script>";

        var prompt = await CreatePromptAsync(name, text);

        var read = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        read.Name.Should().Be(name);
        read.Text.Should().Be(text);

        var inBundle = (await _aiPrompts.AiPromptsExportAsync(Token)).Prompts.Single(p => p.Id == prompt.Id);
        inBundle.Name.Should().Be(name);
        inBundle.Text.Should().Be(text);
    }
}
