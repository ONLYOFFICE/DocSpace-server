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
/// Shared plumbing of the prompt access suites: the owner's library seeded before anyone else exists, a call for each of
/// the thirteen prompt routes (and the writes with the widest reach), and the check that a refused caller left that
/// library as it was. Saved prompts are a per-user store, so the matrix is short: every member owns their own library, a
/// Guest has none, and an anonymous caller gets 401.
/// </summary>
public abstract class PromptsPermissionTestBase(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    protected const string OwnerPromptName = "Autotest owner prompt";
    protected const string OwnerFolderName = "Autotest folder";

    public static TheoryData<string> Routes => new()
    {
        "list",
        "get-by-id",
        "create",
        "update",
        "move",
        "delete",
        "list-folders",
        "get-folder-by-id",
        "create-folder",
        "rename-folder",
        "delete-folder",
        "export",
        "import-bundle"
    };

    /// <summary>The owner's prompt and folder, created before the caller under test exists.</summary>
    protected sealed record OwnerSeed(AiPrompt Prompt, AiPromptFolder Folder);

    protected async Task<OwnerSeed> SeedOwnerAsync()
    {
        var prompt = await CreatePromptAsync(OwnerPromptName, "Owner body");
        var folder = await CreateFolderAsync(OwnerFolderName);

        return new OwnerSeed(prompt, folder);
    }

    /// <summary>The owner's ids give the id-taking routes something real to aim at: a refusal of a made-up id could not tell "refused" from "not there".</summary>
    protected Func<Task> CallRoute(string route, OwnerSeed seed)
    {
        var promptId = seed.Prompt.Id;
        var folderId = seed.Folder.Id;

        return route switch
        {
            "list" => () => _aiPrompts.AiPromptsListAsync(cancellationToken: Token),
            "get-by-id" => () => _aiPrompts.AiPromptsGetByIdAsync(promptId, Token),
            "create" => () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest", "Body"), Token),
            "update" => () => _aiPrompts.AiPromptsUpdateAsync(
                new AiPromptsUpdateRequest(promptId, new AiPromptsUpdateRequestUpdates(name: "Autotest hijacked")), Token),
            "move" => () => _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(promptId, folderId), Token),
            "delete" => () => _aiPrompts.AiPromptsDeleteAsync(promptId, Token),
            "list-folders" => () => _aiPrompts.AiPromptsListFoldersAsync(Token),
            "get-folder-by-id" => () => _aiPrompts.AiPromptsGetFolderByIdAsync(folderId, Token),
            "create-folder" => () => _aiPrompts.AiPromptsCreateFolderAsync("Autotest", Token),
            "rename-folder" => () => _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(folderId, "Autotest renamed"), Token),
            "delete-folder" => () => _aiPrompts.AiPromptsDeleteFolderAsync(folderId, Token),
            "export" => () => _aiPrompts.AiPromptsExportAsync(Token),
            _ => () => ImportAsync([], [], null)
        };
    }

    /// <summary>
    /// The writes with the widest reach: create, rename, move, both deletes and, the dangerous one, a replace-import, which
    /// would empty the library if it got through.
    /// </summary>
    protected IEnumerable<(string Label, Func<Task> Call)> WritesAimedAt(OwnerSeed seed)
    {
        var promptId = seed.Prompt.Id;
        var folderId = seed.Folder.Id;

        return
        [
            ("create", () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest planted", "x"), Token)),
            ("create in the folder", () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest planted", "x", folderId), Token)),
            ("create-folder", () => _aiPrompts.AiPromptsCreateFolderAsync("Autotest planted", Token)),
            (
                "update",
                () => _aiPrompts.AiPromptsUpdateAsync(
                    new AiPromptsUpdateRequest(promptId, new AiPromptsUpdateRequestUpdates(name: "Autotest hijacked", text: "Hijacked")), Token)),
            ("move to the root", () => _aiPrompts.AiPromptsMoveAsync(MoveToRoot(promptId), Token)),
            ("rename-folder", () => _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(folderId, "Autotest hijacked"), Token)),
            ("delete", () => _aiPrompts.AiPromptsDeleteAsync(promptId, Token)),
            ("delete-folder", () => _aiPrompts.AiPromptsDeleteFolderAsync(folderId, Token)),
            (
                "import-bundle merge",
                () => ImportAsync([new AiPromptFolder("f", "Autotest planted")], [new AiPrompt("p", "Autotest planted", "x")], AiImportMode.Merge)),
            (
                "import-bundle replace",
                () => ImportAsync([], [new AiPrompt("p", "Autotest planted", "x")], AiImportMode.Replace))
        ];
    }

    /// <summary>A seed for the byte-identical checks: a prompt inside a folder, and one at the root.</summary>
    protected async Task<OwnerSeed> SeedOwnerWithFolderPromptAsync()
    {
        var folder = await CreateFolderAsync("Autotest owner");
        var prompt = await CreatePromptAsync(OwnerPromptName, "Owner body", folder.Id);
        await CreatePromptAsync("Autotest owner root", "Owner root body");

        return new OwnerSeed(prompt, folder);
    }

    /// <summary>Signs the prompts client back in as the owner and checks the owner's prompt and folder are as they were.</summary>
    protected async Task ExpectOwnerSeedIntactAsync(OwnerSeed seed)
    {
        await _aiChatClient.Authenticate(Owner);

        var prompt = await _aiPrompts.AiPromptsGetByIdAsync(seed.Prompt.Id, Token);
        prompt.Name.Should().Be(OwnerPromptName);
        prompt.FolderId.Should().BeNull();
        (await _aiPrompts.AiPromptsGetFolderByIdAsync(seed.Folder.Id, Token)).Name.Should().Be(OwnerFolderName);
    }

    /// <summary>Every write is refused with <paramref name="status"/> and the owner's library is exactly what it was.</summary>
    protected async Task ExpectWritesRefusedAsync(int status)
    {
        var seed = await SeedOwnerWithFolderPromptAsync();
        var before = await ReadLibraryAsync();
        before.Prompts.Should().HaveCount(2, "something a replace-import could wipe");

        await ActAsRefusedCallerAsync();

        foreach (var (label, call) in WritesAimedAt(seed))
        {
            var exception = await Assert.ThrowsAsync<ApiException>(call);
            exception.ErrorCode.Should().Be(status, label);
        }

        await RestoreOwnerAsync();
        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    /// <summary>Makes the caller of the following requests the refused one (anonymous, a Guest, AI switched off).</summary>
    protected virtual Task ActAsRefusedCallerAsync() => throw new NotSupportedException();

    /// <summary>Puts the portal and the client back the way the owner reads it.</summary>
    protected virtual async Task RestoreOwnerAsync() => await _aiChatClient.Authenticate(Owner);
}
