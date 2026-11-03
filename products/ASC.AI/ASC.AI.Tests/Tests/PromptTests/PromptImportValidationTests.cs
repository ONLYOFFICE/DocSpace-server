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
/// A bundle the validator refuses with a hard 400 writes nothing and, in <c>replace</c>, deletes nothing. Until BUG
/// 84421 and 84422 were fixed the validation ran while the bundle was applied: <c>replace</c> deleted the library first
/// and then failed, <c>merge</c> kept the entries before the bad one. A bundle of the wrong shape was a 500 (BUG 84423),
/// and twins in a bundle were written as duplicates (BUG 84424, 84425). Blank and over-long names and texts go through
/// the typed model; only the bundles it cannot build (a prompt without a text, entries of the wrong shape) are written
/// by hand.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Prompts")]
public class PromptImportValidationTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    public static TheoryData<string> BadEntries => new()
    {
        "a prompt with a blank name",
        "a prompt with a blank text",
        "a prompt name of 256 characters",
        "a folder with a blank name",
        "a folder name of 256 characters"
    };

    public static TheoryData<string> ReplaceRefusals => new()
    {
        "a prompt with a blank name",
        "a prompt with a blank text",
        "a valid entry followed by an invalid one"
    };

    /// <summary>The folders and prompts of the bundle a <see cref="BadEntries"/> or <see cref="ReplaceRefusals"/> row names.</summary>
    private static (List<AiPromptFolder> Folders, List<AiPrompt> Prompts) BundleOf(string label) => label switch
    {
        "a prompt with a blank name" => ([], [new AiPrompt("a", string.Empty, "t")]),
        "a prompt with a blank text" => ([], [new AiPrompt("a", "n", string.Empty)]),
        "a prompt name of 256 characters" => ([], [new AiPrompt("a", new string('N', 256), "t")]),
        "a folder with a blank name" => ([new AiPromptFolder("a", string.Empty)], []),
        "a folder name of 256 characters" => ([new AiPromptFolder("a", new string('N', 256))], []),
        "a valid entry followed by an invalid one" => ([], MixedPrompts),
        _ => throw new ArgumentOutOfRangeException(nameof(label), label, null)
    };

    private static List<AiPrompt> MixedPrompts => [new("ok", "Autotest valid first", "t"), new("bad", "Autotest invalid second", string.Empty)];

    /// <summary>The members of the request body; the options are appended by the test.</summary>
    public static TheoryData<string, string> Malformed => new()
    {
        { "a prompt that is a string", "\"bundle\":{\"version\":1,\"folders\":[],\"prompts\":[\"x\"]}" },
        { "a folder that is a number", "\"bundle\":{\"version\":1,\"folders\":[5],\"prompts\":[]}" },
        { "a prompt without a name", "\"bundle\":{\"version\":1,\"folders\":[],\"prompts\":[{\"id\":\"a\",\"text\":\"t\"}]}" },
        { "no bundle at all", string.Empty },
        { "a null bundle", "\"bundle\":null" },
        { "a bundle without folders", "\"bundle\":{\"version\":1,\"prompts\":[]}" },
        { "a bundle without prompts", "\"bundle\":{\"version\":1,\"folders\":[]}" },
        { "a bundle whose folders is a string", "\"bundle\":{\"version\":1,\"folders\":\"x\",\"prompts\":[]}" },
        { "a bundle whose prompts is an object", "\"bundle\":{\"version\":1,\"folders\":[],\"prompts\":{}}" }
    };

    [Theory]
    [MemberData(nameof(BadEntries))]
    public async Task Merge_ABundleWithASingleInvalidEntry_Is400_AndWritesNothing(string label)
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();
        var (folders, prompts) = BundleOf(label);

        await ExpectRefusalAsync(() => ImportAsync(folders, prompts, AiImportMode.Merge), null, 400);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, $"{label}: the library");
    }

    [Fact]
    public async Task Merge_APromptWithoutAText_Is400_AndWritesNothing()
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();

        // Raw: the typed prompt refuses a null text client-side, so a prompt without one cannot be built.
        var (status, body) = await ImportBundleRawAsync(BundleJson(string.Empty, "{\"id\":\"a\",\"name\":\"n\"}"), "merge");

        status.Should().Be(400, body);
        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, "the library");
    }

    [Fact]
    [Trait("Bug", "84421")]
    public async Task Merge_AnInvalidEntryAfterAValidOne_Is400_AndNothingIsWritten()
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();

        var refused = await ExpectRefusalAsync(() => ImportAsync([], MixedPrompts, AiImportMode.Merge), null, 400);

        BodyOf(refused).GetProperty("success").GetBoolean().Should().BeFalse(refused.ErrorContent?.ToString());

        // "a corrupt bundle is rejected whole rather than applied halfway".
        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Theory]
    [MemberData(nameof(ReplaceRefusals))]
    [Trait("Bug", "84422")]
    public async Task Replace_AnInvalidBundle_Is400_AndTheExistingLibraryIsKept(string label)
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();
        before.Prompts.Should().NotBeEmpty("there is something to lose");
        var (folders, prompts) = BundleOf(label);

        var refused = await ExpectRefusalAsync(() => ImportAsync(folders, prompts, AiImportMode.Replace), null, 400);

        BodyOf(refused).GetProperty("success").GetBoolean().Should().BeFalse($"{label}: {refused.ErrorContent}");

        // replace is documented as destructive, but a refused bundle must not have destroyed anything.
        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Theory]
    [MemberData(nameof(Malformed))]
    [Trait("Bug", "84423")]
    public async Task Merge_AMalformedBundle_Is400_AndWritesNothing(string label, string members)
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();

        var (status, body) = await ImportRawAsync(members, "merge");

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, $"{label}: nothing was written");

        status.Should().Be(400, $"{label}: {body}");
    }

    /// <summary>The same bundle in <c>merge</c> is refused, and so is a second <c>create</c> of that name.</summary>
    [Fact]
    [Trait("Bug", "84424")]
    public async Task Replace_TwoPromptsOfOneNameInTheBundle_AreNotWrittenAsDuplicates()
    {
        await IgnoreRefusalAsync(() => ImportAsync([], [new AiPrompt("a", "Autotest twin", "first"), new AiPrompt("b", "Autotest twin", "second")], AiImportMode.Replace));

        var names = (await ReadLibraryAsync()).Prompts.Select(p => p.Name.ToLowerInvariant()).ToList();

        names.Should().OnlyHaveUniqueItems("prompt names in the library");
    }

    /// <summary>create-folder and rename-folder refuse a second folder of an existing name.</summary>
    [Fact]
    [Trait("Bug", "84425")]
    public async Task Merge_TwoFoldersOfOneNameInTheBundle_AreNotWrittenAsDuplicates()
    {
        await IgnoreRefusalAsync(() => ImportAsync([new AiPromptFolder("a", "Autotest twin"), new AiPromptFolder("b", "Autotest twin")], [], AiImportMode.Merge));

        var names = (await ReadLibraryAsync()).Folders.Select(f => f.Name.ToLowerInvariant()).ToList();

        names.Should().OnlyHaveUniqueItems("folder names in the library");
    }
}
