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
/// <c>GET /files/file/:fileId/openedit?edit=true</c> and <c>PUT .../manageformfilling</c> with
/// <see cref="FormFillingManageAction.Edit"/> on a started form in a form filling room. The editor
/// re-requests its configuration in its own mode when the document server asks to refresh the file,
/// and that mode is "edit" while a form is being filled too. The request used to take the form off
/// filling for whoever sent it - an anonymous filler through the room link included, who then got
/// 403 while the stop had already been saved - so filling stopped for everyone with
/// <c>Users [guest]. Form stopped</c> in the audit. Now only a caller who may edit the form stops it.
/// </summary>
[Trait("Category", "Features")]
[Trait("Feature", "FormFilling")]
public class OpenEditFormFillingTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    private async Task<(int RoomId, int FormId)> CreateStartedForm()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest OpenEdit Filling Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        await StartFormFilling(form.Id);

        return (room.Id, form.Id);
    }

    private async Task<bool?> GetStartFilling(int formId)
    {
        await _filesClient.Authenticate(Owner);

        return (await _filesApi.GetFileInfoAsync(formId, cancellationToken: TestContext.Current.CancellationToken)).Response.StartFilling;
    }

    [Fact]
    public async Task OpenEditFile_AnonymousThroughRoomLinkWithEdit_KeepsFormFilling()
    {
        // Arrange
        var (roomId, formId) = await CreateStartedForm();
        var link = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(roomId, TestContext.Current.CancellationToken)).Response;

        // Act
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, link.SharedLink.RequestToken);

        try
        {
            await _filesApi.OpenEditFileAsync(formId, edit: true, cancellationToken: TestContext.Current.CancellationToken);
            await _filesApi.OpenEditFileAsync(formId, cancellationToken: TestContext.Current.CancellationToken);
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        }

        // Assert
        (await GetStartFilling(formId)).Should().BeTrue("an anonymous filler cannot edit the form, so edit=true must not stop filling");
    }

    [Fact]
    public async Task OpenEditFile_FormFillerWithEdit_KeepsFormFilling()
    {
        // Arrange
        var (roomId, formId) = await CreateStartedForm();
        var filler = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, filler, FileShare.FillForms);

        // Act
        await _filesClient.Authenticate(filler);
        await _filesApi.OpenEditFileAsync(formId, edit: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        (await GetStartFilling(formId)).Should().BeTrue("a member with form filling access cannot edit the form, so edit=true must not stop filling");
    }

    /// <summary>
    /// A content creator may edit a started form only when they uploaded it or started its filling,
    /// the same rule that keeps them from stopping a filling somebody else started.
    /// </summary>
    [Fact]
    public async Task OpenEditFile_ContentCreatorOfAnotherUsersFormWithEdit_KeepsFormFilling()
    {
        // Arrange
        var (roomId, formId) = await CreateStartedForm();
        var creator = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, creator, FileShare.ContentCreator);

        // Act
        await _filesClient.Authenticate(creator);
        await _filesApi.OpenEditFileAsync(formId, edit: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        (await GetStartFilling(formId)).Should().BeTrue("a content creator cannot edit a form somebody else uploaded and started");
    }

    [Fact]
    public async Task ManageFormFilling_EditByFormFiller_ReturnsForbiddenAndKeepsFormFilling()
    {
        // Arrange
        var (roomId, formId) = await CreateStartedForm();
        var filler = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, filler, FileShare.FillForms);

        // Act
        await _filesClient.Authenticate(filler);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.ManageFormFillingAsync(
                formId.ToString(),
                new ManageFormFillingDto(formId, FormFillingManageAction.Edit),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        (await GetStartFilling(formId)).Should().BeTrue();
    }

    [Fact]
    public async Task OpenEditFile_OwnerWithEdit_StopsFormFilling()
    {
        // Arrange
        var (_, formId) = await CreateStartedForm();

        // Act
        await _filesApi.OpenEditFileAsync(formId, edit: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        (await GetStartFilling(formId)).Should().BeFalse("opening the form for editing takes it off filling");
    }
}
