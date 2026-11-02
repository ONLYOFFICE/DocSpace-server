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
/// <c>GET prompts/export</c>: the caller's whole library as one versioned bundle, exactly as the other routes read it,
/// empty folders included, and nothing that was deleted.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Prompts")]
public class PromptExportTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    private static PromptRow RowOf(AiPrompt p) => new(p.Id, p.Name, p.Text, p.FolderId, p.CreatedAt, p.UpdatedAt);

    private static FolderRow RowOf(AiPromptFolder f) => new(f.Id, f.Name, f.CreatedAt, f.UpdatedAt);

    [Fact]
    public async Task Export_ExportsTheCallersPromptsAndFolders()
    {
        // The version is a one-member enum in the SDK, so it is read off the wire.
        var empty = ParseBody(await ExportRawAsync());
        empty.GetProperty("version").GetInt32().Should().Be(1);
        empty.GetProperty("prompts").GetArrayLength().Should().Be(0);
        empty.GetProperty("folders").GetArrayLength().Should().Be(0);

        var folder = await CreateFolderAsync("Autotest folder");
        var rootPrompt = await CreatePromptAsync("Autotest root", "Root body");
        var foldered = await CreatePromptAsync("Autotest foldered", "Foldered body", folder.Id);

        var bundle = await _aiPrompts.AiPromptsExportAsync(Token);

        ParseBody(await ExportRawAsync()).GetProperty("version").GetInt32().Should().Be(1);
        bundle.Folders.Select(f => f.Id).Should().Equal(folder.Id);
        bundle.Prompts.Select(p => p.Id).Should().BeEquivalentTo([rootPrompt.Id, foldered.Id]);

        // The bundle keeps the folder binding, which is what makes it re-importable.
        bundle.Prompts.Single(p => p.Id == foldered.Id).FolderId.Should().Be(folder.Id);
    }

    [Fact]
    public async Task Export_CarriesEveryPromptAndFolder_ExactlyAsTheOtherRoutesReadThem()
    {
        var emptyFolder = await CreateFolderAsync("Autotest empty");
        var fullFolder = await CreateFolderAsync("Autotest 📁 full");
        var root = await CreatePromptAsync("Autotest root", "# Root\n\n- one\n- two");
        var inside = await CreatePromptAsync("Autotest 提示", "Строка с \"кавычками\" и \\ слешем\n{\"k\": [1, 2]}", fullFolder.Id);

        using var response = await _aiChatClient.GetAsync("/api/2.0/ai/prompts/export", Token);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/json");

        var bundle = await _aiPrompts.AiPromptsExportAsync(Token);

        bundle.Folders.Select(f => f.Id).Should().BeEquivalentTo([emptyFolder.Id, fullFolder.Id], "an empty folder is exported too");

        foreach (var id in new[] { root.Id, inside.Id })
        {
            var read = await _aiPrompts.AiPromptsGetByIdAsync(id, Token);
            RowOf(bundle.Prompts.Single(p => p.Id == id)).Should().BeEquivalentTo(RowOf(read), $"prompt {read.Name} against get-by-id");
        }

        foreach (var folder in bundle.Folders)
        {
            var read = await _aiPrompts.AiPromptsGetFolderByIdAsync(folder.Id, Token);
            RowOf(folder).Should().BeEquivalentTo(RowOf(read), $"folder {folder.Name} against get-folder-by-id");
        }
    }

    [Fact]
    public async Task Export_OnlyEmptyFolders_AndOnlyRootPrompts()
    {
        var folder = await CreateFolderAsync("Autotest empty");

        var onlyFolders = await _aiPrompts.AiPromptsExportAsync(Token);
        onlyFolders.Folders.Select(f => f.Id).Should().Equal(folder.Id);
        onlyFolders.Prompts.Should().BeEmpty();

        await _aiPrompts.AiPromptsDeleteFolderAsync(folder.Id, Token);
        var prompt = await CreatePromptAsync("Autotest root", "Body");

        var onlyPrompts = await _aiPrompts.AiPromptsExportAsync(Token);
        onlyPrompts.Folders.Should().BeEmpty();
        onlyPrompts.Prompts.Select(p => p.Id).Should().Equal(prompt.Id);
        onlyPrompts.Prompts[0].FolderId.Should().BeNull();
    }

    [Fact]
    public async Task Export_LeavesOutDeletedPromptsAndFolders_AndRepeatsUnchanged()
    {
        var keptFolder = await CreateFolderAsync("Autotest kept");
        var doomedFolder = await CreateFolderAsync("Autotest doomed");
        var keptRoot = await CreatePromptAsync("Autotest kept root", "Body");
        var keptInside = await CreatePromptAsync("Autotest kept inside", "Body", keptFolder.Id);
        var deletedRoot = await CreatePromptAsync("Autotest deleted root", "Body");
        await CreatePromptAsync("Autotest doomed inside", "Body", doomedFolder.Id);

        await _aiPrompts.AiPromptsDeleteAsync(deletedRoot.Id, Token);
        await _aiPrompts.AiPromptsDeleteFolderAsync(doomedFolder.Id, Token);

        var exported = await ReadLibraryAsync();

        exported.Folders.Select(f => f.Id).Should().Equal(keptFolder.Id);
        exported.Prompts.Select(p => p.Id).Should().BeEquivalentTo([keptRoot.Id, keptInside.Id]);
        exported.Prompts.Should().OnlyContain(p => p.FolderId == null || p.FolderId == keptFolder.Id, "no prompt points at the deleted folder");

        (await ReadLibraryAsync()).Should().BeEquivalentTo(exported, "a second export is identical");
    }
}
