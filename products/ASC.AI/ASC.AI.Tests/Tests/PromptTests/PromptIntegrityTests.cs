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
/// An update, a move and a delete keep the rest intact: a partial update touches only what was sent, a move keeps the
/// prompt's identity and creation date, a delete leaves its folder and the siblings alone, and every refused name
/// conflict leaves the whole library byte-identical, timestamps included.
/// </summary>
/// <remarks>
/// Left alone on purpose, because the SDK does not say what is right: what <c>update</c> with an empty <c>updates</c>
/// answers (only that it cannot edit), and names that differ only by surrounding whitespace.
/// </remarks>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Prompts")]
public class PromptIntegrityTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    [Fact]
    public async Task Update_APartialUpdateTouchesOnlyWhatWasSent_AndARepeatChangesNothingMore()
    {
        var folder = await CreateFolderAsync("Autotest home");
        var prompt = await CreatePromptAsync("Autotest original", "Original body", folder.Id);
        var bystander = await CreatePromptAsync("Autotest bystander", "Bystander body");
        var original = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        var bystanderBefore = (await _aiPrompts.AiPromptsGetByIdAsync(bystander.Id, Token)).ToJson();
        await NextSecondAsync();

        var updates = new Dictionary<string, object?> { ["name"] = "Autotest both", ["text"] = "Both body" };
        var (status, body) = await UpdateRawAsync(prompt.Id, updates);
        status.Should().Be(200, body);
        ParseBody(body).GetProperty("success").GetBoolean().Should().BeTrue(body);

        var read = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        read.Id.Should().Be(prompt.Id, "the id survives");
        read.Name.Should().Be("Autotest both");
        read.Text.Should().Be("Both body");
        read.FolderId.Should().Be(folder.Id, "the folder was not in `updates`");
        read.CreatedAt.Should().Be(original.CreatedAt, "createdAt is not rewritten");
        read.UpdatedAt.Should().BeGreaterThan(original.UpdatedAt, "updatedAt moves forward");

        var (againStatus, againBody) = await UpdateRawAsync(prompt.Id, updates);
        againStatus.Should().Be(200, againBody);
        ParseBody(againBody).GetProperty("success").GetBoolean().Should().BeTrue("the same values again");

        var afterAgain = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        afterAgain.Name.Should().Be("Autotest both");
        afterAgain.Text.Should().Be("Both body");
        afterAgain.FolderId.Should().Be(folder.Id);

        (await _aiPrompts.AiPromptsGetByIdAsync(bystander.Id, Token)).ToJson().Should().Be(bystanderBefore, "another prompt is untouched");
    }

    [Fact]
    public async Task Update_AnEmptyUpdatesObjectOrANullOne_ChangesNothing()
    {
        var folder = await CreateFolderAsync("Autotest home");
        var prompt = await CreatePromptAsync("Autotest keeper", "Body", folder.Id);
        var before = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);

        // Whether an empty `updates` is an accepted no-op or a refusal is not stated by the SDK, so the status is not
        // asserted, only that it cannot edit. Raw on purpose: the typed request always carries `folderId: null`, and
        // cannot be built with `updates` null or absent.
        await SendRawAsync(HttpMethod.Put, "update", Json(new { id = prompt.Id, updates = new { } }));
        await SendRawAsync(HttpMethod.Put, "update", Json(new { id = prompt.Id, updates = (object?)null }));
        await SendRawAsync(HttpMethod.Put, "update", Json(new { id = prompt.Id }));

        var after = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        after.Name.Should().Be(before.Name);
        after.Text.Should().Be(before.Text);
        after.FolderId.Should().Be(before.FolderId);
        after.CreatedAt.Should().Be(before.CreatedAt);
    }

    [Fact]
    public async Task Update_ALongUnicodeText_IsStoredWhole()
    {
        var prompt = await CreatePromptAsync("Autotest long", "short");
        var text = string.Concat(Enumerable.Repeat("Тест 界 🚀 ", 5000));

        var updated = await _aiPrompts.AiPromptsUpdateAsync(
            new AiPromptsUpdateRequest(prompt.Id, new AiPromptsUpdateRequestUpdates(text: text)), Token);

        updated.Success.Should().BeTrue();
        (await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token)).Text.Should().Be(text);
        (await _aiPrompts.AiPromptsExportAsync(Token)).Prompts[0].Text.Should().Be(text, "and through export");
    }

    [Fact]
    public async Task Update_FolderId_MovesThePromptWithoutTouchingItsNameOrText_AndNullReturnsItToTheRoot()
    {
        var folderA = await CreateFolderAsync("Autotest A");
        var folderB = await CreateFolderAsync("Autotest B");
        var prompt = await CreatePromptAsync("Autotest traveller", "Traveller body", folderA.Id);

        foreach (var (label, target) in new (string, string?)[] { ("to folder B", folderB.Id), ("to the root", null), ("to folder A", folderA.Id) })
        {
            var updated = await _aiPrompts.AiPromptsUpdateAsync(
                new AiPromptsUpdateRequest(prompt.Id, new AiPromptsUpdateRequestUpdates(folderId: target!)), Token);

            updated.Success.Should().BeTrue(label);
            var read = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
            read.FolderId.Should().Be(target, label);
            read.Name.Should().Be("Autotest traveller", label);
            read.Text.Should().Be("Traveller body", label);
        }
    }

    [Fact]
    public async Task Move_KeepsTheIdNameTextAndCreatedAt_AndOmittingFolderIdMeansTheRoot()
    {
        var folder = await CreateFolderAsync("Autotest home");
        var prompt = await CreatePromptAsync("Autotest traveller", "Traveller body");
        var original = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);

        (await _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(prompt.Id, folder.Id), Token)).Success.Should().BeTrue();
        var inside = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        inside.FolderId.Should().Be(folder.Id);
        inside.Id.Should().Be(original.Id);
        inside.Name.Should().Be(original.Name);
        inside.Text.Should().Be(original.Text);
        inside.CreatedAt.Should().Be(original.CreatedAt);

        // The SDK: "to the root when `folderId` is omitted or null". Raw on purpose: the typed request requires a folderId.
        var (status, body) = await SendRawAsync(HttpMethod.Put, "move", Json(new { id = prompt.Id }));
        status.Should().Be(200, body);
        ParseBody(body).GetProperty("success").GetBoolean().Should().BeTrue(body);
        (await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token)).FolderId.Should().BeNull();
        (await ListIdsAsync(folder.Id)).Should().BeEmpty("the folder is empty again");

        // Root to root is a no-op that is still answered with success.
        (await _aiPrompts.AiPromptsMoveAsync(MoveToRoot(prompt.Id), Token)).Success.Should().BeTrue();
        (await ListIdsAsync()).Should().Equal(prompt.Id);
    }

    [Fact]
    public async Task Move_IntoTheFolderThePromptIsAlreadyIn_Succeeds_AndChangesNothing()
    {
        var folder = await CreateFolderAsync("Autotest home");
        var prompt = await CreatePromptAsync("Autotest settled", "Body", folder.Id);
        var before = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);

        foreach (var attempt in new[] { "first", "repeat" })
        {
            var moved = await _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(prompt.Id, folder.Id), Token);

            moved.Success.Should().BeTrue($"{attempt}: it is not a conflict with itself");
        }

        var after = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        after.FolderId.Should().Be(folder.Id);
        after.Name.Should().Be(before.Name);
        after.Text.Should().Be(before.Text);
        (await ListIdsAsync(folder.Id)).Should().Equal(prompt.Id);
    }

    [Fact]
    public async Task Delete_RemovesAPromptFromItsFolderOnly_AndTheFolderAndItsOtherPromptsStay()
    {
        var folder = await CreateFolderAsync("Autotest home");
        var doomed = await CreatePromptAsync("Autotest doomed", "Body", folder.Id);
        var sibling = await CreatePromptAsync("Autotest sibling", "Body", folder.Id);
        var root = await CreatePromptAsync("Autotest root", "Body");

        (await _aiPrompts.AiPromptsDeleteAsync(doomed.Id, Token)).Success.Should().BeTrue();

        (await FindPromptAsync(doomed.Id)).Should().BeNull();
        (await ListIdsAsync(folder.Id)).Should().Equal(sibling.Id);
        (await FindFolderAsync(folder.Id))!.Id.Should().Be(folder.Id);
        (await ListIdsAsync()).Should().Equal(root.Id);
        (await ReadLibraryAsync()).Prompts.Select(p => p.Id).Should().BeEquivalentTo([sibling.Id, root.Id], "export agrees");

        // Deleting the last prompt of a folder does not delete the folder.
        await _aiPrompts.AiPromptsDeleteAsync(sibling.Id, Token);
        (await ListIdsAsync(folder.Id)).Should().BeEmpty();
        (await FindFolderAsync(folder.Id))!.Id.Should().Be(folder.Id);

        // And the repeat touches nothing else.
        var before = await ReadLibraryAsync();
        (await _aiPrompts.AiPromptsDeleteAsync(doomed.Id, Token)).Success.Should().BeTrue();
        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task DeleteFolder_WithSeveralPrompts_RemovesAllOfThemEverywhere_AndSparesTheOtherFolders()
    {
        var doomedFolder = await CreateFolderAsync("Autotest doomed");
        var keptFolder = await CreateFolderAsync("Autotest kept");
        var doomed = new List<string>();

        foreach (var name in new[] { "one", "two", "three" })
        {
            doomed.Add((await CreatePromptAsync($"Autotest doomed {name}", "Body", doomedFolder.Id)).Id);
        }

        var kept = await CreatePromptAsync("Autotest doomed one", "Same name, other folder", keptFolder.Id);
        var root = await CreatePromptAsync("Autotest doomed one", "Same name, root");

        (await _aiPrompts.AiPromptsDeleteFolderAsync(doomedFolder.Id, Token)).Success.Should().BeTrue();

        foreach (var id in doomed)
        {
            (await FindPromptAsync(id)).Should().BeNull($"prompt {id}");
        }

        (await FindFolderAsync(doomedFolder.Id)).Should().BeNull();
        (await ListIdsAsync()).Should().Equal([root.Id], "none of them was moved to the root");

        var left = await ReadLibraryAsync();
        left.Folders.Select(f => f.Id).Should().Equal(keptFolder.Id);
        left.Prompts.Select(p => p.Id).Should().BeEquivalentTo([kept.Id, root.Id]);
    }

    [Fact]
    public async Task NameConflicts_EveryRefusalLeavesTheWholeLibraryUntouched_AndARenameLetsTheMoveThrough()
    {
        var folderA = await CreateFolderAsync("Autotest A");
        var folderB = await CreateFolderAsync("Autotest B");
        await CreatePromptAsync("Autotest clash", "A body", folderA.Id);
        var inB = await CreatePromptAsync("Autotest clash", "B body", folderB.Id);
        var inRoot = await CreatePromptAsync("Autotest clash", "Root body");
        var before = await ReadLibraryAsync();
        before.Prompts.Should().HaveCount(3, "three prompts share one name legally");

        var refusals = new List<(string Label, Func<Task> Call, string Message)>
        {
            ("create in A", () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest clash", "x", folderA.Id), Token), PromptNameTaken),
            ("create in the root", () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest clash", "x"), Token), PromptNameTaken),
            ("move the root prompt into A", () => _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(inRoot.Id, folderA.Id), Token), PromptNameTaken),
            (
                "update{folderId} of the B prompt to A",
                () => _aiPrompts.AiPromptsUpdateAsync(new AiPromptsUpdateRequest(inB.Id, new AiPromptsUpdateRequestUpdates(folderId: folderA.Id)), Token),
                PromptNameTaken),
            (
                "update{name, folderId} of the B prompt to A",
                () => _aiPrompts.AiPromptsUpdateAsync(
                    new AiPromptsUpdateRequest(inB.Id, new AiPromptsUpdateRequestUpdates(name: "Autotest clash", folderId: folderA.Id)), Token),
                PromptNameTaken),
            ("create-folder with an existing name", () => _aiPrompts.AiPromptsCreateFolderAsync("Autotest A", Token), FolderNameTaken),
            (
                "rename-folder B onto A",
                () => _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(folderB.Id, "Autotest A"), Token),
                FolderNameTaken)
        };

        foreach (var (_, call, message) in refusals)
        {
            // One at a time: a refusal that wrongly succeeded would otherwise change the state the next call is judged against.
            await ExpectRefusalAsync(call, message, 400, 409);
        }

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, "no refused call changed a row, a folder or a timestamp");

        // The second half: a rename removes the conflict, and the very same move now goes through.
        (await _aiPrompts.AiPromptsUpdateAsync(
            new AiPromptsUpdateRequest(inRoot.Id, new AiPromptsUpdateRequestUpdates(name: "Autotest clash renamed")), Token)).Success.Should().BeTrue();
        (await _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(inRoot.Id, folderA.Id), Token)).Success.Should().BeTrue();

        (await _aiPrompts.AiPromptsListAsync(folderA.Id, Token)).Select(p => p.Name).Order(StringComparer.Ordinal)
            .Should().Equal(["Autotest clash", "Autotest clash renamed"]);
        (await ListIdsAsync()).Should().BeEmpty();
    }
}
