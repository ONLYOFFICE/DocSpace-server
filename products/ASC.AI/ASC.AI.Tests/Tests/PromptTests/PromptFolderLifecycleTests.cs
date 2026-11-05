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
/// The prompt folder, end to end (<c>create-folder</c>, <c>rename-folder</c>, <c>delete-folder</c>,
/// <c>get-folder-by-id</c>). Folders are one flat level; deleting a folder deletes the prompts inside it, silently.
/// The over-long name on create and rename is in <see cref="PromptFolderStorageTests.PromptFolderNameLengthTests"/>.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/PromptFolders")]
public class PromptFolderLifecycleTests(AspireAppFixture fixture) : PromptsTestBase(fixture)
{
    [Fact]
    public async Task Create_Owner_ReadsBackThroughGetAndList()
    {
        (await _aiPrompts.AiPromptsListFoldersAsync(Token)).Should().BeEmpty();

        var folder = await CreateFolderAsync("Autotest folder");

        folder.Id.Should().NotBeNullOrEmpty();
        folder.Name.Should().Be("Autotest folder");

        var read = await _aiPrompts.AiPromptsGetFolderByIdAsync(folder.Id, Token);
        read.Id.Should().Be(folder.Id);
        read.Name.Should().Be("Autotest folder");

        (await ListFolderIdsAsync()).Should().Equal(folder.Id);
    }

    [Fact]
    public async Task Create_NameAsAnObjectField_BindsLikeTheBareString()
    {
        var bare = await CreateFolderAsync("Autotest bare");
        bare.Name.Should().Be("Autotest bare");

        // Raw on purpose: the SDK sends the name as a bare JSON string; the route also takes it as an object field.
        var (status, body) = await SendRawAsync(HttpMethod.Post, "create-folder", Json(new { name = "Autotest wrapped" }));

        status.Should().Be(200, body);
        var answer = ParseBody(body);
        answer.GetProperty("success").GetBoolean().Should().BeTrue(body);
        answer.GetProperty("folder").GetProperty("name").GetString().Should().Be("Autotest wrapped");

        (await _aiPrompts.AiPromptsListFoldersAsync(Token)).Select(f => f.Name).Should().BeEquivalentTo(["Autotest bare", "Autotest wrapped"]);
    }

    [Fact]
    public async Task Rename_Folder_DoesNotTouchItsPrompts()
    {
        var folder = await CreateFolderAsync("Autotest before");
        var prompt = await CreatePromptAsync("Autotest inside", "Body", folder.Id);

        var renamed = await _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(folder.Id, "Autotest after"), Token);

        renamed.Success.Should().BeTrue();
        renamed.Folder.Name.Should().Be("Autotest after");
        (await FindFolderAsync(folder.Id))!.Name.Should().Be("Autotest after");

