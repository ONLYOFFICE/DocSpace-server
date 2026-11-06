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
/// The read side of the caller's own library: <c>get-by-id</c>, <c>get-folder-by-id</c>, <c>list</c> and
/// <c>list-folders</c>. Both listings are documented "newest first" by creation date, which is stored in whole seconds,
/// so the ordering tests wait between creates instead of assuming a tie-break. A listing without a <c>folderId</c>, or
/// with an empty one, is the root, not the whole library.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Prompts")]
public class PromptReadAndListTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    [Fact]
    public async Task GetById_ReturnsEveryField_WithFolderIdOnlyForAPromptInsideAFolder()
    {
        var folder = await CreateFolderAsync("Autotest home");
        var root = await CreatePromptAsync("Autotest root", "Root body");
        var inside = await CreatePromptAsync("Autotest inside", "Inside body", folder.Id);

        var readRoot = await _aiPrompts.AiPromptsGetByIdAsync(root.Id, Token);
        readRoot.Id.Should().Be(root.Id);
        readRoot.Name.Should().Be("Autotest root");
        readRoot.Text.Should().Be("Root body");
        readRoot.FolderId.Should().BeNull("a root prompt carries no folderId");
        readRoot.CreatedAt.Should().BeGreaterThan(0);
        readRoot.UpdatedAt.Should().BeGreaterThan(0);

        var readInside = await _aiPrompts.AiPromptsGetByIdAsync(inside.Id, Token);
        readInside.Id.Should().Be(inside.Id);
        readInside.Name.Should().Be("Autotest inside");
        readInside.Text.Should().Be("Inside body");
        readInside.FolderId.Should().Be(folder.Id);
        readInside.CreatedAt.Should().BeGreaterThan(0);
        readInside.UpdatedAt.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetById_ReflectsAnUpdateAndAMove_AndDoesNotChangeAnything()
    {
        var folder = await CreateFolderAsync("Autotest home");
        var prompt = await CreatePromptAsync("Autotest before", "Before body");

        await _aiPrompts.AiPromptsUpdateAsync(
            new AiPromptsUpdateRequest(prompt.Id, new AiPromptsUpdateRequestUpdates(name: "Autotest after", text: "After body")), Token);
        var updated = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        updated.Name.Should().Be("Autotest after");
        updated.Text.Should().Be("After body");

        await _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(prompt.Id, folder.Id), Token);
        (await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token)).FolderId.Should().Be(folder.Id);

        // Reading is read-only: the same calls repeated leave the library as it was.
        var before = await ReadLibraryAsync();

        for (var i = 0; i < 3; i++)
        {
            await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
            await _aiPrompts.AiPromptsGetFolderByIdAsync(folder.Id, Token);
            await _aiPrompts.AiPromptsListAsync(folder.Id, Token);
            await _aiPrompts.AiPromptsListFoldersAsync(Token);
            await _aiPrompts.AiPromptsExportAsync(Token);
        }

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task GetFolderById_ReturnsTheFolderItself_NeverThePromptsInsideIt()
    {
        var empty = await CreateFolderAsync("Autotest empty");
        var full = await CreateFolderAsync("Autotest full");
        await CreatePromptAsync("Autotest inside", "Body", full.Id);

        foreach (var (folder, name) in new[] { (empty, "Autotest empty"), (full, "Autotest full") })
        {
            // Raw on purpose: the generated model cannot tell a folder from one that carries extra fields.
            var (status, body) = await GetRawAsync($"get-folder-by-id?id={folder.Id}");

            status.Should().Be(200, name);
            var fields = ParseBody(body).EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal);
            fields.Should().Equal(["createdAt", "id", "name", "updatedAt"], name);
            ParseBody(body).GetProperty("name").GetString().Should().Be(name);
        }

        await _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(full.Id, "Autotest renamed"), Token);
        (await FindFolderAsync(full.Id))!.Name.Should().Be("Autotest renamed");
    }

    [Fact]
    public async Task List_ListsTheRootNewestFirst_WithoutDuplicates_AndAnEmptyFolderIdMeansTheRoot()
    {
        var folder = await CreateFolderAsync("Autotest home");
        var first = await CreatePromptAsync("Autotest first", "Body");
        await NextSecondAsync();
        var second = await CreatePromptAsync("Autotest second", "Body");
        await NextSecondAsync();
        var third = await CreatePromptAsync("Autotest third", "Body");
        await CreatePromptAsync("Autotest in folder", "Body", folder.Id);

        (await ListIdsAsync()).Should().Equal([third.Id, second.Id, first.Id], "newest first, and the foldered prompt is not in the root");
        (await ListIdsAsync(string.Empty)).Should().Equal([third.Id, second.Id, first.Id], "an empty folderId lists the root, not the whole library");
    }

    [Fact]
    public async Task List_FoldersDoNotMix_AndAnEmptyFolderListsNothing()
    {
        var folderA = await CreateFolderAsync("Autotest A");
        var folderB = await CreateFolderAsync("Autotest B");
        var emptyFolder = await CreateFolderAsync("Autotest empty");
        var root = await CreatePromptAsync("Autotest root", "Body");
        var inA = await CreatePromptAsync("Autotest in A", "Body", folderA.Id);
        var inB1 = await CreatePromptAsync("Autotest in B one", "Body", folderB.Id);
        await NextSecondAsync();
        var inB2 = await CreatePromptAsync("Autotest in B two", "Body", folderB.Id);

        (await ListIdsAsync(folderA.Id)).Should().Equal([inA.Id], "folder A");
        (await ListIdsAsync(folderB.Id)).Should().Equal([inB2.Id, inB1.Id], "folder B, newest first");
        (await ListIdsAsync()).Should().Equal([root.Id], "the root");
        (await ListIdsAsync(emptyFolder.Id)).Should().BeEmpty("an empty folder: the other listings above show the call is not just returning nothing");
    }

    [Fact]
    public async Task List_AnUpdateAMoveAndADelete_ShowUpInTheListings()
    {
        var folder = await CreateFolderAsync("Autotest home");
        var edited = await CreatePromptAsync("Autotest edited", "Old body");
        var moved = await CreatePromptAsync("Autotest moved", "Body");
        var deleted = await CreatePromptAsync("Autotest deleted", "Body");

        await _aiPrompts.AiPromptsUpdateAsync(
            new AiPromptsUpdateRequest(edited.Id, new AiPromptsUpdateRequestUpdates(name: "Autotest edited twice", text: "New body")), Token);
        await _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(moved.Id, folder.Id), Token);
        await _aiPrompts.AiPromptsDeleteAsync(deleted.Id, Token);

        var root = await _aiPrompts.AiPromptsListAsync(cancellationToken: Token);
        root.Select(p => p.Id).Should().Equal(edited.Id);
        root[0].Name.Should().Be("Autotest edited twice");
        root[0].Text.Should().Be("New body");
        (await ListIdsAsync(folder.Id)).Should().Equal(moved.Id);
    }

    [Fact]
    public async Task ListFolders_ListsEveryFolder_EmptyOnesToo_NewestFirst_AndFollowsRenameAndDelete()
    {
        var first = await CreateFolderAsync("Autotest first");
        await NextSecondAsync();
        var second = await CreateFolderAsync("Autotest second");
        await NextSecondAsync();
        var third = await CreateFolderAsync("Autotest third");
        await CreatePromptAsync("Autotest inside", "Body", second.Id);

        (await ListFolderIdsAsync()).Should().Equal([third.Id, second.Id, first.Id], "all three, the empty ones included, newest first");

        // Raw on purpose: the generated model cannot say that a folder carries nothing but these four fields.
        var (_, body) = await GetRawAsync("list-folders");
        foreach (var folder in ParseBody(body).EnumerateArray())
        {
            folder.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)
                .Should().Equal(["createdAt", "id", "name", "updatedAt"], folder.GetProperty("name").GetString());
        }

        await _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(second.Id, "Autotest renamed"), Token);
        await _aiPrompts.AiPromptsDeleteFolderAsync(first.Id, Token);

        var after = await _aiPrompts.AiPromptsListFoldersAsync(Token);
        after.Select(f => (f.Id, f.Name)).Should().Equal([(third.Id, "Autotest third"), (second.Id, "Autotest renamed")]);
    }
}
