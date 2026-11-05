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
/// Field validation of <c>prompts/update</c>: a missing or malformed id, a blank or wrong-typed name and text, a folder
/// that is not there, and the name rules (unique per folder, case-insensitively, a prompt does not clash with itself).
/// Every refusal is followed by a full library snapshot: "refused" also means "untouched". A name that was not a
/// string used to be a 500 (BUG 84429). The name length limit is in <see cref="PromptStorageTests.PromptNameLengthTests"/>.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Prompts")]
public class PromptUpdateValidationTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    public static TheoryData<string, string> WrongTypes => new()
    {
        { "a number", "5" },
        { "a boolean", "true" },
        { "an array", "[\"a\"]" },
        { "an object", "{\"a\":1}" }
    };

    public static TheoryData<string, string> WrongTypedTexts => new()
    {
        { "an array", "[1]" },
        { "an object", "{\"a\":1}" }
    };

    private Task<(int Status, string Body)> UpdateFieldRawAsync(string id, string field, string valueJson) =>
        SendRawAsync(HttpMethod.Put, "update", $"{{\"id\":\"{id}\",\"updates\":{{\"{field}\":{valueJson}}}}}");

    [Fact]
    public async Task Update_MissingOrMalformedId_Is400()
    {
        await CreatePromptAsync("Autotest keep", "B");
        var before = await ReadLibraryAsync();

        // Raw on purpose: the typed request cannot be built without an id.
        var (missing, missingBody) = await SendRawAsync(HttpMethod.Put, "update", "{\"updates\":{\"name\":\"Autotest renamed\"}}");
        missing.Should().Be(400, $"id missing: {missingBody}");

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsUpdateAsync(
                new AiPromptsUpdateRequest("not-a-guid", new AiPromptsUpdateRequestUpdates(name: "Autotest renamed")), Token),
            null,
            400);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task Update_BlankName_IsRefused_AndThePromptKeepsItsOwn(string name)
    {
        var prompt = await CreatePromptAsync("Autotest keeper", "Body");
        var before = await ReadLibraryAsync();

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsUpdateAsync(new AiPromptsUpdateRequest(prompt.Id, new AiPromptsUpdateRequestUpdates(name: name)), Token),
            NameRequired,
            400);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_BlankText_Is400_AndThePromptKeepsItsOwn(string text)
    {
        var prompt = await CreatePromptAsync("Autotest keeper", "Body");
        var before = await ReadLibraryAsync();

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsUpdateAsync(new AiPromptsUpdateRequest(prompt.Id, new AiPromptsUpdateRequestUpdates(text: text)), Token),
            null,
            400);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Theory]
    [MemberData(nameof(WrongTypedTexts))]
    public async Task Update_ATextThatIsNotAString_Is400_AndThePromptKeepsItsOwn(string label, string textJson)
    {
        var prompt = await CreatePromptAsync("Autotest keeper", "Body");
        var before = await ReadLibraryAsync();

        var (status, body) = await UpdateFieldRawAsync(prompt.Id, "text", textJson);

        status.Should().Be(400, $"text is {label}: {body}");
        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Theory]
    [MemberData(nameof(WrongTypes))]
    [Trait("Bug", "84429")]
    public async Task Update_ANameThatIsNotAString_Is400_AndChangesNothing(string label, string nameJson)
    {
        var prompt = await CreatePromptAsync("Autotest keeper", "Body");
        var before = await ReadLibraryAsync();

        var (status, body) = await UpdateFieldRawAsync(prompt.Id, "name", nameJson);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, $"{label}: the refused call changed nothing");

        status.Should().Be(400, $"{label}: {body}");
    }

    [Theory]
    [InlineData(UnknownFolderId)]
    [InlineData("not-a-guid")]
    public async Task Update_AnUnknownOrMalformedFolderId_IsRefused_AndNothingMoves(string target)
    {
        var folder = await CreateFolderAsync("Autotest home");
        var prompt = await CreatePromptAsync("Autotest keeper", "Body", folder.Id);
        var before = await ReadLibraryAsync();

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsUpdateAsync(
                new AiPromptsUpdateRequest(prompt.Id, new AiPromptsUpdateRequestUpdates(name: "Autotest renamed too", folderId: target)), Token),
            $"Folder not found: {target}",
            404);

        // The rename in the same body must not have been applied half-way.
        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task Update_APromptMayTakeItsOwnNameBack_AndANameUsedInAnotherFolder()
    {
        var folder = await CreateFolderAsync("Autotest folder");
        var inFolder = await CreatePromptAsync("Autotest taken", "folder body", folder.Id);
        var inRoot = await CreatePromptAsync("Autotest free", "root body");

        // Another folder's name is not a conflict: the rule is per folder.
        var other = await _aiPrompts.AiPromptsUpdateAsync(
            new AiPromptsUpdateRequest(inRoot.Id, new AiPromptsUpdateRequestUpdates(name: "Autotest taken")), Token);
        other.Success.Should().BeTrue();
        (await _aiPrompts.AiPromptsGetByIdAsync(inRoot.Id, Token)).Name.Should().Be("Autotest taken");
        (await _aiPrompts.AiPromptsGetByIdAsync(inFolder.Id, Token)).Name.Should().Be("Autotest taken");

        // Its own name, even re-cased, is not a conflict with itself.
        foreach (var name in new[] { "Autotest taken", "AUTOTEST TAKEN" })
        {
            var self = await _aiPrompts.AiPromptsUpdateAsync(
                new AiPromptsUpdateRequest(inRoot.Id, new AiPromptsUpdateRequestUpdates(name: name)), Token);
            self.Success.Should().BeTrue($"to {name}");
        }

        (await _aiPrompts.AiPromptsGetByIdAsync(inRoot.Id, Token)).Name.Should().Be("AUTOTEST TAKEN");
    }

    [Fact]
    public async Task Update_ACaseVariantOfASiblingsName_IsRefused()
    {
        await CreatePromptAsync("Autotest Sibling", "First");
        var second = await CreatePromptAsync("Autotest second", "Second");
        var before = await ReadLibraryAsync();

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsUpdateAsync(
                new AiPromptsUpdateRequest(second.Id, new AiPromptsUpdateRequestUpdates(name: "AUTOTEST SIBLING")), Token),
            PromptNameTaken,
            400,
            409);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }
}
