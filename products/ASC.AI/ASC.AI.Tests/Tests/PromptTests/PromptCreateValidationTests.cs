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
/// Field validation of <c>prompts/create</c>: a name or text of the wrong type is a 400 (until BUG 84429 was fixed a
/// name that was missing, null or not a string was a 500), what text content is stored verbatim, and the name rules:
/// unique per folder, case-insensitively, not trimmed. The name length limit is in
/// <see cref="PromptStorageTests.PromptNameLengthTests"/>.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Prompts")]
public class PromptCreateValidationTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    public static TheoryData<string, string> BadNames => BadValues("name");

    public static TheoryData<string, string> BadTexts => BadValues("text");

    [Theory]
    [MemberData(nameof(BadNames))]
    [Trait("Bug", "84429")]
    public async Task Create_ANameThatIsNotAString_Is400_AndStoresNothing(string label, string nameMember)
    {
        var (status, body) = await SendRawAsync(HttpMethod.Post, "create", ObjectOf(nameMember, "\"text\":\"Body\""));

        (await _aiPrompts.AiPromptsListAsync(cancellationToken: Token)).Should().BeEmpty($"{label}: the refused call stored nothing");

        status.Should().Be(400, $"{label}: {body}");
    }

    [Theory]
    [MemberData(nameof(BadTexts))]
    public async Task Create_ATextThatIsNotAString_Is400_AndStoresNothing(string label, string textMember)
    {
        var (status, body) = await SendRawAsync(HttpMethod.Post, "create", ObjectOf($"\"name\":\"Autotest text {label}\"", textMember));

        status.Should().Be(400, $"text is {label}: {body}");
        (await _aiPrompts.AiPromptsListAsync(cancellationToken: Token)).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_TheShortestNameAndText_AreAccepted()
    {
        var prompt = await CreatePromptAsync("q", "z");

        var read = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        read.Name.Should().Be("q");
        read.Text.Should().Be("z");
    }

    [Fact]
    public async Task Create_WhitespaceQuotesJsonAndCrlfInTheText_ComeBackUnchanged()
    {
        var text = string.Join(
            "\n",
            "  leading and trailing  ",
            "\ttabbed\tline",
            "windows\r\nline break",
            "quote \" apostrophe ' backslash \\ end",
            "{\"json\": [1, 2, {\"k\": null}], \"s\": \"a\\nb\"}",
            "trailing newline\n");

        var prompt = await CreatePromptAsync("Autotest whitespace", text);

        (await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token)).Text.Should().Be(text);
        (await _aiPrompts.AiPromptsExportAsync(Token)).Prompts[0].Text.Should().Be(text, "and through export");
    }

    [Fact]
    public async Task Create_CyrillicAndCjkNamesAndTexts_RoundTrip()
    {
        const string name = "Автотест: краткое изложение 总结";
        const string text = "Сделай краткое изложение документа. 请用中文回答。";

        var prompt = await CreatePromptAsync(name, text);

        var read = await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token);
        read.Name.Should().Be(name);
        read.Text.Should().Be(text);
    }

    [Fact]
    public async Task Create_InAFolder_IsBoundToItAndOnlyListedThere()
    {
        var folder = await CreateFolderAsync("Autotest home");

        var created = await CreatePromptAsync("Autotest inside", "Body", folder.Id);

        created.FolderId.Should().Be(folder.Id, "the create response");
        (await _aiPrompts.AiPromptsGetByIdAsync(created.Id, Token)).FolderId.Should().Be(folder.Id, "get-by-id");
        (await ListIdsAsync(folder.Id)).Should().Equal([created.Id], "the folder listing");
        (await ListIdsAsync()).Should().BeEmpty("the root listing does not carry it");
    }

    [Fact]
    public async Task Create_OneNameIsAllowedInTheRootAndInTwoFolders_AndMayEqualAFolderName()
    {
        var folderA = await CreateFolderAsync("Autotest shared");
        var folderB = await CreateFolderAsync("Autotest other");

        // The uniqueness rule is per folder, so the same name lives in three scopes.
        var inRoot = await CreatePromptAsync("Autotest shared", "root body");
        var inA = await CreatePromptAsync("Autotest shared", "A body", folderA.Id);
        var inB = await CreatePromptAsync("Autotest shared", "B body", folderB.Id);

        (await _aiPrompts.AiPromptsGetByIdAsync(inRoot.Id, Token)).Text.Should().Be("root body");
        (await _aiPrompts.AiPromptsGetByIdAsync(inA.Id, Token)).Text.Should().Be("A body");
        (await _aiPrompts.AiPromptsGetByIdAsync(inB.Id, Token)).Text.Should().Be("B body");
        (await ListIdsAsync(folderA.Id)).Should().Equal(inA.Id);
        (await ListIdsAsync(folderB.Id)).Should().Equal(inB.Id);

        // A prompt may also share its name with a folder: separate namespaces.
        (await _aiPrompts.AiPromptsCreateFolderAsync("Autotest shared prompt", Token)).Success.Should().BeTrue();
        (await _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest shared prompt", "Body"), Token)).Success.Should().BeTrue();
    }

    [Fact]
    public async Task Create_TheNameIsUniqueCaseInsensitively_InsideAFolderToo()
    {
        var folder = await CreateFolderAsync("Autotest folder");
        var root = await CreatePromptAsync("Autotest Case", "root body");
        var inFolder = await CreatePromptAsync("Autotest Case", "folder body", folder.Id);
        var before = await ReadLibraryAsync();

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("AUTOTEST CASE", "x"), Token),
            PromptNameTaken,
            400,
            409);
        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("autotest case", "x", folder.Id), Token),
            PromptNameTaken,
            400,
            409);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, "neither refusal wrote anything");
        before.Prompts.Select(p => p.Id).Should().BeEquivalentTo([root.Id, inFolder.Id]);
    }
}
