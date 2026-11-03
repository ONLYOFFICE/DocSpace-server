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
/// Moving a prompt between folders and the root (<c>prompts/move</c>, and <c>update</c> with a <c>folderId</c>), and the
/// per-folder name rule that has to be re-checked at the moment a prompt lands in another folder. A refusal there used
/// to be a soft 200 <c>{success: false}</c> (BUG 84310, 84311), and <c>move</c>/<c>update{folderId}</c> did not consult the
/// rule at all (BUG 83122).
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/PromptFolders")]
public class PromptMoveTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    [Fact]
    public async Task Move_IntoAFolderAndBackToTheRoot()
    {
        var folder = await CreateFolderAsync("Autotest target");
        var prompt = await CreatePromptAsync("Autotest movable", "Body");

        // Starts at the root: listed without a folderId, absent from the folder.
        (await ListIdsAsync()).Should().Equal(prompt.Id);
        (await ListIdsAsync(folder.Id)).Should().BeEmpty();

        var moved = await _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(prompt.Id, folder.Id), Token);
        moved.Success.Should().BeTrue();
        moved.Prompt.FolderId.Should().Be(folder.Id);

        // The root listing is scoped, not cumulative: a moved prompt leaves it.
        (await ListIdsAsync(folder.Id)).Should().Equal(prompt.Id);
        (await ListIdsAsync()).Should().BeEmpty("the root no longer lists it");

        (await _aiPrompts.AiPromptsMoveAsync(MoveToRoot(prompt.Id), Token)).Success.Should().BeTrue();

        (await ListIdsAsync()).Should().Equal(prompt.Id);
        (await ListIdsAsync(folder.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Move_BetweenTwoFolders_LeavesTheSourceEmpty()
    {
        var source = await CreateFolderAsync("Autotest source");
        var target = await CreateFolderAsync("Autotest target");
        var prompt = await CreatePromptAsync("Autotest movable", "Body", source.Id);

        (await _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(prompt.Id, target.Id), Token)).Success.Should().BeTrue();

        (await ListIdsAsync(target.Id)).Should().Equal(prompt.Id);
        (await ListIdsAsync(source.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Move_ToANonExistentFolder_IsRefused_AndChangesNothing()
    {
        var folder = await CreateFolderAsync("Autotest home");
        var prompt = await CreatePromptAsync("Autotest movable", "Body", folder.Id);

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(prompt.Id, UnknownFolderId), Token),
            $"Folder not found: {UnknownFolderId}",
            404);

        // The prompt did not fall out of its folder on the way.
        (await ListIdsAsync(folder.Id)).Should().Equal(prompt.Id);
    }

    [Fact]
    public async Task Move_UnknownPromptId_IsRefused()
    {
        var folder = await CreateFolderAsync("Autotest target");

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(UnknownPromptId, folder.Id), Token),
            $"Prompt not found: {UnknownPromptId}",
            404);
    }

    [Fact]
    public async Task Update_FolderId_MovesThePromptToo()
    {
        var folder = await CreateFolderAsync("Autotest target");
        var prompt = await CreatePromptAsync("Autotest movable", "Body");

        var updated = await _aiPrompts.AiPromptsUpdateAsync(
            new AiPromptsUpdateRequest(prompt.Id, new AiPromptsUpdateRequestUpdates(folderId: folder.Id)), Token);

        updated.Success.Should().BeTrue();
        (await ListIdsAsync(folder.Id)).Should().Equal(prompt.Id);
    }

    /// <summary>
    /// The duplicate-name rule is scoped to a folder, so two prompts may legally share a name while they sit in different
    /// folders. Moving one onto the other is the moment the rule has to be re-checked.
    /// </summary>
    [Fact]
    [Trait("Bug", "83122")]
    [Trait("Bug", "84310")]
    public async Task Move_OntoATakenName_IsRefusedWith400Or409()
    {
        var target = await CreateFolderAsync("Autotest target");

        // Same name in two scopes, which create allows: the rule is per folder.
        var inFolder = await CreatePromptAsync("Autotest clash", "Folder body", target.Id);
        var inRoot = await CreatePromptAsync("Autotest clash", "Root body");

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(inRoot.Id, target.Id), Token),
            PromptNameTaken,
            400,
            409);

        // Control: create refuses that very name inside the target folder, so the per-folder rule is live.
        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest clash", "Third body", target.Id), Token),
            PromptNameTaken,
            400,
            409);

        var listed = await _aiPrompts.AiPromptsListAsync(target.Id, Token);
        listed.Select(p => p.Id).Should().Equal(inFolder.Id);
        listed[0].Text.Should().Be("Folder body");
        (await ListIdsAsync()).Should().Equal(inRoot.Id);
    }

    [Fact]
    [Trait("Bug", "83122")]
    [Trait("Bug", "84311")]
    public async Task Update_MovingOntoATakenName_IsRefusedWith400Or409()
    {
        var target = await CreateFolderAsync("Autotest target");
        await CreatePromptAsync("Autotest clash", "Folder body", target.Id);
        var inRoot = await CreatePromptAsync("Autotest clash", "Root body");

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsUpdateAsync(
                new AiPromptsUpdateRequest(inRoot.Id, new AiPromptsUpdateRequestUpdates(folderId: target.Id)), Token),
            PromptNameTaken,
            400,
            409);

        // Control: the rule is live on this route's folder too.
        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest clash", "Third body", target.Id), Token),
            PromptNameTaken,
            400,
            409);

        (await ListIdsAsync(target.Id)).Should().HaveCount(1, "the target folder still holds one prompt");
        (await ListIdsAsync()).Should().Equal(inRoot.Id);
    }

    [Fact]
    [Trait("Bug", "84311")]
    public async Task Update_RenamingOntoASiblingsName_IsRefusedWith400Or409()
    {
        var folder = await CreateFolderAsync("Autotest folder");
        await CreatePromptAsync("Autotest taken", "First body", folder.Id);
        var second = await CreatePromptAsync("Autotest free", "Second body", folder.Id);

        // The prompt sits in a folder, so the typed update (which always asks for the root) would not be a rename.
        var (status, body) = await UpdateRawAsync(second.Id, new Dictionary<string, object?> { ["name"] = "Autotest taken" });

        status.Should().BeOneOf(400, 409);
        var answer = ParseBody(body);
        answer.GetProperty("success").GetBoolean().Should().BeFalse(body);
        answer.GetProperty("error").GetProperty("message").GetString().Should().Be(PromptNameTaken);
        (await FindPromptAsync(second.Id))!.Name.Should().Be("Autotest free", "the refused rename left the name alone");
    }

    [Fact]
    public async Task List_UnknownFolderId_ListsNothing()
    {
        // A root prompt as the positive control: an empty answer then means "that folder has nothing", not "the listing is broken".
        var root = await CreatePromptAsync("Autotest root", "Root body");

        (await ListIdsAsync(UnknownFolderId)).Should().BeEmpty("an unknown folder lists nothing");
        (await ListIdsAsync()).Should().Equal(root.Id);
    }

    /// <summary>
    /// A folderId that is not a GUID at all never reaches the store: instead of the empty listing an unknown but
    /// well-formed id gets, the request is refused. The two look alike from the client's side.
    /// </summary>
    [Fact]
    public async Task List_MalformedFolderId_IsRefused()
    {
        var root = await CreatePromptAsync("Autotest root", "Root body");

        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsListAsync("not-a-guid", Token), null, 400);

        (await ListIdsAsync()).Should().Equal([root.Id], "the refused call leaked nothing");
    }
}
