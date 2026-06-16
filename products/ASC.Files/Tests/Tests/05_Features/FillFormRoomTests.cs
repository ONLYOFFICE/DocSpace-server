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

namespace ASC.Files.Tests.Tests._05_Features;

[Collection("Test Collection")]
[Trait("Category", "Features")]
[Trait("Feature", "FillFormRoom")]
public class FillFormRoomTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private readonly AspireAppFixture _fixture = fixture;
    private readonly Func<string, string, CancellationToken, Task<byte[]>> _runDocBuilderAsync =
        (script, output, ct) => fixture.RunDocBuilderAsync(script, output, ct);
    private readonly Func<string, string, string, CancellationToken, Task<byte[]>> _runDocBuilderWithArgAsync =
        (script, output, args, ct) => fixture.RunDocBuilderAsync(script, output, args, ct);

    [Fact]
    public async Task CreateFillingFormsRoom_HasSystemFolders()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateFillingFormsRoom("FFR Structure Test");

        var formPdf = await _runDocBuilderAsync(
            "leave_application_form.docbuilder",
            "leave_application_form.pdf",
            TestContext.Current.CancellationToken);

        await UploadFormToRoomAsync(room.Id, formPdf, "leave_application_form.pdf");

        var content = (await _foldersApi.GetFolderByFolderIdAsync(
            room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        content.Folders.Should().HaveCount(2,
            "FFR room must auto-create InProcessFormFolder and ReadyFormFolder on first form upload");
        content.Current.FoldersCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateFillingFormsRoom_RoomType_IsFillingFormsRoom()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateFillingFormsRoom("FFR Type Check");

        var info = (await _foldersApi.GetFolderByFolderIdAsync(
            room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        info.Current.RoomType.Should().Be(RoomType.FillingFormsRoom);
    }

    [Trait("Category", "Lifecycle")]
    [Fact]
    public async Task StartFilling_FillerOpensForm_CreatesDraftInInProgressFolder()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateFillingFormsRoom("FFR Draft Test");

        var formPdf = await _runDocBuilderAsync(
            "leave_application_form.docbuilder",
            "leave_application_form.pdf",
            TestContext.Current.CancellationToken);

        var formFileId = await UploadFormToRoomAsync(room.Id, formPdf, "leave_application_form.pdf");

        var filler = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Initializer.Owner);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = filler.Id, Access = FileShare.FillForms }]
        }, TestContext.Current.CancellationToken);

        await _filesApi.StartFillingFileAsync(formFileId, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(filler);

        var openEditResponse = (await _filesApi.OpenEditFileAsync(
            formFileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var draftFileId = openEditResponse.File.Id;

        draftFileId.Should().NotBe(formFileId, "a new draft file must be created");

        var draftFile = await GetFile(draftFileId);
        draftFile.Should().NotBeNull();

        var draftFolder = (await _foldersApi.GetFolderByFolderIdAsync(
            draftFile.FolderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        draftFolder.Current.Type.Should().Be(FolderType.FormFillingFolderInProgress);
    }

    [Trait("Category", "Lifecycle")]
    [Fact]
    public async Task SubmitFilledForm_ResultFileAppearsInDoneFolder()
    {
        var ct = TestContext.Current.CancellationToken;

        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateFillingFormsRoom("FFR Result Test");

        var formPdf = await _runDocBuilderAsync(
            "leave_application_form.docbuilder",
            "leave_application_form.pdf",
            ct);

        var formFileId = await UploadFormToRoomAsync(room.Id, formPdf, "leave_application_form.pdf");

        var filler = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Initializer.Owner);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = filler.Id, Access = FileShare.FillForms }]
        }, ct);

        await _filesApi.StartFillingFileAsync(formFileId, ct);

        await _filesClient.Authenticate(filler);

        var openEditResponse = (await _filesApi.OpenEditFileAsync(
            formFileId, cancellationToken: ct)).Response;
        var callbackUrl = openEditResponse.EditorConfig.CallbackUrl;
        var docKey = openEditResponse.Document.Key;

        var filledPdf = await _runDocBuilderWithArgAsync(
            "filled_leave_application_form.docbuilder",
            "filled_leave_application_form.pdf",
            "{\"first_name\":\"John\",\"last_name\":\"Doe\",\"start_date\":\"01/05/2026\",\"end_date\":\"10/05/2026\",\"vacation_destination\":\"Riga, Latvia\"}",
            ct);

        await _fixture.SimulateDocServiceSubmitFormAsync(callbackUrl, docKey, filler.Id, filledPdf, ct);

        await _filesClient.Authenticate(Initializer.Owner);

        var formFileAfter = await GetFile(formFileId);
        formFileAfter.ResultsFolderId.Should().NotBeNull(
            "submission must create a Done folder and set ResultsFolderId on the original form");

        var doneContent = (await _foldersApi.GetFolderByFolderIdAsync(
            formFileAfter.ResultsFolderId!.Value, cancellationToken: ct)).Response;

        doneContent.Current.Type.Should().Be(FolderType.FormFillingFolderDone);
        doneContent.Files.Should().Contain(f => f.Title.EndsWith(".pdf"),
            "a PDF result file must appear in Done folder after form submission");
    }

    [Trait("Category", "Lifecycle")]
    [Fact]
    public async Task SubmitFilledForm_DraftDeletedAfterSessionClose()
    {
        var ct = TestContext.Current.CancellationToken;

        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateFillingFormsRoom("FFR Draft Lifecycle Test");

        var formPdf = await _runDocBuilderAsync(
            "leave_application_form.docbuilder",
            "leave_application_form.pdf",
            ct);

        var formFileId = await UploadFormToRoomAsync(room.Id, formPdf, "leave_application_form.pdf");

        var filler = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Initializer.Owner);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = filler.Id, Access = FileShare.FillForms }]
        }, ct);

        await _filesApi.StartFillingFileAsync(formFileId, ct);

        await _filesClient.Authenticate(filler);

        var openEditResponse = (await _filesApi.OpenEditFileAsync(
            formFileId, cancellationToken: ct)).Response;
        var draftFileId = openEditResponse.File.Id;
        var callbackUrl = openEditResponse.EditorConfig.CallbackUrl;
        var docKey = openEditResponse.Document.Key;

        var filledPdf = await _runDocBuilderWithArgAsync(
            "filled_leave_application_form.docbuilder",
            "filled_leave_application_form.pdf",
            "{\"first_name\":\"Jane\",\"last_name\":\"Smith\",\"start_date\":\"01/06/2026\",\"end_date\":\"05/06/2026\",\"vacation_destination\":\"Tallinn, Estonia\"}",
            ct);

        await _fixture.SimulateDocServiceSubmitFormAsync(callbackUrl, docKey, filler.Id, filledPdf, ct);
        await _fixture.SimulateDocServiceSessionCloseAsync(callbackUrl, docKey, filler.Id, ct);

        await _filesClient.Authenticate(Initializer.Owner);

        await Assert.ThrowsAsync<ApiException>(() => GetFile(draftFileId));
    }

    [Fact]
    public async Task StartFilling_Owner_DoesNotReturnForbidden()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateFillingFormsRoom("FFR ACL Owner Test");
        var file = await CreateFile("form.pdf", room.Id);

        // Owner may get a business error (non-PDF form) but must NOT get 403
        var act = async () => await _filesApi.StartFillingFileAsync(
            file.Id, TestContext.Current.CancellationToken);

        var exception = await Record.ExceptionAsync(act);

        if (exception is ApiException apiEx)
        {
            apiEx.ErrorCode.Should().NotBe(403, "Owner must not receive 403 when calling StartFilling");
        }
    }

    [Fact]
    public async Task StartFilling_FillFormsUser_ReturnsForbidden()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateFillingFormsRoom("FFR ACL FillForms Test");
        var file = await CreateFile("form.pdf", room.Id);

        var filler = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Initializer.Owner);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = filler.Id, Access = FileShare.FillForms }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(filler);

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _filesApi.StartFillingFileAsync(
                file.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetRoomContent_NonMember_ReturnsForbidden()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateFillingFormsRoom("FFR ACL Non-Member Test");

        var nonMember = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(nonMember);

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => _foldersApi.GetFolderByFolderIdAsync(
                room.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetRoomContent_FillFormsUser_ReturnsRoomWithFiles()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateFillingFormsRoom("FFR ACL Visibility Test");
        var file = await CreateFile("form.pdf", room.Id);

        // FillForms users only see files with StartFilling=true (FfrStartedForms filter)
        await _filesApi.StartFillingFileAsync(file.Id, TestContext.Current.CancellationToken);

        var filler = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Initializer.Owner);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = filler.Id, Access = FileShare.FillForms }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(filler);

        var content = (await _foldersApi.GetFolderByFolderIdAsync(
            room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        content.Should().NotBeNull();
        content.Current.Should().NotBeNull();
        content.Current.RoomType.Should().Be(RoomType.FillingFormsRoom);
        content.Files.Should().Contain(f => f.Title == file.Title,
            "filler must see the form file uploaded by the owner");
    }

    private async Task<int> UploadFormToRoomAsync(int roomId, byte[] pdfBytes, string fileName)
    {
        var settings = (await _filesSettingsApi.GetFilesSettingsAsync(
            TestContext.Current.CancellationToken)).Response;

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            roomId,
            new SessionRequest(fileName, pdfBytes.LongLength),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        var chunkSize = (int)settings.ChunkUploadSize;
        var chunkNumber = 1;
        var offset = 0;

        while (offset < pdfBytes.Length)
        {
            var size = Math.Min(chunkSize, pdfBytes.Length - offset);
            using var chunkStream = new MemoryStream(pdfBytes, offset, size);
            await _filesOperationsApi.UploadAsyncSessionAsync(
                roomId, session.Id, chunkNumber,
                new FileParameter(chunkStream),
                TestContext.Current.CancellationToken);
            offset += size;
            chunkNumber++;
        }

        var result = (await _filesOperationsApi.FinalizeSessionAsync(
            roomId, session.Id, TestContext.Current.CancellationToken)).Response;

        return result.File.Id;
    }
}
