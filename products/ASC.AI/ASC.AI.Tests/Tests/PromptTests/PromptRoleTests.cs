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
/// Saved prompts are a per-user store: every member (DocSpace admin, room admin, user) owns their own library with the
/// whole surface, and none of it reaches the owner's. A Guest has none, and an anonymous caller gets 401 (see
/// <see cref="PromptGuestTests"/> and <see cref="PromptAnonymousTests"/>).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Prompts")]
public class PromptRoleTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task Member_HasTheirOwnPromptLibrary(EmployeeType type)
    {
        var member = await InviteContact(type, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(member);

        (await _aiPrompts.AiPromptsListAsync(cancellationToken: Token)).Should().BeEmpty();

        var prompt = await CreatePromptAsync($"Autotest {type}", "Body");
        (await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token)).Id.Should().Be(prompt.Id);

        var folder = await CreateFolderAsync($"Autotest {type}");
        (await _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(prompt.Id, folder.Id), Token)).Success.Should().BeTrue();

        (await _aiPrompts.AiPromptsExportAsync(Token)).Prompts.Select(p => p.Id).Should().Equal(prompt.Id);

        (await _aiPrompts.AiPromptsDeleteAsync(prompt.Id, Token)).Success.Should().BeTrue();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task Member_ManagesTheirOwnLibrary_AndNoneOfItReachesTheOwners(EmployeeType type)
    {
        await CreatePromptAsync("Autotest owner prompt", "Owner body");
        var ownerBefore = await ReadLibraryAsync();

        var member = await InviteContact(type, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(member);

        var folder = await CreateFolderAsync($"Autotest {type}");
        var prompt = await CreatePromptAsync($"Autotest {type}", "Body", folder.Id);

        // The prompt sits in a folder, so a typed update (which always asks for the root) would move it.
        var (status, body) = await UpdateRawAsync(prompt.Id, new Dictionary<string, object?> { ["text"] = "Edited body" });
        status.Should().Be(200, body);
        ParseBody(body).GetProperty("success").GetBoolean().Should().BeTrue(body);
        (await _aiPrompts.AiPromptsGetByIdAsync(prompt.Id, Token)).Text.Should().Be("Edited body");

        var renamed = await _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(folder.Id, $"Autotest {type} renamed"), Token);
        renamed.Success.Should().BeTrue();
        (await FindFolderAsync(folder.Id))!.Name.Should().Be($"Autotest {type} renamed");
        (await ListFolderIdsAsync()).Should().Equal(folder.Id);

        var imported = await ImportAsync([], [new AiPrompt("x", $"Autotest {type} imported", "t")], AiImportMode.Merge);
        imported.Success.Should().BeTrue();
        imported.Imported.Folders.Should().Be(0);
        imported.Imported.Prompts.Should().Be(1);

        (await _aiPrompts.AiPromptsDeleteFolderAsync(folder.Id, Token)).Success.Should().BeTrue();
        (await FindPromptAsync(prompt.Id)).Should().BeNull("the prompt went with its folder");
        (await _aiPrompts.AiPromptsListAsync(cancellationToken: Token)).Select(p => p.Name).Should().Equal($"Autotest {type} imported");

        await _aiChatClient.Authenticate(Owner);
        (await ReadLibraryAsync()).Should().BeEquivalentTo(ownerBefore, "none of it reached the owner's library");
    }
}
