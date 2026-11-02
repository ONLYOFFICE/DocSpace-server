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
/// and twins in a bundle were written as duplicates (BUG 84424, 84425). The bundles are written by hand: a prompt
/// without a text or a folder without a name cannot be built from the typed model.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Prompts")]
public class PromptImportValidationTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    private const string MixedPrompts = "{\"id\":\"ok\",\"name\":\"Autotest valid first\",\"text\":\"t\"},{\"id\":\"bad\",\"name\":\"Autotest invalid second\",\"text\":\"\"}";

    public static TheoryData<string, string> BadEntries => new()
    {
        { "a prompt without a text", BundleJson(string.Empty, "{\"id\":\"a\",\"name\":\"n\"}") },
        { "a prompt with a blank name", BundleJson(string.Empty, "{\"id\":\"a\",\"name\":\"\",\"text\":\"t\"}") },
        { "a prompt with a blank text", BundleJson(string.Empty, "{\"id\":\"a\",\"name\":\"n\",\"text\":\"\"}") },
        { "a prompt name of 256 characters", BundleJson(string.Empty, $"{{\"id\":\"a\",\"name\":\"{new string('N', 256)}\",\"text\":\"t\"}}") },
        { "a folder with a blank name", BundleJson("{\"id\":\"a\",\"name\":\"\"}", string.Empty) },
        { "a folder name of 256 characters", BundleJson($"{{\"id\":\"a\",\"name\":\"{new string('N', 256)}\"}}", string.Empty) }
    };

    public static TheoryData<string, string> ReplaceRefusals => new()
    {
        { "a blank prompt name", BundleJson(string.Empty, "{\"id\":\"a\",\"name\":\"\",\"text\":\"t\"}") },
        { "a blank prompt text", BundleJson(string.Empty, "{\"id\":\"a\",\"name\":\"n\",\"text\":\"\"}") },
        { "a valid entry followed by an invalid one", BundleJson(string.Empty, MixedPrompts) }
    };

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
    public async Task Merge_ABundleWithASingleInvalidEntry_Is400_AndWritesNothing(string label, string bundle)
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();

        var (status, body) = await ImportBundleRawAsync(bundle, "merge");

        status.Should().Be(400, $"{label}: {body}");
        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, $"{label}: the library");
    }

    [Fact]
    [Trait("Bug", "84421")]
    public async Task Merge_AnInvalidEntryAfterAValidOne_Is400_AndNothingIsWritten()
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();

        var (status, body) = await ImportBundleRawAsync(BundleJson(string.Empty, MixedPrompts), "merge");

        status.Should().Be(400, "the bundle is refused: " + body);
        ParseBody(body).GetProperty("success").GetBoolean().Should().BeFalse(body);

        // "a corrupt bundle is rejected whole rather than applied halfway".
        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Theory]
    [MemberData(nameof(ReplaceRefusals))]
    [Trait("Bug", "84422")]
    public async Task Replace_AnInvalidBundle_Is400_AndTheExistingLibraryIsKept(string label, string bundle)
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();
        before.Prompts.Should().NotBeEmpty("there is something to lose");

        var (status, body) = await ImportBundleRawAsync(bundle, "replace");

        status.Should().Be(400, $"{label}: the bundle is refused: {body}");
        ParseBody(body).GetProperty("success").GetBoolean().Should().BeFalse(body);

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
