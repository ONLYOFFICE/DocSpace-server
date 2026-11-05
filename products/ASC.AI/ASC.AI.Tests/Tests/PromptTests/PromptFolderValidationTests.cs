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
/// Field validation of <c>create-folder</c> and <c>rename-folder</c>: a body or name of the wrong type is a 400 (until
/// BUG 84429 was fixed it was a 500), folder names are unique case-insensitively and are not trimmed, a folder does not
/// clash with itself. The name length limit is in <see cref="PromptFolderStorageTests.PromptFolderNameLengthTests"/>.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/PromptFolders")]
public class PromptFolderValidationTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    public static TheoryData<string, string> BadBodies => new()
    {
        { "null", "null" },
        { "a number", "5" },
        { "a boolean", "true" },
        { "an array", "[\"a\"]" },
        { "an object without a name", "{}" },
        { "an object with a null name", "{\"name\":null}" },
        { "an object with a numeric name", "{\"name\":5}" }
    };

    public static TheoryData<string, string> BadNames => BadValues("name");

    [Theory]
    [MemberData(nameof(BadBodies))]
    [Trait("Bug", "84429")]
    public async Task CreateFolder_ABodyThatIsNotAName_Is400_AndCreatesNothing(string label, string bodyJson)
    {
        // Raw on purpose: the typed call takes the name as a string.
        var (status, body) = await SendRawAsync(HttpMethod.Post, "create-folder", bodyJson);

        (await _aiPrompts.AiPromptsListFoldersAsync(Token)).Should().BeEmpty($"{label}: the refused call created nothing");

        status.Should().Be(400, $"{label}: {body}");
    }

    [Fact]
    public async Task CreateFolder_OneCharacterIsEnough()
    {
        var folder = await CreateFolderAsync("z");

        (await FindFolderAsync(folder.Id))!.Name.Should().Be("z");
    }

    [Fact]
    public async Task CreateFolder_TheNameIsUniqueCaseInsensitively()
    {
        var folder = await CreateFolderAsync("Autotest Alpha");
        var before = await ReadLibraryAsync();

        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsCreateFolderAsync("AUTOTEST ALPHA", Token), FolderNameTaken, 400, 409);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, "the refused case variant created nothing");
        before.Folders.Select(f => f.Id).Should().Equal(folder.Id);
    }

    [Theory]
    [MemberData(nameof(BadNames))]
    [Trait("Bug", "84429")]
    public async Task RenameFolder_ANameThatIsNotAString_Is400_AndChangesNothing(string label, string nameMember)
    {
        var folder = await CreateFolderAsync("Autotest keeper");
        var before = await ReadLibraryAsync();

        var (status, body) = await SendRawAsync(HttpMethod.Put, "rename-folder", ObjectOf($"\"id\":\"{folder.Id}\"", nameMember));

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, $"{label}: the refused rename changed nothing");

        status.Should().Be(400, $"{label}: {body}");
    }

    [Fact]
    public async Task RenameFolder_MayTakeItsOwnNameBack_ReCased_OrUnicode()
    {
        var folder = await CreateFolderAsync("Autotest folder");
        var prompt = await CreatePromptAsync("Autotest inside", "Body", folder.Id);

        foreach (var name in new[] { "Autotest folder", "AUTOTEST FOLDER", "Папка для промптов 📁 提示" })
        {
            var renamed = await _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(folder.Id, name), Token);

            renamed.Success.Should().BeTrue($"to {name}");
            (await FindFolderAsync(folder.Id))!.Name.Should().Be(name);
        }

        (await ListIdsAsync(folder.Id)).Should().Equal([prompt.Id], "the prompt stayed in the renamed folder");
    }

    [Fact]
    public async Task RenameFolder_ACaseVariantOfAnotherFoldersName_IsRefused_AndNothingChanges()
    {
        await CreateFolderAsync("Autotest Alpha");
        var second = await CreateFolderAsync("Autotest Beta");
        var before = await ReadLibraryAsync();

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(second.Id, "AUTOTEST ALPHA"), Token),
            FolderNameTaken,
            400,
            409);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }
}
