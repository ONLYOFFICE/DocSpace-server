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
/// The filling state of a form and how the <c>manageformfilling</c> actions and the
/// <c>startfilling</c> endpoint move it: <c>startFilling</c> on the file, the visibility of the form to
/// the form fillers of a form filling room, the drafts dropped when a new version is released, and the
/// answers for a file that is no form of a room.
/// </summary>
[Trait("Category", "Features")]
[Trait("Feature", "FormFilling")]
public class FormFillingStateTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    private async Task<(int RoomId, int FormId, User Filler)> CreateFormWithFiller(bool start)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Filling State Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        var filler = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, filler, FileShare.FillForms);

        if (start)
        {
            await StartFormFilling(form.Id);
        }

        return (room.Id, form.Id, filler);
    }

    private async Task<FileDto> ReadAs(User user, int fileId)
    {
        await _filesClient.Authenticate(user);

        return (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    private async Task<int> ReadStatusAs(User user, int fileId)
    {
        await _filesClient.Authenticate(user);

        try
        {
            await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken);

            return 200;
        }
        catch (ApiException e)
        {
            return e.ErrorCode;
        }
    }

    private async Task Manage(int formId, FormFillingManageAction action)
    {
        await _filesApi.ManageFormFillingAsync(
            formId.ToString(),
            new ManageFormFillingDto(formId, action),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NewFormInFillingRoom_IsNotStartedAndItsStatusIsNone()
    {
        // Arrange
        var (_, formId, _) = await CreateFormWithFiller(start: false);

        // Act
        var form = await ReadAs(Owner, formId);

        // Assert
        form.IsForm.Should().BeTrue();
        form.StartFilling.Should().NotBe(true);
        form.FormFillingStatus.Should().Be(FormFillingStatus.None, "the status is worked out only inside a virtual data room");
    }

    [Fact]
    public async Task Start_SetsStartFillingAndRevealsTheFormToFormFillers()
    {
        // Arrange
        var (_, formId, filler) = await CreateFormWithFiller(start: false);
        var before = await ReadStatusAs(filler, formId);

        // Act
        await _filesClient.Authenticate(Owner);
        await StartFormFilling(formId);

        // Assert
        before.Should().Be(403, "a form that is not started is hidden from form fillers");
        var asOwner = await ReadAs(Owner, formId);
        asOwner.StartFilling.Should().BeTrue();
        asOwner.FormFillingStatus.Should().Be(FormFillingStatus.None);
        var asFiller = await ReadAs(filler, formId);
        asFiller.Id.Should().Be(formId);
        asFiller.StartFilling.Should().BeTrue();
    }

    [Fact]
    public async Task Stop_ClearsStartFillingAndHidesTheFormFromFormFillers()
    {
        // Arrange
        var (_, formId, filler) = await CreateFormWithFiller(start: true);
        (await ReadStatusAs(filler, formId)).Should().Be(200);

        // Act
        await _filesClient.Authenticate(Owner);
        await StopFormFilling(formId);

        // Assert
        (await ReadAs(Owner, formId)).StartFilling.Should().BeFalse();
        (await ReadStatusAs(filler, formId)).Should().Be(403);
    }

    [Fact]
    public async Task Edit_OfStartedForm_ClearsStartFillingAndHidesTheFormFromFormFillers()
    {
        // Arrange
        var (_, formId, filler) = await CreateFormWithFiller(start: true);

        // Act
        await _filesClient.Authenticate(Owner);
        await Manage(formId, FormFillingManageAction.Edit);

        // Assert
        (await ReadAs(Owner, formId)).StartFilling.Should().BeFalse();
        (await ReadStatusAs(filler, formId)).Should().Be(403);
    }

    [Fact]
    public async Task Resume_OfStartedFormInFillingRoom_AnswersOkAndKeepsFilling()
    {
        // Arrange
        var (_, formId, _) = await CreateFormWithFiller(start: true);

        // Act
        await _filesClient.Authenticate(Owner);
        await Manage(formId, FormFillingManageAction.Resume);

        // Assert
        (await ReadAs(Owner, formId)).StartFilling.Should().BeTrue();
    }

    /// <summary>
    /// Edit remembers the version being edited; a later Start of a different version drops the links
    /// between the form and the drafts of its fillers, so a draft made from the old version is no longer
    /// offered to its filler.
    /// </summary>
    [Fact]
    public async Task Start_AfterEditProducedANewVersion_DropsTheDraftOfTheFiller()
    {
        // Arrange
        var (_, formId, filler) = await CreateFormWithFiller(start: true);
        await _filesClient.Authenticate(filler);
        await _filesApi.OpenEditFileAsync(formId, cancellationToken: TestContext.Current.CancellationToken);
        (await ReadAs(filler, formId)).HasDraft.Should().BeTrue("the filler opened the form and got a draft");

        await _filesClient.Authenticate(Owner);
        await Manage(formId, FormFillingManageAction.Edit);
        var saved = (await _filesApi.SaveEditingFileFromFormAsync(
            formId, file: BuildSubmittedFormFile(), forcesave: false,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
        saved.Version.Should().Be(2);

        // Act
        await StartFormFilling(formId);

        // Assert
        var asFiller = await ReadAs(filler, formId);
        asFiller.StartFilling.Should().BeTrue();
        asFiller.HasDraft.Should().NotBe(true, "the draft belonged to the version that was replaced");
    }

    [Fact]
    public async Task ManageFormFilling_DocxInRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Manage Docx Room " + Guid.NewGuid().ToString()[..8]);
        var file = await CreateFile("Autotest Manage.docx", room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Manage(file.Id, FormFillingManageAction.Start));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ManageFormFilling_PdfInMyDocuments_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Manage My.pdf", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Manage(file.Id, FormFillingManageAction.Start));

        // Assert
        exception.ErrorCode.Should().Be(403, "a form outside a room has no filling to manage");
    }

    [Fact]
    public async Task ManageFormFilling_UnknownFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Manage(999999999, FormFillingManageAction.Start));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Resume_OfFormThatWasNeverStarted_Returns500()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Resume Never Started Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Manage(form.Id, FormFillingManageAction.Resume));

        // Assert
        exception.ErrorCode.Should().Be(500, "a form without filling properties has nothing to resume");
    }

    [Fact]
    public async Task StartFillingFile_FormInFillingRoom_StoresStartFilling()
    {
        // Arrange
        var (_, formId, filler) = await CreateFormWithFiller(start: false);

        // Act
        await _filesClient.Authenticate(Owner);
        var result = (await _filesApi.StartFillingFileAsync(formId, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(formId);
        (await ReadAs(Owner, formId)).StartFilling.Should().BeTrue();
        (await ReadStatusAs(filler, formId)).Should().Be(200, "a started form is shown to the form fillers");
    }

    [Fact]
    public async Task StartFillingFile_FormFillerOfTheRoom_Returns403()
    {
        // Arrange
        var (_, formId, filler) = await CreateFormWithFiller(start: true);

        // Act
        await _filesClient.Authenticate(filler);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartFillingFileAsync(formId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task StartFillingFile_FormInCustomRoom_LeavesItUntouched()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest StartFilling Custom Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);

        // Act
        await _filesApi.StartFillingFileAsync(form.Id, TestContext.Current.CancellationToken);

        // Assert
        (await ReadAs(Owner, form.Id)).StartFilling.Should().NotBe(true, "the endpoint writes the filling state only for a form of a form filling room");
    }
}
