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
/// <c>GET /files/file/{fileId}/openedit</c> on a PDF form: the filling button the configuration
/// offers (<c>startFillingMode</c>, <c>startFilling</c>), who is refused a form that is not started,
/// and the filling session a form filler or an anonymous visitor of a form filling room is given.
/// </summary>
[Trait("Category", "Features")]
[Trait("Feature", "FormFilling")]
public class FormOpenSetupTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    private async Task<(int RoomId, int FormId)> CreateFormInFillingRoom(bool start)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Open Setup Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);

        if (start)
        {
            await StartFormFilling(form.Id);
        }

        return (room.Id, form.Id);
    }

    private async Task<ConfigurationDto> OpenAnonymouslyThroughRoomLink(int roomId, int formId)
    {
        await _filesClient.Authenticate(Owner);
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(roomId, TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, link.SharedLink.RequestToken);

        try
        {
            return (await _filesApi.OpenEditFileAsync(formId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        }
    }

    [Fact]
    public async Task OpenEditFile_OwnerOnNotStartedFormInFillingRoom_OffersStartFillingInTheRoom()
    {
        // Arrange
        var (_, formId) = await CreateFormInFillingRoom(start: false);

        // Act
        var config = (await _filesApi.OpenEditFileAsync(formId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        config.StartFillingMode.Should().Be(StartFillingMode.StartFillingFormRoom);
        config.StartFilling.Should().BeTrue();
        config.FillingSessionId.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task OpenEditFile_FormFillerOnNotStartedForm_Returns403()
    {
        // Arrange
        var (roomId, formId) = await CreateFormInFillingRoom(start: false);
        var filler = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, filler, FileShare.FillForms);

        // Act
        await _filesClient.Authenticate(filler);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.OpenEditFileAsync(formId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task OpenEditFile_FormFillerOnStartedForm_OpensOwnDraftEmbeddedWithSessionOfDraftAndUser()
    {
        // Arrange
        var (roomId, formId) = await CreateFormInFillingRoom(start: true);
        var filler = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, filler, FileShare.FillForms);

        // Act
        await _filesClient.Authenticate(filler);
        var config = (await _filesApi.OpenEditFileAsync(formId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        config.EditorType.Should().Be(EditorType.Embedded);
        config.FillingSessionId.Should().EndWith("_" + filler.Id);
        var draftId = int.Parse(config.FillingSessionId.Split('_')[0]);
        draftId.Should().NotBe(formId, "the filler fills a draft copy, not the original form");

        var draft = (await _filesApi.GetFileInfoAsync(draftId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        draft.Title.Should().EndWith(" - Autotest Form.pdf");
        var draftFolder = (await _foldersApi.GetFolderInfoAsync(draft.FolderId, TestContext.Current.CancellationToken)).Response;
        draftFolder.Type.Should().Be(FolderType.FormFillingFolderInProgress);
        draftFolder.Title.Should().Be("Autotest Form");

        var again = (await _filesApi.OpenEditFileAsync(formId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        again.FillingSessionId.Should().Be(config.FillingSessionId, "the same draft is reopened");
    }

    [Fact]
    public async Task OpenEditFile_DraftInInProcessSubfolder_IsFilledEmbeddedInItsOwnSession()
    {
        // Arrange
        var (roomId, formId) = await CreateFormInFillingRoom(start: true);
        var filler = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, filler, FileShare.FillForms);
        await _filesClient.Authenticate(filler);
        var first = (await _filesApi.OpenEditFileAsync(formId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var draftId = int.Parse(first.FillingSessionId.Split('_')[0]);

        // Act
        var config = (await _filesApi.OpenEditFileAsync(draftId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        config.FillingSessionId.Should().Be($"{draftId}_{filler.Id}");
        config.EditorType.Should().Be(EditorType.Embedded);
    }

    [Fact]
    public async Task OpenEditFile_AnonymousThroughRoomLinkOnNotStartedForm_Returns403()
    {
        // Arrange
        var (roomId, formId) = await CreateFormInFillingRoom(start: false);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await OpenAnonymouslyThroughRoomLink(roomId, formId));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task OpenEditFile_AnonymousThroughRoomLinkOnStartedForm_GetsAnonymousSubmitOnlySession()
    {
        // Arrange
        var (roomId, formId) = await CreateFormInFillingRoom(start: true);

        // Act
        var config = await OpenAnonymouslyThroughRoomLink(roomId, formId);

        // Assert
        config.FillingSessionId.Should().StartWith("anon_");
        config.EditorType.Should().Be(EditorType.Embedded);
        config.StartFillingMode.Should().NotBe(StartFillingMode.StartFillingFormRoom, "an anonymous visitor is offered no filling button");
    }

    [Fact]
    public async Task OpenEditFile_OwnFormInMyDocuments_OffersShareToFillOutAndMayStart()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var form = await CreateFileInMy("Autotest Open My Form.pdf", Owner);

        // Act
        var config = (await _filesApi.OpenEditFileAsync(form.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        config.StartFillingMode.Should().Be(StartFillingMode.ShareToFillOut);
        config.StartFilling.Should().BeTrue();
    }

    [Fact]
    public async Task OpenEditFile_FormInCustomRoom_OffersShareToFillOut()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Open Custom Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);

        // Act
        var config = (await _filesApi.OpenEditFileAsync(form.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        config.StartFillingMode.Should().Be(StartFillingMode.ShareToFillOut);
        config.StartFilling.Should().BeTrue("outside My documents, form filling rooms and data rooms the setup keeps its default, which offers the button");
    }
}
