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

namespace ASC.Files.Tests.Tests._01_Files.FormFilling;

/// <summary>
/// The result folders of a form filling room: "In process" and "Complete" with a subfolder per form,
/// created together with the first filling draft, and the rules that keep them and the drafts in them
/// from being changed.
/// </summary>
[Trait("Category", "Features")]
[Trait("Feature", "FormFilling")]
public class FormResultFoldersTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    private sealed record Setup(int RoomId, int FormId, User Filler, int DraftId);

    /// <summary>
    /// A started form in a form filling room and one form filler who asked for a draft through
    /// <c>checkfillformdraft</c>, the call the editor makes before filling. Leaves the client as the owner.
    /// </summary>
    private async Task<(Setup Setup, string DraftUrl)> CreateDraft()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Result Folders Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        await StartFormFilling(form.Id);
        var filler = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, filler, FileShare.FillForms);

        await _filesClient.Authenticate(filler);
        var url = (await _filesApi.CheckFillFormDraftAsync(form.Id, new CheckFillFormDraftRequest(), TestContext.Current.CancellationToken)).Response;
        await _filesClient.Authenticate(Owner);

        return (new Setup(room.Id, form.Id, filler, FileIdOf(url)), url);
    }

    /// <summary>Reads the <c>fileid</c> query value out of an editor address.</summary>
    private static int FileIdOf(string url)
    {
        const string key = "fileid=";
        var start = url.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        start.Should().BeGreaterThanOrEqualTo(0, "the answer is the editor address of the draft: {0}", url);
        start += key.Length;
        var end = start;

        while (end < url.Length && char.IsDigit(url[end]))
        {
            end++;
        }

        return int.Parse(url[start..end]);
    }

    private async Task<List<FolderDto>> FoldersIn(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Folders.OfType<FolderDto>().ToList();
    }

    private async Task<FolderDto> FolderInfo(int folderId)
    {
        return (await _foldersApi.GetFolderInfoAsync(folderId, TestContext.Current.CancellationToken)).Response;
    }

    [Fact]
    public async Task CheckFillFormDraft_FirstDraftOfFiller_CreatesDraftInTheInProcessSubfolderOfTheForm()
    {
        // Arrange
        var (setup, url) = await CreateDraft();

        // Act
        var roomFolders = await FoldersIn(setup.RoomId);

        // Assert
        setup.DraftId.Should().NotBe(setup.FormId);
        url.Should().Contain("#message/", "the editor is told that a draft was created");

        roomFolders.Should().ContainSingle(f => f.Type == FolderType.InProcessFormFolder && f.Title == "In process");
        roomFolders.Should().ContainSingle(f => f.Type == FolderType.ReadyFormFolder && f.Title == "Complete");

        await _filesClient.Authenticate(setup.Filler);
        var draft = (await _filesApi.GetFileInfoAsync(setup.DraftId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        draft.Title.Should().EndWith(" - Autotest Form.pdf");

        await _filesClient.Authenticate(Owner);
        var perForm = await FolderInfo(draft.FolderId);
        perForm.Type.Should().Be(FolderType.FormFillingFolderInProgress);
        perForm.Title.Should().Be("Autotest Form");
        (await FolderInfo(perForm.ParentId)).Type.Should().Be(FolderType.InProcessFormFolder);
    }

    [Fact]
    public async Task CheckFillFormDraft_FirstDraft_PutsTheCompleteSubfolderIdOnTheForm()
    {
        // Arrange
        var (setup, _) = await CreateDraft();

        // Act
        var form = (await _filesApi.GetFileInfoAsync(setup.FormId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        form.ResultsFolderId.Should().NotBeNull();
        var results = await FolderInfo(form.ResultsFolderId!.Value);
        results.Type.Should().Be(FolderType.FormFillingFolderDone);
        results.Title.Should().Be("Autotest Form");
        (await FolderInfo(results.ParentId)).Type.Should().Be(FolderType.ReadyFormFolder);
    }

    [Fact]
    public async Task CheckFillFormDraft_SecondCall_ReturnsTheSameDraft()
    {
        // Arrange
        var (setup, _) = await CreateDraft();
        await _filesClient.Authenticate(setup.Filler);

        // Act
        var url = (await _filesApi.CheckFillFormDraftAsync(setup.FormId, new CheckFillFormDraftRequest(), TestContext.Current.CancellationToken)).Response;

        // Assert
        url.Should().Contain($"fileid={setup.DraftId}");
        url.Should().NotContain("#message/", "no new draft was created");
    }

    [Fact]
    public async Task SystemResultFolders_CannotBeRenamed()
    {
        // Arrange
        var (setup, _) = await CreateDraft();
        var roomFolders = await FoldersIn(setup.RoomId);
        var inProcess = roomFolders.Single(f => f.Type == FolderType.InProcessFormFolder);
        var complete = roomFolders.Single(f => f.Type == FolderType.ReadyFormFolder);
        var perFormInProcess = (await FoldersIn(inProcess.Id)).Single();
        var perFormComplete = (await FoldersIn(complete.Id)).Single();

        foreach (var folder in new[] { inProcess, complete, perFormInProcess, perFormComplete })
        {
            // Act
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolderRequest("Renamed"), TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(403, "folder {0} of type {1} is a system folder", folder.Title, folder.Type);
            (await FolderInfo(folder.Id)).Security.Rename.Should().BeFalse();
        }
    }

    [Fact]
    public async Task SystemResultFolders_InProcessAndComplete_CannotBeDeleted()
    {
        // Arrange
        var (setup, _) = await CreateDraft();
        var roomFolders = await FoldersIn(setup.RoomId);

        foreach (var folder in roomFolders.Where(f => f.Type is FolderType.InProcessFormFolder or FolderType.ReadyFormFolder))
        {
            // Act
            var info = await FolderInfo(folder.Id);

            // Assert
            info.Security.Delete.Should().BeFalse("folder {0} is a system folder", folder.Title);
        }
    }

    [Fact]
    public async Task DraftInInProcessSubfolder_IsFillableByItsCreatorOnlyAndCannotBeRenamedOrMoved()
    {
        // Arrange
        var (setup, _) = await CreateDraft();

        // Act
        await _filesClient.Authenticate(setup.Filler);
        var asCreator = (await _filesApi.GetFileInfoAsync(setup.DraftId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        await _filesClient.Authenticate(Owner);
        var asOwner = (await _filesApi.GetFileInfoAsync(setup.DraftId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        asCreator.Security.FillForms.Should().BeTrue();
        asOwner.Security.FillForms.Should().BeFalse("only the creator of a draft fills it");

        // The owner's Duplicate is asserted on its own, in DraftInInProcessSubfolder_CannotBeDuplicatedByTheRoomOwner.
        var restricted = new Dictionary<string, bool>
        {
            ["creator.Duplicate"] = asCreator.Security.Duplicate
        };

        foreach (var (who, security) in new[] { ("creator", asCreator.Security), ("owner", asOwner.Security) })
        {
            restricted[who + ".Rename"] = security.Rename;
            restricted[who + ".Move"] = security.Move;
            restricted[who + ".Lock"] = security.Lock;
            restricted[who + ".EditHistory"] = security.EditHistory;
            restricted[who + ".Embed"] = security.Embed;
        }

        restricted.Where(r => r.Value).Select(r => r.Key).Should().BeEmpty("a file in a per-form result folder cannot be renamed, moved, duplicated, locked, versioned or embedded");
    }

    /// <summary>
    /// The file-action rule of the per-form result folders refuses Duplicate to everyone, and the room
    /// owner is refused Rename, Move and Lock on the same draft; Duplicate alone comes back true for the
    /// owner. Suspected defect F4 of the form-filling-workflow spec.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task DraftInInProcessSubfolder_CannotBeDuplicatedByTheRoomOwner()
    {
        // Arrange
        var (setup, _) = await CreateDraft();

        // Act
        var asOwner = (await _filesApi.GetFileInfoAsync(setup.DraftId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        asOwner.Security.Duplicate.Should().BeFalse("no file of a per-form result folder is duplicated");
    }

    [Fact]
    public async Task GetFormFilter_CompleteSubfolderWithoutSubmissions_ReturnsEmptyList()
    {
        // Arrange
        var (setup, _) = await CreateDraft();
        var form = (await _filesApi.GetFileInfoAsync(setup.FormId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        var fields = (await _foldersApi.GetFolderAsync(form.ResultsFolderId!.Value, TestContext.Current.CancellationToken)).Response;

        // Assert
        fields.Should().BeEmpty("nothing has been submitted yet");
    }

    [Fact]
    public async Task GetFormFilter_AnonymousOnAnyFolder_ReturnsEmptyListWithoutAuthorization()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Form Filter Room " + Guid.NewGuid().ToString()[..8]);
        await _filesClient.Authenticate(null);

        // Act
        var ofRoom = (await _foldersApi.GetFolderAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        var ofUnknown = (await _foldersApi.GetFolderAsync(999999999, TestContext.Current.CancellationToken)).Response;

        // Assert
        ofRoom.Should().BeEmpty();
        ofUnknown.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateXlsxByFolder_CompleteSubfolderWithoutCompletedForms_Returns404()
    {
        // Arrange
        var (setup, _) = await CreateDraft();
        var form = (await _filesApi.GetFileInfoAsync(setup.FormId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GenerateXlsxByFolderAsync(form.ResultsFolderId!.Value, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404, "the report is rebuilt from a completed copy, and there is none yet");
    }

    [Fact]
    public async Task GenerateXlsxByFolder_FolderOtherThanACompleteSubfolder_Returns403()
    {
        // Arrange
        var (setup, _) = await CreateDraft();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GenerateXlsxByFolderAsync(setup.RoomId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
