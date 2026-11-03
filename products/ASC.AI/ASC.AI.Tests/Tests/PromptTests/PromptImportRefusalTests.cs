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
/// A bundle the import refuses with a per-entry report (an unsupported version, a prompt pointing at a folder the bundle
/// does not have, twin names) is refused whole in both modes: nothing is written, and a <c>replace</c> deletes nothing.
/// The documented promise is "either every entry persisted with counts, or no entries persisted plus a per-entry error
/// report". Only the version cases are written by hand: a version other than 1 does not exist in the typed model.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Prompts")]
public class PromptImportRefusalTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    public static TheoryData<string, string, string> VersionRefusals
    {
        get
        {
            var data = new TheoryData<string, string, string>();

            foreach (var mode in new[] { "merge", "replace" })
            {
                data.Add(mode, "a bundle version of 0", BundleJson(string.Empty, string.Empty, "0"));
                data.Add(mode, "a bundle version of 2", BundleJson(string.Empty, string.Empty, "2"));
                data.Add(mode, "no bundle version", "{\"folders\":[],\"prompts\":[]}");
            }

            return data;
        }
    }

    public static TheoryData<AiImportMode> Modes => new() { AiImportMode.Merge, AiImportMode.Replace };

    [Theory]
    [MemberData(nameof(VersionRefusals))]
    public async Task Import_UnsupportedVersion_IsReported_AndNothingIsWrittenOrDeleted(string mode, string label, string bundle)
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();

        var (status, body) = await ImportBundleRawAsync(bundle, mode);

        status.Should().Be(400, $"{label}: {body}");
        var answer = ParseBody(body);
        answer.GetProperty("success").GetBoolean().Should().BeFalse(label);
        answer.TryGetProperty("imported", out _).Should().BeFalse(label);
        answer.GetProperty("errors")[0].GetProperty("error").GetProperty("message").GetString().Should().Contain("Unsupported bundle version", label);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, $"{label}: the library");
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Import_APromptInAFolderTheBundleDoesNotHave_NextToACleanOne_IsReported_AndNothingIsWrittenOrDeleted(AiImportMode mode)
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();

        var refused = await ExpectRefusalAsync(
            () => ImportAsync([], [new AiPrompt("ok", "Autotest clean", "t"), new AiPrompt("orphan", "Autotest orphan", "t", "nope")], mode),
            null,
            400);

        var answer = BodyOf(refused);
        answer.GetProperty("success").GetBoolean().Should().BeFalse(refused.ErrorContent?.ToString());
        answer.TryGetProperty("imported", out _).Should().BeFalse(refused.ErrorContent?.ToString());
        answer.GetProperty("errors")[0].GetProperty("error").GetProperty("message").GetString().Should().Contain("references missing folder");

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, $"{mode}: the library");
    }

    [Fact]
    public async Task Merge_TwoPromptsOfOneNameInTheBundle_AreRefusedWhole_TheCleanOneWithThem()
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();
        List<AiPrompt> prompts =
        [
            new("ok", "Autotest clean", "t"),
            new("b", "Autotest twin", "first"),
            new("c", "Autotest twin", "second")
        ];

        var refused = await ExpectRefusalAsync(() => ImportAsync([], prompts, AiImportMode.Merge), null, 400);

        var answer = BodyOf(refused);
        answer.GetProperty("success").GetBoolean().Should().BeFalse(refused.ErrorContent?.ToString());
        answer.GetProperty("errors")[0].GetProperty("error").GetProperty("message").GetString()
            .Should().Be("The bundle holds two prompts of this name in one folder");

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }
}
