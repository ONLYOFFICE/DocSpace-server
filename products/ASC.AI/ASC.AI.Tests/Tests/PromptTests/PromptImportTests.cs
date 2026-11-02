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
/// <c>POST prompts/import-bundle</c>: the counts it reports, what a bundle's ids are worth, the difference between
/// <c>merge</c> (add to the library) and <c>replace</c> (wipe it first), and the collision report of BUG 83130. What
/// happens to the library when a bundle is refused is in <see cref="PromptImportRefusalTests"/> and
/// <see cref="PromptImportValidationTests"/>.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Prompts")]
public class PromptImportTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    [Fact]
    public async Task Import_Prompts_ReportsTheCounts_AndIssuesItsOwnIds()
    {
        var result = await ImportAsync(
                [],
                [new AiPrompt("ignored-1", "Autotest imported one", "Body one"), new AiPrompt("ignored-2", "Autotest imported two", "Body two")],
                null);

        result.Success.Should().BeTrue();
        result.Imported.Prompts.Should().Be(2);
        result.Imported.Folders.Should().Be(0);

        var listed = await _aiPrompts.AiPromptsListAsync(cancellationToken: Token);
        listed.Select(p => p.Name).Should().BeEquivalentTo(["Autotest imported one", "Autotest imported two"]);

        // The ids in the bundle are not honoured: the portal issues its own, so an import cannot plant a chosen id.
        listed.Select(p => p.Id).Should().NotContain(["ignored-1", "ignored-2"]);
    }

    [Fact]
    public async Task ExportThenImport_ABundleRoundTrips()
    {
        var folder = await CreateFolderAsync("Autotest bundle");
        await CreatePromptAsync("Autotest bundled", "Bundled body", folder.Id);

        // The export goes back verbatim: the typed bundle's version is a one-member enum that does not survive a read.
        var exported = await ExportRawAsync();

        // Re-importing the caller's own bundle must not fail on the names that are already there; with no options the
        // import replaces the library, so whatever the merge rule is, the call has to be answered.
        var (status, body) = await ImportBundleRawAsync(exported, null);

        status.Should().Be(200, body);
        ParseBody(body).GetProperty("success").GetBoolean().Should().BeTrue(body);
        ShapeOf(await ReadLibraryAsync()).Prompts.Should().Equal("Autotest bundle/Autotest bundled: Bundled body");
    }

    /// <summary><c>options.mode</c> decides whether the library is added to or wiped: the difference between a merge and data loss.</summary>
    [Fact]
    public async Task Import_ModeMerge_KeepsWhatIsAlreadyThere()
    {
        var existingFolder = await CreateFolderAsync("Autotest existing folder");
        var existing = await CreatePromptAsync("Autotest existing", "Existing body", existingFolder.Id);

        var result = await ImportAsync(
                [new AiPromptFolder("ignored-folder", "Autotest imported folder")],
                [new AiPrompt("ignored-prompt", "Autotest imported", "Imported body", "ignored-folder")],
                AiImportMode.Merge);

        result.Success.Should().BeTrue();
        result.Imported.Prompts.Should().Be(1);
        result.Imported.Folders.Should().Be(1);

        // Both libraries are there afterwards, and the imported prompt landed inside the imported folder rather than
        // in the root: the bundle's own folder ids are re-issued, so the binding has to be remapped, not dropped.
        var folders = await _aiPrompts.AiPromptsListFoldersAsync(Token);
        folders.Select(f => f.Name).Should().BeEquivalentTo(["Autotest existing folder", "Autotest imported folder"]);
        (await ListIdsAsync(existingFolder.Id)).Should().Equal([existing.Id], "the prompt that was already there");

        var importedFolder = folders.Single(f => f.Name == "Autotest imported folder");
        var inside = await _aiPrompts.AiPromptsListAsync(importedFolder.Id, Token);
        inside.Select(p => p.Name).Should().Equal("Autotest imported");
        inside[0].Text.Should().Be("Imported body");
    }

    [Fact]
    public async Task Import_ModeReplace_DropsTheExistingLibrary()
    {
        var doomedFolder = await CreateFolderAsync("Autotest doomed folder");
        var doomed = await CreatePromptAsync("Autotest doomed", "Doomed body", doomedFolder.Id);

        var result = await ImportAsync([], [new AiPrompt("ignored", "Autotest replacement", "New body")], AiImportMode.Replace);

        result.Success.Should().BeTrue();
        result.Imported.Prompts.Should().Be(1);

        // Replace means replace: the previous prompt and its folder are gone, and the library is exactly the bundle.
        // It is destructive by design, which is why it is pinned rather than left to a client's assumption.
        (await FindPromptAsync(doomed.Id)).Should().BeNull();
        (await FindFolderAsync(doomedFolder.Id)).Should().BeNull();
        (await _aiPrompts.AiPromptsListFoldersAsync(Token)).Should().BeEmpty();
        (await _aiPrompts.AiPromptsListAsync(cancellationToken: Token)).Select(p => p.Name).Should().Equal("Autotest replacement");
    }

    /// <summary>A bundle entry that collides with an existing name used to be written anyway; now it is reported and nothing is written.</summary>
    [Fact]
    [Trait("Bug", "83130")]
    public async Task Import_ModeMerge_ACollidingNameIsReported_AndNothingIsWritten()
    {
        var existing = await CreatePromptAsync("Autotest collision", "Existing body");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ImportAsync(
                [],
                [new AiPrompt("ignored-1", "Autotest collision", "Bundle body"), new AiPrompt("ignored-2", "Autotest fresh", "Fresh body")],
                AiImportMode.Merge));

        exception.ErrorCode.Should().Be(409);

        // Control: create still refuses a duplicate name, so the library really has a uniqueness rule for the import
        // to report against.
        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest collision", "Another body"), Token),
            PromptNameTaken,
            400,
            409);

        var listed = await _aiPrompts.AiPromptsListAsync(cancellationToken: Token);
        listed.Single(p => p.Id == existing.Id).Text.Should().Be("Existing body", "the existing prompt was not overwritten");

        var body = BodyOf(exception);
        body.GetProperty("success").GetBoolean().Should().BeFalse();
        var errors = body.GetProperty("errors");
        errors.GetArrayLength().Should().Be(1);
        errors[0].GetProperty("kind").GetString().Should().Be("prompt");
        errors[0].GetProperty("ref").GetString().Should().Contain("Autotest collision");

        // Nothing was written: not the colliding entry, and not the clean one that shared the bundle with it.
        listed.Select(p => p.Id).Should().Equal(existing.Id);
    }
}