        var inside = await _aiPrompts.AiPromptsListAsync(folder.Id, Token);
        inside.Select(p => p.Id).Should().Equal(prompt.Id);
        inside[0].Name.Should().Be("Autotest inside");
    }

    [Fact]
    public async Task Delete_EmptyFolder_LeavesTheOthers()
    {
        var doomed = await CreateFolderAsync("Autotest doomed");
        var keeper = await CreateFolderAsync("Autotest keeper");

        (await _aiPrompts.AiPromptsDeleteFolderAsync(doomed.Id, Token)).Success.Should().BeTrue();

        (await FindFolderAsync(doomed.Id)).Should().BeNull();
        (await ListFolderIdsAsync()).Should().Equal(keeper.Id);
    }

    [Fact]
    public async Task Delete_Folder_DeletesThePromptsInsideIt()
    {
        var folder = await CreateFolderAsync("Autotest cascade");
        var inside = await CreatePromptAsync("Autotest inside", "Body", folder.Id);
        var outside = await CreatePromptAsync("Autotest outside", "Body");

        (await _aiPrompts.AiPromptsDeleteFolderAsync(folder.Id, Token)).Success.Should().BeTrue();

        // Cascade, with no warning in the answer: the prompt is gone, not moved to the root. A client that treats the
        // folder as a label loses user data here.
        (await FindPromptAsync(inside.Id)).Should().BeNull("the prompt inside the folder");
        (await FindPromptAsync(outside.Id))!.Id.Should().Be(outside.Id, "a prompt outside the folder");
        (await ListIdsAsync()).Should().Equal(outside.Id);
    }

    /// <summary>
    /// The second delete lands on an id the store no longer knows, which answered 200 success while deleting nothing
    /// (BUG 83138): it is "Folder not found" now, like any other folder the caller cannot resolve.
    /// </summary>
    [Fact]
    [Trait("Bug", "83138")]
    public async Task Delete_MissingId_IsRefused_AndASecondDeleteIsNotFound()
    {
        var folder = await CreateFolderAsync("Autotest once");

        var missing = await ExpectRefusalAsync(() => _aiPrompts.AiPromptsDeleteFolderAsync(string.Empty, Token), null, 400);
        missing.ErrorContent?.ToString().Should().Contain("id required");
        (await FindFolderAsync(folder.Id))!.Id.Should().Be(folder.Id, "the rejected delete removed nothing");

        (await _aiPrompts.AiPromptsDeleteFolderAsync(folder.Id, Token)).Success.Should().BeTrue();

        var again = await ExpectRefusalAsync(() => _aiPrompts.AiPromptsDeleteFolderAsync(folder.Id, Token), null, 404);
        again.ErrorContent?.ToString().Should().Contain("Folder not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankName_IsRefused(string name)
    {
        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsCreateFolderAsync(name, Token), NameRequired, 400);

        (await _aiPrompts.AiPromptsListFoldersAsync(Token)).Should().BeEmpty();
    }

    /// <summary>A taken folder name used to be a soft HTTP 200 <c>{success: false}</c>.</summary>
    [Fact]
    [Trait("Bug", "84308")]
    public async Task Create_TakenName_IsRefusedWith400Or409()
    {
        var folder = await CreateFolderAsync("Autotest dup");

        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsCreateFolderAsync("Autotest dup", Token), FolderNameTaken, 400, 409);

        (await ListFolderIdsAsync()).Should().Equal(folder.Id);
    }

    [Fact]
    public async Task Create_UnicodeAndEmojiName_RoundTrips()
    {
        const string name = "Autotest prompts 📁";

        var folder = await CreateFolderAsync(name);

        (await FindFolderAsync(folder.Id))!.Name.Should().Be(name);
    }

    [Fact]
    public async Task GetFolderById_MalformedOrUnknownId()
    {
        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsGetFolderByIdAsync("not-a-guid", Token), null, 400);
        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsGetFolderByIdAsync(string.Empty, Token), null, 400);

        (await FindFolderAsync(UnknownFolderId)).Should().BeNull("a well-formed unknown id is 200 null");
    }

    // rename-folder is a separate handler from create-folder, so its validation is pinned in its own right.

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task Rename_BlankName_IsRefused_AndTheFolderKeepsItsOwn(string name)
    {
        var folder = await CreateFolderAsync("Autotest keeper");

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(folder.Id, name), Token),
            NameRequired,
            400);

        (await FindFolderAsync(folder.Id))!.Name.Should().Be("Autotest keeper", "a blank rename must not wipe the name");
        (await _aiPrompts.AiPromptsListFoldersAsync(Token)).Select(f => f.Name).Should().Equal("Autotest keeper");
    }

    /// <summary>Renaming onto another folder's name used to be a soft HTTP 200 <c>{success: false}</c>.</summary>
    [Fact]
    [Trait("Bug", "84309")]
    public async Task Rename_OntoAnExistingFolderName_IsRefusedWith400Or409()
    {
        var first = await CreateFolderAsync("Autotest first");
        var second = await CreateFolderAsync("Autotest second");

        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(second.Id, "Autotest first"), Token),
            FolderNameTaken,
            400,
            409);

        // Neither folder moved: create refuses the duplicate, so rename must too, or the tree ends up with two
        // identical labels the user cannot tell apart.
        (await FindFolderAsync(second.Id))!.Name.Should().Be("Autotest second");
        (await FindFolderAsync(first.Id))!.Name.Should().Be("Autotest first");
    }

    /// <summary>
    /// Rename answered 200 <c>{success: true}</c> and silently cut an over-long name to 255 characters (BUG 83123, 256 up
    /// to 1025 all the same), leaving the folder with a name the caller never asked for. The stored-name assertion
    /// comes first so a route that rejects the call but still writes the name would not read as green.
    /// </summary>
    [Fact]
    [Trait("Bug", "83123")]
    public async Task Rename_OverLongName_IsRejectedNotTruncated()
    {
        var folder = await CreateFolderAsync("Autotest short");

        // Control: the same over-long name is a 400 on create-folder, so the limit exists and the rename route is the
        // one disagreeing with create about the same field.
        await ExpectRefusalAsync(() => _aiPrompts.AiPromptsCreateFolderAsync(new string('C', 5000), Token), null, 400);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(folder.Id, new string('N', 5000)), Token));

        (await FindFolderAsync(folder.Id))!.Name.Should().Be("Autotest short");
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task Rename_UnknownFolderId_IsRefused()
    {
        await ExpectRefusalAsync(
            () => _aiPrompts.AiPromptsRenameFolderAsync(new AiPromptsRenameFolderRequest(UnknownFolderId, "Autotest renamed"), Token),
            $"Folder not found: {UnknownFolderId}",
            404);
    }
}
