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
/// report". The bundles are written by hand: a version other than 1 does not exist in the typed model.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Prompts")]
public class PromptImportRefusalTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    private const string CleanPrompt = "{\"id\":\"ok\",\"name\":\"Autotest clean\",\"text\":\"t\"}";
    private const string OrphanPrompt = "{\"id\":\"orphan\",\"name\":\"Autotest orphan\",\"text\":\"t\",\"folderId\":\"nope\"}";

    public static TheoryData<string, string, string, string> SoftRefusals
    {
        get
        {
            var data = new TheoryData<string, string, string, string>();

            foreach (var mode in new[] { "merge", "replace" })
            {
                data.Add(mode, "a bundle version of 0", BundleJson(string.Empty, string.Empty, "0"), "Unsupported bundle version");
                data.Add(mode, "a bundle version of 2", BundleJson(string.Empty, string.Empty, "2"), "Unsupported bundle version");
                data.Add(mode, "no bundle version", "{\"folders\":[],\"prompts\":[]}", "Unsupported bundle version");
                data.Add(mode, "a prompt in a folder the bundle does not have, next to a clean prompt", BundleJson(string.Empty, $"{CleanPrompt},{OrphanPrompt}"), "references missing folder");
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SoftRefusals))]
    public async Task Import_UnsupportedVersionOrDanglingFolderReference_IsReported_AndNothingIsWrittenOrDeleted(
        string mode,
        string label,
        string bundle,
        string message)
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();

        var (status, body) = await ImportBundleRawAsync(bundle, mode);

        status.Should().Be(400, $"{label}: {body}");
        var answer = ParseBody(body);
        answer.GetProperty("success").GetBoolean().Should().BeFalse(label);
        answer.TryGetProperty("imported", out _).Should().BeFalse(label);
        answer.GetProperty("errors")[0].GetProperty("error").GetProperty("message").GetString().Should().Contain(message, label);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, $"{label}: the library");
    }

    [Fact]
    public async Task Merge_TwoPromptsOfOneNameInTheBundle_AreRefusedWhole_TheCleanOneWithThem()
    {
        await SeedLibraryAsync();
        var before = await ReadLibraryAsync();
        var prompts = string.Join(
            ",",
            CleanPrompt,
            "{\"id\":\"b\",\"name\":\"Autotest twin\",\"text\":\"first\"}",
            "{\"id\":\"c\",\"name\":\"Autotest twin\",\"text\":\"second\"}");

        var (status, body) = await ImportBundleRawAsync(BundleJson(string.Empty, prompts), "merge");

        status.Should().Be(400, body);
        var answer = ParseBody(body);
        answer.GetProperty("success").GetBoolean().Should().BeFalse(body);
        answer.GetProperty("errors")[0].GetProperty("error").GetProperty("message").GetString()
            .Should().Be("The bundle holds two prompts of this name in one folder");

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }
}
