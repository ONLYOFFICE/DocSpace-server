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

namespace ASC.AI.Tests.Tests.PromptStorageTests;

[Trait("Category", "Validation")]
[Trait("Feature", "AI/Prompts")]
public class PromptNameLengthTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int MaxNameLength = 255;

    [Fact]
    public async Task Create_NameAtLimit_Succeeds()
    {
        var name = new string('a', MaxNameLength);

        var result = await _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput(name: name, text: "body"), TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue($"prompts/create rejected the prompt: {result.Error}");
        result.Prompt.Name.Should().Be(name);
        (await _aiPrompts.AiPromptsGetByIdAsync(result.Prompt.Id, TestContext.Current.CancellationToken)).Name.Should().Be(name);
    }

    [Theory]
    [InlineData(MaxNameLength + 1)]
    [InlineData(1024)]
    [InlineData(5000)]
    public async Task Create_NameOverLimit_Returns400(int length)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _aiPrompts.AiPromptsCreateAsync(
            new AiCreatePromptInput(name: new string('a', length), text: "body"),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await _aiPrompts.AiPromptsListAsync(cancellationToken: TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    // Internal contract: the prompt batch create has no public route (the chat service's createMany fans out to single creates, so import-bundle never reaches /prompts/batch).
    [Theory]
    [InlineData(MaxNameLength + 1)]
    [InlineData(1024)]
    [InlineData(5000)]
    public async Task CreateMany_NameOverLimit_Returns400(int length)
    {
        using var response = await _ai.PostAsync(
            $"{PromptsPath}/batch",
            new
            {
                prompts = new[]
                {
                    new { name = "valid", text = "body", folderId = (Guid?)null },
                    new { name = new string('a', length), text = "body", folderId = (Guid?)null }
                }
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_NameAtLimit_PersistsFullName()
    {
        var created = await CreatePromptAsync("original");
        var name = new string('a', MaxNameLength);

        var result = await _aiPrompts.AiPromptsUpdateAsync(
            new AiPromptsUpdateRequest(id: created.Id, updates: new AiPromptsUpdateRequestUpdates(name: name, text: "body")),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue($"prompts/update rejected the prompt: {result.Error}");
        result.Prompt.Name.Should().Be(name);
        (await _aiPrompts.AiPromptsGetByIdAsync(created.Id, TestContext.Current.CancellationToken)).Name.Should().Be(name);
    }

    [Theory]
    [InlineData(MaxNameLength + 1)]
    [InlineData(1024)]
    [InlineData(5000)]
    public async Task Update_NameOverLimit_Returns400(int length)
    {
        var created = await CreatePromptAsync("original");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _aiPrompts.AiPromptsUpdateAsync(
            new AiPromptsUpdateRequest(id: created.Id, updates: new AiPromptsUpdateRequestUpdates(name: new string('a', length), text: "body")),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        (await _aiPrompts.AiPromptsGetByIdAsync(created.Id, TestContext.Current.CancellationToken)).Name.Should().Be("original");
    }

    private async Task<AiPrompt> CreatePromptAsync(string name)
    {
        var result = await _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput(name: name, text: "body"), TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue($"prompts/create rejected the prompt: {result.Error}");

        return result.Prompt;
    }
}
