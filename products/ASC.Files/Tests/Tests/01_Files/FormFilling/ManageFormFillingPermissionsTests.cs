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
/// Access-level scenarios of <c>PUT /files/file/:fileId/manageformfilling</c>: starting a form
/// is open to anyone with <see cref="FileShare.ContentCreator"/> or above, but stopping one someone
/// else started requires <see cref="FileShare.RoomManager"/> - matching
/// <c>FileSecurity.CanStopFillingAsync</c>'s "owner or the user who started it" rule.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "FormFilling")]
public class ManageFormFillingPermissionsTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    private async Task<(int RoomId, int FormId)> CreateRoomWithOwnerUploadedForm(string title)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom(title + " " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);

        return (room.Id, form.Id);
    }

    [Fact]
    public async Task ManageFormFilling_DocSpaceAdminWithRoomManagerAccess_CanStartAndStop()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest ManageFormFilling RoomManager Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await InviteToRoom(room.Id, admin, FileShare.RoomManager);

        // Act & Assert
        await _filesClient.Authenticate(admin);
        await StartFormFilling(form.Id);
        await StopFormFilling(form.Id);
    }

    [Fact]
    public async Task ManageFormFilling_ContentCreatorCanStartFormTheyDidNotUpload()
    {
        // Arrange - the owner uploads the form, a plain member only holds ContentCreator access.
        var (roomId, formId) = await CreateRoomWithOwnerUploadedForm("Autotest ManageFormFilling ContentCreator Start Room");

        var contentCreator = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, contentCreator, FileShare.ContentCreator);

        // Act & Assert
        await _filesClient.Authenticate(contentCreator);
        await StartFormFilling(formId);
    }

    [Fact]
    public async Task ManageFormFilling_ContentCreatorCanStartAndStopFormTheyCreated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest ManageFormFilling ContentCreator Own Form Room " + Guid.NewGuid().ToString()[..8]);

        var contentCreator = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, contentCreator, FileShare.ContentCreator);

        await _filesClient.Authenticate(contentCreator);
        var form = await CreateFormInRoom(room.Id);

        // Act & Assert
        await StartFormFilling(form.Id);
        await StopFormFilling(form.Id);
    }

    /// <summary>
    /// BUG 81470 (fixed): a <see cref="FileShare.ContentCreator"/> used to be unable to stop form
    /// filling they themselves had started on a form someone else uploaded. It works now - the trait
    /// stays so a regression is immediately attributable to this bug number.
    /// </summary>
    [Trait("Bug", "81470")]
    [Fact]
    public async Task ManageFormFilling_ContentCreatorCanStopFormFillingTheyStarted()
    {
        // Arrange - the owner uploads the form, the content creator is the one who starts filling it.
        var (roomId, formId) = await CreateRoomWithOwnerUploadedForm("Autotest ManageFormFilling ContentCreator Stop Started Room");

        var contentCreator = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, contentCreator, FileShare.ContentCreator);

        await _filesClient.Authenticate(contentCreator);
        await StartFormFilling(formId);

        // Act & Assert
        await StopFormFilling(formId);
    }

    [Fact]
    public async Task ManageFormFilling_ContentCreatorCannotStopFormFillingStartedByOwner()
    {
        // Arrange
        var (roomId, formId) = await CreateRoomWithOwnerUploadedForm("Autotest ManageFormFilling ContentCreator Cannot Stop Room");

        var contentCreator = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, contentCreator, FileShare.ContentCreator);

        await _filesClient.Authenticate(Owner);
        await StartFormFilling(formId);

        // Act & Assert
        await _filesClient.Authenticate(contentCreator);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await StopFormFilling(formId));

        exception.ErrorCode.Should().Be(403);
    }
}
