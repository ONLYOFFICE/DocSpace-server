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
/// The ids the routes take: a missing, malformed or whitespace id on <c>move</c>, <c>delete</c>, <c>delete-folder</c>,
/// <c>get-by-id</c> and <c>get-folder-by-id</c> is a 400, a folder that is not there is a 404, and a prompt id that no
/// library holds is answered with success on <c>delete</c> (the SDK documents "an ID that does not exist is not
/// reported"). A refused call leaves the library as it was.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Prompts")]
public class PromptIdValidationTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    [Fact]
    public async Task Move_AMissingOrMalformedPromptId_Is400()
    {
        var folder = await CreateFolderAsync("Autotest home");
        await CreatePromptAsync("Autotest keeper", "Body", folder.Id);
        var before = await ReadLibraryAsync();

        // Raw on purpose: the typed request cannot be built without an id.
        var (noBody, noBodyAnswer) = await SendRawAsync(HttpMethod.Put, "move", "{}");
        noBody.Should().Be(400, $"an empty body: {noBodyAnswer}");

        var (noId, noIdAnswer) = await SendRawAsync(HttpMethod.Put, "move", Json(new { folderId = folder.Id }));
        noId.Should().Be(400, $"id missing: {noIdAnswer}");

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest("not-a-guid", folder.Id), Token),
            null,
            400);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData(UnknownFolderId)]
    public async Task Move_ATargetFolderThatIsNotThere_Is404_AndNothingMoves(string target)
    {
        var folder = await CreateFolderAsync("Autotest home");
        var prompt = await CreatePromptAsync("Autotest keeper", "Body", folder.Id);
        var before = await ReadLibraryAsync();

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(prompt.Id, target), Token),
            $"Folder not found: {target}",
            404);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("   ")]
    public async Task Delete_AMalformedOrWhitespaceId_Is400_AndDeletesNothing(string id)
    {
        await CreatePromptAsync("Autotest keeper", "B");
        var before = await ReadLibraryAsync();

        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsDeleteAsync(id, Token), null, 400);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task Delete_APromptIdNoLibraryHolds_IsAnsweredWithSuccess_AndDeletesNothing()
    {
        await CreatePromptAsync("Autotest keeper", "B");
        var before = await ReadLibraryAsync();

        (await _aiPrompts.AiPromptsDeleteAsync(UnknownPromptId, Token)).Success.Should().BeTrue();

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("   ")]
    public async Task DeleteFolder_AMalformedOrWhitespaceId_Is400_AndDeletesNothing(string id)
    {
        var folder = await CreateFolderAsync("Autotest keeper");
        await CreatePromptAsync("Autotest inside", "B", folder.Id);
        var before = await ReadLibraryAsync();

        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsDeleteFolderAsync(id, Token), null, 400);

        (await ReadLibraryAsync()).Should().BeEquivalentTo(before, "neither the folder nor the prompt inside it was touched");
    }

    [Fact]
    public async Task Get_AWhitespaceOrMalformedId_Is400()
    {
        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsGetByIdAsync("   ", Token), null, 400);
        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsGetByIdAsync("not-a-guid", Token), null, 400);
        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsGetFolderByIdAsync("   ", Token), null, 400);
    }
}
