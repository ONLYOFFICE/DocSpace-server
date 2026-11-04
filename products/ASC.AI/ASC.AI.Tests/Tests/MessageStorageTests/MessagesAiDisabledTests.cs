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


namespace ASC.AI.Tests.Tests.MessageStorageTests;

/// <summary>
/// The portal AI switch (<c>PUT /settings/ai-access</c>) as it applies to <c>POST /api/2.0/ai/text-to-docx</c>: off,
/// the export is a 403 even for the Owner, although the export itself never talks to a model. Body validation runs
/// before the switch is checked, so an off-state test with an incomplete body would never see that 403.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Messages")]
public class MessagesAiDisabledTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    // Waits for the export it accepts to land as a .docx, so the document server has to be up.
    protected override bool NeedsDocumentServer => true;

    /// <summary>Switches AI off as the Owner; see <see cref="BaseTest.SwitchPortalAiOffAsync"/>.</summary>
    private async Task SwitchAiOffAsync()
    {
        await _aiChatClient.Authenticate(Owner);
        await SwitchPortalAiOffAsync();
    }

    // A transition, not an end state: a test that only asserted 403 after the flip would also pass if the route were
    // permanently forbidden or the flip had silently failed. The starting state is set, not assumed.
    [Fact]
    public async Task TextToDocx_PortalAiSwitchOff_Returns403()
    {
        await SetPortalAiAccessAsync(true);
        var folderId = await MyDocumentsOfAsync(Owner);

        var beforeTitle = ExportTitle("Exported before");
        var before = await ExportAsAsync(Owner, BuildExport(folderId, beforeTitle));
        before.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await WaitForExportedFileAsync(Owner, folderId, $"{beforeTitle}.docx"))
            .Should().NotBeNull("the export has to work before the switch is flipped");

        await SwitchAiOffAsync();

        var afterTitle = ExportTitle("Exported after");
        var exception = await ExportRefusedAsAsync(Owner, BuildExport(folderId, afterTitle));

        // The refusal is synchronous, so nothing was queued to land later; the document from before the flip is the
        // positive control that the listing is trustworthy.
        var titles = (await ListFilesAsync(Owner, folderId)).Select(f => f.Title).ToList();
        titles.Should().Contain($"{beforeTitle}.docx");
        titles.Should().NotContain($"{afterTitle}.docx");
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Forbidden");
    }

    [Fact]
    public async Task TextToDocx_EmptyContentWithTheSwitchOff_Returns400Not403()
    {
        var folderId = await MyDocumentsOfAsync(Owner);
        await SwitchAiOffAsync();

        var exception = await ExportRefusedAsAsync(Owner, BuildExport(folderId, "T", string.Empty));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("title and content are required");
    }

    // Raw bodies: the SDK request rejects a missing title, content or folder id client-side, and its `format` enum
    // cannot carry an unrecognised value.
    [Theory]
    [InlineData("a missing title", "title and content are required")]
    [InlineData("a missing folderId", "folderId is required")]
    [InlineData("a null folderId", "folderId is required")]
    [InlineData("a missing content", "title and content are required")]
    [InlineData("an unrecognised format", "format must be one of Docx, Pdf, Md")]
    public async Task TextToDocx_InvalidBodyWithTheSwitchOff_Returns400Not403(string kind, string message)
    {
        var folderId = await MyDocumentsOfAsync(Owner);
        await SwitchAiOffAsync();

        // Only the folderId cases leave the folder out; every other body fails on its own defect and no other.
        var body = kind switch
        {
            "a missing title" => JsonSerializer.Serialize(new { content = "hello", folderId }),
            "a missing folderId" => JsonSerializer.Serialize(new { title = "T", content = "hello" }),
            "a null folderId" => """{"title":"T","content":"hello","folderId":null}""",
            "a missing content" => JsonSerializer.Serialize(new { title = "T", folderId }),
            _ => JsonSerializer.Serialize(new { title = "T", content = "hello", folderId, format = "bogus" })
        };

        var (status, responseBody) = await PostExportRawAsync(body);

        responseBody.Should().Contain(message);
        status.Should().Be(HttpStatusCode.BadRequest, kind);
    }
}
