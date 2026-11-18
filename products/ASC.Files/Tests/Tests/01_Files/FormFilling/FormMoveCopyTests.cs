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
/// The filling state follows a PDF form that is moved or copied: a form arriving in a form filling room
/// is started there and shown to its form fillers, and a form leaving the room loses its filling state.
/// </summary>
[Trait("Category", "Features")]
[Trait("Feature", "FormFilling")]
public class FormMoveCopyTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    private async Task<(int RoomId, User Filler)> CreateFillingRoomWithFiller()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Form Move Room " + Guid.NewGuid().ToString()[..8]);
        var filler = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, filler, FileShare.FillForms);

        return (room.Id, filler);
    }

    private async Task Transfer(int fileId, int destFolderId, bool move)
    {
        var request = new BatchRequestDto
        {
            ReturnSingleOperation = true,
            FileIds = [new BatchRequestDtoAllOfFileIds(fileId)],
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(destFolderId),
            DeleteAfter = false
        };

        if (move)
        {
            await _filesOperationsApi.MoveBatchItemsAsync(request, TestContext.Current.CancellationToken);
        }
        else
        {
            await _filesOperationsApi.CopyBatchItemsAsync(request, TestContext.Current.CancellationToken);
        }

        var statuses = await WaitLongOperation();
        statuses.Should().NotBeNull();
        statuses!.Should().OnlyContain(s => s.Finished && string.IsNullOrEmpty(s.Error));
    }

    private async Task<FileDto> SingleFileIn(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Files.OfType<FileDto>().Single(f => f.FileExst == ".pdf");
    }

    [Fact]
    public async Task MoveBatchItems_PdfFromMyDocumentsIntoFillingRoom_StartsFillingAndShowsItToFormFillers()
    {
        // Arrange
        var (roomId, filler) = await CreateFillingRoomWithFiller();
        var pdf = await CreateFileInMy("Autotest Moved In.pdf", Owner);

        // Act
        await Transfer(pdf.Id, roomId, move: true);

        // Assert
        var moved = (await _filesApi.GetFileInfoAsync(pdf.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        moved.FolderId.Should().Be(roomId);
        moved.StartFilling.Should().BeTrue();

        await _filesClient.Authenticate(filler);
        var asFiller = (await _filesApi.GetFileInfoAsync(pdf.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        asFiller.StartFilling.Should().BeTrue();
    }

    /// <summary>
    /// A move into the room starts the form (<see cref="MoveBatchItems_PdfFromMyDocumentsIntoFillingRoom_StartsFillingAndShowsItToFormFillers"/>),
    /// and so do a creation and an upload there; the copy branch of the operation writes no filling state,
    /// so the copy arrives not started and hidden from the form fillers. Suspected defect F5 of the
    /// form-filling-workflow spec.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task CopyBatchItems_PdfFromMyDocumentsIntoFillingRoom_StartsTheCopy()
    {
        // Arrange
        var (roomId, _) = await CreateFillingRoomWithFiller();
        var pdf = await CreateFileInMy("Autotest Copied In.pdf", Owner);

        // Act
        await Transfer(pdf.Id, roomId, move: false);

        // Assert
        var copy = await SingleFileIn(roomId);
        copy.Id.Should().NotBe(pdf.Id);
        copy.StartFilling.Should().BeTrue();
    }

    [Fact]
    public async Task MoveBatchItems_StartedFormOutOfFillingRoom_DropsItsFillingState()
    {
        // Arrange
        var (roomId, _) = await CreateFillingRoomWithFiller();
        var form = await CreateFormInRoom(roomId);
        await StartFormFilling(form.Id);
        var myFolderId = await GetUserFolderIdAsync(Owner);

        // Act
        await Transfer(form.Id, myFolderId, move: true);

        // Assert
        var moved = (await _filesApi.GetFileInfoAsync(form.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        moved.FolderId.Should().Be(myFolderId);
        moved.StartFilling.Should().NotBe(true);
        moved.ResultsFolderId.Should().BeNull();
    }

    [Fact]
    public async Task MoveBatchItems_FormWithRolesOutOfVirtualDataRoom_DropsItsRoles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Form Move VDR " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);
        await _filesApi.SaveFormRoleMappingAsync(
            form.Id.ToString(),
            new SaveFormRoleMappingDto(form.Id, [new FormRoleRequest(roomId: room.Id, roleName: "Alpha", roleColor: "ff0000", userId: Owner.Id)]),
            TestContext.Current.CancellationToken);
        var target = await CreateCustomRoom("Autotest Form Move Target " + Guid.NewGuid().ToString()[..8]);

        // Act
        await Transfer(form.Id, target.Id, move: true);

        // Assert
        (await _filesApi.GetAllFormRolesAsync(form.Id, TestContext.Current.CancellationToken)).Response.Should().BeEmpty();
        (await _filesApi.GetFileInfoAsync(form.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.StartFilling.Should().NotBe(true);
    }
}
