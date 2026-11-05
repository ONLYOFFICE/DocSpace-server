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
/// An exported library goes back in whole, in either mode, and a <c>merge</c> adds to a library without touching what is
/// there. Ids are re-issued by the import, so what survives a round trip is names, texts and which folder holds what.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Prompts")]
public class PromptImportRoundTripTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    [Fact]
    public async Task Replace_AnExportedLibrary_ComesBackWholeAfterTheLibraryWasChanged()
    {
        var folder = await SeedLibraryAsync();
        var original = await ReadLibraryAsync();
        var exported = await _aiPrompts.AiPromptsExportAsync(Token);

        // Change everything the export holds.
        await _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(folder.Id, "Autotest moved"), Token);
        await CreatePromptAsync("Autotest stray", "added afterwards");
        await _aiPrompts.AiPromptsDeleteAsync(original.Prompts[0].Id, Token);

        var answer = await ImportBundleAsync(exported, AiImportMode.Replace);

        answer.Success.Should().BeTrue();
        answer.Imported.Folders.Should().Be(original.Folders.Count);
        answer.Imported.Prompts.Should().Be(original.Prompts.Count);

        ShapeOf(await ReadLibraryAsync()).Should().BeEquivalentTo(ShapeOf(original), "names, texts and which folder holds what");
    }

    [Fact]
    public async Task Merge_AnExportedLibrary_IsRestoredIntoAnEmptiedOne_EmptyFoldersIncluded()
    {
        await SeedLibraryAsync();
        var original = await ReadLibraryAsync();
        var exported = await _aiPrompts.AiPromptsExportAsync(Token);

        foreach (var folder in original.Folders)
        {
            await _aiPrompts.AiPromptsDeleteFolderAsync(folder.Id, Token);
        }

        foreach (var prompt in await _aiPrompts.AiPromptsListAsync(cancellationToken: Token))
        {
            await _aiPrompts.AiPromptsDeleteAsync(prompt.Id, Token);
        }

        var emptied = await ReadLibraryAsync();
        emptied.Folders.Should().BeEmpty("the library really was emptied first");
        emptied.Prompts.Should().BeEmpty();

        var answer = await ImportBundleAsync(exported, AiImportMode.Merge);

        answer.Success.Should().BeTrue();
        answer.Imported.Folders.Should().Be(original.Folders.Count);
        answer.Imported.Prompts.Should().Be(original.Prompts.Count);

        var restored = await ReadLibraryAsync();
        ShapeOf(restored).Should().BeEquivalentTo(ShapeOf(original));
        restored.Folders.Select(f => f.Name).Should().Contain("Autotest empty", "the empty folder came back");
    }

    [Fact]
    public async Task Merge_ALibrarysOwnExportMergedIntoItself_IsRefusedWhole_AndChangesNothing()
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();
        var exported = await _aiPrompts.AiPromptsExportAsync(Token);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ImportBundleAsync(exported, AiImportMode.Merge));

        var body = exception.ErrorContent?.ToString();
        exception.ErrorCode.Should().Be(409, body);
        var answer = BodyOf(exception);
        answer.GetProperty("success").GetBoolean().Should().BeFalse(body);
        answer.GetProperty("errors").EnumerateArray().Select(e => e.GetProperty("ref").GetString())
            .Order(StringComparer.Ordinal)
            .Should().Equal(["Autotest SP", "Autotest SR"], "one error per colliding prompt");
        answer.TryGetProperty("imported", out _).Should().BeFalse(body);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task Merge_KeepsExistingEntriesByteIdentical_AndTheCountsMatchWhatWasAdded()
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();

        var result = await ImportAsync(
                [new AiPromptFolder("f1", "Autotest imported folder")],
                [new AiPrompt("p1", "Autotest imported root", "root body"), new AiPrompt("p2", "Autotest imported inside", "inside body", "f1")],
                AiImportMode.Merge);

        result.Success.Should().BeTrue();
        result.Imported.Folders.Should().Be(1);
        result.Imported.Prompts.Should().Be(2);

        var after = await ReadLibraryAsync();
        after.Folders.Should().HaveCount(before.Folders.Count + 1);
        after.Prompts.Should().HaveCount(before.Prompts.Count + 2);
        after.Folders.Should().Contain(before.Folders, "every existing folder is unchanged, timestamps included");
        after.Prompts.Should().Contain(before.Prompts, "every existing prompt is unchanged, timestamps included");
    }

    [Fact]
    public async Task Merge_ABundlePromptMayReuseANameFromAnotherFolder_AndA255CharacterNameImportsWhole()
    {
        await SeedLibraryAsync();
        var longName = new string('L', 255);

        // "Autotest SP" exists in folder S, not in this one.
        var result = await ImportAsync(
                [new AiPromptFolder("f1", "Autotest other folder")],
                [new AiPrompt("p1", "Autotest SP", "other body", "f1"), new AiPrompt("p2", longName, "long")],
                AiImportMode.Merge);

        result.Success.Should().BeTrue();

        var after = await ReadLibraryAsync();
        after.Prompts.Where(p => p.Name == "Autotest SP").Should().HaveCount(2, "the same name now lives in two folders");
        after.Prompts.Should().Contain(p => p.Name == longName);
    }

    [Fact]
    public async Task Merge_ABundleFolderNamedLikeAnExistingOne_DoesNotCreateASecondFolder()
    {
        var folder = await SeedLibraryAsync();
        var before = await ReadLibraryAsync();

        // The outcome is not pinned, only what it leaves behind.
        await IgnoreRefusalAsync(() => ImportAsync([new AiPromptFolder("f1", "AUTOTEST S")], [], AiImportMode.Merge));

        var after = await ReadLibraryAsync();
        after.Folders.Select(f => f.Name).Order(StringComparer.Ordinal)
            .Should().Equal(before.Folders.Select(f => f.Name).Order(StringComparer.Ordinal), "still one folder per name");
        after.Folders.Single(f => f.Id == folder.Id).Name.Should().Be("Autotest S", "the existing folder keeps its own spelling");
    }
}
