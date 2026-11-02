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
/// Cross-user isolation (the IDOR entries of the access matrix): a prompt or folder id that belongs to someone else is
/// invisible, cannot be edited, moved, renamed or deleted, and cannot be reached through an import. Every "unchanged" is
/// a full library snapshot with timestamps, and the owner's snapshot is taken before the second user exists.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Prompts")]
public class PromptIsolationTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    private async Task<User> ActAsNewMemberAsync()
    {
        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(member);

        return member;
    }

    [Fact]
    public async Task ListAndGet_AnotherUsersPrompts_AreInvisible()
    {
        var secret = $"OWNER-SECRET-{Guid.NewGuid():N}";
        var ownerPrompt = await CreatePromptAsync("Autotest owner prompt", secret);
        var ownerFolder = await CreateFolderAsync("Autotest owner");

        await ActAsNewMemberAsync();

        (await _aiPrompts.AiPromptsListAsync(cancellationToken: Token)).Should().BeEmpty("the member's own library is empty");
        (await _aiPrompts.AiPromptsListFoldersAsync(Token)).Should().BeEmpty();

        // A direct read by id: the IDOR case.
        (await FindPromptAsync(ownerPrompt.Id)).Should().BeNull("the owner's prompt read by id");
        (await FindFolderAsync(ownerFolder.Id)).Should().BeNull("the owner's folder read by id");

        (await _aiPrompts.AiPromptsExportAsync(Token)).Prompts.Should().BeEmpty();
        (await ExportRawAsync()).Should().NotContain(secret, "the export must not carry the owner's text");
    }

    [Fact]
    public async Task UpdateAndMove_AnotherUsersPrompt_CannotBeEdited()
    {
        var ownerPrompt = await CreatePromptAsync("Autotest owner prompt", "Owner body");

        await ActAsNewMemberAsync();
        var memberFolder = await CreateFolderAsync("Autotest member");

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsUpdateAsync(
                new AiPromptsUpdateRequest(ownerPrompt.Id, new AiPromptsUpdateRequestUpdates(name: "Autotest hijacked", text: "Hijacked body")), Token),
            $"Prompt not found: {ownerPrompt.Id}",
            404);

        // Nor can it be dragged into the member's own folder.
        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(ownerPrompt.Id, memberFolder.Id), Token),
            $"Prompt not found: {ownerPrompt.Id}",
            404);
        (await ListIdsAsync(memberFolder.Id)).Should().BeEmpty();

        await _aiChatClient.Authenticate(Owner);
        var read = await _aiPrompts.AiPromptsGetByIdAsync(ownerPrompt.Id, Token);
        read.Name.Should().Be("Autotest owner prompt", "the owner's prompt is unchanged");
        read.Text.Should().Be("Owner body");
        read.FolderId.Should().BeNull();
    }

    /// <summary>
    /// The data is safe (the store is scoped, so nothing is removed), but the call used to answer 200 <c>{success: true}</c>:
    /// a caller cannot tell "deleted" from "not yours", and the neighbouring update and move on the same id do report
    /// "not found". The call must not report success.
    /// </summary>
    [Fact]
    [Trait("Bug", "82809")]
    public async Task Delete_AnotherUsersPrompt_DoesNotReportSuccess()
    {
        var ownerPrompt = await CreatePromptAsync("Autotest owner prompt", "Owner body");
        await ActAsNewMemberAsync();

        AiSuccessResponse? answer = null;
        ApiException? refusal = null;

        try
        {
            answer = await _aiPrompts.AiPromptsDeleteAsync(ownerPrompt.Id, Token);
        }
        catch (ApiException exception)
        {
            refusal = exception;
        }

        await _aiChatClient.Authenticate(Owner);
        (await FindPromptAsync(ownerPrompt.Id))!.Id.Should().Be(ownerPrompt.Id, "the owner's prompt survives");

        var reportedSuccess = refusal is null && answer!.Success;
        reportedSuccess.Should().BeFalse("deleting a prompt the caller does not own must not report success");
    }

    [Fact]
    public async Task RenameFolder_AnotherUsersFolder_CannotBeRenamed()
    {
        var ownerFolder = await CreateFolderAsync("Autotest owner");
        await ActAsNewMemberAsync();

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(ownerFolder.Id, "Autotest hijacked"), Token),
            $"Folder not found: {ownerFolder.Id}",
            404);

        await _aiChatClient.Authenticate(Owner);
        (await FindFolderAsync(ownerFolder.Id))!.Name.Should().Be("Autotest owner", "the owner's folder keeps its name");
    }

    /// <summary>
    /// delete-folder is the one write with a blast radius: it cascade-deletes the prompts inside. It used to answer 200
    /// <c>{success: true}</c> while deleting nothing; it is "Folder not found" now, like rename-folder on the same id for
    /// the same caller. The folder gets a prompt, and the prompt is what the assertions read.
    /// </summary>
    [Fact]
    [Trait("Bug", "83138")]
    public async Task DeleteFolder_AnotherUsersFolder_IsRefused()
    {
        var ownerFolder = await CreateFolderAsync("Autotest owner");
        var ownerPrompt = await CreatePromptAsync("Autotest inside", "Owner body", ownerFolder.Id);
        await ActAsNewMemberAsync();

        var refusal = await ExpectRefusalAsync(() => _aiPrompts.AiPromptsDeleteFolderAsync(ownerFolder.Id, Token), null, 404);
        refusal.ErrorContent?.ToString().Should().Contain("Folder not found");

        await _aiChatClient.Authenticate(Owner);
        (await FindFolderAsync(ownerFolder.Id))!.Id.Should().Be(ownerFolder.Id, "the owner's folder survives");
        (await FindPromptAsync(ownerPrompt.Id))!.Id.Should().Be(ownerPrompt.Id, "and so does the prompt inside it");
    }

    [Fact]
    public async Task CreateMoveUpdate_APromptCannotBePutIntoOrPulledOutOfAnotherUsersFolder()
    {
        var ownerFolder = await CreateFolderAsync("Autotest owner");
        var ownerPrompt = await CreatePromptAsync("Autotest owner prompt", "Owner body", ownerFolder.Id);
        var ownerBefore = await ReadLibraryAsync();

        await ActAsNewMemberAsync();
        await CreateFolderAsync("Autotest member");
        var memberPrompt = await CreatePromptAsync("Autotest member prompt", "Member body");
        var memberBefore = await ReadLibraryAsync();

        var noFolder = $"Folder not found: {ownerFolder.Id}";
        var noPrompt = $"Prompt not found: {ownerPrompt.Id}";
        var refusals = new List<(string Label, Func<Task> Call, string Message)>
        {
            (
                "create a prompt in the owner's folder",
                () => _aiPrompts.AiPromptsCreateAsync(new AiCreatePromptInput("Autotest planted", "x", ownerFolder.Id), Token),
                noFolder),
            (
                "move the member's own prompt into the owner's folder",
                () => _aiPrompts.AiPromptsMoveAsync(new AiPromptsMoveRequest(memberPrompt.Id, ownerFolder.Id), Token),
                noFolder),
            (
                "update{folderId} of the member's prompt to the owner's folder",
                () => _aiPrompts.AiPromptsUpdateAsync(
                    new AiPromptsUpdateRequest(memberPrompt.Id, new AiPromptsUpdateRequestUpdates(folderId: ownerFolder.Id)), Token),
                noFolder),
            (
                "update{name, folderId} of the member's prompt to the owner's folder",
                () => _aiPrompts.AiPromptsUpdateAsync(
                    new AiPromptsUpdateRequest(memberPrompt.Id, new AiPromptsUpdateRequestUpdates(name: "Autotest renamed", folderId: ownerFolder.Id)), Token),
                noFolder),
            (
                "move the owner's prompt out to the member's root",
                () => _aiPrompts.AiPromptsMoveAsync(MoveToRoot(ownerPrompt.Id), Token),
                noPrompt),
            (
                "update{folderId: null} of the owner's prompt",
                () => _aiPrompts.AiPromptsUpdateAsync(new AiPromptsUpdateRequest(ownerPrompt.Id, new AiPromptsUpdateRequestUpdates(folderId: null!)), Token),
                noPrompt)
        };

        foreach (var (_, call, message) in refusals)
        {
            await ExpectRefusalAsync(call, message, 404);
        }

        // Reads aimed at the owner's folder find nothing: the folder is not "empty", it is not there as far as this user can tell.
        (await FindFolderAsync(ownerFolder.Id)).Should().BeNull();
        (await ListIdsAsync(ownerFolder.Id)).Should().BeEmpty();

        (await ReadLibraryAsync()).Should().BeEquivalentTo(memberBefore, "the member's own library is untouched");
        await _aiChatClient.Authenticate(Owner);
        (await ReadLibraryAsync()).Should().BeEquivalentTo(ownerBefore, "so is the owner's, with the prompt still inside its folder");
        (await ListIdsAsync(ownerFolder.Id)).Should().Equal([ownerPrompt.Id], "positive control: the folder the member could not see is really there");
    }

    [Fact]
    public async Task TwoUsers_MayUseTheSameNames_AndEachExportHoldsOnlyItsOwnLibrary()
    {
        var ownerSecret = $"OWNER-{Guid.NewGuid():N}";
        var ownerFolder = await CreateFolderAsync("Autotest shared");
        var ownerPrompt = await CreatePromptAsync("Autotest shared", ownerSecret, ownerFolder.Id);
        var ownerBefore = await ReadLibraryAsync();

        await ActAsNewMemberAsync();
        var memberSecret = $"MEMBER-{Guid.NewGuid():N}";

        // Same folder name and same prompt name: no conflict across users.
        var memberFolder = await CreateFolderAsync("Autotest shared");
        var memberPrompt = await CreatePromptAsync("Autotest shared", memberSecret, memberFolder.Id);
        memberFolder.Id.Should().NotBe(ownerFolder.Id);
        memberPrompt.Id.Should().NotBe(ownerPrompt.Id);

        var memberExport = await _aiPrompts.AiPromptsExportAsync(Token);
        memberExport.Prompts.Select(p => p.Id).Should().Equal(memberPrompt.Id);
        memberExport.Folders.Select(f => f.Id).Should().Equal(memberFolder.Id);
        (await ExportRawAsync()).Should().NotContain(ownerSecret);

        await _aiChatClient.Authenticate(Owner);
        var ownerExport = await _aiPrompts.AiPromptsExportAsync(Token);
        ownerExport.Prompts.Select(p => p.Id).Should().Equal(ownerPrompt.Id);
        (await ExportRawAsync()).Should().NotContain(memberSecret);
        (await ReadLibraryAsync()).Should().BeEquivalentTo(ownerBefore, "the member's writes did not touch the owner's library");
    }

    [Fact]
    public async Task ImportAndDeleteFolder_AMembersReplaceImportChangesOnlyTheirLibrary_AndBundleIdsCannotNameTheOwnersRows()
    {
        var ownerFolder = await CreateFolderAsync("Autotest owner");
        var ownerPrompt = await CreatePromptAsync("Autotest owner prompt", "Owner body", ownerFolder.Id);
        await CreatePromptAsync("Autotest owner root", "Owner root body");
        var ownerBefore = await ReadLibraryAsync();

        await ActAsNewMemberAsync();
        var oldFolder = await CreateFolderAsync("Autotest old");
        await CreatePromptAsync("Autotest old prompt", "Old", oldFolder.Id);

        // The bundle reuses the owner's own ids. They are ignored: the member's rows get fresh ids and the owner's rows
        // are not written through them.
        var result = await ImportAsync(
                [new AiPromptFolder(ownerFolder.Id, "Autotest imported")],
                [new AiPrompt(ownerPrompt.Id, "Autotest imported prompt", "Imported body", ownerFolder.Id)],
                AiImportMode.Replace);
        result.Success.Should().BeTrue();
        result.Imported.Folders.Should().Be(1);
        result.Imported.Prompts.Should().Be(1);

        var memberAfter = await ReadLibraryAsync();
        ShapeOf(memberAfter).Should().BeEquivalentTo(new Shape(["Autotest imported"], ["Autotest imported/Autotest imported prompt: Imported body"]));
        memberAfter.Folders.Select(f => f.Id).Should().NotContain(ownerFolder.Id, "the member's new folder id is not the owner's");
        memberAfter.Prompts.Select(p => p.Id).Should().NotContain(ownerPrompt.Id, "the member's new prompt id is not the owner's");

        // And the member's own cleanup stays inside their library too.
        (await _aiPrompts.AiPromptsDeleteFolderAsync(memberAfter.Folders[0].Id, Token)).Success.Should().BeTrue();
        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsDeleteFolderAsync(ownerFolder.Id, Token), null, 404);

        await _aiChatClient.Authenticate(Owner);
        (await ReadLibraryAsync()).Should().BeEquivalentTo(ownerBefore, "the owner's library is byte-identical after all of it");
    }
}
