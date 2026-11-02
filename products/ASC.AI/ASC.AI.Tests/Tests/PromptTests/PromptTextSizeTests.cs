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
/// "Save AI prompt to folder" in the editor's AI chat is a plain <c>prompts/create {name, text, folderId}</c>: the call is
/// synchronous, the row is stored and answered with, no model is called and no operation is queued. Reported as a
/// possible DoS: a 5000-character prompt text. The contract is that <c>name</c> is limited to 255 characters and
/// <c>text</c> is not (a 50 KB body is stored whole, see <see cref="PromptLifecycleTests"/>); these tests pin the
/// boundary values around 5000 for the text only, with exact lengths, in a folder. The role matrix, foreign-folder
/// isolation and the blank-text 400 are in the other suites of this folder.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Prompts")]
public class PromptTextSizeTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    private const string FolderName = "Autotest size folder";
    private const string Ascii = "abcdefghijklmnopqrstuvwxyz0123456789";
    private const string Cyrillic = "АБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжз";
    private const string Emoji = "😀🚀👨‍👩‍👧🇷🇺✨";
    private const string JsonSpecial = "\"\\/\b\f\t{}[]:,'<>&\0";

    public static TheoryData<string> Samples => new()
    {
        "ASCII",
        "Cyrillic",
        "one repeated character",
        "newlines (LF and CRLF)",
        "JSON special characters",
        "emoji, 5000 UTF-16 units",
        "emoji, 5000 code points"
    };

    /// <summary>Deterministic text of exactly <paramref name="codePoints"/> code points built from <paramref name="unit"/>.</summary>
    private static string ExactText(string unit, int codePoints)
    {
        var runes = unit.EnumerateRunes().ToArray();
        var builder = new System.Text.StringBuilder();

        for (var i = 0; i < codePoints; i++)
        {
            builder.Append(runes[i % runes.Length].ToString());
        }

        return builder.ToString();
    }

    private static string SampleText(string label) => label switch
    {
        "ASCII" => ExactText(Ascii, 5000),
        "Cyrillic" => ExactText(Cyrillic, 5000),
        "one repeated character" => new string('x', 5000),
        "newlines (LF and CRLF)" => ExactText("line one\nline two\r\n\n", 5000),
        "JSON special characters" => ExactText(JsonSpecial, 5000),
        // 5000 UTF-16 units but 2500 code points: the "5000" of the report depends on which length is meant.
        "emoji, 5000 UTF-16 units" => ExactText("😀", 2500),
        // 5000 code points = 10000 UTF-16 units = 20000 UTF-8 bytes for plain emoji.
        _ => ExactText(Emoji, 5000)
    };

    [Theory]
    [InlineData(1)]
    [InlineData(4999)]
    [InlineData(5000)]
    [InlineData(5001)]
    [InlineData(10000)]
    public async Task Create_AnAsciiTextOfExactLength_IsStoredWholeInAFolder(int size)
    {
        var folder = await CreateFolderAsync(FolderName);
        var text = ExactText(Ascii, size);
        text.Length.Should().Be(size, "the generator is exact");

        var created = await CreatePromptAsync($"Autotest {size}", text, folder.Id);

        var read = await _aiPrompts.AiPromptsGetByIdAsync(created.Id, Token);
        read.Text.Length.Should().Be(size, "not truncated");
        read.Text.Should().Be(text, "byte-for-byte");
        read.Name.Should().Be($"Autotest {size}");
        read.FolderId.Should().Be(folder.Id);
    }

    [Fact]
    public async Task Create_AnEmptyText_Is400_AndStoresNothing()
    {
        var folder = await CreateFolderAsync(FolderName);

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest empty", string.Empty, folder.Id), Token),
            null,
            400);

        (await ListIdsAsync(folder.Id)).Should().BeEmpty("the refused call stored nothing");
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task Create_FiveThousandCharacters_SurviveCreateGetListAndExport(string label)
    {
        var folder = await CreateFolderAsync(FolderName);
        var text = SampleText(label);

        var created = await CreatePromptAsync($"Autotest {label}", text, folder.Id);

        (await _aiPrompts.AiPromptsGetByIdAsync(created.Id, Token)).Text.Should().Be(text, "get-by-id returns the exact text");

        var inFolder = await _aiPrompts.AiPromptsListAsync(folder.Id, Token);
        inFolder.Single(p => p.Id == created.Id).Text.Should().Be(text, "list returns the exact text");

        var exported = await _aiPrompts.AiPromptsExportAsync(Token);
        exported.Prompts.Single(p => p.Id == created.Id).Text.Should().Be(text, "export returns the exact text");
    }

    [Fact]
    public async Task Create_OneSave_LeavesExactlyOnePrompt_InTheChosenFolder_WithTheChosenName()
    {
        var folder = await CreateFolderAsync(FolderName);

        var created = await CreatePromptAsync("Autotest single", ExactText(Ascii, 5000), folder.Id);

        (await ListIdsAsync(folder.Id)).Should().Equal(created.Id);
        (await ListIdsAsync()).Should().BeEmpty("nothing leaked to the root");
        (await _aiPrompts.AiPromptsListFoldersAsync(Token)).Should().HaveCount(1);

        var first = await _aiPrompts.AiPromptsGetByIdAsync(created.Id, Token);
        var second = await _aiPrompts.AiPromptsGetByIdAsync(created.Id, Token);
        second.ToJson().Should().Be(first.ToJson(), "a repeated read changes nothing");
    }

    [Fact]
    public async Task Create_AFiveThousandCharacterSave_AnswersPromptly_AndTheApiStaysResponsive()
    {
        var folder = await CreateFolderAsync(FolderName);

        var timings = new Dictionary<int, long>();

        foreach (var size in new[] { 1, 5000, 10000 })
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await _aiPrompts.AiPromptsCreateAsync(
                new AiCreatePromptInput($"Autotest timing {size}", ExactText(Ascii, size), folder.Id), Token);
            timings[size] = stopwatch.ElapsedMilliseconds;

            result.Success.Should().BeTrue($"size {size}");
        }

        // An absolute bound, not a ratio: a one-off timing ratio is noise. A multi-second answer for 10 KB would be a finding.
        timings[5000].Should().BeLessThan(5_000, "5000 characters, ms");
        timings[10000].Should().BeLessThan(5_000, "10000 characters, ms");

        // Related APIs still answer after the large writes.
        (await ListIdsAsync(folder.Id)).Should().HaveCount(3);
        (await _aiPrompts.AiPromptsListFoldersAsync(Token)).Should().HaveCount(1);
        (await _aiPrompts.AiPromptsExportAsync(Token)).Prompts.Should().HaveCount(3);

        await FilesHttpClient.Authenticate(Owner);
        var operations = (await OperationsApi.GetOperationStatusesAsync(cancellationToken: Token)).Response;
        operations.Where(op => !op.Finished).Should().BeEmpty("the save queued no background operation");
    }
}
